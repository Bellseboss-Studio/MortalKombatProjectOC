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
        "Modifier/TRS Shape",
        ModuleName = "TRS Shape",
        Description = "Transform,Rotate,Scale a Shape"
    )]
    [HelpURL(AssetInformation.DocsRedirectionBaseUrl + "cgtrsshape")]
#pragma warning disable 618
    public class ModifierTRSShape : TRSModuleBase, IOnRequestProcessing, IPathProvider
#pragma warning restore 618
    {
        [HideInInspector]
        [InputSlotInfo(
            typeof(CGShape),
            Name = "Shape A",
            ModifiesData = true
        )]
        public CGModuleInputSlot InShape = new CGModuleInputSlot();

        [HideInInspector]
        [OutputSlotInfo(typeof(CGShape))]
        public CGModuleOutputSlot OutShape = new CGModuleOutputSlot();

        #region ### Public Properties ###

        public bool PathIsClosed => IsConfigured && InShape.SourceSlot().PathProvider.PathIsClosed;

        #endregion

        #region ### IOnRequestProcessing ###

        public CGData[] OnSlotDataRequest(CGModuleInputSlot requestedBy, CGModuleOutputSlot requestedSlot,
            params CGDataRequestParameter[] requests)
        {
            if (requestedSlot != OutShape)
                return Array.Empty<CGData>();

            CGShape data = InShape.GetData<CGShape>(
                out bool isDisposable,
                requests
            );
#if CURVY_SANITY_CHECKS
            // I forgot why I added this assertion, but I trust my past self
            Assert.IsTrue(data == null || isDisposable);
#endif
            if (data == null)
                return Array.Empty<CGData>();

            ApplyTrsOnShape(data);

            return new CGData[1] { data };
        }

        #endregion
    }
}