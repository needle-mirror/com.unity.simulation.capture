using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Simulation;
using UnityEngine;
using UnityEngine.Analytics;
using Random = System.Random;

namespace Unity.Simulation
{
    [Serializable]
    [AddCollectorMenu("Collectors/Sample Profiler Collector")]
    public class SampleProfilerCollector : CollectorBase
    {
        public override void PopulateEvents()
        {
            AddEvent(new CounterEvent("FunctionCallCounter", 10));
            AddEvent( new ProfilerSamplingEvent("SampleSamplingEvent"));
        }
    }   
}
