// =====================================================================
// Copyright © 2013 ToolBuddy
// All rights reserved
// 
// http://www.toolbuddy.net
// =====================================================================

using System;
using System.Linq;
using FluffyUnderware.Curvy.Generator;
using JetBrains.Annotations;
using UnityEditor;
using UnityEngine;

namespace FluffyUnderware.CurvyEditor.Generator
{
    public partial class CGGraph
    {
        [CanBeNull]
        private CGModuleLink GetLinkUnderMouse(
            float zoom)
        {
            for (int moduleIndex = 0; moduleIndex < Modules.Count; moduleIndex++)
            {
                CGModule module = Modules[moduleIndex];
                for (int linkIndex = 0; linkIndex < module.OutputLinks.Count; linkIndex++)
                {
                    CGModuleLink outputLink = module.OutputLinks[linkIndex];
                    if (IsMouseOverLink(
                            outputLink,
                            zoom
                        ))
                        return outputLink;
                }
            }

            return null;
        }

        private bool IsMouseOverLink(
            [NotNull] CGModuleLink link,
            float zoom)
        {
            if (link == null)
                return false;

            CGModule module = Generator.GetModule(
                link.ModuleID,
                true
            );
            CGModule targetModule = Generator.GetModule(
                link.TargetModuleID,
                true
            );

            if (module == null)
                throw new InvalidOperationException($"Module with ID {link.ModuleID} not found");
            if (targetModule == null)
                throw new InvalidOperationException($"Module with ID {link.TargetModuleID} not found");

            Vector3 startPosition = module.GetOutputSlot(link.SlotName).Origin;
            Vector3 endPosition = targetModule.GetInputSlot(link.TargetSlotName).Origin;

            GetLinkBezierTangents(
                startPosition,
                endPosition,
                out Vector2 startTangent,
                out Vector2 endTangent
            );

            return HandleUtility.DistancePointBezier(
                       Viewport.CanvasMousePosition,
                       startPosition,
                       endPosition,
                       startTangent,
                       endTangent
                   )
                   <= LinkSelectionDistance / zoom;
        }

        #region Drawing

        private void DrawModuleOutputLinks(
            [NotNull] CGModule module)
        {
            foreach (CGModuleOutputSlot slotOut in module.OutputByName.Values)
                foreach (CGModuleSlot slotIn in slotOut.LinkedSlots)
                    DrawOutputLink(
                        slotOut,
                        slotIn,
                        Viewport.Zoom
                    );
        }

        private void DrawOutputLink(
            [NotNull] CGModuleOutputSlot startSlot,
            [NotNull] CGModuleSlot endSlot,
            float zoom)
        {
            Vector2 startPosition = startSlot.Origin;
            Vector2 endPosition = endSlot.Origin;

            bool isLinkClipped = Viewport.IsLinkClipped(
                startPosition,
                endPosition
            );

            if (isLinkClipped)
                return;

            float linkWidth = GetLinkWidth(
                endSlot,
                startSlot,
                zoom
            );

            Color slotColor = DataTypeColors.Instance.GetTypeColor(startSlot.Info.DataType);

            GetLinkBezierTangents(
                startPosition,
                endPosition,
                out Vector2 startTangent,
                out Vector2 endTangent
            );

            if (((CGModuleInputSlot)endSlot).IsOnRequest)
                DrawDoubleLink(
                    startPosition,
                    endPosition,
                    startTangent,
                    endTangent,
                    slotColor,
                    CurvyStyles.LineTexture,
                    linkWidth,
                    zoom
                );
            else
                DrawSimpleLink(
                    startPosition,
                    endPosition,
                    startTangent,
                    endTangent,
                    slotColor,
                    CurvyStyles.LineTexture,
                    linkWidth
                );
        }

        private void DrawLinkDrag()
        {
            if (IsLinkDrag == false)
                throw new InvalidOperationException("No link is being dragged");

            Vector2 startPosition = IsOutputLinkDrag
                ? OutputLinkDragFrom.Origin
                : Viewport.CanvasMousePosition;
            Vector2 endPosition = IsOutputLinkDrag
                ? Viewport.CanvasMousePosition
                : InputLinkDragFrom.Origin;

            //todo both links seem the same to me. Change CurvyStyles.RequestLineTexture so that it is double link?
            Texture2D lineTexture = linkDragFrom.IsOnRequest
                    ? CurvyStyles.RequestLineTexture
                    : CurvyStyles.LineTexture;

            DrawSimpleLink(
                startPosition,
                endPosition,
                Color.white,
                lineTexture,
                2
            );
        }

        private static void DrawSimpleLink(
            Vector2 startPosition,
            Vector2 endPosition,
            Color slotColor,
            [NotNull] Texture2D texture,
            float linkWidth)
        {
            GetLinkBezierTangents(
                startPosition,
                endPosition,
                out Vector2 startTangent,
                out Vector2 endTangent
            );

            DrawSimpleLink(
                startPosition,
                endPosition,
                startTangent,
                endTangent,
                slotColor,
                texture,
                linkWidth
            );
        }

        private static void DrawSimpleLink(
            Vector2 startPosition,
            Vector2 endPosition,
            Vector3 startTangent,
            Vector3 endTangent,
            Color slotColor,
            [NotNull] Texture2D texture,
            float linkWidth)
        {
            Handles.DrawBezier(
                startPosition,
                endPosition,
                startTangent,
                endTangent,
                slotColor,
                texture,
                linkWidth
            );
        }

        private static void DrawDoubleLink(
            Vector2 startPosition,
            Vector2 endPosition,
            Vector2 startTangent,
            Vector2 endTangent,
            Color slotColor,
            [NotNull] Texture2D texture,
            float linkWidth,
            float zoom)
        {
            //draw two parallel lines
            Vector2 yOffset = new Vector3(
                0,
                2 / zoom
            );
            Handles.DrawBezier(
                startPosition + yOffset,
                endPosition + yOffset,
                startTangent + yOffset,
                endTangent + yOffset,
                slotColor,
                texture,
                linkWidth
            );

            Handles.DrawBezier(
                startPosition - yOffset,
                endPosition - yOffset,
                startTangent - yOffset,
                endTangent - yOffset,
                slotColor,
                texture,
                linkWidth
            );
        }

        /// <summary>
        /// Given a link's start and end positions, you get the Bezier tangents of the link at those positions
        /// </summary>
        private static void GetLinkBezierTangents(
            Vector2 startPosition,
            Vector2 endPosition,
            out Vector2 startTangent,
            out Vector2 endTangent)
        {
            float deltaX = Mathf.Abs(endPosition.x - startPosition.x);
            float deltaY = Mathf.Abs(endPosition.y - startPosition.y);

            float xInfluence = deltaX / 2;
            //when there is a big delta in y, a small delta in x, and multiple links going to the same module, the links are too close to distinguish. I "bump" the links (by increasing the tangent) in those cases so that they are distinguishable near the modules.
            float yInfluence = (100f
                                * Mathf.Min(
                                    1000f,
                                    deltaY
                                ))
                               / 1000f;

            Vector2 tangent = new Vector2(
                xInfluence + yInfluence,
                0
            );
            startTangent = startPosition + tangent;
            endTangent = endPosition - tangent;
        }

        private float GetLinkWidth(
            CGModuleSlot slotIn,
            CGModuleOutputSlot slotOut,
            float zoom)
        {
            bool isLinkSelected = CanvasSelection.SelectedLink != null
                                  && CanvasSelection.SelectedLink.IsBetween(
                                      slotOut,
                                      slotIn
                                  );

            const float unselectedLinkWidth = 2;
            const float selectedLinkWidth = 7;
            return (isLinkSelected
                       ? selectedLinkWidth
                       : unselectedLinkWidth)
                   / zoom;
        }

        #endregion


        #region Link dragging

        [CanBeNull]
        private CGModuleSlot linkDragFrom;

        /// <summary>
        /// The filtered modules list shows only modules that can be linked to this slot
        /// If null, no filtered modules list is shown
        /// </summary>
        [CanBeNull]
        private CGModuleSlot modulesFilter;

        [CanBeNull] private CGModuleInputSlot InputLinkDragFrom => linkDragFrom as CGModuleInputSlot;

        [CanBeNull] private CGModuleOutputSlot OutputLinkDragFrom => linkDragFrom as CGModuleOutputSlot;

        /// <summary>
        /// Gets whether a link is currently dragged
        /// </summary>
        private bool IsLinkDrag => linkDragFrom != null;

        private bool IsOutputLinkDrag => OutputLinkDragFrom != null;
        private bool IsInputLinkDrag => InputLinkDragFrom != null;

        /// <summary>
        /// The filtered modules list shows only modules that can be linked to this slot
        /// If null, no filtered modules list is shown
        /// </summary>
        [CanBeNull]
        private CGModuleOutputSlot OutputModulesFilter => modulesFilter as CGModuleOutputSlot;

        /// <summary>
        /// The filtered modules list shows only modules that can be linked to this slot
        /// If null, no filtered modules list is shown
        /// </summary>
        [CanBeNull]
        private CGModuleInputSlot InputModulesFilter => modulesFilter as CGModuleInputSlot;


        private bool UpdateLinkDragState(
            Event @event)
        {
            bool wasEventUsed = false;

            if (Viewport.IsMouseHover)
                for (int i = 0; i < Modules.Count; i++)
                    wasEventUsed |= TryStartOrEndLinksWithModule(
                        Modules[i],
                        @event
                    );

            if (IsLinkDrag)
            {
                //end link on empty area
                if (ShouldStopLinkDrag(@event))
                {
                    modulesFilter = linkDragFrom;
                    wasEventUsed = true;
                    StopLinkDrag();
                }
                //cancel link
                else if (ShouldCancelLinkDrag(@event))
                {
                    wasEventUsed = true;
                    StopLinkDrag();
                }
            }

            return wasEventUsed;
        }

        /// <summary>
        /// Starts new links, modifies existing links or ends links when dropped on valid slots
        /// </summary>
        /// <param name="module"></param>
        /// <param name="event"></param>
        /// <returns></returns>
        private bool TryStartOrEndLinksWithModule(
            [NotNull] CGModule module,
            Event @event)
        {
            bool wasEventUsed = false;

            foreach (CGModuleInputSlot inputSlot in module.Input)
                if (IsOutputLinkDrag)
                {
                    TryEndLinkDragOnSlot(
                        @event,
                        inputSlot
                    );
                    wasEventUsed = IsLinkDrag == false;
                } 
                else if (ShouldStartLinkDrag(
                             inputSlot,
                             @event
                         ))
                {
                    wasEventUsed = true;
                    if (@event.control
                        && inputSlot.Count >= 1)
                        StartDragExistingLink(inputSlot);
                    else
                        StartDragNewLink(inputSlot);
                }

            foreach (CGModuleOutputSlot outputSlot in module.Output)
                if (IsInputLinkDrag)
                {
                    TryEndLinkDragOnSlot(
                        @event,
                        outputSlot
                    );
                    wasEventUsed = IsLinkDrag == false;
                }
                else if (ShouldStartLinkDrag(
                             outputSlot,
                             @event
                         ))
                {
                    wasEventUsed = true;
                    StartDragNewLink(outputSlot);
                }

            return wasEventUsed;
        }

        private void TryEndLinkDragOnSlot(
            Event @event,
            CGModuleSlot slot)
        {
            if (IsLinkDrag == false)
                throw new InvalidOperationException("No link is being dragged");

            if (!ShouldStopLinkDrag(@event))
                return;
            if (!IsMouseOnSlotDropZone(slot))
                return;

            CGModuleSlot other;
            {
                if (IsOutputLinkDrag)
                    other = OutputLinkDragFrom;
                else if (IsInputLinkDrag)
                    other = InputLinkDragFrom;
                else
                    throw new InvalidOperationException("No link is being dragged");
            }

            if (slot.CanLinkTo(other))
            {
                other.LinkTo(slot);
                EditorUtility.SetDirty(slot.Module);
            }

            StopLinkDrag();
        }

        private bool ShouldStartLinkDrag(
            [NotNull] CGModuleSlot slot,
            [NotNull] Event @event) =>
            @event.type == EventType.MouseDown
            && @event.button == 0
            && IsMouseOnSlotDropZone(slot);

        private static bool ShouldCancelLinkDrag(
            [NotNull] Event @event) =>
            @event.isKey && @event.keyCode == KeyCode.Escape;

        private bool ShouldStopLinkDrag(
            [NotNull] Event @event)
            => @event.isMouse
               && @event.type != EventType.MouseDrag
               && @event.type != EventType.MouseDown;

        private bool IsMouseOnSlotDropZone(
            [NotNull] CGModuleSlot slot) =>
            slot.CanvasSpaceDropZone.Contains(Viewport.CanvasMousePosition);


        private void StartDragNewLink(
            [NotNull] CGModuleSlot slot)
        {
            CanvasSelection.Clear();
            linkDragFrom = slot;
        }

        private void StartDragExistingLink(
            [NotNull] CGModuleInputSlot slot)
        {
            CGModuleOutputSlot linkedOutSlot = slot.SourceSlot();
            linkedOutSlot.UnlinkFrom(slot);
            //todo should other module be dirtied as well?
            EditorUtility.SetDirty(slot.Module);
            StartDragNewLink(linkedOutSlot);
        }

        private void StopLinkDrag() =>
            linkDragFrom = null;

        private void ResetLinkDragState()
        {
            linkDragFrom = null;
            modulesFilter = null;
        }

        private void TryShowFilteredAddModuleMenu()
        {
            if (OutputModulesFilter)
            {
                ui.ShowFilteredAddModuleMenu(OutputModulesFilter);
                modulesFilter = null;
            }
            else if (InputModulesFilter)
            {
                ui.ShowFilteredAddModuleMenu(InputModulesFilter);
                modulesFilter = null;
            }
        }

        #endregion
    }
}