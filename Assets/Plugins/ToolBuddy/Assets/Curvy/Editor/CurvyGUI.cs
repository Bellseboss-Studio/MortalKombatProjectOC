// =====================================================================
// Copyright © 2013 ToolBuddy
// All rights reserved
// 
// http://www.toolbuddy.net
// =====================================================================

using FluffyUnderware.DevToolsEditor;
using UnityEditor;
using UnityEngine;

namespace FluffyUnderware.CurvyEditor
{
    public static class CurvyGUI
    {
        #region ### GUI Controls ###

        public static bool Foldout(ref bool state, string text)
            => Foldout(
                ref state,
                new GUIContent(text),
                null
            );

        public static bool Foldout(ref bool state, string text, string helpURL)
            => Foldout(
                ref state,
                new GUIContent(text),
                helpURL
            );

        public static bool Foldout(ref bool state, GUIContent content, string helpURL, bool hierarchyMode = true)
        {
            Rect controlRect = GUILayoutUtility.GetRect(
                content,
                CurvyStyles.Foldout
            );
            bool isInsideInspector = DTInspectorNode.IsInsideInspector;
            int xOffset = isInsideInspector
                ? 12
                : -2;
            controlRect.x -= xOffset;
            controlRect.width += isInsideInspector
                ? 0
                : 1;

            int indentLevel = DTInspectorNodeDefaultRenderer.RenderHeader(
                controlRect,
                xOffset,
                helpURL,
                content,
                ref state
            );

            EditorGUI.indentLevel = indentLevel;

            return state;
        }

        #endregion
    }
}