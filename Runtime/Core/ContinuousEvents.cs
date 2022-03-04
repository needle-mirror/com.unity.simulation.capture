#if !UNITY_SIMULATION_SDK_DISABLED
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Unity.Simulation;
using UnityEngine;
using UnityEngine.Profiling;

using Debug = UnityEngine.Debug;

namespace Unity.Simulation
{
    /// <summary>
    /// Continuous Events class for creating and updating events that measure some metric at a frequency and aggregate over a period.
    /// </summary>
    public static class ContinuousEvents
    {
        // Public Members

        /// <summary>
        /// Dispatch delegate for consuming aggregated event data.
        /// </summary>
        /// <param name="event">The event whose aggregated data you wish to consume.</param>
        public delegate void EventDispatchDelegate();

        /// <summary>
        /// Delegate for collecting some metric.
        /// </summary>
        /// <returns>the metric represented by a double.</returns>
        public delegate double EventCollectionDelegate();

        /// <summary>
        /// Create a continuous event at a particular frequency, aggregated over a period.
        /// </summary>
        /// <param name="name">The name of the event.</param>
        /// <param name="interval">The interval in which to collect the metric, in seconds.</param>
        /// <param name="period">The aggregation period in seconds.</param>
        /// <param name="collector">A delegate to collect the metric.</param>
        /// <returns>A newly constructed Event instance.</returns>
        public static AggregationEvent Create(string name, float interval, float period, EventCollectionDelegate collector)
        {
            //var e = new Event(name, interval, period, collector);
            var e = new AggregationEvent(name, interval, collector);
            AddEvent(e);
            return e;
        }

        /// <summary>
        /// Add an event to be measure according to its frequency.
        /// </summary>
        /// <param name="event">The event to add.</param>
        public static void AddEvent(EventBase e)
        {
            if (!_events.Contains(e))
                _events.Add(e);
        }

        /// <summary>
        /// Removes an event from being collected.
        /// </summary>
        /// <param name="event">The event to remove.</param>
        public static void RemoveEvent(EventBase e)
        {
            _events.Remove(e);
        }

        /// <summary>
        /// The default dispatcher, which just logs to the console.
        /// </summary>
        /// <param name="event">The event to log.</param>
        /// <returns></returns>
        public static void DefaultDispatchDelegate(AggregationEvent e)
        {
            Debug.Log($"Event {e.eventName} min {e.min} max {e.max} mean {e.mean} variance {e.variance}");
        }
        

        // Non Public Members
        static List<EventBase> _events = new List<EventBase>();
    }
}
#endif // !UNITY_SIMULATION_SDK_DISABLED
