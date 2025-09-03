// =====================================================================
// Copyright © 2013 ToolBuddy
// All rights reserved
// 
// http://www.toolbuddy.net
// =====================================================================

using System;
using System.Collections.Generic;
using System.Linq;
using FluffyUnderware.DevTools;
using FluffyUnderware.DevTools.Extensions;
using JetBrains.Annotations;
using UnityEngine;

namespace FluffyUnderware.Curvy
{
    public partial class CurvyConnection
    {
        private class TransformSynchronizer
        {
            [NotNull]
            private readonly CurvyConnection connection;

            [CanBeNull]
            private TransformMonitor connectionMonitor;

            [NotNull]
            private TransformMonitor ConnectionMonitor
            {
                get
                {
                    if (connectionMonitor == null)
                        connectionMonitor = new TransformMonitor(
                            connection.transform,
                            true,
                            true,
                            false
                        );
                    return connectionMonitor;
                }
            }


            //todo use TransformMonitor instances to monitor the CPs, instead of recoding a monitor in IsCPTriggeringTransformChange?
            //pros: get rid of code in IsCPTriggeringTransformChange + code easier to understand since connectionMonitor uses TransformMonitor too.
            //cons: You will have to handle cases where cps' TransformMonitors will have to update their monitoring options when for example the value of a controlPoint.ConnectionSyncPosition changes
            /// <summary>
            /// The coordinates of the connection's control points the last time synchronization was processed
            /// </summary>
            [NotNull]
            private readonly Dictionary<CurvySplineSegment, (Vector3, Quaternion)> monitoredCPCoordinated =
                new Dictionary<CurvySplineSegment, (Vector3, Quaternion)>();

            public TransformSynchronizer([NotNull] CurvyConnection connection) =>
                this.connection = connection;

            #region Updating

            public void OnControlPointsUpdated() => ResetCPsMonitoring();

            public void OnUpdate()
            {
                EnsureCPsMonitorIsValid();

                GetMonitorChanges(
                    out Vector3? positionChange,
                    out Quaternion? rotationChange
                );

                if (positionChange != null || rotationChange != null)
                    ApplyTransform(
                        positionChange ?? connection.transform.position,
                        rotationChange ?? connection.transform.rotation
                    );
            }

            private void EnsureCPsMonitorIsValid()
            {
                if (!IsCPsMonitorValid)
                    //happens if you delete a connection then undo
                    ResetCPsMonitoring();
            }

            private bool IsCPsMonitorValid
            {
                get
                {
                    if (monitoredCPCoordinated.Count != connection.Count)
                        return false;
                    return connection.ControlPointsList.All(controlPoint => monitoredCPCoordinated.ContainsKey(controlPoint));
                }
            }

            #endregion

            #region Getting changes

            private void GetMonitorChanges(out Vector3? positionChange, out Quaternion? rotationChange)
            {
                bool gotMonitorChanges = GetConnectionMonitorChanges(
                    out positionChange,
                    out rotationChange
                );
                if (gotMonitorChanges == false)
                    GetCPsMonitorChanges(
                        out positionChange,
                        out rotationChange
                    );
            }

            private bool GetConnectionMonitorChanges(out Vector3? positionChange, out Quaternion? rotationChange)
            {
                bool monitorChanged = ConnectionMonitor.CheckForChanges();

                if (monitorChanged)
                {
                    positionChange = connection.transform.position;
                    rotationChange = connection.transform.rotation;
                }
                else
                {
                    positionChange = null;
                    rotationChange = null;
                }

                return monitorChanged;
            }

            private void GetCPsMonitorChanges(out Vector3? position, out Quaternion? rotation)
            {
                position = null;
                rotation = null;

                bool positionWasChanged = false;
                bool rotationWasChanged = false;

                foreach (CurvySplineSegment controlPoint in connection.ControlPointsList)
                {
                    if (controlPoint.gameObject == null)
                    {
                        //The only case I am aware of where this happens is when running test (see [TestFixture]), when the test is finished, a connection is duplicated and "restored", while having in its CPs list CPs that have been destroyed.
                        //This is somehow related to the following statement in the RemoveControlPoint method:
                        //Undo.RegisterCompleteObjectUndo(new UnityEngine.Object[]{
                        //    controlPoint, this
                        //}, undoingStepLabel);

                        //If you fix the problem above, remove unnecessary checks on controlPoint.gameObject. Look for the following comment to find such places:
                        // "see comment in CurvyConnection.DoUpdate to know more about when cp.gameObject can be null"

                        DTLog.LogError(
                            $"[Curvy] Connection named '{connection.name}' had in its list a control point with no game object. Control point was ignored",
                            connection
                        );
                        continue;
                    }

                    GetCPMonitorChanges(
                        controlPoint,
                        out Vector3? positionChange,
                        out Quaternion? rotationChange
                    );

                    if (positionChange.HasValue)
                    {
                        positionWasChanged = true;
                        position = positionChange;
                    }

                    if (rotationChange.HasValue)
                    {
                        rotationWasChanged = true;
                        rotation = rotationChange;
                    }

                    if (positionWasChanged && rotationWasChanged)
                        break;
                }
            }

            private void GetCPMonitorChanges([NotNull] CurvySplineSegment controlPoint, out Vector3? position,
                out Quaternion? rotation)
            {
                IsCPTriggeringTransformChange(
                    controlPoint,
                    out bool syncPosition,
                    out bool syncRotation
                );
                position = syncPosition
                    ? controlPoint.transform.position
                    : (Vector3?)null;
                rotation = syncRotation
                    ? controlPoint.transform.rotation
                    : (Quaternion?)null;
            }

            private void IsCPTriggeringTransformChange([NotNull] CurvySplineSegment controlPoint, out bool syncPosition,
                out bool syncRotation)
            {
                if (controlPoint.ConnectionSyncPosition == false && controlPoint.ConnectionSyncRotation == false)
                {
                    syncPosition = false;
                    syncRotation = false;
                    return;
                }

                (Vector3, Quaternion) processedControlPointTransform = monitoredCPCoordinated[controlPoint];
                Transform controlPointTransform = controlPoint.transform;

                syncPosition = controlPoint.ConnectionSyncPosition
                               && controlPointTransform.position.NotApproximately(processedControlPointTransform.Item1);
                syncRotation = controlPoint.ConnectionSyncRotation
                               && controlPointTransform.rotation.DifferentOrientation(processedControlPointTransform.Item2);
            }

            #endregion

            #region Applying changes

            public void ApplyTransform(Vector3 position, Quaternion rotation)
            {
                ApplyTransformToConnection(
                    position,
                    rotation
                );
                ApplyTransformToCPs(
                    position,
                    rotation
                );
                ResetMonitoring();
            }

            private void ApplyTransformToConnection(Vector3 position, Quaternion rotation)
            {
                Transform cachedTransform = connection.transform;
                cachedTransform.position = position;
                cachedTransform.rotation = rotation;
            }

            private void ApplyTransformToCPs(Vector3 referencePosition, Quaternion referenceRotation)
            {
                for (int i = 0; i < connection.Count; i++)
                {
                    CurvySplineSegment controlPoint = connection.ControlPointsList[i];

                    bool positionModified = controlPoint.ConnectionSyncPosition
                                            && controlPoint.transform.position.NotApproximately(referencePosition);
                    bool rotationModified = controlPoint.ConnectionSyncRotation
                                            && controlPoint.transform.rotation.DifferentOrientation(referenceRotation);

                    if (positionModified)
                        controlPoint.transform.position = referencePosition;
                    if (rotationModified)
                        controlPoint.transform.rotation = referenceRotation;

                    if (!positionModified && (!rotationModified || !controlPoint.OrientationInfluencesSpline))
                        continue;

                    if (controlPoint.Spline == null)
                        throw new InvalidOperationException(
                            $"[Curvy] Control point named '{controlPoint.name}' has no spline. Please raise a bug report"
                        );

                    controlPoint.Spline.SetDirtyPartial(
                        controlPoint,
                        positionModified == false
                            ? SplineDirtyingType.OrientationOnly
                            : SplineDirtyingType.Everything
                    );
                }
            }

            #endregion

            #region Resetting monitoring

            public void ResetMonitoring()
            {
                ResetConnectionMonitoring();
                ResetCPsMonitoring();
            }

            [UsedImplicitly]
            private void ResetConnectionMonitoring() => ConnectionMonitor.ResetMonitoring();

            [UsedImplicitly]
            private void ResetCPsMonitoring()
            {
                monitoredCPCoordinated.Clear();
                foreach (CurvySplineSegment controlPoint in connection.ControlPointsList)
                {
#if CURVY_SANITY_CHECKS
                    if (monitoredCPCoordinated.ContainsKey(controlPoint))
                        DTLog.LogError(
                            $"[Curvy] Connection named '{connection.name}' had in its list a control point that was already in the list. Control point was ignored",
                            connection
                        );
#endif
                    monitoredCPCoordinated[controlPoint] = (controlPoint.transform.position, controlPoint.transform.rotation);
                }
            }

            #endregion
        }
    }
}