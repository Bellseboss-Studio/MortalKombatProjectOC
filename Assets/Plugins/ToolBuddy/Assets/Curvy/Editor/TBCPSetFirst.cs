// =====================================================================
// Copyright © 2013 ToolBuddy
// All rights reserved
// 
// http://www.toolbuddy.net
// =====================================================================

using FluffyUnderware.Curvy;
using FluffyUnderware.DevToolsEditor;
using UnityEditor;

namespace FluffyUnderware.CurvyEditor.UI
{
    [ToolbarItem(
        161,
        "Curvy",
        "Set 1.",
        "Set as first Control Point",
        "setfirstcp,24,24"
    )]
    public class TBCPSetFirst : DTToolbarButton
    {
        public override string StatusBarInfo => "Make this Control Point the first of the spline";

        public TBCPSetFirst() =>
            KeyBindings.Add(
                new EditorKeyBinding(
                    "Set 1. CP",
                    ""
                )
            );

        public override void OnClick()
        {
            base.OnClick();
            CurvySplineSegment cp = DTSelection.GetAs<CurvySplineSegment>();
            if (cp && cp.Spline)
            {
                Undo.RegisterFullObjectHierarchyUndo(
                    cp.Spline,
                    "Set first CP"
                );
                cp.Spline.SetFirstControlPoint(cp);
            }
        }

        public override void OnSelectionChange()
        {
            base.OnSelectionChange();
            CurvySplineSegment cp = DTSelection.GetAs<CurvySplineSegment>();
            Visible = cp != null;
            Enabled = Visible && cp.Spline && cp.Spline.GetControlPointIndex(cp) > 0;
        }
    }
}