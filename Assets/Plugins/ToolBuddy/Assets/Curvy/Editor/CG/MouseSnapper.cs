// =====================================================================
// Copyright © 2013 ToolBuddy
// All rights reserved
// 
// http://www.toolbuddy.net
// =====================================================================

using System;
using UnityEngine;

namespace FluffyUnderware.CurvyEditor.Generator
{
    /// <summary>
    /// Provides functionality to accumulate mouse deltas and snap them based on a specified grid size.
    /// </summary>
    public class MouseSnapper
    {
        private Vector2 mouseDeltaAccumulator;

        /// <summary>
        /// Resets the accumulated mouse delta to zero.
        /// </summary>
        public void Reset()
        {
            mouseDeltaAccumulator = Vector2.zero;
        }

        /// <summary>
        /// Updates the accumulator with the given mouse delta and snaps the position to the specified grid size.
        /// </summary>
        /// <param name="position">The current position</param>
        /// <param name="mouseDelta">The mouse delta to accumulate.</param>
        /// <param name="snapGridSize">The grid size to snap the accumulated delta to.</param>
        /// <returns>The snapped delta, which may be applied to an object's position.</returns>
        public Vector2 GetSnappedPosition(
            Vector2 position,
            Vector2 mouseDelta,
            int snapGridSize)
        {
            if (snapGridSize <= 0)
                throw new ArgumentOutOfRangeException(
                    nameof(snapGridSize),
                    "Snap grid size must be positive."
                );

            mouseDeltaAccumulator += mouseDelta;

            Vector2 unsnappedPosition = new Vector2(
                position.x + mouseDeltaAccumulator.x,
                position.y + mouseDeltaAccumulator.y
            );

            Vector2 snappedPosition;
            snappedPosition.x = Mathf.RoundToInt(unsnappedPosition.x / snapGridSize) * snapGridSize;
            snappedPosition.y = Mathf.RoundToInt(unsnappedPosition.y / snapGridSize) * snapGridSize;

            mouseDeltaAccumulator = unsnappedPosition - snappedPosition;

            return snappedPosition;
        }
    }
}