// =====================================================================
// Copyright © 2013 ToolBuddy
// All rights reserved
// 
// http://www.toolbuddy.net
// =====================================================================

using FluffyUnderware.Curvy.Controllers;
using UnityEditor;
using UnityEngine;

namespace FluffyUnderware.CurvyEditor.Controllers
{
    [CustomPropertyDrawer(typeof(CurvyController.MoveModeEnum))]
    public class MoveModeDrawer : PropertyDrawer
    {
        private readonly GUIContent[] options =
        {
            new GUIContent(
                "Relative",
                "Speed is expressed as spline lengths per second"
            ),
            new GUIContent(
                "Absolute",
                "Speed is expressed as world units per second"
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
    }
}