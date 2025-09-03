// =====================================================================
// Copyright © 2013 ToolBuddy
// All rights reserved
// 
// http://www.toolbuddy.net
// =====================================================================

using System;
using FluffyUnderware.DevTools;
using UnityEngine;

namespace FluffyUnderware.Curvy.Generator.Modules
{
    [ModuleInfo(
        "Modifier/Variable Mix Shapes",
        ModuleName = "Variable Mix Shapes",
        Description = "Interpolates between two shapes in a way that varies along the shape extrusion"
    )]
    [HelpURL(AssetInformation.DocsRedirectionBaseUrl + "cgvariablemixshapes")]
#pragma warning disable 618
    public class ModifierVariableMixShapes : CGModule, IOnRequestProcessing, IPathProvider
#pragma warning restore 618
    {
        [HideInInspector]
        [InputSlotInfo(
            typeof(CGShape),
            Name = "Shape A"
        )]
        public CGModuleInputSlot InShapeA = new CGModuleInputSlot();

        [HideInInspector]
        [InputSlotInfo(
            typeof(CGShape),
            Name = "Shape B"
        )]
        public CGModuleInputSlot InShapeB = new CGModuleInputSlot();

        [HideInInspector]
        [ShapeOutputSlotInfo(
            OutputsVariableShape = true,
            Array = true,
            ArrayType = SlotInfo.SlotArrayType.Hidden
        )]
        public CGModuleOutputSlot OutShape = new CGModuleOutputSlot();

        #region ### Serialized Fields ###

        [Label(
            "Mix Curve",
            "Mix between the shapes. Values (Y axis) between -1 for Shape A and 1 for Shape B. Times (X axis) between 0 for extrusion start and 1 for extrusion end"
        )]
        [SerializeField]
        private AnimationCurve m_MixCurve = AnimationCurve.Linear(
            0,
            -1,
            1,
            1
        );

        #endregion

        #region ### Public Properties ###

        public bool PathIsClosed => IsConfigured
                                    && InShapeA.SourceSlot().PathProvider.PathIsClosed
                                    && InShapeB.SourceSlot().PathProvider.PathIsClosed;

        /// <summary>
        /// Defines how the result is interpolated. Values (Y axis) between -1 for Shape A and 1 for Shape B. Times (X axis) between 0 for extrusion start and 1 for extrusion end
        /// </summary>
        public AnimationCurve MixCurve
        {
            get => m_MixCurve;
            set
            {
                if (m_MixCurve != value)
                {
                    m_MixCurve = value;
                    Dirty = true;
                }
            }
        }

        #endregion

        #region ### Unity Callbacks ###

#if DOCUMENTATION___FORCE_IGNORE___UNITY == false

        public override void Reset()
        {
            base.Reset();
            m_MixCurve = AnimationCurve.Linear(
                0,
                -1,
                1,
                1
            );
        }

#endif

        #endregion

        #region ### IOnRequestProcessing ###

        public CGData[] OnSlotDataRequest(CGModuleInputSlot requestedBy, CGModuleOutputSlot requestedSlot,
            params CGDataRequestParameter[] requests)
        {
            CGDataRequestShapeRasterization raster = GetRequestParameter<CGDataRequestShapeRasterization>(ref requests);
            if (!raster)
                return Array.Empty<CGData>();

            int pathFLength = raster.RelativeDistances.Count;

            CGData[] result = new CGData[pathFLength];

            if (pathFLength > 0)
            {
#if UNITY_EDITOR
                bool warnedAboutInterpolation = false;
#endif
                CGShape shapeA = InShapeA.GetData<CGShape>(
                    out bool isADisposable,
                    requests
                );
                CGShape shapeB = InShapeB.GetData<CGShape>(
                    out bool isBDisposable,
                    requests
                );

                for (int crossIndex = 0; crossIndex < pathFLength; crossIndex++)
                {
                    float mix = MixCurve.Evaluate(raster.RelativeDistances.Array[crossIndex]);
#if UNITY_EDITOR
                    if ((mix < -1 || mix > 1) && warnedAboutInterpolation == false)
                    {
                        warnedAboutInterpolation = true;
                        UIMessages.Add(
                            String.Format(
                                "Mix Curve should have values between -1 and 1. Found a value of {0} at time {1}. The value was corrected",
                                mix,
                                raster.RelativeDistances.Array[crossIndex]
                            )
                        );
                        mix = Mathf.Clamp(
                            mix,
                            -1,
                            1
                        );
                    }
#endif
                    result[crossIndex] = ModifierMixShapes.MixShapes(
                        shapeA,
                        shapeB,
                        mix,
                        UIMessages,
                        crossIndex != 0
                    );
                }

                if (isADisposable)
                    shapeA.Dispose();

                if (isBDisposable)
                    shapeB.Dispose();
            }

            return result;
        }

        #endregion
    }
}