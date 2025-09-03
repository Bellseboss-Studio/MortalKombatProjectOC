// =====================================================================
// Copyright © 2013 ToolBuddy
// All rights reserved
// 
// http://www.toolbuddy.net
// =====================================================================

using FluffyUnderware.DevTools;
using UnityEngine;

namespace FluffyUnderware.Curvy.Generator.Modules
{
    [ModuleInfo(
        "Debug/VMesh",
        ModuleName = "Debug VMesh"
    )]
    [HelpURL(AssetInformation.DocsRedirectionBaseUrl + "cgdebugvmesh")]
    public class DebugVMesh : CGModule
    {
        [HideInInspector]
        [InputSlotInfo(
            typeof(CGVMesh),
            Name = "VMesh"
        )]
        public CGModuleInputSlot InData = new CGModuleInputSlot();

        #region ### Serialized Fields ###

        [Tab("General")]
        public bool ShowVertices;

        public bool ShowVertexID;
        public bool ShowUV;

        #endregion

        #region ### Unity Callbacks ###

#if DOCUMENTATION___FORCE_IGNORE___UNITY == false
        public override void Reset()
        {
            base.Reset();
            ShowVertices = false;
            ShowVertexID = false;
            ShowUV = false;
        }

#endif

        #endregion
    }
}