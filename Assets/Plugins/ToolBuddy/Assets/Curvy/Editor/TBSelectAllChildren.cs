// =====================================================================
// Copyright © 2013 ToolBuddy
// All rights reserved
// 
// http://www.toolbuddy.net
// =====================================================================

using System.Collections.Generic;
using FluffyUnderware.Curvy;
using FluffyUnderware.DevToolsEditor;
using UnityEngine;

namespace FluffyUnderware.CurvyEditor.UI
{
    [ToolbarItem(
        101,
        "Curvy",
        "Select Children",
        "",
        "selectchilds,24,24"
    )]
    public class TBSelectAllChildren : DTToolbarButton
    {
        public override string StatusBarInfo => "Select Control Points";

        public TBSelectAllChildren() =>
            KeyBindings.Add(
                new EditorKeyBinding(
                    "Select Children",
                    "",
                    KeyCode.Backslash,
                    true
                )
            );

        public override void OnClick()
        {
            base.OnClick();
            List<CurvySpline> splines = DTSelection.GetAllAs<CurvySpline>();
            List<CurvySplineSegment> cps = DTSelection.GetAllAs<CurvySplineSegment>();
            foreach (CurvySplineSegment cp in cps)
                if (cp.Spline && !splines.Contains(cp.Spline))
                    splines.Add(cp.Spline);
            List<CurvySplineSegment> res = new List<CurvySplineSegment>();
            foreach (CurvySpline spl in splines)
                res.AddRange(spl.ControlPointsList);

            DTSelection.SetGameObjects(res.ToArray());
        }

        public override void OnSelectionChange()
        {
            base.OnSelectionChange();
            Visible = DTSelection.HasComponent<CurvySpline, CurvySplineSegment>(true);
        }
    }
}