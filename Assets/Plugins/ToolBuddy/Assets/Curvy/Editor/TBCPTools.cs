// =====================================================================
// Copyright © 2013 ToolBuddy
// All rights reserved
// 
// http://www.toolbuddy.net
// =====================================================================

using System.Collections.Generic;
using System.Linq;
using FluffyUnderware.Curvy;
using FluffyUnderware.DevToolsEditor;
using UnityEngine;

namespace FluffyUnderware.CurvyEditor.UI
{
    [ToolbarItem(
        200,
        "Curvy",
        "CPTools",
        "Control Point Tools",
        "tools,24,24"
    )]
    public class TBCPTools : DTToolbarToggleButton
    {
        public override string StatusBarInfo => "Open Control Point Tools menu";

        public class SplineRange
        {
            public CurvySpline Spline;
            public CurvySplineSegment Low;
            public CurvySplineSegment High;

            public bool CanSubdivide =>
                Low && High && High.Spline.GetControlPointIndex(High) - Low.Spline.GetControlPointIndex(Low) > 0;

            public bool CanSimplify =>
                Low && High && High.Spline.GetControlPointIndex(High) - Low.Spline.GetControlPointIndex(Low) > 1;

            public SplineRange(CurvySpline spline)
            {
                Spline = spline;
                Low = null;
                High = null;
            }

            public void AddCP(CurvySplineSegment cp)
            {
                if (Low == null || Low.Spline.GetControlPointIndex(Low) > cp.Spline.GetControlPointIndex(cp))
                    Low = cp;
                if (High == null || High.Spline.GetControlPointIndex(High) < cp.Spline.GetControlPointIndex(cp))
                    High = cp;
            }
        }

        private List<CurvySplineSegment> mCPSelection;
        private readonly Dictionary<CurvySpline, SplineRange> mSplineRanges = new Dictionary<CurvySpline, SplineRange>();

        public bool CanSubdivide
        {
            get
            {
                foreach (SplineRange sr in mSplineRanges.Values)
                    if (sr.CanSubdivide)
                        return true;
                return false;
            }
        }

        public bool CanSimplify
        {
            get
            {
                foreach (SplineRange sr in mSplineRanges.Values)
                    if (sr.CanSimplify)
                        return true;
                return false;
            }
        }


        public override void RenderClientArea(Rect r)
        {
            base.RenderClientArea(r);
            SetElementSize(
                ref r,
                32,
                32
            );
            GUI.enabled = CanSubdivide;
            if (GUI.Button(
                    r,
                    new GUIContent(
                        CurvyStyles.IconSubdivide,
                        "Subdivide"
                    )
                ))
                Subdivide();
            Advance(ref r);

            GUI.enabled = CanSimplify;
            if (GUI.Button(
                    r,
                    new GUIContent(
                        CurvyStyles.IconSimplify,
                        "Simplify"
                    )
                ))
                Simplify();
            Advance(ref r);
            if (GUI.Button(
                    r,
                    new GUIContent(
                        CurvyStyles.IconEqualize,
                        "Equalize"
                    )
                ))
                Equalize();
            GUI.enabled = true;
        }

        public override void OnSelectionChange()
        {
            mCPSelection = DTSelection.GetAllAs<CurvySplineSegment>().Where(cp => cp.Spline != null).ToList();
            getRange();
            Visible = mCPSelection.Count > 1;
            if (!Visible)
                On = false;
        }

        private void Subdivide()
        {
            foreach (SplineRange sr in mSplineRanges.Values)
                if (sr.CanSubdivide)
                    sr.Spline.Subdivide(
                        sr.Low,
                        sr.High
                    );
        }

        private void Simplify()
        {
            foreach (SplineRange sr in mSplineRanges.Values)
                if (sr.CanSimplify)
                    sr.Spline.Simplify(
                        sr.Low,
                        sr.High
                    );
        }

        private void Equalize()
        {
            foreach (SplineRange sr in mSplineRanges.Values)
                if (sr.CanSimplify)
                    sr.Spline.Equalize(
                        sr.Low,
                        sr.High
                    );
        }

        private void getRange()
        {
            mSplineRanges.Clear();
            foreach (CurvySplineSegment cp in mCPSelection)
            {
                SplineRange sr;
                if (!mSplineRanges.TryGetValue(
                        cp.Spline,
                        out sr
                    ))
                {
                    sr = new SplineRange(cp.Spline);
                    mSplineRanges.Add(
                        cp.Spline,
                        sr
                    );
                }

                sr.AddCP(cp);
            }
        }
    }
}