// =====================================================================
// Copyright © 2013 ToolBuddy
// All rights reserved
// 
// http://www.toolbuddy.net
// =====================================================================

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using FluffyUnderware.DevTools;
using JetBrains.Annotations;
using UnityEngine.Assertions;
using Environment = FluffyUnderware.DevTools.Environment;

namespace FluffyUnderware.Curvy
{
    public partial class CurvySpline
    {
        private class DirtinessManager : IDisposable
        {
            /// <summary>
            /// Contains references to persistent data used to avoid per frame allocations
            /// </summary>
            private class Cache
            {
                [NotNull]
                public readonly List<CurvySplineSegment> SegmentList = new List<CurvySplineSegment>();

                [NotNull]
                public readonly HashSet<CurvySplineSegment> SegmentSet = new HashSet<CurvySplineSegment>();

                [NotNull]
                public readonly HashSet<CurvySpline> SplineSet = new HashSet<CurvySpline>();

                [NotNull]
                public readonly OrientationGroup OrientationGroup = new OrientationGroup();
            }


            #region dirty state

            private bool dirtyCurve;

            private bool dirtyOrientation;

            //todo DESIGN I think AllControlPointsAreDirty can be removed, and related code can just fill dirtyControlPointsMinimalSet with all control points instead. Check that perfs lose is not significant before doing so.
            private bool allControlPointsAreDirty;
            private readonly HashSet<CurvySplineSegment> dirtyControlPointsMinimalSet = new HashSet<CurvySplineSegment>();

            private bool DirtyCurve
            {
                get => dirtyCurve;
                set
                {
                    DoSanityChecks();

                    dirtyCurve = value;
                }
            }

            private bool DirtyOrientation
            {
                get => dirtyOrientation;
                set
                {
                    DoSanityChecks();
                    dirtyOrientation = value;
                }
            }

            public bool AllControlPointsAreDirty
            {
                get => allControlPointsAreDirty;
                private set
                {
                    DoSanityChecks();
                    allControlPointsAreDirty = value;
                }
            }

            public bool Dirty => AllControlPointsAreDirty || dirtyControlPointsMinimalSet.Count > 0;

            #endregion

            [NotNull]
            private CurvySpline spline;

            private bool processingDirtyControlPoints;

            private readonly ThreadPoolWorker<CurvySplineSegment> threadWorker = new ThreadPoolWorker<CurvySplineSegment>();

            private readonly Action<CurvySplineSegment, int, int> refreshOrientationStaticAction =
            (
                controlPoint,
                cpIndex,
                cpsCount) => controlPoint.refreshOrientationStaticINTERNAL();

            private readonly Cache cache = new Cache();

            public DirtinessManager(
                [NotNull] CurvySpline spline)
            {
                this.spline = spline;
                Reset();
            }

            public void SetDirtyAll(
                SplineDirtyingType dirtyingType,
                bool dirtyConnectedControlPoints)
            {
                DoSanityChecks();

                AllControlPointsAreDirty = true;
                SetDirtyingFlagsAndInvalidateSplineCurveCachesIfNeeded(dirtyingType);

                if (dirtyConnectedControlPoints)
                    //OPTIM: maintain a boolean saying if any control point has a connection. And then, avoid the following code if that boolean is false. Same suggestion for other dirtying codes related to connections
                    for (int index = 0; index < spline.ControlPoints.Count; index++)
                    {
                        CurvySplineSegment controlPoint = spline.ControlPoints[index];
                        if (controlPoint
                            && controlPoint
                                .Connection) //controlPoint can be null if you create a spline via the pen tool, and then undo it
                        {
                            ReadOnlyCollection<CurvySplineSegment> connectedControlPoints =
                                controlPoint.Connection.ControlPointsList;
                            for (int connectedIndex = 0; connectedIndex < connectedControlPoints.Count; connectedIndex++)
                            {
                                CurvySplineSegment connectedControlPoint = connectedControlPoints[connectedIndex];
                                CurvySpline connectedSpline = connectedControlPoint != null
                                    ? connectedControlPoint.Spline
                                    : null;
                                if (connectedSpline && connectedSpline != spline)
                                    connectedSpline.dirtinessManager
                                        .AddToMinimalSetAndSetDirtyingFlagsAndInvalidateSplineCurveCachesIfNeeded(
                                            connectedControlPoint,
                                            dirtyingType
                                        );
                            }
                        }
                    }
            }

            /// <summary>
            /// Marks a Control Point to get recalculated on next call to Refresh(). Will also mark connected control points if dirtyConnection is set to true.  Will also mark control points that depend on the current one through the Follow-Up feature.
            /// </summary>
            /// <param name="controlPoint">the Control Point to refresh</param>
            /// <param name="dirtyingType">Defines what aspect should be dirtied</param>
            /// <param name="previousControlPoint"></param>
            /// <param name="nextControlPoint"></param>
            /// <param name="ignoreConnectionOfInputControlPoint">If true, this method will not mark as dirty the control points connected to the "controlPoint" parameter</param>
            public void SetDirty(
                CurvySplineSegment controlPoint,
                SplineDirtyingType dirtyingType,
                CurvySplineSegment previousControlPoint,
                CurvySplineSegment nextControlPoint,
                bool ignoreConnectionOfInputControlPoint)
            {
                DoSanityChecks();

                if (ReferenceEquals(
                        spline,
                        controlPoint.Spline
                    )
                    == false)
                    throw new ArgumentException(
                        String.Format(
                            InvalidCPErrorMessage,
                            controlPoint,
                            spline.name
                        )
                    );

                if (ignoreConnectionOfInputControlPoint == false && controlPoint.Connection)
                {
                    //Setting all connected CPs is a bit overkill, but at least, you are sure to avoid the multitude of Connections related bugs, plus simplifies the code a lot. You might try to OPTIM by dirtying only the relevant connected CPs, and only in the relevant scenarios, but (seeing the old code that I removed) it is a very dangerous optimization, and you can surely optimize other stuff that will take less time to optimize, and can generate less bugs
                    ReadOnlyCollection<CurvySplineSegment> connectionControlPoints = controlPoint.Connection.ControlPointsList;
                    for (int index = 0; index < connectionControlPoints.Count; index++)
                    {
                        CurvySplineSegment connectedControlPoint = connectionControlPoints[index];
                        CurvySpline connectedSpline = connectedControlPoint.Spline;
                        if (connectedSpline)
                            connectedSpline.dirtinessManager
                                .AddToMinimalSetAndSetDirtyingFlagsAndInvalidateSplineCurveCachesIfNeeded(
                                    connectedControlPoint,
                                    dirtyingType
                                );
                    }
#if CURVY_SANITY_CHECKS
                    if (connectionControlPoints.Contains(controlPoint) == false)
                        DTLog.LogError(
                            "[Curvy] SetDirty couldn't find the dirtying control point in the connection.",
                            spline
                        );
#endif
                }
                else
                    AddToMinimalSetAndSetDirtyingFlagsAndInvalidateSplineCurveCachesIfNeeded(
                        controlPoint,
                        dirtyingType
                    );

                //Dirty CPs that could depend on the current CP through the Follow-Up feature
                {
                    if (previousControlPoint && previousControlPoint.Connection)
                    {
                        ReadOnlyCollection<CurvySplineSegment> connectionControlPoints =
                            previousControlPoint.Connection.ControlPointsList;
                        for (int index = 0; index < connectionControlPoints.Count; index++)
                        {
                            CurvySplineSegment connectedControlPoint = connectionControlPoints[index];
                            CurvySpline connectedSpline = connectedControlPoint.Spline;
                            if (connectedSpline && connectedControlPoint.FollowUp == previousControlPoint)
                                connectedSpline.dirtinessManager
                                    .AddToMinimalSetAndSetDirtyingFlagsAndInvalidateSplineCurveCachesIfNeeded(
                                        connectedControlPoint,
                                        dirtyingType
                                    );
                        }
                    }

                    if (nextControlPoint && nextControlPoint.Connection)
                    {
                        ReadOnlyCollection<CurvySplineSegment> connectionControlPoints =
                            nextControlPoint.Connection.ControlPointsList;
                        for (int index = 0; index < connectionControlPoints.Count; index++)
                        {
                            CurvySplineSegment connectedControlPoint = connectionControlPoints[index];
                            CurvySpline connectedSpline = connectedControlPoint.Spline;
                            if (connectedSpline && connectedControlPoint.FollowUp == nextControlPoint)
                                connectedSpline.dirtinessManager
                                    .AddToMinimalSetAndSetDirtyingFlagsAndInvalidateSplineCurveCachesIfNeeded(
                                        connectedControlPoint,
                                        dirtyingType
                                    );
                        }
                    }
                }
            }

            public void ClearMinimalSet()
            {
                DoSanityChecks();
                dirtyControlPointsMinimalSet.Clear();
            }

            public void RemoveFromMinimalSet(
                CurvySplineSegment item)
            {
                DoSanityChecks();

                dirtyControlPointsMinimalSet.Remove(item);
            }

            [MustUseReturnValue]
            public bool ProcessDirtyControlPoints()
            {
                spline.relationshipCache.EnsureIsValid();
#if CURVY_SANITY_CHECKS
                Assert.IsTrue(
                    spline.cpsSynchronizer.CurrentRequest != ControlPointsSynchronizer.SynchronizationRequest.HierarchyToSpline
                );
#endif

                if (Dirty == false)
                    return false;

                if (DirtyOrientation == false && DirtyCurve == false)
                    throw new InvalidOperationException("[Curvy] Processing dirty control points while no dirtying flag is set");

                ValidateConnectedSplines(
                    cache.SegmentList,
                    cache.SplineSet
                );

                processingDirtyControlPoints = true;

                bool processingSucceeded = true;
                try
                {
                    if (spline.ControlPointCount != 0)
                    {
                        List<CurvySplineSegment> dirtyCpsExtendedList;
                        {
                            dirtyCpsExtendedList = cache.SegmentList;
                            FillDirtyCpsExtendedList(
                                dirtyCpsExtendedList,
                                cache.SegmentSet
                            );
                        }

                        spline.PrepareThreadCompatibleData();

                        //OPTIM: the current implementation will refresh all dirty CP's orientations, even if one of them needed it, and the others needed only position related refresh. This is because the dirtinessManager.DirtyCurve and dirtinessManager.DirtyOrientation are spline wide, and not per CP. This can be improved
                        //OPTIM: make all the per CP work threadable, and multi thread everything
                        if (DirtyCurve)
                            ProcessDirtyCurve(dirtyCpsExtendedList);

                        if (DirtyOrientation)
                            ProcessDirtyOrientation(dirtyCpsExtendedList);
                    }
                }
                catch (Exception exception)
                {
                    DTLog.LogException(
                        exception,
                        spline
                    );
                    processingSucceeded = false;
                }
                finally
                {
                    processingDirtyControlPoints = false;
                    DirtyCurve = false;
                    DirtyOrientation = false;
                    AllControlPointsAreDirty = false;
                    dirtyControlPointsMinimalSet.Clear();
                }

                return processingSucceeded;
            }

            public void Reset()
            {
                DirtyCurve = true;
                DirtyOrientation = true;
                dirtyControlPointsMinimalSet.Clear();
                AllControlPointsAreDirty = true;
                processingDirtyControlPoints = false;
            }

            public void Dispose() =>
                threadWorker.Dispose();


            private void ProcessDirtyOrientation(
                [NotNull] List<CurvySplineSegment> dirtyCpsExtendedList)
            {
                if (dirtyCpsExtendedList.Count == 0)
                    throw new InvalidOperationException("[Curvy] No dirty control points to process");

                switch (spline.Orientation)
                {
                    case CurvyOrientation.None:
                        //No threading here since the operation is too quick to have any benefice in multithreading it
                        for (int i = 0; i < dirtyCpsExtendedList.Count; i++)
                            dirtyCpsExtendedList[i].refreshOrientationNoneINTERNAL();
                        break;
                    case CurvyOrientation.Static:
                        if (spline.UseThreading && Environment.IsThreadingSupported)
                            threadWorker.ParallelFor(
                                refreshOrientationStaticAction,
                                dirtyCpsExtendedList
                            );
                        else
                            for (int i = 0; i < dirtyCpsExtendedList.Count; i++)
                                dirtyCpsExtendedList[i].refreshOrientationStaticINTERNAL();
                        break;
                    case CurvyOrientation.Dynamic:
                        ProcessDirtyDynamicOrientation(dirtyCpsExtendedList);
                        break;
                    default:
                        DTLog.LogError(
                            "[Curvy] Invalid Orientation value " + spline.Orientation,
                            spline
                        );
                        break;
                }


                if (!spline.Closed && spline.Count > 0)
                {
                    // Handle very last CP's Up
                    CurvySplineSegment beforeLastVisibleCp = spline.GetPreviousControlPoint(spline.LastVisibleControlPoint);
                    spline.LastVisibleControlPoint.UpsApproximation.Array[0] =
                        beforeLastVisibleCp.UpsApproximation.Array[beforeLastVisibleCp.CacheSize];
                }
            }

            private void ProcessDirtyDynamicOrientation(
                [NotNull] List<CurvySplineSegment> dirtyCpsExtendedList)
            {
#if CURVY_SANITY_CHECKS
                if (spline.relationshipCache.IsValid == false)
                    throw new InvalidOperationException("Control points relationship cache is not valid");
#endif

                short[] orientationAnchorIndices = spline.GetOrientationAnchorIndices();

                int dead = spline.ControlPointCount + 1;
                do
                {
                    CurvySplineSegment dirtyCP = dirtyCpsExtendedList[0];
                    if (spline.IsControlPointASegment(dirtyCP) == false)
                    {
                        dirtyCP.refreshOrientationDynamicINTERNAL(dirtyCP.getOrthoUp0INTERNAL());
                        dirtyCpsExtendedList.RemoveAt(0);
                    }
                    else
                    {
                        OrientationGroup orientationGroup = cache.OrientationGroup;

                        orientationGroup.SetupOrientationGroup(
                            orientationAnchorIndices[dirtyCP.GetExtrinsicPropertiesINTERNAL().ControlPointIndex],
                            dirtyCP.Spline.ControlPoints,
                            orientationAnchorIndices
                        );

                        orientationGroup.UpdateOrientation();

                        //TODO optim: the removal of cps from dirtyCpsExtendedList is a bit expensive. I am sure there are ways to avoid doing so. For examples, have an array of bools, each bool representing the dirtiness of a cp. That way, no look up needed in dirtyCpsExtendedList
                        for (int i = 0; i < orientationGroup.Segments.Count; i++)
                            dirtyCpsExtendedList.Remove(orientationGroup.Segments[i]);
                    }

#if CURVY_SANITY_CHECKS
                    Assert.IsFalse(dirtyCpsExtendedList.Contains(dirtyCP));
#endif
                } while (dirtyCpsExtendedList.Count > 0 && dead-- > 0);

                if (dead <= 0)
                    DTLog.LogWarning(
                        "[Curvy] Deadloop in CurvySpline.Refresh! Please raise a bugreport!",
                        spline
                    );
            }

            private void ProcessDirtyCurve(
                [NotNull] List<CurvySplineSegment> dirtyCpsExtendedList)
            {
                if (dirtyCpsExtendedList.Count == 0)
                    throw new InvalidOperationException("[Curvy] No dirty control points to process");

                // Update Bezier Handles
                if (spline.Interpolation == CurvyInterpolation.Bezier)
                    for (int i = 0; i < dirtyCpsExtendedList.Count; i++)
                    {
                        CurvySplineSegment dirtyControlPoint = dirtyCpsExtendedList[i];
                        if (dirtyControlPoint.AutoHandles)
                            dirtyControlPoint.SetBezierHandles(
                                -1f,
                                true,
                                true,
                                true
                            );
                    }

                // Iterate through all changed for threadable tasks (cache Approximation, ApproximationT, ApproximationDistance)
                if (spline.UseThreading && Environment.IsThreadingSupported)
                    threadWorker.ParallelFor(
                        spline.refreshCurveAction,
                        dirtyCpsExtendedList
                    );
                else
                    for (int i = 0; i < dirtyCpsExtendedList.Count; i++)
                        dirtyCpsExtendedList[i].refreshCurveINTERNAL();

                // Iterate through all ControlPoints for some basic actions
                if (spline.ControlPointCount > 0)
                {
                    spline.UpdateControlPointDistances();
                    if (spline.TangentCachingMode == CalculationMode.Fast)
                        spline.EnforceTangentContinuity();
                }
            }

            //todo design: subdivide
            private void SetDirtyingFlagsAndInvalidateSplineCurveCachesIfNeeded(
                SplineDirtyingType dirtyingType)
            {
                DoSanityChecks();

                DirtyCurve = DirtyCurve || dirtyingType == SplineDirtyingType.Everything;
                DirtyOrientation = true;

                if (DirtyCurve)
                    spline.InvalidateAccumulators();
            }

            //todo design: subdivide
            private void AddToMinimalSetAndSetDirtyingFlagsAndInvalidateSplineCurveCachesIfNeeded(
                CurvySplineSegment controlPoint,
                SplineDirtyingType dirtyingType)
            {
                DoSanityChecks();

                dirtyControlPointsMinimalSet.Add(controlPoint);
                SetDirtyingFlagsAndInvalidateSplineCurveCachesIfNeeded(dirtyingType);
            }

            #region Filling dirty CPs extended list

            /// <summary>
            /// Fills dirtyCpsExtendedList from dirtyControlPointsMinimalSet
            /// </summary>
            /// <param name="dirtyCpsExtendedList"> The list that will be filled with the extended list of dirty control points</param>
            /// <param name="cachedReusableCPsSet"> A set given to avoid allocating a new one. Its content is irrelevant. There are no guarantees on the content of the list after returning from this method.</param>
            private void FillDirtyCpsExtendedList(
                [NotNull] List<CurvySplineSegment> dirtyCpsExtendedList,
                [NotNull] HashSet<CurvySplineSegment> cachedReusableCPsSet)
            {
                dirtyCpsExtendedList.Clear();

                if (AllControlPointsAreDirty)
                    dirtyCpsExtendedList.AddRange(spline.ControlPoints);
                else
                {
                    //OPTIM use cps indexes in dirtyControlPointsMinimalSet instead of cps references, will reduce the time passed in getHash and ==
                    //We expend dirtyControlPointsMinimalSet to include the extended list of dirty control points

                    cachedReusableCPsSet.Clear();

                    switch (spline.Interpolation)
                    {
                        case CurvyInterpolation.Linear:
                        case CurvyInterpolation.CatmullRom:
                        case CurvyInterpolation.TCB:
                        case CurvyInterpolation.Bezier:
                            MinimalSetToExtendedSet_NonBSpline(
                                spline,
                                dirtyControlPointsMinimalSet,
                                cachedReusableCPsSet
                            );
                            break;
                        case CurvyInterpolation.BSpline:
                            MinimalSetToExtendedList_BSpline(
                                spline,
                                dirtyControlPointsMinimalSet,
                                cachedReusableCPsSet
                            );
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }

                    dirtyCpsExtendedList.AddRange(cachedReusableCPsSet);

#if CURVY_SANITY_CHECKS
                    Assert.IsTrue(spline.relationshipCache.IsValid);
#endif
                }
            }

            private static void MinimalSetToExtendedSet_NonBSpline(
                [NotNull] CurvySpline spline,
                [NotNull] HashSet<CurvySplineSegment> dirtyCPsMinimalSet,
                [NotNull] HashSet<CurvySplineSegment> dirtyCPsExtendedSet)
            {
                foreach (CurvySplineSegment dirtyCp in dirtyCPsMinimalSet)
                {
                    dirtyCPsExtendedSet.Add(dirtyCp);

                    CurvySplineSegment previousCp = spline.GetPreviousControlPoint(dirtyCp);
                    if (previousCp)
                        dirtyCPsExtendedSet.Add(previousCp);

                    if (spline.Interpolation == CurvyInterpolation.CatmullRom || spline.Interpolation == CurvyInterpolation.TCB || spline.Interpolation == CurvyInterpolation.Bezier)
                    {
                        //Add other segments to reflect the effect of Bezier handles (and Auto Handles) and Catmull-Rom and TCB's extended influence of CPs.
                        //OPTIM in the bezier case, always including this extended set of CPs is overkill, but at least it avoids bugs and the complicated dirtying logic associated with the Bezier handles handling code.
                        if (previousCp)
                        {
                            //OPTIM you can get dirtyCp's index, then use GetPreviousControlPointIndex to get previousCp and previousPreviousCp
                            CurvySplineSegment previousPreviousCp = spline.GetPreviousControlPoint(previousCp);
                            if (previousPreviousCp)
                                dirtyCPsExtendedSet.Add(previousPreviousCp);
                        }

                        CurvySplineSegment nextCp = spline.GetNextControlPoint(dirtyCp);
                        if (nextCp)
                            dirtyCPsExtendedSet.Add(nextCp);
                    }
                }
            }

            private static void MinimalSetToExtendedList_BSpline(
                [NotNull] CurvySpline curvySpline,
                [NotNull] HashSet<CurvySplineSegment> dirtyCpsMinimalSet,
                [NotNull] HashSet<CurvySplineSegment> dirtyCPsExtendedSet)
            {
                foreach (CurvySplineSegment dirtyCp in dirtyCpsMinimalSet)
                {
                    int controlPointsCount = curvySpline.ControlPoints.Count;
                    int degree = curvySpline.BSplineDegree;
                    bool closed = curvySpline.Closed;
                    bool isClamped = curvySpline.IsBSplineClamped;
                    int n = BSplineHelper.GetBSplineN(
                        controlPointsCount,
                        degree,
                        closed
                    );
                    int dirtyCpIndex = curvySpline.GetControlPointIndex(dirtyCp);
                    for (int testedCpIndex = 0; testedCpIndex < controlPointsCount; testedCpIndex++)
                    {
                        CurvySplineSegment testedCP = curvySpline.ControlPoints[testedCpIndex];
                        if (dirtyCPsExtendedSet.Contains(testedCP))
                            continue;

                        bool shouldAddCP;
                        {
                            int startK;
                            int endK;

                            BSplineHelper.GetBSplineUAndK(
                                curvySpline.SegmentToTF(testedCP),
                                isClamped,
                                degree,
                                n,
                                out _,
                                out startK
                            );
                            if (dirtyCpIndex >= startK - degree && dirtyCpIndex <= startK)
                                shouldAddCP = true;
                            else
                            {
                                BSplineHelper.GetBSplineUAndK(
                                    curvySpline.SegmentToTF(
                                        testedCP,
                                        1
                                    ),
                                    isClamped,
                                    degree,
                                    n,
                                    out _,
                                    out endK
                                );
                                if (dirtyCpIndex >= endK - degree && dirtyCpIndex <= endK)
                                    shouldAddCP = true;
                                else if (closed)
                                {
                                    int loopedCpIndex = dirtyCpIndex + controlPointsCount;
                                    if (loopedCpIndex >= startK - degree && loopedCpIndex <= startK)
                                        shouldAddCP = true;
                                    else
                                    {
                                        BSplineHelper.GetBSplineUAndK(
                                            curvySpline.SegmentToTF(
                                                testedCP,
                                                1
                                            ),
                                            isClamped,
                                            degree,
                                            n,
                                            out _,
                                            out endK
                                        );
                                        shouldAddCP = loopedCpIndex >= endK - degree && loopedCpIndex <= endK;
                                    }
                                }
                                else
                                    shouldAddCP = false;
                            }
                        }

                        if (shouldAddCP)
                            dirtyCPsExtendedSet.Add(testedCP);
                    }
                }
            }

            #endregion

            #region PreProcessing validations

            /// <summary>
            /// 
            /// </summary>
            /// <param name="cachedSegmentList"> A list given to avoid allocating a new one. Its content is irrelevant. There are no guarantees on the content of the list after returning from this method.</param>
            /// <param name="cacheSplineSet"> A set given to avoid allocating a new one. Its content is irrelevant. There are no guarantees on the content of the list after returning from this method.</param>
            private void ValidateConnectedSplines(
                [NotNull] List<CurvySplineSegment> cachedSegmentList,
                [NotNull] HashSet<CurvySpline> cacheSplineSet)
            {
                //this code is reached when initializing a spline. If spline A is connected to spline B, and A is initialized first, B will not be ready. So to make sure that relevant B data is ready, we synchronize B with its hierarchy if needed. 
                //This use case happened in a resolved bug which ticket I didn't look for. I do remember it was related to deleting cps then undoing them in scene 03_Connections. What I can say for sure is that the synchronization methods bellow do work when deleting at once then undoing: OuterShape.cp0001, Looping.CP0003, InnerShape.CP0002 and 0003.
                List<CurvySplineSegment> connectedCPs = cachedSegmentList;
                FillConnectedCPs(connectedCPs);

                //for performance reasons, keep the order of these two calls, to avoid checking in SynchronizeSplinesWithNullCps splines synchronized (and thus valid and don't need checking) in SynchronizeUninitializedSplines
                SynchronizeSplinesWithNullCps(
                    connectedCPs,
                    cacheSplineSet
                );
                SynchronizeUninitializedSplines(connectedCPs);
            }

            /// <summary>
            /// Fills the given list with the connected control points from other splines
            /// </summary>
            /// <param name="resultList"> Its content is irrelevant. Will cleared and filled with the result.</param>
            /// <returns></returns>
            private void FillConnectedCPs(
                [NotNull] List<CurvySplineSegment> resultList)
            {
                resultList.Clear();
                for (int index = 0; index < spline.ControlPoints.Count; index++)
                {
                    CurvySplineSegment cp = spline.ControlPoints[index];
                    if (cp.Connection == null)
                        continue;

                    for (int i = 0; i < cp.Connection.ControlPointsList.Count; i++)
                    {
                        CurvySplineSegment connectedCp = cp.Connection.ControlPointsList[i];
                        if (connectedCp.Spline != spline)
                            resultList.Add(connectedCp);
                    }
                }
            }

            private void SynchronizeSplinesWithNullCps(
                [NotNull] List<CurvySplineSegment> controlPoints,
                [NotNull] HashSet<CurvySpline> cacheSplineSet)
            {
                HashSet<CurvySpline> splinesWithNullCps = cacheSplineSet;
                splinesWithNullCps.Clear();
                for (int index = 0; index < controlPoints.Count; index++)
                {
                    CurvySplineSegment cp = controlPoints[index];
                    if (cp.Spline == null)
                        continue;

                    splinesWithNullCps.Add(cp.Spline);
                }

                foreach (CurvySpline currentSpline in splinesWithNullCps)
                {
#if CURVY_SANITY_CHECKS
                    Assert.IsTrue(currentSpline != spline);
#endif
                    if (currentSpline.ControlPoints.Exists(cp => cp == null))
                        currentSpline.SyncSplineFromHierarchy();
                }
            }

            private static void SynchronizeUninitializedSplines(
                [NotNull] List<CurvySplineSegment> connectedCPs)
            {
                //Spline field is null. This means either followup has no parent spline, or its parent spline is not initialized. The spline initialization is what assigns the Spline member of the control point
                for (int index = 0; index < connectedCPs.Count; index++)
                {
                    CurvySplineSegment controlPoint = connectedCPs[index];
                    if (controlPoint.Spline != null)
                        continue;

                    CurvySpline parentSpline = controlPoint.transform.parent.GetComponent<CurvySpline>();
                    if (parentSpline != null)
                        parentSpline.SyncSplineFromHierarchy();
                }
            }

            #endregion

            [System.Diagnostics.Conditional(CompilationSymbols.CurvySanityChecks)]
            private void DoSanityChecks()
            {
                if (processingDirtyControlPoints)
                    throw new InvalidOperationException("[Curvy] Dirtying while processing dirty state is not allowed");
            }
        }
    }
}