using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Unity.Simulation
{
    public interface IGlobalProfilerDataDispatcher
    {
        void Initialize();
    
        void DispatchProfilerReport(ProfilerReport report);
    }   
}
