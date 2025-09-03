// =====================================================================
// Copyright 2013-2017 Fluffy Underware
// All rights reserved
// 
// http://www.fluffyunderware.com
// =====================================================================

using System;
using System.Collections.Generic;
using System.Linq;
using FluffyUnderware.DevTools.Extensions;
using UnityEditor;
using UnityEngine;

namespace FluffyUnderware.DevToolsEditor
{
    public static class DTToolbar
    {
        private static Event _handleEvent;

        internal static void Initialize()
        {
            RecalcItemSize = true;
            _handleEvent = null;
            loadItems();
            DTSelection.OnSelectionChange -= OnSelectionChange;
            DTSelection.OnSelectionChange += OnSelectionChange;
            SceneView.duringSceneGui -= RenderToolbar;
            SceneView.duringSceneGui += RenderToolbar;
            EditorApplication.hierarchyWindowItemOnGUI -= onHierarchy;
            EditorApplication.hierarchyWindowItemOnGUI += onHierarchy;
            EditorApplication.update -= onUpdate;
            EditorApplication.update += onUpdate;
        }


        private static void loadItems()
        {
            foreach (DTProject prj in DT.Projects)
                prj.ToolbarItems.Clear();

            TypeCache.TypeCollection toolbarItemTypes = TypeCache.GetTypesDerivedFrom(typeof(DTToolbarItem));
            toolbarItemTypes.Intersect(TypeCache.GetTypesWithAttribute<ToolbarItemAttribute>())
                .ForEach(type => Activator.CreateInstance(type));

            foreach (DTProject prj in DT.Projects)
                prj.ToolbarItems.Sort();
        }

        private static void OnSelectionChange()
        {
            foreach (DTProject project in DT.Projects)
                foreach (DTToolbarItem item in project.ToolbarItems)
                    item.OnSelectionChange();

            RecalcItemSize = true;
        }

        private static void onHierarchy(
            int instanceID,
            Rect selectionRect)
        {
            if (Selection.instanceIDs.Contains(instanceID))
                _handleEvent = new Event(Event.current);
        }

        private static void onUpdate()
        {
            if (Event.current != null || _handleEvent != null)
            {
                DTSelection.MuteEvents = true;
                foreach (DTProject project in DT.Projects)
                    foreach (DTToolbarItem item in project.ToolbarItems)
                    {
                        if (_handleEvent != null && item.Visible && item.Enabled)
                            item.HandleEvents(_handleEvent);
                        if (Event.current != null && item.Visible && item.Enabled)
                            item.HandleEvents(Event.current);
                    }

                DTSelection.MuteEvents = false;
            }

            _handleEvent = null;
        }


        #region Rendering

        private static void RenderToolbar(
            SceneView view)
        {
            List<DTProject> projects = DT.Projects;
            projects.Sort();
            if (projects.Count != 1)
                throw new NotSupportedException("Multiple projects, or no projects, are no more supported");

            DTProject project = projects[0];
            List<DTToolbarItem> items = project.ToolbarItems;
            if (!items.Any(item => item.Visible))
                return;

            DTToolbarOrientation toolbarOrientation = project.ToolbarOrientation;
            DTToolbarMargins margins = project.ToolbarMargins;
 
            UpdateItemSize();

            Event ev = Event.current;

            Handles.BeginGUI();
            GUI.skin = null; // to ensure light-skin is used if set in preferences (or not Pro)

            Vector2 itemsDrawingAreaSize = GetItemsDrawingAreaSize(view);

            DTSelection.MuteEvents = true;

            Rect lastDrawnItemRectangle = RenderItems(
                items,
                itemsDrawingAreaSize,
                toolbarOrientation,
                margins,
                ev
            );

            DTSelection.MuteEvents = false;

            DTToolbarItem hoveredItem = items.Where(item => item.Visible && item.Enabled).FirstOrDefault(
                item => item.mItemRect.Contains(DTGUI.MousePosition)
            );

            RenderItemsClientArea(
                items,
                lastDrawnItemRectangle,
                ev
            );

            SetStatusBarText(hoveredItem);

            RenderStatusBar(
                toolbarOrientation,
                itemsDrawingAreaSize,
                lastDrawnItemRectangle.y
            );

            if (project.ShowMargins)
                ShowMarginsUI(margins,
                    itemsDrawingAreaSize
                );
            Handles.EndGUI();
        }

        #region Items

        public static bool RecalcItemSize = true;
        private static Vector2 itemSize;

        private static void UpdateItemSize()
        {
            // Get largest item for each side
            if (RecalcItemSize)
            {
                itemSize = GetMaxItemDimension();
                RecalcItemSize = false;
            }
        }

        private static Vector2 GetItemsDrawingAreaSize(
            SceneView view)
        {
            Vector2 drawingAreaSize;
            {
#if UNITY_2022_3_OR_NEWER
                drawingAreaSize = view.cameraViewport.size;
#else
                drawingAreaSize = view.position.size;
                drawingAreaSize.y -= 20; // Unity's toolbar height
#endif
            }
            return drawingAreaSize;
        }

        private static Rect RenderItems(
            List<DTToolbarItem> items,
            Vector2 drawingAreaSize,
            DTToolbarOrientation toolbarOrientation,
            DTToolbarMargins toolbarMargins,
            Event ev)
        {
            Vector2 currentPosition = new Vector2();
            Rect lastDrawnItemRectangle = new Rect();

            for (int i = 0; i < items.Count; i++)
            {
                if (items[i].Visible == false)
                    continue;

                currentPosition = GetItemPosition(
                    items,
                    i,
                    currentPosition,
                    drawingAreaSize,
                    toolbarOrientation,
                    toolbarMargins
                );

                lastDrawnItemRectangle = items[i].mItemRect = new Rect
                {
                    x = currentPosition.x,
                    y = currentPosition.y,
                    width = itemSize.x,
                    height = itemSize.y
                };


                if (items[i].Enabled)
                {
                    Handles.EndGUI();
                    EditorKeyBinding.BindingsEnabled = false;
                    items[i].OnSceneGUI();
                    EditorKeyBinding.BindingsEnabled = true;
                    Handles.BeginGUI();
                }

                GUI.enabled = items[i].Enabled;
                items[i].Render(items[i].mItemRect);
                GUI.enabled = true;

                if (ev != null
                    && items[i].Enabled
                    && (DTToolbarItem.FocusedItem == null || DTToolbarItem.FocusedItem == items[i]))
                    items[i].HandleEvents(ev);
            }

            return lastDrawnItemRectangle;
        }

        private static Vector2 GetItemPosition(
            List<DTToolbarItem> items,
            int itemIndex,
            Vector2 currentPosition,
            Vector2 drawingAreaSize,
            DTToolbarOrientation toolbarOrientation,
            DTToolbarMargins toolbarMargins)
        {
            currentPosition = itemIndex == 0
                ? GetInitialItemPosition(
                    drawingAreaSize,
                    toolbarOrientation,
                    toolbarMargins
                )
                : GetNextPosition(
                    currentPosition,
                    toolbarOrientation,
                    toolbarMargins,
                    items[itemIndex].Order - items[itemIndex - 1].Order >= 10
                );

            if (itemIndex != 0 || toolbarMargins.StartSpacing > 0)
                currentPosition = GetWrappedPositionIfNeeded(
                    currentPosition,
                    drawingAreaSize,
                    toolbarOrientation,
                    toolbarMargins
                );
            return currentPosition;
        }

        private static Vector2 GetInitialItemPosition(
            Vector2 itemsDrawingAreaSize,
            DTToolbarOrientation toolbarOrientation,
            DTToolbarMargins toolbarMargins)
        {
            Vector2 initialPosition;
            switch (toolbarOrientation)
            {
                case DTToolbarOrientation.Left:
                    initialPosition.x = toolbarMargins.LeftMargin;
                    initialPosition.y = toolbarMargins.TopMargin + toolbarMargins.StartSpacing;
                    break;
                case DTToolbarOrientation.Right:
                    initialPosition.x = itemsDrawingAreaSize.x - toolbarMargins.RightMargin - itemSize.x;
                    initialPosition.y = toolbarMargins.TopMargin + toolbarMargins.StartSpacing;
                    break;
                case DTToolbarOrientation.Top:
                    initialPosition.x = toolbarMargins.LeftMargin + toolbarMargins.StartSpacing;
                    initialPosition.y = toolbarMargins.TopMargin;
                    break;
                case DTToolbarOrientation.Bottom:
                    initialPosition.x = toolbarMargins.LeftMargin + toolbarMargins.StartSpacing;
                    initialPosition.y = itemsDrawingAreaSize.y - toolbarMargins.BottomMargin - itemSize.y;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            return initialPosition;
        }

        private static Vector2 GetNextPosition(
            Vector2 previousPosition,
            DTToolbarOrientation toolbarOrientation,
            DTToolbarMargins toolbarMargins,
            bool isStartingNewGroup)
        {
            float interGroupSpace = isStartingNewGroup
                ? toolbarMargins.GroupSpacing
                : 0;

            Vector2 nextPosition = new Vector2();

            switch (toolbarOrientation)
            {
                case DTToolbarOrientation.Left:
                case DTToolbarOrientation.Right:
                    nextPosition.x = previousPosition.x;
                    nextPosition.y = previousPosition.y + itemSize.y + toolbarMargins.ButtonSpacing + interGroupSpace;
                    break;
                case DTToolbarOrientation.Top:
                case DTToolbarOrientation.Bottom:
                    nextPosition.x = previousPosition.x + itemSize.x + toolbarMargins.ButtonSpacing + interGroupSpace;
                    nextPosition.y = previousPosition.y;
                    break;
            }

            return nextPosition;
        }

        private static Vector2 GetWrappedPositionIfNeeded(
            Vector2 preWrapPosition,
            Vector2 drawingAreaSize,
            DTToolbarOrientation toolbarOrientation,
            DTToolbarMargins toolbarMargins)
        {
            Vector2 nextPosition = new Vector2();

            switch (toolbarOrientation)
            {
                case DTToolbarOrientation.Left:
                case DTToolbarOrientation.Right:
                    if (preWrapPosition.y + itemSize.y
                        > drawingAreaSize.y - toolbarMargins.BottomMargin)
                    {
                        float xMovement = itemSize.x + toolbarMargins.WrapSpacing;

                        nextPosition.x = toolbarOrientation == DTToolbarOrientation.Left
                            ? preWrapPosition.x + xMovement
                            : preWrapPosition.x - xMovement;

                        nextPosition.y = toolbarMargins.TopMargin;
                    }
                    else
                        nextPosition = preWrapPosition;

                    break;
                case DTToolbarOrientation.Top:
                case DTToolbarOrientation.Bottom:
                    if (preWrapPosition.x + itemSize.x
                        > drawingAreaSize.x - toolbarMargins.RightMargin) //if should wrap
                    {
                        nextPosition.x = toolbarMargins.LeftMargin;

                        float yMovement = itemSize.y + toolbarMargins.WrapSpacing;

                        nextPosition.y = toolbarOrientation == DTToolbarOrientation.Top
                            ? preWrapPosition.y + yMovement
                            : preWrapPosition.y - yMovement;
                    }
                    else
                        nextPosition = preWrapPosition;

                    break;
            }

            return nextPosition;
        }

        /// <summary>
        /// For each toolbar orientation, find the item with the largest dimensions
        /// </summary>
        private static Vector2 GetMaxItemDimension()
        {
            Vector2 result = new Vector2();
            DTProject prj = DT.Projects[0];
            foreach (DTToolbarItem item in prj.ToolbarItems)
                if (item.Visible)
                    result = Vector2.Max(
                        result,
                        item.GetItemSize()
                    );
            return result;
        }

        private static void ShowMarginsUI(
            DTToolbarMargins margins,
            Vector2 itemsDrawingAreaSize)
        {
            EditorGUI.DrawRect(
                new Rect(
                    margins.LeftMargin,
                    margins.TopMargin,
                    itemsDrawingAreaSize.x - margins.LeftMargin - margins.RightMargin,
                    itemsDrawingAreaSize.y - margins.TopMargin - margins.BottomMargin
                ),
                new Color(0, 1, 0, 0.2f));

            EditorGUI.DrawRect(
                new Rect(
                    0,
                    0,
                    itemsDrawingAreaSize.x,
                    itemsDrawingAreaSize.y
                ),
                new Color(.5f, 0, 0, 0.2f));
        }

        #endregion

        #region Client Area

        private static Rect GetClientAreaRectangle(
            DTToolbarItem item,
            Rect lastDrawnItem)
        {
            DTToolbarMargins toolbarMargins = item.Project.ToolbarMargins;
            DTToolbarOrientation toolbarOrientation = item.Project.ToolbarOrientation;
            int areaMargin = toolbarMargins.ItemClientAreaSpacing;
            int interColumnRowSpace = toolbarMargins.WrapSpacing;

            Rect clientRect = new Rect();
            //the client area is next to the button, or further away if there are other buttons in the way (row/column wrapping)
            switch (toolbarOrientation)
            {
                case DTToolbarOrientation.Left:
                    clientRect.x = lastDrawnItem.y + itemSize.y > item.mItemRect.y
                        ? lastDrawnItem.x + itemSize.x + areaMargin
                        : (lastDrawnItem.x + itemSize.x + areaMargin) - (itemSize.x + interColumnRowSpace);
                    clientRect.y = item.mItemRect.y;
                    break;
                case DTToolbarOrientation.Right:
                    clientRect.x = lastDrawnItem.y + itemSize.y > item.mItemRect.y
                        ? lastDrawnItem.x - areaMargin
                        : (lastDrawnItem.x - areaMargin) + (itemSize.x + interColumnRowSpace);
                    clientRect.y = item.mItemRect.y;
                    break;
                case DTToolbarOrientation.Top:
                    clientRect.x = item.mItemRect.x;
                    clientRect.y = lastDrawnItem.x + itemSize.x > item.mItemRect.x
                        ? lastDrawnItem.y + itemSize.y + areaMargin
                        : (lastDrawnItem.y + itemSize.y + areaMargin) - (itemSize.y + interColumnRowSpace);
                    break;
                case DTToolbarOrientation.Bottom:
                    clientRect.x = item.mItemRect.x;
                    clientRect.y = lastDrawnItem.x + itemSize.x > item.mItemRect.x
                        ? lastDrawnItem.y - areaMargin
                        : (lastDrawnItem.y - areaMargin) + (itemSize.y + interColumnRowSpace);
                    break;
            }

            clientRect.width = item.mItemRect.width;
            clientRect.height = item.mItemRect.height;
            return clientRect;
        }

        private static void RenderItemsClientArea(
            List<DTToolbarItem> items,
            Rect lastDrawnItemRectangle,
            Event ev)
        {
            for (int i = 0; i < items.Count; i++)
            {
                DTToolbarItem item = items[i];

                if (!item.Visible || !item.ShowClientArea)
                    continue;

                Rect clientAreaRectangle = GetClientAreaRectangle(
                    item,
                    lastDrawnItemRectangle
                );

                if (!(clientAreaRectangle.width > 0) || !(clientAreaRectangle.height > 0))
                    continue;

                item.mBackgroundRects.Clear();
                EditorKeyBinding.BindingsEnabled = false;
                item.RenderClientArea(clientAreaRectangle);
                EditorKeyBinding.BindingsEnabled = true;
                if (DTGUI.IsClick)
                    foreach (Rect background in item.mBackgroundRects)
                        if (background.Contains(ev.mousePosition))
                            DTGUI.UseEvent(
                                item.GetHashCode(),
                                ev
                            );
            }
        }

        #endregion

        #region Status bar

        private static Rect GetStatusBarRect(
            DTToolbarOrientation toolbarOrientation,
            float drawingAreaBottom,
            float lastDrawnItemY)
        {
            Vector2 v = GUIUtility.GUIToScreenPoint(Vector2.zero);
            Rect r = SceneView.currentDrawingSceneView.position;
            // If SceneView is on another monitor, r.x doesn't start at 0, but GUIToScreenPoint gives the offset
            r.x -= v.x;
            r.y = toolbarOrientation == DTToolbarOrientation.Bottom
                ? lastDrawnItemY - 25
                : drawingAreaBottom - 25;
            r.height = 20;

            return r;
        }

        private static void RenderStatusBar(
            DTToolbarOrientation toolbarOrientation,
            Vector2 itemsDrawingAreaSize,
            float lastDrawnItemY)
        {
            Rect statusBarRect = GetStatusBarRect(
                toolbarOrientation,
                itemsDrawingAreaSize.y,
                lastDrawnItemY
            );

            DTToolbarItem._StatusBar.Render(
                statusBarRect,
                null,
                true
            );
        }

        private static void SetStatusBarText(
            DTToolbarItem hovering)
        {
            // Handle status bar info when hovering over an item
            if (hovering != null)
                DTToolbarItem._StatusBar.Set(
                    hovering.StatusBarInfo,
                    "Info"
                );
            else
                DTToolbarItem._StatusBar.Clear("Info");
        }

        #endregion

        #endregion

        internal static void SetRadioGroupState(
            DTToolbarRadioButton active)
        {
            active.Project.SetRadioGroupState(active);
        }
    }
}