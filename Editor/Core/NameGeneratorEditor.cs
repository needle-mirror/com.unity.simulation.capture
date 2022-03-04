#if UNITY_2019_3_OR_NEWER // [SerializeReference] is only supported on 2019.3 and newer, which this editor relies on.
using System;
using System.Reflection;
using System.Linq;
using System.Text.RegularExpressions;

using UnityEngine;
using UnityEditor;
using UnityEditorInternal;

namespace Unity.Simulation
{
    [CustomEditor(typeof(NameGenerator))]
    class NameGeneratorEditor : Editor
    {
        ReorderableList    list;
        SerializedProperty components;
        string             example = "some/example/path/with/a/file.txt";

        void OnEnable()
        {
            components = serializedObject.FindProperty("components");
            list = new ReorderableList(serializedObject, components, true, true, true, true);
            list.drawHeaderCallback            += OnDrawHeaderCallback;
            list.onAddDropdownCallback         += OnAddDropdownCallback;
            list.drawElementCallback           += OnDrawElementCallback;
            list.drawElementBackgroundCallback += OnDrawElementBackgroundCallback;
            list.elementHeightCallback         += OnReorderListElementHeight;
        }

        void OnDisable()
        {
            if (list != null)
            {
                list.drawHeaderCallback            -= OnDrawHeaderCallback;
                list.onAddDropdownCallback         -= OnAddDropdownCallback;
                list.drawElementCallback           -= OnDrawElementCallback;
                list.drawElementBackgroundCallback -= OnDrawElementBackgroundCallback;
                list.elementHeightCallback         -= OnReorderListElementHeight;
                list = null;
            }
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            list.DoLayoutList();
            serializedObject.ApplyModifiedProperties();
            var nameGenerator = serializedObject.targetObject as NameGenerator;

            EditorGUIUtility.labelWidth = 50;
            example = EditorGUILayout.TextField("Input", example);
            EditorGUILayout.LabelField("Result", nameGenerator.Generate(example, false));
        }

        void OnDrawHeaderCallback(Rect rect)
        {
            EditorGUI.LabelField(rect, "Name Generator Components");
        }

        void OnAddDropdownCallback(Rect rect, ReorderableList list)
        {
            var menu = new GenericMenu();

            var nameComponents = Assembly.GetAssembly(typeof(NameComponent)).GetTypes().Where(t => t.IsSubclassOf(typeof(NameComponent)));
            foreach (var component in nameComponents)
            {
                menu.AddItem(new GUIContent(PrettyName(component.Name)), false, o =>
                {
                    list.serializedProperty.InsertArrayElementAtIndex(list.serializedProperty.arraySize);
                    var element = list.serializedProperty.GetArrayElementAtIndex(list.serializedProperty.arraySize - 1);
                    
                    element.managedReferenceValue = Activator.CreateInstance((Type)o);
                    serializedObject.ApplyModifiedProperties();

                }, component);
            }
            menu.ShowAsContext();
        }

        void OnDrawElementCallback(Rect rect, int index, bool active, bool focused)
        {
            var element = components.GetArrayElementAtIndex(index);

            var rc = rect;
            rc.height = 20;
            rc.x     += 15;
            rc.width -= 15;

            element.isExpanded = EditorGUI.BeginFoldoutHeaderGroup(rc, element.isExpanded, PrettyName(element.type.ToString()));
            if (element.isExpanded)
            {
                EditorGUI.indentLevel++;

                var end = element.GetEndProperty();

                while (element.NextVisible(true) && !SerializedProperty.EqualContents(element, end))
                {
                    rc.y     += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
                    rc.height = EditorGUIUtility.singleLineHeight;
                    EditorGUI.PropertyField(rc, element, true);
                }

                EditorGUI.indentLevel--;
            }

            EditorGUI.EndFoldoutHeaderGroup();
        }

        void OnDrawElementBackgroundCallback(Rect rect, int index, bool active, bool focused)
        {
            if (active && focused)
            {
                float height = OnReorderListElementHeight(index);

                var element = list.serializedProperty.GetArrayElementAtIndex(index);

                if (!element.isExpanded)
                    height -= EditorGUIUtility.standardVerticalSpacing;

                var rc = rect;
                rc.width  = 20;
                rc.height = height;

                EditorGUI.DrawRect(rc, Color.grey);

                float offset = 2.0f;
                rc.x      += 20;
                rc.width  -= (20 + offset);

                rc.height -= 20 + offset;
                rc.y      += 20 - offset;

                EditorGUI.DrawRect(rc, Color.grey);
            }
        }

        float OnReorderListElementHeight(int index)
        {
            var length = list.serializedProperty.arraySize;
            if (length <= 0)
                return 0.0f;

            SerializedProperty element = list.serializedProperty.GetArrayElementAtIndex(index);
            SerializedProperty end     = element.GetEndProperty();

            float height = EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

            if (!element.isExpanded)
                return height;

            while (element.NextVisible(true) && !SerializedProperty.EqualContents(element, end))
                height += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

            return height;
        }

        const string kElementNamePrefix = "managedReference<";
        const string kElementNameSuffix = "NameComponent";

        string PrettyName(string typeName)
        {
            if (typeName.StartsWith(kElementNamePrefix))
                typeName = typeName.Substring(kElementNamePrefix.Length);
            if (typeName.EndsWith(">"))
                typeName = typeName.Substring(0, typeName.Length - 1);
            if (typeName.EndsWith(kElementNameSuffix))
                typeName = typeName.Substring(0, typeName.Length - kElementNameSuffix.Length);
            return Regex.Replace(typeName, "([A-Z])", " $1").Trim();
        }
    }
}
#endif // UNITY_2019_3_OR_NEWER
