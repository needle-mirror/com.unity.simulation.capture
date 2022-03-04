#if !UNITY_SIMULATION_SDK_DISABLED
using System;
using System.IO;
using System.Diagnostics;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;

using Debug = UnityEngine.Debug;
using UnityEngine.Assertions;

namespace Unity.Simulation
{
    /// <summary>
    /// The primary manager class for the SDK.
    /// Responsible for tracking data produced, uploading it, and waiting for it to complete.
    /// </summary>
    [Obsolete("Obsolete msg -> Manager (UnityUpgradable)", true)]
    public sealed class DXManager {}

    /// <summary>
    /// DataCapture path constants.
    /// </summary>
    public struct DataCapturePaths
    {
        /// <summary>
        /// Specifies log files location.
        /// </summary>
        public readonly static string Logs = "Logs";

        /// <summary>
        /// Specifies screen capture files location.
        /// </summary>
        public readonly static string ScreenCapture = "ScreenCapture";

        /// <summary>
        /// Specifies file chunks location.
        /// </summary>
        public readonly static string Chunks = "Chunks";
    }

    /// <summary>
    /// The primary manager class for the SDK.
    /// Responsible for tracking data produced, uploading it, and waiting for it to complete.
    /// </summary>
    public sealed class Manager
    {
        internal const string kProfilerLogFileName = "profilerLog.raw";
        internal const string kPlayerLogFileName = "Player.Log";
        internal const string kHeartbeatFileName = "heartbeat.txt";

        string[] _uploadsBlackList = new string[]
        {
            kProfilerLogFileName,
            kHeartbeatFileName
        };

        private static int kMaxTimeBeforeShutdown = 600;

        private float _shutdownTimer = 0;

        private ICondition _shutdownCondition;

        internal string storagePath;

        public ICondition ShutdownCondition
        {
            get => _shutdownCondition;

            set
            {
                if (_shutdownRequested)
                {
                    Log.E("Cannot set a shutdown condition after the shutdown has been requested");
                }
                else
                {
                    _shutdownCondition = value;
                }
            }
        }

        /// <summary>
        /// Accessor to enable/disable the profiler.
        /// </summary>
        public bool ProfilerEnabled { get; set; }

        /// <summary>
        /// Returns the path to the profiler log.
        /// </summary>
        public string ProfilerPath
        {
            get
            {
                return Path.Combine(GetDirectoryFor(DataCapturePaths.Logs), kProfilerLogFileName);
            }
        }

        ConcurrentDictionary<Type, ConcurrentBag<AsyncRequest>> _requestPool = new ConcurrentDictionary<Type, ConcurrentBag<AsyncRequest>>();

        ConcurrentDictionary<AsyncRequest, int> _requestsInFlight = new ConcurrentDictionary<AsyncRequest, int>();

        ConcurrentQueue<Action> _endOfFrameActionQueue = new ConcurrentQueue<Action>();

        ConcurrentQueue<Action> _mainThreadActionQueue = new ConcurrentQueue<Action>();

        List<IDataProduced> _dataConsumers = new List<IDataProduced>();

        /// <summary>
        /// Register a consumer for the data being generated.
        /// </summary>
        /// <param name="consumer">IDataProduced consumer to be added to the list of consumers.</param>
        public void RegisterDataConsumer(IDataProduced consumer)
        {
            if (!_dataConsumers.Contains(consumer))
            {
                if (consumer != null && consumer.Initialize())
                {
                    Log.V($"Registered consumer {consumer.GetType().Name}.");
                    _dataConsumers.Add(consumer);
                }
                else
                {
                    Log.E($"Failed to register consumer {consumer.GetType().Name}. Initialize failed.");
                }
            }
        }

        /// <summary>
        /// Remove the consumer from the list of consumers
        /// </summary>
        /// <param name="consumer">IDataProduced consumer to be removed from the list.</param>
        public void UnregisterDataConsumer(IDataProduced consumer)
        {
            if (_dataConsumers.Contains(consumer))
                _dataConsumers.Remove(consumer);
        }

        /// <summary>
        /// Returns AsyncRequests pool count.
        /// </summary>
        public int requestPoolCount
        {
            get
            {
                var count = 0;
                foreach (var kv in _requestPool)
                    count += kv.Value.Count;
                return count;
            }
        }

        /// <summary>
        /// Delegate declaration for per frame ticks.
        /// </summary>
        public delegate void TickDelegate(float dt);

        /// <summary>
        /// Delegate for receiving per frame ticks.
        /// </summary>
        public TickDelegate Tick;

        /// <summary>
        /// Delegate declaration for notifications.
        /// </summary>
        public delegate void NotificationDelegate();

        /// <summary>
        /// Delegate which is called when the SDK starts.
        /// </summary>
        public NotificationDelegate StartNotification;

        /// <summary>
        /// Delegate which is called when the SDK is shutting down.
        /// </summary>
        public NotificationDelegate ShutdownNotification;

        private Forward _forward;

        static bool _shutdownRequested = false;
        static bool _shutdownNotificationSent = false;
        static bool _finalUploadsDone = false;
        static bool _firstUpdate = true;

        static int _frameCount;

        private string _currentSession = Guid.NewGuid().ToString();

#if !UNITY_2019_3_OR_NEWER
        static int _maxRequestStartFramesToWait;
#endif

        /// <summary>
        /// Returns a boolean indicating if shutdown has been requested.
        /// </summary>
        public static bool ShutdownRequested { get { return _shutdownRequested; } }

        /// <summary>
        /// Returns a boolean indicating if all uploads to the cloud storage are done.
        /// </summary>
        public static bool FinalUploadsDone { get { return _finalUploadsDone; } }

        double _simulationElapsedTime = 0;

        /// <summary>
        /// Returns Simulation time elapsed in seconds.
        /// </summary>
        public double SimulationElapsedTime
        {
            get { return _simulationElapsedTime; }
        }

        double _simulationElapsedTimeUnscaled = 0;

        /// <summary>
        /// Returns unscaled simulation time in seconds.
        /// </summary>
        public double SimulationElapsedTimeUnscaled
        {
            get { return _simulationElapsedTimeUnscaled; }
        }

        Stopwatch _wallElapsedTime = new Stopwatch();

        /// <summary>
        /// Returns Wall time elapsed in seconds.
        /// </summary>
        public double WallElapsedTime
        {
            get { return _wallElapsedTime.Elapsed.TotalSeconds; }
        }

        bool ConsumptionStillInProgress()
        {
            foreach (var consumer in _dataConsumers)
                if (consumer.ConsumptionStillInProgress())
                    return true;
            return false;
        }

        private bool readyToQuit
        {
            get
            {
                _shutdownTimer += Time.deltaTime;
                Shutdown();
                return (_finalUploadsDone && !ConsumptionStillInProgress()) || _shutdownTimer >= kMaxTimeBeforeShutdown;
            }
        }

        static Manager()
        {
        }

        Manager()
        {
            _shutdownRequested = false;
            _shutdownNotificationSent = false;
            Application.wantsToQuit += () =>
            {
                return readyToQuit;
            };

#if (PLATFORM_STANDALONE_OSX || PLATFORM_STANDALONE_LINUX) && !ENABLE_IL2CPP
            InstallSigTermHandler();
            InstallSigAbortHandler();
#endif

            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(UnhandledExceptionHandler);

            storagePath = Application.persistentDataPath;

            Directory.CreateDirectory(storagePath); // TO DO: Add command line parser to accept custom path
        }

        private static readonly Manager _instance = new Manager();
        public static Manager Instance { get => _instance; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void OnAfterSceneLoad()
        {
            /// Ensure we have a GameObject with a Forward component so that Manager
            /// can receive Monobehavior game loop events. 
            var forwards = GameObject.FindObjectsOfType<Forward>();

            Assert.IsTrue(forwards.Length < 2, $"[Simulation Manager] Invalid scene state: found ({forwards.Length}) Forward components.");

            if (0 == forwards.Length)
            {
                var go = new GameObject("UnitySimulationManager");

                go.hideFlags = HideFlags.HideAndDontSave;
                GameObject.DontDestroyOnLoad(go);

                _instance._forward = go.AddComponent<Forward>();
            }

        }

        /// <summary>
        /// Forwards game loop events to Manager.
        /// </summary>
        /// <remarks>
        /// There should only be one instance of this component in the scene.
        /// </remarks>
        class Forward : MonoBehaviour
        {
            public Manager client { get => Manager.Instance; }

            void Start()
            {
                client?.Start();
            }
            void Update()
            {
                client?.Update();
            }
            void LateUpdate()
            {
                client?.LateUpdate();
            }

            void OnDestroy()
            {
                if (null != client)
                    client._forward = null;
            }

            public static IEnumerator QueueEndOfFrameItem(Action<object> callback, object functor)
            {
                yield return new WaitForEndOfFrame();
                callback(functor);
            }
        }

        /// <summary>
        /// Queues an action/callback to be executed at the end of the frame.
        /// </summary>
        /// <param name="callback">Callback action that needs to be invoked at the end of the frame.</param>
        /// <param name="functor">Functor that needs to be passed to the callback as an argument.</param>
        [Obsolete("QueueEndOfFrameItem is obsolete, use QueueForEndOfFrame instead.")]
        public void QueueEndOfFrameItem(Action<object> callback, object functor)
        {
            _forward?.StartCoroutine(Forward.QueueEndOfFrameItem(callback, functor));
        }

        /// <summary>
        /// Queues an action/callback to be executed at the end of the frame.
        /// </summary>
        /// <param name="callback">Callback action that needs to be invoked at the end of the frame.</param>
        /// <param name="functor">Functor that needs to be passed to the callback as an argument.</param>
        public void QueueForEndOfFrame(Action action)
        {
            _endOfFrameActionQueue.Enqueue(action);
        }

        /// <summary>
        /// Queues an action to be executed at the beginning of the next update.
        /// This is preferred to the QueueEndOfFrameItem, which will be deprecated in the future.
        /// </summary>
        /// <param name="action">Action that needs to be invoked.</param>
        public void QueueForMainThread(Action action)
        {
            _mainThreadActionQueue.Enqueue(action);
        }

        /// <summary>
        /// Inform the manager that file is produced and is ready for upload.
        /// </summary>
        /// <param name="filePath">Full path to the file on the local file system</param>
        /// <param name="synchronous">boolean indicating if the upload is to be done synchronously.</param>
        /// <param name="isArtifact">A flag indicating if the file being consumed is an artifact or not.</param>
        public void ConsumerFileProduced(string filePath, bool synchronous = false, bool isArtifact = true)
        {
            foreach (var consumer in _dataConsumers)
                consumer.Consume(filePath, synchronous || _shutdownRequested, isArtifact);
        }

        internal void Start()
        {
            StartNotification?.Invoke();
        }

        /// <summary>
        /// Begins shutting down the SDK.
        /// Shutdown will last until all uploads or any other consumption has completed.
        /// </summary>
        public void Shutdown()
        {
            if (!_shutdownRequested)
            {
                // Clear the shutdown condition *before* calling Update() to avoid issues with recursion.
                _shutdownCondition = null;

                _shutdownRequested = true;

#if !UNITY_2019_3_OR_NEWER
                _maxRequestStartFramesToWait = 3;
#endif
                Update();
            }
        }

        /// <summary>
        /// Shutdown after the specified number of frames have elapsed.
        /// </summary>
        /// <param name="frameCount">Number of frames to elapse before exiting the simulation.</param>
        public void ShutdownAfterFrames(int frameCount)
        {
            _shutdownCondition = new FrameCountCondition(frameCount);
        }

        /// <summary>
        /// Shutdown after the specified number of simulation seconds (scaled) have passed.
        /// </summary>
        /// <param name="seconds">Number of seconds to pass before exiting the simulation.</param>
        public void ShutdownAfterSimSeconds(double seconds)
        {
            _shutdownCondition = new SimSecondsCondition(seconds);
        }

        /// <summary>
        /// Shutdown after the specified number of wall seconds have passed.
        /// </summary>
        /// <param name="seconds">Number of seconds to pass before exiting the simulation.</param>
        public void ShutdownAfterWallSeconds(double seconds)
        {
            _shutdownCondition = new WallSecondsCondition(seconds);
        }

        public void Update()
        {
            // we cache this so that we can use it from other threads.
            _frameCount = Time.frameCount;

            if (_firstUpdate)
            {
                _firstUpdate = false;
                _simulationElapsedTime = 0;
                _simulationElapsedTimeUnscaled = 0;
                _wallElapsedTime.Start();
            }

            DispatchMainThreadActionQueue();

            _simulationElapsedTime += Time.deltaTime;
            _simulationElapsedTimeUnscaled += Time.unscaledDeltaTime;

            Tick?.Invoke(Time.unscaledDeltaTime);

            CompleteTrackedRequests();

            _forward?.StartCoroutine(OnEndOfFrame());

            if (_shutdownCondition != null && _shutdownCondition.HasConditionBeenMet())
            {
                Shutdown();
            }
        }

        public void LateUpdate()
        {
#if !UNITY_2019_3_OR_NEWER
            var jobsInFlight = _requestsInFlight.Where(r => r.Key.jobsInFlight > 0);
            if (jobsInFlight.Any())
                JobHandle.ScheduleBatchedJobs();
#endif

            if (_shutdownRequested)
            {
                if (!_finalUploadsDone)
                {
#if !UNITY_2019_3_OR_NEWER
                    // 2018.4 and older doesn't support AsyncGPUReadback.WaitAllRequests(), and hence we cannot
                    // do a synchronous Shutdown. Instead we wait a few frames for the requests to start.
                    if (AnyRequestsHaveNotStarted())
                        return;
#else
                    // The capture package may have async readback requests in flight, which if not completed,
                    // will not have started their AsyncRequest workload. This call flushes those requests, and
                    // starts the workloads, so that the following CompleteTrackedRequests picks those up.
                    AsyncGPUReadback.WaitAllRequests();
#endif

                    DispatchMainThreadActionQueue();
                    DispatchEndOfFrameActionQueue();

                    ShutdownNotification?.Invoke();
                    _shutdownNotificationSent = true;

                    // Comlete all requests that are outstanding.
                    CompleteTrackedRequests(true);

                    if (ProfilerEnabled)
                    {
                        Log.V("Disabling the Profiler to flush it down to the file system");
                        Profiler.enabled = false;
                        Profiler.enableBinaryLog = false;
                    }

                    try
                    {
                        var profilerLog = Path.Combine(GetDirectoryFor(DataCapturePaths.Logs), kProfilerLogFileName);
                        if (File.Exists(profilerLog))
                        {
                            Log.V("Profiler file length: " + new FileInfo(profilerLog).Length);
                            ConsumerFileProduced(profilerLog, true);
                        }
                    }
                    catch (Exception e)
                    {
                        Log.V($"Exception ocurred uploading profiler log: {e.ToString()}");
                    }

                    // Note: It's super important that this flag is set after the two calls to ConsumerFileProduced.
                    // If set before, they will be rejected, and if not set after, then the next frame, they will be queued again.
                    // This has the effect of preventing a shutdown indefinitely. Anything that interrupts the control flow like
                    // await can cause the setting of this flag to be deferred, allowing the next tick to queue another file.
                    _finalUploadsDone = true;
                    Log.V("Final uploads completed.");
                }

                if (readyToQuit)
                {
                    Log.V("Application quit");
                    Application.Quit();
                }
            }
        }

        IEnumerator OnEndOfFrame()
        {
            yield return new WaitForEndOfFrame();

            DispatchEndOfFrameActionQueue();
        }

        void DispatchMainThreadActionQueue()
        {
            while (_mainThreadActionQueue.TryDequeue(out Action action))
                action?.Invoke();
        }

        void DispatchEndOfFrameActionQueue()
        {
            while (_endOfFrameActionQueue.TryDequeue(out Action action))
                action?.Invoke();
        }

        bool IsFileBlacklisted(string file)
        {
            return Array.Find(_uploadsBlackList, item => item.Equals(Path.GetFileName(file))) != null;
        }

        /// <summary>
        /// Upload the data from the previous run.
        /// </summary>
        /// <param name="path">Full path to the directory containing data from the previous run.</param>
        public void UploadTracesFromPreviousRun(string path)
        {
            if (Directory.Exists(path))
            {
                var files = Directory.EnumerateFiles(path, "*.*", SearchOption.AllDirectories);
                foreach (var f in files)
                {
                    if (!IsFileBlacklisted(f))
                    {
                        if (f.Contains("/others/"))
                        {
                            ConsumerFileProduced(f, false, false);
                        }
                        else
                        {
                            ConsumerFileProduced(f);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Create an instance of an AsyncRequest.
        /// </summary>
        /// <typeparam name="T">AsyncRequest type.</typeparam>
        /// <returns>AsyncRequest of type T.</returns>
        public T CreateRequest<T>() where T : AsyncRequest, new()
        {
            if (_shutdownNotificationSent && Interlocked.Add(ref AsyncRequest._insideRequestExecutionCount, 0) == 0)
            {
                throw new InvalidOperationException("Manager.CreateRequest called after shutdown has completed.");
            }

            ConcurrentBag<AsyncRequest> bag;
            _requestPool.TryGetValue(typeof(T), out bag);

            if (bag == null)
            {
                bag = new ConcurrentBag<AsyncRequest>();
                _requestPool[typeof(T)] = bag;
            }

            AsyncRequest request;
            if (bag.TryTake(out request))
                request.Reset();
            else
                request = new T();

            _requestsInFlight.TryAdd(request, _frameCount);

            return request as T;
        }

        /// <summary>
        /// Recycle the async request and put it back in the pool.
        /// </summary>
        /// <param name="request">AsyncRequest to be recycled.</param>
        /// <typeparam name="T">AsyncRequest type T.</typeparam>
        public void RecycleRequest<T>(T request) where T : AsyncRequest
        {
            _requestPool[typeof(T)].Add(request);
        }

#if !UNITY_2019_3_OR_NEWER
        bool AnyRequestsHaveNotStarted()
        {
            if (_maxRequestStartFramesToWait == 0)
                return false;
            else
                --_maxRequestStartFramesToWait;

            foreach (var kv in _requestsInFlight)
                if (!kv.Key.started)
                    return true;

            return false;
        }
#endif

        public int GetRequestsCount(Type type)
        {
            return _requestPool.ContainsKey(type) ? _requestPool[type].Count : 0;
        }

        public void CompleteTrackedRequests(bool allRequests = false)
        {
            if (allRequests)
            {
                int value;
                foreach (var kv in _requestsInFlight)
                {
                    var req = kv.Key;
                    req.Complete();
                    Debug.Assert(req.completed == true, "Completed request has not been completed.");
                    _requestsInFlight.TryRemove(req, out value);
                }
                Debug.Assert(_requestsInFlight.Count == 0);
            }
            else
            {
                int value;
                foreach (var kv in _requestsInFlight)
                {
                    var req = kv.Key;
                    var age = _frameCount - kv.Value;
                    var max = req.requestFrameAgeToAutoComplete > 0 ? req.requestFrameAgeToAutoComplete : AsyncRequest.maxAsyncRequestFrameAge;

                    if (max > 0)
                    {
                        if (req.started && !req.completed && age >= max)
                        {
                            req.Complete();
                            Debug.Assert(req.completed == true);
                        }
                    }

                    if (req.completed)
                        _requestsInFlight.TryRemove(req, out value);
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="type"></param>
        /// <param name="userPath"></param>
        /// <returns></returns>
        public string GetDirectoryFor(string type = "", string userPath = "")
        {
            string basePath;

            if (string.IsNullOrEmpty(userPath))
            {
                basePath = Path.Combine(storagePath, _currentSession, type);
            }
            else
            {
                if (Path.HasExtension(userPath))
                {
                    Debug.Assert(Directory.Exists(userPath), "Invalid Log Path : " + userPath);
                    basePath = Path.Combine(Path.GetDirectoryName(userPath), _currentSession);
                }
                else
                {
                    basePath = Path.Combine(userPath, _currentSession, type.ToString().ToLower());
                }
            }

            if (!Directory.Exists(basePath))
                Directory.CreateDirectory(basePath);

            return basePath;
        }

        public static void UnhandledExceptionHandler(object sender, UnhandledExceptionEventArgs args)
        {
            Exception e = (Exception)args.ExceptionObject;
            Log.E($"UnhandledException : {e.Message}");
        }

#if (PLATFORM_STANDALONE_OSX || PLATFORM_STANDALONE_LINUX) && !ENABLE_IL2CPP
        delegate void SignalHandlerDelegate();

        [DllImport("__Internal")]
        static extern IntPtr signal(int signum, SignalHandlerDelegate handler);

        [DllImport("__Internal")]
        static extern IntPtr signal(int signum, int foo);

        [DllImport("__Internal")]
        static extern void abort();

        static readonly string _consoleLogPath = Application.consoleLogPath;

        SignalHandlerDelegate InstallSigTermHandler()
        {
            IntPtr previous = signal(15/*SIGTERM*/, () =>
            {
                _shutdownRequested = true;
            });
            return previous != IntPtr.Zero ? Marshal.GetDelegateForFunctionPointer<SignalHandlerDelegate>(previous) : null;
        }

        static bool _abortUploadLog = false;
        SignalHandlerDelegate InstallSigAbortHandler()
        {
            IntPtr previous = signal(6/*SIGABRT*/, () =>
            {
                if (!_abortUploadLog)
                {
                    _abortUploadLog = true;

                    if (!string.IsNullOrEmpty(_consoleLogPath))
                    {
                        foreach (var consumer in _dataConsumers)
                        {
                            if (ProfilerEnabled)
                            {
                                Profiler.enabled = false;
                                Profiler.enableBinaryLog = false;
                                ProfilerEnabled = false;
                            }

                            var profilerLog = Path.Combine(GetDirectoryFor(DataCapturePaths.Logs), kProfilerLogFileName);
                            if (File.Exists(profilerLog))
                                consumer.Consume(profilerLog, true, false);
                        }
                    }
                }
                else
                {
                    signal(6/*SIGABRT*/, 0);
                    abort();
                }
            });
            return previous != IntPtr.Zero ? Marshal.GetDelegateForFunctionPointer<SignalHandlerDelegate>(previous) : null;
        }
#endif//PLATFORM_STANDALONE_OSX || PLATFORM_STANDALONE_LINUX && !ENABLE_IL2CPP
    }
}
#endif // !UNITY_SIMULATION_SDK_DISABLED
