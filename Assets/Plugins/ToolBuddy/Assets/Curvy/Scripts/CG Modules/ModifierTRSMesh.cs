// =====================================================================
// Copyright © 2013 ToolBuddy
// All rights reserved
// 
// http://www.toolbuddy.net
// =====================================================================

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

namespace FluffyUnderware.Curvy.Generator.Modules
{
    [ModuleInfo(
        "Modifier/TRS Mesh",
        ModuleName = "TRS Mesh",
        Description = "Transform,Rotate,Scale a VMesh"
    )]
    [HelpURL(AssetInformation.DocsRedirectionBaseUrl + "cgtrsmesh")]
    public class ModifierTRSMesh : TRSModuleBase
    {
        [HideInInspector]
        [InputSlotInfo(
            typeof(CGVMesh),
            Array = true,
            ModifiesData = true
        )]
        public CGModuleInputSlot InVMesh = new CGModuleInputSlot();

        [HideInInspector]
        [OutputSlotInfo(
            typeof(CGVMesh),
            Array = true
        )]
        public CGModuleOutputSlot OutVMesh = new CGModuleOutputSlot();


        #region ### Public Methods ###

        public override void Refresh()
        {
            base.Refresh();
            if (!OutVMesh.IsLinked)
                return;

            List<CGVMesh> vMesh = InVMesh.GetAllData<CGVMesh>(out bool isDisposable);
            Matrix4x4 matrix = Matrix;
#if CURVY_SANITY_CHECKS
            Assert.IsTrue(isDisposable); // to make sure we are not modifying the original data
#endif
            for (int i = 0; i < vMesh.Count; i++)
                vMesh[i].TRS(matrix);

            OutVMesh.SetDataToCollection(vMesh.ToArray());
        }

        #endregion
    }
}