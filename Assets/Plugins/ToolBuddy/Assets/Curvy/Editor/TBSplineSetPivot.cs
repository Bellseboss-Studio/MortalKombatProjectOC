// =====================================================================
// Copyright © 2013 ToolBuddy
// All rights reserved
// 
// http://www.toolbuddy.net
// =====================================================================

using System.Collections.Generic;
using FluffyUnderware.Curvy;
using FluffyUnderware.DevTools.Extensions;
using FluffyUnderware.DevToolsEditor;
using UnityEditor;
using UnityEngine;

namespace FluffyUnderware.CurvyEditor.UI
{
    [ToolbarItem(
        120,
        "Curvy",
        "Set Pivot",
        "",
        "centerpivot,24,24"
    )]
    public class TBSplineSetPivot : DTToolbarToggleButton
    {
        public override string StatusBarInfo =>
            "Set center/pivot point";

        private float pivotX;
        private float pivotY;
        private float pivotZ;

        public override void OnSelectionChange() =>
            Visible = DTSelection.HasComponent<CurvySpline>(true);

        public override void RenderClientArea(Rect r)
        {
            base.RenderClientArea(r);

            Background(
                r,
                182,
                187
            );
            SetElementSize(
                ref r,
                180,
                185
            );

            EditorGUIUtility.labelWidth = 20;
            GUILayoutExtension.Area(
                DrawArea,
                new Rect(r)
            );
        }

        private void DrawArea()
        {
            GUILayout.Label(
                "X/Y",
                EditorStyles.boldLabel
            );
            for (int y = -1; y <= 1; y++)
                GUILayoutExtension.Horizontal(() => DrawXYHorizontal(y));

            GUILayout.Label(
                "Z/Y",
                EditorStyles.boldLabel
            );
            for (int y = -1; y <= 1; y++)
                GUILayoutExtension.Horizontal(() => DrawYZHorizontal(y));

            if (GUILayout.Button("Apply"))
            {
                SetPivot();
                On = false;
            }
        }

        private void DrawXYHorizontal(int y)
        {
            for (int x = -1; x <= 1; x++)
            {
                DTGUI.PushBackgroundColor(
                    x == pivotX && y == pivotY
                        ? Color.red
                        : GUI.backgroundColor
                );
                if (GUILayout.Button(
                        "",
                        GUILayout.Width(20)
                    ))
                {
                    pivotX = x;
                    pivotY = y;
                }

                DTGUI.PopBackgroundColor();
            }

            if (y == -1)
            {
                GUILayout.Space(20);
                pivotX = EditorGUILayout.FloatField(
                    "X",
                    pivotX
                );
            }
            else if (y == 0)
            {
                GUILayout.Space(20);
                pivotY = EditorGUILayout.FloatField(
                    "Y",
                    pivotY
                );
            }
        }

        private void DrawYZHorizontal(int y)
        {
            for (int z = -1; z <= 1; z++)
            {
                DTGUI.PushBackgroundColor(
                    y == pivotY && z == pivotZ
                        ? Color.red
                        : GUI.backgroundColor
                );
                if (GUILayout.Button(
                        "",
                        GUILayout.Width(20)
                    ))
                {
                    pivotY = y;
                    pivotZ = z;
                }

                DTGUI.PopBackgroundColor();
            }

            if (y == -1)
            {
                GUILayout.Space(20);
                pivotZ = EditorGUILayout.FloatField(
                    "Z",
                    pivotZ
                );
            }
            else if (y == 0)
            {
                GUILayout.Space(20);
                pivotY = EditorGUILayout.FloatField(
                    "Y",
                    pivotY
                );
            }
        }

        public override void OnSceneGUI()
        {
            if (On)
            {
                List<CurvySpline> splines = DTSelection.GetAllAs<CurvySpline>();
                foreach (CurvySpline spl in splines)
                {
                    Vector3 p = spl.SetPivot(
                        pivotX,
                        pivotY,
                        pivotZ,
                        true
                    );
                    DTHandles.PushHandlesColor(
                        new Color(
                            0.3f,
                            0,
                            0
                        )
                    );
                    DTHandles.BoundsCap(spl.Bounds);
                    Handles.SphereHandleCap(
                        0,
                        p,
                        Quaternion.identity,
                        HandleUtility.GetHandleSize(p) * .1f,
                        EventType.Repaint
                    );
                    DTHandles.PopHandlesColor();
                }
            }
        }


        private void SetPivot()
        {
            List<CurvySpline> splines = DTSelection.GetAllAs<CurvySpline>();
            foreach (CurvySpline spl in splines)
                spl.SetPivot(
                    pivotX,
                    pivotY,
                    pivotZ
                );
        }
    }
}