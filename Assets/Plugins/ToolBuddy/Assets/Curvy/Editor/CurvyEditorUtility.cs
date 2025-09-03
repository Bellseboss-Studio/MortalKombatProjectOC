// =====================================================================
// Copyright © 2013 ToolBuddy
// All rights reserved
// 
// http://www.toolbuddy.net
// =====================================================================

using System;
using System.IO;
using FluffyUnderware.Curvy;
using FluffyUnderware.Curvy.Controllers;
using FluffyUnderware.Curvy.Generator;
using FluffyUnderware.DevTools;
using FluffyUnderware.DevTools.Extensions;
using FluffyUnderware.DevToolsEditor;
using UnityEditor;
using UnityEngine;

namespace FluffyUnderware.CurvyEditor
{
    public static class CurvyEditorUtility
    {
        public static void SendBugReport()
        {
            string par = string.Format(
                "@Operating System@={0}&@Unity Version@={1}&@Curvy Version@={2}",
                SystemInfo.operatingSystem,
                Application.unityVersion,
                AssetInformation.Version
            );
            Application.OpenURL(
                AssetInformation.Website
                + "bugreport?"
                + par.Replace(
                    " ",
                    "%20"
                )
            );
        }

        public static void GenerateAssemblyDefinitions()
        {
            string curvyRootPath = GetCurvyRootPath();
            if (String.IsNullOrEmpty(curvyRootPath))
                DTLog.LogError("[Curvy] Assembly Definitions generation aborted, couldn't locate the installation folder");
            else
            {
                string curvyRootPathAbsolute = Application.dataPath + "/" + curvyRootPath;
                DirectoryInfo parentInfo = Directory.GetParent(curvyRootPathAbsolute).Parent;
                string assetsParentDirectory = parentInfo.FullName;
                string toolbuddyDirectory = parentInfo.Parent.FullName;

                GenerateAssemblyDefinition(
                    $"{assetsParentDirectory}/Arrays Pooling/ToolBuddy.ArraysPooling.asmdef",
                    @"
{
	""name"":""ToolBuddy.ArraysPooling""
}"
                );

                GenerateAssemblyDefinition(
                    $"{toolbuddyDirectory}/Dependencies/Vector Graphics/ToolBuddy.Dependencies.VectorGraphics.asmdef",
                    @"
{
	""name"":""ToolBuddy.Dependencies.VectorGraphics""
}"
                );

                GenerateAssemblyDefinition(
                    $"{toolbuddyDirectory}/Dependencies/DevTools/FluffyUnderware.DevTools.asmdef",
                    @"
{
	""name"":""FluffyUnderware.DevTools""
}"
                );

                GenerateAssemblyDefinition(
                    $"{toolbuddyDirectory}/Dependencies/LibTessDotNet/LibTessDotNet.asmdef",
                    @"
{
	""name"":""LibTessDotNet"",
    ""references"":[
        ""ToolBuddy.ArraysPooling""
    ],
    ""includePlatforms"":[],
    ""excludePlatforms"":[]
}"
                );

                GenerateAssemblyDefinition(
                    $"{toolbuddyDirectory}/Dependencies/DevTools/Editor/FlufyUnderware.DevTools.Editor.asmdef",
                    @"
{
    ""name"":""FluffyUnderware.DevTools.Editor"",
    ""references"":[
        ""ToolBuddy.ArraysPooling"",
        ""FluffyUnderware.DevTools""
    ],
    ""includePlatforms"":[
        ""Editor""
    ],
    ""excludePlatforms"":[]
}"
                );

                GenerateAssemblyDefinition(
                    $"{assetsParentDirectory}/Curvy/ToolBuddy.Curvy.asmdef",
                    @"
{
    ""name"":""ToolBuddy.Curvy"",
    ""references"":[
        ""ToolBuddy.ArraysPooling"",
        ""ToolBuddy.Dependencies.VectorGraphics"",
        ""FluffyUnderware.DevTools"",
        ""LibTessDotNet""
    ],
    ""includePlatforms"":[],
    ""excludePlatforms"":[]
}"
                );

                GenerateAssemblyDefinition(
                    $"{assetsParentDirectory}/Curvy/Editor/ToolBuddy.Curvy.Editor.asmdef",
                    @"
{
    ""name"":""ToolBuddy.Curvy.Editor"",
    ""references"":[
        ""ToolBuddy.ArraysPooling"",
        ""ToolBuddy.Curvy"",
        ""FluffyUnderware.DevTools"",
        ""FluffyUnderware.DevTools.Editor"",
        ""LibTessDotNet""
    ],
    ""includePlatforms"":[
        ""Editor""
    ],
    ""excludePlatforms"":[]
}"
                );

                GenerateAssemblyDefinition(
                    $"{assetsParentDirectory}/Curvy Examples/ToolBuddy.Curvy.Examples.asmdef",
                    @"
{
    ""name"":""ToolBuddy.Curvy.Examples"",
    ""references"":[
        ""ToolBuddy.ArraysPooling"",
        ""FluffyUnderware.DevTools"",
        ""ToolBuddy.Curvy""
    ],
    ""includePlatforms"":[],
    ""excludePlatforms"":[]
}"
                );

                GenerateAssemblyDefinition(
                    $"{assetsParentDirectory}/Curvy Examples/Editor/ToolBuddy.Curvy.Examples.Editor.asmdef",
                    @"
{
    ""name"":""ToolBuddy.Curvy.Examples.Editor"",
    ""references"":[
        ""ToolBuddy.ArraysPooling"",
        ""FluffyUnderware.DevTools"",
        ""FluffyUnderware.DevTools.Editor"",
        ""ToolBuddy.Curvy"",
        ""ToolBuddy.Curvy.Editor"",
        ""ToolBuddy.Curvy.Examples""
    ],
    ""includePlatforms"":[
        ""Editor""
    ],
    ""excludePlatforms"":[]
}"
                );

                AssetDatabase.Refresh();
            }
        }

        private static void GenerateAssemblyDefinition(string filePath, string fileContent)
        {
            DirectoryInfo directory = Directory.GetParent(filePath);
            if (Directory.Exists(directory.FullName) == false)
                EditorUtility.DisplayDialog(
                    "Missing directory",
                    String.Format(
                        "Could not find the directory '{0}', file generation will be skipped",
                        directory.FullName
                    ),
                    "Continue"
                );
            else if (!File.Exists(filePath)
                     || EditorUtility.DisplayDialog(
                         "Replace File?",
                         String.Format(
                             "The file '{0}' already exists! Replace it?",
                             filePath
                         ),
                         "Yes",
                         "No"
                     ))
                using (StreamWriter streamWriter = File.CreateText(filePath))
                {
                    streamWriter.WriteLine(fileContent);
                }
        }


        /// <summary>
        /// Converts a path/file relative to Curvy's root path to the real path, e.g. "ReadMe.txt" gives "Curvy/ReadMe.txt"
        /// </summary>
        /// <param name="relativePath">a path/file inside the Curvy package, WITHOUT the leading Curvy</param>
        /// <returns>the real path, relative to Assets</returns>
        public static string GetPackagePath(string relativePath)
            => GetCurvyRootPath()
            + relativePath.TrimStart(
                '/',
                '\\'
            );

        /// <summary>
        /// Converts a path/file relative to Curvy's root path to the real absolute path
        /// </summary>
        /// <param name="relativePath">a path/file inside the Curvy package, WITHOUT the leading Curvy</param>
        /// <returns>the absolute system path</returns>
        public static string GetPackagePathAbsolute(string relativePath)
            => Application.dataPath + "/" + GetPackagePath(relativePath);

        /// <summary>
        /// Gets the Curvy folder relative path, e.g. "Plugins/Curvy/" by default
        /// </summary>
        /// <returns></returns>
        public static string GetCurvyRootPath()
        {
            // Quick check for the regular path
            if (File.Exists(Application.dataPath + "/Plugins/ToolBuddy/Assets/Curvy/Scripts/Splines/CurvySpline.cs"))
                return "Plugins/ToolBuddy/Assets/Curvy/";


            // Still no luck? Do a project search
            string[]
                guid = AssetDatabase.FindAssets(
                    "curvyspline_private"
                ); //FindAssets("curvyspline") returns also files other than CurvySpline.cs
            if (guid.Length == 0)
            {
                DTLog.LogError(
                    "[Curvy] Unable to locate CurvySpline_private.cs in the project! Is the Curvy package fully imported?"
                );
                return null;
            }

            return AssetDatabase.GUIDToAssetPath(guid[0]).TrimStart("Assets/").TrimEnd("Scripts/Splines/CurvySpline_private.cs");
        }

        /// <summary>
        /// Gets the Curvy folder absolute path, i.e. Application.dataPath+"/"+CurvyEditorUtility.GetCurvyRootPath()
        /// </summary>
        /// <returns></returns>
        public static string GetCurvyRootPathAbsolute()
            => Application.dataPath + "/" + GetCurvyRootPath();

        /// <summary>
        /// Returns true if the selected make the contextual toolbar visible.
        /// </summary>
        /// <returns></returns>
        public static bool IsToolbarActivatingObjectSelected()
            => DTSelection.HasComponent<CurvySpline, CurvySplineSegment, CurvyController, CurvyGenerator>(true);
    }
}