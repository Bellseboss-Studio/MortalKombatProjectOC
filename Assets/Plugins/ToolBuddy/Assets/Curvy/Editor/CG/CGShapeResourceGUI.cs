// =====================================================================
// Copyright © 2013 ToolBuddy
// All rights reserved
// 
// http://www.toolbuddy.net
// =====================================================================

using FluffyUnderware.Curvy;
using FluffyUnderware.DevTools;
using UnityEditor;
using UnityEngine;

namespace FluffyUnderware.CurvyEditor.Generator
{
    [ResourceEditor("Shape")]
    public class CGShapeResourceGUI : CGResourceEditor
    {
        private CurvyShape2D mCurrentShape;
        private readonly string[] mMenuNames;
        private int mSelection;
        private bool mFreeform;

        public CGShapeResourceGUI(Component resource) : base(resource)
        {
            mCurrentShape = resource.GetComponent<CurvyShape2D>();
            mMenuNames = CurvyShape.GetShapesMenuNames(
                mCurrentShape
                    ? mCurrentShape.GetType()
                    : null,
                out mSelection,
                true
            ).ToArray();
            mFreeform = mCurrentShape == null;
        }

        public override bool OnGUI()
        {
            bool dirty = false;

            bool b = GUILayout.Toggle(
                mFreeform,
                "Freeform"
            );
            if (b != mFreeform)
            {
                if (b)
                {
                    mCurrentShape.Spline.ShowGizmos = true;
                    mCurrentShape.Delete();
                    mCurrentShape = null;
                    mFreeform = b;
                }
                else if (EditorUtility.DisplayDialog(
                             "Warning",
                             "The current shape will be irreversible replaced. Are you sure?",
                             "Ok",
                             "Cancel"
                         ))
                {
                    if (DTUtility.DoesPrefabStatusAllowDeletion(
                            Resource.gameObject.gameObject,
                            out string errorMessage
                        ))
                    {
                        mFreeform = b;
                        mCurrentShape =
                            (CurvyShape2D)Resource.gameObject.AddComponent(CurvyShape.GetShapeType(mMenuNames[mSelection]));
                        mCurrentShape.Dirty = true;
                    }
                    else
                        EditorUtility.DisplayDialog(
                            $"Cannot delete Game Object '{Resource.gameObject.name}'",
                            errorMessage,
                            "Ok"
                        );
                }
            }

            if (!mFreeform)
            {
                int sel = EditorGUILayout.Popup(
                    mSelection,
                    mMenuNames
                );
                if (sel != mSelection)
                {
                    if (DTUtility.DoesPrefabStatusAllowDeletion(
                            Resource.gameObject,
                            out string errorMessage
                        ))
                    {
                        mSelection = sel;
                        dirty = true;
                        if (mCurrentShape)
                            mCurrentShape.Delete();
                        mCurrentShape =
                            (CurvyShape2D)Resource.gameObject.AddComponent(CurvyShape.GetShapeType(mMenuNames[mSelection]));
                        mCurrentShape.Dirty = true;
                    }
                    else
                        EditorUtility.DisplayDialog(
                            $"Cannot delete Game Object '{Resource.gameObject.name}'",
                            errorMessage,
                            "Ok"
                        );
                }

                if (mCurrentShape)
                    using (SerializedObject so = new SerializedObject(mCurrentShape))
                    {
                        SerializedProperty prop = so.GetIterator();

                        bool enterChildren = true;

                        while (prop.NextVisible(enterChildren))
                        {
                            switch (prop.name)
                            {
                                case "m_Script":
                                case "InspectorFoldout":
                                case "m_Plane":
                                    //case "m_Persistent":
                                    break;
                                default:
                                    EditorGUILayout.PropertyField(prop);
                                    break;
                            }

                            enterChildren = false;
                        }

                        dirty = dirty || so.ApplyModifiedProperties();
                    }
            }

            return dirty;
        }
    }
}