using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Unity.Simulation
{
    public class SimulationProfiler : SimulationProfilerBase
    {
    
        [SerializeReference]
        public ProfilerProperties m_ProfilerProperties = new ProfilerProperties(); 
        public override ProfilerProperties Properties => m_ProfilerProperties;
    
        public override void ApplySimulationConstantsProperties()
        {
            Time.fixedDeltaTime = (1 / Properties.fixedUpdateFrequency);
            Time.timeScale = Properties.timeScale;
        }
    }   
}
