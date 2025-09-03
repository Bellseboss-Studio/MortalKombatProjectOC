// =====================================================================
// Copyright © 2013 ToolBuddy
// All rights reserved
// 
// http://www.toolbuddy.net
// =====================================================================

using FluffyUnderware.Curvy;
using FluffyUnderware.DevToolsEditor;

namespace FluffyUnderware.CurvyEditor.UI
{
    [ToolbarItem(
        163,
        "Curvy",
        "Split",
        "Split spline at selection",
        "split,24,24"
    )]
    public class TBCPSplit : DTToolbarButton
    {
        public override string StatusBarInfo => "Split current Spline and make this Control Point the first of a new spline";

        public TBCPSplit() =>
            KeyBindings.Add(
                new EditorKeyBinding(
                    "Split Spline",
                    "Split spline at selection"
                )
            );

        public override void OnClick()
        {
            base.OnClick();
            CurvySplineSegment cp = DTSelection.GetAs<CurvySplineSegment>();
            DTSelection.SetGameObjects(cp.Spline.Split(cp));
        }

        public override void OnSelectionChange()
        {
            CurvySplineSegment cp = DTSelection.GetAs<CurvySplineSegment>();
            Visible = cp && cp.Spline && cp.Spline.IsControlPointASegment(cp) && cp.Spline.FirstSegment != cp;
        }
    }
}