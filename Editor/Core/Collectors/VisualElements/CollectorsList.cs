using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using UnityEditor;
using UnityEditor.Simulation.Core.Profiler;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.VFX;

namespace Unity.Simulation
{
    public class CollectorsList : VisualElement
    {
        VisualElement m_Container;
        SerializedProperty m_Property;
        
        public SimulationProfilerBase simulationProfiler => (SimulationProfilerBase)m_Property.serializedObject.targetObject;
        
        public CollectorsList(SerializedProperty property)
        {
            m_Property = property;
            AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
                $"{CoreProfilerUtils.uxmlDir}/Collectors/CollectorsList.uxml").CloneTree(this);

            m_Container = this.Q<VisualElement>("collectors-container");

            var addCollectorButton = this.Q<Button>("add-collectors-button");
            addCollectorButton.clicked += () =>
            {
                inspectorContainer.Add(new AddCollectorsMenu(inspectorContainer, addCollectorButton, this));
            };

            var expandAllButton = this.Q<Button>("expand-all");
            expandAllButton.clicked += () => CollapseCollectors(false);

            var collapseAllButton = this.Q<Button>("collapse-all");
            collapseAllButton.clicked += () => CollapseCollectors(true);

            RefreshList();
            Undo.undoRedoPerformed += () =>
            {
                m_Property.serializedObject.Update();
                RefreshList();
            };
        }
        
        void CollapseCollectors(bool collapsed)
        {
            foreach (var child in m_Container.Children())
                ((CollectorElement)child).collapsed = collapsed;
        }
        
        public void RemoveCollector(CollectorElement element)
        {
            Undo.RegisterCompleteObjectUndo(m_Property.serializedObject.targetObject, "Remove Collector");
            simulationProfiler.RemoveCollector(element.parent.IndexOf(element));
            m_Property.serializedObject.Update();
            RefreshList();
        }
        
        public void AddCollector(Type collectorType)
        {
            Undo.RegisterCompleteObjectUndo(m_Property.serializedObject.targetObject, "Add Collector");
            simulationProfiler.CreateCollector(collectorType);
            m_Property.serializedObject.Update();
            RefreshList();
        }
        
        void RefreshList()
        {
            m_Container.Clear();
            if (m_Property.arraySize > 0 &&
                string.IsNullOrEmpty(m_Property.GetArrayElementAtIndex(0).managedReferenceFullTypename))
            {
                var textElement = new TextElement()
                {
                    text = "One or more collectors have missing scripts. See console for more info."
                };
                textElement.AddToClassList("scenario__info-box");
                textElement.AddToClassList("scenario__error-box");
                m_Container.Add(textElement);
                return;
            }

            for (var i = 0; i < m_Property.arraySize; i++)
                m_Container.Add(new CollectorElement(m_Property.GetArrayElementAtIndex(i), this));
        }
        
        VisualElement inspectorContainer
        {
            get
            {
                var viewport = parent;
                while (!viewport.ClassListContains("unity-inspector-main-container"))
                    viewport = viewport.parent;
                return viewport;
            }
        }

    }   
}
