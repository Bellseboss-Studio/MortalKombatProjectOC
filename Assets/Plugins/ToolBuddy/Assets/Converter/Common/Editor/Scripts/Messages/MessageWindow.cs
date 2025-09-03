// =====================================================================
// Copyright © 2023 ToolBuddy
// All rights reserved
// 
// http://www.toolbuddy.net
// =====================================================================

using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace ToolBuddy.Converter.Editor.Common.Messages
{
    public class MessageWindow : EditorWindow
    {
        private Vector2 scrollPosition;

        private void OnGUI()
        {
            EditorGUILayout.BeginVertical();
            {
                EditorGUILayout.Space(10);
                scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
                {
                    DrawMessages();
                }
                EditorGUILayout.EndScrollView();
                GUILayout.FlexibleSpace();
                DrawBottomButtons();
                EditorGUILayout.Space(10);
            }
            EditorGUILayout.EndVertical();
        }

        private static void DrawMessages()
        {
            IEnumerable<(string, MessageType)> messages = MessageRepository.Instance.Messages;
            if (!messages.Any())
                EditorGUILayout.HelpBox(
                    "No messages to display.",
                    MessageType.Info
                );
            else
                foreach ((string message, MessageType messageType) in messages)
                    EditorGUILayout.HelpBox(
                        message,
                        messageType
                    );
        }

        private void DrawBottomButtons()
        {
#if CURVY_SPLINES == false
            if (GUILayout.Button(
                    "Get Curvy Splines"
                ))
                Application.OpenURL("https://assetstore.unity.com/packages/tools/utilities/curvy-splines-8-212532");
            EditorGUILayout.Space(3);
#endif
#if CURVY_SPLINES

            //Do not allow message clearing if Curvy Splines is not present,to make sure message about getting Curvy is always visible.
            if (GUILayout.Button(
                    $"Clear All Messages ({MessageRepository.Instance.Messages.Count()})"
                ))
                MessageRepository.Instance.Clear();
            EditorGUILayout.Space(3);
#endif
            if (GUILayout.Button(
                    "Close"
                ))
                Close();
        }
    }
}