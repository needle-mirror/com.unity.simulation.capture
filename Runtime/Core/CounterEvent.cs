using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using Unity.Simulation;
using UnityEngine;


namespace Unity.Simulation
{
    [Serializable]
    public class CounterEvent : EventBase
    {
        public ConcurrentDictionary<string, CounterEventData> CounterStore;
        public CounterEvent(string eventName, float interval, ContinuousEvents.EventCollectionDelegate collector = null) : base(eventName, interval, collector)
        {
            if (CounterStore == null)
            {
                CounterStore = new ConcurrentDictionary<string, CounterEventData>();
            }
        }
    
        /// <summary>
        /// Increment the count of the function call.
        /// </summary>
        /// <param name="functionName"></param>
        public void IncrementCount(string functionName)
        {
            Debug.Assert(CounterStore != null, "The Event is not registered in the collector");
            var evntData = new CounterEventData()
            {
                count = 1,
                timeSinceStart = Time.time
            };
            CounterStore.AddOrUpdate(functionName, evntData, (k, v) =>
            {
                var eventData = v;
                Interlocked.Increment(ref eventData.count);
                Interlocked.Exchange(ref eventData.timeSinceStart, Time.time);
                return eventData;
            });
        }
    
        public override void IngestValue(double value)
        {
        }
    }
    
    /// <summary>
    /// Structure to hold counter event data.
    /// </summary>
    [Serializable]
    public struct CounterEventData
    {
        public float timeSinceStart;
        public long count;
    }
}
