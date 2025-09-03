// =====================================================================
// Copyright © 2013 ToolBuddy
// All rights reserved
// 
// http://www.toolbuddy.net
// =====================================================================

using FluffyUnderware.Curvy;
using FluffyUnderware.Curvy.Generator;
using FluffyUnderware.Curvy.Generator.Modules;
using FluffyUnderware.DevToolsEditor;
using UnityEditor;
using UnityEngine;

namespace FluffyUnderware.CurvyEditor.Generator.Modules
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(DebugRasterizedPath))]
    public class DebugRasterizedPathEditor : CGModuleEditor<DebugRasterizedPath>
    {
        protected override void OnEnable()
        {
            base.OnEnable();
            HasDebugVisuals = true;
            ShowDebugVisuals = true;
        }

        // Scene View GUI - Called only if the module is initialized and configured
        //public override void OnModuleSceneGUI() {}

        // Scene View Debug GUI - Called only when Show Debug Visuals is activated
        public override void OnModuleSceneDebugGUI()
        {
            if (Target.ShowNormals || Target.ShowOrientation)
            {
                CGPath path = Target.InPath.GetData<CGPath>(out bool isDisposable);
                if (path)
                {
                    Color gizmoOrientationColor = CurvyGlobalManager.GizmoOrientationColor;
                    Color gizmoTangentColor = CurvySplineSegment.GizmoTangentColor;

                    if (Target.ShowOrientation)
                    {
                        DTHandles.PushHandlesColor(gizmoOrientationColor);

                        for (int i = 0; i < path.Count; i++)
                            Handles.DrawLine(
                                path.Positions.Array[i],
                                path.Positions.Array[i] + (path.Directions.Array[i] * 2)
                            );

                        DTHandles.PopHandlesColor();
                    }

                    if (Target.ShowNormals)
                    {
                        DTHandles.PushHandlesColor(gizmoTangentColor);

                        for (int i = 0; i < path.Count; i++)
                            Handles.DrawLine(
                                path.Positions.Array[i],
                                path.Positions.Array[i] + (path.Normals.Array[i] * 2)
                            );

                        DTHandles.PopHandlesColor();
                    }
                }

                if (isDisposable)
                    path.Dispose();
            }
        }

        public override void OnModuleDebugGUI()
        {
            CGPath path = Target.InPath.GetData<CGPath>(out bool isDisposable);
            if (path)
                EditorGUILayout.LabelField("VertexCount: " + path.Count);
            if (isDisposable)
                path.Dispose();
        }

        // Inspector Debug GUI - Called only when Show Debug Values is activated
        //public override void OnModuleDebugGUI() {}

        protected override void OnCustomInspectorGUI()
        {
            DebugVMeshEditor.CheckGeneratorDebugMode(Target);
            base.OnCustomInspectorGUI();
        }
    }
}