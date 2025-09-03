// =====================================================================
// Copyright © 2013 ToolBuddy
// All rights reserved
// 
// http://www.toolbuddy.net
// =====================================================================

using System.IO;
using FluffyUnderware.DevTools;
using FluffyUnderware.DevTools.Extensions;
using JetBrains.Annotations;
using UnityEditor;
using UnityEngine;

namespace FluffyUnderware.CurvyEditor
{
    public class ShapeWizard : EditorWindow
    {
        private string mShapeClassName = "CS";
        private string mShapeMenuName = "Custom/";
        private bool mShapeIs2D = true;
        private bool mNeedFocus = true;

        private string mShapeParent = "CurvyShape";

        private readonly string mShapeScriptPath =
            CurvyProject.Instance.CustomizationRootPath + CurvyProject.RELPATH_SHAPEWIZARDSCRIPTS;

        private string ScriptTemplate => CurvyEditorUtility.GetPackagePathAbsolute("ClassTemplates/ShapeTemplate.txt");

        private string ShapeFileName => Application.dataPath
                                        + "/"
                                        + mShapeScriptPath.TrimEnd(
                                            '/',
                                            '\\'
                                        )
                                        + "/"
                                        + mShapeClassName
                                        + ".cs";


        public static void Open()
        {
            ShapeWizard win = GetWindow<ShapeWizard>(
                true,
                "Create Shape"
            );
            win.minSize = new Vector2(
                500,
                60
            );
        }

        [UsedImplicitly]
        private void OnGUI()
        {
            EditorGUI.BeginChangeCheck();
            GUI.SetNextControlName("ClassName");
            mShapeClassName = EditorGUILayout.TextField(
                "Class Name",
                mShapeClassName
            );

            if (EditorGUI.EndChangeCheck())
                mShapeMenuName = "Custom/" + ObjectNames.NicifyVariableName(mShapeClassName.TrimStart("CS"));

            mShapeMenuName = EditorGUILayout.TextField(
                "Menu Name",
                mShapeMenuName
            );
            mShapeIs2D = EditorGUILayout.Toggle(
                "Is 2D",
                mShapeIs2D
            );

            GUI.enabled = !string.IsNullOrEmpty(mShapeScriptPath)
                          && !string.IsNullOrEmpty(mShapeClassName)
                          && !string.IsNullOrEmpty(mShapeMenuName);
            if (GUILayout.Button("Create"))
                CreateShape();
            GUI.enabled = true;


            if (mNeedFocus)
            {
                EditorGUI.FocusTextInControl("ClassName");
                mNeedFocus = false;
            }
        }

        private void CreateShape()
        {
            if (!File.Exists(ScriptTemplate))
            {
                DTLog.LogError("[Curvy] Missing Shape Template file '" + ScriptTemplate + "'!");
                return;
            }

            mShapeParent = mShapeIs2D
                ? "CurvyShape2D"
                : "CurvyShape";

            // Script
            string template = File.ReadAllText(ScriptTemplate);
            if (!string.IsNullOrEmpty(template))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(ShapeFileName));
                StreamWriter stream = File.CreateText(ShapeFileName);
                stream.Write(replaceVars(template));
                stream.Close();
            }
            else
            {
                DTLog.LogError("[Curvy] Unable to load template file");
                return;
            }

            AssetDatabase.Refresh();
            Close();
            EditorUtility.DisplayDialog(
                "Shape Script Wizard",
                "Script successfully created!",
                "OK"
            );
            Selection.activeObject =
                AssetDatabase.LoadMainAssetAtPath("Assets/" + mShapeScriptPath + "/" + mShapeClassName + ".cs");
        }

        private string replaceVars(string template)
            => template.Replace(
                    "%MENUNAME%",
                    mShapeMenuName
                )
                .Replace(
                    "%CLASSNAME%",
                    mShapeClassName
                )
                .Replace(
                    "%PARENT%",
                    mShapeParent
                )
                .Replace(
                    "%IS2D%",
                    mShapeIs2D.ToString().ToLower()
                );
    }
}