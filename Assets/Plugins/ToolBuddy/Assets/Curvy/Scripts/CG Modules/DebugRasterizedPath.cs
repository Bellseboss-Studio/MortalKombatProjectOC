// =====================================================================
// Copyright © 2013 ToolBuddy
// All rights reserved
// 
// http://www.toolbuddy.net
// =====================================================================

using UnityEngine;

namespace FluffyUnderware.Curvy.Generator.Modules
{
    /// <summary>
    /// Shows the tangents and orientation of a path
    /// </summary>
    [ModuleInfo(
        "Debug/Rasterized Path",
        ModuleName = "Debug Rasterized Path",
        Description = "Shows the tangents and orientation of a rasterized path"
    )]
    [HelpURL(AssetInformation.DocsRedirectionBaseUrl + "cgdebugrasterizedpath")]
    public class DebugRasterizedPath : CGModule
    {
        [HideInInspector]
        [InputSlotInfo(
            typeof(CGPath),
            DisplayName = "Rasterized Path"
        )]
        public CGModuleInputSlot InPath = new CGModuleInputSlot();

        /// <summary>
        /// Display the normal at each one of the path's points
        /// </summary>
        [Tooltip("Display the normal at each one of the path's points")]
        public bool ShowNormals = true;

        /// <summary>
        /// Display the orientation at each one of the path's points
        /// </summary>
        [Tooltip("Display the orientation at each one of the path's points")]
        public bool ShowOrientation = true;

        #region ### Module Overrides ###

        #region ### Unity Callbacks ###

#if DOCUMENTATION___FORCE_IGNORE___UNITY == false

        public override void Reset()
        {
            base.Reset();
            ShowNormals = ShowOrientation = true;
        }


#endif

        #endregion

        #endregion
    }
}