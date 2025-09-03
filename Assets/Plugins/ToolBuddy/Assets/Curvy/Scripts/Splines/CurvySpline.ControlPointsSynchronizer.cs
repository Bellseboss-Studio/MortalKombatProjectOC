// =====================================================================
// Copyright © 2013 ToolBuddy
// All rights reserved
// 
// http://www.toolbuddy.net
// =====================================================================

using System;
using System.Diagnostics;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Assertions;
using Debug = UnityEngine.Debug;

namespace FluffyUnderware.Curvy
{
    public partial class CurvySpline
    {
        /// <summary>
        /// Synchronizes the control points between the spline's ControlPoints list and the gameobjects hierarchy
        /// </summary>
        private class ControlPointsSynchronizer
        {
            [NotNull]
            private readonly CurvySpline spline;

            private bool processing;
            public SynchronizationRequest CurrentRequest { get; private set; } = SynchronizationRequest.None;

            public ControlPointsSynchronizer([NotNull] CurvySpline spline) =>
                this.spline = spline;

            [Conditional(CompilationSymbols.UnityEditor)]
            public void RequestSplineToHierarchy()
            {
                DebugLog("request spline -> hierarchy" + " " + spline.name);

                if (processing)
                {
                    LogIgnoredRequest();
                    return;
                }

                if (CurrentRequest == SynchronizationRequest.HierarchyToSpline)
                {
                    LogIgnoredRequest();
                    return;
                }

                CurrentRequest = SynchronizationRequest.SplineToHierarchy;
            }


            public void RequestHierarchyToSpline()
            {
                DebugLog("request hierarchy -> spline" + " " + spline.name);

                if (processing)
                {
                    LogIgnoredRequest();
                    return;
                }

                if (CurrentRequest == SynchronizationRequest.SplineToHierarchy)
                {
                    LogIgnoredRequest();
                    return;
                }

                CurrentRequest = SynchronizationRequest.HierarchyToSpline;
            }

            public void ProcessRequests()
            {
                AssertIsNotProcessing();

                processing = true;
                try
                {
                    switch (CurrentRequest)
                    {
                        case SynchronizationRequest.None:
                            break;
                        case SynchronizationRequest.SplineToHierarchy:
                        {
                            DebugLog("process spline -> hierarchy" + " " + spline.name);
                            SynchronizeSplineToHierarchy();
                        }
                            break;
                        case SynchronizationRequest.HierarchyToSpline:
                        {
                            DebugLog("process hierarchy -> spline" + " " + spline.name);
                            SynchronizeHierarchyToSpline();
                        }
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }
                finally
                {
                    processing = false;
                }

                CurrentRequest = SynchronizationRequest.None;
            }


            public void CancelRequests()
            {
                AssertIsNotProcessing();
                CurrentRequest = SynchronizationRequest.None;
            }

            private void SynchronizeHierarchyToSpline()
            {
                spline.ClearControlPoints(
                    true,
                    false
                );
                Transform splineTransform = spline.transform;
                for (int i = 0; i < splineTransform.childCount; i++)
                {
                    CurvySplineSegment curvySplineSegment = splineTransform.GetChild(i).GetComponent<CurvySplineSegment>();
                    if (curvySplineSegment == null)
                        continue;

                    spline.AddControlPoint(
                        curvySplineSegment,
                        false,
                        false
                    );
                }
            }

            /// <summary>
            /// Rebuilds the hierarchy from the ControlPoints list
            /// </summary>
            [Conditional(CompilationSymbols.UnityEditor)]
            private void SynchronizeSplineToHierarchy()
            {
                for (short i = 0; i < spline.ControlPoints.Count; i++)
                {
                    CurvySplineSegment cp = spline.ControlPoints[i];

                    //cp was null in the following case:
                    //In edit mode, using the pen tool, added a new spline with a cp (CTRL + Left click on empty spot), then added a connected spline (CTRL + Right click on empty spot), and then hit ctrl+Z, which undone the creation of the connected spline, and in the next update this code is called with ControlPoints containing destroyed CPs 
                    if (cp)
                        cp.transform.SetSiblingIndex(i);
                }
            }

            [Conditional(CompilationSymbols.CurvyDebug)]
            private static void DebugLog(string message) =>
                Debug.Log(message);

            #region Checks

            [Conditional(CompilationSymbols.CurvySanityChecks)]
            private static void LogIgnoredRequest() =>
                Debug.LogWarning("Ignored request while processing");

            [Conditional(CompilationSymbols.CurvySanityChecks)]
            private void AssertIsNotProcessing() =>
                Assert.IsFalse(processing);

            #endregion

            public enum SynchronizationRequest
            {
                None,
                SplineToHierarchy,
                HierarchyToSpline
            }
        }
    }
}