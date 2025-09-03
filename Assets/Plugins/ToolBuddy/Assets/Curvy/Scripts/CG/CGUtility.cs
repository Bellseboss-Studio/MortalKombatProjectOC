// =====================================================================
// Copyright © 2013 ToolBuddy
// All rights reserved
// 
// http://www.toolbuddy.net
// =====================================================================

using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Assertions;

namespace FluffyUnderware.Curvy.Generator
{
    /// <summary>
    /// Curvy Generator Utility class
    /// </summary>
    public static class CGUtility
    {
        /// <summary>
        /// Calculates lightmap UV's
        /// </summary>
        /// <param name="uv">the UV to create UV2 for</param>
        /// <returns>UV2</returns>
        [UsedImplicitly]
        [Obsolete("Method will get remove in next major update. Copy its content if you need it")]
        public static Vector2[] CalculateUV2(Vector2[] uv)
        {
            Vector2[] UV2 = new Vector2[uv.Length];
            CalculateUV2(
                uv,
                UV2,
                uv.Length
            );
            return UV2;
        }

        /// <summary>
        /// Calculates lightmap UV's. Same as <see cref="CalculateUV2(Vector2[])"/>but without array allocation
        /// </summary>
        /// <param name="uv">the UV to create UV2 for</param>
        /// <param name="uv2">the UV2 array to fill data into</param>
        /// <param name="elementsNumber"> number of array elements to process</param>
        [UsedImplicitly]
        [Obsolete("Method will get remove in next major update. Copy its content if you need it")]
        public static void CalculateUV2(Vector2[] uv, Vector2[] uv2, int elementsNumber)
        {
            float maxU = 0;
            float maxV = 0;
            for (int i = 0; i < elementsNumber; i++)
            {
                maxU = maxU < uv[i].x
                    ? uv[i].x
                    : maxU;
                maxV = maxV < uv[i].y
                    ? uv[i].y
                    : maxV;
            }

            float oneOnMaxU = 1f / maxU;
            float oneOnMaxV = 1f / maxV;
            for (int i1 = 0; i1 < elementsNumber; i1++)
            {
                uv2[i1].x = uv[i1].x * oneOnMaxU;
                uv2[i1].y = uv[i1].y * oneOnMaxV;
            }
        }

        #region ### Rasterization Helpers ###

        /// <summary>
        /// Rasterization Helper class
        /// </summary>
        public static List<ControlPointOption> GetControlPointsWithOptions(CGDataRequestMetaCGOptions options, CurvySpline shape,
            float startDist, float endDist, bool optimize, out int initialMaterialID, out float initialMaxStep)
        {
#if CURVY_SANITY_CHECKS
            Assert.IsTrue(shape.Count > 0);
#endif

            List<ControlPointOption> res = new List<ControlPointOption>();
            initialMaterialID = 0;
            initialMaxStep = float.MaxValue;
            CurvySplineSegment startSeg = shape.DistanceToSegment(startDist);

            CurvySplineSegment finishSeg;
            {
                float clampedEndDist;
                {
                    clampedEndDist = shape.ClampDistance(
                        endDist,
                        shape.Closed
                            ? CurvyClamping.Loop
                            : CurvyClamping.Clamp
                    );
                    if (clampedEndDist == 0)
                        clampedEndDist = endDist;
                }
                finishSeg = clampedEndDist == shape.Length
                    ? shape.LastVisibleControlPoint
                    : shape.DistanceToSegment(clampedEndDist);
                if (endDist != shape.Length && endDist > finishSeg.Distance)
                    finishSeg = shape.GetNextControlPoint(finishSeg);
            }

            MetaCGOptions cgOptions;
            float loopOffset = 0;
            if (startSeg)
            {
                cgOptions = startSeg.GetMetadata<MetaCGOptions>(true);
                initialMaxStep = cgOptions.MaxStepDistance == 0
                    ? float.MaxValue
                    : cgOptions.MaxStepDistance;
                initialMaterialID = cgOptions.MaterialID;
                int currentMaterialID = initialMaterialID;

                float maxDist = cgOptions.MaxStepDistance;
                /*
                if ((options.CheckMaterialID && cgOptions.MaterialID != 0) ||
                       (optimize && cgOptions.MaxStepDistance != 0))
                    res.Add(new ControlPointOption(startSeg.LocalFToTF(0),
                                                   startSeg.Distance,
                                                   true,
                                                   cgOptions.MaterialID,
                                                   options.CheckHardEdges && cgOptions.HardEdge,
                                                   initialMaxStep,
                                                   (options.CheckExtendedUV && cgOptions.UVEdge),
                                                   options.CheckExtendedUV && cgOptions.ExplicitU,
                                                   cgOptions.FirstU,
                                                   cgOptions.SecondU));
                */


                CurvySplineSegment seg = shape.GetNextSegment(startSeg) ?? shape.GetNextControlPoint(startSeg);
                do
                {
                    cgOptions = seg.GetMetadata<MetaCGOptions>(true);
                    if (shape.GetControlPointIndex(seg) < shape.GetControlPointIndex(startSeg))
                        loopOffset = shape.Length;
                    if (options.IncludeControlPoints
                        || cgOptions.CorrectedHardEdge
                        || cgOptions.MaterialID != currentMaterialID
                        || (optimize && cgOptions.MaxStepDistance != maxDist)
                        || cgOptions.CorrectedUVEdge
                        || cgOptions.ExplicitU
                       )
                    {
                        maxDist = cgOptions.MaxStepDistance == 0
                            ? float.MaxValue
                            : cgOptions.MaxStepDistance;
                        currentMaterialID = cgOptions.MaterialID;
                        res.Add(
                            new ControlPointOption(
                                seg.TF + Mathf.FloorToInt(loopOffset / shape.Length),
                                seg.Distance + loopOffset,
                                options.IncludeControlPoints,
                                currentMaterialID,
                                cgOptions.CorrectedHardEdge,
                                cgOptions.MaxStepDistance,
                                cgOptions.CorrectedUVEdge,
                                cgOptions.ExplicitU,
                                cgOptions.FirstU,
                                cgOptions.SecondU
                            )
                        );
                    }

                    seg = shape.GetNextSegment(seg);
                } while (seg && seg != finishSeg);

                // Check UV settings of last cp (not a segment if open spline!)
                if (!seg && shape.LastVisibleControlPoint == finishSeg)
                {
                    cgOptions = finishSeg.GetMetadata<MetaCGOptions>(true);
                    if (cgOptions.ExplicitU)
                        res.Add(
                            new ControlPointOption(
                                1,
                                finishSeg.Distance + loopOffset,
                                options.IncludeControlPoints,
                                currentMaterialID,
                                cgOptions.CorrectedHardEdge,
                                cgOptions.MaxStepDistance,
                                cgOptions.CorrectedUVEdge,
                                cgOptions.ExplicitU,
                                cgOptions.FirstU,
                                cgOptions.SecondU
                            )
                        );
                }
            }

            return res;
        }

        #endregion
    }
}