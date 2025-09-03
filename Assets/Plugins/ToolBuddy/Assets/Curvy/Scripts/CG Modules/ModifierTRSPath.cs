// =====================================================================
// Copyright © 2013 ToolBuddy
// All rights reserved
// 
// http://www.toolbuddy.net
// =====================================================================

using System;
using UnityEngine;
using UnityEngine.Assertions;

namespace FluffyUnderware.Curvy.Generator.Modules
{
    [ModuleInfo(
        "Modifier/TRS Path",
        ModuleName = "TRS Path",
        Description = "Transform,Rotate,Scale a Path"
    )]
    [HelpURL(AssetInformation.DocsRedirectionBaseUrl + "cgtrspath")]
#pragma warning disable 618
    public class ModifierTRSPath : TRSModuleBase, IOnRequestProcessing, IPathProvider
#pragma warning restore 618
    {
        [HideInInspector]
        [InputSlotInfo(
            typeof(CGPath),
            Name = "Path A",
            ModifiesData = true
        )]
        public CGModuleInputSlot InPath = new CGModuleInputSlot();

        [HideInInspector]
        [OutputSlotInfo(typeof(CGPath))]
        public CGModuleOutputSlot OutPath = new CGModuleOutputSlot();


        #region ### Public Properties ###

        public bool PathIsClosed => IsConfigured && InPath.SourceSlot().PathProvider.PathIsClosed;

        #endregion


        #region ### IOnRequestProcessing ###

        public CGData[] OnSlotDataRequest(CGModuleInputSlot requestedBy, CGModuleOutputSlot requestedSlot,
            params CGDataRequestParameter[] requests)
        {
            if (requestedSlot != OutPath)
                return Array.Empty<CGData>();

            CGPath data = InPath.GetData<CGPath>(
                out bool isDisposable,
                requests
            );
#if CURVY_SANITY_CHECKS
            // I forgot why I added this assertion, but I trust my past self
            Assert.IsTrue(data == null || isDisposable);
#endif
            if (data == null)
                return Array.Empty<CGData>();

            Matrix4x4 scaleLessMatrix = ApplyTrsOnShape(data);
            for (int i = 0; i < data.Count; i++)
                data.Directions.Array[i] = scaleLessMatrix.MultiplyVector(data.Directions.Array[i]);

            return new CGData[] { data };
        }
    }

    #endregion
}