// =====================================================================
// Copyright © 2013 ToolBuddy
// All rights reserved
// 
// http://www.toolbuddy.net
// =====================================================================

using FluffyUnderware.Curvy;
using FluffyUnderware.DevTools.Extensions;
using FluffyUnderware.DevToolsEditor;
using UnityEngine;

namespace FluffyUnderware.CurvyEditor.UI
{
    [ToolbarItem(
        140,
        "Curvy",
        "Sync Direction",
        "Synchronize direction of Bezier handles",
        "beziersyncdir,24,24"
    )]
    public class TBCPBezierModeDirection : DTToolbarToggleButton
    {
        public override string StatusBarInfo => "Mirror Bezier Handles Direction";

        public TBCPBezierModeDirection() =>
            KeyBindings.Add(
                new EditorKeyBinding(
                    "Bezier: Sync Dir",
                    "Sync Handles Direction",
                    KeyCode.B
                )
            );

        public override bool On
        {
            get => ((CurvyProject)Project).BezierMode.HasFlag(CurvyBezierModeEnum.Direction);
            set => ((CurvyProject)Project).BezierMode = ((CurvyProject)Project).BezierMode.Set(
                CurvyBezierModeEnum.Direction,
                value
            );
        }


        public override void OnOtherItemClicked(DTToolbarItem other) { } // IMPORTANT!


        public override void OnSelectionChange()
        {
            base.OnSelectionChange();
            CurvySplineSegment cp = DTSelection.GetAs<CurvySplineSegment>();
            Visible = cp && cp.Spline && cp.Spline.Interpolation == CurvyInterpolation.Bezier;
        }
    }
}