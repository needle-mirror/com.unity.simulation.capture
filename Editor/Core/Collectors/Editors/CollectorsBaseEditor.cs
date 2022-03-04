using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEditor;
using UnityEditor.Simulation.Core.Profiler;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.Simulation
{
    [CustomEditor(typeof(SimulationProfilerBase))]
    public class CollectorsBaseEditor : Editor
    {
        VisualElement m_ConstantsListVisualContainer;
        bool m_HasConstantsField;
        VisualElement m_InspectorPropertiesContainer;
        VisualElement m_CollectorListPlaceholder;
        VisualElement m_Root;
        private Toggle m_Dispatchers;
        private SimulationProfilerBase m_SimulationProfilerBase;
        SerializedObject m_SerializedObject;

        public override VisualElement CreateInspectorGUI()
        {
            m_SimulationProfilerBase = (SimulationProfilerBase)target;
            m_SerializedObject = new SerializedObject(m_SimulationProfilerBase);
            m_Root = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
                $"{CoreProfilerUtils.uxmlDir}/SimulationProfilerBaseElement.uxml").CloneTree();

            m_CollectorListPlaceholder = m_Root.Q<VisualElement>("collectors-list-placeholder");
            m_Dispatchers = m_Root.Q<Toggle>("configuration-global-dispatchers");
            
            m_Dispatchers.RegisterValueChangedCallback(evt =>
            {
                var val = evt.newValue;
                var dispatcher = serializedObject.FindProperty(nameof(SimulationProfilerBase.globalDispatchers));
                if (val)
                {
                    Undo.RegisterCompleteObjectUndo(serializedObject.targetObject, "Adding global dispatcher");
                    var globalDispatchers  = CoreProfilerUtils.GetConstructableDerivedTypes<IGlobalProfilerDataDispatcher>();
                    m_SimulationProfilerBase.globalDispatchers = new List<IGlobalProfilerDataDispatcher>();
                    for (int i = 0; i < globalDispatchers.Length; i++)
                    {
                        m_SimulationProfilerBase.globalDispatchers.Add((IGlobalProfilerDataDispatcher) Activator.CreateInstance(globalDispatchers[i]));
                    }
                    
                    serializedObject.Update();
                }
                else
                {
                    Undo.RegisterCompleteObjectUndo(serializedObject.targetObject, "Removing global dispatcher");
                    m_SimulationProfilerBase.globalDispatchers = new List<IGlobalProfilerDataDispatcher>();
                    serializedObject.Update();
                }
            });

            CreatePropertyFields();
            CheckIfConstantsExist();

            return m_Root;
        }
        
        void CreatePropertyFields()
        {
            m_InspectorPropertiesContainer = m_Root.Q<VisualElement>("inspector-properties");
            m_InspectorPropertiesContainer.Clear();

            m_ConstantsListVisualContainer = m_Root.Q<VisualElement>("constants-list");
            m_ConstantsListVisualContainer.Clear();

            var foundProperties = false;
            m_HasConstantsField = false;

            var iterator = m_SerializedObject.GetIterator();
            iterator.NextVisible(true);
            iterator.NextVisible(false);
            do
            {
                switch (iterator.name)
                {
                    case "m_Collectors":
                        m_CollectorListPlaceholder.Add(new CollectorsList(iterator.Copy()));
                        break;
                    case "m_ProfilerProperties":
                        m_HasConstantsField = true;
                        UIElementsEditorUtilities.CreatePropertyFields(iterator.Copy(), m_ConstantsListVisualContainer);
                        break;
                    case "globalDispatcherOverride":
                        break;
                    default:
                    {
                        foundProperties = true;
                        var propertyField = UIElementsEditorUtilities.CreatePropertyField(iterator, m_SimulationProfilerBase.GetType());
                        m_InspectorPropertiesContainer.Add(propertyField);
                        break;
                    }
                }
            } while (iterator.NextVisible(false));

            if (!foundProperties)
                m_InspectorPropertiesContainer.style.display = new StyleEnum<DisplayStyle>(DisplayStyle.None);
        }
        
        void CheckIfConstantsExist()
        {
            m_ConstantsListVisualContainer = m_Root.Q<VisualElement>("constants-container");
            if (!m_HasConstantsField)
            {
                m_InspectorPropertiesContainer.style.marginBottom = 0;
                m_ConstantsListVisualContainer.style.display = new StyleEnum<DisplayStyle>(DisplayStyle.None);
            }
        }

    }   
}
