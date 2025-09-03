// =====================================================================
// Copyright © 2013 ToolBuddy
// All rights reserved
// 
// http://www.toolbuddy.net
// =====================================================================

using System;
using System.Collections.Generic;
using FluffyUnderware.Curvy.Pools;
using FluffyUnderware.DevTools;
using JetBrains.Annotations;
using UnityEngine;

namespace FluffyUnderware.Curvy
{
    /// <summary>
    /// Curvy Global Scene Manager component
    /// </summary>
    [ExecuteInEditMode]
    [RequireComponent(typeof(PoolManager))]
    [RequireComponent(typeof(ArrayPoolsSettings))]
    [HelpURL(AssetInformation.DocsRedirectionBaseUrl + "curvyglobalmanager")]
    //TODO split this class in two, one containing monobehaviours, that should inherit from dtsingleton, the other containing non monobehaviours, that would use a regular singleton (CurvyProject?). The goal is to avoid instantiating CurvyGlobalManager (which in expensive, see DTSingleton.Instance) when not needed
    public class CurvyGlobalManager : DTSingleton<CurvyGlobalManager>
    {
        #region Do not move these

        //Do not move these. If they are moved to another file or lower in this file (bellow their usage by say DefaultGizmoColor), this issue will happen:
        // https://stackoverflow.com/questions/22927167/strange-behavior-on-static-members-of-a-class-hows-this-possible
        // https://github.com/RalfKoban/MiKo-Analyzers/issues/295

        /// <summary>
        /// Default value of the <see cref="DefaultGizmoColor"/> 
        /// </summary>
        public static readonly Color DefaultDefaultGizmoColor = new Color(
            0.71f,
            0.71f,
            0.71f
        );

        /// <summary>
        /// Default value of the <see cref="DefaultGizmoSelectionColor"/>
        /// </summary>
        public static readonly Color DefaultDefaultGizmoSelectionColor = new Color(
            1f,
            0.4f,
            0f
        );

        /// <summary>
        /// Default value of the <see cref="GizmoOrientationColor"/>
        /// </summary>
        public static readonly Color DefaultGizmoOrientationColor = new Color(
            0.75f,
            0.75f,
            0.4f
        );

        #endregion

        #region ### Public Static Fields (Editor->Runtime Bridge) ###

        public static bool HideManager;

        /// <summary>
        /// Whether the output of Curvy Generators should be saved in the scene file.
        /// Disable this option to reduce the size of scene files. This might increase the saving time for complex scenes.
        /// This option applies only on generators that are enabled and have Auto Refresh set to true
        /// </summary>
        public static bool SaveGeneratorOutputs = true;

        /// <summary>
        /// Resolution of SceneView spline rendering
        /// </summary>
        public static float SceneViewResolution = 0.5f;

        /// <summary>
        /// Default spline color
        /// </summary>
        public static Color DefaultGizmoColor = DefaultDefaultGizmoColor;

        /// <summary>
        /// Default selected spline color
        /// </summary>
        public static Color DefaultGizmoSelectionColor = DefaultDefaultGizmoSelectionColor;

        /// <summary>
        /// Default interpolation used by new splines
        /// </summary>
        public static CurvyInterpolation DefaultInterpolation = CurvyInterpolation.Bezier;

        /// <summary>
        /// Size of control point gizmos
        /// </summary>
        public static float GizmoControlPointSize = 0.15f;

        /// <summary>
        /// Size of orientation gizmo
        /// </summary>
        public static float GizmoOrientationLength = 1f;

        /// <summary>
        /// Orientation gizmo color
        /// </summary>
        public static Color GizmoOrientationColor = DefaultGizmoOrientationColor;

        public static int SplineLayer;

        /// <summary>
        /// Default view settings
        /// </summary>
        public static CurvySplineGizmos Gizmos = CurvySplineGizmos.Curve | CurvySplineGizmos.Connections;

        public static bool ShowCurveGizmo
        {
            get => (Gizmos & CurvySplineGizmos.Curve) == CurvySplineGizmos.Curve;
            set
            {
                if (value)
                    Gizmos |= CurvySplineGizmos.Curve;
                else
                    Gizmos &= ~CurvySplineGizmos.Curve;
            }
        }

        public static bool ShowConnectionsGizmo
        {
            get => (Gizmos & CurvySplineGizmos.Connections) == CurvySplineGizmos.Connections;
            set
            {
                if (value)
                    Gizmos |= CurvySplineGizmos.Connections;
                else
                    Gizmos &= ~CurvySplineGizmos.Connections;
            }
        }

        public static bool ShowApproximationGizmo
        {
            get => (Gizmos & CurvySplineGizmos.Approximation) == CurvySplineGizmos.Approximation;
            set
            {
                if (value)
                    Gizmos |= CurvySplineGizmos.Approximation;
                else
                    Gizmos &= ~CurvySplineGizmos.Approximation;
            }
        }

        public static bool ShowTangentsGizmo
        {
            get => (Gizmos & CurvySplineGizmos.Tangents) == CurvySplineGizmos.Tangents;
            set
            {
                if (value)
                    Gizmos |= CurvySplineGizmos.Tangents;
                else
                    Gizmos &= ~CurvySplineGizmos.Tangents;
            }
        }

        public static bool ShowOrientationGizmo
        {
            get => (Gizmos & CurvySplineGizmos.Orientation) == CurvySplineGizmos.Orientation;
            set
            {
                if (value)
                    Gizmos |= CurvySplineGizmos.Orientation;
                else
                    Gizmos &= ~CurvySplineGizmos.Orientation;
            }
        }

        public static bool ShowTFsGizmo
        {
            get => (Gizmos & CurvySplineGizmos.TFs) == CurvySplineGizmos.TFs;
            set
            {
                if (value)
                    Gizmos |= CurvySplineGizmos.TFs;
                else
                    Gizmos &= ~CurvySplineGizmos.TFs;
            }
        }

        public static bool ShowRelativeDistancesGizmo
        {
            get => (Gizmos & CurvySplineGizmos.RelativeDistances) == CurvySplineGizmos.RelativeDistances;
            set
            {
                if (value)
                    Gizmos |= CurvySplineGizmos.RelativeDistances;
                else
                    Gizmos &= ~CurvySplineGizmos.RelativeDistances;
            }
        }

        public static bool ShowLabelsGizmo
        {
            get => (Gizmos & CurvySplineGizmos.Labels) == CurvySplineGizmos.Labels;
            set
            {
                if (value)
                    Gizmos |= CurvySplineGizmos.Labels;
                else
                    Gizmos &= ~CurvySplineGizmos.Labels;
            }
        }

        public static bool ShowMetadataGizmo
        {
            get => (Gizmos & CurvySplineGizmos.Metadata) == CurvySplineGizmos.Metadata;
            set
            {
                if (value)
                    Gizmos |= CurvySplineGizmos.Metadata;
                else
                    Gizmos &= ~CurvySplineGizmos.Metadata;
            }
        }

        public static bool ShowBoundsGizmo
        {
            get => (Gizmos & CurvySplineGizmos.Bounds) == CurvySplineGizmos.Bounds;
            set
            {
                if (value)
                    Gizmos |= CurvySplineGizmos.Bounds;
                else
                    Gizmos &= ~CurvySplineGizmos.Bounds;
            }
        }

        public static bool ShowOrientationAnchorsGizmo
        {
            get => (Gizmos & CurvySplineGizmos.OrientationAnchors) == CurvySplineGizmos.OrientationAnchors;
            set
            {
                if (value)
                    Gizmos |= CurvySplineGizmos.OrientationAnchors;
                else
                    Gizmos &= ~CurvySplineGizmos.OrientationAnchors;
            }
        }

        #endregion

        #region ### Private Fields ###

        private PoolManager poolManager;
        private ComponentPool controlPointPool;
        private ArrayPoolsSettings arrayPoolsSettings;

        #endregion

        #region ### Public Methods & Properties ###

        /// <summary>
        /// Gets the PoolManager
        /// </summary>
        public PoolManager PoolManager
        {
            get
            {
                if (poolManager == null)
                    poolManager = GetComponent<PoolManager>();
                return poolManager;
            }
        }

        public ComponentPool ControlPointPool => controlPointPool;

        public ArrayPoolsSettings ArrayPoolsSettings
        {
            get
            {
                if (arrayPoolsSettings == null)
                    arrayPoolsSettings = GetComponent<ArrayPoolsSettings>();
                return arrayPoolsSettings;
            }
        }

        /// <summary>
        /// Gets all connections in the scene
        /// </summary>
        public CurvyConnection[] Connections => GetComponentsInChildren<CurvyConnection>();

        /// <summary>
        /// Returns all the connections that are exclusively connecting cps within the splines parameter
        /// </summary>
        /// <param name="splines"></param>
        /// <returns></returns>
        [UsedImplicitly]
        [Obsolete("No more used in Curvy. Will get removed. Copy it if you still need it")]
        public CurvyConnection[] GetContainingConnections(params CurvySpline[] splines)
        {
            List<CurvyConnection> connectionsResult = new List<CurvyConnection>();
            List<CurvySpline> splinesList = new List<CurvySpline>(splines);
            foreach (CurvySpline spline in splinesList)
            {
                foreach (CurvySplineSegment controlPoint in spline.ControlPointsList)
                    if (controlPoint.Connection != null && !connectionsResult.Contains(controlPoint.Connection))
                    {
                        bool add = true;
                        // only process connections if all involved splines are part of the prefab
                        foreach (CurvySplineSegment connectedControlPoint in controlPoint.Connection.ControlPointsList)
                            if (connectedControlPoint.Spline != null && !splinesList.Contains(connectedControlPoint.Spline))
                            {
                                add = false;
                                break;
                            }

                        if (add)
                            connectionsResult.Add(controlPoint.Connection);
                    }
            }

            return connectionsResult.ToArray();
        }

        #endregion

        #region ### Unity Callbacks ###

#if DOCUMENTATION___FORCE_IGNORE___UNITY == false
        public override void Awake()
        {
            base.Awake();

            //can be destroyed in base.Awake()
            if (this == null)
                return;

            name = "_CurvyGlobal_";
            transform.SetAsLastSibling();
            //todo is the following still needed?
            // Unity 5.3 introduces buug that hides GameObject when calling this outside playmode!
            if (Application.isPlaying)
                DontDestroyOnLoad(this);

            poolManager = GetComponent<PoolManager>();

            controlPointPool = poolManager.CreateComponentPool<CurvySplineSegment>(new PoolSettings());

            arrayPoolsSettings = GetComponent<ArrayPoolsSettings>();
            //this is needed even though there is a [RequireComponent(typeof(ArrayPoolsSettings))] attribute, because that attribute works only at the moment the component is added, and does nothing for previously existing instances
            if (arrayPoolsSettings == null)
                arrayPoolsSettings = gameObject.AddComponent<ArrayPoolsSettings>();
        }

        [UsedImplicitly]
        private void Start()
        {
            if (HideManager)
                gameObject.hideFlags = HideFlags.HideInHierarchy;
            else
                gameObject.hideFlags = HideFlags.None;
        }

#endif

        #endregion

        #region ### Privates & Internals ###

#if DOCUMENTATION___FORCE_IGNORE___CURVY == false

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        [UsedImplicitly]
        private static void InitializeOnLoad() =>
            InitializeStaticFields();

        [RuntimeInitializeOnLoadMethod]
        [UsedImplicitly]
        private static void LoadRuntimeSettings()
        {
            if (!PlayerPrefs.HasKey("Curvy_MaxCachePPU"))
                SaveRuntimeSettings();
            SceneViewResolution = DTUtility.GetPlayerPrefs(
                "Curvy_SceneViewResolution",
                SceneViewResolution
            );
            HideManager = DTUtility.GetPlayerPrefs(
                "Curvy_HideManager",
                HideManager
            );
            DefaultGizmoColor = DTUtility.GetPlayerPrefs(
                "Curvy_DefaultGizmoColor",
                DefaultGizmoColor
            );
            DefaultGizmoSelectionColor = DTUtility.GetPlayerPrefs(
                "Curvy_DefaultGizmoSelectionColor",
                DefaultGizmoColor
            );
            DefaultInterpolation = DTUtility.GetPlayerPrefs(
                "Curvy_DefaultInterpolation",
                DefaultInterpolation
            );
            GizmoControlPointSize = DTUtility.GetPlayerPrefs(
                "Curvy_ControlPointSize",
                GizmoControlPointSize
            );
            GizmoOrientationLength = DTUtility.GetPlayerPrefs(
                "Curvy_OrientationLength",
                GizmoOrientationLength
            );
            GizmoOrientationColor = DTUtility.GetPlayerPrefs(
                "Curvy_OrientationColor",
                GizmoOrientationColor
            );
            Gizmos = DTUtility.GetPlayerPrefs(
                "Curvy_Gizmos",
                Gizmos
            );
            SplineLayer = DTUtility.GetPlayerPrefs(
                "Curvy_SplineLayer",
                SplineLayer
            );
            SaveGeneratorOutputs = DTUtility.GetPlayerPrefs(
                "Curvy_SaveGeneratorOutputs",
                SaveGeneratorOutputs
            );
        }

        public static void SaveRuntimeSettings()
        {
            //TODO some of these are not runtime settings at all, fix that
            DTUtility.SetPlayerPrefs(
                "Curvy_SceneViewResolution",
                SceneViewResolution
            );
            DTUtility.SetPlayerPrefs(
                "Curvy_HideManager",
                HideManager
            );
            DTUtility.SetPlayerPrefs(
                "Curvy_DefaultGizmoColor",
                DefaultGizmoColor
            );
            DTUtility.SetPlayerPrefs(
                "Curvy_DefaultGizmoSelectionColor",
                DefaultGizmoSelectionColor
            );
            DTUtility.SetPlayerPrefs(
                "Curvy_DefaultInterpolation",
                DefaultInterpolation
            );
            DTUtility.SetPlayerPrefs(
                "Curvy_ControlPointSize",
                GizmoControlPointSize
            );
            DTUtility.SetPlayerPrefs(
                "Curvy_OrientationLength",
                GizmoOrientationLength
            );
            DTUtility.SetPlayerPrefs(
                "Curvy_OrientationColor",
                GizmoOrientationColor
            );
            DTUtility.SetPlayerPrefs(
                "Curvy_Gizmos",
                Gizmos
            );
            DTUtility.SetPlayerPrefs(
                "Curvy_SplineLayer",
                SplineLayer
            );
            DTUtility.SetPlayerPrefs(
                "Curvy_SaveGeneratorOutputs",
                SaveGeneratorOutputs
            );
            PlayerPrefs.Save();
        }

        public override void MergeDoubleLoaded(IDTSingleton newInstance)
        {
            base.MergeDoubleLoaded(newInstance);

            CurvyGlobalManager other = newInstance as CurvyGlobalManager;
            // Merge connection from a doubled CurvyGlobalManager before it get destroyed by DTSingleton
            CurvyConnection[] otherConnections = other.Connections;
            for (int i = 0; i < otherConnections.Length; i++)
                otherConnections[i].transform.SetParent(transform);
        }

#endif

        #endregion
    }
}