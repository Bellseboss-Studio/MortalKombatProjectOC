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
    [CustomEditor(typeof(CreateGameObject))]
    public class CreateGameObjectEditor : CGModuleEditor<CreateGameObject>
    {
        protected override void OnReadNodes()
        {
            base.OnReadNodes();
            Node.FindTabBarAt("Default").AddTab(
                "Export",
                OnExportTab
            );
        }

        private void OnExportTab(DTInspectorNode node)
        {
            GUI.enabled = Target.GameObjects.Count > 0;
            if (GUILayout.Button("Save To Scene"))
                Target.SaveToScene();
            GUI.enabled = true;
        }
    }
}