// =====================================================================
// Copyright © 2013 ToolBuddy
// All rights reserved
// 
// http://www.toolbuddy.net
// =====================================================================

using System;
using UnityEngine;

namespace FluffyUnderware.DevTools.Extensions
{
    /// <summary>
    /// GUILayout extension methods that mirror some of its methods, but handles properly exceptions thrown in between the Begin and End calls
    /// </summary>
    public static class GUILayoutExtension
    {
        public static void Area(
            Action action,
            Rect screenRectangle,
            GUIStyle skinBox)
        {
            GUILayout.BeginArea(
                screenRectangle,
                skinBox
            );
            action();
            GUILayout.EndArea();
        }

        public static void Area(
            Action action,
            Rect screenRectangle)
        {
            GUILayout.BeginArea(screenRectangle);
            action();
            GUILayout.EndArea();
        }

        public static void Horizontal(
            Action action,
            GUIStyle style)
        {
            GUILayout.BeginHorizontal(style);
            action();
            GUILayout.EndHorizontal();
        }

        public static void Horizontal(
            Action action,
            params GUILayoutOption[] layoutOptions)
        {
            GUILayout.BeginHorizontal(layoutOptions);
            action();
            GUILayout.EndHorizontal();
        }

        public static Vector2 ScrollView(
            Action action,
            Vector2 scrollPosition,
            params GUILayoutOption[] layoutOptions)
        {
            scrollPosition = GUILayout.BeginScrollView(
                scrollPosition,
                layoutOptions
            );
            action();
            GUILayout.EndScrollView();

            return scrollPosition;
        }
    }
}