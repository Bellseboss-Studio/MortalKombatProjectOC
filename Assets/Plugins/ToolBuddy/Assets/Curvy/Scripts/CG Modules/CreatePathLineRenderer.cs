// =====================================================================
// Copyright © 2013 ToolBuddy
// All rights reserved
// 
// http://www.toolbuddy.net
// =====================================================================

using FluffyUnderware.Curvy.Utils;
using UnityEngine;

namespace FluffyUnderware.Curvy.Generator.Modules
{
    [ModuleInfo(
        "Create/Path Line Renderer",
        ModuleName = "Create Path Line Renderer",
        Description = "Feeds a Line Renderer with a Path"
    )]
    [RequireComponent(typeof(LineRenderer))]
    [HelpURL(AssetInformation.DocsRedirectionBaseUrl + "cgcreatepathlinerenderer")]
    public class CreatePathLineRenderer : CGModule
    {
        [HideInInspector]
        [InputSlotInfo(
            typeof(CGPath),
            DisplayName = "Rasterized Path"
        )]
        public CGModuleInputSlot InPath = new CGModuleInputSlot();

        #region ### Serialized Fields ###

        public LineRenderer LineRenderer
        {
            get
            {
                if (mLineRenderer == null)
                    mLineRenderer = GetComponent<LineRenderer>();
                return mLineRenderer;
            }
        }

        #endregion

        private LineRenderer mLineRenderer;

        #region ### Unity Callbacks ###

#if DOCUMENTATION___FORCE_IGNORE___UNITY == false

        public override void Reset()
        {
            base.Reset();
            LineRenderer.useWorldSpace = false;
            LineRenderer.textureMode = LineTextureMode.Tile;
            LineRenderer.sharedMaterial = CurvyUtility.GetDefaultMaterial();
        }

#endif

        #endregion

        #region ### Module Overrides ###

        public override void Refresh()
        {
            base.Refresh();
            CGPath path = InPath.GetData<CGPath>(out bool isDisposable);
            if (path != null)
            {
                LineRenderer.useWorldSpace = false;
                LineRenderer.positionCount = path.Positions.Count;
                LineRenderer.SetPositions(path.Positions.Array);
            }
            else
                LineRenderer.positionCount = 0;

            if (isDisposable)
                path.Dispose();
        }

        #endregion
    }
}