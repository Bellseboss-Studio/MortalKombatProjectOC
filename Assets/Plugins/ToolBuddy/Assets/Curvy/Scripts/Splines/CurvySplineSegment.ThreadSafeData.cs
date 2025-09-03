// =====================================================================
// Copyright © 2013 ToolBuddy
// All rights reserved
// 
// http://www.toolbuddy.net
// =====================================================================

using UnityEngine;

namespace FluffyUnderware.Curvy
{
    public partial class CurvySplineSegment
    {
        /// <summary>
        /// When multithreading, you can't access Transform in the not main threads. Here we cache that data so it is available for threads
        /// </summary>
        private class ThreadSafeData
        {
            /// <summary>
            /// This exists because Transform can not be accessed in non main threads. So before refreshing the spline, we store the local position here so it can be accessed in multithread spline refreshing code
            /// </summary>
            /// <remarks>Warning: Make sure it is set with valid value before using it</remarks>
            public Vector3 ThreadSafeLocalPosition;

            /// <summary>
            /// Same as <see cref="ThreadSafeLocalPosition"/>, but for the next CP. Is equal to <see cref="ThreadSafeLocalPosition"/> if no next cp. Takes into consideration Follow-Ups if spline uses them to define its shape
            /// </summary>
            public Vector3 ThreadSafeNextCpLocalPosition;

            /// <summary>
            /// Same as <see cref="ThreadSafeLocalPosition"/>, but for the next CP. Is equal to <see cref="ThreadSafeLocalPosition"/> if no previous cp. Takes into consideration Follow-Ups if spline uses them to define its shape
            /// </summary>
            public Vector3 ThreadSafePreviousCpLocalPosition;

            /// <summary>
            /// This exists because Transform can not be accesed in non main threads. So before refreshing the spline, we store the local rotation here so it can be accessed in multithread spline refreshing code
            /// </summary>
            /// <remarks>Warning: Make sure it is set with valid value before using it</remarks>
            public Quaternion ThreadSafeLocalRotation;


            internal void Set(bool useFollowUp, CurvySplineSegment curvySplineSegment, out CurvySplineSegment nextCP)
            {
                CurvySpline spline = curvySplineSegment.Spline;
                Transform cachedTransform = curvySplineSegment.cachedTransform;
                CurvySplineSegment previousCP = spline.GetPreviousControlPoint(curvySplineSegment);
                nextCP = spline.GetNextControlPoint(curvySplineSegment);

                //TODO: get rid of this the day you will be able to access transforms in threads
                ThreadSafeLocalPosition = cachedTransform.localPosition;
                ThreadSafeLocalRotation = cachedTransform.localRotation;


                if (useFollowUp)
                {
                    CurvySplineSegment followUpPreviousCP;
                    bool hasFollowUp = curvySplineSegment.FollowUp != null;
                    if (hasFollowUp
                        && ReferenceEquals(
                            spline.FirstVisibleControlPoint,
                            curvySplineSegment
                        ))
                        followUpPreviousCP = CurvySpline.GetFollowUpHeadingControlPoint(
                            curvySplineSegment.FollowUp,
                            curvySplineSegment.FollowUpHeading
                        );
                    else
                        followUpPreviousCP = previousCP;
                    CurvySplineSegment followUpNextCP;
                    if (hasFollowUp
                        && ReferenceEquals(
                            spline.LastVisibleControlPoint,
                            curvySplineSegment
                        ))
                        followUpNextCP = CurvySpline.GetFollowUpHeadingControlPoint(
                            curvySplineSegment.FollowUp,
                            curvySplineSegment.FollowUpHeading
                        );
                    else
                        followUpNextCP = nextCP;

                    if (followUpPreviousCP != null)
                        ThreadSafePreviousCpLocalPosition = ReferenceEquals(
                            followUpPreviousCP.Spline,
                            spline
                        )
                            ? followUpPreviousCP.cachedTransform.localPosition
                            : spline.transform.InverseTransformPoint(followUpPreviousCP.cachedTransform.position);
                    else
                        ThreadSafePreviousCpLocalPosition = ThreadSafeLocalPosition;

                    if (followUpNextCP != null)
                        ThreadSafeNextCpLocalPosition = ReferenceEquals(
                            followUpNextCP.Spline,
                            spline
                        )
                            ? followUpNextCP.cachedTransform.localPosition
                            : spline.transform.InverseTransformPoint(followUpNextCP.cachedTransform.position);
                    else
                        ThreadSafeNextCpLocalPosition = ThreadSafeLocalPosition;
                }
                else
                {
                    ThreadSafePreviousCpLocalPosition = ReferenceEquals(
                                                            previousCP,
                                                            null
                                                        )
                                                        == false
                        ? previousCP.cachedTransform.localPosition
                        : ThreadSafeLocalPosition;

                    ThreadSafeNextCpLocalPosition = ReferenceEquals(
                                                        nextCP,
                                                        null
                                                    )
                                                    == false
                        ? nextCP.cachedTransform.localPosition
                        : ThreadSafeLocalPosition;
                }
            }
        }
    }
}