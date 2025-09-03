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

namespace FluffyUnderware.CurvyEditor.UI
{
    [ToolbarItem(
        160,
        "Curvy",
        "Shift",
        "Shift on curve",
        "shiftcp,24,24"
    )]
    public class TBCPShift : DTToolbarToggleButton
    {
        private CurvySplineSegment selCP;

        private float mMin;
        private float mMax;
        private float mShift;

        public override string StatusBarInfo => "Shifts the Control Point toward the previous or next Control Point";

        private Vector3 getLocalPos()
        {
            CurvySpline curvySpline = selCP.Spline;
            Vector3 result;
            if (mShift >= 0)
            {
                if (curvySpline.IsControlPointASegment(selCP))
                    result = selCP.Interpolate(mShift);
                else
                {
                    CurvySplineSegment previousSegment = curvySpline.GetPreviousSegment(selCP);
                    result = previousSegment
                        ? previousSegment.Interpolate(1)
                        : selCP.transform.localPosition;
                }
            }
            else
            {
                CurvySplineSegment previousSegment = curvySpline.GetPreviousSegment(selCP);
                result = previousSegment
                    ? previousSegment.Interpolate(1 + mShift)
                    : selCP.transform.localPosition;
            }

            return result;
        }

        public override void OnSceneGUI()
        {
            if (On && selCP && selCP.Spline)
            {
                Vector3 pos = selCP.Spline.transform.TransformPoint(getLocalPos());
                DTHandles.PushHandlesColor(CurvyGlobalManager.DefaultGizmoSelectionColor);
                Handles.SphereHandleCap(
                    0,
                    pos,
                    Quaternion.identity,
                    HandleUtility.GetHandleSize(pos) * CurvyGlobalManager.GizmoControlPointSize,
                    EventType.Repaint
                );
                DTHandles.PopHandlesColor();
            }
        }

        public override void RenderClientArea(Rect r)
        {
            base.RenderClientArea(r);

            Background(
                r,
                80,
                32
            );
            SetElementSize(
                ref r,
                80,
                32
            );


            //Slider
            r.y += 8;
            mShift = GUI.HorizontalSlider(
                r,
                mShift,
                mMin,
                mMax
            );

            //Ok button
            Advance(ref r);
            r.width = 32;
            r.y -= 8;
            if (GUI.Button(
                    r,
                    "Ok"
                ))
            {
                Undo.RecordObject(
                    selCP.transform,
                    "Shift Control Point"
                );
                selCP.SetLocalPosition(getLocalPos());
                mShift = 0;
                On = false;
            }
        }


        public override void OnSelectionChange()
        {
            base.OnSelectionChange();
            selCP = DTSelection.GetAs<CurvySplineSegment>();
            Visible = selCP != null && selCP.Spline && selCP.Spline.IsControlPointVisible(selCP);
            if (Visible)
            {
                CurvySpline curvySpline = selCP.Spline;
                mMin = curvySpline.GetPreviousSegment(selCP)
                    ? -0.9f
                    : 0;
                mMax = curvySpline.IsControlPointASegment(selCP)
                    ? 0.9f
                    : 0;
                mShift = 0;
            }
        }
    }
}