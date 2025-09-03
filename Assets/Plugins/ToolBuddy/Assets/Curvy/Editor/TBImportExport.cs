// =====================================================================
// Copyright © 2013 ToolBuddy
// All rights reserved
// 
// http://www.toolbuddy.net
// =====================================================================

using FluffyUnderware.DevToolsEditor;

namespace FluffyUnderware.CurvyEditor.UI
{
    [ToolbarItem(
        35,
        "Curvy",
        "Import/Export",
        "Import or export splines",
        "importexport_dark,24,24",
        "importexport_light,24,24"
    )]
    public class TBImportExport : DTToolbarButton
    {
        public override string StatusBarInfo => "Import or export splines to/from various formats";

        public TBImportExport() =>
            KeyBindings.Add(
                new EditorKeyBinding(
                    "Import/Export",
                    ""
                )
            );

        public override void OnClick() =>
            ImportExportWizard.Open();

        public override void OnSelectionChange() =>
            Visible = CurvyProject.Instance.ShowGlobalToolbar
                      || CurvyEditorUtility.IsToolbarActivatingObjectSelected();
    }
}