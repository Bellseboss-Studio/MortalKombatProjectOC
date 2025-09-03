// =====================================================================
// Copyright © 2013 ToolBuddy
// All rights reserved
// 
// http://www.toolbuddy.net
// =====================================================================

using FluffyUnderware.Curvy;
using FluffyUnderware.Curvy.Generator;
using FluffyUnderware.CurvyEditor.Generator;
using FluffyUnderware.DevTools;
using FluffyUnderware.DevToolsEditor;
using UnityEditor;
using UnityEngine;

namespace FluffyUnderware.CurvyEditor
{
    [CustomPropertyDrawer(
        typeof(CGResourceManagerAttribute),
        true
    )]
    public class CGResourceManagerPropertyDrawer : DTPropertyDrawer<CGResourceManagerAttribute>
    {
        private CGResourceEditor ResourceEditor;


        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            Rect mControlRect = position;
            label = EditorGUI.BeginProperty(
                position,
                label,
                property
            );
            CGResourceManagerAttribute A = (CGResourceManagerAttribute)attribute;
            CGModule module = (CGModule)property.serializedObject.targetObject;
            Component res = (Component)property.objectReferenceValue;
            if (res)
            {
                Transform parent = res.transform.parent;
                bool managed = parent != null && parent.transform == module.transform;
                if (managed)
                {
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
                            ),
                            CurvyStyles.SmallButton
                        ))
                        Selection.activeObject = property.objectReferenceValue;
                    if (!A.ReadOnly)
                    {
                        mControlRect.x += mControlRect.width + 2;
                        mControlRect.width = 20;
                        if (GUI.Button(
                                mControlRect,
                                new GUIContent(
                                    CurvyStyles.DeleteSmallTexture,
                                    "Delete resource"
                                ),
                                CurvyStyles.SmallButton
                            ))
                            if (EditorUtility.DisplayDialog(
                                    "Delete resource",
                                    "This will permanently delete the resource! This operation cannot be undone. Proceed?",
                                    "Yes",
                                    "No"
                                ))
                            {
                                if (DTUtility.DoesPrefabStatusAllowDeletion(
                                        res.gameObject,
                                        out string errorMessage
                                    ))
                                {
                                    module.DeleteManagedResource(
                                        A.ResourceName,
                                        res
                                    );
                                    property.objectReferenceValue = null;
                                    ResourceEditor = null;
                                }
                                else
                                    EditorUtility.DisplayDialog(
                                        $"Cannot delete Game Object '{res.name}'",
                                        errorMessage,
                                        "Ok"
                                    );
                            }
                    }

                    if (property.objectReferenceValue != null)
                    {
                        //if (!ResourceEditor)
                        ResourceEditor = CGResourceEditorHandler.GetEditor(
                            A.ResourceName,
                            res
                        );

                        if (ResourceEditor)
                            ResourceEditor.OnGUI();
                    }
                }
                else
                {
                    mControlRect.width -= 20;
                    EditorGUI.PropertyField(
                        mControlRect,
                        property,
                        label
                    );
                    mControlRect.x += mControlRect.width + 2;
                    mControlRect.width = 20;
                    if (GUI.Button(
                            mControlRect,
                            new GUIContent(
                                CurvyStyles.ClearSmallTexture,
                                "Unset"
                            )
                        ))
                    {
                        property.objectReferenceValue = null;
                        ResourceEditor = null;
                    }
                }
            }
            else
            {
                mControlRect.width -= 20;
                EditorGUI.PropertyField(
                    mControlRect,
                    property,
                    label
                );
                mControlRect.x = mControlRect.xMax + 2;
                mControlRect.width = 20;
                if (GUI.Button(
                        mControlRect,
                        new GUIContent(
                            CurvyStyles.AddSmallTexture,
                            "Add Managed"
                        )
                    ))
                    // Call AddResource to create and name the resource
                    property.objectReferenceValue = module.AddManagedResource(A.ResourceName);
            }


            EditorGUI.EndProperty();
        }
    }
}