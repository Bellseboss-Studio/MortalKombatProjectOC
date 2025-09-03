// =====================================================================
// Copyright © 2013 ToolBuddy
// All rights reserved
// 
// http://www.toolbuddy.net
// =====================================================================

using System;
using JetBrains.Annotations;
using UnityEditor;

namespace FluffyUnderware.Curvy
{
    public partial class CurvySpline
    {
        /// <summary>
        /// A cache of relationships between spline and control points. Is here purely for performance reasons. Part of the RelationshipCache is stored outside of this class, in <see cref="CurvySplineSegment.extrinsicProperties"/>.
        /// </summary>
        //todo desing: include CurvySplineSegment.extrinsicProperties in this class?
        private class RelationshipCache
        {
            [NotNull]
            private readonly CurvySpline spline;

            [NotNull]
            private readonly object lockObject = new object();

            [CanBeNull]
            private CurvySplineSegment firstSegment;

            [CanBeNull]
            private CurvySplineSegment lastSegment;

            [CanBeNull]
            private CurvySplineSegment firstVisibleControlPoint;

            [CanBeNull]
            private CurvySplineSegment lastVisibleControlPoint;

            /// <summary>
            /// Gets the first visible Control Point (equals the first segment or this[0])
            /// </summary>
            /// <remarks>Can be null, for example for a Catmull-Rom spline whith only two splines and AutoEndTangent set to false</remarks>
            [CanBeNull]
            public CurvySplineSegment FirstVisibleControlPoint
            {
                get
                {
                    EnsureIsValid();
                    return firstVisibleControlPoint;
                }
            }

            /// <summary>
            /// Gets the last visible Control Point (i.e. the end CP of the last segment)
            /// </summary>
            /// <remarks>Is null if spline has no segments</remarks>
            [CanBeNull]
            public CurvySplineSegment LastVisibleControlPoint
            {
                get
                {
                    EnsureIsValid();
                    return lastVisibleControlPoint;
                }
            }

            /// <summary>
            /// Gets the first segment of the spline
            /// </summary>
            [CanBeNull]
            public CurvySplineSegment FirstSegment
            {
                get
                {
                    EnsureIsValid();
                    return firstSegment;
                }
            }

            /// <summary>
            /// Gets the last segment of the spline
            /// </summary>
            [CanBeNull]
            public CurvySplineSegment LastSegment
            {
                get
                {
                    EnsureIsValid();
                    return lastSegment;
                }
            }

            public bool IsValid { get; private set; }

            //constructor taking a spline
            public RelationshipCache([NotNull] CurvySpline spline) =>
                this.spline = spline;

            public void Invalidate()
            {
                if (!IsValid)
                    return;

                lock (lockObject)
                {
                    IsValid = false;
                    firstSegment = lastSegment = firstVisibleControlPoint = lastVisibleControlPoint = null;
                }
            }

            public void EnsureIsValid()
            {
                if (IsValid)
                    return;

                RebuildAndFixNonCoherentControlPoints();
            }

            //todo design divide in two methods, and get rid of fixNonCoherentControlPoints
            private void RebuildAndFixNonCoherentControlPoints()
            {
                // If true, control points with properties that are no more coherent with their position in the spline will get modified
                bool fixNonCoherentControlPoints = true;

                lock (lockObject)
                {
                    if (IsValid)
                        return;

                    //force clamping of B-spline's degree, in case control points got removed
                    spline.BSplineDegree = spline.bSplineDegree;

                    //TODO Try to do elsewhere the work done here when fixNonCoherentControlPoints, so it is always true, and not only true when Relationship cache is build
                    int controlPointsCount = spline.ControlPoints.Count;
                    spline.mSegments.Clear();
                    spline.mSegments.Capacity = controlPointsCount;
                    if (controlPointsCount > 0)
                    {
                        CurvySplineSegment firsAssignedSegment = null;
                        bool firstSegmentFound = false;
                        CurvySplineSegment lastAssignedSegment = null;

                        CurvySplineSegment.ControlPointExtrinsicProperties previousCpInfo =
                            new CurvySplineSegment.ControlPointExtrinsicProperties(false,
                            -1,
                            -1,
                            -1,
                            -1,
                            -1,
                            false,
                            false,
                            false
                            );

                        bool isSplineClosed = spline.Closed;
                        bool isCatmullRomOrTcb = spline.Interpolation == CurvyInterpolation.CatmullRom
                                                 || spline.Interpolation == CurvyInterpolation.TCB;
                        bool notAutoEndTangentsAndIsCatmullRomOrTcb = spline.AutoEndTangents == false && isCatmullRomOrTcb;
                        bool isBSpline = spline.Interpolation == CurvyInterpolation.BSpline;


                        float tfInverseDenominator;
                        {
                            if (notAutoEndTangentsAndIsCatmullRomOrTcb)
                                tfInverseDenominator = 1f
                                                       / (controlPointsCount > 3
                                                           ? controlPointsCount - 3
                                                           : 1);
                            else if (isSplineClosed)
                                tfInverseDenominator = 1f / controlPointsCount;
                            else
                                tfInverseDenominator = 1f
                                                       / (controlPointsCount > 1
                                                           ? controlPointsCount - 1
                                                           : 1);
                        }

                        short segmentIndex = 0;
                        for (short index = 0; index < controlPointsCount; index++)
                        {
                            CurvySplineSegment controlPoint = spline.ControlPoints[index];

                            short previousControlPointIndex = GetPreviousControlPointIndex(
                                index,
                                isSplineClosed,
                                controlPointsCount
                            );
                            short nextControlPointIndex = GetNextControlPointIndex(
                                index,
                                isSplineClosed,
                                controlPointsCount
                            );

                            bool isSegment = IsControlPointASegment(
                                index,
                                controlPointsCount,
                                isSplineClosed,
                                notAutoEndTangentsAndIsCatmullRomOrTcb,
                                isBSpline,
                                spline.bSplineDegree
                            );
                            bool isVisible = isSegment || previousCpInfo.IsSegment;

                            bool canHaveFollowUp = isVisible && (nextControlPointIndex == -1 || previousControlPointIndex == -1);

                            float tf;
                            {
                                if (notAutoEndTangentsAndIsCatmullRomOrTcb)
                                    tf = tfInverseDenominator
                                         * (index == 0
                                             ? 0
                                             : index == controlPointsCount - 1
                                                 ? Math.Max(
                                                     0,
                                                     index - 2
                                                 )
                                                 : index - 1);
                                else
                                    tf = tfInverseDenominator * index;
                            }

                            previousCpInfo = new CurvySplineSegment.ControlPointExtrinsicProperties(
                                isVisible,
                                tf,
                                isSegment
                                    ? segmentIndex
                                    : (short)-1,
                                index,
                                previousControlPointIndex,
                                nextControlPointIndex,
                                previousControlPointIndex != -1
                                && IsControlPointASegment(
                                    previousControlPointIndex,
                                    controlPointsCount,
                                    isSplineClosed,
                                    notAutoEndTangentsAndIsCatmullRomOrTcb,
                                    isBSpline,
                                    spline.bSplineDegree
                                ),
                                nextControlPointIndex != -1
                                && IsControlPointASegment(
                                    nextControlPointIndex,
                                    controlPointsCount,
                                    isSplineClosed,
                                    notAutoEndTangentsAndIsCatmullRomOrTcb,
                                    isBSpline,
                                    spline.bSplineDegree
                                ),
                                canHaveFollowUp
                            );
                            controlPoint.SetExtrinsicPropertiesINTERNAL(previousCpInfo);

                            if (isSegment)
                            {
                                spline.mSegments.Add(controlPoint);
                                segmentIndex++;
                                if (firstSegmentFound == false)
                                {
                                    firstSegmentFound = true;
                                    firsAssignedSegment = controlPoint;
                                }

                                lastAssignedSegment = controlPoint;
                            }

                            if (fixNonCoherentControlPoints && canHaveFollowUp == false)
                                controlPoint.UnsetFollowUpWithoutDirtyingINTERNAL();
                        }

                        firstSegment = firsAssignedSegment;
                        lastSegment = lastAssignedSegment;
                        firstVisibleControlPoint = firstSegment;
                        lastVisibleControlPoint = ReferenceEquals(
                                                      lastSegment,
                                                      null
                                                  )
                                                  == false
                            ? spline.ControlPoints[lastSegment.GetExtrinsicPropertiesINTERNAL().NextControlPointIndex]
                            : null;
                    }
                    else
                        firstSegment = lastSegment = firstVisibleControlPoint = lastVisibleControlPoint = null;

                    IsValid = true;
                }

#if UNITY_EDITOR
                //trigger update of Orientation Anchor icons in the hierarchy. See CurvyProject.OnHierarchyWindowItemOnGUI
                EditorApplication.RepaintHierarchyWindow();
#endif
            }
        }
    }
}