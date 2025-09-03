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
        "Debug/Volume",
        ModuleName = "Debug Volume"
    )]
    [HelpURL(AssetInformation.DocsRedirectionBaseUrl + "cgdebugvolume")]
    public class DebugVolume : CGModule
    {
        [HideInInspector]
        [InputSlotInfo(
            typeof(CGVolume),
            Name = "Volume"
        )]
        public CGModuleInputSlot InData = new CGModuleInputSlot();

        #region ### Serialized Fields ###

        [Tab("General")]
        public bool ShowPathSamples = true;

        public bool ShowCrossSamples = true;

        [FieldCondition(
            nameof(ShowCrossSamples),
            true
        )]
        [IntRegion(RegionIsOptional = true)]
        public IntRegion LimitCross = new IntRegion(
            0,
            0
        );

        public bool ShowNormals;
        public bool ShowIndex;
        public bool ShowMap;
        public Color PathColor = Color.white;
        public Color VolumeColor = Color.gray;
        public Color NormalColor = Color.yellow;

        [Tab("Interpolate")]
        public bool Interpolate;

        [RangeEx(
            -1,
            1,
            "Path"
        )]
        public float InterpolatePathF;

        [RangeEx(
            -1,
            1,
            "Cross"
        )]
        public float InterpolateCrossF;

        #endregion

        #region ### Unity Callbacks ###

#if DOCUMENTATION___FORCE_IGNORE___UNITY == false

        protected override void OnEnable()
        {
            base.OnEnable();
            Properties.MinWidth = 300;
        }

        public override void Reset()
        {
            base.Reset();
            ShowPathSamples = true;
            ShowCrossSamples = true;
            LimitCross = new IntRegion(
                0,
                0
            );
            ShowNormals = false;
            ShowIndex = false;
            ShowMap = false;
            PathColor = Color.white;
            VolumeColor = Color.gray;
            NormalColor = Color.yellow;
            Interpolate = false;
            InterpolatePathF = 0;
            InterpolateCrossF = 0;
        }

#endif

        #endregion
    }
}