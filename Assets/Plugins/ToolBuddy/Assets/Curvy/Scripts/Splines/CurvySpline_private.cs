// =====================================================================
// Copyright © 2013 ToolBuddy
// All rights reserved
// 
// http://www.toolbuddy.net
// =====================================================================

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;
using FluffyUnderware.Curvy.Pools;
using FluffyUnderware.DevTools;
using FluffyUnderware.DevTools.Extensions;
using JetBrains.Annotations;
using ToolBuddy.Pooling.Collections;
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
    /// Curvy Spline class
    /// </summary>
    public partial class CurvySpline : DTVersionedMonoBehaviour
    {
        #region ### Privates Fields ###

        #region ### Serialized fields ###

        /// <summary>
        /// The list of control points
        /// </summary>
        /// <remarks>The returned list should not be modified</remarks>
        [SerializeField, HideInInspector]
        [NotNull]
        private List<CurvySplineSegment> ControlPoints = new List<CurvySplineSegment>();

        #region --- General ---

        [Section(
            "General",
            HelpURL = AssetInformation.DocsRedirectionBaseUrl + "curvyspline_general"
        )]
        [Tooltip("Interpolation Method")]
        [SerializeField, FormerlySerializedAs("Interpolation")]
        private CurvyInterpolation m_Interpolation = CurvyGlobalManager.DefaultInterpolation;

        [Tooltip("Restrict Control Points to a local 2D plane")]
        [SerializeField]
        private bool m_RestrictTo2D;

        [Tooltip("The local 2D plane to restrict the spline's control points to")]
        [SerializeField]
        [FieldCondition(
            nameof(RestrictTo2D),
            true
        )]
        [FieldAction("CBCheck2DPlanar")]
        private CurvyPlane restricted2DPlane = CurvyPlane.XY;

        [SerializeField, FormerlySerializedAs("Closed")]
        private bool m_Closed;

        [FieldCondition(
            nameof(CanHaveManualEndCp),
            Action = ActionAttribute.ActionEnum.Enable
        )]
        [Tooltip("Handle End Control Points automatically?")]
        [SerializeField, FormerlySerializedAs("AutoEndTangents")]
        private bool m_AutoEndTangents = CurvySplineDefaultValues.AutoEndTangents;

        [Tooltip("Orientation Flow")]
        [SerializeField, FormerlySerializedAs("Orientation")]
        private CurvyOrientation m_Orientation = CurvySplineDefaultValues.Orientation;

        #endregion

        #region --- Bezier Options ---

        [Section(
            "Global Bezier Options",
            HelpURL = AssetInformation.DocsRedirectionBaseUrl + "curvyspline_bezier"
        )]
        [GroupCondition(
            nameof(m_Interpolation),
            CurvyInterpolation.Bezier
        )]
        [RangeEx(
            0,
            1,
            "Default Distance %",
            "Handle length by distance to neighbours"
        )]
        [SerializeField]
        private float m_AutoHandleDistance = CurvySplineDefaultValues.AutoHandleDistance;

        #endregion

        #region --- TCB Options ---

        [Section(
            "Global TCB Options",
            HelpURL = AssetInformation.DocsRedirectionBaseUrl + "curvyspline_tcb"
        )]
        [GroupCondition(
            nameof(m_Interpolation),
            CurvyInterpolation.TCB
        )]
        [GroupAction(
            "TCBOptionsGUI",
            Position = ActionAttribute.ActionPositionEnum.Below
        )]
        [SerializeField, FormerlySerializedAs("Tension")]
        private float m_Tension;

        [SerializeField, FormerlySerializedAs("Continuity")]
        private float m_Continuity;

        [SerializeField, FormerlySerializedAs("Bias")]
        private float m_Bias;

        #endregion

        #region --- B-Spline Options ---

        [Section(
            "B-Spline Options",
            HelpURL = AssetInformation.DocsRedirectionBaseUrl + "curvyspline_bspline"
        )]
        [GroupCondition(
            nameof(m_Interpolation),
            CurvyInterpolation.BSpline
        )]
        [RangeEx(
            MinBSplineDegree,
            nameof(MaxBSplineDegree),
            "Degree",
            "The degree of the piecewise polynomial functions.\nIs in the range [2; control points count - 1]"
        )]
        [SerializeField]
        private int bSplineDegree = CurvySplineDefaultValues.BSplineDegree;


        [FieldCondition(
            nameof(CanBeClamped),
            Action = ActionAttribute.ActionEnum.Enable
        )]
        [Label(
            "Clamped",
            "Make the curve pass through the first and last control points by increasing the multiplicity of the first and last knots.\n\nIn technical terms, when this parameter is true, the knot vector is [0, 0, ...,0, 1, 2, ..., N-1, N, N, ..., N]. When false, it is [0, 1, 2, ..., N-1, N]"
        )]
        [SerializeField]
        private bool isBSplineClamped = CurvySplineDefaultValues.IsBSplineClamped;

        #endregion

        #region --- Advanced Settings ---

        [Section(
            "Advanced Settings",
            HelpURL = AssetInformation.DocsRedirectionBaseUrl + "curvyspline_advanced"
        )]
        [FieldAction(
            "ShowGizmoGUI",
            Position = ActionAttribute.ActionPositionEnum.Above
        )]
        [Label(
            "Color",
            "Gizmo color"
        )]
        [SerializeField]
        private Color m_GizmoColor = CurvyGlobalManager.DefaultGizmoColor;

        [FieldAction(
            "CheckGizmoColor",
            Position = ActionAttribute.ActionPositionEnum.Above
        )]
        [FieldAction(
            "CheckGizmoSelectionColor",
            Position = ActionAttribute.ActionPositionEnum.Below
        )]
        [Label(
            "Active Color",
            "Selected Gizmo color"
        )]
        [SerializeField]
        private Color m_GizmoSelectionColor = CurvyGlobalManager.DefaultGizmoSelectionColor;

        [RangeEx(
            1,
            100
        )]
        [SerializeField, FormerlySerializedAs("Granularity"),
         Tooltip(
             "Defines how densely the cached points are. When the value is 100, the number of cached points per world distance unit is equal to the spline's MaxPointsPerUnit"
         )]
        private int m_CacheDensity = CurvySplineDefaultValues.CacheDensity;

        [SerializeField,
         Tooltip(
             "The maximum number of sampling points per world distance unit. Sampling is used in caching or shape extrusion for example"
         )]
        private float m_MaxPointsPerUnit = CurvySplineDefaultValues.MaxPointsPerUnit;

        [SerializeField]
        [Tooltip("Should the cached tangents be computed in a fast and approximative way, or a slow and precise way?")]
        private CalculationMode tangentCachingMode = CalculationMode.Fast;

        [SerializeField, Tooltip("Use a GameObject pool at runtime")]
        private bool m_UsePooling = CurvySplineDefaultValues.UsePooling;

        [SerializeField,
         Tooltip(
             "Use threading where applicable. Threading is is currently not supported when targetting WebGL and Universal Windows Platform"
         )]
        private bool m_UseThreading;

        [Tooltip("Refresh when Control Point position change?")]
        [SerializeField, FormerlySerializedAs("AutoRefresh")]
        private bool m_CheckTransform = CurvySplineDefaultValues.CheckTransform;

        [SerializeField]
        private CurvyUpdateMethod m_UpdateIn = CurvySplineDefaultValues.UpdateIn;

        #endregion

        #region --- Events ---

#if DOCUMENTATION___FORCE_IGNORE___CURVY == false

        [Group(
            "Events",
            Expanded = false,
            Sort = 1000,
            HelpURL = AssetInformation.DocsRedirectionBaseUrl + "curvyspline_events"
        )]
        [SerializeField]
        protected CurvySplineEvent onInitialized = new CurvySplineEvent();

        [Group(
            "Events",
            Sort = 1000
        )]
        [SerializeField]
        protected CurvySplineEvent m_OnRefresh = new CurvySplineEvent();

        [Group(
            "Events",
            Sort = 1000
        )]
        [SerializeField]
        protected CurvySplineEvent m_OnAfterControlPointChanges = new CurvySplineEvent();

        [Group(
            "Events",
            Sort = 1000
        )]
        [SerializeField]
        protected CurvyControlPointEvent m_OnBeforeControlPointAdd = new CurvyControlPointEvent();

        [Group(
            "Events",
            Sort = 1000
        )]
        [SerializeField]
        protected CurvyControlPointEvent m_OnAfterControlPointAdd = new CurvyControlPointEvent();

        [Group(
            "Events",
            Sort = 1000
        )]
        [SerializeField]
        protected CurvyControlPointEvent m_OnBeforeControlPointDelete = new CurvyControlPointEvent();

#endif

        #endregion

        #endregion

        private Action<CurvySpline> onGlobalCoordinatesChanged;

        #region lifetime

        private bool mIsInitialized;
        private bool isStarted;

        #endregion

        private bool sendOnRefreshEventNextUpdate;

        //OPTIM Instead of having a segments list, use the controlPointsList, while providing the methods to convert from a segment index to a control point index.
        /// <summary>
        /// Controlpoints that start a valid spline segment
        /// </summary>
        private readonly List<CurvySplineSegment> mSegments = new List<CurvySplineSegment>();

        private readonly DirtinessManager dirtinessManager;

        //reusable events
        private readonly RelationshipCache relationshipCache;

        [NotNull]
        private readonly SanityChecker sanityChecker;

        [NotNull]
        private readonly ControlPointsSynchronizer cpsSynchronizer;

        [NotNull]
        private readonly ControlPointNamer controlPointNamer;

        [CanBeNull]
        private TransformMonitor transformMonitor;

        [NotNull]
        private TransformMonitor TransformMonitor
        {
            get
            {
                if (transformMonitor == null)
                    transformMonitor = new TransformMonitor(
                        transform,
                        true,
                        true,
                        true
                    );
                return transformMonitor;
            }
        }


        #region cache

        private Transform cachedTransform;

        /// <summary>
        /// Read-only version of <see cref="ControlPoints"/>
        /// </summary>
        private ReadOnlyCollection<CurvySplineSegment> readOnlyControlPoints;

        /// <summary>
        /// A persistent array of indices, used to avoid frequently allocating indices arrays
        /// </summary>
        private short[] cachedShortsArray = Array.Empty<short>();


        /// <summary>
        /// ControlPointsDistances[i] is equal to ControlPoints[i].Distance. ControlPointsDistances exists only to make search time shorter when searching for a Cp based on its Distance
        /// </summary>
        //TODO optim set its value only when needed?
        private float[] controlPointsDistances = Array.Empty<float>();

        private readonly Action<CurvySplineSegment, int, int> refreshCurveAction;

        [NotNull]
        private readonly List<CurvySplineSegment> cachedListOfSegments = new List<CurvySplineSegment>();

        #region Accumulators

        //fields tha contain the accumulation of segments' properties
        private float length = -1;
        private int mCacheSize = -1;
        private Bounds? mBounds;

        #endregion

        #region events

        private readonly CurvySplineEventArgs defaultSplineEventArgs;
        private readonly CurvyControlPointEventArgs defaultAddAfterEventArgs;
        private readonly CurvyControlPointEventArgs defaultDeleteEventArgs;

        #endregion

        #endregion

        #endregion


        #region ### Unity Callbacks ###

#if DOCUMENTATION___FORCE_IGNORE___UNITY == false
        protected override void OnValidate()
        {
            base.OnValidate();

            MaxPointsPerUnit = m_MaxPointsPerUnit;
            AutoEndTangents = m_AutoEndTangents;

            if (IsActiveAndEnabled)
            {
                relationshipCache.Invalidate();
                SetDirtyAll(
                    SplineDirtyingType.Everything,
                    true
                );
            }
        }

        [UsedImplicitly]
        private void Awake()
        {
            cachedTransform = transform;

            //Debug.Log("Awake " + name);

            if (UsePooling)
                //Create the CurvyGlobalManager if not existing already
                _ = CurvyGlobalManager.Instance;
            //TODO I can't see why it is needed, and it might just create issues more than fixes. I would like to remove this line at some point, but doing so would break the assumption that Curvy users are making until now: a spline in a scene => the global manager is instantiated. I leave the act of breaking this assumption to a later date.
        }

        protected override void OnEnable()
        {
            base.OnEnable();

            HookEditorUpdate();

            if (isStarted)
            {
                bool processedDirtyCps = Initialize();
                if (processedDirtyCps)
                    OnRefreshEvent(defaultSplineEventArgs);
            }
            else
                //so that ControlPoints is valid for all splines when Start() is called. Might not be needed
                SyncSplineFromHierarchy();
        }

        //todo why is it not in api reference?
        /// <summary>
        /// Initialize the spline. This is called automatically by Unity at the first frame.
        /// The only situation I see where you will need to call it manually is if you instantiate a CurvySpline via Unity API, and need to use it the same frame before Unity calls Start() on it the next frame.
        /// </summary>
        public void Start()
        {
            //Debug.Log("Start");
            if (isStarted == false)
            {
                bool processedDirtyCps = Initialize();
                isStarted = true;
                if (processedDirtyCps)
                    OnRefreshEvent(defaultSplineEventArgs);
            }
        }

        protected override void OnDisable()
        {
            base.OnDisable();

            //Debug.Log("OnDisable " + name);
            mIsInitialized = false;
            UnhookEditorUpdate();
            ClearControlPoints(
                false,
                false
            );
        }


        [UsedImplicitly]
        private void OnDestroy()
        {
            if (ShouldUseControlPointPooling(out CurvyGlobalManager curvyGlobalManager))
                PushChildCPsToPool(curvyGlobalManager.ControlPointPool);
            dirtinessManager.Dispose();
            isStarted = false;
        }

#if UNITY_EDITOR
        [UsedImplicitly]
        private void OnTransformChildrenChanged()
        {
            if (IsInitialized == false)
                return;

            //The SynchronizeHierarchyToSpline is meant only to handle the case where the user adds or removes a control point from the editor hierarchy. The addition or removal of a control point through Curvy's API is handled efficiently elsewhere. I said efficiently because, contrary to SynchronizeHierarchyToSpline, it does not lead to rebuilding the whole spline.
            //There is in fact another case where the following code would be useful, which is removing a CP's gameobject through Unity's API. In this case, even if UNITY_EDITOR == false, the syncing would be necessary. But, to not impact the performances of the common user case (using Curvy API to modify CPs), I decided to not handle this case. Removing cps, or adding them, via Unity API is not supported.

            if (
                cpsSynchronizer.CurrentRequest
                == ControlPointsSynchronizer.SynchronizationRequest
                    .None //request is ignored if there is already a SplineToHierarchy request pending, because we consider that Spline to Hierarchy synchronizations are more important than the other way around.
                && IsHierarchyCoherentWithCPsList() == false)
                cpsSynchronizer.RequestHierarchyToSpline();

            controlPointNamer.RequestRename();
        }

#endif

        [UsedImplicitly]
        private void Update()
        {
            if (UpdateIn != CurvyUpdateMethod.Update)
                return;
            if (Application.isPlaying == false)
                return;

            DoUpdate();
        }

        [UsedImplicitly]
        private void LateUpdate()
        {
            if (UpdateIn != CurvyUpdateMethod.LateUpdate)
                return;
            if (Application.isPlaying == false)
                return;

            DoUpdate();
        }

        [UsedImplicitly]
        private void FixedUpdate()
        {
            if (UpdateIn != CurvyUpdateMethod.FixedUpdate)
                return;
            if (Application.isPlaying == false)
                return;

            DoUpdate();
        }
#endif

        #endregion

        #region ### Privates & Internals ###

        #region consts

        /// <summary>
        /// The number of precomputed spline names 
        /// </summary>
        //DESIGN Make this parametrable by users?
        private const short CachedControlPointsNameCount = 250;

        private const float MinimalMaxPointsPerUnit = 0.0001f;

        /// <summary>
        /// The maximal size of the cache of a spline's segment
        /// </summary>
        private const float MaxSegmentCacheSize = 1000000;

        private const string InvalidCPErrorMessage =
            "[Curvy] Method called with a control point '{0}' that is not part of the current spline '{1}'";

        private const int
            MinBSplineDegree =
                2; //update CurvySplineDefaultValues.BSplineDegree and documentation/tooltip of IsBSplineClamped if you modify this

        #endregion

        /// <summary>
        /// Access the list of Segments
        /// </summary>
        /// <remarks>The returned list should not be modified</remarks>
        private List<CurvySplineSegment> Segments
        {
            get
            {
                relationshipCache.EnsureIsValid();
                return mSegments;
            }
        }

        private int MaxBSplineDegree => Mathf.Max(
            MinBSplineDegree,
            ControlPoints.Count - 1
        );


#if CURVY_SANITY_CHECKS
        /// <summary>
        /// Returns isCpsRelationshipCacheValid. Getter was created just for the sake of some sanity checks
        /// </summary>
        internal bool IsCpsRelationshipCacheValidINTERNAL => relationshipCache.IsValid;
#endif


#if UNITY_EDITOR
        [UsedImplicitly]
        [Obsolete]
        public static int
            _newSelectionInstanceIDINTERNAL; // Editor Bridge helper to determine new selection after object deletion
#endif

#if CONTRACTS_FULL
        [ContractInvariantMethod]
        [System.Diagnostics.CodeAnalysis.SuppressMessage(
            "Microsoft.Performance",
            "CA1822:MarkMembersAsStatic",
            Justification =
                "Required for code contracts."
        )]
        private void ObjectInvariant()
        {
            Contract.Invariant(MaxPointsPerUnit.IsANumber());
            Contract.Invariant(MaxPointsPerUnit > 0);

            //TODO CONTRACT reactivate these if you find a way to call GetSegmentIndex and IsSegment without modifying the cache
            //Contract.Invariant(Contract.ForAll(Segments, s => GetSegmentIndex(s) == Segments.IndexOf(s)));
            //Contract.Invariant(Contract.ForAll(Segments, s => IsSegment(s)));

            //TODO CONTRACT more code contracts
            Contract.Invariant(
                Contract.ForAll(
                    ControlPoints,
                    cp => cp.Spline == this
                )
            );
        }
#endif

        [MustUseReturnValue]
        private bool Initialize()
        {
            SetDirtyAll(
                SplineDirtyingType.Everything,
                false
            );
            relationshipCache.Invalidate();

            SyncSplineFromHierarchy();
            controlPointNamer.RequestRename();

            bool processedDirtyCps = dirtinessManager.ProcessDirtyControlPoints();
            TransformMonitor.ResetMonitoring();
            mIsInitialized = true;
            if (OnInitialized != null)
                OnInitialized.Invoke(defaultSplineEventArgs);
            return processedDirtyCps;
        }

        #region editor update

#if UNITY_EDITOR
        private void EditorUpdate()
        {
            if (!IsInitialized)
                return;
            if (Application.isPlaying)
                return;

            DoUpdate();
        }
#endif

        [System.Diagnostics.Conditional(CompilationSymbols.UnityEditor)]
        private void HookEditorUpdate()
        {
#if UNITY_EDITOR
            EditorApplication.update -= EditorUpdate;
            EditorApplication.update += EditorUpdate;
#endif
        }


        [System.Diagnostics.Conditional(CompilationSymbols.UnityEditor)]
        private void UnhookEditorUpdate()
        {
#if UNITY_EDITOR
            EditorApplication.update -= EditorUpdate;
#endif
        }

        #endregion


        private void DoUpdate()
        {
            sanityChecker.OnUpdate();
            cpsSynchronizer.ProcessRequests();
            controlPointNamer.ProcessRequests();

            int controlPointCount = ControlPointCount;
            for (int index = 0; index < controlPointCount; index++)
            {
                CurvySplineSegment controlPoint = ControlPoints[index];
                if (controlPoint.AutoBakeOrientation && controlPoint.UpsApproximation.Count > 0)
                    controlPoint.BakeOrientationToTransform();
            }

            relationshipCache.EnsureIsValid();

            if (TransformMonitor.CheckForChanges())
                ClearBounds();

            if ((CheckTransform || !Application.isPlaying) && dirtinessManager.AllControlPointsAreDirty == false)
                for (int i = 0; i < controlPointCount; i++)
                {
                    CurvySplineSegment currentControlPoint = ControlPoints[i];
                    bool dirtyCurve = currentControlPoint.HasUnprocessedLocalPosition;
                    if (dirtyCurve
                        || (currentControlPoint.HasUnprocessedLocalOrientation
                            && currentControlPoint.OrientationInfluencesSpline))
                        currentControlPoint.Spline.SetDirty(
                            currentControlPoint,
                            dirtyCurve == false
                                ? SplineDirtyingType.OrientationOnly
                                : SplineDirtyingType.Everything
                        );
                }

            if (Dirty)
                Refresh();
            else if (sendOnRefreshEventNextUpdate)
                OnRefreshEvent(defaultSplineEventArgs);

            sendOnRefreshEventNextUpdate = false;

            if (TransformMonitor.HasChanged && OnGlobalCoordinatesChanged != null)
                OnGlobalCoordinatesChanged.Invoke(this);
        }

        private void ClearBounds()
        {
            mBounds = null;
            //OPTIM Right now, transform change lead to recomputing the bounds in world space. This can be avoided by computing the bounds in local space only when the spline is modified, and transform that to the world space here, where a spline transform has changed.
            int controlPointCount = ControlPointCount;
            for (int i = 0; i < controlPointCount; i++)
                ControlPoints[i].ClearBoundsINTERNAL();
        }

        /// <summary>
        /// are manual start/end CP's allowed?
        /// </summary>
        private bool CanHaveManualEndCp()
            => !Closed && (Interpolation == CurvyInterpolation.CatmullRom || Interpolation == CurvyInterpolation.TCB);

        private bool CanBeClamped()
            => !Closed && Interpolation == CurvyInterpolation.BSpline;

        private void ReverseControlPoints()
        {
            ControlPoints.Reverse();
            relationshipCache.Invalidate();
            cpsSynchronizer.RequestSplineToHierarchy();
            SetDirtyAll(
                SplineDirtyingType.Everything,
                IsActiveAndEnabled
            );
        }

        private static short GetNextControlPointIndex(short controlPointIndex, bool isSplineClosed, int controlPointsCount)
        {
            if (isSplineClosed && controlPointsCount <= 1)
                return -1;
            if (controlPointIndex + 1 < controlPointsCount)
                return (short)(controlPointIndex + 1);
            return (short)(isSplineClosed
                ? 0
                : -1);
        }

        private static short GetPreviousControlPointIndex(short controlPointIndex, bool isSplineClosed, int controlPointsCount)
        {
            if (isSplineClosed && controlPointsCount <= 1)
                return -1;
            if (controlPointIndex - 1 >= 0)
                return (short)(controlPointIndex - 1);
            return (short)(isSplineClosed
                ? controlPointsCount - 1
                : -1);
        }

        //OPTIM should you use this instead of the isSegment properties in ControlPointExtrinsicProperties?
        private static bool IsControlPointASegment(int controlPointIndex, int controlPointCount, bool isClosed,
            bool notAutoEndTangentsAndIsCatmullRomOrTCB, bool isBSpline, int bSplineDegree)
        {
#if CONTRACTS_FULL
            Contract.Requires(controlPointIndex >= 0 && controlPointIndex < ControlPointCount);
            Contract.Requires(bSplineDegree >= 0);
#endif

            return (isBSpline == false || bSplineDegree < controlPointCount)
                   && ((isClosed && controlPointCount > 1)
                       || (notAutoEndTangentsAndIsCatmullRomOrTCB
                           ? controlPointIndex > 0 && controlPointIndex < controlPointCount - 2
                           : controlPointIndex < controlPointCount - 1));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsControlPointAnOrientationAnchor(bool isVisible, bool isSerializedOrientationAnchor,
            bool isFirstVisibleControlPoint, bool isLastVisibleControlPoint)
            => isFirstVisibleControlPoint || isLastVisibleControlPoint || (isSerializedOrientationAnchor && isVisible);

        #region Modifying control points list

        private void AddControlPoint([NotNull] CurvySplineSegment item, bool invalidateAndDirty,
            bool requestSplineToHierarchySynchronization)
        {
#if CURVY_SANITY_CHECKS
            if (item == null)
                throw new ArgumentNullException(nameof(item));
#endif
            ControlPoints.Add(item);
            item.LinkToSpline(this);
            if (requestSplineToHierarchySynchronization)
                cpsSynchronizer.RequestSplineToHierarchy();
            if (invalidateAndDirty)
            {
                relationshipCache.Invalidate();
                short previousControlPointIndex = GetPreviousControlPointIndex(
                    (short)(ControlPoints.Count - 1),
                    Closed,
                    ControlPoints.Count
                );
                short nextControlPointIndex = GetNextControlPointIndex(
                    (short)(ControlPoints.Count - 1),
                    Closed,
                    ControlPoints.Count
                );
                dirtinessManager.SetDirty(
                    item,
                    SplineDirtyingType.Everything,
                    previousControlPointIndex != -1
                        ? ControlPoints[previousControlPointIndex]
                        : null,
                    nextControlPointIndex != -1
                        ? ControlPoints[nextControlPointIndex]
                        : null,
                    false
                );
            }
        }

        /// <summary>
        /// Adds a control point at a specific index
        /// </summary>
        /// <param name="index"></param>
        /// <param name="item"></param>
        private void InsertControlPoint(int index, CurvySplineSegment item)
        {
            ControlPoints.Insert(
                index,
                item
            );
            item.LinkToSpline(this);
            relationshipCache.Invalidate();
            cpsSynchronizer.RequestSplineToHierarchy();
            //Dirtying
            {
                short previousControlPointIndex = GetPreviousControlPointIndex(
                    (short)index,
                    Closed,
                    ControlPoints.Count
                );
                short nextControlPointIndex = GetNextControlPointIndex(
                    (short)index,
                    Closed,
                    ControlPoints.Count
                );
                dirtinessManager.SetDirty(
                    item,
                    SplineDirtyingType.Everything,
                    previousControlPointIndex == -1
                        ? null
                        : ControlPoints[previousControlPointIndex],
                    nextControlPointIndex == -1
                        ? null
                        : ControlPoints[nextControlPointIndex],
                    false
                );
            }
        }

        private void RemoveControlPoint(CurvySplineSegment item)
        {
            int indexOftItem = GetControlPointIndex(item);
            //Dirtying
            if (ControlPoints.Count == 1) //Removing the last CP
                SetDirtyAll(
                    SplineDirtyingType.Everything,
                    IsActiveAndEnabled
                );
            else
            {
                //todo can this be replace with SetDirty(item, SplineDirtyingType.Everything); ?
                short previousControlPointIndex = GetPreviousControlPointIndex(
                    (short)indexOftItem,
                    Closed,
                    ControlPoints.Count
                );
                short nextControlPointIndex = GetNextControlPointIndex(
                    (short)indexOftItem,
                    Closed,
                    ControlPoints.Count
                );
                if (previousControlPointIndex != -1)
                    SetDirty(
                        ControlPoints[previousControlPointIndex],
                        SplineDirtyingType.Everything
                    );
                if (nextControlPointIndex != -1)
                    SetDirty(
                        ControlPoints[nextControlPointIndex],
                        SplineDirtyingType.Everything
                    );
            }

            ControlPoints.RemoveAt(indexOftItem);
            dirtinessManager.RemoveFromMinimalSet(item);
            item.UnlinkFromSpline(this);
            relationshipCache.Invalidate();
            cpsSynchronizer.RequestSplineToHierarchy();
        }

        private void ClearControlPoints(bool invalidateAndDirty, bool requestSplineToHierarchySynchronization)
        {
            if (invalidateAndDirty)
                SetDirtyAll(
                    SplineDirtyingType.Everything,
                    IsActiveAndEnabled
                );
            for (int index = 0; index < ControlPoints.Count; index++)
            {
                CurvySplineSegment controlPoint = ControlPoints[index];
                if (controlPoint) //controlPoint can be null if you create a spline via the pen tool, and then undo it
                    controlPoint.UnlinkFromSpline(this);
            }

            ControlPoints.Clear();
            dirtinessManager.ClearMinimalSet();
            if (requestSplineToHierarchySynchronization)
                cpsSynchronizer.RequestSplineToHierarchy();
            if (invalidateAndDirty)
                relationshipCache.Invalidate();
        }

        #endregion


#if DOCUMENTATION___FORCE_IGNORE___CURVY == false

        [UsedImplicitly]
        [Obsolete]
        internal void InvalidateControlPointsRelationshipCacheINTERNAL() =>
            relationshipCache.Invalidate();

        #region Processing dirty control points

        [UsedImplicitly]
        private void UpdateControlPointDistances()
        {
            int controlPointsCount = ControlPoints.Count;

            Array.Resize(
                ref controlPointsDistances,
                controlPointsCount
            );
            controlPointsDistances[0] = ControlPoints[0].Distance = 0;
            for (int i = 1; i < controlPointsCount; i++)
                controlPointsDistances[i] =
                    ControlPoints[i].Distance = ControlPoints[i - 1].Distance + ControlPoints[i - 1].Length;
        }

        private void EnforceTangentContinuity()
        {
            //Because the tangent cache, when using the approximation, needs the position(i+1) to compute tangent(i), and to be compatible with multithreading, we can't compute the tangent of the last point of a segment. So we assign it here.
            List<CurvySplineSegment> segments = Segments;
            int segmentsCount = segments.Count;

            for (int index = 0; index < segmentsCount; index++)
            {
                CurvySplineSegment segment = segments[index];
                segment.TangentsApproximation.Array[segment.CacheSize] =
                    GetNextControlPoint(segment).TangentsApproximation.Array[0];
            }
        }


        /// <summary>
        /// Set the correct values to the thread compatible local positions and rotation
        /// When multithreading, you can't access Transform in the not main threads. Here we cache that data so it is available for threads
        /// </summary>
        private void PrepareThreadCompatibleData()
        {
#if CURVY_SANITY_CHECKS
            Assert.IsTrue(relationshipCache.IsValid);
#endif
            int controlPointsCount = ControlPointCount;
            bool useFollowUp = Interpolation == CurvyInterpolation.CatmullRom || Interpolation == CurvyInterpolation.TCB;

            //prepare the TTransform for all needed control points, which are ....
            // OPTIM: preparing the Threading compatible data of all those CPs is overkill. Restrict the following the prepared CPs to only the CPs being related to the dirtied CPs
            //... all the spline's control points, and ...
            for (int i = 0; i < controlPointsCount; i++)
            {
                CurvySplineSegment controlPoint = ControlPoints[i];
                controlPoint.PrepareThreadCompatibleDataINTERNAL(useFollowUp);
            }

            //... possible other splines' control points because of the followup feature ...
            if (Count > 0)
            {
                CurvySplineSegment beforeFirst = GetPreviousControlPointUsingFollowUp(FirstVisibleControlPoint);
                //before first can be contorlPoints[0] in the case of a spline with AutoEndTangent set to false
                if (ReferenceEquals(
                        beforeFirst,
                        null
                    )
                    == false
                    && beforeFirst.Spline != this)
                    beforeFirst.PrepareThreadCompatibleDataINTERNAL(useFollowUp);
                CurvySplineSegment afterLast = GetNextControlPointUsingFollowUp(LastVisibleControlPoint);
                //afterLast first can be contorlPoints[controlPoints.Count - 1] in the case of a spline with AutoEndTangent set to false
                if (ReferenceEquals(
                        afterLast,
                        null
                    )
                    == false
                    && afterLast.Spline != this)
                    afterLast.PrepareThreadCompatibleDataINTERNAL(useFollowUp);
            }
        }


        /// <summary>
        /// Gets for each control point an index that is:
        /// The index of the control point being the orientation anchor for the anchor group containing the current control point. Is -1 for non visible control points.
        /// </summary>
        private short[] GetOrientationAnchorIndices()
        {
            int controlPointsCount = ControlPoints.Count;
            if (controlPointsCount <= 0)
                return Array.Empty<short>();

            if (cachedShortsArray.Length < controlPointsCount)
                Array.Resize(
                    ref cachedShortsArray,
                    controlPointsCount
                );

            CurvySplineSegment firstVisibleControlPoint = relationshipCache.FirstVisibleControlPoint;
            CurvySplineSegment lastVisibleControlPoint = relationshipCache.LastVisibleControlPoint;

            short lastProcessedOrientationAnchorIndex = -1;
            for (short index = 0; index < controlPointsCount; index++)
            {
                CurvySplineSegment controlPoint = ControlPoints[index];

                bool isVisible = controlPoint.GetExtrinsicPropertiesINTERNAL().IsVisible;

                bool isControlPointAnOrientationAnchor = IsControlPointAnOrientationAnchor(
                    isVisible,
                    controlPoint.SerializedOrientationAnchor,
                    ReferenceEquals(
                        controlPoint,
                        firstVisibleControlPoint
                    ),
                    ReferenceEquals(
                        controlPoint,
                        lastVisibleControlPoint
                    )
                );

                short orientationAnchorIndex;
                {
                    if (isControlPointAnOrientationAnchor)
                        orientationAnchorIndex = index;
                    else
                        orientationAnchorIndex = isVisible
                            ? lastProcessedOrientationAnchorIndex
                            : (short)-1;
                }

                cachedShortsArray[index] = orientationAnchorIndex;

                if (isControlPointAnOrientationAnchor)
                    lastProcessedOrientationAnchorIndex = index;
            }

            return cachedShortsArray;
        }

        #endregion

        private void InvalidateAccumulators()
        {
            mCacheSize = -1;
            length = -1;
            mBounds = null;
        }


        /// <summary>
        /// Call this to make the spline send an event to notify its listeners of the change in the spline data.
        /// </summary>
        internal void NotifyMetaDataModification() =>
            //DESIGN until 2.2.3, meta data change triggered OnRefresh event by dirtying its associated control point. I think spline should have different events (or at least a param in the event) to distinguish between the event coming from an actual change in the spline's geometry, and a change in its meta data.
            sendOnRefreshEventNextUpdate = true;

        /// <summary>
        /// Either destroys or pushes in a pool the given control point
        /// </summary>
        /// <param name="controlPoint">The CP to get rid of</param>
        /// <param name="isUndoable">If true, the destruction of the control point's game object is made undoable (CTRL+Z) in the editor</param>
        private void DisposeOfControlPoint(CurvySplineSegment controlPoint, bool isUndoable)
        {
            if (ShouldUseControlPointPooling(out CurvyGlobalManager curvyGlobalManager))
                curvyGlobalManager.ControlPointPool.Push(controlPoint);
            else
                controlPoint.gameObject.Destroy(
                    isUndoable,
                    true
                );
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="curvyGlobalManager">If returned value is true, this parameter has in it the CurvyGlobalManager singleton instance</param>
        /// <returns></returns>
        private bool ShouldUseControlPointPooling(out CurvyGlobalManager curvyGlobalManager)
        {
            if (!UsePooling || !Application.isPlaying)
            {
                curvyGlobalManager = null;
                return false;
            }

            CurvyGlobalManager singletonInstance = CurvyGlobalManager.Instance;
            if (singletonInstance == null)
            {
                curvyGlobalManager = null;
                return false;
            }

            curvyGlobalManager = singletonInstance;
            return true;
        }


        /// <summary>
        /// Inserts a Control Point, trigger events and refresh spline
        /// </summary>
        /// <param name="beforeEventCP">A control point used as a param of the OnBeforeControlPointAddEvent</param>
        /// <param name="position">The position of the control point at its creation</param>
        /// <param name="insertionIndex">Index at which the newly created control point will be inserted in the spline.</param>
        /// <param name="insertionMode">Used as a param of send events</param>
        /// <param name="skipRefreshingAndEvents">If true, the spline's <see cref="Refresh"/> method will not be called, and the relevant events will not be triggered</param>
        /// <param name="space">Whether the positions are in the local or global space</param>
        /// <returns>The created Control Point</returns>
        private CurvySplineSegment InsertAt([CanBeNull] CurvySplineSegment beforeEventCP, Vector3 position, int insertionIndex,
            CurvyControlPointEventArgs.ModeEnum insertionMode, bool skipRefreshingAndEvents, Space space)
        {
#if CONTRACTS_FULL
            Contract.Requires(controlPoint.Spline == this);
            Contract.Requires(controlPoints.Contains(controlPoint));
#endif

            if (skipRefreshingAndEvents == false)
                OnBeforeControlPointAddEvent(
                    new CurvyControlPointEventArgs(
                        this,
                        this,
                        beforeEventCP,
                        insertionMode
                    )
                );

            CurvySplineSegment newCP = AcquireNewControlPoint();

            newCP.gameObject.layer = gameObject.layer;

            InsertControlPoint(
                insertionIndex,
                newCP
            );
            newCP.AutoHandleDistance = AutoHandleDistance;
            //InsertControlPoint should be called before SetParent, because the former leads to a call to RequestSplineToHierarchy, while the latter leads to a call to RequestHierarchyToSpline. We want to prioritize calls to RequestSplineToHierarchy over RequestHierarchyToSpline
            newCP.transform.SetParent(cachedTransform);
            if (space == Space.World)
                newCP.transform.position = position;
            else
                newCP.transform.localPosition = position;
            newCP.transform.localRotation = Quaternion.identity;
            newCP.transform.localScale = Vector3.one;

            if (skipRefreshingAndEvents == false)
            {
                Refresh();
                OnAfterControlPointAddEvent(
                    new CurvyControlPointEventArgs(
                        this,
                        this,
                        newCP,
                        insertionMode
                    )
                );
                OnAfterControlPointChangesEvent(defaultSplineEventArgs);
            }

            return newCP;
        }

        [NotNull]
        private CurvySplineSegment AcquireNewControlPoint()
        //todo: both branches lead to different names of the game object. Is it an issue?
            => ShouldUseControlPointPooling(out CurvyGlobalManager curvyGlobalManager)
                    ? (CurvySplineSegment)curvyGlobalManager.ControlPointPool.Pop()
                    : GameObjectExt.UndoableCreateGameObject(
                            "NewCP",
                            typeof(CurvySplineSegment)
                        )
                        .GetComponent<CurvySplineSegment>();

        private SubArray<Vector3> GetSegmentApproximationsInSpace(
            [NotNull] Func<CurvySplineSegment, SubArray<Vector3>> approximationGetter,
            Space space)
        {
            SubArray<Vector3> approximations = ConcatenateSegmentApproximations(approximationGetter);
            if (space == Space.World)
                TransformToWorldSpace(approximations);

            return approximations;
        }

        private SubArray<Vector3> ConcatenateSegmentApproximations(
            [NotNull] Func<CurvySplineSegment, SubArray<Vector3>> approximationGetter)
        {
            sanityChecker.Check();

            SubArray<Vector3> result = ArrayPools.Vector3.Allocate(CacheSize + 1);
            Vector3[] resultArray = result.Array;

            int approximationIndex = 0;
            for (int segmentIndex = 0; segmentIndex < Count; segmentIndex++)
            {
                SubArray<Vector3> approximation = approximationGetter(this[segmentIndex]);
                Array.Copy(
                    approximation.Array,
                    0,
                    resultArray,
                    approximationIndex,
                    approximation.Count
                );
                approximationIndex += Mathf.Max(
                    0,
                    approximation.Count - 1
                );
            }

            return result;
        }

        private void TransformToWorldSpace(SubArray<Vector3> localSpaceVectors)
        {
            Matrix4x4 matrix = transform.localToWorldMatrix;
            Vector3[] resultArray = localSpaceVectors.Array;
            for (int i = 0; i < localSpaceVectors.Count; i++)
                resultArray[i] = matrix.MultiplyPoint3x4(resultArray[i]);
        }

        private void PushChildCPsToPool([NotNull] ComponentPool controlPointPool)
        {
            //todo design: Here we look for CPs in the hierarchy, but we don't look for CPs in ControlPoints list. The code assumes that the two lists are the same, and relies on ControlPointsSynchronizer to do keep the assumption true. Would love to get rid of this assumption.
            for (int i = 0; i < transform.childCount; i++)
            {
                CurvySplineSegment childSegment = transform.GetChild(i).GetComponent<CurvySplineSegment>();
                if (childSegment == null)
                    continue;
                controlPointPool.Push(childSegment);
            }
        }

        /// <summary>
        /// Are the CPs in the hierarchy and the ones in the ControlPoints list in sync?
        /// </summary>
        private bool IsHierarchyCoherentWithCPsList()
        {
            bool areChildrenInSync;
            {
                transform.GetComponentsInChildren(cachedListOfSegments);

                if (ControlPointCount != cachedListOfSegments.Count)
                    areChildrenInSync = false;
                else
                {
                    areChildrenInSync = true;
                    for (int i = 0; i < ControlPoints.Count; i++)
                        if (cachedListOfSegments[i] != ControlPoints[i])
                        {
                            areChildrenInSync = false;
                            break;
                        }
                }
            }
            return areChildrenInSync;
        }

        #region Events

        private CurvySplineEventArgs OnRefreshEvent(CurvySplineEventArgs e)
        {
            if (OnRefresh != null)
                OnRefresh.Invoke(e);
            return e;
        }

        private CurvyControlPointEventArgs OnBeforeControlPointAddEvent(CurvyControlPointEventArgs e)
        {
            if (OnBeforeControlPointAdd != null)
                OnBeforeControlPointAdd.Invoke(e);
            return e;
        }

        private CurvyControlPointEventArgs OnAfterControlPointAddEvent(CurvyControlPointEventArgs e)
        {
            if (OnAfterControlPointAdd != null)
                OnAfterControlPointAdd.Invoke(e);
            return e;
        }

        private CurvyControlPointEventArgs OnBeforeControlPointDeleteEvent(CurvyControlPointEventArgs e)
        {
            if (OnBeforeControlPointDelete != null)
                OnBeforeControlPointDelete.Invoke(e);
            return e;
        }

        private CurvySplineEventArgs OnAfterControlPointChangesEvent(CurvySplineEventArgs e)
        {
            if (OnAfterControlPointChanges != null)
                OnAfterControlPointChanges.Invoke(e);
            return e;
        }

        #endregion


        protected override void ResetOnEnable()
        {
            base.ResetOnEnable();
            relationshipCache.Invalidate();
            cpsSynchronizer.CancelRequests();
            controlPointNamer.CancelRequests();
            controlPointsDistances = Array.Empty<float>();
            dirtinessManager.Reset();
            InvalidateAccumulators();
            sendOnRefreshEventNextUpdate = false;
        }
    }
#endif

    #endregion
}