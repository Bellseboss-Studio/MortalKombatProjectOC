// =====================================================================
// Copyright © 2013 ToolBuddy
// All rights reserved
// 
// http://www.toolbuddy.net
// =====================================================================

using System;
using FluffyUnderware.Curvy;
using UnityEditor;
using UnityEngine;

namespace FluffyUnderware.CurvyEditor.Controllers
{
    [CustomPropertyDrawer(typeof(CurvyPositionMode))]
    public class CurvyPositionModeDrawer : PropertyDrawer
    {
        private const string Relative = "Relative";
        private const string Absolute = "Absolute";

        private readonly GUIContent[] options =
        {
            new GUIContent(
                Relative,
                "Position is expressed as a fraction of a spline: 0 meaning the spline start, 1 meaning the spline end."
            ),
            new GUIContent(
                Absolute,
                "Position is expressed as world units"
            )
        };

        private readonly GUIStyle guiStyle = EditorStyles.popup;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(
                position,
                label,
                property
            );
            property.intValue = EditorGUI.Popup(
                position,
                label,
                property.intValue,
                options,
                guiStyle
            );
            EditorGUI.EndProperty();
        }

        /// <summary>
        /// Gets the display name associated with a specific CurvyPositionMode value
        /// </summary>
        public static string GetDisplayName(CurvyPositionMode positionMode)
        {
            string displayName;

            switch (positionMode)
            {
                case CurvyPositionMode.Relative:
                    displayName = Relative;
                    break;
                case CurvyPositionMode.WorldUnits:
                    displayName = Absolute;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(positionMode),
                        positionMode,
                        null
                    );
            }

            return displayName;
        }
    }
}