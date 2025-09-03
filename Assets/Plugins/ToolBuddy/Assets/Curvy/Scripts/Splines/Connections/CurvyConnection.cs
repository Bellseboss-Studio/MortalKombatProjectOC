// =====================================================================
// Copyright © 2013 ToolBuddy
// All rights reserved
// 
// http://www.toolbuddy.net
// =====================================================================

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using FluffyUnderware.DevTools;
using FluffyUnderware.DevTools.Extensions;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Experimental.SceneManagement;
using UnityEditor.SceneManagement;
#endif

namespace FluffyUnderware.Curvy
{
    /// <summary>
    /// Connection component
    /// </summary>
    [ExecuteInEditMode]
    [HelpURL(AssetInformation.DocsRedirectionBaseUrl + "curvyconnection")]
    public partial class CurvyConnection : DTVersionedMonoBehaviour, ISerializationCallbackReceiver
    {
        public CurvyConnection()
        {
            transformSynchronizer = new TransformSynchronizer(this);
            undoFixer = new UndoFixer(this);
        }

        #region ### Public Properties ###

        /// <summary>
        /// The list of connected control points
        /// </summary>
        public ReadOnlyCollection<CurvySplineSegment> ControlPointsList
        {
            //TODO apply the same TODOs than CurvySpline.ControlPointsList
            get
            {
                if (readOnlyControlPoints == null)
                    readOnlyControlPoints = m_ControlPoints.AsReadOnly();
                return readOnlyControlPoints;
            }
        }

        /// <summary>
        /// Gets the number of Control Points being part of this connection
        /// </summary>
        public int Count => m_ControlPoints.Count;

        /// <summary>
        /// Gets a certain Control Point by index
        /// </summary>
        /// <param name="idx">index of the Control Point</param>
        /// <returns>a Control Point</returns>
        public CurvySplineSegment this[int idx] => m_ControlPoints[idx];

        #endregion

        #region ### Unity Callbacks ###

#if DOCUMENTATION___FORCE_IGNORE___UNITY == false


        protected override void OnEnable()
        {
            base.OnEnable();
            SceneManager.sceneLoaded += OnSceneLoaded;

            transformSynchronizer.ResetMonitoring();
#if UNITY_EDITOR
            EditorApplication.update += EditorUpdate;
            Undo.undoRedoPerformed += undoFixer.FixIssuesIntroducedByUndoing;
#endif
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            SceneManager.sceneLoaded -= OnSceneLoaded;
#if UNITY_EDITOR
            EditorApplication.update -= EditorUpdate;
            Undo.undoRedoPerformed -= undoFixer.FixIssuesIntroducedByUndoing;
#endif
        }

#if UNITY_EDITOR
        private void EditorUpdate()
        {
            if (IsActiveAndEnabled == false)
            {
                //todo bug? For some reason I can't explain,EditorUpdate is called while transform is unity null. OnDisable stops listening on this method, but it is called anyways. Maybe this is due to the connection being destroyed and a new one created in the same frame. This happens when synching a Curvy Spline with a Unity spline container having links
                return;
            }

            DoUpdate();
        }
#endif
        [UsedImplicitly]
        private void Update()
        {
            if (Application.isPlaying)
                DoUpdate();
        }

        [UsedImplicitly]
        private void LateUpdate()
        {
            if (Application.isPlaying)
                DoUpdate();
        }

        [UsedImplicitly]
        private void FixedUpdate()
        {
            if (Application.isPlaying)
                DoUpdate();
        }

        [UsedImplicitly]
        private void OnDestroy()
        {
#if UNITY_EDITOR
            const string undoingStepLabel = "Destroy Connection";
            Undo.RecordObject(
                this,
                undoingStepLabel
            );
#endif
            foreach (CurvySplineSegment cp in m_ControlPoints)
            {
#if UNITY_EDITOR
                Undo.RecordObject(
                    cp,
                    undoingStepLabel
                );
#endif

                cp.Connection = null;
                cp.Disconnect(false);
            }

            m_ControlPoints.Clear();
            transformSynchronizer.OnControlPointsUpdated();
        }


#endif

        #endregion

        #region ### Public Methods ###

        /// <summary>
        /// Creates a connection and adds Control Points
        /// </summary>
        /// <param name="controlPoints">Control Points to add</param>
        /// <returns>the new connection</returns>
        public static CurvyConnection Create(
            [NotNull] [ItemNotNull] params CurvySplineSegment[] controlPoints)
        {
            if (controlPoints == null)
                throw new ArgumentNullException(nameof(controlPoints));

#if UNITY_EDITOR

            bool isInPrefabStage = PrefabStageUtility.GetCurrentPrefabStage() != null;
            if (isInPrefabStage)
            {
                DTLog.LogError(
                    "[Curvy] Connections can not be created in Prefab mode. Please delay their creation to after exiting Prefab mode."
                );
                return null;
            }
#endif

            CurvyGlobalManager curvyGlobalManager = CurvyGlobalManager.Instance;
            if (curvyGlobalManager == null)
            {
                DTLog.LogError("[Curvy] Couldn't find Curvy Global Manager. Please raise a bug report.");
                return null;
            }

            const string undoOperationName = "Add Connection";

            GameObject gameObject = new GameObject("Connection");
#if UNITY_EDITOR
            Undo.RegisterCreatedObjectUndo(
                gameObject,
                undoOperationName
            );
#endif
            gameObject.transform.UndoableSetParent(
                curvyGlobalManager.transform,
                true,
                undoOperationName
            );

            CurvyConnection connection = gameObject.UndoableAddComponent<CurvyConnection>();
            if (connection == null)
                return null;
            if (controlPoints.Length == 0)
                return connection;

            connection.transform.position = controlPoints[0].transform.position;
            connection.AddControlPoints(controlPoints);

            return connection;
        }

        /// <summary>
        /// Adds Control Points to this connection
        /// </summary>
        /// <param name="controlPoints">the Control Points to add</param>
        public void AddControlPoints(params CurvySplineSegment[] controlPoints)
        {
            foreach (CurvySplineSegment cp in controlPoints)
            {
                if (cp.Connection)
                {
                    DTLog.LogErrorFormat(
                        this,
                        "[Curvy] CurvyConnection.AddControlPoints called on a control point '{0}' that has already a connection. Only control points with no connection can be added.",
                        cp
                    );
                    continue;
                }

#if UNITY_EDITOR
                Undo.RecordObject(
                    cp,
                    "Add Connection"
                );
#endif
#if CURVY_SANITY_CHECKS
                Assert.IsFalse(m_ControlPoints.Contains(cp));
#endif
                m_ControlPoints.Add(cp);
                cp.Connection = this;
            }

            transformSynchronizer.OnControlPointsUpdated();
            AutoSetFollowUp();
        }

        /// <summary>
        /// Sets the <see cref="CurvySplineSegment.ConnectionSyncPosition"/> and <see cref="CurvySplineSegment.ConnectionSyncRotation"/> options for all the connected control points
        /// </summary>
        public void SetSynchronizationOptions(
            bool syncPosition,
            bool syncRotation)
        {
            for (int index = 0; index < ControlPointsList.Count; index++)
            {
                CurvySplineSegment controlPoint = ControlPointsList[index];
                controlPoint.ConnectionSyncPosition = syncPosition;
                controlPoint.ConnectionSyncRotation = syncRotation;
            }
        }

        public void AutoSetFollowUp()
        {
            if (Count == 2)
            {
                CurvySplineSegment firstControlPoint = m_ControlPoints[0];
                CurvySplineSegment secondControlPoint = m_ControlPoints[1];
                if (firstControlPoint.transform.position == secondControlPoint.transform.position
                    && firstControlPoint.ConnectionSyncPosition
                    && secondControlPoint.ConnectionSyncPosition)
                {
                    if (firstControlPoint.FollowUp == null
                        && firstControlPoint.Spline
                        && firstControlPoint.Spline.CanControlPointHaveFollowUp(firstControlPoint))
                        firstControlPoint.SetFollowUp(secondControlPoint);
                    if (secondControlPoint.FollowUp == null
                        && secondControlPoint.Spline
                        && secondControlPoint.Spline.CanControlPointHaveFollowUp(secondControlPoint))
                        secondControlPoint.SetFollowUp(firstControlPoint);
                }
            }
        }

        /// <summary>
        /// Removes a Control Point from this connection
        /// </summary>
        /// <param name="controlPoint">the Control Point to remove</param>
        /// <param name="destroySelfIfEmpty">whether the connection should be destroyed when empty afterwards</param>
        public void RemoveControlPoint(CurvySplineSegment controlPoint, bool destroySelfIfEmpty = true)
        {
#if UNITY_EDITOR
            const string undoingStepLabel = "Disconnect from Connection";
            Undo.RecordObject(
                this,
                undoingStepLabel
            );
            Undo.RecordObject(
                controlPoint,
                undoingStepLabel
            );
#endif

            controlPoint.Connection = null;
            m_ControlPoints.Remove(controlPoint);
            transformSynchronizer.OnControlPointsUpdated();

            foreach (CurvySplineSegment splineSegment in m_ControlPoints)
                if (splineSegment.FollowUp == controlPoint)
                {
#if UNITY_EDITOR
                    Undo.RecordObject(
                        splineSegment,
                        undoingStepLabel
                    );
#endif
                    splineSegment.SetFollowUp(null);
                }

            if (m_ControlPoints.Count == 0 && destroySelfIfEmpty)
                Delete();
        }

        /// <summary>
        /// Deletes the connection
        /// </summary>
        public void Delete() =>
            gameObject.Destroy(
                true,
                true
            );

        /// <summary>
        /// Gets all Control Points except the one provided
        /// </summary>
        /// <param name="source">the Control Point to filter out</param>
        /// <returns>list of Control Points</returns>
        [Obsolete("Inline the method's body if needed")]
        public List<CurvySplineSegment> OtherControlPoints(CurvySplineSegment source)
            => ControlPointsList
                .Where(cp => cp != source)
                .ToList();

        /// <summary>
        /// Synchronize all the connected control points to match the given position and rotation, based on their synchronisation options, namely <see cref="CurvySplineSegment.ConnectionSyncPosition"/> and <see cref="CurvySplineSegment.ConnectionSyncRotation"/>. Will update the CurvyConnection's game object's transform too.
        /// </summary>
        /// <remarks>Can dirty the splines of the updated control points</remarks>
        //todo rename to use Synchronization instead of Synchronisation
        public void SetSynchronisationPositionAndRotation(Vector3 referencePosition, Quaternion referenceRotation) =>
            transformSynchronizer.ApplyTransform(
                referencePosition,
                referenceRotation
            );

#if UNITY_EDITOR
        /// <summary>
        /// Gets the gizmo color based on the synchronization options of the connected control points
        /// </summary>
        [UsedImplicitly]
        [Obsolete("Use CurvyConnectionEditor.GetGizmoColor instead")]
        public Color GetGizmoColor()
        {
            Color gizmoColor;

            if (ControlPointsList.Count == 0)
                gizmoColor = Color.black;
            else
            {
                bool allPositionsSynced = true;
                bool allRotationsSynced = true;
                foreach (CurvySplineSegment controlPoint in ControlPointsList)
                {
                    allPositionsSynced = allPositionsSynced && controlPoint.ConnectionSyncPosition;
                    allRotationsSynced = allRotationsSynced && controlPoint.ConnectionSyncRotation;

                    if (allPositionsSynced == false && allRotationsSynced == false)
                        break;
                }

                if (allPositionsSynced)
                    gizmoColor = allRotationsSynced
                        ? Color.white
                        : new Color(
                            255 / 255f,
                            49 / 255f,
                            38 / 255f
                        );
                else if (allRotationsSynced)
                    gizmoColor = new Color(
                        1,
                        1,
                        0
                    );
                else
                    gizmoColor = Color.black;
            }

            return gizmoColor;
        }
#endif

        #endregion

        #region ISerializationCallbackReceiver

        /// <summary>
        /// Implementation of UnityEngine.ISerializationCallbackReceiver
        /// Called automatically by Unity, is not meant to be called by Curvy's users
        /// </summary>
        public void OnBeforeSerialize() =>
            RemoveNullCPs();

        /// <summary>
        /// Implementation of UnityEngine.ISerializationCallbackReceiver
        /// Called automatically by Unity, is not meant to be called by Curvy's users
        /// </summary>
        public void OnAfterDeserialize() =>
            RemoveNullCPs();

        private void RemoveNullCPs() =>
            m_ControlPoints.RemoveAll(
                cp => ReferenceEquals(
                    cp,
                    null
                )
            );

        #endregion

        #region ### Privates & Internals ###

#if DOCUMENTATION___FORCE_IGNORE___CURVY == false

        [SerializeField, Hide]
        private List<CurvySplineSegment> m_ControlPoints = new List<CurvySplineSegment>();

        private ReadOnlyCollection<CurvySplineSegment> readOnlyControlPoints;

        [NotNull]
        private readonly TransformSynchronizer transformSynchronizer;

        [NotNull]
        private readonly UndoFixer undoFixer;

        private void DoUpdate() => transformSynchronizer.OnUpdate();

        private void OnSceneLoaded(
            Scene scene,
            LoadSceneMode loadSceneMode)
        {
            //m_ControlPoints can have null references in it because if the cp it has is disabled, and then the scene is switched, the cp will not execute its OnDestroy, ans thus will not remove himself from the connection. And since destroyed unity objects become equal to null, the CPs list will have a null value in it
            int removedElementsCount = m_ControlPoints.RemoveAll(cp => cp == null);
            if (removedElementsCount != 0)
            {
                if (m_ControlPoints.Count == 0)
                    Delete();
                else
                {
                    DTLog.LogWarning(
                        "[Curvy] Connection "
                        + name
                        + " was not destroyed after scene switch. That should not happen. Please raise a bug report.",
                        this
                    );
                    transformSynchronizer.ResetMonitoring();
                }
            }
        }

        protected override void ResetOnEnable()
        {
            base.ResetOnEnable();
            readOnlyControlPoints = null;
        }

#endif

        #endregion
    }
}