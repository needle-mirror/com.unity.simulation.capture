using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Newtonsoft.Json;
using Unity.Simulation;
using UnityEngine;

namespace Unity.Simulation
{
    
    public class IgnoreCollectorAttribute : Attribute
    {
    }
    
    /// <summary>
    /// Profiler category under which the collector data needs to be displayed.
    /// </summary>
    public enum Category
    {
        LOGIC=0,
        RENDERING,
        NETWORK,
        IO
    }

    [Serializable]
    public abstract class CollectorBase
    {

        /// <summary>
        /// Field indicating if the collector is enabled.
        /// </summary>
        [SerializeField, HideInInspector] public bool m_Enabled = true;


        /// <summary>
        /// Category for simulation profiler collector. Default is set to Logic.
        /// </summary>
        [SerializeField, HideInInspector] public Category category = Category.LOGIC;

        /// <summary>
        /// Name of the collector. Default is set to the name of the type.
        /// </summary>
        [SerializeField] public string name;

        [SerializeField, HideInInspector] internal bool collapsed;
        
        private object _mutex = new Mutex();

        [HideInInspector]
        public float simulationElapsedTime;

        /// <summary>
        /// Returns if the default collector dispatcher is enabled.
        /// </summary>
        public bool defaultDispatcherEnabled
        {
            get;
            protected set;
        }

        /// <summary>
        /// Initialize your collector settings here.
        /// </summary>
        public virtual void Initialize()
        {
            defaultDispatcherEnabled = true;
            pathOnFileSystem = Manager.Instance.GetDirectoryFor(DataCapturePaths.Logs);
        }

        /// <summary>
        /// Consumer delegate to consume the collector profiler report.
        /// </summary>
        /// <param name="report"></param>
        public delegate void ConsumeCollectorReport(ProfilerReport report);

        /// <summary>
        /// Consumer delegate to consume the collector profiler report. Invoked when the collector is ready for dispatch.
        /// </summary>
        public ConsumeCollectorReport CollectorConsumer;

        private List<EventBase> _events = new List<EventBase>();
        [HideInInspector] public string pathOnFileSystem;

        /// <summary>
        /// Period at which the collector dispatch needs to be invoked.
        /// </summary>
        public int period;
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="_events"></param>
        public abstract void PopulateEvents();

        protected CollectorBase()
        {
            name = GetType().Name;
        }


        /// <summary>
        /// Dispatch collector at an interval. Override this to dispatch the
        /// results to any other location other than local file system.
        /// </summary>
        /// <param name="report"></param>
        public virtual void DispatchCollector(ProfilerReport report)
        {
            var settings = new JsonSerializerSettings()
            {
                TypeNameHandling = TypeNameHandling.None
            };
            var jsonString = JsonConvert.SerializeObject(report, Formatting.Indented, settings) + Environment.NewLine;
            pathOnFileSystem = Path.Combine(Manager.Instance.GetDirectoryFor(DataCapturePaths.Logs), name + ".json");
            var asyncReq = Manager.Instance.CreateRequest<AsyncRequest<string>>();
            asyncReq.data = jsonString;
            asyncReq.Enqueue(r =>
            {
                if (_mutex == null)
                    _mutex = new Mutex();
                
                lock (_mutex)
                {
                    if (File.Exists(pathOnFileSystem))
                        File.AppendAllText(pathOnFileSystem, r.data);
                    else
                    {
                        File.WriteAllText(pathOnFileSystem, r.data);   
                    }   
                }
                return File.Exists(pathOnFileSystem) ? AsyncRequest.Result.Completed : AsyncRequest.Result.Error;
            });
            asyncReq.Execute();
        }


        /// <summary>
        /// Add Continuous event to the collector for profiling.
        /// </summary>
        /// <param name="e"></param>
        public void AddEvent(EventBase e)
        {
            if (_events == null)
            {
                _events = new List<EventBase>();
            }
            foreach (var evnt in _events)
            {
                if (evnt.GetType() == e.GetType() && !evnt.IsMultipleEventsInstancesAllowed())
                {
                    Debug.LogError("Event of type" + e.GetType().Name + " is not allowed. This API call will be ignored.");
                    return;
                }
            }
            _events.Add(e);
        }

        /// <summary>
        /// Get all the events associated with the collector.
        /// </summary>
        /// <returns></returns>
        public List<EventBase> GetEvents()
        {
            return _events;
        }
    }   
}
