// =====================================================================
// Copyright © 2013 ToolBuddy
// All rights reserved
// 
// http://www.toolbuddy.net
// =====================================================================

using FluffyUnderware.Curvy;
using FluffyUnderware.Curvy.Generator;
using FluffyUnderware.CurvyEditor.Generator;
using FluffyUnderware.DevToolsEditor;
using UnityEditor;
using UnityEngine;

namespace FluffyUnderware.CurvyEditor
{
    [CustomPropertyDrawer(typeof(CGDataReferenceSelectorAttribute))]
    public class CGDataReferenceSelectorPropertyDrawer : DTPropertyDrawer<CGDataReferenceSelectorAttribute>
    {
        private SerializedProperty CurrentProp;

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
            => GetPropertySourceField<CGDataReference>(property).HasValue
                ? base.GetPropertyHeight(
                    property,
                    label
                )
                : base.GetPropertyHeight(
                      property,
                      label
                  )
                  * 2;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            CGDataReferenceSelectorAttribute attrib = (CGDataReferenceSelectorAttribute)attribute;
            CurrentProp = property;
            CGDataReference field = GetPropertySourceField<CGDataReference>(property);

            EditorGUI.PrefixLabel(
                position,
                label
            );

            position.x += EditorGUIUtility.labelWidth;
            position.width -= EditorGUIUtility.labelWidth;

            Rect r = new Rect(position);
            if (field.Module != null)
                r.width -= 30;

            string btnLabel = field.Module
                ? string.Format(
                    "{0}.{1}",
                    field.Module.ModuleName,
                    field.SlotName
                )
                : "None";
            string btnTT = field.Module && field.Module.Generator
                ? string.Format(
                    "{0}.{1}.{2}",
                    field.Module.Generator.name,
                    field.Module.ModuleName,
                    field.SlotName
                )
                : "Click to choose";
            if (GUI.Button(
                    r,
                    new GUIContent(
                        btnLabel,
                        btnTT
                    )
                ))
                CGEditorUtility.ShowOutputSlotsMenu(
                    OnMenu,
                    attrib.DataType
                );
            if (field.Module != null)
            {
                r.width = 30;
                r.x = position.xMax - 30;
                if (GUI.Button(
                        r,
                        new GUIContent(
                            CurvyStyles.SelectTexture,
                            "Select"
                        )
                    ))
                    EditorGUIUtility.PingObject(field.Module);
            }
            else
                EditorGUILayout.HelpBox(
                    string.Format(
                        "Missing source of type {0}",
                        attrib.DataType.Name
                    ),
                    MessageType.Error
                );
        }

        private void OnMenu(object userData)
        {
            CGModuleOutputSlot slot = userData as CGModuleOutputSlot;
            CGDataReference field = GetPropertySourceField<CGDataReference>(CurrentProp);
            if (slot == null)
                field.Clear();
            else
                field.setINTERNAL(
                    slot.Module,
                    slot.Info.Name
                );

            CurrentProp.serializedObject.ApplyModifiedProperties();
        }
    }
}