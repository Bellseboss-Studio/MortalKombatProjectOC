// =====================================================================
// Copyright © 2013 ToolBuddy
// All rights reserved
// 
// http://www.toolbuddy.net
// =====================================================================

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using FluffyUnderware.Curvy.Pools;
using FluffyUnderware.Curvy.Utils;
using FluffyUnderware.DevTools;
using FluffyUnderware.DevTools.Extensions;
using JetBrains.Annotations;
using ToolBuddy.Pooling.Collections;
using ToolBuddy.Pooling.Pools;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Serialization;
#if UNITY_EDITOR
using UnityEditor;
#endif

#if CONTRACTS_FULL
using System.Diagnostics.Contracts;
#endif

namespace FluffyUnderware.Curvy
{
    /// <summary>
    /// Class covering a Curvy Spline Segment / ControlPoint
    /// </summary>
    public partial class CurvySplineSegment : DTVersionedMonoBehaviour, IPoolable
    {
        #region ### Serialized Fields ###

        #region --- General ---

        [Group("General")]
        [FieldAction(
            "CBBakeOrientation",
            Position = ActionAttribute.ActionPositionEnum.Below
        )]
        [Label(
            "Bake Orientation",
            "Automatically apply orientation to CP transforms?"
        )]
        [SerializeField]
        private bool m_AutoBakeOrientation;

        [Group("General")]
        [Tooltip("Check to use this transform's rotation")]
        [FieldCondition(
            nameof(IsOrientationAnchorEditable),
            true
        )]
        [SerializeField]
        private bool m_OrientationAnchor;

        [Label(
            "Swirl",
            "Add Swirl to orientation?"
        )]
        [Group("General")]
        [FieldCondition(
            nameof(CanHaveSwirl),
            true
        )]
        [SerializeField]
        private CurvyOrientationSwirl m_Swirl = CurvySplineSegmentDefaultValues.Swirl;

        [Label(
            "Turns",
            "Number of swirl turns"
        )]
        [Group("General")]
        [FieldCondition(
            nameof(CanHaveSwirl),
            true,
            false,
            ConditionalAttribute.OperatorEnum.AND,
            "m_Swirl",
            CurvyOrientationSwirl.None,
            true
        )]
        [SerializeField]
        private float m_SwirlTurns;

        #endregion

        #region --- Bezier ---

        [Section(
            "Bezier Options",
            Sort = 1,
            HelpURL = AssetInformation.DocsRedirectionBaseUrl + "curvysplinesegment_bezier"
        )]
        [GroupCondition(
            nameof(Interpolation),
            CurvyInterpolation.Bezier
        )]
        [SerializeField]
        private bool m_AutoHandles = CurvySplineSegmentDefaultValues.AutoHandles;

        [RangeEx(
            0,
            1,
            "Distance %",
            "Handle length by distance to neighbours"
        )]
        [FieldCondition(
            nameof(m_AutoHandles),
            true,
            Action = ActionAttribute.ActionEnum.Enable
        )]
        [SerializeField]
        private float m_AutoHandleDistance = CurvySplineSegmentDefaultValues.AutoHandleDistance;

        [VectorEx(
            Precision = 3,
            Options = AttributeOptionsFlags.Clipboard | AttributeOptionsFlags.Negate,
            Color = "#FFFF00"
        )]
        [SerializeField, FormerlySerializedAs("HandleIn")]
        private Vector3 m_HandleIn = CurvySplineSegmentDefaultValues.HandleIn;

        [VectorEx(
            Precision = 3,
            Options = AttributeOptionsFlags.Clipboard | AttributeOptionsFlags.Negate,
            Color = "#00FF00"
        )]
        [SerializeField, FormerlySerializedAs("HandleOut")]
        private Vector3 m_HandleOut = CurvySplineSegmentDefaultValues.HandleOut;

        #endregion

        #region --- TCB ---

        [Section(
            "TCB Options",
            Sort = 1,
            HelpURL = AssetInformation.DocsRedirectionBaseUrl + "curvysplinesegment_tcb"
        )]
        [GroupCondition(
            nameof(Interpolation),
            CurvyInterpolation.TCB
        )]
        [GroupAction(
            "TCBOptionsGUI",
            Position = ActionAttribute.ActionPositionEnum.Below
        )]
        [Label(
            "Local Tension",
            "Override Spline Tension?"
        )]
        [SerializeField, FormerlySerializedAs("OverrideGlobalTension")]
        private bool m_OverrideGlobalTension;

        [Label(
            "Local Continuity",
            "Override Spline Continuity?"
        )]
        [SerializeField, FormerlySerializedAs("OverrideGlobalContinuity")]
        private bool m_OverrideGlobalContinuity;

        [Label(
            "Local Bias",
            "Override Spline Bias?"
        )]
        [SerializeField, FormerlySerializedAs("OverrideGlobalBias")]
        private bool m_OverrideGlobalBias;

        [Tooltip("Synchronize Start and End Values")]
        [SerializeField, FormerlySerializedAs("SynchronizeTCB")]
        private bool m_SynchronizeTCB = CurvySplineSegmentDefaultValues.SynchronizeTCB;

        [Label("Tension"), FieldCondition(
             "m_OverrideGlobalTension",
             true
         )]
        [SerializeField, FormerlySerializedAs("StartTension")]
        private float m_StartTension;

        [Label("Tension (End)"), FieldCondition(
             "m_OverrideGlobalTension",
             true,
             false,
             ConditionalAttribute.OperatorEnum.AND,
             "m_SynchronizeTCB",
             false,
             false
         )]
        [SerializeField, FormerlySerializedAs("EndTension")]
        private float m_EndTension;

        [Label("Continuity"), FieldCondition(
             "m_OverrideGlobalContinuity",
             true
         )]
        [SerializeField, FormerlySerializedAs("StartContinuity")]
        private float m_StartContinuity;

        [Label("Continuity (End)"), FieldCondition(
             "m_OverrideGlobalContinuity",
             true,
             false,
             ConditionalAttribute.OperatorEnum.AND,
             "m_SynchronizeTCB",
             false,
             false
         )]
        [SerializeField, FormerlySerializedAs("EndContinuity")]
        private float m_EndContinuity;

        [Label("Bias"), FieldCondition(
             "m_OverrideGlobalBias",
             true
         )]
        [SerializeField, FormerlySerializedAs("StartBias")]
        private float m_StartBias;

        [Label("Bias (End)"), FieldCondition(
             "m_OverrideGlobalBias",
             true,
             false,
             ConditionalAttribute.OperatorEnum.AND,
             "m_SynchronizeTCB",
             false,
             false
         )]
        [SerializeField, FormerlySerializedAs("EndBias")]
        private float m_EndBias;

        #endregion

        /*
#region --- CG Options ---
        
        /// <summary>
        /// Material ID (used by CG)
        /// </summary>
        [Section("Generator Options", true, Sort = 5, HelpURL = AssetInformation.DocsRedirectionBaseUrl + "curvysplinesegment_cg")]
        [Positive(Label="Material ID")]
        [SerializeField]
        int m_CGMaterialID;

        /// <summary>
        /// Whether to create a hard edge or not (used by PCG)
        /// </summary>
        [Label("Hard Edge")]
        [SerializeField]
        bool m_CGHardEdge;
        /// <summary>
        /// Maximum vertex distance when using optimization (0=infinite)
        /// </summary>
        [Positive(Label="Max Step Size",Tooltip="Max step distance when using optimization")]
        [SerializeField]
        float m_CGMaxStepDistance;
#endregion
        */

        #region --- Connections ---

        [SerializeField, HideInInspector]
        private CurvySplineSegment m_FollowUp;

        [SerializeField, HideInInspector]
        private ConnectionHeadingEnum m_FollowUpHeading = ConnectionHeadingEnum.Auto;

        //DESIGN: shouldn't these two be part of Connection? By spreading them on the ControlPoints, we risk a desynchronization between m_ConnectionSyncPosition's value of a CP and the one of the connected CP
        [SerializeField, HideInInspector]
        private bool m_ConnectionSyncPosition;

        [SerializeField, HideInInspector]
        private bool m_ConnectionSyncRotation;

        [SerializeField, HideInInspector]
        private CurvyConnection m_Connection;

        #endregion

        #endregion

        #region ### Private Fields ###

        private Transform cachedTransform;

        /// <summary>
        /// The cached result of Spline.GetNextControlPoint(this)
        /// OPTIM: use this more often?
        /// </summary>
        [CanBeNull]
        private CurvySplineSegment cachedNextControlPoint;

        [CanBeNull]
        private ThreadSafeData threadSafeData;

        private CurvySpline mSpline;
        private Bounds? mBounds;

        /// <summary>
        /// The Metadata components added to this GameObject
        /// </summary>
        private readonly HashSet<CurvyMetadataBase> mMetadata = new HashSet<CurvyMetadataBase>();

        /// <summary>
        /// The local position used in the segment approximations cache latest computation
        /// </summary>
        private Vector3? lastProcessedLocalPosition;

        /// <summary>
        /// The local rotation used in the segment approximations cache latest computation
        /// </summary>
        private Quaternion? lastProcessedLocalRotation;

        private float distance = -1;
        private float length = -1;

        /// <summary>
        /// A subArray used in the computation of B-Splines, to avoid arrays computation at each computation
        /// </summary>
        private SubArray<Vector3> bSplineP0Array;
        /// <summary>
        /// A subArray used in the computation of the tangent of B-Splines, to avoid arrays allocation at each computation
        /// </summary>
        private SubArray<Vector3> bSplineDeltaPArray;

        //todo design: include in CurvySpline.RelationshipCache?
        private ControlPointExtrinsicProperties extrinsicProperties;

        [NotNull]
        private readonly Approximations approximations = new Approximations();

        #endregion

        #region ### Unity Callbacks ###

#if DOCUMENTATION___FORCE_IGNORE___UNITY == false


        [UsedImplicitly]
        private void Awake()
        {
            cachedTransform = transform;

            DoInitialValidations();
        }

        protected override void OnEnable()
        {
            base.OnEnable();

            DoInitialValidations();

            if (CanTouchItsSpline)
                Spline.SetDirtyAll(
                    SplineDirtyingType.Everything,
                    Connection != null
                );
        }

#if UNITY_EDITOR

        [UsedImplicitly]
        private void OnDrawGizmos()
        {
            //Debug.Log($"{name} OnDrawGizmos");

            if (Spline == null)
                return;
            if (Spline.IsInitialized == false)
                return;
            if (Spline.ShowGizmos == false)
                return;

            doGizmos(gameObject.IsSelectedOrParentSelected());
        }

        [UsedImplicitly]
        private void OnDrawGizmosSelected()
        {
            //Debug.Log($"{name} OnDrawGizmosSelected");

            if (Spline == null)
                return;
            if (Spline.IsInitialized == false)
                return;

            doGizmos(true);
        }
#endif

        [UsedImplicitly]
        private void OnDestroy()
        {
            UpdateSelectionIfNeeded();

            Disconnect();

            ArrayPools.Vector3.Free(bSplineP0Array);
            ArrayPools.Vector3.Free(bSplineDeltaPArray);
            approximations.Clear();
        }

        protected override void OnValidate()
        {
            base.OnValidate();

            AutoHandles = m_AutoHandles;
            Connection = m_Connection;

            if (CanTouchItsSpline)
                Spline.SetDirty(
                    this,
                    SplineDirtyingType.Everything
                );

            ForceHierarchyDrawing();
        }

        /// <summary>
        /// Resets the properties of this control point, but will not remove its Connection if it has any.
        /// </summary>
        [UsedImplicitly]
        [Obsolete("Use ResetConnectionUnrelatedProperties instead")]
        //todo remove this method. That way Reset is used only as a callback from unity
        public new void Reset()
        {
            ResetConnectionUnrelatedProperties();
            base.Reset();
        }

#endif

        #endregion

        #region ### Privates & Internals ###

        #region Properties used in inspector's field condition and group condition

        private CurvyInterpolation Interpolation => Spline
            ? Spline.Interpolation
            : CurvyInterpolation.Linear;

        private bool IsDynamicOrientation => Spline && Spline.Orientation == CurvyOrientation.Dynamic;

        private bool IsOrientationAnchorEditable
        {
            get
            {
                CurvySpline curvySpline = Spline;
                return IsDynamicOrientation
                       && curvySpline.IsControlPointVisible(this)
                       && curvySpline.FirstVisibleControlPoint != this
                       && curvySpline.LastVisibleControlPoint != this;
            }
        }

        private bool CanHaveSwirl
        {
            get
            {
                CurvySpline curvySpline = Spline;
                return IsDynamicOrientation
                       && curvySpline
                       && curvySpline.IsControlPointAnOrientationAnchor(this)
                       && (curvySpline.Closed || curvySpline.LastVisibleControlPoint != this);
            }
        }

        #endregion

#if DOCUMENTATION___FORCE_IGNORE___CURVY == false

        #region BSplines

        /// <summary>
        /// A subArray used in the computation of B-Splines, to avoid arrays allocation at each computation
        /// </summary>
        private SubArray<Vector3> BSplineP0Array
        {
            get
            {
                if (bSplineP0Array.Count != mSpline.BSplineDegree + 1)
                {
                    ArrayPool<Vector3> arrayPool = ArrayPools.Vector3;
                    if (bSplineP0Array.Count > 0)
                        arrayPool.Free(bSplineP0Array);
                    bSplineP0Array = arrayPool.Allocate(
                        Spline.BSplineDegree + 1,
                        false
                    );
                }

                return bSplineP0Array;
            }
        }
        
        /// <summary>
        /// A subArray used in the computation of the tangent of B-Splines, to avoid arrays allocation at each computation
        /// </summary>
        private SubArray<Vector3> BSplineDeltaPArray
        {
            get
            {
                if (bSplineDeltaPArray.Count != mSpline.BSplineDegree)
                {
                    ArrayPool<Vector3> arrayPool = ArrayPools.Vector3;
                    if (bSplineDeltaPArray.Count > 0)
                        arrayPool.Free(bSplineDeltaPArray);
                    bSplineDeltaPArray = arrayPool.Allocate(
                        Spline.BSplineDegree,
                        false
                    );
                }

                return bSplineDeltaPArray;
            }
        }

        /// <summary>
        /// Fills <paramref name="pArray"/> with the P0s numbers as defined in the B-Spline section, De Boor's algorithm, here: https://pages.mtu.edu/~shene/COURSES/cs3621/NOTES/
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void GetBSplineP0s([NotNull] ReadOnlyCollection<CurvySplineSegment> controlPoints, int controlPointsCount,
            int degree, int k, [NotNull] Vector3[] pArray)
        {
#if CURVY_SANITY_CHECKS
            Assert.IsTrue(pArray.Length >= degree + 1);
#endif
            for (int j = 0; j <= degree; j++)
            {
                int index = (j + k) - degree;
                pArray[j] = controlPoints[
                        index < controlPointsCount
                            ? index
                            : index - controlPointsCount
                    ].threadSafeData
                    .ThreadSafeLocalPosition;
            }
        }

        #endregion

        #region Extrinsic properties

        /// <summary>
        /// Properties describing the relationship between this CurvySplineSegment and its containing CurvySpline.
        /// </summary>
        internal void SetExtrinsicPropertiesINTERNAL(ControlPointExtrinsicProperties value) =>
            extrinsicProperties = value;

        internal ref readonly ControlPointExtrinsicProperties GetExtrinsicPropertiesINTERNAL()
            => ref extrinsicProperties;

        #endregion


        /// <summary>
        /// True if the this instance is allowed to influence its spline
        /// </summary>
        private bool CanTouchItsSpline =>
            IsActiveAndEnabled
            && mSpline
            != null; //OPTIM if needed, replace != with ReferenceEquals, while handling the case where == null and ReferenceEquals(,null) == false

        private void DoInitialValidations()
        {
            //Happens when duplicating a spline that has a connection. This can be avoided
            if (Connection && Connection.ControlPointsList.Contains(this) == false)
                SetConnection(null);

#pragma warning disable CS0618
            ReloadMetaData();
#pragma warning restore CS0618
        }

        private void CheckAgainstMetaDataDuplication()
        {
            if (Metadata.Count > 1)
            {
                HashSet<Type> metaDataTypes = new HashSet<Type>();
                foreach (CurvyMetadataBase metaData in Metadata)
                {
                    Type componentType = metaData.GetType();
                    if (metaDataTypes.Contains(componentType))
                        DTLog.LogWarning(
                            String.Format(
                                "[Curvy] Game object '{0}' has multiple Components of type '{1}'. Control Points should have no more than one Component instance for each MetaData type.",
                                ToString(),
                                componentType
                            ),
                            this
                        );
                    else
                        metaDataTypes.Add(componentType);
                }
            }
        }

        /// <summary>
        /// Sets the connection handler this Control Point is using
        /// </summary>
        /// <param name="newConnection"></param>
        /// <returns>Whether a modification was done or not</returns>
        /// <remarks>If set to null, FollowUp wil be set to null to</remarks>
        private bool SetConnection(CurvyConnection newConnection)
        {
            bool modificationDone = false;
            if (m_Connection != newConnection)
            {
                modificationDone = true;
                m_Connection = newConnection;
            }

            if (m_Connection == null && m_FollowUp != null)
            {
                modificationDone = true;
                m_FollowUp = null;
            }

            return modificationDone;
        }

        /// <summary>
        /// Returns a different ConnectionHeadingEnum value when connectionHeading has a value that is no more valid in the context of this spline. For example, heading to start (Minus) when there is no previous CP
        /// </summary>
        private static ConnectionHeadingEnum GetValidateConnectionHeading(ConnectionHeadingEnum connectionHeading,
            [CanBeNull] CurvySplineSegment followUp)
        {
            if (followUp == null)
                return connectionHeading;

            if ((connectionHeading == ConnectionHeadingEnum.Minus && CanFollowUpHeadToStart(followUp) == false)
                || (connectionHeading == ConnectionHeadingEnum.Plus && CanFollowUpHeadToEnd(followUp) == false))
                return ConnectionHeadingEnum.Auto;

            return connectionHeading;
        }

        /// <summary>
        /// Sets Auto Handles. When setting it the value of connected control points is also updated
        /// </summary>
        /// <param name="newValue"></param>
        /// <returns>Whether a modifcation was done or not</returns>
        private bool SetAutoHandles(bool newValue)
        {
            bool modificationDone = false;
            if (Connection)
            {
                ReadOnlyCollection<CurvySplineSegment> controlPoints = Connection.ControlPointsList;
                for (int index = 0; index < controlPoints.Count; index++)
                {
                    CurvySplineSegment controlPoint = controlPoints[index];
                    modificationDone = modificationDone || controlPoint.m_AutoHandles != newValue;
                    controlPoint.m_AutoHandles = newValue;
                }
            }
            else
            {
                modificationDone = m_AutoHandles != newValue;
                m_AutoHandles = newValue;
            }

            return modificationDone;
        }

        #region approximations cache computation

        /// <summary>
        /// Set the correct values to the thread safe local positions and rotation
        /// When multithreading, you can't access Transform in the not main threads. Here we cache that data so it is available for threads
        /// </summary>
        internal void PrepareThreadCompatibleDataINTERNAL(bool useFollowUp)
        {
            if (threadSafeData == null)
                threadSafeData = new ThreadSafeData();

            threadSafeData.Set(
                useFollowUp,
                this,
                out CurvySplineSegment nextCP
            );

            //This isn't cached for thread compatibility, but for performance
            cachedNextControlPoint = nextCP;
        }

        internal void refreshCurveINTERNAL()
        {
#if CURVY_SANITY_CHECKS
            Assert.IsNotNull(threadSafeData);
            Assert.IsNotNull(Spline);
            Assert.IsNotNull(approximations);
#endif

            float segmentLength;
            CurvySpline spline = Spline;
            Approximations segmentApproximation = approximations;
            Vector3 currentPosition = threadSafeData.ThreadSafeLocalPosition;
            Vector3 nextPosition = threadSafeData.ThreadSafeNextCpLocalPosition;


            if (spline.IsControlPointASegment(this))
            {
                int approximationCount = GetSegmentCacheSize() + 1;
                CurvySplineSegment nextCP = cachedNextControlPoint;
#if CURVY_SANITY_CHECKS
                Assert.IsNotNull(nextCP);
#endif
                switch (spline.Interpolation)
                {
                    case CurvyInterpolation.BSpline:
                        ApproximationsSetter.SetPositionsToBSpline(
                            segmentApproximation,
                            approximationCount,
                            BSplineP0Array,
                            new BSplineApproximationParameters(this)
                        );
                        break;
                    case CurvyInterpolation.Bezier:
                        ApproximationsSetter.SetPositionsToBezier(
                            segmentApproximation,
                            approximationCount,
                            currentPosition,
                            currentPosition + HandleOut,
                            nextPosition,
                            nextPosition + nextCP.HandleIn
                        );
                        break;
                    case CurvyInterpolation.CatmullRom:
                        ApproximationsSetter.SetPositionsToCatmullRom(
                            segmentApproximation,
                            approximationCount,
                            currentPosition,
                            nextPosition,
                            threadSafeData.ThreadSafePreviousCpLocalPosition,
                            nextCP.threadSafeData.ThreadSafeNextCpLocalPosition
                        );
                        break;
                    case CurvyInterpolation.TCB:
                        ApproximationsSetter.SetPositionsToTCB(
                            segmentApproximation,
                            approximationCount,
                            EffectiveTcbParameters,
                            currentPosition,
                            nextPosition,
                            threadSafeData.ThreadSafePreviousCpLocalPosition,
                            nextCP.threadSafeData.ThreadSafeNextCpLocalPosition
                        );
                        break;
                    case CurvyInterpolation.Linear:
                        ApproximationsSetter.SetPositionsToLinear(
                            segmentApproximation,
                            approximationCount,
                            currentPosition,
                            nextPosition
                        );
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(spline.Interpolation));

                }

                segmentLength = ApproximationsSetter.SetSegmentTangentsAndDistances(
                    segmentApproximation,
                    approximationCount,
                    this
                );
            }
            else
            {
                ApproximationsSetter.SetPositionsToPoint(
                    segmentApproximation,
                    currentPosition
                );

                segmentLength = ApproximationsSetter.SetPointTangentAndDistance(
                    segmentApproximation,
                    threadSafeData.ThreadSafePreviousCpLocalPosition,
                    currentPosition,
                    nextPosition,
                    threadSafeData.ThreadSafeLocalRotation,
                    this
                );
            }

            Length = segmentLength;
            ClearBoundsINTERNAL();
            UpdateLasProcessedLocalPosition();
        }

        private int GetSegmentCacheSize()
        {
            int cacheSize = CurvySpline.CalculateCacheSize(
                Spline.CacheDensity,
                threadSafeData.ThreadSafeNextCpLocalPosition.Subtraction(threadSafeData.ThreadSafeLocalPosition).magnitude,
                Spline.MaxPointsPerUnit
            );
#if CURVY_SANITY_CHECKS
            Assert.IsTrue(cacheSize > 0);
#endif
            return cacheSize;
        }


        internal void refreshOrientationNoneINTERNAL()
        {
            ApproximationsSetter.SetOrientationToNone(
                approximations,
                CacheSize + 1
            );
            UpdateLasProcessedLocalRotation();
        }

        internal void refreshOrientationStaticINTERNAL()
        {
            ApproximationsSetter.SetOrientationToStatic(
                approximations,
                CacheSize + 1,
                getOrthoUp0INTERNAL(),
                getOrthoUp1INTERNAL()
            );
            UpdateLasProcessedLocalRotation();
        }

        /// <summary>
        /// Set each point's up as the initialUp rotated by the same rotation than the one that rotates initial tangent to the point's tangent
        /// </summary>
        /// <remarks>Does not handle swirl</remarks>
        /// <param name="initialUp"></param>
        internal void refreshOrientationDynamicINTERNAL(Vector3 initialUp)
        {
            ApproximationsSetter.SetOrientationToDynamic(
                approximations,
                CacheSize + 1,
                initialUp
            );
            UpdateLasProcessedLocalRotation();
        }

        [UsedImplicitly]
        private void UpdateLasProcessedLocalPosition() =>
            lastProcessedLocalPosition = threadSafeData.ThreadSafeLocalPosition;

        [UsedImplicitly]
        private void UpdateLasProcessedLocalRotation() =>
            lastProcessedLocalRotation = threadSafeData.ThreadSafeLocalRotation;

        #endregion

        internal void ClearBoundsINTERNAL() =>
            mBounds = null;

        /// <summary>
        /// Gets Transform.up orthogonal to ApproximationT[0]
        /// </summary>
        internal Vector3 getOrthoUp0INTERNAL()
        {
            Vector3 u = threadSafeData.ThreadSafeLocalRotation * Vector3.up;
            Vector3[] tangentsArray = TangentsApproximation.Array;
            Vector3.OrthoNormalize(
                ref tangentsArray[0],
                ref u
            );
            return u;
        }

        private Vector3 getOrthoUp1INTERNAL()
        {
            CurvySplineSegment nextControlPoint = Spline.GetNextControlPoint(this);
            Quaternion nextRotation = nextControlPoint
                ? nextControlPoint.threadSafeData.ThreadSafeLocalRotation
                : threadSafeData.ThreadSafeLocalRotation;
            Vector3 u = nextRotation * Vector3.up;
            Vector3[] tangentsArray = TangentsApproximation.Array;
            Vector3.OrthoNormalize(
                ref tangentsArray[CacheSize],
                ref u
            );
            return u;
        }

        internal void UnsetFollowUpWithoutDirtyingINTERNAL()
        {
            m_FollowUp = null;
            m_FollowUpHeading = ConnectionHeadingEnum.Auto;
        }

        [System.Diagnostics.Conditional(CompilationSymbols.CurvySanityChecks)]
        private void DoSanityChecks()
        {
            //todo merge with or resuse CurvySpline.SanityChecker

            if (Spline == null)
                DTLog.LogError(
                    "[Curvy] Calling public method on an orphan segment.",
                    this
                );
            if (Spline.IsInitialized == false)
                DTLog.LogError(
                    "[Curvy] Calling public method on non initialized spline.",
                    Spline
                );
            if (Spline.Dirty)
                DTLog.LogWarning(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "[Curvy] Calling public method on a dirty spline. The returned result will not be up to date. Either refresh the spline manually by calling Refresh(), or wait for it to be refreshed automatically at the next {0} call",
                        Spline.UpdateIn.ToString()
                    ),
                    Spline
                );
        }

        [System.Diagnostics.Conditional(CompilationSymbols.UnityEditor)]
        [UsedImplicitly]
        private void UpdateSelectionIfNeeded()
        {
#if UNITY_EDITOR
            //mSpline is non null when the user delete only this CP. mSpline is null when the user deletes the spline, which then leads to this method to be called
            if (Spline == null)
                return;
            if (Application.isPlaying)
                return;
            if (Selection.activeGameObject != gameObject)
                return;
            CurvySplineSegment previousControlPoint = Spline.GetPreviousControlPoint(this);

            if (previousControlPoint)
                Selection.activeGameObject = previousControlPoint.gameObject;
            else
            {
                CurvySplineSegment nextControlPoint = Spline.GetNextControlPoint(this);
                Selection.activeGameObject = nextControlPoint
                    ? nextControlPoint.gameObject
                    : Spline.gameObject;
            }
#endif
        }

#if UNITY_EDITOR

        #region Gizmo drawing

        private void doGizmos(bool selected)
        {
            //log
            //DTLog.LogWarning($"{name} doGizmos {selected}");
            //OPTIM try multithreading some of the loops in this method. All loops have in them operations that unity forbids the usage of outside the main thread (drawline, worldtoscreen, ...). Maybe using Unity's jobs?
            if (CurvyGlobalManager.Gizmos == CurvySplineGizmos.None)
                return;

            Camera currentCamera = Camera.current;
            int cameraPixelWidth = currentCamera.pixelWidth;
            int cameraPixelHeight = currentCamera.pixelHeight;
            Transform cameraTransform = currentCamera.transform;
            Vector3 cameraPosition = cameraTransform.position;
            Vector3 cameraZDirection;
            Vector3 cameraXDirection;
            {
                Quaternion cameraRotation = cameraTransform.rotation;
                Vector3 direction;
                {
                    direction.x = 0;
                    direction.y = 0;
                    direction.z = 1;
                }
                cameraZDirection = cameraRotation * direction;

                {
                    direction.x = 1;
                    direction.y = 0;
                    direction.z = 0;
                }
                cameraXDirection = cameraRotation * direction;
            }
            Bounds bounds = Bounds;

            // Skip if the segment isn't in view
            if (!GeometryUtility.TestPlanesAABB(
                    CameraFrustumPlanesProvider.Instance.GetFrustumPlanes(currentCamera),
                    bounds
                ))
                return;

            CurvySpline spline = Spline;
            Transform splineTransform = spline.transform;
            Vector3 splineTransformLocalScale = splineTransform.localScale;
            Vector3 scale;
            {
                scale.x = 1 / splineTransformLocalScale.x;
                scale.y = 1 / splineTransformLocalScale.y;
                scale.z = 1 / splineTransformLocalScale.z;
            }
            Color splineGizmoColor = selected
                ? spline.GizmoSelectionColor
                : spline.GizmoColor;
            Vector3 transformPosition = transform.position;
            float cameraCenterWidth = cameraPixelWidth * 0.5f;
            float cameraCenterHeight = cameraPixelHeight * 0.5f;

            bool viewCurve = CurvyGlobalManager.ShowCurveGizmo;

            // Control Point
            if (viewCurve)
            {
                Gizmos.color = splineGizmoColor;
                float handleSize = DTUtility.GetHandleSize(
                    transformPosition,
                    currentCamera,
                    cameraCenterWidth,
                    cameraCenterHeight,
                    cameraPosition,
                    cameraZDirection,
                    cameraXDirection
                );
                float cpGizmoSize = handleSize
                                    * (selected
                                        ? 1
                                        : 0.7f)
                                    * CurvyGlobalManager.GizmoControlPointSize;

                if (spline.RestrictTo2D)
                    Gizmos.DrawCube(
                        transformPosition,
                        OptimizedOperators.Multiply(
                            Vector3.one,
                            cpGizmoSize
                        )
                    );
                else
                    Gizmos.DrawSphere(
                        transformPosition,
                        cpGizmoSize
                    );
            }

            //Remaining
            if (spline.IsControlPointASegment(this))
            {
                if (spline.Dirty)
                    spline.Refresh();

                Matrix4x4 initialGizmoMatrix = Gizmos.matrix;
                Matrix4x4 currentGizmoMatrix = Gizmos.matrix = splineTransform.localToWorldMatrix;

                //Spline lines
                if (viewCurve)
                {
                    float steps;
                    {
                        float camDistance = cameraPosition.Subtraction(bounds.ClosestPoint(cameraPosition)).magnitude;

                        float df = Mathf.Clamp(
                                       camDistance,
                                       1,
                                       3000
                                   )
                                   / 3000;
                        df = df < 0.01f
                            ? DTTween.SineOut(
                                df,
                                0,
                                1
                            )
                            : DTTween.QuintOut(
                                df,
                                0,
                                1
                            );

                        steps = Mathf.Clamp(
                            (Length * CurvyGlobalManager.SceneViewResolution * 0.1f) / df,
                            1,
                            10000
                        );
                    }
                    DrawGizmoLines(1 / steps);
                }

                //Approximations
                if (PositionsApproximation.Count > 0 && CurvyGlobalManager.ShowApproximationGizmo)
                {
                    Vector3[] positionsArray = PositionsApproximation.Array;
                    Gizmos.color = spline.GizmoColor.Multiply(0.8f);
                    Vector3 size = OptimizedOperators.Multiply(
                        0.1f,
                        scale
                    );
                    for (int i = 0; i < PositionsApproximation.Count; i++)
                    {
                        float handleSize = DTUtility.GetHandleSize(
                            currentGizmoMatrix.MultiplyPoint3x4(positionsArray[i]),
                            currentCamera,
                            cameraCenterWidth,
                            cameraCenterHeight,
                            cameraPosition,
                            cameraZDirection,
                            cameraXDirection
                        );

                        Gizmos.DrawCube(
                            positionsArray[i],
                            handleSize.Multiply(size)
                        );
                    }
                }

                //Orientation
                if (spline.Orientation != CurvyOrientation.None
                    && UpsApproximation.Count > 0
                    && CurvyGlobalManager.ShowOrientationGizmo)
                {
                    Vector3[] upsArray = UpsApproximation.Array;
                    Vector3[] positionsArray = PositionsApproximation.Array;

                    Gizmos.color = CurvyGlobalManager.GizmoOrientationColor;
                    Vector3 orientationGizmoSize = scale.Multiply(CurvyGlobalManager.GizmoOrientationLength);

                    for (int i = 0; i < UpsApproximation.Count; i++)
                    {
                        Vector3 lineEnd;
                        lineEnd.x = positionsArray[i].x + (upsArray[i].x * orientationGizmoSize.x);
                        lineEnd.y = positionsArray[i].y + (upsArray[i].y * orientationGizmoSize.y);
                        lineEnd.z = positionsArray[i].z + (upsArray[i].z * orientationGizmoSize.z);

                        Gizmos.DrawLine(
                            positionsArray[i],
                            lineEnd
                        );
                    }


                    if (spline.IsControlPointAnOrientationAnchor(this) && spline.Orientation == CurvyOrientation.Dynamic)
                        if (UpsApproximation.Count != 0)
                        {
                            Gizmos.color = CurvyGlobalManager.GizmoOrientationColor;
                            Vector3 u = upsArray[0];
                            u.Set(
                                u.x * scale.x,
                                u.y * scale.y,
                                u.z * scale.z
                            );
                            Gizmos.DrawRay(
                                positionsArray[0],
                                CurvyGlobalManager.GizmoOrientationLength * 1.75f * u
                            );
                        }
                }

                //Tangent
                if (TangentsApproximation.Count > 0 && CurvyGlobalManager.ShowTangentsGizmo)
                {
                    Vector3[] tangentsArray = TangentsApproximation.Array;
                    Vector3[] positionsArray = PositionsApproximation.Array;

                    int segmentCacheSize = CacheSize;
                    float tangentSize = CurvyGlobalManager.GizmoOrientationLength;
                    for (int i = 0; i < TangentsApproximation.Count; i++)
                    {
                        //updating gizmo color
                        if (i == 0)
                            Gizmos.color = Color.blue;
                        else if (i == 1)
                            Gizmos.color = GizmoTangentColor;
                        else if (i == segmentCacheSize)
                            Gizmos.color = Color.black;

                        Vector3 lineEnd;
                        lineEnd.y = positionsArray[i].y + (tangentsArray[i].y * tangentSize);
                        lineEnd.z = positionsArray[i].z + (tangentsArray[i].z * tangentSize);
                        lineEnd.x = positionsArray[i].x + (tangentsArray[i].x * tangentSize);

                        Gizmos.DrawLine(
                            positionsArray[i],
                            lineEnd
                        );
                    }
                }

                Gizmos.matrix = initialGizmoMatrix;
            }
        }

        /// <summary>
        /// Draw gizmo lines representing the spline segment
        /// </summary>
        /// <param name="stepSize">The relative distance between the start and end of each line. Must be exclusively between 0 and 1</param>
        private void DrawGizmoLines(float stepSize)
        {
            CurvySpline spline = Spline;
            CurvyInterpolation splineInterpolation = spline.Interpolation;

#if CURVY_SANITY_CHECKS
            if (spline.Dirty)
                DTLog.LogWarning(
                    "Interpolate should not be called on segment of a dirty spline. Call CurvySpline.Refresh first",
                    this
                );
            Assert.IsTrue(spline.IsControlPointASegment(this));
            Assert.IsTrue(spline.IsCpsRelationshipCacheValidINTERNAL);
            Assert.IsTrue(stepSize > 0);
            Assert.IsTrue(stepSize <= 1);
#endif
            if (splineInterpolation == CurvyInterpolation.Linear)
                Gizmos.DrawLine(
                    Interpolate(0),
                    Interpolate(1)
                );
            else
            {
                Vector3 startPoint;
                if (splineInterpolation == CurvyInterpolation.BSpline)
                    startPoint = BSpline(
                        spline.ControlPointsList,
                        spline.SegmentToTF(this),
                        spline.IsBSplineClamped,
                        spline.Closed,
                        spline.BSplineDegree,
                        BSplineP0Array.Array
                    );
                else
                    startPoint = threadSafeData.ThreadSafeLocalPosition;

                //used only in BSplines for performance reasons
                bool isBSplineClamped = default;
                int bSplineDegree = default;
                ReadOnlyCollection<CurvySplineSegment> controlPoints = default;
                int controlPointsCount = default;
                float segmentTF = default;
                int n = default;
                int nPlus1 = default;
                int previousK = default;
                Vector3[] ps = default;
                int psCount = default;
                Vector3[] psCopy = default;
                SubArray<Vector3> psCopySubArray = default;
                if (splineInterpolation == CurvyInterpolation.BSpline)
                {
                    isBSplineClamped = spline.IsBSplineClamped;
                    bSplineDegree = spline.BSplineDegree;
                    controlPoints = spline.ControlPointsList;
                    segmentTF = spline.SegmentToTF(this);
                    controlPointsCount = controlPoints.Count;
                    n = BSplineHelper.GetBSplineN(
                        controlPointsCount,
                        bSplineDegree,
                        spline.Closed
                    );
                    nPlus1 = n + 1;
                    previousK = int.MinValue;
                    SubArray<Vector3> splinePsVector = BSplineP0Array;
                    ps = splinePsVector.Array;
                    psCount = splinePsVector.Count;
                    psCopySubArray = ArrayPools.Vector3.Allocate(psCount);
                    psCopy = psCopySubArray.Array;
                }

                for (float localF = 0; localF < 1; localF += stepSize)
                {
                    Vector3 interpolatedPoint;
                    {
                        Vector3 result;
                        //Inlined version of Interpolate, stripped from some code for performance reasons
                        //If you modify this, modify also the inlined version of this method in refreshCurveINTERNAL()
                        switch (splineInterpolation)
                        {
                            case CurvyInterpolation.BSpline:
                            {
                                float tf = segmentTF + (localF / spline.Count);
                                BSplineHelper.GetBSplineUAndK(
                                    tf,
                                    isBSplineClamped,
                                    bSplineDegree,
                                    n,
                                    out float u,
                                    out int k
                                );
                                if (k != previousK)
                                {
                                    GetBSplineP0s(
                                        controlPoints,
                                        controlPointsCount,
                                        bSplineDegree,
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
                                result = isBSplineClamped
                                    ? BSplineHelper.DeBoorClamped(
                                        bSplineDegree,
                                        k,
                                        u,
                                        nPlus1,
                                        psCopy
                                    )
                                    : BSplineHelper.DeBoorUnclamped(
                                        bSplineDegree,
                                        k,
                                        u,
                                        psCopy
                                    );
                                break;
                            }
                            case CurvyInterpolation.CatmullRom:
                            {
                                result = CurvySpline.CatmullRom(
                                    threadSafeData.ThreadSafePreviousCpLocalPosition,
                                    threadSafeData.ThreadSafeLocalPosition,
                                    threadSafeData.ThreadSafeNextCpLocalPosition,
                                    cachedNextControlPoint.threadSafeData.ThreadSafeNextCpLocalPosition,
                                    localF
                                );
                            }
                                break;
                            case CurvyInterpolation.Bezier:
                            {
                                result = CurvySpline.Bezier(
                                    threadSafeData.ThreadSafeLocalPosition.Addition(HandleOut),
                                    threadSafeData.ThreadSafeLocalPosition,
                                    threadSafeData.ThreadSafeNextCpLocalPosition,
                                    threadSafeData.ThreadSafeNextCpLocalPosition.Addition(cachedNextControlPoint.HandleIn),
                                    localF
                                );
                                break;
                            }
                            case CurvyInterpolation.TCB:
                            {
                                TcbParameters tcbParameters = EffectiveTcbParameters;

                                result = CurvySpline.TCB(
                                    threadSafeData.ThreadSafePreviousCpLocalPosition,
                                    threadSafeData.ThreadSafeLocalPosition,
                                    threadSafeData.ThreadSafeNextCpLocalPosition,
                                    cachedNextControlPoint.threadSafeData.ThreadSafeNextCpLocalPosition,
                                    localF,
                                    tcbParameters.StartTension,
                                    tcbParameters.StartContinuity,
                                    tcbParameters.StartBias,
                                    tcbParameters.EndTension,
                                    tcbParameters.EndContinuity,
                                    tcbParameters.EndBias
                                );
                            }
                                break;
                            default:
                                DTLog.LogError(
                                    "[Curvy] Invalid interpolation value " + splineInterpolation,
                                    this
                                );
                                result = startPoint;
                                break;
                        }

                        interpolatedPoint = result;
                    }

                    Gizmos.DrawLine(
                        startPoint,
                        interpolatedPoint
                    );
                    startPoint = interpolatedPoint;
                }

                if (Interpolation == CurvyInterpolation.BSpline)
                    ArrayPools.Vector3.Free(psCopySubArray);

                Vector3 endPoint;
                if (splineInterpolation == CurvyInterpolation.BSpline)
                    endPoint = BSpline(
                        spline.ControlPointsList,
                        spline.SegmentToTF(
                            this,
                            1
                        ),
                        spline.IsBSplineClamped,
                        spline.Closed,
                        spline.BSplineDegree,
                        BSplineP0Array.Array
                    );
                else
                    endPoint = threadSafeData.ThreadSafeNextCpLocalPosition;
                ;
                Gizmos.DrawLine(
                    startPoint,
                    endPoint
                );
            }
        }

        #endregion

#endif

        [System.Diagnostics.Conditional(CompilationSymbols.UnityEditor)]
        private static void ForceHierarchyDrawing()
        {
#if UNITY_EDITOR
            EditorApplication.RepaintHierarchyWindow();
#endif
        }

#endif

        #endregion

        #region protected

#if DOCUMENTATION___FORCE_IGNORE___CURVY == false
        protected override void ResetOnEnable()
        {
            base.ResetOnEnable();
            lastProcessedLocalPosition = default;
            lastProcessedLocalRotation = default;
            mBounds = default;
            threadSafeData = default;
            distance = length = -1;
            approximations.Clear();
        }
#endif

        #endregion
    }
}