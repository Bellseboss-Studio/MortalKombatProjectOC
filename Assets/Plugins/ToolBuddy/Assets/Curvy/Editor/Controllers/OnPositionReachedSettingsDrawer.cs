// =====================================================================
// Copyright © 2013 ToolBuddy
// All rights reserved
// 
// http://www.toolbuddy.net
// =====================================================================

#region

using System;
using System.Collections.Generic;
using FluffyUnderware.Curvy;
using FluffyUnderware.Curvy.Controllers;
using FluffyUnderware.DevTools.Extensions;
using UnityEditor;
using UnityEngine;

#endregion

namespace FluffyUnderware.CurvyEditor.Controllers
{
    [CustomPropertyDrawer(typeof(OnPositionReachedSettings))]
    public class OnPositionReachedSettingsDrawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            OnPositionReachedSettings onPositionReachedSettings;
            {
                object value = fieldInfo.GetValue(property.serializedObject.targetObject);
                if (value.GetType().IsArrayOrList())
                {
                    int startIndex = property.propertyPath.LastIndexOf('[');
                    int endIndex = property.propertyPath.LastIndexOf(']');
                    int index = Convert.ToInt32(
                        property.propertyPath.Substring(
                            startIndex + 1,
                            endIndex - startIndex - 1
                        )
                    );
                    onPositionReachedSettings = ((List<OnPositionReachedSettings>)value)[index];
                }
                else
                    onPositionReachedSettings = (OnPositionReachedSettings)value;
            }

            return GetPropertyHeight(
                property,
                onPositionReachedSettings
            );
        }

        public static float GetPropertyHeight(SerializedProperty property, OnPositionReachedSettings onPositionReachedSettings)
        {
            if (property.isExpanded == false)
                return EditorGUIUtility.singleLineHeight;

            return 190
            + (Math.Max(
                   0,
                   onPositionReachedSettings.Event.GetPersistentEventCount() - 1
               )
               * 47);
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            SerializedProperty nameProperty = property.FindPropertyRelative(nameof(OnPositionReachedSettings.Name));
            SerializedProperty colorProperty = property.FindPropertyRelative(nameof(OnPositionReachedSettings.GizmoColor));
            SerializedProperty eventProperty = property.FindPropertyRelative(nameof(OnPositionReachedSettings.Event));
            SerializedProperty positionProperty = property.FindPropertyRelative(nameof(OnPositionReachedSettings.Position));
            SerializedProperty positionModeProperty =
                property.FindPropertyRelative(nameof(OnPositionReachedSettings.PositionMode));
            SerializedProperty triggeringDirectionsProperty =
                property.FindPropertyRelative(nameof(OnPositionReachedSettings.TriggeringDirections));

            float fieldHeight = EditorGUIUtility.singleLineHeight;
            float fieldMarginHeight = EditorGUIUtility.standardVerticalSpacing;

            // Using BeginProperty / EndProperty on the parent property means that
            // prefab override logic works on the entire property.
            EditorGUI.BeginProperty(
                position,
                label,
                property
            );

            string labelString;
            {
                if (property.isExpanded)
                    labelString = $"{nameProperty.stringValue}";
                else
                {
                    string positionModeEnumName;
                    {
                        bool positionModeParseSucceeded = Enum.TryParse(
                            positionModeProperty.enumNames[positionModeProperty.enumValueIndex],
                            out CurvyPositionMode positionMode
                        );
                        positionModeEnumName = positionModeParseSucceeded
                            ? CurvyPositionModeDrawer.GetDisplayName(positionMode)
                            : positionModeProperty.enumDisplayNames[positionModeProperty.enumValueIndex];
                    }

                    labelString =
                        $"{nameProperty.stringValue} (Position: {positionModeEnumName} {positionProperty.floatValue} - Direction: {triggeringDirectionsProperty.enumDisplayNames[triggeringDirectionsProperty.enumValueIndex]})";
                }
            }
            int indent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = indent + 1;

            property.isExpanded = EditorGUI.Foldout(
                new Rect(
                    position.x,
                    position.y,
                    position.width,
                    EditorGUIUtility.singleLineHeight
                ),
                property.isExpanded,
                labelString
            );

            if (property.isExpanded)
            {
                float yCoordinate = position.y;

                EditorGUI.indentLevel = indent + 1;

                yCoordinate += fieldHeight + fieldMarginHeight;

                Rect nameRect = new Rect(
                    position.x,
                    yCoordinate,
                    position.width - 75,
                    fieldHeight
                );
                Rect colorRect = new Rect(
                    (position.x + position.width) - 70,
                    yCoordinate,
                    70,
                    fieldHeight
                );

                yCoordinate += fieldHeight + fieldMarginHeight;
                Rect positionRect = new Rect(
                    position.x,
                    yCoordinate,
                    position.width - 115,
                    fieldHeight
                );
                Rect positionModeRect = new Rect(
                    (position.x + position.width) - 110,
                    yCoordinate,
                    110,
                    fieldHeight
                );

                yCoordinate += fieldHeight + fieldMarginHeight;
                Rect directionRect = new Rect(
                    position.x,
                    yCoordinate,
                    position.width,
                    fieldHeight
                );

                yCoordinate += fieldHeight + fieldMarginHeight;
                Rect eventRect = new Rect(
                    position.x,
                    yCoordinate,
                    position.width,
                    position.height - 70
                );
                eventRect = EditorGUI.IndentedRect(eventRect);

                EditorGUI.PropertyField(
                    nameRect,
                    nameProperty
                );
                EditorGUI.PropertyField(
                    colorRect,
                    colorProperty,
                    GUIContent.none
                );
                EditorGUI.PropertyField(
                    eventRect,
                    eventProperty,
                    GUIContent.none
                );
                EditorGUI.PropertyField(
                    positionRect,
                    positionProperty
                );
                EditorGUI.PropertyField(
                    positionModeRect,
                    positionModeProperty,
                    GUIContent.none
                );
                EditorGUI.PropertyField(
                    directionRect,
                    triggeringDirectionsProperty
                );
            }

            EditorGUI.indentLevel = indent;


            EditorGUI.EndProperty();
        }
    }
}