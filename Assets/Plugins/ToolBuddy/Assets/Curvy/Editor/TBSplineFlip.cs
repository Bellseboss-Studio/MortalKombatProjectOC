// =====================================================================
// Copyright © 2013 ToolBuddy
// All rights reserved
// 
// http://www.toolbuddy.net
// =====================================================================

using System.Collections.Generic;
using FluffyUnderware.Curvy;
using FluffyUnderware.DevToolsEditor;

namespace FluffyUnderware.CurvyEditor.UI
{
    [ToolbarItem(
        122,
        "Curvy",
        "Flip",
        "Flip spline direction",
        "flip,24,24"
    )]
    public class TBSplineFlip : DTToolbarButton
    {
        public override string StatusBarInfo => "Invert all Control Points, making the spline direction flip";

        public TBSplineFlip() =>
            KeyBindings.Add(
                new EditorKeyBinding(
                    "Flip",
                    "Flip spline direction"
                )
            );

        public override void OnClick()
        {
            List<CurvySpline> splines = DTSelection.GetAllAs<CurvySpline>();
            foreach (CurvySpline spline in splines)
                spline.Flip();
        }

        public override void OnSelectionChange() =>
            Visible = DTSelection.HasComponent<CurvySpline>(true);
    }
}