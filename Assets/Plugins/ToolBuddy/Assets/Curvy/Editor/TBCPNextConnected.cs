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
        120,
        "Curvy",
        "Next connected",
        "Toggle between connected CP",
        "nextcon,24,24"
    )]
    public class TBCPNextConnected : DTToolbarButton
    {
        public override string StatusBarInfo => "Select next Control Point being part of this connection";

        public TBCPNextConnected() =>
            KeyBindings.Add(
                new EditorKeyBinding(
                    "Toggle Connection",
                    "",
                    KeyCode.C
                )
            );

        public override void OnClick()
        {
            base.OnClick();
            CurvySplineSegment cp = DTSelection.GetAs<CurvySplineSegment>();
            if (cp)
            {
                int idx = (int)Mathf.Repeat(
                    cp.Connection.ControlPointsList.IndexOf(cp) + 1,
                    cp.Connection.ControlPointsList.Count
                );
                DTSelection.SetGameObjects(cp.Connection.ControlPointsList[idx]);
            }
        }

        public override void OnSelectionChange()
        {
            CurvySplineSegment cp = DTSelection.GetAs<CurvySplineSegment>();
            Visible = cp != null && cp.Connection != null && cp.Connection.ControlPointsList.Count > 1;
        }
    }
}