// =====================================================================
// Copyright © 2013 ToolBuddy
// All rights reserved
// 
// http://www.toolbuddy.net
// =====================================================================

using System.Collections.Generic;
using FluffyUnderware.DevTools;
using UnityEngine;

namespace FluffyUnderware.Curvy.Generator.Modules
{
    [ModuleInfo(
        "Input/Spots",
        ModuleName = "Input Spots",
        Description = "Defines an array of placement spots"
    )]
    [HelpURL(AssetInformation.DocsRedirectionBaseUrl + "cginputspots")]
    public class InputSpots : CGModule
    {
        [HideInInspector]
        [OutputSlotInfo(typeof(CGSpots))]
        public CGModuleOutputSlot OutSpots = new CGModuleOutputSlot();

        #region ### Serialized Fields ###

        [ArrayEx]
        [SerializeField]
        private List<CGSpot> m_Spots = new List<CGSpot>();

        #endregion

        #region ### Public Properties ###

        public List<CGSpot> Spots
        {
            get => m_Spots;
            set
            {
                if (m_Spots != value)
                {
                    m_Spots = value;
                    Dirty = true;
                }
            }
        }

        #endregion

        #region ### Unity Callbacks ###

#if DOCUMENTATION___FORCE_IGNORE___UNITY == false

        protected override void OnEnable()
        {
            base.OnEnable();
            Properties.MinWidth = 250;
        }

        public override void Reset()
        {
            base.Reset();
            Spots.Clear();
        }

#endif

        #endregion

        #region ### Public Methods ###

        public override void Refresh()
        {
            base.Refresh();

            if (OutSpots.IsLinked)
                OutSpots.SetDataToElement(new CGSpots(Spots));
        }

        #endregion
    }
}