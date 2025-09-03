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
        142,
        "Curvy",
        "Sync Connection",
        "Synchronize Bezier handles in a Connection",
        "beziersynccon,24,24"
    )]
    public class TBCPBezierModeConnections : DTToolbarToggleButton
    {
        public override string StatusBarInfo =>
            "Apply 'Sync Handles Length' and 'Sync Handles Direction' on connected Control Points as well";

        public TBCPBezierModeConnections() =>
            KeyBindings.Add(
                new EditorKeyBinding(
                    "Bezier: Sync Con",
                    "Sync connected CP' handles",
                    KeyCode.M
                )
            );

        public override bool On
        {
            get => ((CurvyProject)Project).BezierMode.HasFlag(CurvyBezierModeEnum.Connections);
            set => ((CurvyProject)Project).BezierMode = ((CurvyProject)Project).BezierMode.Set(
                CurvyBezierModeEnum.Connections,
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