// =====================================================================
// Copyright © 2013 ToolBuddy
// All rights reserved
// 
// http://www.toolbuddy.net
// =====================================================================

using System.Collections.Generic;
using System.IO;
using FluffyUnderware.Curvy.Generator;
using FluffyUnderware.DevToolsEditor.Extensions;
using JetBrains.Annotations;
using UnityEditor;
using UnityEngine;

namespace FluffyUnderware.CurvyEditor.Generator
{
    public class TemplateWizard : EditorWindow
    {
        private string mName;
        private List<CGModule> mModules;
        private CanvasUI canvasUI;

        public static void Open(List<CGModule> modules, CanvasUI canvasUI)
        {
            if (modules == null || modules.Count == 0)
                return;
            TemplateWizard win = GetWindow<TemplateWizard>(
                true,
                "Save Template"
            );
            win.minSize = new Vector2(
                300,
                90
            );
            win.maxSize = win.minSize;
            win.mName = "";
            win.mModules = modules;
            win.canvasUI = canvasUI;
        }

        [UsedImplicitly]
        private void OnGUI()
        {
            EditorGUILayout.HelpBox(
                "Only Managed Resources will be saved!",
                MessageType.Warning
            );
            GUI.SetNextControlName("TPLWIZ_txtName");
            mName = EditorGUILayout.TextField(
                "Template Menu Name",
                mName
            ).TrimStart('/');


            EditorGUILayoutExtension.Horizontal(
                () =>
                {
                    GUI.enabled = !string.IsNullOrEmpty(mName);
                    if (GUILayout.Button("Save"))
                    {
                        Save();
                        Close();
                    }

                    GUI.enabled = true;
                    if (GUILayout.Button("Cancel"))
                        Close();
                }
            );

            if (GUI.GetNameOfFocusedControl() == "")
                EditorGUI.FocusTextInControl("TPLWIZ_txtName");
        }

        private void Save()
        {
            string absFolder = Application.dataPath
                               + "/"
                               + CurvyProject.Instance.CustomizationRootPath
                               + CurvyProject.RELPATH_CGTEMPLATES;
            string file = absFolder + "/" + mName + ".prefab";
            if (!File.Exists(file)
                || EditorUtility.DisplayDialog(
                    "Replace File?",
                    "The file already exists! Replace it?",
                    "Yes",
                    "No"
                ))
                if (CGEditorUtility.CreateTemplate(
                        mModules,
                        file
                    ))
                {
                    EditorUtility.DisplayDialog(
                        "Save Generator Template",
                        "Template successfully saved!",
                        "Ok"
                    );
                    if (canvasUI != null)
                        canvasUI.ReloadTemplates();
                }
        }
    }
}