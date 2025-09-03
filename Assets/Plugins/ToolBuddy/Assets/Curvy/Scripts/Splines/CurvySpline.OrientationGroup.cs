// =====================================================================
// Copyright © 2013 ToolBuddy
// All rights reserved
// 
// http://www.toolbuddy.net
// =====================================================================

using System;
using System.Collections.Generic;
using FluffyUnderware.DevTools;
using FluffyUnderware.DevTools.Extensions;
using JetBrains.Annotations;
using ToolBuddy.Pooling.Collections;
using UnityEngine;
using UnityEngine.Assertions;

namespace FluffyUnderware.Curvy
{
    public partial class CurvySpline
    {
        /// <summary>
        /// A group of segments which orientation is defined separately from the rest of the spline. Is used only for splines which have <see cref="CurvySpline.Orientation"/> set to <see cref="CurvyOrientation.Dynamic"/>.
        /// </summary>
        /// <seealso cref="CurvySplineSegment.SerializedOrientationAnchor"/>
        /// <seealso cref="CurvySplineSegment.Swirl"/>
        /// <seealso cref="CurvySplineSegment.SwirlTurns"/>
        private class OrientationGroup
        {
            [NotNull]
            [ItemNotNull]
            private readonly List<CurvySplineSegment> segments;

            private SegmentGroupMetrics currentMetrics;

            /// <summary>
            /// accumulatedSwirlAngles[i] is the accumulated swirl angle of the subgroup ending with a given segments[i], inclusive
            /// </summary>
            [NotNull]
            private float[] accumulatedSwirlAngles;

            /// <summary>
            /// accumulatedSwirlAngles[i] is the accumulated cache size of the subgroup ending with a given segments[i], inclusive
            /// </summary>
            [NotNull]
            private readonly List<int> accumulatedCacheSizes;

            /// <summary>
            /// The segments that are part of this group
            /// </summary>
            [NotNull]
            public List<CurvySplineSegment> Segments => segments;

            public OrientationGroup()
            {
                segments = new List<CurvySplineSegment>();
                accumulatedSwirlAngles = Array.Empty<float>();
                accumulatedCacheSizes = new List<int>();
            }

            public void SetupOrientationGroup(short anchorIndex,
                [NotNull] [ItemNotNull] List<CurvySplineSegment> splineControlPoints, [NotNull] short[] orientationAnchorIndices)
            {
                segments.Clear();
                accumulatedCacheSizes.Clear();
                currentMetrics = default;

                short currentCpIndex = anchorIndex;
                do
                {
                    CurvySplineSegment segment = splineControlPoints[currentCpIndex];

#if CURVY_SANITY_CHECKS
                    if (ReferenceEquals(
                            segment,
                            null
                        ))
                        throw new ArgumentNullException(nameof(segment));

                    if (ReferenceEquals(
                            segment.Spline,
                            null
                        ))
                        throw new ArgumentException($"{nameof(segment)} has a null Spline");

                    foreach (CurvySplineSegment existingCP in Segments)
                        if (ReferenceEquals(
                                segment.Spline,
                                existingCP.Spline
                            )
                            == false)
                            throw new ArgumentException("Can't add a control point that is not from the same spline");

                    if (segment.Spline.relationshipCache.IsValid == false)
                        throw new ArgumentException(
                            "Can't add a control point that is not from a spline with a valid relationship cache"
                        );

                    if (segment.Spline.IsControlPointASegment(segment) == false)
                        throw new ArgumentException("Can't add a control point that is not a segment");

                    if (segment.Spline.GetNextControlPointIndex(segment) == -1)
                        throw new ArgumentException(
                            "OrientationGroup can not contain a control point that has no next control point"
                        );

#endif

                    //update group's fields
                    segments.Add(segment);
                    currentMetrics.Increment(segment);
                    accumulatedCacheSizes.Add(currentMetrics.CacheSize);

                    //go to next CP
                    currentCpIndex = segment.GetExtrinsicPropertiesINTERNAL().NextControlPointIndex;
                } while
                    //optimized version of !IsControlPointAnOrientationAnchor(anchorGroupCurrentCp)
                    (orientationAnchorIndices[currentCpIndex] != currentCpIndex);
            }

            public void UpdateOrientation()
            {
                ApplyParallelTransport();
                ApplySwirlAndSmoothing();
            }

            private void ApplySwirlAndSmoothing()
            {
                //smoothing and swirl
                float orientationGap = GetOrientationGap();
                bool noSmoothing = orientationGap == 0;
                CurvySplineSegment anchor = segments[0];
                bool noSwirl = anchor.Swirl == CurvyOrientationSwirl.None || anchor.SwirlTurns == 0;

                if (noSmoothing && noSwirl)
                    return;

                float[] swirlAngles = GetAccumulatedSwirlAngles();
                float smoothingAnglePerSample = orientationGap / currentMetrics.CacheSize;
                for (int segmentIndex = 0; segmentIndex < segments.Count; segmentIndex++)
                {
                    CurvySplineSegment segment = segments[segmentIndex];
                    Vector3[] tangents = segment.TangentsApproximation.Array;
                    SubArray<Vector3> upsSubArray = segment.UpsApproximation;
                    Vector3[] ups = upsSubArray.Array;
                    int upsLength = upsSubArray.Count;

                    float accumulatedSwirlAngle;
                    int previousSamplesCounter;
                    {
                        if (segmentIndex == 0)
                        {
                            previousSamplesCounter = 0;
                            accumulatedSwirlAngle = 0;
                        }
                        else
                        {
                            int previousSegmentIndex = segmentIndex - 1;
                            previousSamplesCounter = accumulatedCacheSizes[previousSegmentIndex];
                            accumulatedSwirlAngle = swirlAngles[previousSegmentIndex];
                        }
                    }

                    float swirlAnglePerSample = (swirlAngles[segmentIndex] - accumulatedSwirlAngle) / segment.CacheSize;
                    for (int sampleIndex = 0; sampleIndex < upsLength; sampleIndex++)
                    {
                        float anglePerSample = accumulatedSwirlAngle
                                               + (sampleIndex * swirlAnglePerSample) //swirl
                                               + ((previousSamplesCounter + sampleIndex) * smoothingAnglePerSample); //smoothing

                        ups[sampleIndex] = Quaternion.AngleAxis(
                                               anglePerSample,
                                               tangents[sampleIndex]
                                           )
                                           * ups[sampleIndex];
                    }
                }
            }

            private void ApplyParallelTransport()
            {
                for (int index = 0; index < segments.Count; index++)
                {
                    CurvySplineSegment currentSegment = segments[index];
                    Vector3 segmentInitialUp;
                    {
                        if (index == 0)
                            segmentInitialUp = currentSegment.getOrthoUp0INTERNAL();
                        else
                        {
                            CurvySplineSegment previousSegment = segments[index - 1];
                            segmentInitialUp = previousSegment.UpsApproximation.Array[previousSegment.UpsApproximation.Count - 1];
                        }
                    }
                    currentSegment.refreshOrientationDynamicINTERNAL(segmentInitialUp);
                }
            }

            private float GetOrientationGap()
            {
                CurvySplineSegment lastSegment = segments[segments.Count - 1];
#if CURVY_SANITY_CHECKS
                Assert.IsTrue(lastSegment.Spline.relationshipCache.IsValid);
                Assert.IsTrue(lastSegment.Spline.IsControlPointASegment(lastSegment));
#endif
                CurvySplineSegment postGroupFirstCp =
                    lastSegment.Spline.ControlPoints[lastSegment.GetExtrinsicPropertiesINTERNAL().NextControlPointIndex];

#if CURVY_SANITY_CHECKS
                if (postGroupFirstCp == null)
                    throw new InvalidOperationException(
                        "OrientationGroup can not contain a control point that has no next control point"
                    );
#endif

                Vector3 groupLastUp = lastSegment.UpsApproximation.Array[lastSegment.UpsApproximation.Count - 1];
                Vector3 postGroupFirstUp = postGroupFirstCp.getOrthoUp0INTERNAL();
                return groupLastUp.AngleSigned(
                    postGroupFirstUp,
                    postGroupFirstCp.TangentsApproximation.Array[0]
                );
            }

            [NotNull]
            private float[] GetAccumulatedSwirlAngles()
            {
                if (segments.Count > accumulatedSwirlAngles.Length)
                    Array.Resize(
                        ref accumulatedSwirlAngles,
                        segments.Count
                    );

                CurvySplineSegment anchor = segments[0];
                float swirlTurns = anchor.SwirlTurns;
                SegmentGroupMetrics metrics = currentMetrics;
                int segmentCount = segments.Count;
                switch (anchor.Swirl)
                {
                    case CurvyOrientationSwirl.Segment:
                    {
                        float swirlPerSegment = swirlTurns * 360;
                        for (int i = 0; i < segmentCount; i++)
                            accumulatedSwirlAngles[i] = swirlPerSegment * (i + 1);
                    }
                        break;
                    case CurvyOrientationSwirl.AnchorGroup:
                    {
                        float swirlPerSegment = (swirlTurns * 360) / metrics.SegmentCount;
                        for (int i = 0; i < segmentCount; i++)
                            accumulatedSwirlAngles[i] = swirlPerSegment * (i + 1);
                    }
                        break;
                    case CurvyOrientationSwirl.AnchorGroupAbs:
                    {
                        float constantValue = (swirlTurns * 360) / metrics.Length;
                        float accumulatedSwirl = 0;
                        for (int i = 0; i < segmentCount; i++)
                        {
                            accumulatedSwirl += constantValue * segments[i].Length;
                            accumulatedSwirlAngles[i] = accumulatedSwirl;
                        }
                    }
                        break;
                    case CurvyOrientationSwirl.None:
                        Array.Clear(
                            accumulatedSwirlAngles,
                            0,
                            segmentCount
                        );
                        break;
                    default:
                        Array.Clear(
                            accumulatedSwirlAngles,
                            0,
                            segmentCount
                        );
                        DTLog.LogError($"[Curvy] Invalid Swirl value {anchor.Swirl}");
                        break;
                }

                return accumulatedSwirlAngles;
            }
        }
    }
}