// =====================================================================
// Copyright © 2013 ToolBuddy
// All rights reserved
// 
// http://www.toolbuddy.net
// =====================================================================

using UnityEngine;

namespace FluffyUnderware.CurvyEditor.Generator
{
    public partial class CGGraph
    {
        //empiric, based on Unity 2022.3.10f1, confirmed on 2020.3.48f1
        private const int ToolbarHeight = 27;
        private const int StatusBarHeight = 20;
        
        private const int ModuleRefreshHighlightSize = 9;
        private const int ModuleRefreshHighlightDuration = 500;
        private const int LinkSelectionDistance = 6;
    }
}