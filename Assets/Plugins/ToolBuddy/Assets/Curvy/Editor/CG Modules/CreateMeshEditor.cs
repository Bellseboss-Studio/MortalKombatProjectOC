// =====================================================================
// Copyright © 2013 ToolBuddy
// All rights reserved
// 
// http://www.toolbuddy.net
// =====================================================================

using FluffyUnderware.Curvy.Generator.Modules;
using FluffyUnderware.DevToolsEditor;
using UnityEditor;
using UnityEngine;

namespace FluffyUnderware.CurvyEditor.Generator.Modules
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(CreateMesh))]
    public class CreateMeshEditor : CGModuleEditor<CreateMesh>
    {
        public override void OnModuleDebugGUI()
        {
            base.OnModuleDebugGUI();
            if (Target)
            {
                EditorGUILayout.LabelField("Meshes: " + Target.MeshCount);
                EditorGUILayout.LabelField("Vertices: " + Target.VertexCount);
            }
        }

        protected override void OnReadNodes()
        {
            base.OnReadNodes();
            Node.FindTabBarAt("Default").AddTab(
                "Export",
                onExportTab
            );
        }

        private void onExportTab(DTInspectorNode node)
        {
            GUI.enabled = Target.MeshCount > 0;

            if (GUILayout.Button(
                    new GUIContent(
                        "Save To Scene",
                        "Creates a GameObject, outside of the generator, containing a copy of the generated mesh(es)"
                    )
                ))
                Target.SaveToScene();
            if (GUILayout.Button(
                    new GUIContent(
                        "Save To Assets",
                        "Saves a copy of the generated mesh(es) as Asset(s)"
                    )
                ))
                Target.SaveToAsset();
            if (GUILayout.Button(
                    new GUIContent(
                        "Save To Both (Prefab Compatible)",
                        "Saves a copy of the generated mesh(es) as Asset(s), then creates a GameObject, outside of the generator, referencing those mesh assets. This way the created GameObject can be made part of a prefab without issues"
                    )
                ))
                Target.SaveToSceneAndAsset();

            GUI.enabled = true;
        }
    }
}