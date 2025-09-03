// =====================================================================
// Copyright © 2013 ToolBuddy
// All rights reserved
// 
// http://www.toolbuddy.net
// =====================================================================

using System;
using System.Collections.Generic;
using FluffyUnderware.Curvy.Pools;
using FluffyUnderware.DevTools;
using JetBrains.Annotations;
using ToolBuddy.Pooling.Collections;
using UnityEngine;
using UnityEngine.Assertions;

namespace FluffyUnderware.Curvy.Generator.Modules
{
    [ModuleInfo(
        "Modifier/Mix Paths",
        ModuleName = "Mix Paths",
        Description = "Interpolates between two paths"
    )]
    [HelpURL(AssetInformation.DocsRedirectionBaseUrl + "cgmixpaths")]
#pragma warning disable 618
    public class ModifierMixPaths : CGModule, IOnRequestProcessing, IPathProvider
#pragma warning restore 618
    {
        private const int MixMinValue = -1;
        private const int MixMaxValue = 1;

        [HideInInspector]
        [InputSlotInfo(
            typeof(CGPath),
            Name = "Path A"
        )]
        public CGModuleInputSlot InPathA = new CGModuleInputSlot();

        [HideInInspector]
        [InputSlotInfo(
            typeof(CGPath),
            Name = "Path B"
        )]
        public CGModuleInputSlot InPathB = new CGModuleInputSlot();

        [HideInInspector]
        [OutputSlotInfo(typeof(CGPath))]
        public CGModuleOutputSlot OutPath = new CGModuleOutputSlot();

        #region ### Serialized Fields ###

        [SerializeField, RangeEx(
             MixMinValue,
             MixMaxValue,
             Tooltip = "Mix between the paths. Values between -1 for Path A and 1 for Path B"
         )]
        private float m_Mix;

        #endregion

        #region ### Public Properties ###

        /// <summary>
        /// Defines how the result is interpolated. Values between -1 for Path A and 1 for Path B
        /// </summary>
        public float Mix
        {
            get => m_Mix;
            set
            {
                float validatedValue = Mathf.Clamp(
                    value,
                    MixMinValue,
                    MixMaxValue
                );
                if (m_Mix != validatedValue)
                {
                    m_Mix = validatedValue;
                    Dirty = true;
                }
            }
        }

        public bool PathIsClosed => IsConfigured
                                    && InPathA.SourceSlot().PathProvider.PathIsClosed
                                    && InPathB.SourceSlot().PathProvider.PathIsClosed;

        #endregion

        #region ### Unity Callbacks ###

#if DOCUMENTATION___FORCE_IGNORE___UNITY == false

        protected override void OnEnable()
        {
            base.OnEnable();
            Properties.MinWidth = 250;
            Properties.LabelWidth = 50;
        }

        public override void Reset()
        {
            base.Reset();
            Mix = 0;
        }

#endif

        #endregion

        #region ### IOnRequestProcessing ###

        public CGData[] OnSlotDataRequest(CGModuleInputSlot requestedBy, CGModuleOutputSlot requestedSlot,
            params CGDataRequestParameter[] requests)
        {
            CGDataRequestRasterization raster = GetRequestParameter<CGDataRequestRasterization>(ref requests);
            if (!raster)
                return Array.Empty<CGData>();

            CGPath DataA = InPathA.GetData<CGPath>(
                out bool isADisposable,
                requests
            );
            CGPath DataB = InPathB.GetData<CGPath>(
                out bool isBDisposable,
                requests
            );

            CGPath data = MixPath(
                DataA,
                DataB,
                Mix,
                UIMessages
            );

            if (isADisposable)
                DataA.Dispose();
            if (isBDisposable)
                DataB.Dispose();

            return data == null
                ? Array.Empty<CGData>()
                : new CGData[] { data };
        }

        #endregion

        #region ### Public Static Methods ###

        /// <summary>
        /// Returns the mixed path
        /// </summary>
        /// <param name="pathA"></param>
        /// <param name="pathB"></param>
        /// <param name="mix"> A value between -1 and 1. -1 will select the path with the most points. 1 will select the other </param>
        /// <param name="warningsContainer">Is filled with warnings raised by the mixing logic</param>
        /// <returns>The mixed path</returns>
        [CanBeNull]
        public static CGPath MixPath([CanBeNull] CGPath pathA, [CanBeNull] CGPath pathB, float mix,
            [NotNull] List<string> warningsContainer)
        {
            if (pathA == null)
                return pathB;

            if (pathB == null)
                return pathA;

            int pathVertexCount = Mathf.Max(
                pathA.Count,
                pathB.Count
            );

            CGPath data = new CGPath();
            ModifierMixShapes.InterpolateShape(
                data,
                pathA,
                pathB,
                mix,
                warningsContainer
            );

            float interpolationTime = (mix + 1) * 0.5f;
            Assert.IsTrue(interpolationTime >= 0);
            Assert.IsTrue(interpolationTime <= 1);

            //BUG: Directions should be recomputed based on positions, and not interpolated. This is already done in the Recalculate() method called inside InterpolateShape() (line above), but Recalculate has a bug that makes it not compute Direction[0], so I kept the code bellow to recompute directions.
            //OPTIM avoid double computation of directions
            SubArray<Vector3> directions = ArrayPools.Vector3.Allocate(pathVertexCount);
            if (pathA.Count == pathVertexCount)
                for (int i = 0; i < pathVertexCount; i++)
                {
                    Vector3 bDirection;
                    {
                        float frag;
                        int idx = pathB.GetFIndex(
                            pathA.RelativeDistances.Array[i],
                            out frag
                        );
                        bDirection = Vector3.SlerpUnclamped(
                            pathB.Directions.Array[idx],
                            pathB.Directions.Array[idx + 1],
                            frag
                        );
                    }

                    directions.Array[i] = Vector3.SlerpUnclamped(
                        pathA.Directions.Array[i],
                        bDirection,
                        interpolationTime
                    );
                }
            else
                for (int i = 0; i < pathVertexCount; i++)
                {
                    Vector3 aDirection;
                    {
                        float frag;
                        int idx = pathA.GetFIndex(
                            pathB.RelativeDistances.Array[i],
                            out frag
                        );
                        aDirection = Vector3.SlerpUnclamped(
                            pathA.Directions.Array[idx],
                            pathA.Directions.Array[idx + 1],
                            frag
                        );
                    }

                    directions.Array[i] = Vector3.SlerpUnclamped(
                        aDirection,
                        pathB.Directions.Array[i],
                        interpolationTime
                    );
                }

            data.Directions = directions;
            return data;
        }

        #endregion
    }
}