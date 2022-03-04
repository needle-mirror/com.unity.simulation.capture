using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Newtonsoft.Json;
using Unity.Simulation;
using UnityEngine.Assertions.Must;
using Debug = UnityEngine.Debug;

namespace Unity.Simulation
{
    /// <summary>
    /// Event for sampling a piece of the code
    /// </summary>
    public class ProfilerSamplingEvent : EventBase
    {
        private ConcurrentDictionary<string, Sample> _allSamples;
        public Dictionary<string, AggregatedSample> AggregatedSamples;
        private HashSet<string> uniqueSampleNames = new HashSet<string>();
        public ProfilerSamplingEvent(string eventName) : base(eventName, -1)
        {
            _allSamples = new ConcurrentDictionary<string, Sample>();
            dispatchDelegate += () =>
            {
                foreach (var sname in uniqueSampleNames)
                {
                    var aggSample = new AggregatedSample()
                    {
                        min = Double.MaxValue,
                        max = Double.MinValue,
                        avg = 0
                    };

                    var aggSampleDefaultUpdate = false;
                    
                    int count = 0;
                    foreach (var keyValuePair in _allSamples)
                    {
                        if (keyValuePair.Key.StartsWith(sname) && keyValuePair.Value.sampleValues.Count > 0)
                        {
                            count++;
                            var entry = keyValuePair.Value;
                            double avg = 0;
                            var queueCount = entry.sampleValues.Count;
                            while (entry.sampleValues.Count > 0)
                            {
                                var val = entry.sampleValues.Dequeue();
                                if (val < aggSample.min)
                                    aggSample.min = val;
                                if (val > aggSample.max)
                                    aggSample.max = val;
                                avg += val;
                                aggSampleDefaultUpdate = true;
                            }

                            aggSample.avg = aggSample.avg + (avg / queueCount);
                        }
                    }

                    if (!aggSampleDefaultUpdate || count == 0)
                        return;
                    
                    aggSample.avg = aggSample.avg / count;

                    if (AggregatedSamples == null)
                    {
                        AggregatedSamples = new Dictionary<string, AggregatedSample>();
                    }

                    if(!AggregatedSamples.ContainsKey(sname))
                        AggregatedSamples.Add(sname, aggSample);
                    else
                    {
                        AggregatedSamples[sname] = aggSample;
                    }
                }
            };
        }

        /// <summary>
        /// Start sampling. This start the stopwatch for the provided sample name.
        /// </summary>
        /// <param name="key">Name of the sample that is uniquely identifiable in the associated collector.</param>
        public void BeginSample(string key)
        {
            uniqueSampleNames.Add(key);
            //Add ThreadID to the key
            key += "_" + Thread.CurrentThread.ManagedThreadId;
            
            if (!_allSamples.ContainsKey(key))
            {
                if (!_allSamples.TryAdd(key, new Sample()
                {
                    threadID = Thread.CurrentThread.ManagedThreadId,
                    value = 0,
                    sampleValues = new Queue<double>(),
                    stopwatch = new Stopwatch()
                }))
                {
                    Log.E("[Simulation Profiler]: Cannot add Sample for event: " + eventName);
                }
            }
            _allSamples[key].stopwatch.Reset();
            _allSamples[key].stopwatch.Start();
        }

        /// <summary>
        /// End sample. This stops the stopwatch for provided sample name and captures the elasped time.
        /// </summary>
        /// <param name="key">Name of the sample used to begin sample.</param>
        public void EndSample(string key)
        {
            Debug.Assert(uniqueSampleNames.Contains(key), "This should not happen. " +
                                                          "Perhaps, EndSample is called before BeginSample");
            key += "_" + Thread.CurrentThread.ManagedThreadId;
            
            if (!_allSamples.ContainsKey(key))
            {
                Log.E("[Simulation Profiler]: No sample named: " + key);
            }
            else
            {
                Debug.Assert(Thread.CurrentThread.ManagedThreadId == _allSamples[key].threadID, 
                    "End sample cannot be called from a differnt thread. BeginSample was called from threadID: " + 
                    _allSamples[key].threadID);

                var elapsedTimeSeconds = _allSamples[key].stopwatch.ElapsedMilliseconds/1000.0f;
                _allSamples[key].stopwatch.Stop();
                Interlocked.Exchange(ref _allSamples[key].value, elapsedTimeSeconds);
                Interlocked.Exchange(ref _allSamples[key].timeSinceStartup, PerfStatsManager.Instance.realTimeSinceStartUp);
                _allSamples[key].sampleValues.Enqueue(_allSamples[key].value);
            }
        }
    }

    [Serializable]
    public class Sample
    {
        [JsonIgnore]
        public int threadID;
        [JsonIgnore]
        public Queue<double> sampleValues;
        [JsonIgnore]
        public Stopwatch stopwatch;
        
        public double value;
        public float timeSinceStartup;
    }


    public struct AggregatedSample
    {
        public double min;
        public double max;
        public double avg;
    }
}
