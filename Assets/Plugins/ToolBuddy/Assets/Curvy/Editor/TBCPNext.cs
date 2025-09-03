// =====================================================================
// Copyright © 2013 ToolBuddy
// All rights reserved
// 
// http://www.toolbuddy.net
// =====================================================================

using FluffyUnderware.Curvy;
using FluffyUnderware.DevToolsEditor;
using UnityEngine;

namespace FluffyUnderware.CurvyEditor.UI
{
    [ToolbarItem(
        106,
        "Curvy",
        "Next",
        "Select Next",
        "next,24,24"
    )]
    public class TBCPNext : DTToolbarButton
    {
        public override string StatusBarInfo => "Select next Control Point";

        public TBCPNext() =>
            KeyBindings.Add(
                new EditorKeyBinding(
                    "Select Next",
                    "",
                    KeyCode.Tab
                )
            );

        public override void OnClick()
        {
            base.OnClick();
            CurvySplineSegment cp = DTSelection.GetAs<CurvySplineSegment>();
            if (cp && cp.Spline)
                DTSelection.SetGameObjects(
                    cp.Spline.ControlPointsList[(int)Mathf.Repeat(
                        cp.Spline.GetControlPointIndex(cp) + 1,
                        cp.Spline.ControlPointCount
                    )]
                );
            else
            {
                CurvySpline spl = DTSelection.GetAs<CurvySpline>();
                if (spl && spl.ControlPointCount > 0)
                    DTSelection.SetGameObjects(spl.ControlPointsList[0]);
            }
        }

        public override void OnSelectionChange() =>
            Visible = DTSelection.HasComponent<CurvySplineSegment>() || DTSelection.HasComponent<CurvySpline>();
    }
}