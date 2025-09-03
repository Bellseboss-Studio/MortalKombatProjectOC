// =====================================================================
// Copyright © 2013 ToolBuddy
// All rights reserved
// 
// http://www.toolbuddy.net
// =====================================================================

using System.Collections.Generic;
using FluffyUnderware.Curvy;
using FluffyUnderware.DevTools;
using FluffyUnderware.DevToolsEditor;
using JetBrains.Annotations;
using UnityEditor;
using UnityEngine;

namespace FluffyUnderware.CurvyEditor
{
    public partial class CurvyProject : DTProject
    {
        public const string NAME = "Curvy";
        public const string RELPATH_SHAPEWIZARDSCRIPTS = "/Shapes";
        public const string RELPATH_CGMODULEWIZARDSCRIPTS = "/Generator Modules";
        public const string RELPATH_CGMODULEWIZARDEDITORSCRIPTS = "/Generator Modules/Editor";
        public const string RELPATH_CGTEMPLATES = "/Generator Templates";

        public static CurvyProject Instance => (CurvyProject)DT.Project(NAME);

        #region ### Persistent Settings ###

        // DESIGN those settings are a mess: some are public fields, other properties of which the setter updates the editor preferences, and finally others ae part of CurvyGlobalManager (see CurvyProject.LoadPreferences to have a listing of them all). Shouldn't all of those settings be treated similarly?

        // Settings from Preferences window not stored in CurvyGlobalManager

        public bool SnapValuePrecision = true;

        /// <summary>
        /// If enabled and the spline has Restrict To 2D enabled, dots are shown instead of the default move handles
        /// </summary>
        public bool UseTiny2DHandles;

        /// <summary>
        /// Should the spline's text gizmos fade if the spline is too small on the screen
        /// </summary>
        public bool AutoFadeLabels = true;

        /// <summary>
        /// If true, the toolbar is always visible in the SceneView. Otherwise, it is only shown when Curvy Splines related GameObjects are selected
        /// </summary>
        public bool ShowGlobalToolbar = true;

        public bool ShowHints = true;
        public bool AnnotateHierarchy = true;

        public bool EnableMetrics = true;
        public bool EnableAnnouncements = true;

        // Settings made in the toolbar or somewhere else

        private bool mCGAutoModuleDetails;

        /// <summary>
        /// Auto-Expand selected Curvy Generator modules
        /// </summary>
        public bool CGAutoModuleDetails
        {
            get => mCGAutoModuleDetails;
            set
            {
                if (mCGAutoModuleDetails != value)
                {
                    mCGAutoModuleDetails = value;
                    SetEditorPrefs(
                        "CGAutoModuleDetails",
                        mCGAutoModuleDetails
                    );
                }
            }
        }

        private bool mCGSynchronizeSelection = true;

        public bool CGSynchronizeSelection
        {
            get => mCGSynchronizeSelection;
            set
            {
                if (mCGSynchronizeSelection != value)
                {
                    mCGSynchronizeSelection = value;
                    SetEditorPrefs(
                        "CGSynchronizeSelection",
                        mCGSynchronizeSelection
                    );
                }
            }
        }

        private bool mCGAutoFocusOnSelection;

        public bool CGAutoFocusOnSelection
        {
            get => mCGAutoFocusOnSelection;
            set
            {
                if (mCGAutoFocusOnSelection != value)
                {
                    mCGAutoFocusOnSelection = value;
                    SetEditorPrefs(
                        "CGAutoFocusOnSelection",
                        mCGAutoFocusOnSelection
                    );
                }
            }
        }

        private bool mCGShowHelp = true;

        public bool CGShowHelp
        {
            get => mCGShowHelp;
            set
            {
                if (mCGShowHelp != value)
                {
                    mCGShowHelp = value;
                    SetEditorPrefs(
                        "CGShowHelp",
                        mCGShowHelp
                    );
                }
            }
        }

        private int mCGGraphSnapping = 20;

        /// <summary>
        /// The size of the grid used for snapping when dragging a module in Curvy Generator Graph
        /// </summary>
        public int CGGraphSnapping
        {
            get => mCGGraphSnapping;
            set
            {
                int v = Mathf.Max(
                    1,
                    value
                );
                if (mCGGraphSnapping != v)
                {
                    mCGGraphSnapping = v;
                    SetEditorPrefs(
                        "CGGraphSnapping",
                        mCGGraphSnapping
                    );
                }
            }
        }

        private string mCustomizationRootPath = "Packages/Curvy Customization";

        public string CustomizationRootPath
        {
            get => mCustomizationRootPath;
            set
            {
                if (mCustomizationRootPath != value)
                {
                    mCustomizationRootPath = value;
                    SetEditorPrefs(
                        "CustomizationRootPath",
                        mCustomizationRootPath
                    );
                }
            }
        }

        private CurvyBezierModeEnum mBezierMode = CurvyBezierModeEnum.Direction | CurvyBezierModeEnum.Length;

        public CurvyBezierModeEnum BezierMode
        {
            get => mBezierMode;
            set
            {
                if (mBezierMode != value)
                {
                    mBezierMode = value;
                    SetEditorPrefs(
                        "BezierMode",
                        mBezierMode
                    );
                }
            }
        }

        private CurvyAdvBezierModeEnum mAdvBezierMode = CurvyAdvBezierModeEnum.Direction | CurvyAdvBezierModeEnum.Length;

        public CurvyAdvBezierModeEnum AdvBezierMode
        {
            get => mAdvBezierMode;
            set
            {
                if (mAdvBezierMode != value)
                {
                    mAdvBezierMode = value;
                    SetEditorPrefs(
                        "AdvBezierMode",
                        mAdvBezierMode
                    );
                }
            }
        }

        private bool mShowAboutOnLoad = true;

        public bool ShowAboutOnLoad
        {
            get => mShowAboutOnLoad;
            set
            {
                if (mShowAboutOnLoad != value)
                    mShowAboutOnLoad = value;
                SetEditorPrefs(
                    "ShowAboutOnLoad",
                    mShowAboutOnLoad
                );
            }
        }

        #endregion


        private static Vector2 scroll;
        private static readonly bool[] foldouts = new bool[] { true, true, true, true, true };

        /// <summary>
        /// InstanceIDs of the GameObjects containing CurvyConnections
        /// </summary>
        private readonly HashSet<int> connectionGameObjectIDs = new HashSet<int>();

        private CurvySplineSegment curvySplineSegment;


        public CurvyProject()
            : base(
                NAME,
                AssetInformation.Version
            )
        {
            Resource = CurvyResource.Instance;
            Undo.undoRedoPerformed += OnUndoRedo;
            EditorApplication.update += checkLaunch;
            EditorApplication.hierarchyChanged += ScanConnections;
            EditorApplication.hierarchyWindowItemOnGUI += OnHierarchyWindowItemOnGUI;
            ScanConnections();
        }

        protected override void Dispose(
            bool disposing)
        {
            if (disposing)
            {
                Undo.undoRedoPerformed -= OnUndoRedo;
                EditorApplication.update -= checkLaunch;
                EditorApplication.hierarchyChanged -= ScanConnections;
                EditorApplication.hierarchyWindowItemOnGUI -= OnHierarchyWindowItemOnGUI;
            }

            base.Dispose(disposing);
        }

        /// <summary>
        /// Rebuilds the list of GameObject that needs to show a connection icon in the hierarchy window
        /// </summary>
        /// <remarks>Usually there is no need to call this manually</remarks>
        public void ScanConnections()
        {
            int oldCount = connectionGameObjectIDs.Count;

            UpdateConnectionGameObjectIDs(connectionGameObjectIDs);

            if (oldCount != connectionGameObjectIDs.Count)
                EditorApplication.RepaintHierarchyWindow();
        }

        private static void UpdateConnectionGameObjectIDs(
            HashSet<int> instanceIDs)
        {
            instanceIDs.Clear();

            if (CurvyGlobalManager.HasInstance == false)
                return;

            CurvyGlobalManager curvyGlobalManager = CurvyGlobalManager.Instance;

            if (curvyGlobalManager == null)
                return;

            CurvyConnection[] connections = curvyGlobalManager.GetComponentsInChildren<CurvyConnection>();
            foreach (CurvyConnection connection in connections)
            {
                foreach (CurvySplineSegment cp in connection.ControlPointsList)
                {
                    if (cp == null)
                        continue;
                    if (cp.gameObject == null)
                        continue;

                    // see comment in CurvyConnection.DoUpdate to know more about when cp.gameObject can be null
                    instanceIDs.Add(cp.gameObject.GetInstanceID());
                }
            }
        }

        #region Hierarchy annotations

        private void OnHierarchyWindowItemOnGUI(
            int instanceid,
            Rect selectionrect)
        {
            if (AnnotateHierarchy == false)
                return;

            TryDrawAnchorIcon(
                instanceid,
                selectionrect
            );

            TryDrawConnectionIcon(
                instanceid,
                selectionrect
            );
        }

        private void TryDrawAnchorIcon(
            int instanceid,
            Rect selectionrect)
        {
            GameObject gameObject = EditorUtility.InstanceIDToObject(instanceid) as GameObject;
            if (gameObject == null)
                return;

            curvySplineSegment = gameObject.GetComponent<CurvySplineSegment>();
            if (curvySplineSegment == null)
                return;

            CurvySpline curvySpline = curvySplineSegment.Spline;
            if (curvySpline == null)
                return;

            if (curvySpline.IsControlPointAnOrientationAnchor(curvySplineSegment))
                GUI.DrawTexture(
                    new Rect(
                        selectionrect.xMax - 28,
                        selectionrect.yMin + 2,
                        12,
                        12
                    ),
                    CurvyStyles.HierarchyAnchorTexture
                );
        }

        private void TryDrawConnectionIcon(
            int instanceid,
            Rect selectionrect)
        {
            if (!connectionGameObjectIDs.Contains(instanceid))
                return;

            GUI.DrawTexture(
                new Rect(
                    selectionrect.xMax - 14,
                    selectionrect.yMin + 2,
                    12,
                    12
                ),
                CurvyStyles.HierarchyConnectionTexture
            );
        }

        #endregion

        private void checkLaunch()
        {
            EditorApplication.update -= checkLaunch;
            if (ShowAboutOnLoad)
                AboutWindow.Open();
        }

        private void OnUndoRedo()
        {
            List<CurvySpline> splines = DTSelection.GetAllAs<CurvySpline>();
            List<CurvySplineSegment> cps = DTSelection.GetAllAs<CurvySplineSegment>();
            foreach (CurvySplineSegment cp in cps)
            {
                CurvySpline curvySpline = cp.transform.parent
                    ? cp.transform.parent.GetComponent<CurvySpline>()
                    : cp.Spline;
                if (curvySpline && !splines.Contains(curvySpline))
                    splines.Add(curvySpline);
            }

            foreach (CurvySpline spl in splines)
            {
                spl.SyncSplineFromHierarchy();
                spl.ApplyControlPointsNames();
                spl.SetDirtyAll(
                    SplineDirtyingType.Everything,
                    true
                );
            }
        }


        public override void ResetPreferences()
        {
            //reset only settings that are settable through the Preferences window

            base.ResetPreferences();
            CurvyGlobalManager.DefaultInterpolation = CurvyInterpolation.Bezier;
            CurvyGlobalManager.DefaultGizmoColor = CurvyGlobalManager.DefaultDefaultGizmoColor;
            CurvyGlobalManager.DefaultGizmoSelectionColor = CurvyGlobalManager.DefaultDefaultGizmoSelectionColor;
            CurvyGlobalManager.GizmoControlPointSize = 0.15f;
            CurvyGlobalManager.GizmoOrientationLength = 1f;
            CurvyGlobalManager.GizmoOrientationColor = CurvyGlobalManager.DefaultGizmoOrientationColor;
            CurvyGlobalManager.SceneViewResolution = 0.5f;
            CurvyGlobalManager.HideManager = false;
            CurvyGlobalManager.SplineLayer = 0;
            CurvyGlobalManager.SaveGeneratorOutputs = true;

            CustomizationRootPath = "Packages/Curvy Customization";
            SnapValuePrecision = true;
            UseTiny2DHandles = false;
            AutoFadeLabels = true;
            ShowGlobalToolbar = true;
            EnableMetrics = true;
            EnableAnnouncements = true;
            ShowHints = true;
            AnnotateHierarchy = true;

            ToolbarMode = DTToolbarMode.Full;
            ToolbarOrientation = DTToolbarOrientation.Left;
            ToolbarMargins = new DTToolbarMargins();
            ShowMargins = false;

            DT._UseSnapValuePrecision = SnapValuePrecision;
            DTToolbarItem._StatusBar.Visible = ShowHints;
        }

        public override void LoadPreferences()
        {
            if (GetEditorPrefs(
                    "Version",
                    "PreDT"
                )
                == "PreDT")
            {
                DeletePreDTSettings();
                SavePreferences();
            }

            base.LoadPreferences();
            CurvyGlobalManager.DefaultInterpolation = GetEditorPrefs(
                "DefaultInterpolation",
                CurvyGlobalManager.DefaultInterpolation
            );
            CurvyGlobalManager.DefaultGizmoColor = GetEditorPrefs(
                "GizmoColor",
                CurvyGlobalManager.DefaultGizmoColor
            );
            CurvyGlobalManager.DefaultGizmoSelectionColor = GetEditorPrefs(
                "GizmoSelectionColor",
                CurvyGlobalManager.DefaultGizmoSelectionColor
            );
            CurvyGlobalManager.GizmoControlPointSize = GetEditorPrefs(
                "GizmoControlPointSize",
                CurvyGlobalManager.GizmoControlPointSize
            );
            CurvyGlobalManager.GizmoOrientationLength = GetEditorPrefs(
                "GizmoOrientationLength",
                CurvyGlobalManager.GizmoOrientationLength
            );
            CurvyGlobalManager.GizmoOrientationColor = GetEditorPrefs(
                "GizmoOrientationColor",
                CurvyGlobalManager.GizmoOrientationColor
            );
            CurvyGlobalManager.Gizmos = GetEditorPrefs(
                "Gizmos",
                CurvyGlobalManager.Gizmos
            );
            CurvyGlobalManager.SceneViewResolution = Mathf.Clamp01(
                GetEditorPrefs(
                    "SceneViewResolution",
                    CurvyGlobalManager.SceneViewResolution
                )
            );
            CurvyGlobalManager.HideManager = GetEditorPrefs(
                "HideManager",
                CurvyGlobalManager.HideManager
            );
            CurvyGlobalManager.SplineLayer = GetEditorPrefs(
                "SplineLayer",
                CurvyGlobalManager.SplineLayer
            );
            CurvyGlobalManager.SaveGeneratorOutputs = GetEditorPrefs(
                "SaveGeneratorOutputs",
                CurvyGlobalManager.SaveGeneratorOutputs
            );

            mCustomizationRootPath = GetEditorPrefs(
                "CustomizationRootPath",
                mCustomizationRootPath
            );
            SnapValuePrecision = GetEditorPrefs(
                "SnapValuePrecision",
                true
            );
            UseTiny2DHandles = GetEditorPrefs(
                "UseTiny2DHandles",
                UseTiny2DHandles
            );
            AutoFadeLabels = GetEditorPrefs(
                "AutoFadeLabels",
                AutoFadeLabels
            );
            ShowGlobalToolbar = GetEditorPrefs(
                "ShowGlobalToolbar",
                ShowGlobalToolbar
            );
            EnableMetrics = GetEditorPrefs(
                "EnableMetrics",
                true
            );
            EnableAnnouncements = GetEditorPrefs(
                "EnableAnnouncements",
                true
            );
            ShowHints = GetEditorPrefs(
                "ShowHints",
                ShowHints
            );
            AnnotateHierarchy = GetEditorPrefs(
                "AnnotateHierarchy",
                AnnotateHierarchy
            );

            CurvyGlobalManager.SaveRuntimeSettings();

            mCGAutoModuleDetails = GetEditorPrefs(
                "CGAutoModuleDetails",
                mCGAutoModuleDetails
            );
            mCGSynchronizeSelection = GetEditorPrefs(
                "CGSynchronizeSelection",
                mCGSynchronizeSelection
            );
            mCGAutoFocusOnSelection = GetEditorPrefs(
                "CGAutoFocusOnSelection",
                mCGAutoFocusOnSelection
            );

            mCGShowHelp = GetEditorPrefs(
                "CGShowHelp",
                mCGShowHelp
            );
            mCGGraphSnapping = GetEditorPrefs(
                "CGGraphSnapping",
                mCGGraphSnapping
            );
            mBezierMode = GetEditorPrefs(
                "BezierMode",
                mBezierMode
            );
            mAdvBezierMode = GetEditorPrefs(
                "AdvBezierMode",
                mAdvBezierMode
            );
            mShowAboutOnLoad = GetEditorPrefs(
                "ShowAboutOnLoad",
                mShowAboutOnLoad
            );

            DT._UseSnapValuePrecision = SnapValuePrecision;
            DTToolbarItem._StatusBar.Visible = ShowHints;
        }

        public override void SavePreferences()
        {
            base.SavePreferences();
            SetEditorPrefs(
                "DefaultInterpolation",
                CurvyGlobalManager.DefaultInterpolation
            );
            SetEditorPrefs(
                "GizmoColor",
                CurvyGlobalManager.DefaultGizmoColor
            );
            SetEditorPrefs(
                "GizmoSelectionColor",
                CurvyGlobalManager.DefaultGizmoSelectionColor
            );
            SetEditorPrefs(
                "GizmoControlPointSize",
                CurvyGlobalManager.GizmoControlPointSize
            );
            SetEditorPrefs(
                "GizmoOrientationLength",
                CurvyGlobalManager.GizmoOrientationLength
            );
            SetEditorPrefs(
                "GizmoOrientationColor",
                CurvyGlobalManager.GizmoOrientationColor
            );
            SetEditorPrefs(
                "Gizmos",
                CurvyGlobalManager.Gizmos
            );
            SetEditorPrefs(
                "SnapValuePrecision",
                SnapValuePrecision
            );
            SetEditorPrefs(
                "EnableAnnouncements",
                EnableAnnouncements
            );
            SetEditorPrefs(
                "EnableMetrics",
                EnableMetrics
            );
            SetEditorPrefs(
                "SceneViewResolution",
                CurvyGlobalManager.SceneViewResolution
            );
            SetEditorPrefs(
                "HideManager",
                CurvyGlobalManager.HideManager
            );
            SetEditorPrefs(
                "UseTiny2DHandles",
                UseTiny2DHandles
            );
            SetEditorPrefs(
                "AutoFadeLabels",
                AutoFadeLabels
            );
            SetEditorPrefs(
                "ShowGlobalToolbar",
                ShowGlobalToolbar
            );
            SetEditorPrefs(
                "AnnotateHierarchy",
                AnnotateHierarchy
            );
            SetEditorPrefs(
                "ShowHints",
                ShowHints
            );
            SetEditorPrefs(
                "SplineLayer",
                CurvyGlobalManager.SplineLayer
            );
            SetEditorPrefs(
                "SaveGeneratorOutputs",
                CurvyGlobalManager.SaveGeneratorOutputs
            );
            SetEditorPrefs(
                "CustomizationRootPath",
                mCustomizationRootPath
            );

            CurvyGlobalManager.SaveRuntimeSettings();
            DT._UseSnapValuePrecision = SnapValuePrecision;
            DTToolbarItem._StatusBar.Visible = ShowHints;
        }

        protected override void UpgradePreferences(
            string oldVersion)
        {
            base.UpgradePreferences(oldVersion);
            // Ensure that About Window will be shown after upgrade
            DeleteEditorPrefs("ShowAboutOnLoad");
            if (oldVersion == "2.0.0")
                if (GetEditorPrefs(
                        "GizmoOrientationLength",
                        CurvyGlobalManager.GizmoOrientationLength
                    )
                    == 4)
                    DeleteEditorPrefs("GizmoOrientationLength");
        }

        private void DeletePreDTSettings()
        {
            DTLog.Log("[Curvy] Removing old preferences");
            EditorPrefs.DeleteKey("Curvy_GizmoColor");
            EditorPrefs.DeleteKey("Curvy_GizmoSelectionColor");
            EditorPrefs.DeleteKey("Curvy_ControlPointSize");
            EditorPrefs.DeleteKey("Curvy_OrientationLength");
            EditorPrefs.DeleteKey("Curvy_Gizmos");
            EditorPrefs.DeleteKey("Curvy_ToolbarLabels");
            EditorPrefs.DeleteKey("Curvy_ToolbarOrientation");
            EditorPrefs.DeleteKey("Curvy_ShowShapeWizardUndoWarning");
            EditorPrefs.DeleteKey("Curvy_KeyBindings");
        }


        #region Settings window

        /// <summary>
        /// The name of the settings entry of Curvy
        /// </summary>
        private const string SettingsEntryName = "Curvy";

        [SettingsProvider]
        [UsedImplicitly]
        private static SettingsProvider MyNewPrefCode()
            => new CurvySettingsProvider(SettingsScope.User);

        public static void PreferencesGUI()
        {
            scroll = EditorGUILayout.BeginScrollView(scroll);
            DisplayGeneralPreferences();

            DisplayGizmoPreferences();

            DisplayUIPreferences();

            DisplayShortcutsPreferences();

            if (GUILayout.Button("Reset to defaults"))
            {
                Instance.ResetPreferences();

                List<EditorKeyBinding> keys = Instance.GetProjectBindings();
                foreach (EditorKeyBinding binding in keys)
                    Instance.DeleteEditorPrefs(binding.Name);
            }

            EditorGUILayout.EndScrollView();

            if (GUI.changed)
            {
                EditorApplication.RepaintHierarchyWindow();
                Instance.SavePreferences();
                DT.ReInitialize(false);
                SceneView.RepaintAll();
            }
        }

        private static void DisplayGeneralPreferences()
        {
            CurvyGlobalManager.DefaultInterpolation = (CurvyInterpolation)EditorGUILayout.EnumPopup(
                "Default Spline Type",
                CurvyGlobalManager.DefaultInterpolation
            );
            CurvyGlobalManager.SplineLayer = EditorGUILayout.LayerField(
                new GUIContent(
                    "Default Spline Layer",
                    "Layer to use for splines and Control Points"
                ),
                CurvyGlobalManager.SplineLayer
            );

            CurvyGlobalManager.SaveGeneratorOutputs = EditorGUILayout.Toggle(
                new GUIContent(
                    "Save Generator Outputs",
                    "Whether the output of Curvy Generators should be saved in the scene file.\nDisable this option to reduce the size of scene files. This might increase the saving time for complex scenes.\nThis option applies only on generators that are enabled and have Auto Refresh set to true"
                ),
                CurvyGlobalManager.SaveGeneratorOutputs
            );

            Instance.SnapValuePrecision = EditorGUILayout.Toggle(
                new GUIContent(
                    "Snap Value Precision",
                    "Round inspector values"
                ),
                Instance.SnapValuePrecision
            );

            CurvyGlobalManager.HideManager = EditorGUILayout.Toggle(
                new GUIContent(
                    "Hide _CurvyGlobal_",
                    "Hide the global manager in Hierarchy?"
                ),
                CurvyGlobalManager.HideManager
            );

            Instance.EnableAnnouncements = EditorGUILayout.Toggle(
                new GUIContent(
                    "Enable Announcements",
                    "Display announcements from Curvy's developers"
                ),
                Instance.EnableAnnouncements
            );

            Instance.EnableMetrics = EditorGUILayout.Toggle(
                new GUIContent(
                    "Enable Metrics",
                    "Send metrics to Curvy's developers. This includes data such as the Unity version, Curvy version, etc... Keeping this enabled helps us a lot maintaining Curvy"
                ),
                Instance.EnableMetrics
            );

            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.TextField(
                    new GUIContent(
                        "Customization Root Path",
                        "Base Path for custom Curvy extensions"
                    ),
                    Instance.CustomizationRootPath
                );
                if (GUILayout.Button(
                        new GUIContent(
                            "<",
                            "Select"
                        ),
                        GUILayout.ExpandWidth(false)
                    ))
                {
                    string path = EditorUtility.OpenFolderPanel(
                        "Customization Root Path",
                        Application.dataPath,
                        ""
                    );
                    if (!string.IsNullOrEmpty(path))
                        Instance.CustomizationRootPath = path.Replace(
                            Application.dataPath + "/",
                            ""
                        );
                }

                EditorGUILayout.EndHorizontal();
            }
            CurvyGlobalManager.SceneViewResolution = EditorGUILayout.Slider(
                new GUIContent(
                    "SceneView Resolution",
                    "Lower values results in faster SceneView drawing"
                ),
                CurvyGlobalManager.SceneViewResolution,
                0,
                1
            );
        }

        private static void DisplayGizmoPreferences()
        {
            foldouts[0] = EditorGUILayout.Foldout(
                foldouts[0],
                "Gizmo",
                CurvyStyles.Foldout
            );
            if (foldouts[0])
            {
                EditorGUI.indentLevel++;

                CurvyGlobalManager.DefaultGizmoColor = EditorGUILayout.ColorField(
                    "Spline color",
                    CurvyGlobalManager.DefaultGizmoColor
                );
                CurvyGlobalManager.DefaultGizmoSelectionColor = EditorGUILayout.ColorField(
                    "Spline Selection color",
                    CurvyGlobalManager.DefaultGizmoSelectionColor
                );
                CurvyGlobalManager.GizmoControlPointSize = EditorGUILayout.FloatField(
                    "Control Point Size",
                    CurvyGlobalManager.GizmoControlPointSize
                );
                CurvyGlobalManager.GizmoOrientationLength = EditorGUILayout.FloatField(
                    new GUIContent(
                        "Orientation Length",
                        "Orientation gizmo size"
                    ),
                    CurvyGlobalManager.GizmoOrientationLength
                );
                CurvyGlobalManager.GizmoOrientationColor = EditorGUILayout.ColorField(
                    new GUIContent(
                        "Orientation Color",
                        "Orientation gizmo color"
                    ),
                    CurvyGlobalManager.GizmoOrientationColor
                );
                Instance.UseTiny2DHandles = EditorGUILayout.Toggle(
                    new GUIContent(
                        "Use Tiny 2D Handles",
                        "If enabled and the spline has Restrict To 2D enabled, dots are shown instead of the default move handles"
                    ),
                    Instance.UseTiny2DHandles
                );
                Instance.AutoFadeLabels = EditorGUILayout.Toggle(
                    new GUIContent(
                        "Auto Fade Labels",
                        "Should the spline's text gizmos fade if the spline is too small on the screen"
                    ),
                    Instance.AutoFadeLabels
                );

                EditorGUI.indentLevel--;
            }
        }

        private static void DisplayUIPreferences()
        {
            foldouts[1] = EditorGUILayout.Foldout(
                foldouts[1],
                "UI",
                CurvyStyles.Foldout
            );
            if (foldouts[1])
            {
                EditorGUI.indentLevel++;

                Instance.ShowHints = EditorGUILayout.Toggle(
                    new GUIContent(
                        "Show Hints",
                        "Show hints, at the bottom of scene view, about the usage of some Curvy editor tools"
                    ),
                    Instance.ShowHints
                );

                Instance.AnnotateHierarchy = EditorGUILayout.Toggle(
                    new GUIContent(
                        "Annotate Hierarchy",
                        "Annotate the Hierarchy view to emphasize Orientation Anchors and connected Control Points"
                    ),
                    Instance.AnnotateHierarchy
                );

                DisplayToolbarCustomizationPreferences();

                EditorGUI.indentLevel--;
            }
        }

        private static void DisplayToolbarCustomizationPreferences()
        {
            foldouts[3] = EditorGUILayout.Foldout(
                foldouts[3],
                "Toolbar Customization",
                CurvyStyles.Foldout
            );
            if (foldouts[3])
            {
                EditorGUI.indentLevel++;

                Instance.ShowGlobalToolbar = EditorGUILayout.Toggle(
                    new GUIContent(
                        "Always Visible",
                        "If enabled, the toolbar is always visible in the SceneView. Otherwise, it is only shown when Curvy Splines related GameObjects are selected"
                    ),
                    Instance.ShowGlobalToolbar
                );

                Instance.ToolbarOrientation = (DTToolbarOrientation)EditorGUILayout.EnumPopup(
                    new GUIContent(
                        "Alignment",
                        "Defines the toolbar's alignment with window edges"
                    ),
                    Instance.ToolbarOrientation
                );

                Instance.ToolbarMode = (DTToolbarMode)EditorGUILayout.EnumPopup(
                    new GUIContent(
                        "Mode",
                        "How the toolbar's buttons should be displayed"
                    ),
                    Instance.ToolbarMode
                );

                DisplayMarginsPreferences();

                Instance.ToolbarMargins.ButtonSpacing = EditorGUILayout.IntField(
                    new GUIContent(
                        "Button Spacing",
                        "Spacing between buttons"
                    ),
                    Instance.ToolbarMargins.ButtonSpacing
                );
                Instance.ToolbarMargins.GroupSpacing = EditorGUILayout.IntField(
                    new GUIContent(
                        "Group Spacing",
                        "Spacing between groups of buttons"
                    ),
                    Instance.ToolbarMargins.GroupSpacing
                );
                Instance.ToolbarMargins.WrapSpacing = EditorGUILayout.IntField(
                    new GUIContent(
                        "Wrap Spacing",
                        "Column or row wrap spacing"
                    ),
                    Instance.ToolbarMargins.WrapSpacing
                );

                Instance.ToolbarMargins.StartSpacing = EditorGUILayout.IntField(
                    new GUIContent(
                        "Start Spacing",
                        "Number of empty spots at the start of the toolbar"
                    ),
                    Instance.ToolbarMargins.StartSpacing
                );

                EditorGUI.indentLevel--;
            }
        }

        private static void DisplayMarginsPreferences()
        {
            foldouts[4] = EditorGUILayout.Foldout(
                foldouts[4],
                "Margins",
                CurvyStyles.Foldout
            );
            if (foldouts[4])
            {
                EditorGUI.indentLevel++;

                Instance.ToolbarMargins.LeftMargin = EditorGUILayout.IntField(
                    new GUIContent(
                        "Left",
                        "Margin between the the toolbar and the left edge of the window"
                    ),
                    Instance.ToolbarMargins.LeftMargin
                );
                Instance.ToolbarMargins.RightMargin = EditorGUILayout.IntField(
                    new GUIContent(
                        "Right",
                        "Margin between the the toolbar and the right edge of the window"
                    ),
                    Instance.ToolbarMargins.RightMargin
                );
                Instance.ToolbarMargins.TopMargin = EditorGUILayout.IntField(
                    new GUIContent(
                        "Top",
                        "Margin between the the toolbar and the top edge of the window"
                    ),
                    Instance.ToolbarMargins.TopMargin
                );
                Instance.ToolbarMargins.BottomMargin = EditorGUILayout.IntField(
                    new GUIContent(
                        "Bottom",
                        "Margin between the the toolbar and the bottom edge of the window"
                    ),
                    Instance.ToolbarMargins.BottomMargin
                );

                Instance.ShowMargins
                    = EditorGUILayout.Toggle(
                        new GUIContent(
                            "Show Margins",
                            "Shows a UI to help you calibrate the margins"
                        ),
                        Instance.ShowMargins
                    );

                EditorGUI.indentLevel--;
            }
        }

        private static void DisplayShortcutsPreferences()
        {
            foldouts[2] = EditorGUILayout.Foldout(
                foldouts[2],
                "Shortcuts",
                CurvyStyles.Foldout
            );

            if (foldouts[2])
            {
                EditorGUI.indentLevel++;

                List<EditorKeyBinding> keys = Instance.GetProjectBindings();
                foreach (EditorKeyBinding binding in keys)
                {
                    if (binding.OnPreferencesGUI()) // save changed bindings
                        Instance.SetEditorPrefs(
                            binding.Name,
                            binding.ToPrefsString()
                        );
                    GUILayout.Space(2);
                    GUILayout.Box(
                        "",
                        GUILayout.Height(1),
                        GUILayout.ExpandWidth(true)
                    );
                    GUILayout.Space(2);
                }

                EditorGUI.indentLevel--;
            }
        }

        #endregion
    }
}