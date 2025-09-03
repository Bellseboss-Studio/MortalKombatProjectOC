// =====================================================================
// Copyright © 2013 ToolBuddy
// All rights reserved
// 
// http://www.toolbuddy.net
// =====================================================================

using FluffyUnderware.Curvy.Generator;
using FluffyUnderware.Curvy.Pools;
using JetBrains.Annotations;
using ToolBuddy.Pooling.Pools;
using UnityEditor;
using UnityEngine;

namespace FluffyUnderware.DevToolsEditor
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(ArrayPoolsSettings))]
    public class ArrayPoolsSettingsEditor : Editor
    {
        private ArrayPoolUsageData CGSpotUsageData;
        private ArrayPoolUsageData Int32UsageData;
        private ArrayPoolUsageData SingleUsageData;
        private ArrayPoolUsageData Vector2UsageData;
        private ArrayPoolUsageData Vector3UsageData;
        private ArrayPoolUsageData Vector4UsageData;


        private SerializedProperty vector2Capacity;
        private SerializedProperty vector3Capacity;
        private SerializedProperty vector4Capacity;
        private SerializedProperty intCapacity;
        private SerializedProperty floatCapacity;
        private SerializedProperty cgSpotCapacity;
        private SerializedProperty logAllocations;

        [UsedImplicitly]
        private void OnEnable()
        {
            vector2Capacity = serializedObject.FindProperty("vector2Capacity");
            vector3Capacity = serializedObject.FindProperty("vector3Capacity");
            vector4Capacity = serializedObject.FindProperty("vector4Capacity");
            intCapacity = serializedObject.FindProperty("intCapacity");
            floatCapacity = serializedObject.FindProperty("floatCapacity");
            cgSpotCapacity = serializedObject.FindProperty("cgSpotCapacity");
            logAllocations = serializedObject.FindProperty("logAllocations");
        }


        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            //bool needRepaint = Vector2UsageData != ArrayPools.Vector2.UsageData
            //              || Vector3UsageData != ArrayPools.Vector3.UsageData
            //              || Vector4UsageData != ArrayPools.Vector4.UsageData
            //              || Int32UsageData != ArrayPools.Int32.UsageData
            //              || SingleUsageData != ArrayPools.Single.UsageData
            //              || CGSpotUsageData != ArrayPools.CGSpot.UsageData;

            EditorGUILayout.PropertyField(vector2Capacity);
            DisplayUsageData(
                nameof(Vector2),
                Vector2UsageData = ArrayPools.Vector2.UsageData
            );
            EditorGUILayout.Space();

            EditorGUILayout.PropertyField(vector3Capacity);
            DisplayUsageData(
                nameof(Vector3),
                Vector3UsageData = ArrayPools.Vector3.UsageData
            );
            EditorGUILayout.Space();

            EditorGUILayout.PropertyField(vector4Capacity);
            DisplayUsageData(
                nameof(Vector4),
                Vector4UsageData = ArrayPools.Vector4.UsageData
            );
            EditorGUILayout.Space();

            EditorGUILayout.PropertyField(intCapacity);
            DisplayUsageData(
                "int",
                Int32UsageData = ArrayPools.Int32.UsageData
            );
            EditorGUILayout.Space();

            EditorGUILayout.PropertyField(floatCapacity);
            DisplayUsageData(
                "float",
                SingleUsageData = ArrayPools.Single.UsageData
            );
            EditorGUILayout.Space();

            EditorGUILayout.PropertyField(cgSpotCapacity);
            DisplayUsageData(
                nameof(CGSpot),
                CGSpotUsageData = ArrayPools.CGSpot.UsageData
            );
            EditorGUILayout.Space();

            EditorGUILayout.PropertyField(logAllocations);

            serializedObject.ApplyModifiedProperties();

            //This was done to avoid repainting when nothing changed, but it doesn't seem to work when pools' usage changes in edit mode
            //if (needRepaint)
            Repaint();
        }

        private static void DisplayUsageData(string name, ArrayPoolUsageData usageData)
        {
            EditorGUILayout.LabelField("Available data:");

            EditorGUI.ProgressBar(
                EditorGUILayout.GetControlRect(
                    false,
                    GUILayout.Height(20)
                ),
                usageData.ElementsCount / (float)usageData.ElementsCapacity,
                $"Elements: {usageData.ElementsCount:0,0} / {usageData.ElementsCapacity:0,0}\tArrays: {usageData.ArraysCount}"
            );
        }
    }
}