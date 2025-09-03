// =====================================================================
// Copyright © 2013 ToolBuddy
// All rights reserved
// 
// http://www.toolbuddy.net
// =====================================================================

using FluffyUnderware.Curvy;
using FluffyUnderware.Curvy.Shapes;
using FluffyUnderware.DevTools.Extensions;
using FluffyUnderware.DevToolsEditor;
using UnityEditor;
using UnityEngine;

namespace FluffyUnderware.CurvyEditor.UI
{
    [ToolbarItem(
        124,
        "Curvy",
        "Shape",
        "Apply a shape",
        "shapewizard,24,24"
    )]
    public class TBSplineSetShape : DTToolbarToggleButton
    {
        public override string StatusBarInfo =>
            "Apply a shape. <b><color=#ff0000>WARNING: THIS CAN'T BE UNDONE!</color></b>";

        private Vector2 scroll;
        private readonly float winHeight = 120;

        public override void RenderClientArea(Rect r)
        {
            base.RenderClientArea(r);
            CurvySpline spline;
            CurvyShape shape = DTSelection.GetAs<CurvyShape>();
            if (shape == null && (spline = DTSelection.GetAs<CurvySpline>()))
            {
                shape = spline.gameObject.AddComponent<CSCircle>();
                shape.Dirty = true;
                shape.Refresh();
            }

            if (shape != null)
            {
                CurvyShapeEditor ShapeEditor = Editor.CreateEditor(
                    shape,
                    typeof(CurvyShapeEditor)
                ) as CurvyShapeEditor;
                if (ShapeEditor != null)
                {
                    FocusedItem = this;
                    ShapeEditor.ShowOnly2DShapes = false;
                    ShapeEditor.ShowPersistent = true;

                    Background(
                        r,
                        300,
                        winHeight
                    );
                    SetElementSize(
                        ref r,
                        300,
                        winHeight
                    );

                    GUILayoutExtension.Area(
                        () =>
                        {
                            scroll = GUILayoutExtension.ScrollView(
                                () => ShapeEditor.OnEmbeddedGUI(),
                                scroll,
                                GUILayout.Height(winHeight - 25)
                            );
                        },
                        r
                    );

                    r.y += winHeight - 20;
                    r.height = 20;

                    if (GUI.Button(
                            r,
                            "Close"
                        ))
                        On = false;

                    Object.DestroyImmediate(ShapeEditor);
                }
            }
        }

        public override void OnSelectionChange()
        {
            Visible = DTSelection.HasComponent<CurvySpline>();
            scroll = Vector2.zero;
        }
    }
}