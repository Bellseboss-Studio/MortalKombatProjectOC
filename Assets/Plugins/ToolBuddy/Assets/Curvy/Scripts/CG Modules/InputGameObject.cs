// =====================================================================
// Copyright © 2013 ToolBuddy
// All rights reserved
// 
// http://www.toolbuddy.net
// =====================================================================

using System.Collections.Generic;
using System.Linq;
using FluffyUnderware.DevTools;
using UnityEngine;

namespace FluffyUnderware.Curvy.Generator.Modules
{
    [ModuleInfo(
        "Input/GameObjects",
        ModuleName = "Input GameObjects",
        Description = ""
    )]
    [HelpURL(AssetInformation.DocsRedirectionBaseUrl + "cginputgameobject")]
    public class InputGameObject : CGModule
    {
        [HideInInspector]
        [OutputSlotInfo(
            typeof(CGGameObject),
            Array = true
        )]
        public CGModuleOutputSlot OutGameObject = new CGModuleOutputSlot();

        #region ### Serialized Fields ###

        [ArrayEx]
        [SerializeField]
        private List<CGGameObjectProperties> m_GameObjects = new List<CGGameObjectProperties>();

        #endregion

        #region ### Public Properties ###

        public List<CGGameObjectProperties> GameObjects => m_GameObjects;

        public bool SupportsIPE => false;

        #endregion

        #region ### Unity Callbacks ###

#if DOCUMENTATION___FORCE_IGNORE___UNITY == false

        public override void Reset()
        {
            base.Reset();
            GameObjects.Clear();
        }

#endif

        #endregion

        #region ### Public Methods ###

        public override void Refresh()
        {
            base.Refresh();

            WarnAboutInvalidInputs();

            if (OutGameObject.IsLinked)
                OutGameObject.SetDataToCollection(
                    GameObjects
                        .Where(go => go.Object != null)
                        .Select(go => new CGGameObject(go))
                        .ToArray()
                );
        }

        public override void OnTemplateCreated()
        {
            base.OnTemplateCreated();
            GameObjects.Clear();
        }

        #endregion

        #region ### Privates ###

#if DOCUMENTATION___FORCE_IGNORE___CURVY == false
        [System.Diagnostics.Conditional(CompilationSymbols.UnityEditor)]
        private void WarnAboutInvalidInputs()
        {
            if (GameObjects.Exists(g => g.Object == null))
                UIMessages.Add("Missing Game Object input");
        }

#endif

        #endregion
    }
}