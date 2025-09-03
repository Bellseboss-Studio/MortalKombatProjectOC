// =====================================================================
// Copyright © 2013 ToolBuddy
// All rights reserved
// 
// http://www.toolbuddy.net
// =====================================================================

using FluffyUnderware.Curvy.Generator.Modules;
using FluffyUnderware.DevTools;
using FluffyUnderware.DevToolsEditor;
using FluffyUnderware.DevToolsEditor.Extensions;
using UnityEditor;
using UnityEngine;

namespace FluffyUnderware.CurvyEditor.Generator.Modules
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(InputTransformSpots))]
    public class InputTransformSpotsEditor : CGModuleEditor<InputTransformSpots>
    {
        protected override void SetupArrayEx(DTFieldNode node, ArrayExAttribute attribute)
        {
            base.SetupArrayEx(
                node,
                attribute
            );
            node.ArrayEx.elementHeight *= 2;
            node.ArrayEx.drawElementCallback = OnSpotGUI;
            node.ArrayEx.onAddCallback = l =>
            {
                int spotsCount = Target.TransformSpots.Count;

                //value of -1 means nothing selected previously
                int selectedIndex = l.index;
                //when deleting all entries, while having element 0 selected, l.index would be equal to 0, so to handle this:
                selectedIndex = selectedIndex < spotsCount
                    ? selectedIndex
                    : -1;

                InputTransformSpots.TransformSpot newSpot;
                int insertionIndex;
                if (selectedIndex < 0)
                {
                    newSpot = new InputTransformSpots.TransformSpot();
                    insertionIndex = Mathf.Max(
                        spotsCount - 1,
                        0
                    );
                }
                else
                {
                    newSpot = Target.TransformSpots[selectedIndex];
                    insertionIndex = selectedIndex;
                }

                Target.TransformSpots.Insert(
                    insertionIndex,
                    newSpot
                );
                EditorUtility.SetDirty(Target);
                Target.Dirty = true;
            };
        }


        private void OnSpotGUI(Rect rect, int index, bool isActive, bool isFocused)
        {
            SerializedProperty prop = serializedObject.FindProperty(
                string.Format(
                    "transformSpots.Array.data[{0}]",
                    index.ToString()
                )
            );
            rect.height = EditorGUIUtility.singleLineHeight;
            if (prop != null)
            {
                EditorGUIUtility.labelWidth = 40;
                Rect r = new Rect(rect);
                GUI.Label(
                    new Rect(
                        r.x,
                        r.y,
                        30,
                        r.height
                    ),
                    "#" + index
                );
                EditorGUI.PropertyField(
                    new Rect(
                        r.x + 30,
                        r.y,
                        115,
                        r.height
                    ),
                    prop.FindPropertyRelative("index")
                );

                EditorGUIUtility.labelWidth = 55;
                r.y += r.height + 1;
                EditorGUI.PropertyField(
                    r,
                    prop.FindPropertyRelative("transform")
                );
                if (serializedObject.ApplyModifiedProperties())
                    Target.Dirty = true;
            }
        }

        private Vector2 scroll;


        public override void OnInspectorGUI()
        {
            scroll = EditorGUILayoutExtension.ScrollView(
                () => base.OnInspectorGUI(),
                scroll,
                GUILayout.Height(210)
            );

            if (GUILayout.Button("Clear")
                && EditorUtility.DisplayDialog(
                    "Clear List",
                    "Are you sure?",
                    "Yes",
                    "No"
                ))
                Target.TransformSpots.Clear();
        }
    }
}