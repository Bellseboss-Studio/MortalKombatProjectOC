// =====================================================================
// Copyright © 2013 ToolBuddy
// All rights reserved
// 
// http://www.toolbuddy.net
// =====================================================================

using FluffyUnderware.DevTools.Extensions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace FluffyUnderware.CurvyEditor.Generator
{
    public partial class CGGraph
    {
        #region Cached GUIContents

        private static GUIContent _helpIcon;

        private static GUIContent HelpIcon
        {
            get
            {
                if (_helpIcon == null)
                    _helpIcon = new GUIContent(
                        CurvyStyles.HelpTexture,
                        "Show Help"
                    );
                return _helpIcon;
            }
        }

        private static GUIContent _clearModulesIcon;

        private static GUIContent ClearModulesIcon
        {
            get
            {
                if (_clearModulesIcon == null)
                    _clearModulesIcon = new GUIContent(
                        CurvyStyles.DeleteTexture,
                        "Clear modules"
                    );
                return _clearModulesIcon;
            }
        }

        private static GUIContent _clearOutputIcon;

        private static GUIContent ClearOutputIcon
        {
            get
            {
                if (_clearOutputIcon == null)
                    _clearOutputIcon = new GUIContent(
                        CurvyStyles.DeleteBTexture,
                        "Clear output"
                    );
                return _clearOutputIcon;
            }
        }

        private static GUIContent _saveOutputIcon;

        private static GUIContent SaveOutputIcon
        {
            get
            {
                if (_saveOutputIcon == null)
                    _saveOutputIcon = new GUIContent(
                        CurvyStyles.SaveResourcesTexture,
                        "Save output to scene"
                    );
                return _saveOutputIcon;
            }
        }

        private static GUIContent _refreshIcon;

        private static GUIContent RefreshIcon
        {
            get
            {
                if (_refreshIcon == null)
                    _refreshIcon = new GUIContent(
                        CurvyStyles.RefreshTexture,
                        "Refresh"
                    );
                return _refreshIcon;
            }
        }

        private static GUIContent _reorderModulesIcon;

        private static GUIContent ReorderModulesIcon
        {
            get
            {
                if (_reorderModulesIcon == null)
                    _reorderModulesIcon = new GUIContent(
                        CurvyStyles.ReorderTexture,
                        "Reorder modules"
                    );
                return _reorderModulesIcon;
            }
        }

        private static GUIContent _synchronizeSelectionIcon;

        private static GUIContent SynchronizeSelectionIcon
        {
            get
            {
                if (_synchronizeSelectionIcon == null)
                    _synchronizeSelectionIcon = new GUIContent(
                        CurvyStyles.SynchronizeTexture,
                        "Synchronize Generator and Hierarchy selection"
                    );
                return _synchronizeSelectionIcon;
            }
        }

        private static GUIContent _saveTemplateIcon;

        private static GUIContent SaveTemplateIcon
        {
            get
            {
                if (_saveTemplateIcon == null)
                    _saveTemplateIcon = new GUIContent(
                        CurvyStyles.AddTemplateTexture,
                        "Save selection as Template"
                    );
                return _saveTemplateIcon;
            }
        }

        private static GUIContent _snapGridSizeIcon;

        private static GUIContent SnapGridSizeIcon
        {
            get
            {
                if (_snapGridSizeIcon == null)
                    _snapGridSizeIcon = new GUIContent(
                        CurvyStyles.TexGridSnap,
                        "Snap Grid Size\n(Hold Alt while dragging to snap)"
                    );
                return _snapGridSizeIcon;
            }
        }

        private static GUIContent _expandAllIcon;

        private static GUIContent ExpandAllIcon
        {
            get
            {
                if (_expandAllIcon == null)
                    _expandAllIcon = new GUIContent(
                        CurvyStyles.ExpandTexture,
                        "Expand all"
                    );
                return _expandAllIcon;
            }
        }

        private static GUIContent _expandAllInconDisabled;

        private static GUIContent ExpandAllInconDisabled
        {
            get
            {
                if (_expandAllInconDisabled == null)
                    _expandAllInconDisabled = new GUIContent(
                        CurvyStyles.ExpandTexture,
                        "Expand all. Is disabled in Overview mode"
                    );
                return _expandAllInconDisabled;
            }
        }

        private static GUIContent _collapseAllIcon;

        private static GUIContent CollapseAllIcon
        {
            get
            {
                if (_collapseAllIcon == null)
                    _collapseAllIcon = new GUIContent(
                        CurvyStyles.CollapseTexture,
                        "Collapse all"
                    );
                return _collapseAllIcon;
            }
        }

        private static GUIContent _collapseAllIconDisabled;

        private static GUIContent CollapseAllIconDisabled
        {
            get
            {
                if (_collapseAllIconDisabled == null)
                    _collapseAllIconDisabled = new GUIContent(
                        CurvyStyles.CollapseTexture,
                        "Collapse all. Is disabled in Overview mode"
                    );
                return _collapseAllIconDisabled;
            }
        }

        private static GUIContent _autoExpandIcon;

        private static GUIContent AutoExpandIcon
        {
            get
            {
                if (_autoExpandIcon == null)
                    _autoExpandIcon = new GUIContent(
                        CurvyStyles.CGAutoFoldTexture,
                        "Auto-Expand selected module"
                    );
                return _autoExpandIcon;
            }
        }

        private static GUIContent _autoExpandIconDisabled;

        private static GUIContent AutoExpandIconDisabled
        {
            get
            {
                if (_autoExpandIconDisabled == null)
                    _autoExpandIconDisabled = new GUIContent(
                        CurvyStyles.CGAutoFoldTexture,
                        "Auto-Expand selected module. Is disabled in Overview mode"
                    );
                return _autoExpandIconDisabled;
            }
        }

        private static GUIContent _debugIcon;

        private static GUIContent DebugIcon
        {
            get
            {
                if (_debugIcon == null)
                    _debugIcon = new GUIContent(
                        CurvyStyles.DebugTexture,
                        "Debug Mode"
                    );
                return _debugIcon;
            }
        }

        private static GUIContent _debugIconDisabled;

        private static GUIContent DebugIconDisabled

        {
            get
            {
                if (_debugIconDisabled == null)
                    _debugIconDisabled = new GUIContent(
                        CurvyStyles.DebugTexture,
                        "Debug Mode. Is disabled in Overview mode"
                    );
                return _debugIconDisabled;
            }
        }

        #endregion

        /// <summary>
        /// The bar at the top of the generator window
        /// </summary>
        private void DrawToolbar(
            out bool reorderModules)
        {
            GUILayout.BeginHorizontal(CurvyStyles.Toolbar);
            {
                DrawClearModules();

                GUILayout.Space(10);

                DrawClearOutput();
                DrawSaveOutputToScene();

                GUILayout.Space(10);

                DrawRefresh();
                reorderModules = DrawReorderModules();
                DrawDebug();

                GUILayout.Space(10);

                DrawExpandAndCollapse();

                GUILayout.Space(10);

                DrawSynchronizeSelection();

                GUILayout.Space(10);

                DrawSaveSelectionAsTemplate();

                GUILayout.FlexibleSpace();

                DrawZoom();
                DrawGridSnapTool();
                DrawHelpTool();
            }
            GUILayout.EndHorizontal();
        }

        private void DrawClearModules()
        {
            if (GUILayout.Button(
                    ClearModulesIcon,
                    EditorStyles.miniButton
                )
                && EditorUtility.DisplayDialog(
                    "Clear",
                    "Clear graph?",
                    "Yes",
                    "No"
                ))
            {
                CanvasSelection.Clear();
                Generator.Clear();
            }
        }

        private void DrawClearOutput()
        {
            if (GUILayout.Button(
                    ClearOutputIcon,
                    EditorStyles.miniButton
                ))
            {
                bool associatedPrefabWasModified;
                if (Generator.DeleteAllOutputManagedResources(out associatedPrefabWasModified)
                    && Application.isPlaying == false)
                    if (PrefabStageUtility.GetPrefabStage(Generator.gameObject)
                        == null) //if not editing the prefab in prefab mode
                        Generator.gameObject.MarkParentSceneAsDirty();

                if (associatedPrefabWasModified)
                    EditorUtility.DisplayDialog(
                        "Prefab asset modified",
                        "The prefab asset associated with the prefab instance containing this Curvy Generator was modified.\n\nThis was done in order to allow the operation you initiated (Clear Output). You might need to apply the operation again.",
                        "Ok"
                    );
            }
        }

        private void DrawSaveOutputToScene()
        {
            if (GUILayout.Button(
                    SaveOutputIcon,
                    EditorStyles.miniButton
                ))
                Generator.SaveAllOutputManagedResources();
        }

        private void DrawRefresh()
        {
            if (GUILayout.Button(
                    RefreshIcon,
                    EditorStyles.miniButton,
                    GUILayout.ExpandWidth(false)
                ))
            {
                Modules = null;
                Generator.Refresh(true);
            }
        }

        private bool DrawReorderModules() =>
            GUILayout.Button(
                ReorderModulesIcon,
                EditorStyles.miniButton,
                GUILayout.ExpandWidth(false)
            );

        private void DrawDebug()
        {
            //see comment in DrawExpandAndCollapse to learn more about why we disable the debug button in overview mode
            bool isEnabled = Viewport.IsInOverviewMode == false;
            GUI.enabled = isEnabled;

            EditorGUI.BeginChangeCheck();
            mShowDebug.target = GUILayout.Toggle(
                mShowDebug.target,
                isEnabled
                    ? DebugIcon
                    : DebugIconDisabled,
                EditorStyles.miniButton
            );
            if (EditorGUI.EndChangeCheck())
            {
                Generator.ShowDebug = mShowDebug.target;
                SceneView.RepaintAll();
            }

            GUI.enabled = true;
        }

        private void DrawExpandAndCollapse()
        {
            //If in overview mode, we disable these buttons, because the module drawing callback ModuleDrawingCallback_LOD1 does not handle the expanded state. A better fix should be to modify ModuleDrawingCallback_LOD1, but disabling the buttons is a quick fix.
            //todo bug The quick fix is not perfect, since one can enter overview mode while the modules are in the process of expanding/collapsing, leading to ModuleDrawingCallback_LOD1 drawing dimensions that are not valid once the animation is done.
            bool isEnabled = Viewport.IsInOverviewMode == false;
            GUI.enabled = isEnabled;

            CurvyProject.Instance.CGAutoModuleDetails = GUILayout.Toggle(
                CurvyProject.Instance.CGAutoModuleDetails,
                isEnabled
                    ? AutoExpandIcon
                    : AutoExpandIconDisabled,
                EditorStyles.miniButton
            );
            if (GUILayout.Button(
                    isEnabled
                        ? ExpandAllIcon
                        : ExpandAllInconDisabled,
                    EditorStyles.miniButton
                ))
                CGEditorUtility.SetModulesExpandedState(
                    true,
                    Generator.Modules.ToArray()
                );
            if (GUILayout.Button(
                    isEnabled
                        ? CollapseAllIcon
                        : CollapseAllIconDisabled,
                    EditorStyles.miniButton
                ))
                CGEditorUtility.SetModulesExpandedState(
                    false,
                    Generator.Modules.ToArray()
                );

            GUI.enabled = true;
        }

        private static void DrawSynchronizeSelection()
        {
            CurvyProject.Instance.CGSynchronizeSelection = GUILayout.Toggle(
                CurvyProject.Instance.CGSynchronizeSelection,
                SynchronizeSelectionIcon,
                EditorStyles.miniButton
            );
        }

        private void DrawSaveSelectionAsTemplate()
        {
            GUI.enabled = CanvasSelection.SelectedModule != null;
            if (GUILayout.Button(
                    SaveTemplateIcon,
                    EditorStyles.miniButton
                ))
                TemplateWizard.Open(
                    CanvasSelection.SelectedModules,
                    ui
                );

            GUI.enabled = true;
        }

        private void DrawZoom()
        {
            // Add a label to indicate the slider
            GUILayout.Label("Zoom");
            // Create a horizontal slider that goes from 1 to 10
            float newZoomValue = GUILayout.HorizontalSlider(
                Viewport.Zoom,
                Viewport.MinZoom,
                Viewport.MaxZoom,
                GUILayout.Width(60)
            );

            Viewport.SetZoom(
                newZoomValue,
                Viewport.ClientRectangle.size / 2
            );

            GUILayout.Label(
                $"x{Viewport.Zoom:0.0}",
                GUILayout.Width(40)
            );
        }

        private static void DrawGridSnapTool()
        {
            GUILayout.Label(
                SnapGridSizeIcon
            );
            const float minSnap = 10;
            const float maxSnap = 40;
            CurvyProject.Instance.CGGraphSnapping = (int)GUILayout.HorizontalSlider(
                Mathf.Clamp(
                    CurvyProject.Instance.CGGraphSnapping,
                    minSnap,
                    maxSnap
                ),
                minSnap,
                maxSnap,
                GUILayout.Width(60)
            );
            GUILayout.Label(
                CurvyProject.Instance.CGGraphSnapping.ToString(),
                GUILayout.Width(20)
            );
        }

        private static void DrawHelpTool()
        {
            CurvyProject.Instance.CGShowHelp = GUILayout.Toggle(
                CurvyProject.Instance.CGShowHelp,
                HelpIcon,
                EditorStyles.miniButton,
                GUILayout.Height(20)
            );
        }
    }
}