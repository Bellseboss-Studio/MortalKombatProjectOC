// =====================================================================
// Copyright © 2013 ToolBuddy
// All rights reserved
// 
// http://www.toolbuddy.net
// =====================================================================

using FluffyUnderware.Curvy.Generator;
using FluffyUnderware.Curvy.Generator.Modules;
using UnityEditor;

namespace FluffyUnderware.CurvyEditor.Generator.Modules
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(BuildVolumeCaps))]
    public class BuildVolumeCapsEditor : CGModuleEditor<BuildVolumeCaps>
    {
        public override void OnModuleDebugGUI()
        {
            if (Target.OutVMesh.Data.Length == 0)
                return;
            CGVMesh vmesh = (CGVMesh)Target.OutVMesh.Data[0];
            EditorGUILayout.LabelField("Vertices: " + vmesh.Count);
            EditorGUILayout.LabelField("Triangles: " + vmesh.TriangleCount);
            EditorGUILayout.LabelField("SubMeshes: " + vmesh.SubMeshes.Length);
        }
    }
}