// =====================================================================
// Copyright © 2013 ToolBuddy
// All rights reserved
// 
// http://www.toolbuddy.net
// =====================================================================

using System.IO;
using FluffyUnderware.DevTools;
using JetBrains.Annotations;
using UnityEditor;
using UnityEngine;

namespace FluffyUnderware.CurvyEditor.Generator
{
    public class ModuleWizard : EditorWindow
    {
        private string mModuleClassName = string.Empty;
        private string mModuleMenuName = string.Empty;
        private string mModuleName = string.Empty;
        private string mModuleDescription = string.Empty;

        private readonly string mModuleScriptPath =
            CurvyProject.Instance.CustomizationRootPath + CurvyProject.RELPATH_CGMODULEWIZARDSCRIPTS;

        private readonly string mModuleEditorScriptPath =
            CurvyProject.Instance.CustomizationRootPath + CurvyProject.RELPATH_CGMODULEWIZARDEDITORSCRIPTS;

        private bool mNeedFocus = true;

        private string ScriptTemplate => CurvyEditorUtility.GetPackagePathAbsolute("ClassTemplates/CGModuleTemplate.txt");

        private string EditorScriptTemplate =>
            CurvyEditorUtility.GetPackagePathAbsolute("ClassTemplates/CGModuleEditorTemplate.txt");

        private string ModuleFileName => Application.dataPath
                                         + "/"
                                         + mModuleScriptPath.TrimEnd(
                                             '/',
                                             '\\'
                                         )
                                         + "/"
                                         + mModuleClassName
                                         + ".cs";

        private string ModuleEditorFileName => Application.dataPath
                                               + "/"
                                               + mModuleEditorScriptPath.TrimEnd(
                                                   '/',
                                                   '\\'
                                               )
                                               + "/"
                                               + mModuleClassName
                                               + "Editor.cs";

        public static void Open()
        {
            ModuleWizard win = GetWindow<ModuleWizard>(
                true,
                "Create CG Module"
            );
            win.minSize = new Vector2(
                500,
                120
            );
        }

        [UsedImplicitly]
        private void OnGUI()
        {
            EditorGUI.BeginChangeCheck();
            GUI.SetNextControlName("ClassName");
            mModuleClassName = EditorGUILayout.TextField(
                new GUIContent(
                    "Class Name",
                    "C# class name"
                ),
                mModuleClassName
            );


            if (EditorGUI.EndChangeCheck())
            {
                mModuleName = ObjectNames.NicifyVariableName(mModuleClassName);
                mModuleMenuName = "Custom/" + mModuleName;
            }

            mModuleName = EditorGUILayout.TextField(
                new GUIContent(
                    "Module Name",
                    "The default module instance name"
                ),
                mModuleName
            );
            mModuleMenuName = EditorGUILayout.TextField(
                new GUIContent(
                    "Menu Name",
                    "Name to show in the CG menu"
                ),
                mModuleMenuName
            );

            EditorGUILayout.PrefixLabel("Description");
            mModuleDescription = EditorGUILayout.TextArea(mModuleDescription);

            GUI.enabled = !string.IsNullOrEmpty(mModuleScriptPath)
                          && !string.IsNullOrEmpty(mModuleEditorScriptPath)
                          && !string.IsNullOrEmpty(mModuleClassName)
                          && !string.IsNullOrEmpty(mModuleMenuName)
                          && !string.IsNullOrEmpty(mModuleName);
            if (GUILayout.Button("Create"))
                CreateModule();

            GUI.enabled = true;

            if (mNeedFocus)
            {
                EditorGUI.FocusTextInControl("ClassName");
                mNeedFocus = false;
            }
        }

        private void CreateModule()
        {
            if (!File.Exists(ScriptTemplate))
            {
                DTLog.LogError("[Curvy] Missing Module Template file '" + ScriptTemplate + "'!");
                return;
            }

            if (!File.Exists(EditorScriptTemplate))
            {
                DTLog.LogError("[Curvy] Missing Module Template file '" + EditorScriptTemplate + "'!");
                return;
            }

            // Script
            string template = File.ReadAllText(ScriptTemplate);
            if (!string.IsNullOrEmpty(template))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(ModuleFileName));
                StreamWriter stream = File.CreateText(ModuleFileName);
                stream.Write(replaceVars(template));
                stream.Close();
            }
            else
            {
                DTLog.LogError("[Curvy] Unable to load template file");
                return;
            }

            // Editor Script
            template = File.ReadAllText(EditorScriptTemplate);
            if (!string.IsNullOrEmpty(template))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(ModuleEditorFileName));
                StreamWriter stream = File.CreateText(ModuleEditorFileName);
                stream.Write(replaceVars(template));
                stream.Close();
            }
            else
            {
                DTLog.LogError("[Curvy] Unable to load editor template file");
                return;
            }

            AssetDatabase.Refresh();
            Close();
            EditorUtility.DisplayDialog(
                "CG Module Wizard",
                "Scripts successfully created!",
                "OK"
            );

            Selection.objects = new Object[2]
            {
                AssetDatabase.LoadMainAssetAtPath(
                    ModuleFileName.Replace(
                        Application.dataPath,
                        "Assets"
                    )
                ),
                AssetDatabase.LoadMainAssetAtPath(
                    ModuleEditorFileName.Replace(
                        Application.dataPath,
                        "Assets"
                    )
                )
            };
        }

        private string replaceVars(string template)
            => template.Replace(
                    "%MENUNAME%",
                    mModuleMenuName
                )
                .Replace(
                    "%MODULENAME%",
                    mModuleName
                )
                .Replace(
                    "%DESCRIPTION%",
                    mModuleDescription
                )
                .Replace(
                    "%CLASSNAME%",
                    mModuleClassName
                );
    }
}