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
        141,
        "Curvy",
        "Sync Length",
        "Synchronize length of Bezier handles",
        "beziersynclen,24,24"
    )]
    public class TBCPBezierModeLength : DTToolbarToggleButton
    {
        public override string StatusBarInfo => "Mirror Bezier Handles Size";

        public TBCPBezierModeLength() =>
            KeyBindings.Add(
                new EditorKeyBinding(
                    "Bezier: Sync Len",
                    "Sync Handles Length",
                    KeyCode.N
                )
            );

        public override bool On
        {
            get => ((CurvyProject)Project).BezierMode.HasFlag(CurvyBezierModeEnum.Length);
            set => ((CurvyProject)Project).BezierMode = ((CurvyProject)Project).BezierMode.Set(
                CurvyBezierModeEnum.Length,
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