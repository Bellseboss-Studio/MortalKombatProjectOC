// =====================================================================
// Copyright © 2013 ToolBuddy
// All rights reserved
// 
// http://www.toolbuddy.net
// =====================================================================

using System.Collections.Generic;
using System.Linq;
using FluffyUnderware.DevTools;
using FluffyUnderware.DevTools.Extensions;
using UnityEngine;

namespace FluffyUnderware.Curvy.Generator.Modules
{
    [ModuleInfo(
        "Input/Meshes",
        ModuleName = "Input Meshes",
        Description = "Create VMeshes"
    )]
    [HelpURL(AssetInformation.DocsRedirectionBaseUrl + "cginputmesh")]
    public class InputMesh : CGModule, IExternalInput
    {
        [HideInInspector]
        [OutputSlotInfo(
            typeof(CGVMesh),
            Array = true
        )]
        public CGModuleOutputSlot OutVMesh = new CGModuleOutputSlot();

        #region ### Serialized Fields ###

        [SerializeField]
        [ArrayEx]
        private List<CGMeshProperties> m_Meshes = new List<CGMeshProperties>(new[] { new CGMeshProperties() });

        #endregion

        #region ### Public Properties ###

        public List<CGMeshProperties> Meshes => m_Meshes;

        public bool SupportsIPE => false;

        #endregion

        #region ### Unity Callbacks ###

#if DOCUMENTATION___FORCE_IGNORE___UNITY == false

        protected override void OnValidate()
        {
            base.OnValidate();

            foreach (CGMeshProperties m in Meshes)
                m.OnValidate();
        }

        public override void Reset()
        {
            base.Reset();
            Meshes.Clear();
        }

#endif

        #endregion

        #region ### Public Methods ###

        public override void Refresh()
        {
            base.Refresh();

            WarnAboutInvalidInputs();

            if (OutVMesh.IsLinked)
                OutVMesh.SetDataToCollection(
                    Meshes
                        .Where(p => p.Mesh != null)
                        .Select(p => new CGVMesh(p))
                        .ToArray()
                );
        }


        public override void OnTemplateCreated()
        {
            base.OnTemplateCreated();
            Meshes.Clear();
        }

        #endregion

        #region ### Privates ###

#if DOCUMENTATION___FORCE_IGNORE___CURVY == false

        [System.Diagnostics.Conditional(CompilationSymbols.UnityEditor)]
        private void WarnAboutInvalidInputs()
        {
            if (Meshes.Exists(m => m.Mesh == null))
                UIMessages.Add("Missing Mesh input");

            Meshes
                .Select(p => p.Mesh)
                .Where(m => m != null && m.isReadable == false)
                .ForEach(
                    m => UIMessages.Add(
                        $"Input mesh '{m.name}' is not readable. Please set the 'Read/Write Enabled' parameter to true in the mesh model import settings"
                    )
                );
        }

#endif

        #endregion
    }
}