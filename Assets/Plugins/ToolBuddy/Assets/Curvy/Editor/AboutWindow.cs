// =====================================================================
// Copyright © 2013 ToolBuddy
// All rights reserved
// 
// http://www.toolbuddy.net
// =====================================================================

using System.Linq;
using FluffyUnderware.Curvy;
using FluffyUnderware.DevTools;
using FluffyUnderware.DevToolsEditor;
using JetBrains.Annotations;
using UnityEditor;
using UnityEngine;

namespace FluffyUnderware.CurvyEditor
{
    public class AboutWindow : EditorWindow
    {
        private bool heightHasBeenSet;

        public static void Open() =>
            GetWindow<AboutWindow>(
                true,
                "About Curvy"
            );

        [UsedImplicitly]
        private void OnEnable() =>
            CurvyProject.Instance.ShowAboutOnLoad = false;

        [UsedImplicitly]
        private void OnGUI()
        {
            GUILayout.BeginVertical();
            GUILayout.Label(new GUIContent(CurvyStyles.TexLogoBig));
            DTGUI.PushContentColor(Color.black);

            GUIStyle labelStyle = new GUIStyle(EditorStyles.label);
            labelStyle.alignment = TextAnchor.UpperLeft;
            labelStyle.fontSize = 18;
            labelStyle.richText = true;

            GUI.Label(
                new Rect(
                    300,
                    70,
                    215,
                    40
                ),
                "<b>v " + AssetInformation.Version + "</b>",
                labelStyle
            );
            labelStyle.fontSize = 14;
            GUI.Label(
                new Rect(
                    300,
                    95,
                    215,
                    40
                ),
                "© 2013 ToolBuddy",
                labelStyle
            );
            DTGUI.PopContentColor();


            //head("Links");
            if (buttonCol(
                    "Release notes",
                    "View release notes and upgrade instructions"
                ))
                OpenReleaseNotes();
            if (buttonCol(
                    "Leave a review",
                    "We've got to feed the Asset Store's algorithm"
                ))
                Application.OpenURL(AssetInformation.ReviewUrl);
            if (buttonCol(
                    "Custom development",
                    "We can provide custom modifications for Curvy"
                ))
                Application.OpenURL($"mailto:{AssetInformation.CustomDevRequestEmail}?subject=Curvy custom development request");
            if (buttonCol(
                    "Curvy Website",
                    "Visit Curvy Splines' website"
                ))
                OpenWeb();
            if (buttonCol(
                    "Our other assets",
                    "Find our other assets on the Asset Store"
                ))
                Application.OpenURL(AssetInformation.PublisherUrl);
            if (buttonCol(
                    "Submit a bug report",
                    "Found a bug? Please issue a bug report"
                ))
                CurvyEditorUtility.SendBugReport();
            foot();

            GUILayout.Space(10);

            head("Learning Resources");
            if (buttonCol(
                    "View Examples",
                    "Show examples folder in the Project window"
                ))
                ShowExamples();
            if (buttonCol(
                    "Tutorials",
                    "Watch some tutorials"
                ))
                OpenTutorials();
            if (buttonCol(
                    "Documentation",
                    "Manuals! That magic source of wisdom"
                ))
                OpenDocs();
            if (buttonCol(
                    "API Reference",
                    "Browse the API reference"
                ))
                OpenAPIDocs();
            if (buttonCol(
                    "Support Forum",
                    "Visit Support forum"
                ))
                OpenForum();
            foot();

            GUILayout.EndVertical();

            if (!heightHasBeenSet && Event.current.type == EventType.Repaint)
                setHeightToContent();
        }

        private void setHeightToContent()
        {
            int w = 500;
            float height = GUILayoutUtility.GetLastRect().height + 10f;
            position.Set(
                position.x,
                position.y,
                w,
                height
            );
            minSize = new Vector2(
                w,
                height
            );
            maxSize = new Vector2(
                w,
                height + 1
            );
            heightHasBeenSet = true;
        }

        private void head(string text)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Space(35);
            GUILayout.Label(
                text,
                EditorStyles.boldLabel
            );
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            GUILayout.Space(3);
        }

        private void foot() =>
            GUILayout.Space(5);

        private bool buttonCol(string btnText, string text)
            => buttonCol(
                new GUIContent(btnText),
                text
            );

        private bool buttonCol(GUIContent btn, string text)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Space(20);
            bool res = GUILayout.Button(
                btn,
                GUILayout.Width(150)
            );
            GUILayout.Space(20);
            EditorGUILayout.LabelField(
                "<i>" + text + "</i>",
                DTStyles.HtmlLabel
            );
            GUILayout.EndHorizontal();
            return res;
        }

        public static void ShowExamples()
        {
            string searchString;
            searchString = "t:Folder Curvy Examples";
            string[] assetsGuids = AssetDatabase.FindAssets(searchString);
            if (assetsGuids.Any())
                EditorGUIUtility.PingObject(
                    AssetDatabase.LoadMainAssetAtPath(AssetDatabase.GUIDToAssetPath(assetsGuids.First()))
                );
            else
                DTLog.LogError("[Curvy] Could not find the \"Curvy Examples\" folder");
        }

        public static void OpenTutorials() =>
            Application.OpenURL(AssetInformation.DocsRedirectionBaseUrl + "tutorials");

        public static void OpenReleaseNotes() =>
            Application.OpenURL(AssetInformation.DocsRedirectionBaseUrl + "releasenotes");

        public static void OpenDocs() =>
            Application.OpenURL(AssetInformation.Website + "documentation/");

        public static void OpenAPIDocs() =>
            Application.OpenURL("https://api.curvyeditor.com/" + AssetInformation.ApiVersion + "/");

        public static void OpenWeb() =>
            Application.OpenURL(AssetInformation.Website);

        public static void OpenForum() =>
            Application.OpenURL("https://forum.curvyeditor.com");
    }
}