// =====================================================================
// Copyright 2013-2017 Fluffy Underware
// All rights reserved
// 
// http://www.fluffyunderware.com
// =====================================================================

using FluffyUnderware.DevTools.Extensions;
using JetBrains.Annotations;
using UnityEditor;
using UnityEngine;

namespace FluffyUnderware.DevToolsEditor
{
    public class DTDebugWindow : EditorWindow
    {
        private string Label;

        public string Text
        {
            get => string.Join(
                "",
                texts
            );
            set
            {
                int size = Mathf.FloorToInt(value.Length / 16382);
                texts = new string[size];
                for (int i = 0; i < size; i++)
                    texts[i] = value.Substring(
                        i * 16382,
                        16382
                    );
            }
        }

        private Vector2 scroll;

        private string[] texts;

        private GUIStyle stHtmlArea;

        public static void Open(string label, string text, string windowName = "Debug Dump")
        {
            DTDebugWindow win = GetWindow<DTDebugWindow>(
                true,
                windowName
            );
            win.Label = label;
            win.Text = text;
        }

        [UsedImplicitly]
        private void OnGUI()
        {
            if (stHtmlArea == null)
            {
                stHtmlArea = new GUIStyle(EditorStyles.textArea);
                stHtmlArea.richText = true;
            }

            GUILayout.Label(
                Label,
                EditorStyles.boldLabel
            );
            scroll = GUILayoutExtension.ScrollView(
                () =>
                {
                    for (int i = 0; i < texts.Length; i++)
                        if (i == texts.Length - 1)
                            GUILayout.TextArea(
                                texts[i],
                                stHtmlArea,
                                GUILayout.ExpandHeight(true)
                            );
                        else
                            GUILayout.TextArea(
                                texts[i],
                                stHtmlArea
                            );
                },
                scroll
            );
        }
    }
}