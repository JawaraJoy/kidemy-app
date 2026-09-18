#if UNITY_EDITOR

using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace EduGame.Editor
{
    public abstract class EduGameGroupedInspectorBase : UnityEditor.Editor
    {
        private const string RootNamespace = "EduGame";

        private class InspectorGroup
        {
            public string Name;

            public readonly List<FieldInfo> Fields =
                new List<FieldInfo>();
        }

        public override void OnInspectorGUI()
        {
            if (!IsEduGameType())
            {
                DrawDefaultInspector();
                return;
            }

            serializedObject.Update();

            DrawGroupedInspector();

            serializedObject.ApplyModifiedProperties();
        }

        // =========================================================
        // Namespace Check
        // =========================================================

        private bool IsEduGameType()
        {
            string ns = target.GetType().Namespace;

            if (string.IsNullOrEmpty(ns))
                return false;

            return ns == RootNamespace ||
                   ns.StartsWith(RootNamespace + ".");
        }

        // =========================================================
        // Main Inspector
        // =========================================================

        private void DrawGroupedInspector()
        {
            List<Type> hierarchy =
                BuildHierarchy(target.GetType());

            var groups =
                new Dictionary<string, InspectorGroup>();

            var groupOrder =
                new List<string>();

            var ungroupedFields =
                new List<FieldInfo>();

            // =====================================================
            // Parent -> Child
            // =====================================================

            foreach (Type type in hierarchy)
            {
                string currentGroup = null;

                FieldInfo[] fields = type
                    .GetFields(
                        BindingFlags.Instance |
                        BindingFlags.Public |
                        BindingFlags.NonPublic |
                        BindingFlags.DeclaredOnly
                    )
                    .OrderBy(f => f.MetadataToken)
                    .ToArray();

                foreach (FieldInfo field in fields)
                {
                    if (!IsUnitySerializedField(field))
                        continue;

                    HeaderAttribute header = field
                        .GetCustomAttributes(typeof(HeaderAttribute), false)
                        .Cast<HeaderAttribute>()
                        .FirstOrDefault();

                    // =============================================
                    // Header ditemukan
                    // =============================================

                    if (header != null)
                    {
                        currentGroup = header.header;

                        // Group belum ada?
                        if (!groups.ContainsKey(currentGroup))
                        {
                            groups.Add(
                                currentGroup,
                                new InspectorGroup
                                {
                                    Name = currentGroup
                                }
                            );

                            groupOrder.Add(currentGroup);
                        }
                    }

                    // =============================================
                    // Add field
                    // =============================================

                    if (currentGroup != null)
                    {
                        groups[currentGroup]
                            .Fields
                            .Add(field);
                    }
                    else
                    {
                        ungroupedFields.Add(field);
                    }
                }
            }

            // =====================================================
            // Script
            // =====================================================

            SerializedProperty script =
                serializedObject.FindProperty("m_Script");

            if (script != null)
            {
                using (new EditorGUI.DisabledScope(true))
                {
                    EditorGUILayout.PropertyField(script);
                }
            }

            // =====================================================
            // Ungrouped
            // =====================================================

            foreach (FieldInfo field in ungroupedFields)
            {
                DrawField(field);
            }

            // =====================================================
            // Groups
            // =====================================================

            foreach (string groupName in groupOrder)
            {
                InspectorGroup group =
                    groups[groupName];

                EditorGUILayout.Space(8);

                EditorGUILayout.LabelField(
                    group.Name,
                    EditorStyles.boldLabel
                );

                foreach (FieldInfo field in group.Fields)
                {
                    DrawField(field);
                }
            }
        }

        // =========================================================
        // Build Inheritance Hierarchy
        // =========================================================

        private List<Type> BuildHierarchy(Type targetType)
        {
            List<Type> hierarchy =
                new List<Type>();

            Type current = targetType;

            while (
                current != null &&
                current != typeof(MonoBehaviour) &&
                current != typeof(ScriptableObject) &&
                current != typeof(UnityEngine.Object)
            )
            {
                hierarchy.Insert(0, current);

                current = current.BaseType;
            }

            return hierarchy;
        }

        // =========================================================
        // Draw Serialized Field
        // =========================================================

        private void DrawField(FieldInfo field)
        {
            SerializedProperty property =
                serializedObject.FindProperty(field.Name);

            if (property == null)
                return;

            EditorGUILayout.PropertyField(
                property,
                true
            );
        }

        // =========================================================
        // Unity Serialization Check
        // =========================================================

        private bool IsUnitySerializedField(FieldInfo field)
        {
            if (field.IsStatic)
                return false;

            if (field.IsNotSerialized)
                return false;

            if (field.IsPublic)
                return true;

            return field.IsDefined(
                typeof(SerializeField),
                false
            );
        }
    }

    // =============================================================
    // MonoBehaviour
    // =============================================================

    [CustomEditor(typeof(MonoBehaviour), true)]
    [CanEditMultipleObjects]
    public class EduGameMonoBehaviourInspector
        : EduGameGroupedInspectorBase
    {
    }

    // =============================================================
    // ScriptableObject
    // =============================================================

    [CustomEditor(typeof(ScriptableObject), true)]
    [CanEditMultipleObjects]
    public class EduGameScriptableObjectInspector
        : EduGameGroupedInspectorBase
    {
    }
}

#endif