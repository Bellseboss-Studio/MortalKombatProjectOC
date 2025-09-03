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
using FluffyUnderware.DevTools.Extensions;
using JetBrains.Annotations;
using UnityEditor;
using UnityEngine;

namespace FluffyUnderware.CurvyEditor.Generator
{
    public class SelectionRectangle
    {
        /// <summary>
        /// Starting position of selection drag in canvas coordinates.
        /// Is null if no selection drag is in progress.
        /// </summary>
        private Vector2? DragStart { get; set; }

        public bool IsDragging => DragStart.HasValue;

        public void Reset() =>
            DragStart = null;

        public void Draw(
            Vector2 canvasMousePosition)
        {
            if (IsDragging == false)
                return;

            Vector2 startPosition = DragStart.Value;
            Vector2 endPosition = canvasMousePosition;

            Vector3[] verts = new Vector3[4]
            {
                new Vector3(
                    startPosition.x,
                    startPosition.y,
                    0
                ),
                new Vector3(
                    endPosition.x,
                    startPosition.y,
                    0
                ),
                new Vector3(
                    endPosition.x,
                    endPosition.y,
                    0
                ),
                new Vector3(
                    startPosition.x,
                    endPosition.y,
                    0
                )
            };
            Handles.DrawSolidRectangleWithOutline(
                verts,
                new Color(
                    .5f,
                    .5f,
                    .5f,
                    0.1f
                ),
                Color.white
            );
        }

        [UsedImplicitly]
        public void StartDrag(
            Vector2 canvasMousePosition)
        {
            if (IsDragging)
                throw new InvalidOperationException(
                    "Drag operation already in progress."
                );

            DragStart = canvasMousePosition;
        }

        public void EndDrag(
            Vector2 canvasMousePosition,
            List<CGModule> modules,
            CanvasSelection canvasSelection,
            bool isControlModifier,
            bool isShiftModifier)
        {
            if (IsDragging == false)
                throw new InvalidOperationException(
                    "No drag operation in progress."
                );

            Rect selectionRectangle = new Rect().SetBetween(
                DragStart.Value,
                canvasMousePosition
            );

            List<CGModule> modulesInRectangle = modules.Where(
                m => selectionRectangle.Overlaps(
                    m.Properties.Dimensions,
                    true
                )
            ).ToList();

            canvasSelection.OnSelectionRectangle(
                modulesInRectangle,
                isControlModifier,
                isShiftModifier
            );

            DragStart = null;
        }

        public void CancelDrag() =>
            DragStart = null;
    }
}