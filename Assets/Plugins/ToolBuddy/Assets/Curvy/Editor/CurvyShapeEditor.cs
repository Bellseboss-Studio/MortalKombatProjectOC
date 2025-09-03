// =====================================================================
// Copyright © 2013 ToolBuddy
// All rights reserved
// 
// http://www.toolbuddy.net
// =====================================================================

using FluffyUnderware.Curvy;
using FluffyUnderware.DevToolsEditor;
using UnityEditor;
using UnityEngine;

namespace FluffyUnderware.CurvyEditor
{
    [CanEditMultipleObjects]
    [CustomEditor(
        typeof(CurvyShape),
        true
    )]
    public class CurvyShapeEditor : CurvyEditorBase<CurvyShape>
    {
        private int mSelection;
        public bool ShowOnly2DShapes = false;
        public bool ShowPersistent;

        protected override void OnEnable() =>
            base.OnEnable();

        protected override void OnReadNodes()
        {
            DTFieldNode node;
            if (Node.FindNodeAt(
                    "m_Plane",
                    out node
                ))
                node.SortOrder = 0;
        }

        private bool ShowShapeSelector()
        {
            EditorGUI.BeginChangeCheck();
            string[] menuNames = CurvyShape.GetShapesMenuNames(
                Target.GetType(),
                out mSelection,
                ShowOnly2DShapes
            ).ToArray();

            mSelection = EditorGUILayout.Popup(
                "Shape Type",
                mSelection,
                menuNames
            );
            if (EditorGUI.EndChangeCheck())
            {
                Target.Replace(menuNames[mSelection]);
                GUIUtility.ExitGUI();
                return true;
            }

            return false;
        }

        /// <summary>
        /// Shows inspector for embedding into other GUI code.
        /// </summary>
        /// <returns>True if the shape script was changed</returns>
        public bool OnEmbeddedGUI()
        {
            bool changed = ShowShapeSelector();
            base.OnInspectorGUI();
            return changed;
        }


        public override void OnInspectorGUI()
        {
            // TODO: CONDITIONAL
            //HideFields("Persistent");
            ShowShapeSelector();
            base.OnInspectorGUI();
            //HideFields();
        }
    }
}