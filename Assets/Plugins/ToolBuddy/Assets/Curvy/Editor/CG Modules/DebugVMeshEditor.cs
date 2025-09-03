// =====================================================================
// Copyright © 2013 ToolBuddy
// All rights reserved
// 
// http://www.toolbuddy.net
// =====================================================================

using System.Linq;
using FluffyUnderware.Curvy.Generator;
using FluffyUnderware.Curvy.Generator.Modules;
using UnityEditor;
using UnityEngine;

namespace FluffyUnderware.CurvyEditor.Generator.Modules
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(DebugVMesh))]
    public class DebugVMeshEditor : CGModuleEditor<DebugVMesh>
    {
        protected override void OnEnable()
        {
            base.OnEnable();
            HasDebugVisuals = true;
            ShowDebugVisuals = true;
        }

        public override void OnModuleSceneDebugGUI()
        {
            CGVMesh vmesh = Target.InData.GetData<CGVMesh>(out bool isDisposable);
            if (vmesh)
            {
                Handles.matrix = Target.Generator.transform.localToWorldMatrix;
                if (Target.ShowVertices)
                    CGEditorUtility.SceneGUIPlot(
                        vmesh.Vertices.Array,
                        vmesh.Vertices.Count,
                        0.1f,
                        Color.gray
                    );

                if (Target.ShowVertexID)
                {
                    string[] labels = Enumerable.Range(
                        0,
                        vmesh.Count
                    ).Select(i => i.ToString()).ToArray();
                    CGEditorUtility.SceneGUILabels(
                        vmesh.Vertices.Array,
                        vmesh.Vertices.Count,
                        labels,
                        Color.black,
                        Vector2.zero
                    );
                }

                if (Target.ShowUV && vmesh.HasUV)
                {
                    string[] labels = Enumerable.Range(
                        0,
                        vmesh.UVs.Count - 1
                    ).Select(
                        i => string.Format(
                            "({0:0.##},{1:0.##})",
                            vmesh.UVs.Array[i].x,
                            vmesh.UVs.Array[i].y
                        )
                    ).ToArray();
                    CGEditorUtility.SceneGUILabels(
                        vmesh.Vertices.Array,
                        vmesh.Vertices.Count,
                        labels,
                        Color.black,
                        Vector2.zero
                    );
                }

                Handles.matrix = Matrix4x4.identity;
            }

            if (isDisposable)
                vmesh.Dispose();
        }

        public override void OnModuleDebugGUI()
        {
            CGVMesh vmesh = Target.InData.GetData<CGVMesh>(out bool isDisposable);
            if (vmesh)
                EditorGUILayout.LabelField("VertexCount: " + vmesh.Count);
            if (isDisposable)
                vmesh.Dispose();
        }

        protected override void OnCustomInspectorGUI()
        {
            CheckGeneratorDebugMode(Target);
            base.OnCustomInspectorGUI();
        }

        /// <summary>
        /// Clears all existing UI messages and adds one if the generator debug mode is not active
        /// </summary>
        internal static void CheckGeneratorDebugMode(CGModule module)
        {
            module.UIMessages.Clear();
            if (module.Generator.ShowDebug == false)
                module.UIMessages.Add(
                    "To display the debug information, activate the Generator's Debug mode either via its toolbar, or its inspector"
                );
        }
    }
}