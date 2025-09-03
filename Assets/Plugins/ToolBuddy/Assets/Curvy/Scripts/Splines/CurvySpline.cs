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
using System.Linq;
using System.Reflection;
using FluffyUnderware.Curvy.Pools;
using FluffyUnderware.Curvy.Utils;
using FluffyUnderware.DevTools;
using FluffyUnderware.DevTools.Extensions;
using JetBrains.Annotations;
using ToolBuddy.Pooling.Collections;
using UnityEngine;
using UnityEngine.Assertions;
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
    [HelpURL(AssetInformation.DocsRedirectionBaseUrl + "curvyspline")]
    [AddComponentMenu("Curvy/Curvy Spline")]
    [ExecuteAlways]
    public partial class CurvySpline : DTVersionedMonoBehaviour
    {
#if DOCUMENTATION___FORCE_IGNORE___CURVY == false
        [Obsolete("Use FluffyUnderware.Curvy.AssetInformation instead")]
        public const string VERSION = "8.5.0";

        [Obsolete("Use FluffyUnderware.Curvy.AssetInformation instead")]
        public const string APIVERSION = "850";

        [Obsolete("Use FluffyUnderware.Curvy.AssetInformation instead")]
        public const string WEBROOT = "https://curvyeditor.com/";

        [Obsolete("Use FluffyUnderware.Curvy.AssetInformation instead")]
        public const string DOCLINK = WEBROOT + "doclink/";

#endif

        public CurvySpline()
        {
            sanityChecker = new SanityChecker(this);
            dirtinessManager = new DirtinessManager(this);
            relationshipCache = new RelationshipCache(this);

            refreshCurveAction = (controlPoint, controlPointIndex, controlPointsCount) => controlPoint.refreshCurveINTERNAL();

            defaultSplineEventArgs = new CurvySplineEventArgs(
                this,
                this
            );
            defaultAddAfterEventArgs = new CurvyControlPointEventArgs(
                this,
                this,
                null,
                CurvyControlPointEventArgs.ModeEnum.AddAfter
            );
            defaultDeleteEventArgs = new CurvyControlPointEventArgs(
                this,
                this,
                null,
                CurvyControlPointEventArgs.ModeEnum.Delete
            );
            cpsSynchronizer = new ControlPointsSynchronizer(this);
            controlPointNamer = new ControlPointNamer(this);
        }

        #region ### Serialized fields ###

        /// <summary>
        /// Whether to show the Gizmos enabled in the view settings or not at all 
        /// </summary>
        [HideInInspector]
        public bool ShowGizmos = true;

        #endregion

        #region ### Public Properties ###

        #region --- General ---

        /// <summary>
        /// The interpolation method used by this spline
        /// </summary>
        /// <remarks>AutoEndTangents's value can be updated depending on Interpolation value</remarks>
        public CurvyInterpolation Interpolation
        {
            get => m_Interpolation;
            set
            {
                if (m_Interpolation != value)
                {
                    m_Interpolation = value;
                    relationshipCache.Invalidate();
                    SetDirtyAll(
                        SplineDirtyingType.Everything,
                        false
                    );
                }

                //Since canHaveManualEndCP uses Interpolation, and is used by AutoEndTangents, we force the later's update
                AutoEndTangents = m_AutoEndTangents;
            }
        }

        /// <summary>
        /// Whether to restrict Control Points to a local 2D plane
        /// </summary>
        /// <seealso cref="Restricted2DPlane"/>
        public bool RestrictTo2D
        {
            get => m_RestrictTo2D;
            set
            {
                if (m_RestrictTo2D != value)
                {
                    m_RestrictTo2D = value;
                    SetDirtyAll(
                        SplineDirtyingType.Everything,
                        false
                    );
                }
            }
        }

        /// <summary>
        /// The local 2D plane to restrict the spline's control point in
        /// </summary>
        /// <seealso cref="RestrictTo2D"/>
        public CurvyPlane Restricted2DPlane
        {
            get => restricted2DPlane;
            set
            {
                if (restricted2DPlane != value)
                {
                    restricted2DPlane = value;
                    SetDirtyAll(
                        SplineDirtyingType.Everything,
                        false
                    );
                }
            }
        }

        /// <summary>
        /// Gets or sets the default Handle distance for Bezier splines
        /// </summary>
        public float AutoHandleDistance
        {
            get => m_AutoHandleDistance;
            set
            {
                float clampedValue = Mathf.Clamp01(value);
                if (m_AutoHandleDistance != clampedValue)
                {
                    m_AutoHandleDistance = clampedValue;
                    SetDirtyAll(
                        SplineDirtyingType.Everything,
                        false
                    );
                }
            }
        }

        /// <summary>
        /// Whether this spline is closed or not
        /// </summary>
        /// <remarks>AutoEndTangents's value can be updated depending on Close value</remarks>
        public bool Closed
        {
            get => m_Closed;
            set
            {
                if (m_Closed != value)
                {
                    m_Closed = value;
                    relationshipCache.Invalidate();
                    SetDirtyAll(
                        SplineDirtyingType.Everything,
                        IsActiveAndEnabled
                    );
                }

                //Since canHaveManualEndCP uses Closed, and is used by AutoEndTangents, we force the later's update
                AutoEndTangents = m_AutoEndTangents;
            }
        }

        /// <summary>
        /// Whether the first/last Control Point should act as the end tangent, too.
        /// </summary>
        /// <remarks>Used only by Catmull Rom and TCB splines</remarks>
        public bool AutoEndTangents
        {
            get => m_AutoEndTangents;
            set
            {
                bool v = !CanHaveManualEndCp() || value;
                if (m_AutoEndTangents != v)
                {
                    m_AutoEndTangents = v;
                    relationshipCache.Invalidate();
                    SetDirtyAll(
                        SplineDirtyingType.Everything,
                        IsActiveAndEnabled
                    );
                }
            }
        }

        /// <summary>
        /// Orientation mode
        /// </summary>
        public CurvyOrientation Orientation
        {
            get => m_Orientation;
            set
            {
                if (m_Orientation != value)
                {
                    m_Orientation = value;
                    SetDirtyAll(
                        SplineDirtyingType.Everything,
                        false
                    );
                }
            }
        }

        public CurvyUpdateMethod UpdateIn
        {
            get => m_UpdateIn;
            set => m_UpdateIn = value;
        }

        #endregion

        #region --- Advanced Settings ---

        /// <summary>
        /// Gets or sets Spline color
        /// </summary>
        public Color GizmoColor
        {
            get => m_GizmoColor;
            set => m_GizmoColor = value;
        }

        /// <summary>
        /// Gets or sets selected segment color
        /// </summary>
        public Color GizmoSelectionColor
        {
            get => m_GizmoSelectionColor;
            set => m_GizmoSelectionColor = value;
        }

        /// <summary>
        /// Gets or sets the cache density
        /// Defines how densely the cached points are. When the value is 100, the number of cached points per world distance unit is equal to the spline's <see cref="MaxPointsPerUnit"/>
        /// </summary>
        public int CacheDensity
        {
            get => m_CacheDensity;
            set
            {
                int clampedDensity = Mathf.Clamp(
                    value,
                    1,
                    100
                );

                if (m_CacheDensity != clampedDensity)
                {
                    m_CacheDensity = clampedDensity;
                    SetDirtyAll(
                        SplineDirtyingType.Everything,
                        false
                    );
                }
            }
        }

        /// <summary>
        /// The maximum number of sampling points per world distance unit. Sampling is used in caching or shape extrusion for example</summary>
        public float MaxPointsPerUnit
        {
            get => m_MaxPointsPerUnit;
            set
            {
                float clampedValue = Mathf.Clamp(
                    value,
                    MinimalMaxPointsPerUnit,
                    1000
                );
                if (m_MaxPointsPerUnit != clampedValue)
                {
                    m_MaxPointsPerUnit = clampedValue;
                    SetDirtyAll(
                        SplineDirtyingType.Everything,
                        false
                    );
                }
            }
        }

        /// <summary>
        /// Should the cached tangents be computed in a fast and approximative way, or a slow and precise way?
        /// </summary>
        public CalculationMode TangentCachingMode
        {
            get => tangentCachingMode;
            set
            { 
                if (tangentCachingMode != value)
                {
                    tangentCachingMode = value;
                    SetDirtyAll(
                        SplineDirtyingType.Everything,
                        false
                    );
                }
            }
        }

        /// <summary>
        /// Whether to use GameObject pooling for Control Points at runtime
        /// </summary>
        public bool UsePooling
        {
            get => m_UsePooling;
            set => m_UsePooling = value;
        }

        /// <summary>
        /// Whether to use threading where applicable or not.
        /// Threading is currently not supported when targeting WebGL and Universal Windows Platform
        /// </summary>
        public bool UseThreading
        {
            get => m_UseThreading;
            set => m_UseThreading = value;
        }

        /// <summary>
        /// Whether the spline should automatically refresh when a Control Point's position change
        /// </summary>
        /// <remarks>Enable this if you animate a Control Point's transform!</remarks>
        public bool CheckTransform
        {
            get => m_CheckTransform;
            set
            {
                if (m_CheckTransform != value)
                {
                    m_CheckTransform = value;
                    SetDirtyAll(
                        SplineDirtyingType.Everything,
                        false
                    );
                }
            }
        }

        #endregion

        #region --- TCB Options ---

        /// <summary>
        /// Global Tension
        /// </summary>
        /// <remarks>This only applies to TCB interpolation</remarks>
        public float Tension
        {
            get => m_Tension;
            set
            {
                if (m_Tension != value)
                {
                    m_Tension = value;
                    SetDirtyAll(
                        SplineDirtyingType.Everything,
                        false
                    );
                }
            }
        }

        /// <summary>
        /// Global Continuity
        /// </summary>
        /// <remarks>This only applies to TCB interpolation</remarks>
        public float Continuity
        {
            get => m_Continuity;
            set
            {
                if (m_Continuity != value)
                {
                    m_Continuity = value;
                    SetDirtyAll(
                        SplineDirtyingType.Everything,
                        false
                    );
                }
            }
        }

        /// <summary>
        /// Global Bias
        /// </summary>
        /// <remarks>This only applies to TCB interpolation</remarks>
        public float Bias
        {
            get => m_Bias;
            set
            {
                if (m_Bias != value)
                {
                    m_Bias = value;
                    SetDirtyAll(
                        SplineDirtyingType.Everything,
                        false
                    );
                }
            }
        }

        #endregion

        #region --- B-Spline Options ---

        /// <summary>
        /// Used only when <see cref="Interpolation"/> is <see cref="CurvyInterpolation.BSpline"/>
        /// The degree of the piecewise polynomial functions
        /// Is in the range [2; control points count - 1]
        /// </summary>
        public int BSplineDegree
        {
            get => bSplineDegree;
            set
            {
                value = Mathf.Min(
                    Mathf.Max(
                        MinBSplineDegree,
                        value
                    ),
                    MaxBSplineDegree
                );
                if (bSplineDegree != value)
                {
                    bSplineDegree = value;
                    SetDirtyAll(
                        SplineDirtyingType.Everything,
                        false
                    );
                }
            }
        }

        /// <summary>
        /// Used only when <see cref="Interpolation"/> is <see cref="CurvyInterpolation.BSpline"/>
        /// Make the curve pass through the first and last control points by increasing the multiplicity of the first and last knots.
        /// In technical terms, when this parameter is true, the knot vector is [0, 0, ...,0, 1, 2, ..., N-1, N, N, ..., N]. When false, it is [0, 1, 2, ..., N-1, N]
        /// </summary>
        public bool IsBSplineClamped
        {
            get => CanBeClamped() && isBSplineClamped;
            set
            {
                if (isBSplineClamped != value)
                {
                    isBSplineClamped = value;
                    SetDirtyAll(
                        SplineDirtyingType.Everything,
                        false
                    );
                }
            }
        }

        #endregion

        #region --- Others ---

        /// <summary>
        /// Whether the spline is fully initialized and all segments loaded
        /// </summary>
        public bool IsInitialized => mIsInitialized;

        /// <summary>
        /// The bounding box of the spline, in world space
        /// </summary>
        public Bounds Bounds
        {
            get
            {
                if (!mBounds.HasValue)
                {
                    sanityChecker.Check();

                    Bounds bounds;
                    if (Count > 0)
                    {
                        Bounds b = this[0].Bounds;

                        for (int i = 1; i < Count; i++)
                            b.Encapsulate(this[i].Bounds);
                        bounds = b;
                    }
                    else
                        bounds = new Bounds(
                            transform.position,
                            Vector3.zero
                        );

                    if (Dirty == false)
                        mBounds = bounds;

                    return bounds;
                }

                return mBounds.Value;
            }
        }

        /// <summary>
        /// Gets the number of Segments
        /// </summary>
        public int Count => Segments.Count;

        /// <summary>
        /// Gets the number of Control Points
        /// </summary>
        public int ControlPointCount => ControlPoints.Count;

        /// <summary>
        /// Gets total Cache Size
        /// </summary>
        public int CacheSize
        {
            get
            {
                if (mCacheSize < 0)
                {
                    sanityChecker.Check();

                    int cacheSize = 0;
                    List<CurvySplineSegment> segments = Segments;
                    for (int i = 0; i < segments.Count; i++)
                        cacheSize += segments[i].CacheSize;

                    if (Dirty == false)
                        mCacheSize = cacheSize;

                    return cacheSize;
                }

                return mCacheSize;
            }
        }

        /// <summary>
        /// Gets the total length of the Spline
        /// </summary>
        /// <remarks>The accuracy depends on the current Granularity (higher Granularity means more exact values)</remarks>
        public float Length
        {
            get
            {
                if (length < 0)
                {
                    sanityChecker.Check();

                    float tempLength;
                    if (Segments.Count == 0)
                        tempLength = 0;
                    else
                        tempLength = Closed
                            ? this[Count - 1].Distance + this[Count - 1].Length
                            : LastVisibleControlPoint.Distance;

                    if (Dirty == false)
                        length = tempLength;

                    return tempLength;
                }

                return length;
            }
        }

        /// <summary>
        /// When a spline is dirty, this means that it's cached data is no more up to date, and should be updated. The update is done automatically each frame when needed, or manually by calling <see cref="Refresh"/>
        /// </summary>
        public bool Dirty => dirtinessManager.Dirty;

        /// <summary>
        /// Gets the Segment at a certain index
        /// </summary>
        /// <param name="idx">an index in the range 0..Count</param>
        /// <returns>the corresponding spline segment</returns>
        public CurvySplineSegment this[int idx]
        {
            get
            {
#if CONTRACTS_FULL
                Contract.Requires(idx > -1 && idx < Segments.Count);
#endif
                return Segments[idx];
            }
        }

        /// <summary>
        /// The list of control points
        /// </summary>
        public ReadOnlyCollection<CurvySplineSegment> ControlPointsList
        {
            //TODO use IReadOnlyList when .NET 4.6 will be default
            get
            {
                //OPTIM find the proper place to initialize readOnlyControlPoints, to avoid the if bellow to be tested at each call.
                //Note: even when initializing readOnlyControlPoints in Awake, OnEnable, OnAfterDeserialize  and OnValidate, scene 25 had a null reference exception on readOnlyControlPoints. Here is the stack trace:
                /*
                NullReferenceException: Object reference not set to an instance of an object
                FluffyUnderware.Curvy.CurvySpline.GetControlPointIndex (FluffyUnderware.Curvy.CurvySplineSegment controlPoint) (at Assets/Packages/Curvy/Base/CurvySpline.cs:254)
                FluffyUnderware.Curvy.CurvySplineSegment.GetPreviousControlPoint (System.Boolean segmentsOnly, System.Boolean useFollowUp) (at Assets/Packages/Curvy/Base/CurvySplineSegment.cs:1552)
                FluffyUnderware.Curvy.CurvySplineSegment.SetDirty (System.Boolean dirtyCurve, System.Boolean dirtyOrientation) (at Assets/Packages/Curvy/Base/CurvySplineSegment.cs:2148)
                FluffyUnderware.Curvy.CurvyMetadataBase.SetDirty () (at Assets/Packages/Curvy/Base/CurvyMetadataBase.cs:84)
                FluffyUnderware.Curvy.MetaCGOptions.OnValidate () (at Assets/Packages/Curvy/Base/CG/MetaCGOptions.cs:239)
                */
                if (readOnlyControlPoints == null)
                    readOnlyControlPoints = ControlPoints.AsReadOnly();
                return readOnlyControlPoints;
            }
        }


        /// <summary>
        /// Gets the first visible Control Point (equals the first segment or this[0])
        /// </summary>
        /// <remarks>Can be null, for example for a Catmull-Rom spline whith only two splines and AutoEndTangent set to false</remarks>
        [CanBeNull]
        public CurvySplineSegment FirstVisibleControlPoint => relationshipCache.FirstVisibleControlPoint;

        /// <summary>
        /// Gets the last visible Control Point (i.e. the end CP of the last segment)
        /// </summary>
        /// <remarks>Is null if spline has no segments</remarks>
        [CanBeNull]
        public CurvySplineSegment LastVisibleControlPoint => relationshipCache.LastVisibleControlPoint;

        /// <summary>
        /// Gets the first segment of the spline
        /// </summary>
        [CanBeNull]
        public CurvySplineSegment FirstSegment => relationshipCache.FirstSegment;

        /// <summary>
        /// Gets the last segment of the spline
        /// </summary>
        [CanBeNull]
        public CurvySplineSegment LastSegment => relationshipCache.LastSegment;

        /// <summary>
        /// Returns true if the global position, rotation or scale of the spline has changed this frame
        /// </summary>
        /// <seealso cref="OnGlobalCoordinatesChanged"/>
        public bool GlobalCoordinatesChangedThisFrame => TransformMonitor.HasChanged;

        /// <summary>
        /// Is triggered when the global position, rotation or scale of the spline changes.
        /// The triggering instance of CurvySpline is passed as a parameter of the delegate
        /// </summary>
        /// <remarks>This is triggered at the very end of the spline updating method. <see cref="UpdateIn"/></remarks>
        /// <seealso cref="GlobalCoordinatesChangedThisFrame"/>
        [CanBeNull]
        public Action<CurvySpline> OnGlobalCoordinatesChanged
        {
            get => onGlobalCoordinatesChanged;
            set => onGlobalCoordinatesChanged = value;
        }

        /// <summary>
        /// Event triggered when the spline is updated, including its initial update
        /// </summary>
        public CurvySplineEvent OnRefresh
        {
            get => m_OnRefresh;
            set => m_OnRefresh = value;
        }

        /// <summary>
        /// Event triggered when the spline is initialized
        /// </summary>
        public CurvySplineEvent OnInitialized
        {
            get => onInitialized;
            set => onInitialized = value;
        }

        /// <summary>
        /// Callback after one or more Control Points have been added or deleted
        /// </summary>
        /// <remarks>This executes last, after individual add/delete events and OnRefresh </remarks>
        public CurvySplineEvent OnAfterControlPointChanges
        {
            get => m_OnAfterControlPointChanges;
            set => m_OnAfterControlPointChanges = value;
        }

        /// <summary>
        /// Callback before a Control Point is about to be added
        /// </summary>
        public CurvyControlPointEvent OnBeforeControlPointAdd
        {
            get => m_OnBeforeControlPointAdd;
            set => m_OnBeforeControlPointAdd = value;
        }

        /// <summary>
        /// Callback after a Control Point has been added and the spline was refreshed
        /// </summary>
        public CurvyControlPointEvent OnAfterControlPointAdd
        {
            get => m_OnAfterControlPointAdd;
            set => m_OnAfterControlPointAdd = value;
        }

        /// <summary>
        /// Callback before a Control Point is about to be deleted. Return false to cancel the execution.
        /// </summary>
        public CurvyControlPointEvent OnBeforeControlPointDelete
        {
            get => m_OnBeforeControlPointDelete;
            set => m_OnBeforeControlPointDelete = value;
        }

        #endregion

        #endregion

        #region ### Public Static Methods ###

        /// <summary>
        /// Creates an empty spline
        /// </summary>
        public static CurvySpline Create()
        {
            //todo use UndoableCreateGameObject?
            CurvySpline spl = new GameObject(
                "Curvy Spline",
                typeof(CurvySpline)
            ).GetComponent<CurvySpline>();
            spl.gameObject.layer = CurvyGlobalManager.SplineLayer;
            spl.Start();
            return spl;
        }

        /// <summary>
        /// Creates an empty spline with the same settings as another spline
        /// </summary>
        /// <param name="takeOptionsFrom">another spline</param>
        public static CurvySpline Create(CurvySpline takeOptionsFrom)
        {
            CurvySpline spl = Create();
            if (takeOptionsFrom)
            {
                spl.RestrictTo2D = takeOptionsFrom.RestrictTo2D;
                spl.GizmoColor = takeOptionsFrom.GizmoColor;
                spl.GizmoSelectionColor = takeOptionsFrom.GizmoSelectionColor;
                spl.Interpolation = takeOptionsFrom.Interpolation;
                spl.Closed = takeOptionsFrom.Closed;
                spl.AutoEndTangents = takeOptionsFrom.AutoEndTangents;
                spl.CacheDensity = takeOptionsFrom.CacheDensity;
                spl.MaxPointsPerUnit = takeOptionsFrom.MaxPointsPerUnit;
                spl.Orientation = takeOptionsFrom.Orientation;
                spl.CheckTransform = takeOptionsFrom.CheckTransform;
            }

            return spl;
        }

        /// <summary>
        /// Gets the number of Cache Points needed for a certain part of a spline
        /// </summary>
        /// <param name="density">A value between 1 and 100 included. When equal to 100, the number of cache points per world distance unit is equal to maxPointsPerUnit</param>
        /// <param name="segmentLength">the length of the spline segment</param>
        /// <param name="maxPointsPerUnit">Maximum number of Cache Points per world distance unit</param>
        public static int CalculateCacheSize(int density, float segmentLength, float maxPointsPerUnit)
        {
#if CONTRACTS_FULL
            Contract.Requires(CodeContractsUtility.IsPositiveNumber(segmentLength));
#endif
            //This basically equals to Mathf.FloorToInt(length * (maxPointsPerUnit * (density - 1) / 99)² + MinimalMaxPointsPerUnit) + 1
            //Here is a plot of (density - 1) / 99)²
            //https://www.wolframalpha.com/input/?i=plot+((x+-+1)+%2F+99+)%5E2+for+x+from+1+to+100
            float samplePoints = CalculateSamplingPointsPerUnit(
                                     density,
                                     maxPointsPerUnit
                                 )
                                 * segmentLength;
            samplePoints = Math.Min(
                samplePoints,
                MaxSegmentCacheSize - 1
            );
            return Mathf.FloorToInt(samplePoints) + 1;
        }

        /// <summary>
        /// Returns the (floating) number of sampling points per world distance unit.
        /// </summary>
        /// <param name="density">A value between 1 and 100 included. When equal to 100, the number of sampling points per world distance unit is equal to maxPointsPerUnit</param>
        /// <param name="maxPointsPerUnit">Maximum number of sampling points per world distance unit</param>
        /// <returns></returns>
        public static float CalculateSamplingPointsPerUnit(int density, float maxPointsPerUnit)
        {
#if CONTRACTS_FULL
            Contract.Requires(density > 0);
            Contract.Requires(density <= 100);
            Contract.Requires(maxPointsPerUnit.IsPositiveNumber());
#endif
            int clampedDensity = Mathf.Clamp(
                density,
                1,
                100
            );
            if (clampedDensity != density)
            {
                DTLog.LogWarning(
                    "[Curvy] CalculateSamplingPointsPerUnit got an invalid density parameter. It should be between 1 and 100. The parameter value was "
                    + density
                );
                density = clampedDensity;
            }

            //This basically equals to (maxPointsPerUnit * (density - 1) / 99)² + MinimalMaxPointsPerUnit)
            //Here is a plot of (density - 1) / 99)²
            //https://www.wolframalpha.com/input/?i=plot+((x+-+1)+%2F+99+)%5E2+for+x+from+1+to+100
            return DTTween.QuadIn(
                density - 1,
                MinimalMaxPointsPerUnit,
                maxPointsPerUnit,
                99
            );
        }


        /// <summary>
        /// This method returns the Control Point next to the Follow-Up, based on the ConnectionHeadingEnum's value
        /// </summary>
        /// <param name="followUp">The Control Point used as a Follow-Up</param>
        /// <param name="headingDirection">The head to direction</param>
        /// <returns>The Control Point the Follow-Up is heading to </returns>
        public static CurvySplineSegment GetFollowUpHeadingControlPoint([NotNull] CurvySplineSegment followUp,
            ConnectionHeadingEnum headingDirection)
        {
#if CURVY_SANITY_CHECKS
            Assert.IsTrue(followUp != null);
            Assert.IsTrue(followUp.Spline != null);
#endif
            ConnectionHeadingEnum resolveHeading = headingDirection.ResolveAuto(followUp);
            CurvySplineSegment result;
            switch (resolveHeading)
            {
                case ConnectionHeadingEnum.Minus:
                    result = followUp.Spline.GetPreviousControlPoint(followUp);
                    break;
                case ConnectionHeadingEnum.Plus:
                    result = followUp.Spline.GetNextControlPoint(followUp);
                    break;
                case ConnectionHeadingEnum.Sharp:
                    result = followUp;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            return result;
        }

        #endregion

        #region ### Public Methods ###

        #region --- Methods based on TF (total fragment) ---

        /// <summary>
        /// Gets the position of a point on the spline segment
        /// </summary>
        /// <param name="tf">TF stands for Total Fragment. It's a value ranging from 0 to 1 inclusive. 0 means the spline's start and 1 means the spline's end.This is the "time" parameter used in the splines' formulas. A point's F is not proportional to its distance from the spline's start. Depending on the spline, a value of 0.5 does not always mean the middle, distance wise, of the spline</param>
        /// <param name="space">The space (local/object or global/world) in which the returned result is expressed</param>
        public Vector3 Interpolate(float tf, Space space = Space.Self)
        {
            sanityChecker.Check();

            float localF;
            CurvySplineSegment seg = TFToSegment(
                tf,
                out localF
            );
            if (ReferenceEquals(
                    seg,
                    null
                )
                == false)
                return seg.Interpolate(
                    localF,
                    space
                );

            return space == Space.Self
                ? Vector3.zero
                : cachedTransform.position;
        }

        /// <summary>
        /// Gets the position of a point on the spline segment.
        /// Instead of computing the exact value, this method uses a linear interpolation between cached points for faster result
        /// </summary>
        /// <param name="tf">TF stands for Total Fragment. It's a value ranging from 0 to 1 inclusive. 0 means the spline's start and 1 means the spline's end. This is the "time" parameter used in the splines' formulas. A point's TF is not proportional to its distance from the spline's start. Depending on the spline, a value of 0.5 does not always mean the middle, distance wise, of the spline</param>
        /// <param name="space">The space (local/object or global/world) in which the returned result is expressed</param>
        public Vector3 InterpolateFast(float tf, Space space = Space.Self)
        {
            sanityChecker.Check();

            float localF;
            CurvySplineSegment seg = TFToSegment(
                tf,
                out localF
            );
            if (ReferenceEquals(
                    seg,
                    null
                )
                == false)
                return seg.InterpolateFast(
                    localF,
                    space
                );

            return space == Space.Self
                ? Vector3.zero
                : cachedTransform.position;
        }

        /// <summary>
        /// Gets the position of a point on the spline segment
        /// </summary>
        /// <param name="distance">The distance between the spline's start and the point you are interested in. Value should be in the range from 0 to <see cref="Length"/> inclusive</param>
        /// <param name="space">The space (local/object or global/world) in which the returned result is expressed</param>
        public Vector3 InterpolateByDistance(float distance, Space space = Space.Self)
            => Interpolate(
                DistanceToTF(distance),
                space
            );

        /// <summary>
        /// Gets the position of a point on the spline segment.
        /// Instead of computing the exact value, this method uses a linear interpolation between cached points for faster result
        /// </summary>
        /// <param name="distance">The distance between the spline's start and the point you are interested in. Value should be in the range from 0 to <see cref="Length"/> inclusive</param>
        /// <param name="space">The space (local/object or global/world) in which the returned result is expressed</param>
        public Vector3 InterpolateByDistanceFast(float distance, Space space = Space.Self)
            => InterpolateFast(
                DistanceToTF(distance),
                space
            );

        /// <summary>
        /// Gets the normalized tangent at a point on the spline segment
        /// </summary>
        /// <param name="tf">TF stands for Total Fragment. It's a value ranging from 0 to 1 inclusive. 0 means the spline's start and 1 means the spline's end. This is the "time" parameter used in the splines' formulas. A point's TF is not proportional to its distance from the spline's start. Depending on the spline, a value of 0.5 does not always mean the middle, distance wise, of the spline</param>
        /// <param name="space">The space (local/object or global/world) in which the returned result is expressed</param>
        public Vector3 GetTangent(float tf, Space space = Space.Self)
        {
            sanityChecker.Check();

            float localF;
            CurvySplineSegment seg = TFToSegment(
                tf,
                out localF
            );
            if (ReferenceEquals(
                    seg,
                    null
                )
                == false)
                return seg.GetTangent(
                    localF,
                    space
                );

            return space == Space.Self
                ? Vector3.zero
                : cachedTransform.position;
        }

        /// <summary>
        /// Gets the normalized tangent at a point on the spline segment.
        /// This method is faster than <see cref="GetTangent(float, Space)"/> if you have already the position of the point.
        /// </summary>
        /// <param name="tf">TF stands for Total Fragment. It's a value ranging from 0 to 1 inclusive. 0 means the spline's start and 1 means the spline's end. This is the "time" parameter used in the splines' formulas. A point's TF is not proportional to its distance from the spline's start. Depending on the spline, a value of 0.5 does not always mean the middle, distance wise, of the spline</param>
        /// <param name="position">the position of the point at localF. In other words, the result of <see cref="Interpolate(float, Space)"/></param>
        /// <param name="space">The space (local/object or global/world) in which the returned result and the <paramref name="position"/> parameter are expressed</param>
        [Obsolete("This method returns approximated values. Use GetTangent(float,Space), which is more precise and faster.")]
        public Vector3 GetTangent(float tf, Vector3 position, Space space = Space.Self)
        {
            sanityChecker.Check();

            float localF;
            CurvySplineSegment seg = TFToSegment(
                tf,
                out localF
            );
            if (ReferenceEquals(
                    seg,
                    null
                )
                == false)
                return seg.GetTangent(
                    localF,
                    position,
                    space
                );

            return space == Space.Self
                ? Vector3.zero
                : cachedTransform.position;
        }

        /// <summary>
        /// Gets the normalized tangent at a point on the spline segment.
        /// Instead of computing the exact value, this method uses a linear interpolation between cached points for faster result
        /// </summary>
        /// <param name="tf">TF stands for Total Fragment. It's a value ranging from 0 to 1 inclusive. 0 means the spline's start and 1 means the spline's end. This is the "time" parameter used in the splines' formulas. A point's TF is not proportional to its distance from the spline's start. Depending on the spline, a value of 0.5 does not always mean the middle, distance wise, of the spline</param>
        /// <param name="space">The space (local/object or global/world) in which the returned result is expressed</param>
        public Vector3 GetTangentFast(float tf, Space space = Space.Self)
        {
            sanityChecker.Check();

            float localF;
            CurvySplineSegment seg = TFToSegment(
                tf,
                out localF
            );
            if (ReferenceEquals(
                    seg,
                    null
                )
                == false)
                return seg.GetTangentFast(
                    localF,
                    space
                );

            return space == Space.Self
                ? Vector3.zero
                : cachedTransform.position;
        }

        /// <summary>
        /// Gets the normalized tangent at a point on the spline segment
        /// </summary>
        /// <param name="distance">The distance between the spline's start and the point you are interested in. Value should be in the range from 0 to <see cref="Length"/> inclusive</param>
        /// <param name="space">The space (local/object or global/world) in which the returned result is expressed</param>
        public Vector3 GetTangentByDistance(float distance, Space space = Space.Self)
            => GetTangent(
                DistanceToTF(distance),
                space
            );

        /// <summary>
        /// Gets the normalized tangent at a point on the spline segment.
        /// Instead of computing the exact value, this method uses a linear interpolation between cached points for faster result
        /// </summary>
        /// <param name="distance">The distance between the spline's start and the point you are interested in. Value should be in the range from 0 to <see cref="Length"/> inclusive</param>
        /// <param name="space">The space (local/object or global/world) in which the returned result is expressed</param>
        public Vector3 GetTangentByDistanceFast(float distance, Space space = Space.Self)
            => GetTangentFast(
                DistanceToTF(distance),
                space
            );

        /// <summary>
        /// Gets the position and normalized tangent at a point on the spline segment
        /// Is Faster than calling <see cref="Interpolate(float, Space)"/> and <see cref="Interpolate(float, Space)"/> separately
        /// </summary>
        /// <param name="tf">TF stands for Total Fragment. It's a value ranging from 0 to 1 inclusive. 0 means the spline's start and 1 means the spline's end. This is the "time" parameter used in the splines' formulas. A point's TF is not proportional to its distance from the spline's start. Depending on the spline, a value of 0.5 does not always mean the middle, distance wise, of the spline</param>
        /// <param name="position">the output position</param>
        /// <param name="tangent">the output tangent</param>
        /// <param name="space">The space (local/object or global/world) in which the returned result is expressed</param>
        public void InterpolateAndGetTangent(float tf, out Vector3 position, out Vector3 tangent, Space space = Space.Self)
        {
            sanityChecker.Check();

            float localF;
            CurvySplineSegment seg = TFToSegment(
                tf,
                out localF
            );
            if (ReferenceEquals(
                    seg,
                    null
                )
                == false)
            {
                position = seg.Interpolate(
                    localF,
                    space
                );
                tangent = seg.GetTangent(
                    localF,
                    space
                );
            }
            else
                position = tangent = space == Space.Self
                    ? Vector3.zero
                    : cachedTransform.position;
        }

        /// <summary>
        /// Gets the position and normalized tangent at a point on the spline segment
        /// Is Faster than calling <see cref="Interpolate(float, Space)"/> and <see cref="Interpolate(float, Space)"/> separately
        /// Instead of computing the exact value, this method uses a linear interpolation between cached points for faster result
        /// </summary>
        /// <param name="tf">TF stands for Total Fragment. It's a value ranging from 0 to 1 inclusive. 0 means the spline's start and 1 means the spline's end. This is the "time" parameter used in the splines' formulas. A point's TF is not proportional to its distance from the spline's start. Depending on the spline, a value of 0.5 does not always mean the middle, distance wise, of the spline</param>
        /// <param name="position">the output position</param>
        /// <param name="tangent">the output tangent</param>
        /// <param name="space">The space (local/object or global/world) in which the returned result is expressed</param>
        public void InterpolateAndGetTangentFast(float tf, out Vector3 position, out Vector3 tangent, Space space = Space.Self)
        {
            sanityChecker.Check();

            float localF;
            CurvySplineSegment seg = TFToSegment(
                tf,
                out localF
            );
            if (ReferenceEquals(
                    seg,
                    null
                )
                == false)
                seg.InterpolateAndGetTangentFast(
                    localF,
                    out position,
                    out tangent,
                    space
                );
            else
                position = tangent = space == Space.Self
                    ? Vector3.zero
                    : cachedTransform.position;
        }

        /// <summary>
        /// Gets the Up vector of a point on the spline segment.
        /// Instead of computing the exact value, this method uses a linear interpolation between cached points for faster result
        /// </summary>
        /// <param name="tf">TF stands for Total Fragment. It's a value ranging from 0 to 1 inclusive. 0 means the spline's start and 1 means the spline's end. This is the "time" parameter used in the splines' formulas. A point's TF is not proportional to its distance from the spline's start. Depending on the spline, a value of 0.5 does not always mean the middle, distance wise, of the spline</param>
        /// <param name="space">The space (local/object or global/world) in which the returned result is expressed</param>
        public Vector3 GetOrientationUpFast(float tf, Space space = Space.Self)
        {
            sanityChecker.Check();

            float localF;
            CurvySplineSegment seg = TFToSegment(
                tf,
                out localF
            );
            if (ReferenceEquals(
                    seg,
                    null
                )
                == false)
                return seg.GetOrientationUpFast(
                    localF,
                    space
                );

            return space == Space.Self
                ? Vector3.zero
                : cachedTransform.position;
        }

        /// <summary>
        /// Gets a rotation looking to Tangent with the head upwards along the Up-Vector
        /// </summary>
        /// <param name="tf">TF stands for Total Fragment. It's a value ranging from 0 to 1 inclusive. 0 means the spline's start and 1 means the spline's end. This is the "time" parameter used in the splines' formulas. A point's TF is not proportional to its distance from the spline's start. Depending on the spline, a value of 0.5 does not always mean the middle, distance wise, of the spline</param>
        /// <param name="inverse">whether the orientation should be inversed or not</param>
        /// <param name="space">The space (local/object or global/world) in which the returned result is expressed</param>
        /// <returns>a rotation, relative to the spline's local space</returns>
        public Quaternion GetOrientationFast(float tf, bool inverse = false, Space space = Space.Self)
        {
            sanityChecker.Check();

            float localF;
            CurvySplineSegment seg = TFToSegment(
                tf,
                out localF
            );
            if (ReferenceEquals(
                    seg,
                    null
                )
                == false)
                return seg.GetOrientationFast(
                    localF,
                    inverse,
                    space
                );

            return space == Space.Self
                ? Quaternion.identity
                : cachedTransform.rotation;
        }

        /// <summary>
        /// Gets metadata for a certain TF
        /// </summary>
        /// <typeparam name="T">Metadata type interfacing ICurvyMetadata</typeparam>
        /// <param name="tf">TF stands for Total Fragment. It's a value ranging from 0 to 1 inclusive. 0 means the spline's start and 1 means the spline's end. This is the "time" parameter used in the splines' formulas. A point's TF is not proportional to its distance from the spline's start. Depending on the spline, a value of 0.5 does not always mean the middle, distance wise, of the spline</param>
        /// <returns>the metadata</returns>
        public T GetMetadata<T>(float tf) where T : CurvyMetadataBase
        {
            sanityChecker.Check();

            float localF;
            CurvySplineSegment seg = TFToSegment(
                tf,
                out localF
            );
            return ReferenceEquals(
                       seg,
                       null
                   )
                   == false
                ? seg.GetMetadata<T>()
                : null;
        }


        /// <summary>
        /// Gets an interpolated Metadata value for a certain TF
        /// </summary>
        /// <typeparam name="T">Metadata type inheriting from CurvyInterpolatableMetadataBase</typeparam>
        /// <typeparam name="U">Metadata's Value type</typeparam>
        /// <param name="tf">TF stands for Total Fragment. It's a value ranging from 0 to 1 inclusive. 0 means the spline's start and 1 means the spline's end. This is the "time" parameter used in the splines' formulas. A point's TF is not proportional to its distance from the spline's start. Depending on the spline, a value of 0.5 does not always mean the middle, distance wise, of the spline</param>
        /// <returns>The interpolated value. If no Metadata of specified type is present at the given tf, the default value of type U is returned</returns>
        public U GetInterpolatedMetadata<T, U>(float tf) where T : CurvyInterpolatableMetadataBase<U>
        {
            sanityChecker.Check();

            float localF;
            CurvySplineSegment seg = TFToSegment(
                tf,
                out localF
            );
            return ReferenceEquals(
                       seg,
                       null
                   )
                   == false
                ? seg.GetInterpolatedMetadata<T, U>(localF)
                : default;
        }

        #endregion

        #region --- Conversion Methods ---

        /// <summary>
        /// Converts a TF value to a distance
        /// </summary>
        /// <param name="tf">TF stands for Total Fragment. It's a value ranging from 0 to 1 inclusive. 0 means the spline's start and 1 means the spline's end.This is the "time" parameter used in the splines' formulas. A point's F is not proportional to its distance from the spline's start. Depending on the spline, a value of 0.5 does not always mean the middle, distance wise, of the spline</param>
        /// <param name="clamping">Clamping to use</param>
        /// <returns>distance from spline's start</returns>
        public float TFToDistance(float tf, CurvyClamping clamping = CurvyClamping.Clamp)
        {
            sanityChecker.Check();
            float splineLength = Length;

            float result;
            if (splineLength == 0)
                result = 0;
            else if (tf == 0)
                result = 0;
            else if (tf == 1)
                result = splineLength;
            else
            {
                float localF;
                CurvySplineSegment seg = TFToSegment(
                    tf,
                    out localF,
                    clamping
                );
                result = ReferenceEquals(
                             seg,
                             null
                         )
                         == false
                    ? seg.Distance + seg.LocalFToDistance(localF)
                    : 0;
            }

            return result;
        }

        /// <summary>
        /// Gets the segment and the local F for a certain TF
        /// </summary>
        /// <param name="tf">TF stands for Total Fragment. It's a value ranging from 0 to 1 inclusive. 0 means the spline's start and 1 means the spline's end.This is the "time" parameter used in the splines' formulas. A point's F is not proportional to its distance from the spline's start. Depending on the spline, a value of 0.5 does not always mean the middle, distance wise, of the spline</param>
        /// <param name="localF">gets the remaining localF in the range 0..1</param>
        /// <param name="isOnSegmentStart">Is True if the given distance is positioned at the returned segment's start</param>
        /// <param name="isOnSegmentEnd">Is True if the given distance is positioned at the returned segment's end</param>
        /// <param name="clamping">Clamping to use</param>
        /// <returns>the segment the given TF is inside</returns>
        public CurvySplineSegment TFToSegment(float tf, out float localF, out bool isOnSegmentStart, out bool isOnSegmentEnd,
            CurvyClamping clamping)
        {
            sanityChecker.Check();

            tf = CurvyUtility.ClampTF(
                tf,
                clamping
            );
            int segmentsCount = Count;
            if (segmentsCount == 0)
            {
                localF = 0;
                isOnSegmentStart = false;
                isOnSegmentEnd = false;
                return null;
            }

            float f = tf * segmentsCount;
            int idx = (int)f;
            localF = f - idx;

            if (idx == segmentsCount)
            {
                idx--;
                localF = 1;
            }

            isOnSegmentStart = f == idx;
            isOnSegmentEnd = tf == 1f;

            return this[idx];
        }

        /// <summary>
        /// Gets the segment and the local F for a certain TF
        /// </summary>
        /// <param name="tf">TF stands for Total Fragment. It's a value ranging from 0 to 1 inclusive. 0 means the spline's start and 1 means the spline's end.This is the "time" parameter used in the splines' formulas. A point's F is not proportional to its distance from the spline's start. Depending on the spline, a value of 0.5 does not always mean the middle, distance wise, of the spline</param>
        /// <param name="localF">gets the remaining localF in the range 0..1</param>
        /// <param name="clamping">Clamping to use</param>
        /// <returns>the segment the given TF is inside</returns>
        public CurvySplineSegment TFToSegment(float tf, out float localF, CurvyClamping clamping)
            => TFToSegment(
                tf,
                out localF,
                out _,
                out _,
                clamping
            );

        /// <summary>
        /// Gets the segment for a certain TF
        /// </summary>
        /// <param name="tf">TF stands for Total Fragment. It's a value ranging from 0 to 1 inclusive. 0 means the spline's start and 1 means the spline's end.This is the "time" parameter used in the splines' formulas. A point's F is not proportional to its distance from the spline's start. Depending on the spline, a value of 0.5 does not always mean the middle, distance wise, of the spline</param>
        /// <param name="clamping">Clamping to use</param>
        /// <returns>the segment the given TF is inside</returns>
        public CurvySplineSegment TFToSegment(float tf, CurvyClamping clamping)
            => TFToSegment(
                tf,
                out _,
                clamping
            );

        /// <summary>
        /// Gets the segment for a certain TF clamped to 0..1
        /// </summary>
        /// <param name="tf">TF stands for Total Fragment. It's a value ranging from 0 to 1 inclusive. 0 means the spline's start and 1 means the spline's end.This is the "time" parameter used in the splines' formulas. A point's F is not proportional to its distance from the spline's start. Depending on the spline, a value of 0.5 does not always mean the middle, distance wise, of the spline</param>
        /// <returns>the segment the given TF is inside</returns>
        public CurvySplineSegment TFToSegment(float tf)
            => TFToSegment(
                tf,
                out _,
                CurvyClamping.Clamp
            );

        /// <summary>
        /// Gets the segment and the local F for a certain TF clamped to 0..1
        /// </summary>
        /// <param name="tf">TF stands for Total Fragment. It's a value ranging from 0 to 1 inclusive. 0 means the spline's start and 1 means the spline's end.This is the "time" parameter used in the splines' formulas. A point's F is not proportional to its distance from the spline's start. Depending on the spline, a value of 0.5 does not always mean the middle, distance wise, of the spline</param>
        /// <param name="localF">gets the remaining localF in the range 0..1</param>
        /// <returns>the segment the given TF is inside</returns>
        public CurvySplineSegment TFToSegment(float tf, out float localF)
            => TFToSegment(
                tf,
                out localF,
                CurvyClamping.Clamp
            );

        /// <summary>
        /// Gets a TF value from a segment
        /// TF stands for Total Fragment. It's a value ranging from 0 to 1 inclusive. 0 means the spline's start and 1 means the spline's end.This is the "time" parameter used in the splines' formulas. A point's F is not proportional to its distance from the spline's start. Depending on the spline, a value of 0.5 does not always mean the middle, distance wise, of the spline
        /// </summary>
        /// <param name="segment">a segment</param>
        /// <returns>a TF value in the range 0..1</returns>
        public float SegmentToTF(CurvySplineSegment segment)
        {
#if CURVY_SANITY_CHECKS
            if (ReferenceEquals(
                    this,
                    segment.Spline
                )
                == false)
                throw new ArgumentException(
                    String.Format(
                        InvalidCPErrorMessage,
                        segment,
                        name
                    )
                );
#endif

            relationshipCache.EnsureIsValid();
            return segment.GetExtrinsicPropertiesINTERNAL().TF;
        }

        /// <summary>
        /// Gets a TF value from a segment and a local F
        /// TF stands for Total Fragment. It's a value ranging from 0 to 1 inclusive. 0 means the spline's start and 1 means the spline's end.This is the "time" parameter used in the splines' formulas. A point's F is not proportional to its distance from the spline's start. Depending on the spline, a value of 0.5 does not always mean the middle, distance wise, of the spline
        /// </summary>
        /// <param name="segment">a segment</param>
        /// <param name="localF">F of this segment in the range 0..1</param>
        /// <returns>a TF value in the range 0..1</returns>
        public float SegmentToTF(CurvySplineSegment segment, float localF)
        {
#if CURVY_SANITY_CHECKS
            if (ReferenceEquals(
                    this,
                    segment.Spline
                )
                == false)
                throw new ArgumentException(
                    String.Format(
                        InvalidCPErrorMessage,
                        segment,
                        name
                    )
                );
            Assert.IsTrue(
                localF.IsBetween0And1(),
                localF.ToString("R")
            );
#endif

            float result;
            if (IsControlPointASegment(segment))
            {
                //OPTIM a possible optimization would be to make this branch of the if the only executed code, so no call to IsControlPointASegment. the comparison with 1 bellow will take care of the last cp. The only remaining issue would be to handle the fake first cp create by AutoEndTangents for CatmullRom and TCB splines. I believe the only way to handle it would be to have separate treatment for autoendtangents. Might still be worth it.

                result = SegmentToTF(segment) + (localF / Count);
                //sometimes due to float imprecision, result can go beyond 1
                if (result > 1)
                    result = 1;
            }
            else
                result = SegmentToTF(segment);

#if CURVY_SANITY_CHECKS
            Assert.IsTrue(
                result.IsBetween0And1(),
                result.ToString("R")
            );
#endif

            return result;
        }

        /// <summary>
        /// Converts a distance to a TF value
        /// TF stands for Total Fragment. It's a value ranging from 0 to 1 inclusive. 0 means the spline's start and 1 means the spline's end.This is the "time" parameter used in the splines' formulas. A point's F is not proportional to its distance from the spline's start. Depending on the spline, a value of 0.5 does not always mean the middle, distance wise, of the spline
        /// </summary>
        /// <param name="distance">distance</param>
        /// <param name="clamping">Clamping to use</param>
        /// <returns>a TF value in the range 0..1</returns>
        public float DistanceToTF(float distance, CurvyClamping clamping = CurvyClamping.Clamp)
        {
            sanityChecker.Check();
            float result;
            if (Length == 0)
                result = 0;
            else if (distance == 0)
                result = 0;
            else if (distance == Length)
                result = 1;
            else
            {
                float localDistance;
                // Get the segment the distance lies within
                CurvySplineSegment seg = DistanceToSegment(
                    distance,
                    out localDistance,
                    clamping
                );
                result = ReferenceEquals(
                             seg,
                             null
                         )
                         == false
                    ? SegmentToTF(
                        seg,
                        seg.DistanceToLocalF(localDistance)
                    )
                    : 0;
            }

            return result;
        }

        /// <summary>
        /// Gets the segment a certain distance lies within
        /// </summary>
        /// <param name="distance">a distance in the range 0..Length</param>
        /// <param name="clamping">clamping to use</param>
        /// <returns>a spline segment or null</returns>
        public CurvySplineSegment DistanceToSegment(float distance, CurvyClamping clamping = CurvyClamping.Clamp)
        {
            sanityChecker.Check();

            float d;
            return DistanceToSegment(
                distance,
                out d,
                clamping
            );
        }

        /// <summary>
        /// Gets the segment a certain distance lies within
        /// </summary>
        /// <param name="distance">a distance in the range 0..Length</param>
        /// <param name="localDistance">gets the remaining distance inside the segment</param>
        /// <param name="clamping">clamping to use</param>
        /// <returns>a spline segment</returns>
        public CurvySplineSegment DistanceToSegment(float distance, out float localDistance,
            CurvyClamping clamping = CurvyClamping.Clamp)
            => DistanceToSegment(
                distance,
                out localDistance,
                out _,
                out _,
                clamping
            );

        /// <summary>
        /// Gets the segment a certain distance lies within
        /// </summary>
        /// <param name="distance">a distance in the range 0..Length</param>
        /// <param name="localDistance">gets the remaining distance inside the segment</param>
        /// <param name="isOnSegmentStart">Is True if the given distance is positioned at the returned segment's start</param>
        /// <param name="isOnSegmentEnd">Is True if the given distance is positioned at the returned segment's end</param>
        /// <param name="clamping">clamping to use</param>
        /// <returns>a spline segment</returns>
        public CurvySplineSegment DistanceToSegment(float distance, out float localDistance, out bool isOnSegmentStart,
            out bool isOnSegmentEnd, CurvyClamping clamping = CurvyClamping.Clamp)
        {
            sanityChecker.Check();
            distance = CurvyUtility.ClampDistance(
                distance,
                clamping,
                Length
            );
            CurvySplineSegment resultSegment;
            if (Count > 0)
            {
                int resultCpIndex = CurvyUtility.InterpolationSearch(
                    controlPointsDistances,
                    controlPointsDistances.Length,
                    distance
                );
                bool notAutoEndTangents = AutoEndTangents == false;
                int cpCount = ControlPointsList.Count;
                if (notAutoEndTangents)
                {
#if CURVY_SANITY_CHECKS_PRIVATE
                    Assert.IsFalse(Closed);
#endif
                    if (resultCpIndex == 0)
                        resultCpIndex = 1;
                    else if (resultCpIndex == cpCount - 1 || resultCpIndex == cpCount - 2)
                        resultCpIndex = cpCount - 3;
                }
                else if (Closed == false && resultCpIndex == cpCount - 1)
                    resultCpIndex = cpCount - 2;

                resultSegment = ControlPointsList[resultCpIndex];
                localDistance = distance - resultSegment.Distance;

                isOnSegmentStart = distance == resultSegment.Distance;
                isOnSegmentEnd = distance == Length;
            }
            else
            {
                resultSegment = null;
                localDistance = -1;
                isOnSegmentStart = false;
                isOnSegmentEnd = false;
            }

            return resultSegment;
        }

        #endregion


        #region Clamping

        /// <summary>
        /// Clamps absolute position
        /// </summary>
        public float ClampDistance(float distance, CurvyClamping clamping)
            => CurvyUtility.ClampDistance(
                distance,
                clamping,
                Length
            );

        /// <summary>
        /// Clamps absolute position
        /// </summary>
        public float ClampDistance(float distance, CurvyClamping clamping, float min, float max)
            => CurvyUtility.ClampDistance(
                distance,
                clamping,
                Length,
                min,
                max
            );

        /// <summary>
        /// Clamps absolute position and sets new direction
        /// </summary>
        public float ClampDistance(float distance, ref int dir, CurvyClamping clamping)
            => CurvyUtility.ClampDistance(
                distance,
                ref dir,
                clamping,
                Length
            );

        /// <summary>
        /// Clamps absolute position and sets new direction
        /// </summary>
        public float ClampDistance(float distance, ref int dir, CurvyClamping clamping, float min, float max)
            => CurvyUtility.ClampDistance(
                distance,
                ref dir,
                clamping,
                Length,
                min,
                max
            );

        #endregion

        #region --- General ---

        /// <summary>
        /// Adds a Control Point at the end of the spline
        /// This method will <see cref="Refresh"/> the spline and call the relevant events.
        /// If you want more control on the order of the added Control Point, its position, or whether <see cref="Refresh"/> and events should be called, use the <see cref="InsertBefore(CurvySplineSegment,Vector3,bool,Space)"/> and <see cref="InsertAfter(CurvySplineSegment,Vector3,bool,Space)"/> and <see cref="Add(Vector3,Space)"/> instead
        /// </summary>
        /// <returns>The added Control Point</returns>
        public CurvySplineSegment Add()
            => InsertAfter(null);

        /// <summary>
        /// Adds several Control Points at the end of the spline
        /// This method will <see cref="Refresh"/> the spline and call the relevant events.
        /// If you want more control on the order of the added Control Points, their position, or whether <see cref="Refresh"/> and events should be called, use the <see cref="InsertBefore(CurvySplineSegment,Vector3,bool,Space)"/> and <see cref="InsertAfter(CurvySplineSegment,Vector3,bool,Space)"/> and <see cref="Add(Vector3[],Space)"/> instead
        /// </summary>
        /// <param name="controlPointsCount">The number of  Control Points to add</param>
        public CurvySplineSegment[] Add(int controlPointsCount)
        {
            Vector3[] controlPointsLocalPositions = new Vector3[controlPointsCount];
            return Add(controlPointsLocalPositions);
        }

        /// <summary>
        /// Adds several Control Points at the end of the spline
        /// This method will <see cref="Refresh"/> the spline and call the relevant events.
        /// If you want more control on the order of the added Control Points, their position, or whether <see cref="Refresh"/> and events should be called, use the <see cref="InsertBefore(CurvySplineSegment,Vector3,bool,Space)"/> and <see cref="InsertAfter(CurvySplineSegment,Vector3,bool,Space)"/> instead
        /// </summary>
        /// <param name="controlPointPosition">The position of the Control Point to add</param>
        /// <param name="space">Whether the position is in the local or global space</param>
        /// <returns>The added Control Points</returns>
        public CurvySplineSegment Add(Vector3 controlPointPosition, Space space)
        {
            OnBeforeControlPointAddEvent(defaultAddAfterEventArgs);

            CurvySplineSegment result = InsertAfter(
                null,
                controlPointPosition,
                true,
                space
            );

            Refresh();
            OnAfterControlPointAddEvent(defaultAddAfterEventArgs);
            OnAfterControlPointChangesEvent(defaultSplineEventArgs);

            return result;
        }

        /// <summary>
        /// Adds several Control Points at the end of the spline
        /// This method will <see cref="Refresh"/> the spline and call the relevant events.
        /// If you want more control on the order of the added Control Points, their position, or whether <see cref="Refresh"/> and events should be called, use the <see cref="InsertBefore(CurvySplineSegment,Vector3,bool,Space)"/> and <see cref="InsertAfter(CurvySplineSegment,Vector3,bool,Space)"/> and <see cref="Add(Vector3[],Space)"/> instead
        /// </summary>
        /// <param name="controlPointsLocalPositions">The local position of the Control Points to add</param>
        /// <returns>The added Control Points</returns>
        public CurvySplineSegment[] Add(params Vector3[] controlPointsLocalPositions)
        {
            OnBeforeControlPointAddEvent(defaultAddAfterEventArgs);

            CurvySplineSegment[] cps = new CurvySplineSegment[controlPointsLocalPositions.Length];
            for (int i = 0; i < controlPointsLocalPositions.Length; i++)
                cps[i] = InsertAfter(
                    null,
                    controlPointsLocalPositions[i],
                    true,
                    Space.Self
                );

            Refresh();
            OnAfterControlPointAddEvent(defaultAddAfterEventArgs);
            OnAfterControlPointChangesEvent(defaultSplineEventArgs);

            return cps;
        }

        /// <summary>
        /// Adds several Control Points at the end of the spline
        /// This method will <see cref="Refresh"/> the spline and call the relevant events.
        /// If you want more control on the order of the added Control Points, their position, or whether <see cref="Refresh"/> and events should be called, use the <see cref="InsertBefore(CurvySplineSegment,Vector3,bool,Space)"/> and <see cref="InsertAfter(CurvySplineSegment,Vector3,bool,Space)"/> instead
        /// </summary>
        /// <param name="controlPointsPositions">The positions of the Control Points to add</param>
        /// <param name="space">Whether the positions are in the local or global space</param>
        /// <returns>The added Control Points</returns>
        public CurvySplineSegment[] Add(Vector3[] controlPointsPositions, Space space)
        {
            OnBeforeControlPointAddEvent(defaultAddAfterEventArgs);

            CurvySplineSegment[] cps = new CurvySplineSegment[controlPointsPositions.Length];
            for (int i = 0; i < controlPointsPositions.Length; i++)
                cps[i] = InsertAfter(
                    null,
                    controlPointsPositions[i],
                    true,
                    space
                );

            Refresh();
            OnAfterControlPointAddEvent(defaultAddAfterEventArgs);
            OnAfterControlPointChangesEvent(defaultSplineEventArgs);

            return cps;
        }

        /// <summary>
        /// Inserts a Control Point before a given Control Point
        /// </summary>
        /// <remarks>If you add several Control Points in a row, using <see cref="Add(Vector3[])"/> will be more efficient</remarks>
        /// <param name="controlPoint">A control point of the spline, before which the new control point will be added. If null, the CP will be added at the start of the spline</param>
        /// <returns>The created Control Point</returns>
        /// <param name="skipRefreshingAndEvents">If true, the spline's <see cref="Refresh"/> method will not be called, and the relevant events will not be triggered</param>
        public CurvySplineSegment InsertBefore(CurvySplineSegment controlPoint, bool skipRefreshingAndEvents = false)
        {
            Vector3 position;
            CurvySplineSegment previousControlPoint;
            if (controlPoint && (previousControlPoint = GetPreviousControlPoint(controlPoint)))
                position = IsControlPointASegment(previousControlPoint)
                    ? previousControlPoint.Interpolate(
                        0.5f,
                        Space.World
                    )
                    : OptimizedOperators.LerpUnclamped(
                        previousControlPoint.transform.position,
                        controlPoint.transform.position,
                        0.5f
                    );
            else
                position = transform.position;

            return InsertBefore(
                controlPoint,
                position,
                skipRefreshingAndEvents
            );
        }

        /// <summary>
        /// Inserts a Control Point before a given Control Point
        /// </summary>
        /// <remarks>If you add several Control Points in a row, using <see cref="Add(Vector3[])"/> will be more efficient</remarks>
        /// <param name="controlPoint">A control point of the spline, before which the new control point will be added. If null, the CP will be added at the start of the spline</param>
        /// <param name="position">The position of the control point at its creation</param>
        /// <param name="skipRefreshingAndEvents">If true, the spline's <see cref="Refresh"/> method will not be called, and the relevant events will not be triggered</param>
        /// <param name="space">Whether the position is in the local or global space</param>
        /// <returns>The created Control Point</returns>
        public CurvySplineSegment InsertBefore([CanBeNull] CurvySplineSegment controlPoint, Vector3 position,
            bool skipRefreshingAndEvents = false, Space space = Space.World)
        {
#if CURVY_SANITY_CHECKS
            if (ReferenceEquals(
                    controlPoint,
                    null
                )
                == false
                && ReferenceEquals(
                    this,
                    controlPoint.Spline
                )
                == false)
                throw new ArgumentException(
                    String.Format(
                        InvalidCPErrorMessage,
                        controlPoint,
                        name
                    )
                );
#endif

            return InsertAt(
                controlPoint,
                position,
                ReferenceEquals(
                    controlPoint,
                    null
                )
                == false
                    ? Mathf.Max(
                        0,
                        GetControlPointIndex(controlPoint)
                    )
                    : 0,
                CurvyControlPointEventArgs.ModeEnum.AddBefore,
                skipRefreshingAndEvents,
                space
            );
        }

        /// <summary>
        /// Inserts a Control Point after a given Control Point
        /// </summary>
        /// <remarks>If you add several Control Points in a row, using <see cref="Add(Vector3[])"/> will be more efficient</remarks>
        /// <param name="controlPoint">A control point of the spline, behind which the new control point will be added. If null, the CP will be added at the end of the spline</param>
        /// <param name="skipRefreshingAndEvents">If true, the spline's <see cref="Refresh"/> method will not be called, and the relevant events will not be triggered</param>
        /// <returns>the new Control Point</returns>
        public CurvySplineSegment InsertAfter(CurvySplineSegment controlPoint, bool skipRefreshingAndEvents = false)
        {
            Vector3 position;
            if (controlPoint)
            {
                if (IsControlPointASegment(controlPoint))
                    position = controlPoint.Interpolate(
                        0.5f,
                        Space.World
                    );
                else
                {
                    CurvySplineSegment nextControlPoint = GetNextControlPoint(controlPoint);
                    position = nextControlPoint
                        ? OptimizedOperators.LerpUnclamped(
                            nextControlPoint.transform.position,
                            controlPoint.transform.position,
                            0.5f
                        )
                        : controlPoint.transform.position;
                }
            }
            else
                position = transform.position;

            return InsertAfter(
                controlPoint,
                position,
                skipRefreshingAndEvents
            );
        }

        /// <summary>
        /// Inserts a Control Point after a given Control Point
        /// </summary>
        /// <remarks>If you add several Control Points in a row, using <see cref="Add(Vector3[])"/> will be more efficient</remarks>
        /// <param name="controlPoint">A control point of the spline, behind which the new control point will be added. If null, the CP will be added at the end of the spline</param>
        /// <param name="position">The position of the control point at its creation</param>
        /// <param name="skipRefreshingAndEvents">If true, the spline's <see cref="Refresh"/> method will not be called, and the relevant events will not be triggered</param>
        /// <param name="space">Whether the position is in the local or global space</param>
        /// <returns>the new Control Point</returns>
        public CurvySplineSegment InsertAfter([CanBeNull] CurvySplineSegment controlPoint, Vector3 position,
            bool skipRefreshingAndEvents = false, Space space = Space.World)
        {
#if CURVY_SANITY_CHECKS
            if (ReferenceEquals(
                    controlPoint,
                    null
                )
                == false
                && ReferenceEquals(
                    this,
                    controlPoint.Spline
                )
                == false)
                throw new ArgumentException(
                    String.Format(
                        InvalidCPErrorMessage,
                        controlPoint,
                        name
                    )
                );
#endif
            return InsertAt(
                controlPoint,
                position,
                ReferenceEquals(
                    controlPoint,
                    null
                )
                == false
                    ? GetControlPointIndex(controlPoint) + 1
                    : ControlPoints.Count,
                CurvyControlPointEventArgs.ModeEnum.AddAfter,
                skipRefreshingAndEvents,
                space
            );
        }

        /// <summary>
        /// Adds or removes control points to match a given count
        /// </summary>
        public void SetControlPointCount(
            int newCount)
        {
            if (newCount == ControlPointCount)
                return;

            if (newCount > ControlPointCount)
                Add(newCount - ControlPointCount);
            else
            {
#if CURVY_SANITY_CHECKS
                Assert.IsTrue(newCount < ControlPointCount);
#endif
                while (newCount != ControlPointCount)
                {
#if CURVY_SANITY_CHECKS
                    Assert.IsTrue(ControlPointCount != 0);
#endif
                    CurvySplineSegment controlPoint = ControlPoints.Last();

                    OnBeforeControlPointDeleteEvent(
                        new CurvyControlPointEventArgs(
                            this,
                            this,
                            controlPoint,
                            CurvyControlPointEventArgs.ModeEnum.Delete
                        )
                    );
                    Delete(
                        controlPoint,
                        true
                    );
                }
            }

            OnAfterControlPointChangesEvent(defaultSplineEventArgs);
        }


        /// <summary>
        /// Removes all control points
        /// <param name="isUndoable">If true, the clearing of the spline is made undoable (CTRL+Z) in the editor</param>
        /// </summary>
        public void Clear(bool isUndoable = true)
        {
            OnBeforeControlPointDeleteEvent(defaultDeleteEventArgs);

            // RequestSplineToHierarchy is done here instead of in ClearControlPoints bellow because the DisposeOfControlPoint bellow changes the children list of the spline, leading to OnTransformChildrenChanged being called. That call leads to a hierarchy to spline synchronization if no prior spline to hierarchy synchronization is requested. That's why we request the latter here.  
            cpsSynchronizer.RequestSplineToHierarchy();

            for (int i = ControlPointCount - 1; i >= 0; i--)
                DisposeOfControlPoint(
                    ControlPoints[i],
                    isUndoable
                );

            ClearControlPoints(
                true,
                false
            );

            Refresh();
            OnAfterControlPointChangesEvent(defaultSplineEventArgs);
        }

        /// <summary>
        /// Deletes a Control Point
        /// </summary>
        /// <param name="controlPoint">a Control Point</param>
        /// <param name="skipRefreshingAndEvents">If true, the spline's <see cref="Refresh"/> method will not be called, and the relevant events will not be triggered</param>
        public void Delete(CurvySplineSegment controlPoint, bool skipRefreshingAndEvents = false) =>
            Delete(
                controlPoint,
                skipRefreshingAndEvents,
                true
            );

        /// <summary>
        /// Deletes a Control Point
        /// </summary>
        /// <param name="controlPoint">a Control Point</param>
        /// <param name="skipRefreshingAndEvents">If true, the spline's <see cref="Refresh"/> method will not be called, and the relevant events will not be triggered</param>
        /// <param name="isUndoableDeletion">If true, the destruction of the control point's game object is made undoable (CTRL+Z) in the editor</param>
        public void Delete(CurvySplineSegment controlPoint, bool skipRefreshingAndEvents, bool isUndoableDeletion)
        {
            if (!controlPoint)
                return;

#if CURVY_SANITY_CHECKS
            if (ReferenceEquals(
                    this,
                    controlPoint.Spline
                )
                == false)
                throw new ArgumentException(
                    String.Format(
                        InvalidCPErrorMessage,
                        controlPoint,
                        name
                    )
                );
#endif

            if (skipRefreshingAndEvents == false)
                OnBeforeControlPointDeleteEvent(
                    new CurvyControlPointEventArgs(
                        this,
                        this,
                        controlPoint,
                        CurvyControlPointEventArgs.ModeEnum.Delete
                    )
                );

            RemoveControlPoint(controlPoint);

#if UNITY_EDITOR == false
            //TODO do we need this SetAsLastSibling?
            controlPoint.transform
                .SetAsLastSibling(); // IMPORTANT! Runtime Delete is delayed, so we need to make sure it got sorted to the end 
#endif
            DisposeOfControlPoint(
                controlPoint,
                isUndoableDeletion
            );

            if (skipRefreshingAndEvents == false)
            {
                Refresh();
                OnAfterControlPointChangesEvent(defaultSplineEventArgs);
            }
        }

        #region Cache access

        /// <summary>
        /// Gets an array containing all approximation points
        /// </summary>
        /// <param name="space">The space (local/object or global/world) in which the returned result is expressed</param>
        /// <remarks>Returns a copy of the internal cache</remarks>
        /// <returns>an array of world/local positions</returns>
        public SubArray<Vector3> GetPositionsCache(Space space) => GetSegmentApproximationsInSpace(
            s => s.PositionsApproximation,
            space
        );

        /// <summary>
        /// Gets an array containing all approximation points
        /// </summary>
        /// <param name="space">The space (local/object or global/world) in which the returned result is expressed</param>
        /// <remarks>This can be used to feed meshbuilders etc...</remarks>
        /// <returns>an array of world/local positions</returns>
        [UsedImplicitly]
        [Obsolete("Use GetPositionsCache instead")]
        public Vector3[] GetApproximation(Space space = Space.Self) => GetPositionsCache(space).CopyToArray(ArrayPools.Vector3);

        /// <summary>
        /// Gets all Approximation points for a given spline part
        /// </summary>
        /// <param name="fromTF">start TF</param>
        /// <param name="toTF">end TF</param>
        /// <param name="includeEndPoint">Whether the end position should be included</param>
        /// <param name="space">The space (local/object or global/world) in which the returned result is expressed</param>
        /// <returns>an array of Approximation points</returns>
        // DESIGN Make a GetPositionsCache equivalent, then make obsolete and remove/rename getApproximationIndexINTERNAL
        public Vector3[] GetApproximation(float fromTF, float toTF, bool includeEndPoint = true, Space space = Space.Self)
        {
            float startLF;
            float startFrag;
            float endLF;
            float endFrag;
            CurvySplineSegment startSeg = TFToSegment(
                fromTF,
                out startLF
            );
            int startIdx = startSeg.getApproximationIndexINTERNAL(
                startLF,
                out startFrag
            );
            CurvySplineSegment endSeg = TFToSegment(
                toTF,
                out endLF
            );
            int endIdx = endSeg.getApproximationIndexINTERNAL(
                endLF,
                out endFrag
            );

            CurvySplineSegment seg = startSeg;
            //bug usage of startIdx + 1 whithout checking if it is not out of bounds, which may happen if seg's length is 0
            SubArray<Vector3> positionsSubArray = seg.PositionsApproximation;
            Vector3[] positionsArray = positionsSubArray.Array;
            Vector3[] res = new Vector3[1]
            {
                Vector3.Lerp(
                    positionsArray[startIdx],
                    positionsArray[startIdx + 1],
                    startFrag
                )
            };
            //if (startFrag == 1)
            //    seg = seg.NextSegment;
            while (seg
                   && ReferenceEquals(
                       seg,
                       endSeg
                   )
                   == false)
            {
                res = res.AddRange(
                    positionsArray.SubArray(
                        startIdx + 1,
                        positionsSubArray.Count - 1
                    )
                );
                startIdx = 1;
                seg = seg.Spline.GetNextSegment(seg);
            }

            if (ReferenceEquals(
                    seg,
                    null
                )
                == false)
            {
                int i = startSeg == seg
                    ? startIdx + 1
                    : 1;
                res = res.AddRange(
                    positionsArray.SubArray(
                        i,
                        endIdx - i
                    )
                );
                if (includeEndPoint && (endFrag > 0 || endFrag < 1))
                    res = res.Add(
                        Vector3.Lerp(
                            positionsArray[endIdx],
                            positionsArray[endIdx + 1],
                            endFrag
                        )
                    );
            }

            if (space == Space.World)
            {
                Matrix4x4 m = transform.localToWorldMatrix;
                for (int i = 0; i < res.Length; i++)
                    res[i] = m.MultiplyPoint3x4(res[i]);
            }

            return res;
        }

        /// <summary>
        /// Gets an array containing all approximation tangents
        /// </summary>
        /// <param name="space">The space (local/object or global/world) in which the returned result is expressed</param>
        /// <remarks>Returns a copy of the internal cache</remarks>
        /// <returns>an array of tangents</returns>
        public SubArray<Vector3> GetTangentsCache(Space space) => GetSegmentApproximationsInSpace(
            s => s.TangentsApproximation,
            space
        );


        /// <summary>
        /// Gets an array containing all approximation tangents
        /// </summary>
        /// <param name="space">The space (local/object or global/world) in which the returned result is expressed</param>
        /// <remarks>This can be used to feed meshbuilders etc...</remarks>
        /// <returns>an array of tangents</returns>
        [UsedImplicitly]
        [Obsolete("Use GetTangentsCache instead")]
        public Vector3[] GetApproximationT(Space space = Space.Self) => GetTangentsCache(space).CopyToArray(ArrayPools.Vector3);

        /// <summary>
        /// Gets an array containing all approximation Up-Vectors
        /// </summary>
        /// <param name="space">The space (local/object or global/world) in which the returned result is expressed</param>
        /// <remarks>Returns a copy of the internal cache</remarks>
        /// <returns>an array of Up-Vectors</returns>
        public SubArray<Vector3> GetNormalsCache(Space space) => GetSegmentApproximationsInSpace(
            s => s.UpsApproximation,
            space
        );

        /// <summary>
        /// Gets an array containing all approximation Up-Vectors
        /// </summary>
        /// <param name="space">The space (local/object or global/world) in which the returned result is expressed</param>
        /// <remarks>This can be used to feed meshbuilders etc...</remarks>
        /// <returns>an array of Up-Vectors</returns>
        [UsedImplicitly]
        [Obsolete("Use GetNormalsCache instead")]
        public Vector3[] GetApproximationUpVectors(Space space = Space.Self) =>
            GetNormalsCache(space).CopyToArray(ArrayPools.Vector3);

        #endregion

        /// <summary>
        /// Gets the point on the spline that is the nearest to a given <paramref name="position"/>
        /// </summary>
        /// <param name="position">The input point's position</param>
        /// <param name="space">The space (local/object or global/world) in which the <paramref name="position"/> and the returned value are expressed</param>
        /// <remarks>This method's precision and speed depend on the <see cref="CacheDensity"/></remarks>
        /// <returns>The nearest point on the spline to the given <paramref name="position"/>, expressed in the space defined by the <paramref name="space"/> parameter</returns>
        public Vector3 GetNearestPoint(Vector3 position, Space space)
        {
            Vector3 result;
            GetNearestPointTF(
                position,
                out result,
                out _,
                out _,
                0,
                -1,
                space
            );
            return result;
        }

        /// <summary>
        /// Gets the TF value of the point on the spline that is the nearest to a given position
        /// TF stands for Total Fragment. It's a value ranging from 0 to 1 inclusive. 0 means the spline's start and 1 means the spline's end. This is the "time" parameter used in the splines' formulas. A point's TF is not proportional to its distance from the spline's start. Depending on the spline, a value of 0.5 does not always mean the middle, distance wise, of the spline
        /// </summary>
        /// <param name="localPosition">The point's position expressed in the spline's local space</param>
        /// <remarks>This method's precision and speed depend on the <see cref="CacheDensity"/></remarks>
        /// <returns>a TF value in the range 0..1. If spline has no segments the returned value will be -1</returns>
        public float GetNearestPointTF(Vector3 localPosition)
            => GetNearestPointTF(
                localPosition,
                out _,
                out _,
                out _
            );

        /// <summary>
        /// Gets the TF value of the point on the spline that is the nearest to a given position
        /// TF stands for Total Fragment. It's a value ranging from 0 to 1 inclusive. 0 means the spline's start and 1 means the spline's end. This is the "time" parameter used in the splines' formulas. A point's TF is not proportional to its distance from the spline's start. Depending on the spline, a value of 0.5 does not always mean the middle, distance wise, of the spline
        /// </summary>
        /// <param name="position">The point's position</param>
        /// <param name="space">The space (local/object or global/world) in which the <paramref name="position"/> is expressed</param>
        /// <remarks>This method's precision and speed depend on the <see cref="CacheDensity"/></remarks>
        /// <returns>a TF value in the range 0..1. If spline has no segments the returned value will be -1</returns>
        public float GetNearestPointTF(Vector3 position, Space space)
            => GetNearestPointTF(
                position,
                out _,
                out _,
                out _,
                0,
                -1,
                space
            );

        /// <summary>
        /// Gets the TF value of the point on the spline that is the nearest to a given position
        /// TF stands for Total Fragment. It's a value ranging from 0 to 1 inclusive. 0 means the spline's start and 1 means the spline's end. This is the "time" parameter used in the splines' formulas. A point's TF is not proportional to its distance from the spline's start. Depending on the spline, a value of 0.5 does not always mean the middle, distance wise, of the spline
        /// </summary>
        /// <param name="localPosition">The point's position expressed in the spline's local space</param>
        /// <param name="nearestPoint">the nearest point on the spline to the given <paramref name="localPosition"/>, expressed in the local space</param>
        /// <remarks>This method's precision and speed depend on the <see cref="CacheDensity"/></remarks>
        /// <returns>a TF value in the range 0..1. If spline has no segments the returned value will be -1</returns>
        public float GetNearestPointTF(Vector3 localPosition, out Vector3 nearestPoint)
            => GetNearestPointTF(
                localPosition,
                out nearestPoint,
                out _,
                out _
            );

        /// <summary>
        /// Gets the TF value of the point on the spline that is the nearest to a given position
        /// TF stands for Total Fragment. It's a value ranging from 0 to 1 inclusive. 0 means the spline's start and 1 means the spline's end. This is the "time" parameter used in the splines' formulas. A point's TF is not proportional to its distance from the spline's start. Depending on the spline, a value of 0.5 does not always mean the middle, distance wise, of the spline
        /// </summary>
        /// <param name="position">The point's position</param>
        /// <param name="nearestPoint">the nearest point on the spline to the given <paramref name="position"/>, expressed in the space defined by the <paramref name="space"/> parameter</param>
        /// <param name="space">The space (local/object or global/world) in which the <paramref name="position"/> is expressed</param>
        /// <remarks>This method's precision and speed depend on the <see cref="CacheDensity"/></remarks>
        /// <returns>a TF value in the range 0..1. If spline has no segments the returned value will be -1</returns>
        public float GetNearestPointTF(Vector3 position, out Vector3 nearestPoint, Space space)
            => GetNearestPointTF(
                position,
                out nearestPoint,
                out _,
                out _,
                0,
                -1,
                space
            );

        /// <summary>
        /// Gets the TF value of the point on the spline that is the nearest to a given position
        /// TF stands for Total Fragment. It's a value ranging from 0 to 1 inclusive. 0 means the spline's start and 1 means the spline's end. This is the "time" parameter used in the splines' formulas. A point's TF is not proportional to its distance from the spline's start. Depending on the spline, a value of 0.5 does not always mean the middle, distance wise, of the spline
        /// </summary>
        /// <param name="position">The point's position</param>
        /// <param name="searchStartSegmentIndex">the index of the first segment to include in the search. Set it to 0 to start searching from the spline's start</param>
        /// <param name="searchEndSegmentIndex">the index of the last segment to include in the search. Set it to -1 to search until the spline's end</param>
        /// <param name="space">The space (local/object or global/world) in which the <paramref name="position"/> is expressed</param>
        /// <remarks>This method's precision and speed depend on the <see cref="CacheDensity"/></remarks>
        /// <returns>a TF value in the range 0..1. If spline has no segments the returned value will be -1</returns>
        public float GetNearestPointTF(Vector3 position, int searchStartSegmentIndex = 0, int searchEndSegmentIndex = -1,
            Space space = Space.Self)
            => GetNearestPointTF(
                position,
                out _,
                out _,
                out _,
                searchStartSegmentIndex,
                searchEndSegmentIndex,
                space
            );

        /// <summary>
        /// Gets the TF value of the point on the spline that is the nearest to a given position
        /// TF stands for Total Fragment. It's a value ranging from 0 to 1 inclusive. 0 means the spline's start and 1 means the spline's end. This is the "time" parameter used in the splines' formulas. A point's TF is not proportional to its distance from the spline's start. Depending on the spline, a value of 0.5 does not always mean the middle, distance wise, of the spline
        /// </summary>
        /// <param name="position">The point's position</param>
        /// <param name="nearestPoint">the nearest point on the spline to the given <paramref name="position"/>, expressed in the space defined by the <paramref name="space"/> parameter</param>
        /// <param name="searchStartSegmentIndex">the index of the first segment to include in the search. Set it to 0 to start searching from the spline's start</param>
        /// <param name="searchEndSegmentIndex">the index of the last segment to include in the search. Set it to -1 to search until the spline's end</param>
        /// <param name="space">The space (local/object or global/world) in which the <paramref name="position"/> is expressed</param>
        /// <remarks>This method's precision and speed depend on the <see cref="CacheDensity"/></remarks>
        /// <returns>a TF value in the range 0..1. If spline has no segments the returned value will be -1</returns>
        public float GetNearestPointTF(Vector3 position, out Vector3 nearestPoint, int searchStartSegmentIndex = 0,
            int searchEndSegmentIndex = -1, Space space = Space.Self)
            => GetNearestPointTF(
                position,
                out nearestPoint,
                out _,
                out _,
                searchStartSegmentIndex,
                searchEndSegmentIndex,
                space
            );

        /// <summary>
        /// Gets the TF value of the point on the spline that is the nearest to a given position
        /// TF stands for Total Fragment. It's a value ranging from 0 to 1 inclusive. 0 means the spline's start and 1 means the spline's end. This is the "time" parameter used in the splines' formulas. A point's TF is not proportional to its distance from the spline's start. Depending on the spline, a value of 0.5 does not always mean the middle, distance wise, of the spline
        /// </summary>
        /// <param name="position">The point's position</param>
        /// <param name="nearestPoint">the nearest point on the spline to the given <paramref name="position"/>, expressed in the space defined by the <paramref name="space"/> parameter</param>
        /// <param name="nearestSegment">the nearest segment of the spline to the given <paramref name="position"/></param>
        /// <param name="nearestPointLocalF">LocalF of the nearest point on the nearest segment</param>
        /// <param name="searchStartSegmentIndex">the index of the first segment to include in the search. Set it to 0 to start searching from the spline's start</param>
        /// <param name="searchEndSegmentIndex">the index of the last segment to include in the search. Set it to -1 to search until the spline's end</param>
        /// <param name="space">The space (local/object or global/world) in which the <paramref name="position"/> is expressed</param>
        /// <remarks>This method's precision and speed depend on the <see cref="CacheDensity"/></remarks>
        /// <returns>a TF value in the range 0..1. If spline has no segments the returned value will be -1</returns>
        public float GetNearestPointTF(Vector3 position, out Vector3 nearestPoint,
            [CanBeNull] out CurvySplineSegment nearestSegment, out float nearestPointLocalF, int searchStartSegmentIndex = 0,
            int searchEndSegmentIndex = -1, Space space = Space.Self)
        {
            sanityChecker.Check();

            nearestPoint = Vector3.zero;
            if (Count == 0)
            {
                nearestSegment = null;
                nearestPointLocalF = -1;
                return -1;
            }

            // for each segment, get the distance to it's approximation points
            float distSqr = float.MaxValue;
            float resF = 0;

            CurvySplineSegment resSeg = null;
            if (searchEndSegmentIndex == -1)
                searchEndSegmentIndex = Count - 1;
            searchStartSegmentIndex = Mathf.Clamp(
                searchStartSegmentIndex,
                0,
                Count - 1
            );
            searchEndSegmentIndex = Mathf.Clamp(
                searchEndSegmentIndex + 1,
                searchStartSegmentIndex + 1,
                Count
            );
            for (int i = searchStartSegmentIndex; i < searchEndSegmentIndex; i++)
            {
                float f = this[i].GetNearestPointF(
                    position,
                    space
                );
                Vector3 v = this[i].Interpolate(
                    f,
                    space
                );
                float magSqr = (v - position).sqrMagnitude;
                if (magSqr <= distSqr)
                {
                    resSeg = this[i];
                    resF = f;
                    nearestPoint = v;
                    distSqr = magSqr;
                }
            }

            nearestSegment = resSeg;
            nearestPointLocalF = resF;
            // return the nearest
            return resSeg.LocalFToTF(resF);
        }

        /// <summary>
        /// Refreshs the spline
        /// </summary>
        /// <remarks>This is called automatically on the next Update() if any changes are pending</remarks>
        public void Refresh()
        {
            if (dirtinessManager.ProcessDirtyControlPoints())
                OnRefreshEvent(defaultSplineEventArgs);
        }

        /// <summary>
        /// Ensure the whole spline will be recalculated on next call to Refresh()
        /// </summary>
        public void SetDirtyAll() =>
            SetDirtyAll(
                SplineDirtyingType.Everything,
                true
            );

        /// <summary>
        /// Ensure the whole spline will be recalculated on next call to Refresh()
        /// </summary>
        /// <param name="dirtyingType">Defines what aspect should be dirtied</param>
        /// <param name="dirtyConnectedControlPoints">Whether to mark control points of other splines as dirty when they are connected to any control point of this spline.</param>
        public void SetDirtyAll(SplineDirtyingType dirtyingType, bool dirtyConnectedControlPoints) =>
            dirtinessManager.SetDirtyAll(
                dirtyingType,
                dirtyConnectedControlPoints
            );

        /// <summary>
        /// Marks a Control Point to get recalculated on next call to Refresh(). Will also mark connected control points and control points that depend on the current one through the Follow-Up feature.
        /// </summary>
        /// <param name="dirtyControlPoint">the Control Point to dirty</param>
        /// <param name="dirtyingType">Defines what aspect should be dirtied</param>
        public void SetDirty(CurvySplineSegment dirtyControlPoint, SplineDirtyingType dirtyingType) =>
            dirtinessManager.SetDirty(
                dirtyControlPoint,
                dirtyingType,
                GetPreviousControlPoint(dirtyControlPoint),
                GetNextControlPoint(dirtyControlPoint),
                false
            );

        /// <summary>
        /// Marks a Control Point to get recalculated on next call to Refresh(). Will also mark connected control points and control points that depend on the current one through the Follow-Up feature. Be aware, this method, and unlike SetDirty, will not mark as dirty the control points connected to the "controlPoint" parameter
        /// </summary>
        /// <param name="dirtyControlPoint">the Control Point to dirty</param>
        /// <param name="dirtyingType">Defines what aspect should be dirtied</param>
        public void SetDirtyPartial(CurvySplineSegment dirtyControlPoint, SplineDirtyingType dirtyingType) =>
            //OPTIM skip this if dirtyControlPoint is already dirty?
            dirtinessManager.SetDirty(
                dirtyControlPoint,
                dirtyingType,
                GetPreviousControlPoint(dirtyControlPoint),
                GetNextControlPoint(dirtyControlPoint),
                true
            );

        /// <summary>
        /// Transforms position from local space to world space
        /// </summary>
        public Vector3 ToWorldPosition(Vector3 localPosition)
            => cachedTransform.TransformPoint(localPosition);

        /// <summary>
        /// Transforms direction from local space to world space
        /// </summary>
        public Vector3 ToWorldDirection(Vector3 localDirection)
            => cachedTransform.TransformDirection(localDirection);

        /// <summary>
        /// Transforms position from world space to local space
        /// </summary>
        public Vector3 ToLocalPosition(Vector3 worldPosition)
            => cachedTransform.InverseTransformPoint(worldPosition);

        /// <summary>
        /// Transforms direction from world space to local space
        /// </summary>
        public Vector3 ToLocalDirection(Vector3 localDirection)
            => cachedTransform.InverseTransformDirection(localDirection);

#if DOCUMENTATION___FORCE_IGNORE___CURVY == false

        /// <summary>
        /// Apply proper names to all Control Points
        /// </summary>
        [System.Diagnostics.Conditional(CompilationSymbols.UnityEditor)]
        public void ApplyControlPointsNames()
        {
            controlPointNamer.RequestRename();
            controlPointNamer.ProcessRequests();
        }

#endif

        /// <summary>
        /// Rebuilds the ControlPoints list from the hierarchy. It sets the spline as Dirty
        /// </summary>
        public void SyncSplineFromHierarchy()
        {
#if CURVY_SANITY_CHECKS
            if (cpsSynchronizer.CurrentRequest == ControlPointsSynchronizer.SynchronizationRequest.SplineToHierarchy)
                Debug.LogError("SyncSplineFromHierarchy is canceling request ");
#endif
            cpsSynchronizer.CancelRequests();
            cpsSynchronizer.RequestHierarchyToSpline();
            cpsSynchronizer.ProcessRequests();
        }

        #endregion

        #region --- Utilities ---

        /// <summary>
        /// Checks if the curve is planar
        /// </summary>
        /// <param name="ignoreAxis">returns the axis that can be ignored (0=x,1=y,2=z)</param>
        /// <returns>true if a planar axis was found</returns>
        [Obsolete]
        [UsedImplicitly]
        public bool IsPlanar(out int ignoreAxis)
        {
            bool xp, yp;
            bool res = IsPlanar(
                out xp,
                out yp,
                out _
            );
            if (xp)
                ignoreAxis = 0;
            else if (yp)
                ignoreAxis = 1;
            else
                ignoreAxis = 2;
            return res;
        }

        /// <summary>
        /// Is the spline a 2D spline contained in one of the main planes?
        /// </summary>
        /// <param name="isYZ">Is spline contained within the YZ plane</param>
        /// <param name="isXZ">Is spline contained within the XZ plane</param>
        /// <param name="isXY">Is spline contained within the XY plane</param>
        /// <returns>True if the control points are planar along at least one axis; otherwise, false.</returns>
        public bool IsPlanar(out bool isYZ, out bool isXZ, out bool isXY)
        {
            isYZ = true;
            isXZ = true;
            isXY = true;
            if (ControlPointCount == 0) return true;
            Vector3 p = ControlPoints[0].transform.localPosition;
            for (int i = 1; i < ControlPointCount; i++)
            {
                if (!Mathf.Approximately(
                        ControlPoints[i].transform.localPosition.x,
                        p.x
                    ))
                    isYZ = false;
                if (!Mathf.Approximately(
                        ControlPoints[i].transform.localPosition.y,
                        p.y
                    ))
                    isXZ = false;
                if (!Mathf.Approximately(
                        ControlPoints[i].transform.localPosition.z,
                        p.z
                    ))
                    isXY = false;

                if (isYZ == false && isXZ == false && isXY == false)
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Determines if the spline is at zero position on a certain plane
        /// </summary>
        /// <param name="plane">the plane the spline should be tested against</param>
        /// <returns>true if the spline is on the plane</returns>
        public bool IsPlanar(CurvyPlane plane)
        {
            switch (plane)
            {
                case CurvyPlane.XY:
                    for (int i = 0; i < ControlPointCount; i++)
                        if (ControlPoints[i].transform.localPosition.z.Approximately(0f) == false)
                            return false;
                    break;
                case CurvyPlane.XZ:
                    for (int i = 0; i < ControlPointCount; i++)
                        if (ControlPoints[i].transform.localPosition.y.Approximately(0f) == false)
                            return false;
                    break;
                case CurvyPlane.YZ:
                    for (int i = 0; i < ControlPointCount; i++)
                        if (ControlPoints[i].transform.localPosition.x.Approximately(0f) == false)
                            return false;
                    break;
            }

            return true;
        }

        /// <summary>
        /// Forces the spline to be at zero position on a certain plane
        /// </summary>
        /// <param name="plane">the plane the should be on</param>
        public void MakePlanar(CurvyPlane plane)
        {
            switch (plane)
            {
                case CurvyPlane.XY:
                    for (int i = 0; i < ControlPointCount; i++)
                        if (ControlPoints[i].transform.localPosition.z != 0)
                        {
#if UNITY_EDITOR
                            Undo.RecordObject(
                                ControlPoints[i].transform,
                                "MakePlanar"
                            );
#endif
                            ControlPoints[i].SetLocalPosition(
                                new Vector3(
                                    ControlPoints[i].transform.localPosition.x,
                                    ControlPoints[i].transform.localPosition.y,
                                    0
                                )
                            );
                        }

                    break;
                case CurvyPlane.XZ:
                    for (int i = 0; i < ControlPointCount; i++)
                        if (ControlPoints[i].transform.localPosition.y != 0)
                        {
#if UNITY_EDITOR
                            Undo.RecordObject(
                                ControlPoints[i].transform,
                                "MakePlanar"
                            );
#endif
                            ControlPoints[i].SetLocalPosition(
                                new Vector3(
                                    ControlPoints[i].transform.localPosition.x,
                                    0,
                                    ControlPoints[i].transform.localPosition.z
                                )
                            );
                        }

                    break;
                case CurvyPlane.YZ:
                    for (int i = 0; i < ControlPointCount; i++)
                        if (ControlPoints[i].transform.localPosition.x != 0)
                        {
#if UNITY_EDITOR
                            Undo.RecordObject(
                                ControlPoints[i].transform,
                                "MakePlanar"
                            );
#endif
                            ControlPoints[i].SetLocalPosition(
                                new Vector3(
                                    0,
                                    ControlPoints[i].transform.localPosition.y,
                                    ControlPoints[i].transform.localPosition.z
                                )
                            );
                        }

                    break;
                default:
                    throw new NotImplementedException();
            }

            Refresh();
        }

        /// <summary>
        /// Equalize one axis of the spline to match the first control points's value
        /// </summary>
        /// <param name="axis">the axis to equalize (0=x,1=y,2=z)</param>
        [UsedImplicitly]
        [Obsolete("No more used in Curvy. Will get removed. Copy it if you still need it")]
        public void MakePlanar(int axis)
        {
            Vector3 p = ControlPoints[0].transform.localPosition;
            for (int i = 1; i < ControlPointCount; i++)
            {
#if UNITY_EDITOR
                Undo.RecordObject(
                    ControlPoints[i].transform,
                    "MakePlanar"
                );
#endif
                Vector3 pi = ControlPoints[i].transform.localPosition;
                switch (axis)
                {
                    case 0:
                        pi.x = p.x;
                        break;
                    case 1:
                        pi.y = p.y;
                        break;
                    case 2:
                        pi.z = p.z;
                        break;
                }

                ControlPoints[i].transform.localPosition = pi;
            }

            SetDirtyAll(
                SplineDirtyingType.Everything,
                true
            );
            Refresh();
        }


        /// <summary>
        /// Subdivides the spline, i.e. adds additional segments to a certain range
        /// </summary>
        /// <param name="fromCP">starting ControlPoint</param>
        /// <param name="toCP">ending ControlPoint</param>
        public void Subdivide(CurvySplineSegment fromCP = null, CurvySplineSegment toCP = null)
        {
            if (!fromCP)
                fromCP = FirstVisibleControlPoint;
            if (!toCP)
                toCP = LastVisibleControlPoint;

            if (fromCP == null || toCP == null || fromCP.Spline != this || toCP.Spline != this)
            {
                Debug.Log("CurvySpline.Subdivide: Not a valid range selection!");
                return;
            }

            int startCPIndex = Mathf.Clamp(
                fromCP.Spline.GetControlPointIndex(fromCP),
                0,
                ControlPointCount - 2
            );
            int endCPIndex = Mathf.Clamp(
                toCP.Spline.GetControlPointIndex(toCP),
                startCPIndex + 1,
                ControlPointCount - 1
            );

            if (endCPIndex - startCPIndex < 1)
                Debug.Log("CurvySpline.Subdivide: Not a valid range selection!");
            else
            {
                OnBeforeControlPointAddEvent(defaultAddAfterEventArgs);

                Dictionary<int, Vector3> newCPsPosition = new Dictionary<int, Vector3>();
                //We first iterate to compute the positions. Interpolate should be called on non Dirty splines, that's why I first call Interpolate then in a latter loop update the spline (which will dirty it)
                for (int i = endCPIndex - 1; i >= startCPIndex; i--)
                    newCPsPosition[i] = ControlPoints[i].Interpolate(0.5f);

                for (int i = endCPIndex - 1; i >= startCPIndex; i--)
                {
                    CurvySplineSegment previousControlPoint = ControlPoints[i];
                    CurvySplineSegment nextControlPoint = ControlPoints[i + 1];
                    CurvySplineSegment newControlPoint =
                        InsertAfter(
                            ControlPoints[i],
                            newCPsPosition[i],
                            true,
                            Space.Self
                        );

                    if (Interpolation == CurvyInterpolation.Bezier)
                    {
#if UNITY_EDITOR
                        Undo.RecordObject(
                            nextControlPoint,
                            "Subdivide"
                        );
                        Undo.RecordObject(
                            previousControlPoint,
                            "Subdivide"
                        );
#endif

                        //Update Bézier handles to maintain the spline's shape
                        //Based on De Casteljau's algorithm. The following is it's special case implementation for a subdivision at the middle of a spline segment.
                        //Here is a picture explaining things: https://jeremykun.files.wordpress.com/2013/05/subdivision.png from https://jeremykun.com/2013/05/11/bezier-curves-and-picasso/

                        Vector3 P0 = previousControlPoint.transform.position;
                        Vector3 P1 = previousControlPoint.HandleOutPosition;
                        Vector3 P2 = nextControlPoint.HandleInPosition;
                        Vector3 P3 = nextControlPoint.transform.position;

                        Vector3 m0 = (P0 + P1) / 2;
                        Vector3 m1 = (P1 + P2) / 2;
                        Vector3 m2 = (P2 + P3) / 2;

                        Vector3 q0 = (m0 + m1) / 2;
                        Vector3 q1 = (m1 + m2) / 2;

                        previousControlPoint.AutoHandles = false;
                        previousControlPoint.HandleOutPosition = m0;

                        nextControlPoint.AutoHandles = false;
                        nextControlPoint.HandleInPosition = m2;

                        newControlPoint.AutoHandles = false;
                        newControlPoint.HandleInPosition = q0;
                        newControlPoint.HandleOutPosition = q1;
                    }
#if UNITY_EDITOR
                    Undo.RegisterCreatedObjectUndo(
                        newControlPoint.gameObject,
                        "Subdivide"
                    );
#endif
                }

                Refresh();
                OnAfterControlPointAddEvent(defaultAddAfterEventArgs);
                OnAfterControlPointChangesEvent(defaultSplineEventArgs);
            }
        }

        /// <summary>
        /// Simplifies the spline, i.e. remove segments from a certain range
        /// </summary>
        /// <param name="fromCP">starting ControlPoint</param>
        /// <param name="toCP">ending ControlPoint</param>
        public void Simplify(CurvySplineSegment fromCP = null, CurvySplineSegment toCP = null)
        {
            if (!fromCP)
                fromCP = FirstVisibleControlPoint;
            if (!toCP)
                toCP = LastVisibleControlPoint;

            if (fromCP == null || toCP == null || fromCP.Spline != this || toCP.Spline != this)
            {
                Debug.Log("CurvySpline.Simplify: Not a valid range selection!");
                return;
            }

            int startCPIndex = Mathf.Clamp(
                fromCP.Spline.GetControlPointIndex(fromCP),
                0,
                ControlPointCount - 2
            );
            int endCPIndex = Mathf.Clamp(
                toCP.Spline.GetControlPointIndex(toCP),
                startCPIndex + 2,
                ControlPointCount - 1
            );
            if (endCPIndex - startCPIndex < 2)
                Debug.Log("CurvySpline.Simplify: Not a valid range selection!");
            else
            {
                OnBeforeControlPointDeleteEvent(defaultDeleteEventArgs);

                for (int i = endCPIndex - 2; i >= startCPIndex; i -= 2)
                    Delete(
                        ControlPoints[i + 1],
                        true
                    );

                Refresh();
                OnAfterControlPointChangesEvent(defaultSplineEventArgs);
            }
        }

        /// <summary>
        /// Equalizes the segment length of a certain range
        /// </summary>
        /// <param name="fromCP">starting ControlPoint</param>
        /// <param name="toCP">ending ControlPoint</param>
        public void Equalize(CurvySplineSegment fromCP = null, CurvySplineSegment toCP = null)
        {
            if (!fromCP)
                fromCP = FirstVisibleControlPoint;
            if (!toCP)
                toCP = LastVisibleControlPoint;

            if (fromCP == null || toCP == null || fromCP.Spline != this || toCP.Spline != this)
            {
                Debug.Log("CurvySpline.Equalize: Not a valid range selection!");
                return;
            }

            int startCPIndex = Mathf.Clamp(
                GetControlPointIndex(fromCP),
                0,
                ControlPointCount - 2
            );
            int endCPIndex = Mathf.Clamp(
                GetControlPointIndex(toCP),
                startCPIndex + 2,
                ControlPointCount - 1
            );
            if (endCPIndex - startCPIndex < 2)
            {
                Debug.Log("CurvySpline.Equalize: Not a valid range selection!");
                return;
            }

            float segmentLength = ControlPoints[endCPIndex].Distance - ControlPoints[startCPIndex].Distance;
            float equal = segmentLength / (endCPIndex - startCPIndex);
            float dist = ControlPoints[startCPIndex].Distance;

            Vector3[] newCpPositions = new Vector3[endCPIndex - startCPIndex - 1];

            for (int i = startCPIndex + 1; i < endCPIndex; i++)
            {
                int iterationIndex = i - startCPIndex - 1;
                newCpPositions[iterationIndex] = InterpolateByDistance(dist + ((iterationIndex + 1) * equal));
            }

            for (int i = startCPIndex + 1; i < endCPIndex; i++)
            {
                int iterationIndex = i - startCPIndex - 1;
#if UNITY_EDITOR
                Undo.RecordObject(
                    ControlPoints[i].transform,
                    "Equalize"
                );
#endif
                ControlPoints[i].SetLocalPosition(newCpPositions[iterationIndex]);
            }

            Refresh();
        }

        /// <summary>
        /// Applies a spline's scale to it's Control Points and resets scale
        /// </summary>
        public void Normalize()
        {
            Vector3 scl = transform.localScale;

            if (scl != Vector3.one)
            {
#if UNITY_EDITOR
                Undo.RecordObject(
                    transform,
                    "Normalize Spline"
                );
#endif
                transform.localScale = Vector3.one;
                for (int i = 0; i < ControlPointCount; i++)
                {
                    CurvySplineSegment curvySplineSegment = ControlPoints[i];
#if UNITY_EDITOR
                    Undo.RecordObject(
                        curvySplineSegment.transform,
                        "Normalize Spline"
                    );
                    Undo.RecordObject(
                        curvySplineSegment,
                        "Normalize Spline"
                    );
#endif
                    curvySplineSegment.SetLocalPosition(
                        Vector3.Scale(
                            curvySplineSegment.transform.localPosition,
                            scl
                        )
                    );
                    curvySplineSegment.HandleIn = Vector3.Scale(
                        curvySplineSegment.HandleIn,
                        scl
                    );
                    curvySplineSegment.HandleOut = Vector3.Scale(
                        curvySplineSegment.HandleOut,
                        scl
                    );
                }

                Refresh();
            }
        }

        /// <summary>
        /// Sets the pivot of the spline
        /// </summary>
        /// <param name="xRel">-1 to 1</param>
        /// <param name="yRel">-1 to 1</param>
        /// <param name="zRel">-1 to 1</param>
        /// <param name="preview">if true, only return the new pivot position</param>
        /// <returns>the new pivot position</returns>
        public Vector3 SetPivot(float xRel = 0, float yRel = 0, float zRel = 0, bool preview = false)
        {
            Bounds b = Bounds;
            Vector3 v = new Vector3(
                b.min.x + (b.size.x * ((xRel + 1) / 2)),
                b.max.y - (b.size.y * ((yRel + 1) / 2)),
                b.min.z + (b.size.z * ((zRel + 1) / 2))
            );

            Vector3 off = transform.position - v;
            if (preview)
                return transform.position - off;

            for (int index = 0; index < ControlPoints.Count; index++)
            {
                CurvySplineSegment cp = ControlPoints[index];
#if UNITY_EDITOR
                Undo.RecordObject(
                    cp.transform,
                    "SetPivot"
                );
#endif
                cp.transform.position += off;
            }
#if UNITY_EDITOR
            Undo.RecordObject(
                transform,
                "SetPivot"
            );
#endif
            transform.position -= off;
            SetDirtyAll(
                SplineDirtyingType.Everything,
                IsActiveAndEnabled
            );
            return transform.position;
        }

        /// <summary>
        /// Flips the direction of the spline, i.e. the first Control Point will become the last and vice versa.
        /// </summary>
        public void Flip()
        {
            if (ControlPointCount <= 1)
                return;
#if UNITY_EDITOR
            Undo.RegisterFullObjectHierarchyUndo(
                this,
                "Flip Spline"
            );
#endif
            switch (Interpolation)
            {
                case CurvyInterpolation.TCB:
                    Bias *= -1;
                    for (int i = ControlPointCount - 1; i >= 0; i--)
                    {
                        CurvySplineSegment cur = ControlPoints[i];

                        int j = i - 1;
                        if (j >= 0)
                        {
                            CurvySplineSegment prev = ControlPoints[j];

                            cur.EndBias = prev.StartBias * -1;
                            cur.EndContinuity = prev.StartContinuity;
                            cur.EndTension = prev.StartTension;

                            cur.StartBias = prev.EndBias * -1;
                            cur.StartContinuity = prev.EndContinuity;
                            cur.StartTension = prev.EndTension;

                            cur.OverrideGlobalBias = prev.OverrideGlobalBias;
                            cur.OverrideGlobalContinuity = prev.OverrideGlobalContinuity;
                            cur.OverrideGlobalTension = prev.OverrideGlobalTension;

                            cur.SynchronizeTCB = prev.SynchronizeTCB;
                        }
                    }

                    break;
                case CurvyInterpolation.Bezier:
                    for (int i = ControlPointCount - 1; i >= 0; i--)
                    {
                        CurvySplineSegment cur = ControlPoints[i];

                        (cur.HandleIn, cur.HandleOut) = (cur.HandleOut, cur.HandleIn);
                    }

                    break;
            }

            ReverseControlPoints();
            Refresh();
        }

        /// <summary>
        /// Moves ControlPoints from this spline, inserting them after a destination ControlPoint of another spline
        /// </summary>
        /// <param name="startIndex">ControlPointIndex of the first CP to move</param>
        /// <param name="count">number of ControlPoints to move</param>
        /// <param name="destCP">ControlPoint at the destination spline to insert after</param>
        public void MoveControlPoints(int startIndex, int count, CurvySplineSegment destCP)
        {
            if (!destCP || this == destCP.Spline || destCP.Spline.GetControlPointIndex(destCP) == -1)
                return;
            startIndex = Mathf.Clamp(
                startIndex,
                0,
                ControlPointCount - 1
            );
            count = Mathf.Clamp(
                count,
                startIndex,
                ControlPointCount - startIndex
            );

            for (int i = 0; i < count; i++)
            {
                CurvySplineSegment cp = ControlPoints[startIndex];
                RemoveControlPoint(cp);
                destCP.Spline.InsertControlPoint(
                    destCP.Spline.GetControlPointIndex(destCP) + i + 1,
                    cp
                );
                cp.transform.UndoableSetParent(
                    destCP.Spline.transform,
                    true,
                    "Move ControlPoints"
                );
            }

            Refresh();
            destCP.Spline.Refresh();
        }

        /// <summary>
        /// Insert this spline after another spline's destination Control Point and delete this spline
        /// </summary>
        /// <param name="destCP">the Control Point of the destination spline</param>
        public void JoinWith(CurvySplineSegment destCP)
        {
            if (destCP.Spline == this)
                return;
            MoveControlPoints(
                0,
                ControlPointCount,
                destCP
            );
            gameObject.Destroy(
                true,
                true
            );
        }

        /// <summary>
        /// Splits this spline with the parameter controlPoint becoming the first Control Point of the new spline
        /// </summary>
        /// <returns>The new spline</returns>
        public CurvySpline Split(CurvySplineSegment controlPoint)
        {
#if CURVY_SANITY_CHECKS
            if (ReferenceEquals(
                    this,
                    controlPoint.Spline
                )
                == false)
                throw new ArgumentException(
                    String.Format(
                        InvalidCPErrorMessage,
                        controlPoint,
                        name
                    )
                );
#endif

            const string undoOperationName = "Split Spline";

            CurvySpline newSpline = Create(this);
            newSpline.transform.UndoableSetParent(
                transform.parent,
                true,
                undoOperationName
            );
            newSpline.name = name + "_parted";

#if UNITY_EDITOR
            Undo.RegisterCreatedObjectUndo(
                newSpline.gameObject,
                undoOperationName
            );
#endif

            // Move CPs
            List<CurvySplineSegment> affectedControlPoints;
            {
                int controlPointIndex = GetSegmentIndex(controlPoint);
                affectedControlPoints = new List<CurvySplineSegment>(ControlPointCount - controlPointIndex);
                for (int i = controlPointIndex; i < ControlPointCount; i++)
                    affectedControlPoints.Add(ControlPoints[i]);
            }

            for (int i = 0; i < affectedControlPoints.Count; i++)
            {
                CurvySplineSegment curvySplineSegment = affectedControlPoints[i];
                RemoveControlPoint(curvySplineSegment);
                newSpline.AddControlPoint(
                    curvySplineSegment,
                    true,
                    true
                );
                curvySplineSegment.transform.UndoableSetParent(
                    newSpline.transform,
                    true,
                    undoOperationName
                );
            }

            Refresh();
            newSpline.Refresh();
            return newSpline;
        }

        /// <summary>
        /// Defines the given Control Point to be the first Control Point of the spline
        /// </summary>
        /// <param name="controlPoint">One of this spline's control points</param>
        public void SetFirstControlPoint(CurvySplineSegment controlPoint)
        {
#if CURVY_SANITY_CHECKS
            if (ReferenceEquals(
                    this,
                    controlPoint.Spline
                )
                == false)
                throw new ArgumentException(
                    String.Format(
                        InvalidCPErrorMessage,
                        controlPoint,
                        name
                    )
                );
#endif

            short controlPointIndex = GetControlPointIndex(controlPoint);
            CurvySplineSegment[] controlPointsToMove = new CurvySplineSegment[controlPointIndex];
            for (int i = 0; i < controlPointIndex; i++)
                controlPointsToMove[i] = ControlPoints[i];

            for (int index = 0; index < controlPointsToMove.Length; index++)
            {
                CurvySplineSegment seg = controlPointsToMove[index];
                RemoveControlPoint(seg);
                AddControlPoint(
                    seg,
                    true,
                    true
                );
            }

            Refresh();
        }

        #endregion

        #region Query data about control points

        /// <summary>
        /// Is the control point an orientation anchor? The answer is related to the control point's serialized OrientationAnchor value, plus it's position in the spline.
        /// </summary>
        /// <param name="controlPoint"></param>
        /// <returns></returns>
        public bool IsControlPointAnOrientationAnchor(CurvySplineSegment controlPoint)
        {
#if CURVY_SANITY_CHECKS
            if (ReferenceEquals(
                    this,
                    controlPoint.Spline
                )
                == false)
                throw new ArgumentException(
                    String.Format(
                        InvalidCPErrorMessage,
                        controlPoint,
                        name
                    )
                );
#endif

            return IsControlPointAnOrientationAnchor(
                IsControlPointVisible(controlPoint),
                controlPoint.SerializedOrientationAnchor,
                ReferenceEquals(
                    controlPoint,
                    FirstVisibleControlPoint
                ),
                ReferenceEquals(
                    controlPoint,
                    LastVisibleControlPoint
                )
            );
        }

        /// <summary>
        /// Can this control point have a Follow-Up? This is true if the control point is the beginning of the first segment or the end of the last segment of an open spline
        /// </summary>
        /// <param name="controlPoint"></param>
        /// <returns></returns>
        public bool CanControlPointHaveFollowUp([NotNull] CurvySplineSegment controlPoint)
        {
#if CURVY_SANITY_CHECKS
            if (ReferenceEquals(
                    this,
                    controlPoint.Spline
                )
                == false)
                throw new ArgumentException(
                    String.Format(
                        InvalidCPErrorMessage,
                        controlPoint,
                        name
                    )
                );
#endif

            relationshipCache.EnsureIsValid();
            return controlPoint.GetExtrinsicPropertiesINTERNAL().CanHaveFollowUp;
        }

        /// <summary>
        /// Index of the control point
        /// </summary>
        /// <param name="controlPoint"></param>
        /// <returns></returns>
        public short GetControlPointIndex([NotNull] CurvySplineSegment controlPoint)
        {
#if CURVY_SANITY_CHECKS
            if (ReferenceEquals(
                    this,
                    controlPoint.Spline
                )
                == false)
                throw new ArgumentException(
                    String.Format(
                        InvalidCPErrorMessage,
                        controlPoint,
                        name
                    )
                );
#endif

            relationshipCache.EnsureIsValid();
            short controlPointIndex = controlPoint.GetExtrinsicPropertiesINTERNAL().ControlPointIndex;
#if CURVY_SANITY_CHECKS_PRIVATE //Judged too expensive in cpu time to be part of the sanity checks available to users
            Assert.IsTrue(controlPoint == ControlPoints[controlPointIndex]);
#endif

            return controlPointIndex;
        }

        /// <summary>
        /// Index of the segment that this control point starts. -1 if control point does not start a segment.
        /// </summary>
        /// <param name="segment"></param>
        /// <returns></returns>
        public short GetSegmentIndex([NotNull] CurvySplineSegment segment)
        {
#if CURVY_SANITY_CHECKS
            if (ReferenceEquals(
                    this,
                    segment.Spline
                )
                == false)
                throw new ArgumentException(
                    String.Format(
                        InvalidCPErrorMessage,
                        segment,
                        name
                    )
                );
#endif

            relationshipCache.EnsureIsValid();
            short segementIndex = segment.GetExtrinsicPropertiesINTERNAL().SegmentIndex;
#if CURVY_SANITY_CHECKS_PRIVATE //Judged too expensive in cpu time to be part of the sanity checks available to users
            Assert.IsTrue(segementIndex == -1 || segment == mSegments[segementIndex]);
#endif
            return segementIndex;
        }

        /// <summary>
        /// The next control point on the spline. Is null if none. Follow-Up not considered
        /// </summary>
        /// <param name="controlPoint"></param>
        /// <returns></returns>
        [CanBeNull]
        public CurvySplineSegment GetNextControlPoint([NotNull] CurvySplineSegment controlPoint)
        {
#if CURVY_SANITY_CHECKS
            if (ReferenceEquals(
                    this,
                    controlPoint.Spline
                )
                == false)
                throw new ArgumentException(
                    String.Format(
                        InvalidCPErrorMessage,
                        controlPoint,
                        name
                    )
                );
#endif

            relationshipCache.EnsureIsValid();
            short nextControlPointIndex = controlPoint.GetExtrinsicPropertiesINTERNAL().NextControlPointIndex;
            return nextControlPointIndex == -1
                ? null
                : ControlPoints[nextControlPointIndex];
        }

        /// <summary>
        /// The index of the next control point on the spline. Is -1 if none. Follow-Up not considered
        /// </summary>
        /// <param name="controlPoint"></param>
        /// <returns></returns>
        [CanBeNull]
        public short GetNextControlPointIndex([NotNull] CurvySplineSegment controlPoint)
        {
#if CURVY_SANITY_CHECKS
            if (ReferenceEquals(
                    this,
                    controlPoint.Spline
                )
                == false)
                throw new ArgumentException(
                    String.Format(
                        InvalidCPErrorMessage,
                        controlPoint,
                        name
                    )
                );
#endif

            relationshipCache.EnsureIsValid();
            return controlPoint.GetExtrinsicPropertiesINTERNAL().NextControlPointIndex;
        }

        /// <summary>
        /// The next control point. Is null if none. Follow-Up is considered
        /// </summary>
        /// <param name="controlPoint"></param>
        /// <returns></returns>
        [CanBeNull]
        public CurvySplineSegment GetNextControlPointUsingFollowUp([NotNull] CurvySplineSegment controlPoint)
        {
#if CURVY_SANITY_CHECKS
            if (ReferenceEquals(
                    this,
                    controlPoint.Spline
                )
                == false)
                throw new ArgumentException(
                    String.Format(
                        InvalidCPErrorMessage,
                        controlPoint,
                        name
                    )
                );
#endif

            if (controlPoint.FollowUp == null
                || ReferenceEquals(
                    LastVisibleControlPoint,
                    controlPoint
                )
                == false)
                return GetNextControlPoint(controlPoint);

#if CURVY_SANITY_CHECKS
            if (controlPoint.FollowUp.Spline == null)
                throw new InvalidOperationException("Follow-Up spline is null");
#endif
            return GetFollowUpHeadingControlPoint(
                controlPoint.FollowUp,
                controlPoint.FollowUpHeading
            );
        }

        /// <summary>
        /// The previous control point on the spline. Is null if none. Follow-Up not considered
        /// </summary>
        /// <param name="controlPoint"></param>
        /// <returns></returns>
        [CanBeNull]
        public CurvySplineSegment GetPreviousControlPoint([NotNull] CurvySplineSegment controlPoint)
        {
#if CURVY_SANITY_CHECKS
            if (ReferenceEquals(
                    this,
                    controlPoint.Spline
                )
                == false)
                throw new ArgumentException(
                    String.Format(
                        InvalidCPErrorMessage,
                        controlPoint,
                        name
                    )
                );
#endif

            relationshipCache.EnsureIsValid();
            short previousControlPointIndex = controlPoint.GetExtrinsicPropertiesINTERNAL().PreviousControlPointIndex;
            return previousControlPointIndex == -1
                ? null
                : ControlPoints[previousControlPointIndex];
        }

        /// <summary>
        /// The index of the previous control point on the spline. Is -1 if none. Follow-Up not considered
        /// </summary>
        /// <param name="controlPoint"></param>
        /// <returns></returns>
        [CanBeNull]
        public short GetPreviousControlPointIndex([NotNull] CurvySplineSegment controlPoint)
        {
#if CURVY_SANITY_CHECKS
            if (ReferenceEquals(
                    this,
                    controlPoint.Spline
                )
                == false)
                throw new ArgumentException(
                    String.Format(
                        InvalidCPErrorMessage,
                        controlPoint,
                        name
                    )
                );
#endif

            relationshipCache.EnsureIsValid();
            return controlPoint.GetExtrinsicPropertiesINTERNAL().PreviousControlPointIndex;
        }

        /// <summary>
        /// The previous control point. Is null if none. Follow-Up is considered
        /// </summary>
        /// <param name="controlPoint"></param>
        /// <returns></returns>
        [CanBeNull]
        public CurvySplineSegment GetPreviousControlPointUsingFollowUp([NotNull] CurvySplineSegment controlPoint)
        {
#if CURVY_SANITY_CHECKS
            if (ReferenceEquals(
                    this,
                    controlPoint.Spline
                )
                == false)
                throw new ArgumentException(
                    String.Format(
                        InvalidCPErrorMessage,
                        controlPoint,
                        name
                    )
                );
#endif

            if (controlPoint.FollowUp == null
                || ReferenceEquals(
                    FirstVisibleControlPoint,
                    controlPoint
                )
                == false)
                return GetPreviousControlPoint(controlPoint);

#if CURVY_SANITY_CHECKS
            if (controlPoint.FollowUp.Spline == null)
                throw new InvalidOperationException("Follow-Up spline is null");
#endif

            return GetFollowUpHeadingControlPoint(
                controlPoint.FollowUp,
                controlPoint.FollowUpHeading
            );
        }

        /// <summary>
        /// The next control point on the spline if it starts a segment. Is null if none. Follow-Up not considered
        /// </summary>
        /// <param name="segment"></param>
        /// <returns></returns>
        [CanBeNull]
        public CurvySplineSegment GetNextSegment([NotNull] CurvySplineSegment segment)
        {
#if CURVY_SANITY_CHECKS
            if (ReferenceEquals(
                    this,
                    segment.Spline
                )
                == false)
                throw new ArgumentException(
                    String.Format(
                        InvalidCPErrorMessage,
                        segment,
                        name
                    )
                );
#endif

            relationshipCache.EnsureIsValid();

            CurvySplineSegment.ControlPointExtrinsicProperties cpExtrinsicProperties = segment.GetExtrinsicPropertiesINTERNAL();
            return cpExtrinsicProperties.NextControlPointIsSegment
                ? ControlPoints[cpExtrinsicProperties.NextControlPointIndex]
                : null;
        }

        /// <summary>
        /// The previous control point on the spline if it starts a segment. Is null if none. Follow-Up not considered. 
        /// </summary>
        /// <param name="segment"></param>
        /// <returns></returns>
        [CanBeNull]
        public CurvySplineSegment GetPreviousSegment([NotNull] CurvySplineSegment segment)
        {
#if CURVY_SANITY_CHECKS
            if (ReferenceEquals(
                    this,
                    segment.Spline
                )
                == false)
                throw new ArgumentException(
                    String.Format(
                        InvalidCPErrorMessage,
                        segment,
                        name
                    )
                );
#endif

            relationshipCache.EnsureIsValid();
            CurvySplineSegment.ControlPointExtrinsicProperties cpExtrinsicProperties = segment.GetExtrinsicPropertiesINTERNAL();
            return cpExtrinsicProperties.PreviousControlPointIsSegment
                ? ControlPoints[cpExtrinsicProperties.PreviousControlPointIndex]
                : null;
        }

        /// <summary>
        /// Is the control point the start of a segment?
        /// </summary>
        /// <param name="controlPoint"></param>
        /// <returns></returns>
        public bool IsControlPointASegment([NotNull] CurvySplineSegment controlPoint)
        {
#if CURVY_SANITY_CHECKS
            if (ReferenceEquals(
                    this,
                    controlPoint.Spline
                )
                == false)
                throw new ArgumentException(
                    String.Format(
                        InvalidCPErrorMessage,
                        controlPoint,
                        name
                    )
                );
#endif

            relationshipCache.EnsureIsValid();
            return controlPoint.GetExtrinsicPropertiesINTERNAL().IsSegment;
        }

        /// <summary>
        /// Is the control point part of a segment (whether starting it or ending it)
        /// </summary>
        /// <param name="controlPoint"></param>
        /// <returns></returns>
        public bool IsControlPointVisible([NotNull] CurvySplineSegment controlPoint)
        {
#if CURVY_SANITY_CHECKS
            if (ReferenceEquals(
                    this,
                    controlPoint.Spline
                )
                == false)
                throw new ArgumentException(
                    String.Format(
                        InvalidCPErrorMessage,
                        controlPoint,
                        name
                    )
                );
#endif

            relationshipCache.EnsureIsValid();
            return controlPoint.GetExtrinsicPropertiesINTERNAL().IsVisible;
        }

        /// <summary>
        /// The index of the control point being the orientation anchor for the anchor group containing the controlPoint
        /// Is -1 for non visible control points
        /// </summary>
        [UsedImplicitly]
        [Obsolete(
            "No more used, will be removed. If you need this method, you can use IsControlPointAnOrientationAnchor while traversing the spline's control points to find the needed information."
        )]
        public short GetControlPointOrientationAnchorIndex([NotNull] CurvySplineSegment controlPoint)
        {
#if CURVY_SANITY_CHECKS
            if (ReferenceEquals(
                    this,
                    controlPoint.Spline
                )
                == false)
                throw new ArgumentException(
                    String.Format(
                        InvalidCPErrorMessage,
                        controlPoint,
                        name
                    )
                );
#endif

            return GetOrientationAnchorIndices()[GetControlPointIndex(controlPoint)];
        }

        #endregion

        /// <summary>
        /// Event-friendly helper that sets a field or property value
        /// </summary>
        /// <param name="fieldAndValue">e.g. "MyValue=42"</param>
        public void SetFromString(string fieldAndValue)
        {
            string[] f = fieldAndValue.Split('=');
            if (f.Length != 2)
                return;

            FieldInfo fi = GetType().FieldByName(
                f[0],
                true
            );
            if (fi != null)
                try
                {
                    if (fi.FieldType.IsEnum)
                        fi.SetValue(
                            this,
                            Enum.Parse(
                                fi.FieldType,
                                f[1]
                            )
                        );
                    else
                        fi.SetValue(
                            this,
                            Convert.ChangeType(
                                f[1],
                                fi.FieldType,
                                CultureInfo.InvariantCulture
                            )
                        );
                }
                catch (Exception e)
                {
                    Debug.LogWarning(name + ".SetFromString(): " + e);
                }
            else
            {
                PropertyInfo pi = GetType().PropertyByName(
                    f[0],
                    true
                );
                if (pi != null)
                    try
                    {
                        if (pi.PropertyType.IsEnum)
                            pi.SetValue(
                                this,
                                Enum.Parse(
                                    pi.PropertyType,
                                    f[1]
                                ),
                                null
                            );
                        else
                            pi.SetValue(
                                this,
                                Convert.ChangeType(
                                    f[1],
                                    pi.PropertyType,
                                    CultureInfo.InvariantCulture
                                ),
                                null
                            );
                    }
                    catch (Exception e)
                    {
                        Debug.LogWarning(name + ".SetFromString(): " + e);
                    }
            }
        }

        #endregion
    }
}