// =====================================================================
// Copyright © 2013 ToolBuddy
// All rights reserved
// 
// http://www.toolbuddy.net
// =====================================================================

using FluffyUnderware.DevTools;
using UnityEditor;
using UnityEngine.Assertions;

namespace FluffyUnderware.Curvy
{
    public partial class CurvyConnection
    {
        /// <summary>
        /// This class is responsible for fixing issues that arise when undoing/redoing operations on connections
        /// </summary> 
        private class UndoFixer
        {
            private readonly CurvyConnection curvyConnection;

            public UndoFixer(CurvyConnection curvyConnection)
                => this.curvyConnection = curvyConnection;

            /// <summary>
            /// Fixes various issues due to undoing/redoing. For example, when deleting cps then undoing that operation.
            /// </summary>
            public void FixIssuesIntroducedByUndoing()
            {
                //todo: Try avoid the issue if possible, instead of fixing it afterwards
#if UNITY_EDITOR
                //the following line is not due to undoing, but I fix this here anyway
                curvyConnection.m_ControlPoints.RemoveAll(
                    cp => ReferenceEquals(
                        cp,
                        null
                    )
                );

                FixReferencesToDestroyedControlPoints();
                FixReferencesToDestroyedFollowUps();
                UnsetInvalidFollowUps();
#endif
            }

#if UNITY_EDITOR

            /// <summary>
            /// Fixes issue where deleting cps then undoing that will restore in the control point list not the restaured CPs, but the ones that were destroyed. So we substitute them with the ones that I guess the undo/redo system recreated.
            /// </summary>
            private void FixReferencesToDestroyedControlPoints()
            {
                for (int index = 0; index < curvyConnection.m_ControlPoints.Count; index++)
                {
                    CurvySplineSegment controlPoint = curvyConnection.m_ControlPoints[index];

#if CURVY_SANITY_CHECKS
                    Assert.IsFalse(
                        ReferenceEquals(
                            controlPoint,
                            null
                        )
                    );
#endif
                    if (controlPoint == null)
                    {
                        CurvySplineSegment validControlPoint =
                            (CurvySplineSegment)EditorUtility.InstanceIDToObject(controlPoint.GetInstanceID());
                        if (validControlPoint == null)
                        {
#if CURVY_SANITY_CHECKS
                            DTLog.LogWarning(
                                $"[CURVY] Connection {curvyConnection.name} could not find Control Point of id {controlPoint.GetInstanceID()}"
                            );
#endif
                        }
                        else
                            curvyConnection.m_ControlPoints[index] = validControlPoint;
                    }
                }
            }

            /// <summary>
            /// Same as <seealso cref="FixReferencesToDestroyedControlPoints"/>, but for Follow-Ups
            /// </summary>
            private void FixReferencesToDestroyedFollowUps()
            {
                foreach (CurvySplineSegment controlPoint in curvyConnection.m_ControlPoints)
                    if (controlPoint.FollowUp == null
                        && ReferenceEquals(
                            controlPoint.FollowUp,
                            null
                        )
                        == false)
                    {
                        CurvySplineSegment validFollowUp =
                            (CurvySplineSegment)EditorUtility.InstanceIDToObject(controlPoint.FollowUp.GetInstanceID());

                        if (validFollowUp)
                        {
#if CURVY_SANITY_CHECKS
                            DTLog.LogWarning(
                                $"[CURVY] Connection {curvyConnection.name} could not find Control Point of id {controlPoint.GetInstanceID()}"
                            );
#endif
                        }
                        else
                            controlPoint.SetFollowUp(
                                validFollowUp,
                                controlPoint.FollowUpHeading
                            );
                    }
            }

            /// <summary>
            /// Fixes issue where deleting cps then undoing that will (directly or indirectly) not restore all the control points in this list, which leaves some of the CPs in this connection referencing Follow-Ups that are not in this connection anymore
            /// </summary>
            private void UnsetInvalidFollowUps()
            {
                foreach (CurvySplineSegment cp in curvyConnection.m_ControlPoints)
                {
                    if (cp.FollowUp == null)
                        continue;
                    if (curvyConnection.m_ControlPoints.Contains(cp.FollowUp))
                        continue;

                    cp.SetFollowUp(null);
                }
            }
#endif
        }
    }
}