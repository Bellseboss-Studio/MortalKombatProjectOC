// =====================================================================
// Copyright © 2013 ToolBuddy
// All rights reserved
// 
// http://www.toolbuddy.net
// =====================================================================

using FluffyUnderware.DevTools;
using UnityEngine;

namespace FluffyUnderware.Curvy.Generator.Modules
{
    /// <summary>
    /// Contains method used to validate the range of a rasterization
    /// </summary>
    public static class RasterizationRangeValidator
    {
        /// <summary>
        /// Makes sure the from value is in the range [0, 1[
        /// </summary>
        public static void ValidatedFromValue(
            ref float from) =>
            from = Mathf.Repeat(
                from,
                1
            );

        /// <summary>
        /// Makes sure the to value is in the range [from, 1] if path is open, or is in range [from, inf[ if path is closed
        /// </summary>
        public static void ValidateToValue(
            ref float to,
            float from,
            bool isPathOpen)
        {
            if (isPathOpen)
                to = DTMath.Repeat(
                    to,
                    1
                );

            to = Mathf.Max(
                from,
                to
            );
        }
    }
}