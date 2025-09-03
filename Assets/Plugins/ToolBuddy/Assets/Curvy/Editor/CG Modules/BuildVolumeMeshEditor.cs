// =====================================================================
// Copyright © 2013 ToolBuddy
// All rights reserved
// 
// http://www.toolbuddy.net
// =====================================================================

using FluffyUnderware.Curvy.Generator;
using FluffyUnderware.Curvy.Generator.Modules;
using FluffyUnderware.DevToolsEditor;
using JetBrains.Annotations;
using UnityEditor;
using UnityEngine;

namespace FluffyUnderware.CurvyEditor.Generator.Modules
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(BuildVolumeMesh))]
    public class BuildVolumeMeshEditor : CGModuleEditor<BuildVolumeMesh>
    {
        private bool showAddButton;
        private int matcount;

        public override void OnModuleDebugGUI()
        {
            if (Target.OutVMesh.Data.Length == 0)
                return;

            CGVMesh vmesh = (CGVMesh)Target.OutVMesh.Data[0];
            EditorGUILayout.LabelField("Vertices: " + vmesh.Count);
            EditorGUILayout.LabelField("Triangles: " + vmesh.TriangleCount);
            EditorGUILayout.LabelField("SubMeshes: " + vmesh.SubMeshes.Length);
        }

        protected override void OnReadNodes() =>
            ensureMaterialTabs();

        private void ensureMaterialTabs()
        {
            DTGroupNode tabbar = Node.FindTabBarAt("Default");

            if (tabbar == null)
                return;

            tabbar.MaxItemsPerRow = 4;
            for (int i = 0; i < Target.MaterialCount; i++)
            {
                string tabName = string.Format(
                    "Mat {0}",
                    i
                );
                if (tabbar.Count <= i + 1)
                    tabbar.AddTab(
                        tabName,
                        OnRenderTab
                    );
                else
                {
                    tabbar[i + 1].Name = tabName;
                    tabbar[i + 1].GUIContent.text = tabName;
                }
            }

            while (tabbar.Count > Target.MaterialCount + 1)
                tabbar[tabbar.Count - 1].Delete();
            matcount = Target.MaterialCount;
        }

        private void OnRenderTab(DTInspectorNode node)
        {
            int idx = node.Index - 1;

            if (idx >= 0 && idx < Target.MaterialCount)
            {
                CGMaterialSettingsEx mat = Target.MaterialSettings[idx];
                EditorGUI.BeginChangeCheck();

                bool matSwapUv = EditorGUILayout.Toggle(
                    "Swap UV",
                    mat.SwapUV
                );
                if (matSwapUv != mat.SwapUV)
                {
                    Undo.RecordObject(
                        Target,
                        "Modify Swap UV"
                    );
                    mat.SwapUV = matSwapUv;
                }

                CGKeepAspectMode cgKeepAspectMode = (CGKeepAspectMode)EditorGUILayout.EnumPopup(
                    "Keep Aspect",
                    mat.KeepAspect
                );
                if (cgKeepAspectMode != mat.KeepAspect)
                {
                    Undo.RecordObject(
                        Target,
                        "Modify Keep Aspect"
                    );
                    mat.KeepAspect = cgKeepAspectMode;
                }

                Vector2 matUvOffset = EditorGUILayout.Vector2Field(
                    "UV Offset",
                    mat.UVOffset
                );
                if (matUvOffset != mat.UVOffset)
                {
                    Undo.RecordObject(
                        Target,
                        "Modify UV Offset"
                    );
                    mat.UVOffset = matUvOffset;
                }

                Vector2 matUvScale = EditorGUILayout.Vector2Field(
                    "UV Scale",
                    mat.UVScale
                );
                if (matUvScale != mat.UVScale)
                {
                    Undo.RecordObject(
                        Target,
                        "Modify UV Scale"
                    );
                    mat.UVScale = matUvScale;
                }

                Target.SetMaterial(
                    idx,
                    EditorGUILayout.ObjectField(
                        "Material",
                        Target.GetMaterial(idx),
                        typeof(Material),
                        true
                    ) as Material
                );

                if (Target.MaterialCount > 1 && GUILayout.Button("Remove"))
                {
                    Target.RemoveMaterial(idx);
                    node.Delete();
                    ensureMaterialTabs();
                    GUIUtility.ExitGUI();
                }

                if (EditorGUI.EndChangeCheck())
                {
                    Target.Dirty = true;
                    EditorUtility.SetDirty(Target);
                }
            }
        }

        [UsedImplicitly]
        private void CBAddMaterial()
        {
            if (DTGUI.IsLayout)
                showAddButton = Node.FindTabBarAt("Default").SelectedIndex == 0;
            if (showAddButton)
                if (GUILayout.Button("Add Material Group"))
                {
                    Target.AddMaterial();
                    ensureMaterialTabs();
                    GUIUtility.ExitGUI();
                }
        }

        protected override void OnCustomInspectorGUI()
        {
            base.OnCustomInspectorGUI();
            if (matcount != Target.MaterialCount)
                ensureMaterialTabs();
        }
    }
}