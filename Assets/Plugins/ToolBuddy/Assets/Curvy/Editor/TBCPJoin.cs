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
        162,
        "Curvy",
        "Join",
        "Join Splines",
        "join,24,24"
    )]
    public class TBCPJoin : DTToolbarButton
    {
        public override string StatusBarInfo => mInfo;

        private string mInfo;

        public TBCPJoin() =>
            KeyBindings.Add(
                new EditorKeyBinding(
                    "Join Spline",
                    "Join two splines"
                )
            );


        public override void OnClick()
        {
            base.OnClick();
            CurvySpline source = DTSelection.GetAs<CurvySpline>();
            CurvySplineSegment destCP = DTSelection.GetAs<CurvySplineSegment>();
            int selIdx = destCP.Spline.GetControlPointIndex(destCP) + source.ControlPointCount + 1;
            source.JoinWith(destCP);
            DTSelection.SetGameObjects(
                destCP.Spline.ControlPointsList[Mathf.Min(
                    destCP.Spline.ControlPointCount - 1,
                    selIdx
                )]
            );
        }

        public override void OnSelectionChange()
        {
            CurvySpline source = DTSelection.GetAs<CurvySpline>();
            CurvySplineSegment destCP = DTSelection.GetAs<CurvySplineSegment>();
            Visible = source && destCP && destCP.Spline && source != destCP.Spline;
            mInfo = Visible
                ? string.Format(
                    "Insert all Control Points of <b>{0}</b> after <b>{1}</b>",
                    source.name,
                    destCP
                )
                : "";
        }
    }
}