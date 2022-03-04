using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Unity.Simulation
{
    [Serializable]
    public class ProfilerProperties
    {
        /// <summary>
        /// How many times fixed updates need to be called every second. 
        /// </summary>
        public float fixedUpdateFrequency = 60.0f;
    
        /// <summary>
        /// How many times updates are called every second.
        /// </summary>
        public int timeScale = 1;
    }

    [Serializable]
    public enum Dispatcher
    {
        CUSTOM_GLOBAL_OVERRIDE,
        DEFAUL_COLLECTORS_DISPATCHER
    }   
}
