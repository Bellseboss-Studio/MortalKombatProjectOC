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
        105,
        "Curvy",
        "Previous",
        "Select Previous",
        "prev,24,24"
    )]
    public class TBCPPrevious : DTToolbarButton
    {
        public override string StatusBarInfo => "Select previous Control Point";

        public TBCPPrevious() =>
            KeyBindings.Add(
                new EditorKeyBinding(
                    "Select Previous",
                    "",
                    KeyCode.Tab,
                    true
                )
            );

        public override void OnClick()
        {
            base.OnClick();
            CurvySplineSegment cp = DTSelection.GetAs<CurvySplineSegment>();
            if (cp && cp.Spline)
                DTSelection.SetGameObjects(
                    cp.Spline.ControlPointsList[(int)Mathf.Repeat(
                        cp.Spline.GetControlPointIndex(cp) - 1,
                        cp.Spline.ControlPointCount
                    )]
                );
            else
            {
                CurvySpline spl = DTSelection.GetAs<CurvySpline>();
                if (spl && spl.ControlPointCount > 0)
                    DTSelection.SetGameObjects(spl.ControlPointsList[spl.ControlPointCount - 1]);
            }
        }

        public override void OnSelectionChange() =>
            Visible = DTSelection.HasComponent<CurvySplineSegment>() || DTSelection.HasComponent<CurvySpline>();
    }
}