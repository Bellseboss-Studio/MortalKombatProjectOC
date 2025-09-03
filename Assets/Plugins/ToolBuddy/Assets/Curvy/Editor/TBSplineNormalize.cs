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
        124,
        "Curvy",
        "Normalize",
        "Normalize scale",
        "normalize,24,24"
    )]
    public class TBSplineNormalize : DTToolbarButton
    {
        public override string StatusBarInfo => "Apply transform scale to Control Points and reset scale to 1";

        public TBSplineNormalize() =>
            KeyBindings.Add(
                new EditorKeyBinding(
                    "Normalize",
                    "Normalize spline"
                )
            );

        public override void OnClick()
        {
            List<CurvySpline> splines = DTSelection.GetAllAs<CurvySpline>();
            foreach (CurvySpline spline in splines)
                spline.Normalize();
        }

        public override void OnSelectionChange() =>
            Visible = DTSelection.HasComponent<CurvySpline>(true);
    }
}