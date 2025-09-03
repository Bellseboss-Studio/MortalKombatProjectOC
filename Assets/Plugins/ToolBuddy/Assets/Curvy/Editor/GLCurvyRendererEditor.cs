// =====================================================================
// Copyright © 2013 ToolBuddy
// All rights reserved
// 
// http://www.toolbuddy.net
// =====================================================================

using System.Collections.Generic;
using FluffyUnderware.Curvy;
using FluffyUnderware.Curvy.Components;
using FluffyUnderware.DevTools;
using FluffyUnderware.DevToolsEditor;
using UnityEditor;
using UnityEngine;

namespace FluffyUnderware.CurvyEditor.Components
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(CurvyGLRenderer))]
    public class GLCurvyRendererEditor : DTEditor<CurvyGLRenderer>
    {
        private bool ShowWarning;

        protected override void OnEnable()
        {
            base.OnEnable();
            ShowWarning = Target.GetComponent<Camera>() == null;
        }

        protected override void SetupArrayEx(DTFieldNode node, ArrayExAttribute attribute)
        {
            base.SetupArrayEx(
                node,
                attribute
            );
            node.ArrayEx.elementHeight = 23;
            node.ArrayEx.drawElementCallback = drawSlot;
        }


        private void drawSlot(Rect rect, int index, bool isActive, bool isFocused)
        {
            GLSlotData slot = Target.Splines[index];
            Rect r = new Rect(rect);
            r.height = 19;
            r.width = rect.width - 60;
            r.y += 2;
            slot.Spline = EditorGUI.ObjectField(
                r,
                slot.Spline,
                typeof(CurvySpline),
                true
            ) as CurvySpline;
            r.x += r.width + 2;
            r.width = 50;
            slot.LineColor = EditorGUI.ColorField(
                r,
                slot.LineColor
            );

            // Separator
            if (index > 0)
            {
                DTHandles.PushHandlesColor(
                    new Color(
                        0.1f,
                        0.1f,
                        0.1f
                    )
                );
                Handles.DrawLine(
                    new Vector2(
                        rect.xMin - 5,
                        rect.yMin
                    ),
                    new Vector2(
                        rect.xMax + 4,
                        rect.yMin
                    )
                );
                DTHandles.PopHandlesColor();
            }
        }

        private List<CurvySpline> getDragAndDropSplines()
        {
            List<CurvySpline> res = new List<CurvySpline>();
            if (DragAndDrop.objectReferences.Length > 0)
                foreach (Object o in DragAndDrop.objectReferences)
                    if (o is GameObject)
                    {
                        CurvySpline spl = ((GameObject)o).GetComponent<CurvySpline>();
                        if (spl)
                            res.Add(spl);
                    }

            return res;
        }

        public override void OnInspectorGUI()
        {
            if (ShowWarning)
            {
                EditorGUILayout.HelpBox(
                    "This component needs a GameObject with a camera component present!",
                    MessageType.Error
                );
                return;
            }

            GUILayout.Box(
                new GUIContent("Drag & Drop Splines here!"),
                EditorStyles.miniButton,
                GUILayout.Height(32)
            );
            Rect r = GUILayoutUtility.GetLastRect();

            base.OnInspectorGUI();

            Event ev = Event.current;
            switch (ev.type)
            {
                case EventType.DragUpdated:
                    if (r.Contains(ev.mousePosition))
                        DragAndDrop.visualMode = getDragAndDropSplines().Count > 0
                            ? DragAndDropVisualMode.Copy
                            : DragAndDropVisualMode.Rejected;
                    break;
                case EventType.DragPerform:
                    List<CurvySpline> splinesToAdd = getDragAndDropSplines();
                    Undo.RecordObject(
                        Target,
                        "Add Spline to list"
                    );
                    foreach (CurvySpline spl in splinesToAdd)
                        Target.Splines.Add(new GLSlotData { Spline = spl });
                    break;
            }
        }
    }
}