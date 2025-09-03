// =====================================================================
// Copyright © 2013 ToolBuddy
// All rights reserved
// 
// http://www.toolbuddy.net
// =====================================================================

using FluffyUnderware.Curvy;
using FluffyUnderware.Curvy.Generator;
using FluffyUnderware.DevToolsEditor;
using UnityEditor;
using UnityEngine;

namespace FluffyUnderware.CurvyEditor
{
    [CustomPropertyDrawer(typeof(CGResourceCollectionManagerAttribute))]
    public class CGResourceCollectionManagerPropertyDrawer : DTPropertyDrawer<CGResourceCollectionManagerAttribute>
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            Rect mControlRect = position;
            CGResourceManagerAttribute A = (CGResourceManagerAttribute)attribute;
            ICGResourceCollection lst = GetPropertySourceField<ICGResourceCollection>(property);

            label = EditorGUI.BeginProperty(
                position,
                label,
                property
            );

            if (lst != null)
            {
                if (lst.Count > 0)
                    label.text += string.Format(
                        "[{0}]",
                        lst.Count
                    );
                EditorGUI.PrefixLabel(
                    mControlRect,
                    label
                );
                mControlRect.x = A.ReadOnly
                    ? mControlRect.xMax - 60
                    : mControlRect.xMax - 82;
                mControlRect.width = 60;

                if (GUI.Button(
                        mControlRect,
                        new GUIContent(
                            "Select",
                            CurvyStyles.SelectTexture,
                            "Select"
                        )
                    ))
                    DTSelection.SetGameObjects(lst.ItemsArray);
            }
        }
    }
}