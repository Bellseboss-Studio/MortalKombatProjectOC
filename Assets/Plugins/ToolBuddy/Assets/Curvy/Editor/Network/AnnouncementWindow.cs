// =====================================================================
// Copyright © 2013 ToolBuddy
// All rights reserved
// 
// http://www.toolbuddy.net
// =====================================================================

using JetBrains.Annotations;
using UnityEditor;
using UnityEngine;

namespace FluffyUnderware.CurvyEditor.Network
{
    /// <summary>
    /// Window used to display announcements sent by Curvy's announcements' server
    /// </summary>
    public class AnnouncementWindow : EditorWindow
    {
        private string content;

        private Vector2 scrollViewPosition = new Vector2(
            0,
            0
        );

        /// <summary>
        /// Opens an announcement window
        /// </summary>
        public static AnnouncementWindow Open(string title, string content, Vector2 positionShift)
        {
            AnnouncementWindow window = CreateWindow<AnnouncementWindow>(title);
            window.content = content;

            Rect announcementWindowPosition;
            {
                announcementWindowPosition = window.position;
                announcementWindowPosition.x = 100;
                announcementWindowPosition.y = 50;
                announcementWindowPosition.width = 650;
                announcementWindowPosition.height = 280f;
                announcementWindowPosition.x += positionShift.x;
                announcementWindowPosition.y += positionShift.y;
            }
            window.position = announcementWindowPosition;

            window.minSize = new Vector2(
                announcementWindowPosition.width,
                announcementWindowPosition.height
            );

            return window;
        }

        [UsedImplicitly]
        private void OnGUI()
        {
            GUIStyle labelStyle = new GUIStyle(EditorStyles.label);
            labelStyle.alignment = TextAnchor.UpperLeft;
            labelStyle.fontSize = 22;
            labelStyle.richText = true;

            GUILayout.BeginVertical();

            GUILayout.Space(20);

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.Label(new GUIContent(CurvyStyles.TexLogoSmall));
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            GUILayout.Space(20);

            GUILayout.BeginHorizontal();
            GUILayout.Space(20);
            GUILayout.FlexibleSpace();
            GUILayout.Label(
                titleContent.text,
                labelStyle
            );
            GUILayout.FlexibleSpace();
            GUILayout.Space(20);
            GUILayout.EndHorizontal();

            GUILayout.Space(30);

            labelStyle.wordWrap = true;
            labelStyle.fontSize = 14;

            scrollViewPosition = GUILayout.BeginScrollView(scrollViewPosition);
            GUILayout.BeginHorizontal();
            GUILayout.Space(20);
            GUILayout.FlexibleSpace();
            GUILayout.Label(
                content,
                labelStyle
            );
            float mainTextHeight = GUILayoutUtility.GetLastRect().height;
            GUILayout.FlexibleSpace();
            GUILayout.Space(20);
            GUILayout.EndHorizontal();
            GUILayout.EndScrollView();

            GUILayout.EndVertical();

            float recommendedHeight = 230f + mainTextHeight;
            if (recommendedHeight > position.height && Event.current.type == EventType.Repaint)
            {
                float limitedRecommendedHeight = Mathf.Min(
                    recommendedHeight,
                    600f
                );
                position.Set(
                    position.x,
                    position.y,
                    position.width,
                    limitedRecommendedHeight
                );
                minSize = new Vector2(
                    position.width,
                    limitedRecommendedHeight
                );
            }
        }
    }
}