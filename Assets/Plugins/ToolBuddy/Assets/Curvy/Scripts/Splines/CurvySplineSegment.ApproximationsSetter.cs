// =====================================================================
// Copyright © 2013 ToolBuddy
// All rights reserved
// 
// http://www.toolbuddy.net
// =====================================================================

using System;
using FluffyUnderware.Curvy.Pools;
using FluffyUnderware.Curvy.Utils;
using FluffyUnderware.DevTools.Extensions;
using JetBrains.Annotations;
using ToolBuddy.Pooling.Collections;
using UnityEngine;
using UnityEngine.Assertions;

namespace FluffyUnderware.Curvy
{
    partial class CurvySplineSegment
    {
        private static class ApproximationsSetter
        {
            #region Positions

            public static void SetPositionsToPoint([NotNull] Approximations approximations, Vector3 currentPosition)
            {
                approximations.ResizePositions(1);

                approximations.Positions.Array[0] = currentPosition;
            }

            public static void SetPositionsToLinear([NotNull] Approximations approximations,
                int elementCount,
                Vector3 startPosition,
                Vector3 endPosition)
            {
                approximations.ResizePositions(elementCount);

                //The following code is the inlined version of this code:
                //        for (int i = 1; i < CacheSize; i++)
                //        {
                //            Approximation[i] = interpolateLinear(i * mStepSize);
                //            t = (Approximation[i] - Approximation[i - 1]);
                //            Length += t.magnitude;
                //            ApproximationDistances[i] = Length;
                //            ApproximationT[i - 1] = t.normalized;
                //        }

                float mStepSize = 1f / (elementCount - 1);

                Vector3[] positions = approximations.Positions.Array;
                positions[0] = startPosition;
                for (int i = 1; i < elementCount - 1; i++)
                    positions[i] = OptimizedOperators.LerpUnclamped(
                        startPosition,
                        endPosition,
                        i * mStepSize
                    );

                positions[elementCount - 1] = endPosition;
            }

            public static void SetPositionsToCatmullRom([NotNull] Approximations approximations,
                int elementCount,
                Vector3 startPosition,
                Vector3 endPosition,
                Vector3 preSegmentPosition,
                Vector3 postSegmentPosition)
            {
                approximations.ResizePositions(elementCount);

                //The following code is the inlined version of this code:
                //        for (int i = 1; i < CacheSize; i++)
                //        {
                //            Approximation[i] = interpolateTCB(i * mStepSize);
                //            t = (Approximation[i] - Approximation[i - 1]);
                //            Length += t.magnitude;
                //            ApproximationDistances[i] = Length;
                //            ApproximationT[i - 1] = t.normalized;
                //        }

                float mStepSize = 1f / (elementCount - 1);

                const double Ft1 = -0.5;
                const double Ft2 = 1.5;
                const double Ft3 = -1.5;
                const double Ft4 = 0.5;
                const double Fu2 = -2.5;
                const double Fu3 = 2;
                const double Fu4 = -0.5;
                const double Fv1 = -0.5;
                const double Fv3 = 0.5;

                double FAX = (Ft1 * preSegmentPosition.x)
                             + (Ft2 * startPosition.x)
                             + (Ft3 * endPosition.x)
                             + (Ft4 * postSegmentPosition.x);
                double FBX = preSegmentPosition.x
                             + (Fu2 * startPosition.x)
                             + (Fu3 * endPosition.x)
                             + (Fu4 * postSegmentPosition.x);
                double FCX = (Fv1 * preSegmentPosition.x) + (Fv3 * endPosition.x);
                double FDX = startPosition.x;

                double FAY = (Ft1 * preSegmentPosition.y)
                             + (Ft2 * startPosition.y)
                             + (Ft3 * endPosition.y)
                             + (Ft4 * postSegmentPosition.y);
                double FBY = preSegmentPosition.y
                             + (Fu2 * startPosition.y)
                             + (Fu3 * endPosition.y)
                             + (Fu4 * postSegmentPosition.y);
                double FCY = (Fv1 * preSegmentPosition.y) + (Fv3 * endPosition.y);
                double FDY = startPosition.y;

                double FAZ = (Ft1 * preSegmentPosition.z)
                             + (Ft2 * startPosition.z)
                             + (Ft3 * endPosition.z)
                             + (Ft4 * postSegmentPosition.z);
                double FBZ = preSegmentPosition.z
                             + (Fu2 * startPosition.z)
                             + (Fu3 * endPosition.z)
                             + (Fu4 * postSegmentPosition.z);
                double FCZ = (Fv1 * preSegmentPosition.z) + (Fv3 * endPosition.z);
                double FDZ = startPosition.z;
                Vector3[] positions = approximations.Positions.Array;
                positions[0] = startPosition;
                for (int i = 1; i < elementCount - 1; i++)
                {
                    float localF = i * mStepSize;

                    positions[i].x = (float)((((((FAX * localF) + FBX) * localF) + FCX) * localF) + FDX);
                    positions[i].y = (float)((((((FAY * localF) + FBY) * localF) + FCY) * localF) + FDY);
                    positions[i].z = (float)((((((FAZ * localF) + FBZ) * localF) + FCZ) * localF) + FDZ);
                }

                positions[elementCount - 1] = endPosition;
            }


            public static void SetPositionsToTCB([NotNull] Approximations approximations,
                int elementCount,
                TcbParameters tcbParameters,
                Vector3 startPosition,
                Vector3 endPosition,
                Vector3 preSegmentPosition,
                Vector3 postSegmentPosition)
            {
                approximations.ResizePositions(elementCount);

                //The following code is the inlined version of this code:
                //        for (int i = 1; i < CacheSize; i++)
                //        {
                //            Approximation[i] = interpolateCatmull(i * mStepSize);
                //            t = (Approximation[i] - Approximation[i - 1]);
                //            Length += t.magnitude;
                //            ApproximationDistances[i] = Length;
                //            ApproximationT[i - 1] = t.normalized;
                //        }

                float mStepSize = 1f / (elementCount - 1);

                float ft0 = tcbParameters.StartTension;
                float ft1 = tcbParameters.EndTension;
                float fc0 = tcbParameters.StartContinuity;
                float fc1 = tcbParameters.EndContinuity;
                float fb0 = tcbParameters.StartBias;
                float fb1 = tcbParameters.EndBias;

                double FFA = (1 - ft0) * (1 + fc0) * (1 + fb0);
                double FFB = (1 - ft0) * (1 - fc0) * (1 - fb0);
                double FFC = (1 - ft1) * (1 - fc1) * (1 + fb1);
                double FFD = (1 - ft1) * (1 + fc1) * (1 - fb1);

                double DD = 2;
                double Ft1 = -FFA / DD;
                double Ft2 = ((+4 + FFA) - FFB - FFC) / DD;
                double Ft3 = ((-4 + FFB + FFC) - FFD) / DD;
                double Ft4 = FFD / DD;
                double Fu1 = (+2 * FFA) / DD;
                double Fu2 = ((-6 - (2 * FFA)) + (2 * FFB) + FFC) / DD;
                double Fu3 = ((+6 - (2 * FFB) - FFC) + FFD) / DD;
                double Fu4 = -FFD / DD;
                double Fv1 = -FFA / DD;
                double Fv2 = (FFA - FFB) / DD;
                double Fv3 = FFB / DD;
                double Fw2 = +2 / DD;

                double FAX = (Ft1 * preSegmentPosition.x)
                             + (Ft2 * startPosition.x)
                             + (Ft3 * endPosition.x)
                             + (Ft4 * postSegmentPosition.x);
                double FBX = (Fu1 * preSegmentPosition.x)
                             + (Fu2 * startPosition.x)
                             + (Fu3 * endPosition.x)
                             + (Fu4 * postSegmentPosition.x);
                double FCX = (Fv1 * preSegmentPosition.x) + (Fv2 * startPosition.x) + (Fv3 * endPosition.x);
                double FDX = Fw2 * startPosition.x;

                double FAY = (Ft1 * preSegmentPosition.y)
                             + (Ft2 * startPosition.y)
                             + (Ft3 * endPosition.y)
                             + (Ft4 * postSegmentPosition.y);
                double FBY = (Fu1 * preSegmentPosition.y)
                             + (Fu2 * startPosition.y)
                             + (Fu3 * endPosition.y)
                             + (Fu4 * postSegmentPosition.y);
                double FCY = (Fv1 * preSegmentPosition.y) + (Fv2 * startPosition.y) + (Fv3 * endPosition.y);
                double FDY = Fw2 * startPosition.y;

                double FAZ = (Ft1 * preSegmentPosition.z)
                             + (Ft2 * startPosition.z)
                             + (Ft3 * endPosition.z)
                             + (Ft4 * postSegmentPosition.z);
                double FBZ = (Fu1 * preSegmentPosition.z)
                             + (Fu2 * startPosition.z)
                             + (Fu3 * endPosition.z)
                             + (Fu4 * postSegmentPosition.z);
                double FCZ = (Fv1 * preSegmentPosition.z) + (Fv2 * startPosition.z) + (Fv3 * endPosition.z);
                double FDZ = Fw2 * startPosition.z;

                Vector3[] positions = approximations.Positions.Array;
                positions[0] = startPosition;
                for (int i = 1; i < elementCount - 1; i++)
                {
                    float localF = i * mStepSize;
                    positions[i].x = (float)((((((FAX * localF) + FBX) * localF) + FCX) * localF) + FDX);
                    positions[i].y = (float)((((((FAY * localF) + FBY) * localF) + FCY) * localF) + FDY);
                    positions[i].z = (float)((((((FAZ * localF) + FBZ) * localF) + FCZ) * localF) + FDZ);
                }

                positions[elementCount - 1] = endPosition;
            }


            public static void SetPositionsToBezier([NotNull] Approximations approximations,
                int elementCount,
                Vector3 startPosition,
                Vector3 startTangent,
                Vector3 endPosition,
                Vector3 endTangent)
            {
                approximations.ResizePositions(elementCount);

                //The following code is the inlined version of this code:
                //        for (int i = 1; i < CacheSize; i++)
                //        {
                //            Approximation[i] = interpolateBezier(i * mStepSize);
                //            t = (Approximation[i] - Approximation[i - 1]);
                //            Length += t.magnitude;
                //            ApproximationDistances[i] = Length;
                //            ApproximationT[i - 1] = t.normalized;
                //        }

                float mStepSize = 1f / (elementCount - 1);

                const double Ft2 = 3;
                const double Ft3 = -3;
                const double Fu1 = 3;
                const double Fu2 = -6;
                const double Fu3 = 3;
                const double Fv1 = -3;
                const double Fv2 = 3;

                double FAX = -startPosition.x + (Ft2 * startTangent.x) + (Ft3 * endTangent.x) + endPosition.x;
                double FBX = (Fu1 * startPosition.x) + (Fu2 * startTangent.x) + (Fu3 * endTangent.x);
                double FCX = (Fv1 * startPosition.x) + (Fv2 * startTangent.x);
                double FDX = startPosition.x;

                double FAY = -startPosition.y + (Ft2 * startTangent.y) + (Ft3 * endTangent.y) + endPosition.y;
                double FBY = (Fu1 * startPosition.y) + (Fu2 * startTangent.y) + (Fu3 * endTangent.y);
                double FCY = (Fv1 * startPosition.y) + (Fv2 * startTangent.y);
                double FDY = startPosition.y;

                double FAZ = -startPosition.z + (Ft2 * startTangent.z) + (Ft3 * endTangent.z) + endPosition.z;
                double FBZ = (Fu1 * startPosition.z) + (Fu2 * startTangent.z) + (Fu3 * endTangent.z);
                double FCZ = (Fv1 * startPosition.z) + (Fv2 * startTangent.z);
                double FDZ = startPosition.z;
                Vector3[] positions = approximations.Positions.Array;
                positions[0] = startPosition;
                for (int i = 1; i < elementCount - 1; i++)
                {
                    float localF = i * mStepSize;

                    positions[i].x = (float)((((((FAX * localF) + FBX) * localF) + FCX) * localF) + FDX);
                    positions[i].y = (float)((((((FAY * localF) + FBY) * localF) + FCY) * localF) + FDY);
                    positions[i].z = (float)((((((FAZ * localF) + FBZ) * localF) + FCZ) * localF) + FDZ);
                }

                positions[elementCount - 1] = endPosition;
            }

            public static void SetPositionsToBSpline([NotNull] Approximations approximations,
                int elementCount,
                SubArray<Vector3> splineP0Array,
                BSplineApproximationParameters bSplineParameters)
            {
#if CURVY_SANITY_CHECKS
                Assert.IsTrue(splineP0Array.Array.Length >= bSplineParameters.Degree + 1);
                Assert.IsTrue(bSplineParameters.StartTf.IsBetween0And1());
                Assert.IsTrue(bSplineParameters.EndTf.IsBetween0And1());
#endif

                approximations.ResizePositions(elementCount);

                float mStepSize = 1f / (elementCount - 1);
                float tfIncrement = mStepSize / bSplineParameters.SegmentsCount;

                int controlPointsCount = bSplineParameters.ControlPoints.Count;
                int n = BSplineHelper.GetBSplineN(
                    controlPointsCount,
                    bSplineParameters.Degree,
                    bSplineParameters.IsClosed
                );

                int previousK = int.MinValue;

                int nPlus1 = n + 1;
                Vector3[] positions = approximations.Positions.Array;

                SubArray<Vector3> splinePsVector = splineP0Array;
                Vector3[] ps = splinePsVector.Array;
                int psCount = splinePsVector.Count;

                //positions[0]
                {
                    BSplineHelper.GetBSplineUAndK(
                        bSplineParameters.StartTf,
                        bSplineParameters.IsClamped,
                        bSplineParameters.Degree,
                        n,
                        out float u,
                        out int k
                    );
                    GetBSplineP0s(
                        bSplineParameters.ControlPoints,
                        controlPointsCount,
                        bSplineParameters.Degree,
                        k,
                        ps
                    );
                    positions[0] = bSplineParameters.IsClamped
                        ? BSplineHelper.DeBoorClamped(
                            bSplineParameters.Degree,
                            k,
                            u,
                            nPlus1,
                            ps
                        )
                        : BSplineHelper.DeBoorUnclamped(
                            bSplineParameters.Degree,
                            k,
                            u,
                            ps
                        );
                }

                SubArray<Vector3> psCopySubArray = ArrayPools.Vector3.Allocate(psCount);
                Vector3[] psCopy = psCopySubArray.Array;

                for (int i = 1; i < elementCount - 1; i++)
                {
                    float tf = bSplineParameters.StartTf + (tfIncrement * i);
                    BSplineHelper.GetBSplineUAndK(
                        tf,
                        bSplineParameters.IsClamped,
                        bSplineParameters.Degree,
                        n,
                        out float u,
                        out int k
                    );

                    if (k != previousK)
                    {
                        GetBSplineP0s(
                            bSplineParameters.ControlPoints,
                            controlPointsCount,
                            bSplineParameters.Degree,
                            k,
                            ps
                        );
                        previousK = k;
                    }

                    Array.Copy(
                        ps,
                        0,
                        psCopy,
                        0,
                        psCount
                    );

                    positions[i] = bSplineParameters.IsClamped
                        ? BSplineHelper.DeBoorClamped(
                            bSplineParameters.Degree,
                            k,
                            u,
                            nPlus1,
                            psCopy
                        )
                        : BSplineHelper.DeBoorUnclamped(
                            bSplineParameters.Degree,
                            k,
                            u,
                            psCopy
                        );
                }

                ArrayPools.Vector3.Free(psCopySubArray);

                //positions[cacheSize]
                {
                    BSplineHelper.GetBSplineUAndK(
                        bSplineParameters.EndTf,
                        bSplineParameters.IsClamped,
                        bSplineParameters.Degree,
                        n,
                        out float u,
                        out int k
                    );
                    GetBSplineP0s(
                        bSplineParameters.ControlPoints,
                        controlPointsCount,
                        bSplineParameters.Degree,
                        k,
                        ps
                    );
                    positions[elementCount - 1] = bSplineParameters.IsClamped
                        ? BSplineHelper.DeBoorClamped(
                            bSplineParameters.Degree,
                            k,
                            u,
                            nPlus1,
                            ps
                        )
                        : BSplineHelper.DeBoorUnclamped(
                            bSplineParameters.Degree,
                            k,
                            u,
                            ps
                        );
                }
            }

            #endregion


            #region Orientations

            public static void SetOrientationToNone([NotNull] Approximations approximations,
                int elementCount)
            {
                approximations.ResizeUps(elementCount);
                Array.Clear(
                    approximations.Ups.Array,
                    0,
                    approximations.Ups.Count
                );
            }

            public static void SetOrientationToStatic([NotNull] Approximations approximations,
                int elementCount,
                Vector3 startUp,
                Vector3 endUp)
            {
                approximations.ResizeUps(elementCount);

                Vector3[] upsArray = approximations.Ups.Array;
                upsArray[0] = startUp;
                if (approximations.Ups.Count > 1)
                {
                    float oneOnCacheSize = 1f / (elementCount - 1);
                    for (int i = 1; i < elementCount - 1; i++)
                        upsArray[i] = Vector3.SlerpUnclamped(
                            startUp,
                            endUp,
                            i * oneOnCacheSize
                        );
                    upsArray[elementCount - 1] = endUp;
                }
            }

            public static void SetOrientationToDynamic([NotNull] Approximations approximations,
                int elementCount,
                Vector3 startUp)
            {
                approximations.ResizeUps(elementCount);

#if CURVY_SANITY_CHECKS
                Assert.IsTrue(approximations.Ups.Count == approximations.Tangents.Count);
#endif

                Vector3[] upsArray = approximations.Ups.Array;
                Vector3[] tangentsArray = approximations.Tangents.Array;

                int upsLength = approximations.Ups.Count;
                upsArray[0] = startUp;

                for (int i = 1; i < upsLength; i++)
                {
                    //Inlined version of ups[i] = DTMath.ParallelTransportFrame(ups[i-1], tangents[i - 1], tangents[i]) and with less checks for performance reasons
                    Vector3 tan0 = tangentsArray[i - 1];
                    Vector3 tan1 = tangentsArray[i];
                    //Inlined version of Vector3 A = Vector3.Cross(tan0, tan1);
                    Vector3 A;
                    {
                        A.x = (tan0.y * tan1.z) - (tan0.z * tan1.y);
                        A.y = (tan0.z * tan1.x) - (tan0.x * tan1.z);
                        A.z = (tan0.x * tan1.y) - (tan0.y * tan1.x);
                    }
                    //Inlined version of float a = (float)Math.Atan2(A.magnitude, Vector3.Dot(tan0, tan1));
                    float a = (float)Math.Atan2(
                        Math.Sqrt((A.x * A.x) + (A.y * A.y) + (A.z * A.z)),
                        (tan0.x * tan1.x) + (tan0.y * tan1.y) + (tan0.z * tan1.z)
                    );
                    upsArray[i] = Quaternion.AngleAxis(
                                      Mathf.Rad2Deg * a,
                                      A
                                  )
                                  * upsArray[i - 1];
                }
            }

            #endregion

            #region Tangents and distances

            public static float SetPointTangentAndDistance(
                [NotNull] Approximations approximations,
                Vector3 previousPosition,
                Vector3 currentPosition,
                Vector3 nextPosition,
                Quaternion currentRotation,
                [NotNull] CurvySplineSegment curvySplineSegment)
            {
                approximations.ResizeDistances(1);
                approximations.ResizeTangents(1);

#if CURVY_SANITY_CHECKS
                Assert.IsTrue(approximations.Positions.Count == approximations.Tangents.Count);
                Assert.IsTrue(approximations.Positions.Count == approximations.Distances.Count);
#endif

                #region Tangent

                CurvySpline curvySpline = curvySplineSegment.Spline;
#if CURVY_SANITY_CHECKS
                Assert.IsTrue(curvySpline != null);
#endif
                CurvySplineSegment previousSegment = curvySpline.GetPreviousSegment(curvySplineSegment);
                if (previousSegment != null)
                    approximations.Tangents.Array[0] = previousSegment.GetTangent(1);
                else
                {
                    if (currentPosition != nextPosition)
                        //Is true for first CP in catmull-rom/TCB splines with Auto End Tangents set to false
                        approximations.Tangents.Array[0] = nextPosition.Subtraction(currentPosition).normalized;
                    else if (currentPosition != previousPosition)
                        //Is true for last CP in catmull-rom/TCB splines with Auto End Tangents set to false
                        approximations.Tangents.Array[0] = currentPosition.Subtraction(previousPosition).normalized;
                    else
                        //Is true for two neighboring CPs have the same position
                        approximations.Tangents.Array[0] = currentRotation * Vector3.forward;
                }

                #endregion

                #region Distance

                approximations.Distances.Array[0] = 0;

                return 0;

                #endregion
            }

            public static float SetSegmentTangentsAndDistances(
                [NotNull] Approximations approximations,
                int elementCount,
                CurvySplineSegment curvySplineSegment)
            {
                approximations.ResizeTangents(elementCount);
                approximations.ResizeDistances(elementCount);

#if CURVY_SANITY_CHECKS
                Assert.IsTrue(approximations.Positions.Count == approximations.Tangents.Count);
                Assert.IsTrue(approximations.Positions.Count == approximations.Distances.Count);
#endif

                Vector3[] positions = approximations.Positions.Array;
                float lengthAccumulator = 0;
                Vector3[] tangents = approximations.Tangents.Array;
                float[] distances = approximations.Distances.Array;
                distances[0] = 0;

                bool isFastTangentCalculation = curvySplineSegment.Spline.TangentCachingMode == CalculationMode.Fast;

                for (int i = 1; i < elementCount; i++)
                {
                    Vector3 delta;
                    delta.x = positions[i].x - positions[i - 1].x;
                    delta.y = positions[i].y - positions[i - 1].y;
                    delta.z = positions[i].z - positions[i - 1].z;

                    float deltaMagnitude = Mathf.Sqrt((delta.x * delta.x) + (delta.y * delta.y) + (delta.z * delta.z));

                    lengthAccumulator += deltaMagnitude;
                    distances[i] = lengthAccumulator;

                    if (isFastTangentCalculation)

                        if (deltaMagnitude > 9.99999974737875E-06)
                        {
                            float oneOnMagnitude = 1 / deltaMagnitude;
                            tangents[i - 1].x = delta.x * oneOnMagnitude;
                            tangents[i - 1].y = delta.y * oneOnMagnitude;
                            tangents[i - 1].z = delta.z * oneOnMagnitude;
                        }
                        else
                            tangents[i - 1] = curvySplineSegment.GetTangent((float)(i - 1) / (elementCount - 1));
                }

                if (isFastTangentCalculation)
                    //is overriden in CurvySpline.EnforceTangentContinuity
                    tangents[elementCount - 1] = tangents[elementCount - 2];
                else
                    SetSegmentPreciseTangents(
                        elementCount,
                        curvySplineSegment,
                        tangents
                    );


                return lengthAccumulator;
            }

            private static void SetSegmentPreciseTangents(
                int elementCount,
                CurvySplineSegment curvySplineSegment,
                Vector3[] tangents)
            {
                CurvySpline spline = curvySplineSegment.Spline;
                CurvyInterpolation interpolation = spline.Interpolation;
                switch (interpolation)
                {
                    case CurvyInterpolation.Bezier:
                        SetTangentsToBezier(
                            elementCount,
                            curvySplineSegment,
                            tangents
                        );

                        break;
                    case CurvyInterpolation.CatmullRom:
                        SetTangentsToCatmullRom(
                            elementCount,
                            curvySplineSegment,
                            tangents
                        );
                        break;

                    case CurvyInterpolation.TCB:
                        SetTangentsToTCB(
                            elementCount,
                            curvySplineSegment,
                            tangents
                        );
                        break;
                    case CurvyInterpolation.Linear:
                        SetTangentsToLinear(
                            elementCount,
                            curvySplineSegment,
                            tangents
                        );
                        break;
                    case CurvyInterpolation.BSpline:
                    {
                        if (spline.IsBSplineClamped)
                            for (int i = 0; i < elementCount; i++)
                            {
                                float localF = (float)i / (elementCount - 1);
                                //I haven't found the formula for Clamped BSpline tangent in the given time, so I am using the old approximation code for now
                                Vector3 position = curvySplineSegment.Interpolate(
                                    localF
                                );
#pragma warning disable CS0618
                                // Type or member is obsolete
                                tangents[i] = curvySplineSegment.GetTangent(
                                    localF,
                                    position
                                );
#pragma warning restore CS0618
                                // Type or member is obsolete
                            }
                        else
                            for (int i = 0; i < elementCount; i++)
                            {
                                float localF = (float)i / (elementCount - 1);
                                tangents[i] =
                                    OptimizedOperators.Normalize(
                                        BSplineTangent_Unclamped(
                                            spline.ControlPointsList,
                                            spline.SegmentToTF(
                                                curvySplineSegment,
                                                localF
                                            ),
                                            spline.IsBSplineClamped,
                                            spline.Closed,
                                            spline.BSplineDegree,
                                            curvySplineSegment.BSplineP0Array.Array,
                                            curvySplineSegment.BSplineDeltaPArray.Array
                                        )
                                    );
                            }
                    }
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(
                            nameof(interpolation),
                            interpolation,
                            null
                        );
                }
            }

            private static void SetTangentsToLinear(
                int elementCount,
                CurvySplineSegment curvySplineSegment,
                Vector3[] tangents)
            {
                //If you modify this, modify also the non inline version used by CurvySpline.GetTangent

                Vector3 tangent = OptimizedOperators.Normalize(
                    curvySplineSegment.threadSafeData.ThreadSafeNextCpLocalPosition.Subtraction(
                        curvySplineSegment.threadSafeData.ThreadSafeLocalPosition
                    )
                );
                for (int i = 0; i < elementCount; i++)
                    tangents[i] = tangent;
            }

            private static void SetTangentsToCatmullRom(
                int elementCount,
                CurvySplineSegment curvySplineSegment,
                Vector3[] tangents)
            {
                //If you modify this, modify also the non inline version used by CurvySpline.GetTangent

                // Unpack vector components for T0, P0, P1, T1
                float T0x = curvySplineSegment.threadSafeData.ThreadSafePreviousCpLocalPosition.x,
                    T0y = curvySplineSegment.threadSafeData.ThreadSafePreviousCpLocalPosition.y,
                    T0z = curvySplineSegment.threadSafeData.ThreadSafePreviousCpLocalPosition.z;
                float P0x = curvySplineSegment.threadSafeData.ThreadSafeLocalPosition.x,
                    P0y = curvySplineSegment.threadSafeData.ThreadSafeLocalPosition.y,
                    P0z = curvySplineSegment.threadSafeData.ThreadSafeLocalPosition.z;
                float P1x = curvySplineSegment.threadSafeData.ThreadSafeNextCpLocalPosition.x,
                    P1y = curvySplineSegment.threadSafeData.ThreadSafeNextCpLocalPosition.y,
                    P1z = curvySplineSegment.threadSafeData.ThreadSafeNextCpLocalPosition.z;
                float T1x = curvySplineSegment.cachedNextControlPoint.threadSafeData
                        .ThreadSafeNextCpLocalPosition.x,
                    T1y = curvySplineSegment.cachedNextControlPoint.threadSafeData
                        .ThreadSafeNextCpLocalPosition.y,
                    T1z = curvySplineSegment.cachedNextControlPoint.threadSafeData
                        .ThreadSafeNextCpLocalPosition.z;

                // a0 = 0.5f * (-T0 + P1)
                float a0x = 0.5f * (-T0x + P1x);
                float a0y = 0.5f * (-T0y + P1y);
                float a0z = 0.5f * (-T0z + P1z);

                // a1 = 0.5f * (2f*T0 - 5f*P0 + 4f*P1 - T1)
                float a1x = 0.5f * ((((2f * T0x) - (5f * P0x)) + (4f * P1x)) - T1x);
                float a1y = 0.5f * ((((2f * T0y) - (5f * P0y)) + (4f * P1y)) - T1y);
                float a1z = 0.5f * ((((2f * T0z) - (5f * P0z)) + (4f * P1z)) - T1z);

                // a2 = 0.5f * (-T0 + 3f*P0 - 3f*P1 + T1)
                float a2x = 0.5f * (((-T0x + (3f * P0x)) - (3f * P1x)) + T1x);
                float a2y = 0.5f * (((-T0y + (3f * P0y)) - (3f * P1y)) + T1y);
                float a2z = 0.5f * (((-T0z + (3f * P0z)) - (3f * P1z)) + T1z);

                for (int i = 0; i < elementCount; i++)
                {
                    float localF = (float)i / (elementCount - 1);
                    float t2 = localF * localF;
                    // Derivative = a0 + (2f * a1 * f) + (3f * a2 * t2)
                    float x = a0x + (2f * a1x * localF) + (3f * a2x * t2);
                    float y = a0y + (2f * a1y * localF) + (3f * a2y * t2);
                    float z = a0z + (2f * a1z * localF) + (3f * a2z * t2);
                    //normalization
                    float magnitude = (float)Math.Sqrt((x * (double)x) + (y * (double)y) + (z * (double)z));
                    if (magnitude > 9.99999974737875E-06)
                    {
                        float inversed = 1 / magnitude;
                        tangents[i].x = x * inversed;
                        tangents[i].y = y * inversed;
                        tangents[i].z = z * inversed;
                    }
                    else
                    {
                        tangents[i].x = 0;
                        tangents[i].y = 0;
                        tangents[i].z = 0;
                    }
                }
            }

            private static void SetTangentsToTCB(
                int elementCount,
                CurvySplineSegment curvySplineSegment,
                Vector3[] tangents)
            {
                //If you modify this, modify also the non inline version used by CurvySpline.GetTangent

                TcbParameters tcbParameters = curvySplineSegment.EffectiveTcbParameters;

                Vector3 T0 = curvySplineSegment.threadSafeData.ThreadSafePreviousCpLocalPosition;
                Vector3 T1 = curvySplineSegment.cachedNextControlPoint.threadSafeData.ThreadSafeNextCpLocalPosition;

                // Unpack vector components
                float P0x = curvySplineSegment.threadSafeData.ThreadSafeLocalPosition.x,
                    P0y = curvySplineSegment.threadSafeData.ThreadSafeLocalPosition.y,
                    P0z = curvySplineSegment.threadSafeData.ThreadSafeLocalPosition.z;
                float P1x = curvySplineSegment.threadSafeData.ThreadSafeNextCpLocalPosition.x,
                    P1y = curvySplineSegment.threadSafeData.ThreadSafeNextCpLocalPosition.y,
                    P1z = curvySplineSegment.threadSafeData.ThreadSafeNextCpLocalPosition.z;


                // Factors for M1
                float factorM1 = 1f - tcbParameters.StartTension;
                float s1 = (1f + tcbParameters.StartContinuity) * (1f + tcbParameters.StartBias);
                float s2 = (1f - tcbParameters.StartContinuity) * (1f - tcbParameters.StartBias);

                // (P1 - P0)
                float diffP1P0x = P1x - P0x;
                float diffP1P0y = P1y - P0y;
                float diffP1P0z = P1z - P0z;

                // M1 = 0.5f * factorM1 * s1*(P0 - T0) + s2*(P1 - P0)
                float M1x = 0.5f
                            * factorM1
                            * (((P0x - T0.x)
                                * s1)
                               + (diffP1P0x * s2));
                float M1y = 0.5f
                            * factorM1
                            * (((P0y - T0.y)
                                * s1)
                               + (diffP1P0y * s2));
                float M1z = 0.5f
                            * factorM1
                            * (((P0z - T0.z)
                                * s1)
                               + (diffP1P0z * s2));

                // Factors for M2
                float factorM2 = 1f - tcbParameters.EndTension;
                float s3 = (1f - tcbParameters.EndContinuity) * (1f + tcbParameters.EndBias);
                float s4 = (1f + tcbParameters.EndContinuity) * (1f - tcbParameters.EndBias);

                // M2 = 0.5f * factorM2 * s3*(P1 - P0) + s4*(T1 - P1)

                float M2x = 0.5f
                            * factorM2
                            * ((diffP1P0x * s3)
                               + ((T1.x
                                   - P1x)
                                  * s4));
                float M2y = 0.5f
                            * factorM2
                            * ((diffP1P0y * s3)
                               + ((T1.y
                                   - P1y)
                                  * s4));
                float M2z = 0.5f
                            * factorM2
                            * ((diffP1P0z * s3)
                               + ((T1.z
                                   - P1z)
                                  * s4));

                for (int i = 0; i < elementCount; i++)
                {
                    float localF = (float)i / (elementCount - 1);

                    // Hermite derivative basis
                    float t2 = localF * localF;
                    float d1 = (6f * t2) - (6f * localF); // coefficient for P0
                    float d2 = ((3f * t2) - (4f * localF)) + 1f; // coefficient for M1
                    float d3 = (-6f * t2) + (6f * localF); // coefficient for P1
                    float d4 = (3f * t2) - (2f * localF); // coefficient for M2

                    float x = (P0x * d1) + (M1x * d2) + (P1x * d3) + (M2x * d4);
                    float y = (P0y * d1) + (M1y * d2) + (P1y * d3) + (M2y * d4);
                    float z = (P0z * d1) + (M1z * d2) + (P1z * d3) + (M2z * d4);

                    //normalization
                    float magnitude = (float)Math.Sqrt((x * (double)x) + (y * (double)y) + (z * (double)z));
                    if (magnitude > 9.99999974737875E-06)
                    {
                        float inversed = 1 / magnitude;
                        tangents[i].x = x * inversed;
                        tangents[i].y = y * inversed;
                        tangents[i].z = z * inversed;
                    }
                    else
                    {
                        tangents[i].x = 0;
                        tangents[i].y = 0;
                        tangents[i].z = 0;
                    }
                }
            }

            private static void SetTangentsToBezier(
                int elementCount,
                CurvySplineSegment curvySplineSegment,
                Vector3[] tangents)
            {
                //If you modify this, modify also the non inline version used by CurvySpline.GetTangent

                Vector3 T0 = curvySplineSegment.threadSafeData.ThreadSafeLocalPosition.Addition(
                    curvySplineSegment.HandleOut
                );
                Vector3 T1 = curvySplineSegment.threadSafeData.ThreadSafeNextCpLocalPosition.Addition(
                    curvySplineSegment.cachedNextControlPoint.HandleIn
                );
                Vector3 P0 = curvySplineSegment.threadSafeData.ThreadSafeLocalPosition;
                Vector3 P1 = curvySplineSegment.threadSafeData.ThreadSafeNextCpLocalPosition;

                for (int i = 0; i < elementCount; i++)
                {
                    float localF = (float)i / (elementCount - 1);

                    float x;
                    float y;
                    float z;

                    if (i == 0 && T0 == P0)
                    {
                        x = T1.x - P0.x;
                        y = T1.y - P0.y;
                        z = T1.z - P0.z;
                    }
                    else if (i == elementCount - 1 && T1 == P1)
                    {
                        x = P1.x - T0.x;
                        y = P1.y - T0.y;
                        z = P1.z - T0.z;
                    }
                    else
                    {
                        float mt = 1f - localF;
                        float mt2 = mt * mt;
                        float t2 = localF * localF;

                        x = (3f * mt2 * (T0.x - P0.x))
                            + (6f * mt * localF * (T1.x - T0.x))
                            + (3f
                               * t2
                               * (P1.x - T1.x));
                        y = (3f * mt2 * (T0.y - P0.y))
                            + (6f * mt * localF * (T1.y - T0.y))
                            + (3f
                               * t2
                               * (P1.y - T1.y));
                        z = (3f * mt2 * (T0.z - P0.z))
                            + (6f * mt * localF * (T1.z - T0.z))
                            + (3f
                               * t2
                               * (P1.z - T1.z));
                    }

                    //normalization
                    float magnitude = (float)Math.Sqrt((x * (double)x) + (y * (double)y) + (z * (double)z));
                    if (magnitude > 9.99999974737875E-06)
                    {
                        float inversed = 1 / magnitude;
                        tangents[i].x = x * inversed;
                        tangents[i].y = y * inversed;
                        tangents[i].z = z * inversed;
                    }
                    else
                    {
                        tangents[i].x = 0;
                        tangents[i].y = 0;
                        tangents[i].z = 0;
                    }
                }
            }

            #endregion
        }
    }
}