#if !UNITY_SIMULATION_SDK_DISABLED
using System;
using System.Linq;
using UnityEngine;

namespace Unity.Simulation
{
    /// <summary>
    /// Represents a stream that will flush after a certain size or time threshold.
    /// </summary>
    [Obsolete("Obsolete msg -> ChunkedStream (UnityUpgradable)", true)]
    public class DXChunkedStream {}

    /// <summary>
    /// Represents a stream that will flush after a certain size or time threshold.
    /// </summary>
    public class ChunkedStream : IDisposable
    {
        /// <summary>
        /// The default stream buffer size.
        /// </summary>
        public const int kDefaultBufferSize = 8192;

        /// <summary>
        /// The default flush time in seconds.
        /// </summary>
        public const float kDefaultMaxSecondsElapsed = 5;

        /// <summary>
        /// Callback functor to call when flush occurs. Accumulated data thus far is passed to functor.
        /// </summary>
        public Func<AsyncRequest<object>, AsyncRequest.Result> functor { get; set; }

        int    _index;
        byte[] _buffer;
        object _sync = new object();
        int    _bufferSize;
        float  _maxElapsedSeconds;
        float  _elapsedSeconds;
        private bool _readyToFlush;

        /// <summary>
        /// Constructs a stream object.
        /// </summary>
        /// <param name="bufferSize">Size of the buffer in bytes.</param>
        /// <param name="maxElapsedSeconds">Amount of time after which the buffer will automatically flush.</param>
        /// <param name="functor">Callback function to pass buffered data to.</param>
        public ChunkedStream(int bufferSize = kDefaultBufferSize, float maxElapsedSeconds = kDefaultMaxSecondsElapsed, Func<AsyncRequest<object>, AsyncRequest.Result> functor = null)
        {
            _index = 0;
            _bufferSize = bufferSize;
            _buffer = new byte[_bufferSize];
            _maxElapsedSeconds = maxElapsedSeconds;
            this.functor = functor;
            _elapsedSeconds = 0;
            Manager.Instance.Tick += this.Tick;
            Manager.Instance.ShutdownNotification += this.FlushSynchronous;
        }

        /// <summary>
        /// Disposes of the stream and removes it from Tick.
        /// </summary>
        public void Dispose()
        {
            Manager.Instance.Tick -= this.Tick;
            Manager.Instance.ShutdownNotification -= this.FlushSynchronous;
        }

        /// <summary>
        /// Write the data in the buffer to the file system.
        /// </summary>
        public void Flush(bool synchronous = false)
        {
            byte[] buffer;
            lock(_sync)
            {
                if (_readyToFlush)
                {
                    buffer = _buffer;
                    _buffer = new byte[_bufferSize];
                    if (_index < buffer.Length)
                        Consume(buffer.Take(_index).ToArray(), synchronous);
                    else
                    {
                        Consume(buffer, synchronous);
                    }
                    _index = 0;
                    _elapsedSeconds = 0;
                    _readyToFlush = false;
                }
            }
        }

        void Consume(Array data, bool synchronous = false)
        {
            if (functor != null)
            {
                var executionContext = synchronous ? AsyncRequest.ExecutionContext.Immediate : AsyncRequest.ExecutionContext.None;
                var req = Manager.Instance.CreateRequest<AsyncRequest<object>>();
                req.data = data;
                req.Enqueue(functor);
                req.Execute(executionContext);
            }
        }

        /// <summary>
        /// Append the data to the byte buffer
        /// </summary>
        /// <param name="data">byte array of the data to be appended.</param>
        public void Append(byte[] data)
        {
            int length;
            int remain;

            lock(_sync)
            {
                Debug.Assert(_buffer != null);
                Debug.Assert(_index >= 0 && _index <= _buffer.Length);

                length = _buffer.Length;
                remain = _buffer.Length - _index;
            }

            if (data.Length > length)
            {
                Consume(data);
            }
            else 
            {
                if (data.Length > remain)
                    Flush();

                lock(_sync)
                {
                    Array.Copy(data, 0, _buffer, _index, data.Length);
                    _readyToFlush = true;
                    _index += data.Length;
                }
            }
        }

        internal void Tick(float dt)
        {
            if (_index > 0)
            {
                _elapsedSeconds += dt;
                if (_maxElapsedSeconds > 0 && _elapsedSeconds >= _maxElapsedSeconds)
                {
                    Flush();
                }
            }
        }

        internal void FlushSynchronous()
        {
            if(_index > 0)
                Flush(true);
        }
    }
}
#endif // !UNITY_SIMULATION_SDK_DISABLED
