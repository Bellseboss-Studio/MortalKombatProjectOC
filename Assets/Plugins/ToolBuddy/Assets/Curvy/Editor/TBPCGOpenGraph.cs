// =====================================================================
// Copyright © 2013 ToolBuddy
// All rights reserved
// 
// http://www.toolbuddy.net
// =====================================================================

using FluffyUnderware.Curvy.Generator;
using FluffyUnderware.CurvyEditor.Generator;
using FluffyUnderware.DevToolsEditor;

namespace FluffyUnderware.CurvyEditor.UI
{
    [ToolbarItem(
        190,
        "Curvy",
        "Edit",
        "Open CG Editor",
        "opengraph_dark,24,24",
        "opengraph_light,24,24"
    )]
    public class TBPCGOpenGraph : DTToolbarButton
    {
        public override string StatusBarInfo => "Open Curvy Generator Editor";

        public override void OnClick()
        {
            base.OnClick();
            CurvyGenerator pcg = DTSelection.GetAs<CurvyGenerator>();
            if (pcg)
                CGGraph.Open(pcg);
        }

        public override void OnSelectionChange() =>
            Visible = DTSelection.HasComponent<CurvyGenerator>();
    }
}