using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Unity.Simulation
{
#if UNITY_EDITOR
    [CustomEditor(typeof(SimulationProfiler))]
    public class SimulationProfilerEditor : CollectorsBaseEditor
    {
    }
#endif   
}
