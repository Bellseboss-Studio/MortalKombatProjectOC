// =====================================================================
// Copyright © 2013 ToolBuddy
// All rights reserved
// 
// http://www.toolbuddy.net
// =====================================================================

using System;
using System.Collections.Generic;
using FluffyUnderware.Curvy.Generator;
using FluffyUnderware.DevTools;
using FluffyUnderware.DevToolsEditor;
using JetBrains.Annotations;
using UnityEditor;
using UnityEditor.AnimatedValues;
using UnityEngine;

namespace FluffyUnderware.CurvyEditor.Generator
{
    public partial class CGGraph : EditorWindow
    {
        private static readonly Color SelectedModuleColor = new Color(
            0.227451f,
            0.4470588f,
            0.6901961f
        );

        private readonly AnimBool mShowDebug = new AnimBool();

        private CanvasState canvasState;

        [NotNull]
        public readonly CanvasSelection CanvasSelection;

        [NotNull]
        private readonly SelectionRectangle selectionRectangle;

        [CanBeNull]
        private CurvyGenerator generator;

        /// <summary>
        /// Was the last event of type MouseDown done while the mouse was inside the viewport and on an empty space?
        /// </summary>
        private bool mouseDownWasOnEmptyCanvas;

        [NotNull]
        private readonly DTStatusbar statusBar = new DTStatusbar();

        private CanvasUI ui;

        [NotNull]
        private readonly MouseSnapper mouseSnapper = new MouseSnapper();

        [CanBeNull]
        public CurvyGenerator Generator
        {
            get => generator;
            private set
            {
                InitializeShowDebug(
                    value.ShowDebug
                );

                titleContent.text = value.name;

                generator = value;
            }
        }

        [NotNull] public Viewport Viewport { get; private set; }

        /// <summary>
        /// Mouse position in window space
        /// </summary>
        public Vector2 MousePosition { get; private set; }

        private Event EV =>
            Event.current;

        public CGGraph()
        {
            Viewport = new Viewport(this);
            canvasState = new CanvasState();
            CanvasSelection = new CanvasSelection();
            selectionRectangle = new SelectionRectangle();
            moduleDrawingCallbackLod0Delegate = ModuleDrawingCallback_LOD0;
            moduleDrawingCallbackLod1Delegate = ModuleDrawingCallback_LOD1;
            moduleDrawingCallbackCulledDelegate = ModuleDrawingCallback_Culled;
        }

        #region Opening and initialization

        internal static CGGraph Open(
            [NotNull] CurvyGenerator generator)
        {
            generator.Initialize(true);
            CGGraph window = GetWindow<CGGraph>(
                "",
                true,
                typeof(SceneView)
            );
            window.Initialize(generator);
            return window;
        }

        private void Initialize(
            [NotNull] CurvyGenerator curvyGenerator)
        {
            if (curvyGenerator == null)
                throw new ArgumentNullException(nameof(curvyGenerator));

            DestroyModuleEditors();
            Generator = curvyGenerator;
            Generator.ArrangeModules();
            ResetState();
            SetViewportDimensions();
            Show();
            SetStatusBarInitialMessage();
        }

        private void ResetState()
        {
            lastRepaintTime = Now;
            mouseDownWasOnEmptyCanvas = false;
            //todo reset the remaining state
            CanvasSelection.Reset();
            canvasState.Reset();
            mouseSnapper.Reset();

            ResetLinkDragState();
            selectionRectangle.Reset();
            if (Generator != null)
                //Reset needs a generator to be set to get from it the initial scroll value
                Viewport.Reset();
        }

        private void SetStatusBarInitialMessage()
        {
            if (Generator.Modules.Count == 0)
                statusBar.SetInfo(
                    "Welcome to the Curvy Generator! Right click or drag a CurvySpline on the canvas to get started!",
                    "",
                    10
                );
            else
                statusBar.SetMessage(
                    Generator.Modules.Count + " modules loaded!",
                    "",
                    MessageType.None,
                    3
                );
        }

        private void InitializeShowDebug(
            bool generatorShowDebug)
        {
            mShowDebug.speed = 3;
            mShowDebug.value = generatorShowDebug;
            mShowDebug.valueChanged.RemoveAllListeners();
            mShowDebug.valueChanged.AddListener(Repaint);
        }

        private void SetViewportDimensions()
        {
            Viewport.SetClientRectangle(
                0,
                ToolbarHeight,
                position.width,
                position.height - ToolbarHeight - StatusBarHeight
            );
        }

        #endregion

        #region Modules cache

        private int mModuleCount;
        private List<CGModule> mModules;

        public List<CGModule> Modules
        {
            get
            {
                //TODO DESIGN Do we need this modules cache? Can't we use Generator.Modules directly?
                if (mModules == null)
                    mModules = new List<CGModule>(Generator.Modules.ToArray());
                return mModules;
            }
            private set => mModules = value;
        }

        private void UpdateModulesCache()
        {
            Modules.Clear();
            Modules.AddRange(Generator.Modules);
            mModuleCount = Modules.Count; // store count to be checked in window GUI
        }

        #endregion

        #region Unity callbacks

        [UsedImplicitly]
        private void OnEnable()
        {
            ui = new CanvasUI(this);
            ResetState();

            SceneView.duringSceneGui -= OnSceneGUI;
            SceneView.duringSceneGui += OnSceneGUI;

            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.pauseStateChanged -= OnPauseStateChanged;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            EditorApplication.pauseStateChanged += OnPauseStateChanged;
            EditorApplication.update -= OnUpdate;
            EditorApplication.update += OnUpdate;

            autoRepaintOnSceneChange = true;
            wantsMouseMove = true;
            wantsMouseEnterLeaveWindow = true;
            wantsLessLayoutEvents = false;

            Undo.undoRedoPerformed -= OnUndoRedo;
            Undo.undoRedoPerformed += OnUndoRedo;
        }


        [UsedImplicitly]
        private void OnDisable()
        {
            SceneView.duringSceneGui -= OnSceneGUI;

            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.pauseStateChanged -= OnPauseStateChanged;
            SetIPE();
            EditorApplication.update -= OnUpdate;
            Undo.undoRedoPerformed -= OnUndoRedo;
        }

        [UsedImplicitly]
        private void OnDestroy()
        {
            DestroyModuleEditors();
            Resources.UnloadUnusedAssets();
        }

        [UsedImplicitly]
        private void OnSelectionChange()
        {
            CurvyGenerator gen = null;
            List<CGModule> modules = DTSelection.GetAllAs<CGModule>();
            if (modules.Count > 0)
                gen = modules[0].Generator;
            if (gen == null)
                gen = DTSelection.GetAs<CurvyGenerator>();
            if (gen != null && (Generator == null || gen != Generator))
            {
                Initialize(gen);
                Repaint();
            }
            else if (modules.Count > 0 && CurvyProject.Instance.CGSynchronizeSelection)
            {
                CanvasSelection.SetSelectionTo(modules);
                Repaint();
            }

            //OnSelectionChange is called when a selected module is deleted (from the hierarchy for example)
            CanvasSelection.SelectedModules.RemoveAll(m => m == null);
        }

        [UsedImplicitly]
        private void OnGUI()
        {
            //Debug.Log($"{EV.type}");
            //EventType eventTypeAtStart = EV.type;

            bool isLayout = EV.type == EventType.Layout;
            bool isRepaint = EV.type == EventType.Repaint;

            float repaintDeltaTime = isRepaint
                ? GetTimeSinceLastRepaint()
                : 0;

            if (!Generator)
                return;

            if (!Generator.IsInitialized)
                Generator.Initialize();

            bool needReorderModules;
            bool needRepaint = false;
            if (!Application.isPlaying && !Generator.IsInitialized)
            {
                needRepaint = true;
                needReorderModules = false;
            }
            else
            {
                UpdateModulesCache();

                if (isLayout)
                    SetViewportDimensions();

                if (!isLayout && !isRepaint)
                     ProcessPreDrawingInputs(GetHoveredModule());

                //todo bug We have an issue of dependencies order.
                //UpdateSelection needs the dragged module and the hovered module. The dragged module is updated inside DrawWindow (in module drawing code) and hovered module is based on comparison between mouse and modules coordinates.
                //This means that:
                //1. Calling Update Selection here will work with outdated dragged module, which leads to a bug where, when FPS is really low, you can start a rectangle selection drag and a module drag on the same frame. I avoided this bug by using mouseDownWasOnEmptyCanvas, which is not a nice solution
                //2. When dragging modules, I believe that GetHoveredModule is outdated until we apply the module drag on the modules dimensions property, inside ProcessPostDrawingInputs->DragModules. But to update the dragged modules, we need the selection to be up to date. So this probably means that Update Selection should be divided into two parts?
                needRepaint |= UpdateSelection(
                    GetDraggedModule(),
                    EV.type,
                    GetHoveredModule(),
                    EV.button,
                    EV.control,
                    EV.shift
                );

                if (canvasState.IsDragging)
                    SetMouseCursorToPan(Viewport.ClientRectangle);

                needRepaint |= DrawWindow(
                    isRepaint,
                    out needReorderModules
                );

                CGModule draggedModule = GetDraggedModule();
                if (isRepaint)
                {
                    Vector2 autoScrollTranslation = TryAutoScroll(
                        draggedModule != null,
                        IsLinkDrag,
                        selectionRectangle.IsDragging,
                        repaintDeltaTime
                    );

                    bool hasScrolled = autoScrollTranslation != Vector2.zero;
                    if (hasScrolled && draggedModule != null)
                        DragModules(
                            draggedModule,
                            CanvasSelection.SelectedModules,
                            autoScrollTranslation / Viewport.Zoom,
                            EV.alt
                        );
                    needRepaint |= hasScrolled;
                }
                if (!isLayout && !isRepaint)
                    needRepaint |= ProcessPostDrawingInputs(draggedModule,
                        CanvasSelection.SelectedModules
                    );


                Viewport.ChangeFocusIfNeeded(
                    CanvasSelection.SelectedModules,
                    Modules.GetModulesBoundingBox(),
                    EV
                );

                // IPE
                SyncIPE();
            }

            if (needReorderModules)
            {
                Generator.ReorderModules();
                Viewport.CenterViewOn(
                    Modules.GetModulesBoundingBox(),
                    true,
                    false
                );
            }

            needRepaint = needRepaint
                          || canvasState.IsDragging
                          || selectionRectangle.IsDragging
                          || IsLinkDrag
                          || mShowDebug.isAnimating
                          || Viewport.NeedRepaint
                          || Modules.Exists(m => GetModuleEditor(m).NeedRepaint);
            
            if (needRepaint)
                //Debug.LogWarning($"{eventTypeAtStart}");
                Repaint();
        }


        private void OnUndoRedo()
        {
            if (!Generator)
                return;

            if (mModuleCount == Generator.GetComponentsInChildren<CGModule>().Length)
                return;

            Generator.Initialize(true);
            Generator.Initialize();
            Initialize(Generator);
        }

        private void OnUpdate()
        {
            if (Generator)
                Viewport.UpdateAnimations(Now);
        }


        private void OnPlayModeStateChanged(
            PlayModeStateChange state) =>
            OnStateChanged();

        private void OnPauseStateChanged(
            PauseState state) =>
            OnStateChanged();

        private void OnStateChanged()
        {
            DestroyModuleEditors();

            CurvyGenerator newGenerator;
            if (!Generator
                && Selection.activeGameObject
                && (newGenerator = Selection.activeGameObject.GetComponent<CurvyGenerator>()))
            {
                Initialize(newGenerator);
                Repaint();
            }
        }

        private void OnSceneGUI(
            SceneView sceneView)
        {
            if (!Generator)
                return;

            for (int i = 0; i < Modules.Count; i++)
            {
                CGModule module = Modules[i];
                if (module != null && module.IsInitialized && module.IsConfigured && module.Active)
                {
                    CGModuleEditorBase ed = GetModuleEditor(module);
                    ed.OnModuleSceneGUI();
                    if (Generator.ShowDebug && ed.ShowDebugVisuals)
                        ed.OnModuleSceneDebugGUI();
                }
            }
        }

        #endregion

        #region Module editors

        private readonly Dictionary<CGModule, CGModuleEditorBase> moduleEditors = new Dictionary<CGModule, CGModuleEditorBase>();

        private CGModuleEditorBase GetModuleEditor(
            [NotNull] CGModule module)
        {
            CGModuleEditorBase ed;
            if (!moduleEditors.TryGetValue(
                    module,
                    out ed
                ))
            {
                ed = Editor.CreateEditor(module) as CGModuleEditorBase;
                if (ed)
                {
                    ed.Graph = this;
                    moduleEditors.Add(
                        module,
                        ed
                    );
                }
                else
                    DTLog.LogError(
                        "[Curvy] Curvy Generator: Missing editor script for module '" + module.GetType().Name + "' !",
                        module
                    );
            }

            return ed;
        }

        private void DestroyModuleEditors()
        {
            List<CGModuleEditorBase> ed = new List<CGModuleEditorBase>(moduleEditors.Values);
            for (int i = ed.Count - 1; i >= 0; i--)
                DestroyImmediate(ed[i]);
            moduleEditors.Clear();
            inPlaceEditTarget = null;
            inPlaceEditInitiatedBy = null;
        }

        #endregion

        #region repaint deltat time

        private double lastRepaintTime;
        private double Now => EditorApplication.timeSinceStartup;

        private float GetTimeSinceLastRepaint()
        {
            float repaintDeltaTime = (float)(Now - lastRepaintTime);
            lastRepaintTime = Now;
            return repaintDeltaTime;
        }

        #endregion
    }
}