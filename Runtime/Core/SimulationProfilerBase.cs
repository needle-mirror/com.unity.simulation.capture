using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEditor;
using UnityEngine;


namespace Unity.Simulation
{
    [DefaultExecutionOrder(-1)]
    public abstract class SimulationProfilerBase : MonoBehaviour
    {
        
        [SerializeReference] List<CollectorBase> m_Collectors = new List<CollectorBase>();


        /// <summary>
        /// Set the global dispatched for all the collectors. Default option will abide to all the collectors dispatchers.
        /// </summary>
        public bool globalDispatcherOverride = false;
        
        public abstract ProfilerProperties Properties { get; }

        public ReadOnlyCollection<CollectorBase> collectors => m_Collectors.AsReadOnly();

        [SerializeReference]
        [HideInInspector]
        public List<IGlobalProfilerDataDispatcher> globalDispatchers = new List<IGlobalProfilerDataDispatcher>();

        /// <summary>
        /// Append a Collector to the end of the randomizer list
        /// </summary>
        /// <param name="newRandomizer">The Randomizer to add to the Scenario</param>
        public void AddCollector(CollectorBase newCollector)
        {
            InsertCollector(m_Collectors.Count, newCollector);
        }

        /// <summary>
        /// Insert a collector at a given index within the randomizer list
        /// </summary>
        /// <param name="index">The index to place the randomizer</param>
        /// <param name="newCollector">The collector to add to the list</param>
        /// <exception cref="Exception"></exception>
        public void InsertCollector(int index, CollectorBase newCollector)
        {
            foreach (var collector in m_Collectors)
                if (collector.GetType() == newCollector.GetType())
                    throw new Exception(
                        $"Cannot add another collector of type ${newCollector.GetType()} when " +
                        $"a collector of this type is already present in the SimulationProfiler");
            m_Collectors.Insert(index, newCollector);
        }

        /// <summary>
        /// Remove collector at index.
        /// </summary>
        /// <param name="index"></param>
        public void RemoveCollector(int index)
        {
            Debug.Assert(m_Collectors.Count > index, "No collector present at index " + index);
            
            m_Collectors.RemoveAt(index);
        }
        
        /// <summary>
        /// Called by the "Add Collector" button in the scenario Inspector
        /// </summary>
        /// <param name="collectorType">The type of collector to create</param>
        /// <returns>The newly created randomizer</returns>
        /// <exception cref="Exception"></exception>
        public CollectorBase CreateCollector(Type collectorType)
        {
            if (!collectorType.IsSubclassOf(typeof(CollectorBase)))
                throw new Exception(
                    $"Cannot add non-collecgor type {collectorType.Name} to collectors list");
            var newCollector = (CollectorBase)Activator.CreateInstance(collectorType);
            AddCollector(newCollector);
            return newCollector;
        }
        
        public IEnumerable<CollectorBase> activeCollectors
        {
            get
            {
                foreach (var collector in m_Collectors)
                    if (collector.m_Enabled)
                        yield return collector;
            }
        }

        private void Start()
        {
            ApplySimulationConstantsProperties();
            if (globalDispatchers != null && globalDispatchers.Count > 0)
            {
                foreach (var dispatcher in globalDispatchers)
                {
                    dispatcher.Initialize();
                }
                PerfStatsManager.Instance.SetGlobalDispatchers(globalDispatchers);
            }
            
            PerfStatsManager.Instance.StartProfiling(m_Collectors);
        }

        public abstract void ApplySimulationConstantsProperties();
    }   
}