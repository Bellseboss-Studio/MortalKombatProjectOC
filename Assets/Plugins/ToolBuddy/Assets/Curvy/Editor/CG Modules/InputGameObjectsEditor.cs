// =====================================================================
// Copyright © 2013 ToolBuddy
// All rights reserved
// 
// http://www.toolbuddy.net
// =====================================================================

using FluffyUnderware.Curvy.Generator;
using FluffyUnderware.Curvy.Generator.Modules;
using FluffyUnderware.DevTools;
using FluffyUnderware.DevToolsEditor;
using UnityEditor;
using UnityEngine;

namespace FluffyUnderware.CurvyEditor.Generator.Modules
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(InputGameObject))]
    public class InputGameObjectsEditor : CGModuleEditor<InputGameObject>
    {
        private int selectedIndex;

        protected override void SetupArrayEx(DTFieldNode node, ArrayExAttribute attribute)
        {
            base.SetupArrayEx(
                node,
                attribute
            );
            node.ArrayEx.drawElementCallback = onGameObjectGUI;
            node.ArrayEx.onSelectCallback = l => { selectedIndex = l.index; };
            node.ArrayEx.onAddCallback = l =>
            {
                Target.GameObjects.Insert(
                    Mathf.Clamp(
                        l.index + 1,
                        0,
                        Target.GameObjects.Count
                    ),
                    new CGGameObjectProperties()
                );
                EditorUtility.SetDirty(Target);
                Target.Dirty = true;
            };
        }


        private void onGameObjectGUI(Rect rect, int index, bool isActive, bool isFocused)
        {
            SerializedProperty prop = serializedObject.FindProperty(
                string.Format(
                    "m_GameObjects.Array.data[{0}]",
                    index
                )
            );
            if (prop != null)
            {
                rect.height = EditorGUIUtility.singleLineHeight;
                rect.y += 1;

                SerializedProperty mshProp = prop.FindPropertyRelative("m_Object");
                mshProp.objectReferenceValue = EditorGUI.ObjectField(
                    rect,
                    mshProp.objectReferenceValue,
                    typeof(GameObject),
                    true
                );
            }
        }


        private void OnPropertiesGUI()
        {
            SerializedProperty prop = serializedObject.FindProperty(
                string.Format(
                    "m_GameObjects.Array.data[{0}]",
                    selectedIndex
                )
            );
            if (prop != null)
            {
                EditorGUILayout.PropertyField(prop.FindPropertyRelative("m_Translation"));
                EditorGUILayout.PropertyField(prop.FindPropertyRelative("m_Rotation"));
                EditorGUILayout.PropertyField(prop.FindPropertyRelative("m_Scale"));
            }
        }

        protected override void OnCustomInspectorGUI()
        {
            base.OnCustomInspectorGUI();
            if (Target && selectedIndex < Target.GameObjects.Count && Target.GameObjects[selectedIndex].Object)
            {
                GUILayout.Space(5);
                bool open = true;
                CurvyGUI.Foldout(
                    ref open,
                    "Properties"
                );
                OnPropertiesGUI();
            }
        }
    }
}