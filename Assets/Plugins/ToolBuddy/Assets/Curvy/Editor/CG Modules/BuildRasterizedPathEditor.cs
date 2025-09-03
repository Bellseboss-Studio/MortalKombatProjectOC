// =====================================================================
// Copyright © 2013 ToolBuddy
// All rights reserved
// 
// http://www.toolbuddy.net
// =====================================================================

using FluffyUnderware.Curvy.Generator;
using FluffyUnderware.Curvy.Generator.Modules;
using UnityEditor;
using UnityEngine;

namespace FluffyUnderware.CurvyEditor.Generator.Modules
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(BuildRasterizedPath))]
    public class BuildRasterizedPathEditor : CGModuleEditor<BuildRasterizedPath>
    {
        protected override void OnEnable()
        {
            base.OnEnable();
            HasDebugVisuals = true;
        }

        public override void OnModuleSceneDebugGUI()
        {
            base.OnModuleSceneDebugGUI();
            if (Target.OutPath.Data.Length == 0)
                return;

            CGPath data = (CGPath)Target.OutPath.Data[0];
            Handles.matrix = Target.Generator.transform.localToWorldMatrix;
            CGEditorUtility.SceneGUIPlot(
                data.Positions.Array,
                data.Positions.Count,
                0.1f,
                Color.white
            );
            Handles.matrix = Matrix4x4.identity;
        }

        public override void OnModuleDebugGUI()
        {
            if (Target.OutPath.Data.Length == 0)
                return;
            EditorGUILayout.LabelField($"Samples: {Target.OutPath.Data[0].Count}");
        }
    }
}