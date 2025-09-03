namespace FluffyUnderware.DevToolsEditor
{
    public class DTToolbarMargins
    {
        /// <summary>
        /// Margin between the toolbar and the left edge of the window
        /// </summary>
        public int LeftMargin = 5;
        /// <summary>
        /// Margin between the toolbar and the right edge of the window
        /// </summary>
        public int RightMargin = 5;
        /// <summary>
        /// Margin between the toolbar and the top edge of the window
        /// </summary>
        public int TopMargin = 5;
        /// <summary>
        /// Margin between the toolbar and the bottom edge of the window
        /// </summary>
        public int BottomMargin = 5;
        /// <summary>
        /// Spacing between groups of buttons
        /// </summary>
        public int GroupSpacing = 10;
        /// <summary>
        /// Spacing between buttons
        /// </summary>
        public int ButtonSpacing = 3;
        /// <summary>
        /// Number of empty spots at the start of the toolbar
        /// </summary>
        public int StartSpacing;
        /// <summary>
        /// Column or row wrap spacing
        /// </summary>
        public int WrapSpacing = 5;
        /// <summary>
        /// Spacing between a toolbar item and its associated client area
        /// </summary>
        public readonly int ItemClientAreaSpacing = 5;


        public void SaveToPreferences()
        {
            DT.SetEditorPrefs(
                "ToolbarLeftMargin",
                LeftMargin
            );
            DT.SetEditorPrefs(
                "ToolbarRightMargin",
                RightMargin
            );
            DT.SetEditorPrefs(
                "ToolbarTopMargin",
                TopMargin
            );
            DT.SetEditorPrefs(
                "ToolbarBottomMargin",
                BottomMargin
            );
            DT.SetEditorPrefs(
                "ToolbarGroupSpacing",
                GroupSpacing
            );
            DT.SetEditorPrefs(
                "ToolbarButtonSpacing",
                ButtonSpacing
            );
            DT.SetEditorPrefs(
                "ToolbarWrapSpacing",
                WrapSpacing
            );
            DT.SetEditorPrefs(
                "ToolbarStartSpacing",
                StartSpacing
            );
        }

        public void LoadFromPreferences()
        {
            LeftMargin = DT.GetEditorPrefs(
                "ToolbarLeftMargin",
                LeftMargin
            );
            RightMargin = DT.GetEditorPrefs(
                "ToolbarRightMargin",
                RightMargin
            );
            TopMargin = DT.GetEditorPrefs(
                "ToolbarTopMargin",
                TopMargin
            );
            BottomMargin = DT.GetEditorPrefs(
                "ToolbarBottomMargin",
                BottomMargin
            );
            GroupSpacing = DT.GetEditorPrefs(
                "ToolbarGroupSpacing",
                GroupSpacing
            );
            ButtonSpacing = DT.GetEditorPrefs(
                "ToolbarButtonSpacing",
                ButtonSpacing
            );
            WrapSpacing = DT.GetEditorPrefs(
                "ToolbarWrapSpacing",
                WrapSpacing
            );
            StartSpacing = DT.GetEditorPrefs(
                "ToolbarStartSpacing",
                StartSpacing
            );
        }
    }
}