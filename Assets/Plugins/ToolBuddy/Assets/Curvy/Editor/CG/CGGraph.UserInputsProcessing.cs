// =====================================================================
// Copyright © 2013 ToolBuddy
// All rights reserved
// 
// http://www.toolbuddy.net
// =====================================================================

using System;
using System.Collections.Generic;
using FluffyUnderware.Curvy;
using FluffyUnderware.Curvy.Generator;
using FluffyUnderware.Curvy.Generator.Modules;
using FluffyUnderware.DevTools.Extensions;
using JetBrains.Annotations;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace FluffyUnderware.CurvyEditor.Generator
{
    public partial class CGGraph
    {
        private void ProcessPreDrawingInputs(
            CGModule hoveredModule)
        {
            //todo this should be reworked to clearly define an order between the different drag operations: module, link, canvas and selection rectangle.
            //EV.Use calls should also be scrutinized, to add or remove calls if needed
            if (EV.isMouse || EV.type == EventType.DragUpdated || EV.type == EventType.DragExited)
                MousePosition = EV.mousePosition;

            //try clear status bar
            if (Viewport.IsMouseHover
                && EV.type == EventType.MouseUp)
                statusBar.Clear();


            //try delete link with click
            if (Viewport.IsMouseHover
                && EV.type == EventType.MouseUp
                && EV.button == 1)
            {
                CGModuleLink link = GetLinkUnderMouse(Viewport.Zoom);
                if (link != null)
                {
                    Generator.DeleteLink(link);
                    EV.Use();
                }
            }

            //try show context menu
            if (Viewport.IsMouseHover
                && EV.type == EventType.MouseUp
                && EV.button == 1)
                //todo should it have an EV.Use()?
                ui.ContextMenu(MousePosition);

            //try apply scroll wheel
            if (EV.type == EventType.ScrollWheel)
            {
                //todo move next to oher canvas drags?
                Viewport.ApplyScrollWheelEvent(EV);
                EV.Use();
            }

            bool isModuleDragInProgress = GetDraggedModule() != null;
            bool isSelectionRectangleDrag = selectionRectangle.IsDragging;

            //Link drag
            if (isModuleDragInProgress == false
                && isSelectionRectangleDrag == false)
            {
                bool wasEventUsed = UpdateLinkDragState(EV);
                if (wasEventUsed)
                    EV.Use();
            }

            if (IsLinkDrag)
                if (EV.type == EventType.MouseDrag)
                    EV.Use();

            //canvas drag
            if (isModuleDragInProgress == false
                && isSelectionRectangleDrag == false
                && IsLinkDrag == false)
            {
                bool eventWasUsed = canvasState.Update(EV);
                if (eventWasUsed)
                    EV.Use();
            }

            if (canvasState.IsDragging)
            {
                if (Viewport.IsMouseHover)
                    if (EV.type == EventType.MouseDrag)
                        Viewport.ScrollBy(-EV.delta);

                if (EV.type == EventType.MouseDrag)
                    EV.Use();
            }

            if (Viewport.IsMouseHover)
                switch (EV.type)
                {
                    case EventType.DragUpdated:
                        DragAndDrop.visualMode = hoveredModule == null
                            ? DragAndDropVisualMode.Link
                            : DragAndDropVisualMode.None;
                        break;
                    case EventType.DragPerform:
                        if (hoveredModule == null)
                            HandleDragDropDone(
                                Viewport.CanvasMousePosition,
                                Generator
                            );
                        break;
                }
        }

        private bool ProcessPostDrawingInputs(
            [CanBeNull] CGModule draggedModule,
            [NotNull] [ItemNotNull] List<CGModule> selectedModules)
        {
            bool needRepaint = false;


            if (Viewport.IsMouseHover
                && draggedModule != null
                && EV.delta != Vector2.zero)
            {
                DragModules(
                    draggedModule,
                    selectedModules,
                    EV.delta / Viewport.Zoom,
                    EV.alt
                );

                needRepaint = true;
            }

            ProcessEditCommands(EV);

            TryShowFilteredAddModuleMenu();

            if (ShouldExitTitleEditing(EV))
            {
                ExitTitleEditing();
                needRepaint = true;
            }

            return needRepaint;
        }

        private bool UpdateSelection(
            [CanBeNull] CGModule draggedModule,
            EventType eventType,
            [CanBeNull] CGModule hoveredModule,
            int mouseButton,
            bool isControlModifier,
            bool isShiftModifier)
        {
            bool hasSelectionChanged;

            Vector2 canvasMousePosition = Viewport.CanvasMousePosition;
            bool isModuleDrag = draggedModule != null;
            bool isMouseOverModule = hoveredModule != null;
            bool isDragging = canvasState.IsDragging
                              || isModuleDrag
                              || IsLinkDrag
                              || selectionRectangle.IsDragging;

            switch (eventType)
            {
                case EventType.MouseDown:
                    mouseDownWasOnEmptyCanvas =
                        Viewport.IsMouseHover
                        && hoveredModule == null;

                    if (Viewport.IsMouseHover
                        && isDragging == false)
                        hasSelectionChanged = CanvasSelection.OnMouseDown(
                            mouseButton,
                            GetLinkUnderMouse(Viewport.Zoom),
                            hoveredModule,
                            isControlModifier,
                            isShiftModifier
                        );
                    else
                        hasSelectionChanged = false;

                    break;
                case EventType.MouseDrag:
                {
                    if (mouseDownWasOnEmptyCanvas
                        && Viewport.IsMouseHover
                        && isDragging == false
                        && isMouseOverModule == false)
                        selectionRectangle.StartDrag(canvasMousePosition);

                    hasSelectionChanged = false;
                    break;
                }
                case EventType.MouseUp:
                    if (selectionRectangle.IsDragging)
                    {
                        selectionRectangle.EndDrag(
                            canvasMousePosition,
                            Modules,
                            CanvasSelection,
                            isControlModifier,
                            isShiftModifier
                        );
                        hasSelectionChanged = true;
                    }
                    else if (Viewport.IsMouseHover
                             && isDragging == false)
                        hasSelectionChanged = CanvasSelection.OnMouseUp(
                            mouseButton,
                            GetLinkUnderMouse(Viewport.Zoom),
                            hoveredModule
                        );
                    else
                        hasSelectionChanged = false;

                    break;
                //happens when mouse up is outside of the window
                case EventType.Ignore:
                    if (selectionRectangle.IsDragging)
                    {
                        selectionRectangle.EndDrag(
                            canvasMousePosition,
                            Modules,
                            CanvasSelection,
                            isControlModifier,
                            isShiftModifier
                        );
                        hasSelectionChanged = true;
                    }
                    else
                        hasSelectionChanged = false;

                    break;
                //this should not be necessary now that we use the EventType.Ignore event, but I left it just in case
                case EventType.MouseMove: //todo use leave event
                    if (selectionRectangle.IsDragging)
                        selectionRectangle.CancelDrag();
                    hasSelectionChanged = false;
                    break;
                default:
                    hasSelectionChanged = false;
                    break;
            }

            if (isModuleDrag
                && !CanvasSelection.SelectedModules.Contains(draggedModule))
            {
                CanvasSelection.SetSelectionTo(draggedModule);
                hasSelectionChanged = true;
            }

            return hasSelectionChanged;
        }

        private void SetMouseCursorToPan(
            Rect clientRectangle)
        {
            EditorGUIUtility.AddCursorRect(
                clientRectangle,
                MouseCursor.Pan
            );
        }

        private static void HandleDragDropDone(
            Vector2 canvasMousePosition,
            CurvyGenerator curvyGenerator)
        {
            Vector2 mousePosition = canvasMousePosition;

            foreach (Object @object in DragAndDrop.objectReferences)
            {
                CGModule module = null;
                if (@object is GameObject gameObject)
                {
                    CurvySpline spline = gameObject.GetComponent<CurvySpline>();
                    if (spline)
                    {
                        CurvyShape shape = gameObject.GetComponent<CurvyShape>();
                        if (shape)
                        {
                            InputSplineShape inputModule = curvyGenerator.AddModule<InputSplineShape>();
                            inputModule.Shape = spline;
                            module = inputModule;
                        }
                        else
                        {
                            InputSplinePath inputModule = curvyGenerator.AddModule<InputSplinePath>();
                            inputModule.Spline = spline;
                            module = inputModule;
                        }
                    }
                    else
                    {
                        InputGameObject inputModule = curvyGenerator.AddModule<InputGameObject>();
                        inputModule.GameObjects.Add(new CGGameObjectProperties(gameObject));
                        module = inputModule;
                    }
                }
                else if (@object is Mesh mesh)
                {
                    InputMesh inputModule = curvyGenerator.AddModule<InputMesh>();
                    inputModule.Meshes.Add(new CGMeshProperties(mesh));
                    module = inputModule;
                }

                if (module)
                {
                    module.Properties.Dimensions.position = mousePosition;
                    module.Properties.Dimensions.xMin -= module.Properties.MinWidth / 2;
                    mousePosition.y += module.Properties.Dimensions.height;
                }
            }

            DragAndDrop.AcceptDrag();
        }

        /// <summary>
        /// Processes the Unity commands (Copy, Cut, Paste, SelectAll, Delete, SoftDelete, Duplicate)
        /// </summary>
        /// <param name="event"></param>
        private void ProcessEditCommands(
            Event @event)
        {
            if (@event.type == EventType.ValidateCommand)
                switch (@event.commandName)
                {
                    case "Copy":
                    case "Cut":
                    case "Paste":
                    case "SelectAll":
                    case "Delete":
                    case "SoftDelete":
                    case "Duplicate":
                        @event.Use();
                        break;
                }
            else if (@event.type == EventType.ExecuteCommand)
                switch (@event.commandName)
                {
                    case "Copy":
                        CanvasUI.CopySelection(ui);
                        break;
                    case "Cut":
                        CanvasUI.CutSelection(ui);
                        break;
                    case "Paste":
                        CanvasUI.PastSelection(ui);
                        break;
                    case "SelectAll":
                        CanvasUI.SelectAll(ui);
                        break;
                    case "Delete":
                    case "SoftDelete":
                        CanvasUI.DeleteSelection(ui);
                        break;
                    case "Duplicate":
                        CanvasUI.Duplicate(ui);
                        break;
                }
        }

        [CanBeNull]
        private CGModule GetHoveredModule()
        {
            Vector2 canvasMousePosition = Viewport.CanvasMousePosition;

            for (int index = 0; index < Modules.Count; index++)
            {
                CGModule m = Modules[index];
                if (m.Properties.Dimensions.Contains(canvasMousePosition))
                    return m;
            }

            return null;
        }

        private Vector2 TryAutoScroll(
            bool isModuleDrag,
            bool isLinkDrag,
            bool isSelectionRectangleDrag,
            float deltaTime)
        {
            int autoScrollBorder = isSelectionRectangleDrag
                // I believe tend to place the mouse near the edge when dragging a selection rectangle to select all. With a big scroll border, this operation will tend to trigger an auto scroll, which is not welcomed.
                ? 20
                : 25;

            //todo make scroll speed variable: it starts slow and then accelerates
            const int autoScrollSpeed = 700;


            bool shouldAutoscroll = (isModuleDrag || isLinkDrag || isSelectionRectangleDrag)
                                    && Viewport.IsMouseInAutoScrollArea(autoScrollBorder);


            float autoScrollAmount;
            {
                //I added a MaxScrollAmountPerFrame for the following reasing:
                // deltaTime is the time since last repaint. If someone holds mouse down for two seconds, with no other input, then starts drag, there will be no repaint in those 2s. Meaning deltaTime will be equal to 2s. Meaning that autoscroll will go way too far.
                const int MaxScrollAmountPerFrame = 50;
                autoScrollAmount = Mathf.Min(
                    autoScrollSpeed * deltaTime,
                    MaxScrollAmountPerFrame
                );
            }

            return shouldAutoscroll
                ? Viewport.AutoScroll(
                    autoScrollBorder,
                    autoScrollAmount
                )
                : Vector2.zero;
        }


        #region Module drag

        [CanBeNull]
        private CGModule GetDraggedModule()
        {
            for (int index = 0; index < Modules.Count; index++)
            {
                CGModule m = Modules[index];
                if (GetModuleEditor(m).NeedsDrag)
                    return m;
            }

            return null;
        }

        private void DragModules(
            [NotNull] CGModule referenceModule,
            [NotNull] [ItemNotNull] List<CGModule> allModules,
            Vector2 dragDistance,
            bool eventAltModifier)
        {
            if (referenceModule == null)
                throw new ArgumentNullException(nameof(referenceModule));


            if (allModules.Count == 0)
                return;

            Vector2 translation = eventAltModifier
                ? GetSnappedTranslation(
                    referenceModule,
                    dragDistance
                )
                : dragDistance;

            if (translation != Vector2.zero)
            {
                foreach (CGModule module in allModules)
                    module.Properties.Dimensions.position += translation;

                if (Application.isPlaying == false)
                    Generator.gameObject.MarkParentSceneAsDirty();
            }
        }

        private Vector2 GetSnappedTranslation(
            [NotNull] CGModule referenceModule,
            Vector2 dragDistance)
        {
            Vector2 delta;
            {
                Vector2 referencePreSnapPosition = referenceModule.Properties.Dimensions.position;
                Vector2 referencePostSnapPosition = mouseSnapper.GetSnappedPosition(
                    referencePreSnapPosition,
                    dragDistance,
                    CurvyProject.Instance.CGGraphSnapping
                );
                delta = referencePostSnapPosition - referencePreSnapPosition;
            }
            return delta;
        }

        #endregion
    }
}