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
        100,
        "Curvy",
        "Select Parent",
        "",
        "selectparent,24,24"
    )]
    public class TBSelectParent : DTToolbarButton
    {
        public override string StatusBarInfo => "Select parent spline(s)";

        public TBSelectParent() =>
            KeyBindings.Add(
                new EditorKeyBinding(
                    "Select Parent",
                    "",
                    KeyCode.Backslash
                )
            );

        public override void OnClick()
        {
            base.OnClick();
            List<CurvySplineSegment> cps = DTSelection.GetAllAs<CurvySplineSegment>();
            List<CurvySpline> parents = new List<CurvySpline>();
            foreach (CurvySplineSegment cp in cps)
                if (cp.Spline && !parents.Contains(cp.Spline))
                    parents.Add(cp.Spline);

            DTSelection.SetGameObjects(parents.ToArray());
        }

        public override void OnSelectionChange() =>
            Visible = DTSelection.HasComponent<CurvySplineSegment>(true);
    }
}