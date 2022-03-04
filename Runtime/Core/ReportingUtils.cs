using System;
using System.Collections;
using System.Collections.Generic;
using JetBrains.Annotations;
using Unity.Simulation;
using UnityEngine;

namespace Unity.Simulation
{
    [Serializable]
    public class ProfilerReport
    {
        public string name;
        public Category Category;
        public EventBase[] events;
    }

    public class ReportingUtils
    {
        /// <summary>
        /// Get json string for profiler report.
        /// </summary>
        /// <param name="events">List of events.</param>
        /// <param name="name">Name of the collector.</param>
        /// <param name="category">Category for the collector.</param>
        /// <returns></returns>
        public static ProfilerReport GetProfilerReport(List<EventBase> events, string name, Category category)
        {
            EventBase[] eventsData = new EventBase[events.Count];
            for (int i = 0; i < events.Count; i++)
            {
                events[i].dispatchDelegate?.Invoke();
                eventsData[i] = events[i];
            }
            var profilerReport = new ProfilerReport()
            {
                name = name,
                Category = category,
                events = eventsData
            };
            return profilerReport;
        }
    }   
}
