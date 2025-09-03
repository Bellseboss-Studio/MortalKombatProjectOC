// =====================================================================
// Copyright © 2013 ToolBuddy
// All rights reserved
// 
// http://www.toolbuddy.net
// =====================================================================

using System.Collections.Generic;
using FluffyUnderware.DevTools;
using JetBrains.Annotations;
using UnityEngine;

namespace FluffyUnderware.Curvy.Generator.Modules
{
    [ModuleInfo(
        "Build/Rasterize Path",
        ModuleName = "Rasterize Path",
        Description = "Rasterizes a virtual path"
    )]
    [HelpURL(AssetInformation.DocsRedirectionBaseUrl + "cgbuildrasterizedpath")]
    public class BuildRasterizedPath : CGModule, IPathProvider
    {
        private const int MinResolution = 1;
        private const int MaxResolution = 100;

        private const float MinAngleThreshold = 0.1f;
        private const float MaxAngleThreshold = 120;

        private const int DefaultResolution = 50;
        private const int DefaultAngleThreshold = 10;

        [HideInInspector]
        [InputSlotInfo(
            typeof(CGPath),
            Name = "Path",
            RequestDataOnly = true
        )]
        public CGModuleInputSlot InPath = new CGModuleInputSlot();

        [HideInInspector]
        [OutputSlotInfo(
            typeof(CGPath),
            Name = "Path",
            DisplayName = "Rasterized Path"
        )]
        public CGModuleOutputSlot OutPath = new CGModuleOutputSlot();

        #region ### Serialized Fields ###

        [FloatRegion(
            UseSlider = true,
            RegionOptionsPropertyName = nameof(RangeOptions),
            Precision = 4
        )]
        [SerializeField]
        private FloatRegion m_Range = FloatRegion.ZeroOne;

        [SerializeField, RangeEx(
             MinResolution,
             MaxResolution,
             "Resolution",
             "Defines how densely the path spline's sampling points are. When the value is 100, the number of sampling points per world distance unit is equal to the spline's Max Points Per Unit"
         )]
        private int m_Resolution = DefaultResolution;

        [SerializeField]
        private bool m_Optimize;

        [FieldCondition(
            nameof(m_Optimize),
            true
        )]
        [SerializeField, RangeEx(
             MinAngleThreshold,
             MaxAngleThreshold
         )]
        private float m_AngleTreshold = DefaultAngleThreshold;

        [SerializeField]
        [Section(
            "Backward Compatibility",
            false
        )]
        [Tooltip(
            "Curvy versions prior to 8.0.0 had a bug in the computation of the rasterization range for closed splines. Enable this value to keep that bugged behaviour if your project depends on it"
        )]
        private bool useBuggedRange;

        #endregion

        #region ### Public Properties ###

        public float From
        {
            get => m_Range.From;
            set
            {
                RasterizationRangeValidator.ValidatedFromValue(ref value);

                if (m_Range.From != value)
                {
                    m_Range.From = value;
                    Dirty = true;
                }
            }
        }

        public float To
        {
            get => m_Range.To;
            set
            {
                RasterizationRangeValidator.ValidateToValue(ref value,
                    From,
                    ClampPath
                );

                if (m_Range.To != value)
                {
                    m_Range.To = value;
                    Dirty = true;
                }
            }
        }

        public float Length
        {
            get => ClampPath
                ? m_Range.To - m_Range.From
                : m_Range.To;
            set
            {
                float v = ClampPath
                    ? value - m_Range.To
                    : value;
                if (m_Range.To != v)
                {
                    m_Range.To = v;
                    Dirty = true;
                }
            }
        }

        /// <summary>
        /// Defines how densely the path spline's sampling points are. When the value is 100, the number of sampling points per world distance unit is equal to the spline's MaxPointsPerUnit
        /// </summary>
        public int Resolution
        {
            get => m_Resolution;
            set
            {
                int v = Mathf.Clamp(
                    value,
                    MinResolution,
                    MaxResolution
                );
                if (m_Resolution != v)
                {
                    m_Resolution = v;
                    Dirty = true;
                }
            }
        }

        public bool Optimize
        {
            get => m_Optimize;
            set
            {
                if (m_Optimize != value)
                {
                    m_Optimize = value;
                    Dirty = true;
                }
            }
        }

        public float AngleThreshold
        {
            get => m_AngleTreshold;
            set
            {
                float v = Mathf.Clamp(
                    value,
                    MinAngleThreshold,
                    MaxAngleThreshold
                );
                if (m_AngleTreshold != v)
                {
                    m_AngleTreshold = v;
                    Dirty = true;
                }
            }
        }

        [CanBeNull]
        public CGPath Path => OutPath.Data.Length == 0
            ? null
            : OutPath.Data[0] as CGPath;

        public bool PathIsClosed => !IsConfigured || InPath.SourceSlot().PathProvider.PathIsClosed;

        /// <summary>
        /// Curvy versions prior to 8.0.0 had a bug in the computation of the rasterization range for closed splines. Enable this value to keep that bugged behaviour if your project depends on it
        /// </summary>
        public bool UseBuggedRange
        {
            get => useBuggedRange;
            set
            {
                if (useBuggedRange != value)
                {
                    useBuggedRange = value;
                    Dirty = true;
                }
            }
        }

        #endregion

        #region ### Private Fields & Properties ###

        private bool ClampPath => UseBuggedRange
            ? PathIsClosed
            : !PathIsClosed;

        private RegionOptions<float> RangeOptions
        {
            get
            {
                if (!PathIsClosed)
                    return RegionOptions<float>.MinMax(
                        0,
                        1
                    );
                return new RegionOptions<float>
                {
                    LabelFrom = "Start",
                    ClampFrom = DTValueClamping.Min,
                    FromMin = 0,
                    LabelTo = "Length",
                    ClampTo = DTValueClamping.Range,
                    ToMin = 0,
                    ToMax = 1
                };
            }
        }

        #endregion

        #region ### Unity Callbacks ###

#if DOCUMENTATION___FORCE_IGNORE___UNITY == false

        protected override void OnEnable()
        {
            base.OnEnable();
            Properties.MinWidth = 250;
            Properties.LabelWidth = 112;
        }

        public override void Reset()
        {
            base.Reset();
            m_Range = FloatRegion.ZeroOne;
            Resolution = DefaultResolution;
            AngleThreshold = DefaultAngleThreshold;
            OutPath.ClearData();
            Optimize = false;
        }

#endif

        #endregion

        #region ### Public Methods ###

        public override void Refresh()
        {
            base.Refresh();
            if (Length == 0)
                OutPath.ClearData();
            else
            {
                List<CGDataRequestParameter> req = new List<CGDataRequestParameter>();
                req.Add(
                    new CGDataRequestRasterization(
                        From,
                        Length,
                        Resolution,
                        AngleThreshold,
                        Optimize
                            ? CGDataRequestRasterization.ModeEnum.Optimized
                            : CGDataRequestRasterization.ModeEnum.Even
                    )
                );
                CGPath path = InPath.GetData<CGPath>(
                    out bool isDisposable,
                    req.ToArray()
                );
#if CURVY_SANITY_CHECKS
                // I forgot why I added this assertion, but I trust my past self
                UnityEngine.Assertions.Assert.IsTrue(path == null || isDisposable);
#endif
                if (path == null)
                    OutPath.ClearData();
                else
                    OutPath.SetDataToElement(path);
            }
        }

        #endregion
    }
}