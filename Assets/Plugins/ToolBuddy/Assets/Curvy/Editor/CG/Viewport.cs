// =====================================================================
// Copyright © 2013 ToolBuddy
// All rights reserved
// 
// http://www.toolbuddy.net
// =====================================================================

using System;
using System.Collections.Generic;
using System.Linq;
using FluffyUnderware.Curvy.Generator;
using FluffyUnderware.Curvy.Utils;
using FluffyUnderware.DevTools.Extensions;
using FluffyUnderware.DevToolsEditor;
using JetBrains.Annotations;
using UnityEditor;
using UnityEngine;

namespace FluffyUnderware.CurvyEditor.Generator
{
    /// <summary>
    /// The area in the Curvy Generator window that shows the canvas.
    /// </summary>
    public class Viewport
    {
        [NotNull]
        private readonly CGGraph cgGraph;

        private Rect clientRectangle;

        /// <summary>
        /// Gets the position and size of the area in the editor window that shows the canvas. Coordinates are in the window space.
        /// </summary>
        public Rect ClientRectangle =>
            clientRectangle;

        public Rect VisibleCanvasArea
        {
            get
            {
                Vector2 min = ViewportToCanvas(
                    new Vector2(
                        0,
                        0
                    ),
                    Zoom
                );
                Vector2 max = ViewportToCanvas(
                    new Vector2(
                        ClientRectangle.width,
                        ClientRectangle.height
                    ),
                    Zoom
                );
                return new Rect(
                    min,
                    max - min
                );
            }
        }

        public bool IsMouseHover => ClientRectangle.Contains(cgGraph.MousePosition);

        /// <summary>
        /// Mouse coordinates in Viewport space
        /// </summary>
        public Vector2 MousePosition =>
            WindowToViewport(cgGraph.MousePosition);

        /// <summary>
        /// Mouse coordinates in Canvas space
        /// </summary>
        public Vector2 CanvasMousePosition => ViewportToCanvas(
            MousePosition,
            Zoom
        );

        public Viewport(
            [NotNull] CGGraph cgGraph)
        {
            if (cgGraph == null)
                throw new ArgumentNullException(nameof(cgGraph));

            this.cgGraph = cgGraph;
        }

        private Vector2 WindowToViewport(
            Vector2 position) =>
            position - ClientRectangle.position;

        private Vector2 ViewportToCanvas(
            Vector2 position,
            float zoom) =>
            ScrollValueToCanvasPosition(
                ScrollValue + position,
                zoom
            );

        private Vector2 CanvasToViewport(
            Vector2 position,
            float zoom) =>
            CanvasPositionToScrollValue(
                position,
                zoom
            )
            - ScrollValue;

        // ReSharper disable once TooManyArguments
        public void SetClientRectangle(
            int x,
            int y,
            float width,
            float height)
        {
            clientRectangle.Set(
                x,
                y,
                width,
                height
            );
        }

        #region Drawing

        private GUIStyle backGroundStyle;

        private GUIStyle BackGroundStyle
        {
            get
            {
                if (backGroundStyle == null)
                {
                    backGroundStyle = new GUIStyle();
                    backGroundStyle.normal.background = Texture2D.whiteTexture;
                }

                return backGroundStyle;
            }
        }

        public void DrawBackground()
        {
            DrawBackgroundColor();
            DrawGrid(clientRectangle);
        }

        private void DrawBackgroundColor()
        {
            DTGUI.PushColor(GetBackgroundColor());

            GUI.Box(
                clientRectangle,
                GUIContent.none,
                BackGroundStyle
            );
            DTGUI.PopColor();
        }

        private Color GetBackgroundColor()
        {
            float greyLevel = EditorGUIUtility.isProSkin
                ? 0.29f
                : .57f;

            return new Color(
                greyLevel,
                greyLevel,
                greyLevel
            );
        }

        #region Grid drawing

        private Color GetGridColor()
        {
            float greyLevel = EditorGUIUtility.isProSkin
                ? 0
                : .4f;

            return new Color(
                greyLevel,
                greyLevel,
                greyLevel,
                Zoom
            );
        }

        private Vector2 GetGridOrigin(
            float gridSize)
        {
            Vector2 gridOrigin;
            {
                Vector2 scrollPosition = CanvasToViewport(
                    Vector2.zero,
                    Zoom
                );
                gridOrigin.x = scrollPosition.x % gridSize;
                gridOrigin.y = scrollPosition.y % gridSize;
            }
            return gridOrigin;
        }

        private void DrawGrid(
            Rect canvasArea)
        {
            Color gridColor = GetGridColor();
            float gridThickness = 1f;
            float gridSize = CurvyProject.Instance.CGGraphSnapping * Zoom;
            Vector2 gridOrigin = GetGridOrigin(gridSize);

            Color previousColor = GUI.color;
            GUI.color = gridColor;

            Vector3 canvasOrigin = new Vector3(
                canvasArea.x,
                canvasArea.y,
                0
            );

            //inlined stuff
            float canvasAreaWidth = canvasArea.width;
            float canvasAreaHeight = canvasArea.height;
            float canvasAreaXMin = canvasArea.xMin;
            float canvasAreaXMax = canvasArea.xMax;
            float canvasAreaYMin = canvasArea.yMin;
            float canvasAreaYMax = canvasArea.yMax;
            float canvasOriginX = canvasOrigin.x;
            float canvasOriginY = canvasOrigin.y;
            Texture2D whiteTexture = Texture2D.whiteTexture;

            int verticalLinesCount = Mathf.CeilToInt(canvasAreaWidth / gridSize) + 1;
            int horizontalLinesCount = Mathf.CeilToInt(canvasAreaHeight / gridSize) + 1;

            // Draw vertical lines
            for (int i = 0; i < verticalLinesCount; i++)
            {
                float x = gridOrigin.x + (i * gridSize);
                if (x > canvasAreaWidth)
                    continue;

                float lineOriginX = Mathf.Clamp(
                    canvasOriginX + x,
                    canvasAreaXMin,
                    canvasAreaXMax
                );

                float lineOriginY = Mathf.Clamp(
                    canvasOriginY - gridSize,
                    canvasAreaYMin,
                    canvasAreaYMax
                );

                float lineEndY = Mathf.Clamp(
                    canvasOriginY + canvasAreaHeight + gridSize,
                    canvasAreaYMin,
                    canvasAreaYMax
                );

                Rect lineRect = new Rect(
                    lineOriginX,
                    lineOriginY,
                    gridThickness,
                    lineEndY - lineOriginY
                );
                GUI.DrawTexture(
                    lineRect,
                    whiteTexture
                );
            }

            // Draw horizontal lines
            for (int j = 0; j < horizontalLinesCount; j++)
            {
                float y = gridOrigin.y + (j * gridSize);
                if (y > canvasAreaHeight)
                    continue;


                float lineOriginX = Mathf.Clamp(
                    canvasOrigin.x - gridSize,
                    canvasArea.xMin,
                    canvasArea.xMax
                );

                float lineOriginY = Mathf.Clamp(
                    canvasOrigin.y + y,
                    canvasArea.yMin,
                    canvasArea.yMax
                );

                float lineEndX = Mathf.Clamp(
                    canvasOrigin.x + canvasAreaWidth + gridSize,
                    canvasArea.xMin,
                    canvasArea.xMax
                );


                Rect lineRect = new Rect(
                    lineOriginX,
                    lineOriginY,
                    lineEndX - lineOriginX,
                    gridThickness
                );
                GUI.DrawTexture(
                    lineRect,
                    whiteTexture
                );
            }

            GUI.color = previousColor;
        }

        #endregion

        #endregion


        public void ApplyScrollWheelEvent(
            [NotNull] Event scrollEvent)
        {
            if (scrollEvent.control)
                SetZoom(
                    Zoom - (scrollEvent.delta.y / 60),
                    MousePosition
                );
            else
            {
                Vector2 scrollDelta = scrollEvent.delta * 20;
                if (scrollEvent.alt)
                    scrollDelta *= 4;
                ScrollBy(scrollDelta);
            }
        }

        #region Zoom

        public const float MinZoom = 0.2f;
        public const float MaxZoom = 1f;


        public float Zoom =>
            cgGraph.Generator.ViewportZoom;

        /// <summary>
        /// When zoomed out enough, the modules are drawn in a simplified manner
        /// </summary>
        public bool IsInOverviewMode =>
            Zoom < 0.4f;

        public void SetZoom(
            float newZoom,
            Vector2 zoomCenter)
        {
            newZoom = Mathf.Clamp(
                newZoom,
                MinZoom,
                MaxZoom
            );
            if (cgGraph.Generator.ViewportZoom.Approximately(newZoom) == false)
            {
                float oldZoom = cgGraph.Generator.ViewportZoom;
                cgGraph.Generator.ViewportZoom = newZoom;
                RealignZoomCenter(
                    oldZoom,
                    newZoom,
                    zoomCenter
                );
            }
        }

        /// <summary>
        /// Maintains the zoom center in the same position in the canvas
        /// </summary>
        private void RealignZoomCenter(
            float preChangeZoom,
            float postChangeZoom,
            Vector2 zoomCenter)
        {
            Vector2 zoomCenterInCanvas = ViewportToCanvas(
                zoomCenter,
                preChangeZoom
            );

            ScrollValue = (zoomCenterInCanvas * postChangeZoom)
                          - (zoomCenter - (ClientRectangle.size / 2));
        }


        #region Zoom compatible GUI area

        private Matrix4x4 preScalingMatrix;

        private Rect GetClipArea(
            int toolbarHeight)
        {
            const int EditorTabHeight = 20; // the height of the unity tab in top of the editor window
            // This offset was needed to have the same number of padding pixels as in a unity window (I compared with a Scene View window)
            Vector2 scalableAreaOffset = new Vector2(
                1.5f,
                1f
            );

            Rect drawingArea = new Rect(
                scalableAreaOffset
                + new Vector2(
                    0,
                    (EditorTabHeight + toolbarHeight) / Zoom
                ),
                ClientRectangle.size / Zoom
            );
            return drawingArea;
        }

        public void BeginScalableArea(
            int toolbarHeight)
        {
            //Zooming out will show clipped content, due to the current clipping area. We stop the current one and start a new one that does not clip the content we want to show by zooming out
            GUI.EndClip();

            preScalingMatrix = GUI.matrix;

            GUI.BeginClip(
                GetClipArea(toolbarHeight),
                -ScrollValueToCanvasPosition(
                    ScrollValue,
                    Zoom
                ),
                Vector2.zero,
                false
            );

            GUI.matrix = Matrix4x4.Scale(
                new Vector3(
                    Zoom,
                    Zoom,
                    1f
                )
            );
        }

        public void EndScalableArea() =>
            GUI.matrix = preScalingMatrix;

        #endregion

        #endregion

        #region Scrolling

        private double? previousScrollAnimationUpdateTime;

        private Vector2 scrollValue
        {
            get => cgGraph.Generator.ViewportScroll;
            set => cgGraph.Generator.ViewportScroll = value;
        }

        /// <summary>
        /// Is there an ongoing scrolling animation
        /// </summary>
        private bool IsScrollValueAnimating => ScrollTarget != ScrollValue;

        /// <summary>
        /// The current scroll value. A scrolling of (0,0) means the viewport has its top left corner in the top left corner of the occupied canvas.
        /// Unit is pixels post zoom in/out.
        /// Setting ScrollValue will stop any ongoing animation
        /// </summary>
        public Vector2 ScrollValue
        {
            get => scrollValue;
            private set
            {
                scrollValue = value;
                ScrollTarget = value;
                ScrollSpeed = 0;
            }
        }

        /// <summary>
        /// Target scroll value for the scrolling animation
        /// </summary>
        public Vector2 ScrollTarget { get; private set; }

        /// <summary>
        /// Speed of the scrolling animation
        /// </summary>
        public float ScrollSpeed { get; private set; }

        public bool NeedRepaint => IsScrollValueAnimating;

        private void SetScrollTarget(
            Vector2 target,
            float speed)
        {
            ScrollTarget = target;
            ScrollSpeed = speed;
        }

        /// <summary>
        /// Returns the canvas coordinates of the top left corner of the viewport that matches the given scrolling value 
        /// </summary>
        private Vector2 ScrollValueToCanvasPosition(
            Vector2 scroll,
            float zoom) =>
            (scroll - (clientRectangle.size / 2))
            / zoom;

        private Vector2 CanvasPositionToScrollValue(
            Vector2 canvasPosition,
            float zoom) =>
            (canvasPosition * zoom) + (clientRectangle.size / 2);

        public void ScrollBy(
            Vector2 translation) =>
            ScrollValue += translation;

        public void UpdateAnimations(
            double now)
        {
            double timeSinceLastUpdate = previousScrollAnimationUpdateTime.HasValue == false
                ? 0
                : now - previousScrollAnimationUpdateTime.Value;

//TODO make a smoother animation
            scrollValue = Vector2.MoveTowards(
                ScrollValue,
                ScrollTarget,
                (float)(ScrollSpeed * timeSinceLastUpdate)
            );

            previousScrollAnimationUpdateTime = now;
        }

        #region Auto Scrolling

        private Vector2 GetAutoScrollTranslation(
            int autoScrollBorder,
            float autoScrollAmount)
        {
            Vector2 mousePosition = MousePosition;

            Vector2 translation = Vector2.zero;
            {
                // Check if the mouse is near the left edge of the viewport
                if (mousePosition.x < ClientRectangle.xMin + autoScrollBorder)
                    translation.x = -autoScrollAmount;
                // Check if the mouse is near the right edge of the viewport
                else if (mousePosition.x > ClientRectangle.xMax - autoScrollBorder)
                    translation.x = autoScrollAmount;

                // Check if the mouse is near the top edge of the viewport
                if (mousePosition.y < ClientRectangle.yMin + autoScrollBorder)
                    translation.y = -autoScrollAmount;
                // Check if the mouse is near the bottom edge of the viewport
                else if (mousePosition.y > ClientRectangle.yMax - autoScrollBorder)
                    translation.y = autoScrollAmount;
            }
            return translation;
        }

        public bool IsMouseInAutoScrollArea(
            int autoScrollBorderSize) =>
            VisibleCanvasArea.ScaleBy(-autoScrollBorderSize).Contains(CanvasMousePosition) == false;

        public Vector2 AutoScroll(
            int autoScrollBorder,
            float autoScrollAmount)
        {
            Vector2 translation = GetAutoScrollTranslation(
                autoScrollBorder,
                autoScrollAmount
            );

            ScrollBy(translation);

            return translation;
        }

        #endregion

        #endregion

        #region Auto Focus

        [NotNull]
        private readonly List<CGModule> previouslySelectedModules = new List<CGModule>();


        public void ChangeFocusIfNeeded(
            [NotNull] [ItemNotNull] List<CGModule> selectedModules,
            Rect canvasOccupiedArea,
            [NotNull] Event @event)
        {
            if (@event.type == EventType.ValidateCommand
                && @event.commandName == "FrameSelected")
                @event.Use();

            bool isFocusCommandGiven =
                @event.type == EventType.ExecuteCommand
                && @event.commandName == "FrameSelected";

            bool shouldFocusDueToSelectionChange = CurvyProject.Instance.CGAutoFocusOnSelection
                                                   && previouslySelectedModules.SequenceEqual(selectedModules) == false;

            if (selectedModules.Any()
                && (isFocusCommandGiven || shouldFocusDueToSelectionChange))
                CenterViewOn(
                    selectedModules.GetModulesBoundingBox(),
                    //todo activate zoom change after properly implementing cohabitation between scroll change and zoom change (which leads to scroll change to keep view centered)
                    false,
                    true
                );
            else if (isFocusCommandGiven)
                CenterViewOn(
                    canvasOccupiedArea,
                    true,
                    false
                );

            previouslySelectedModules.Clear();
            previouslySelectedModules.AddRange(selectedModules);
        }


        /// <summary>
        /// Aligns the center of the viewport with the center of the given area. The area is in canvas space.
        /// Also adjusts the zoom level to ensure the entire area is visible within the viewport.
        /// </summary>
        public void CenterViewOn(
            Rect area,
            bool changedZoom,
            bool animate)
        {
            Vector2 scrollDelta = (area.center - VisibleCanvasArea.center) * Zoom;
            Vector2 targetScrollValue = ScrollValue + scrollDelta;

            if (animate)
            {
                float speed = Mathf.Max(
                    60f,
                    scrollDelta.magnitude * 5f
                );

                SetScrollTarget(
                    targetScrollValue,
                    speed
                );
            }
            else
                ScrollValue = targetScrollValue;

            if (changedZoom)
            {
                if (animate)
                    throw new ArgumentException(
                        "Cannot animate zoom change and scroll change at the same time. This is not supported yet."
                    );

                // Calculate the zoom level needed to fit the area within the viewport
                Vector2 paddedClientRectangleSize = ClientRectangle.size - (Vector2.one * 20);
                float zoomX = paddedClientRectangleSize.x / area.width;
                float zoomY = paddedClientRectangleSize.y / area.height;
                float targetZoom = Mathf.Min(
                    zoomX,
                    zoomY
                );

                SetZoom(
                    targetZoom,
                    ClientRectangle.size / 2
                );
            }
        }

        #endregion

        #region Clipping

        public bool IsLinkClipped(
            Vector2 startPosition,
            Vector2 endPosition)
        {
            Rect linkRectangle = new Rect(
                startPosition.x,
                startPosition.y,
                endPosition.x - startPosition.x,
                endPosition.y - startPosition.y
            );

            return !VisibleCanvasArea.Overlaps(
                linkRectangle,
                true
            );
        }

        public bool IsModuleClipped(
            [NotNull] CGModule module,
            int refreshHighlightSize)
        {
            Rect testedBoundaries = module.Properties.Dimensions.ScaleBy(refreshHighlightSize);
            return !VisibleCanvasArea.Contains(testedBoundaries.min)
                   && !VisibleCanvasArea.Overlaps(testedBoundaries);
        }

        #endregion

        public void Reset()
        {
            ScrollValue = scrollValue;
            previousScrollAnimationUpdateTime = null;
            previouslySelectedModules.Clear();
        }
    }
}