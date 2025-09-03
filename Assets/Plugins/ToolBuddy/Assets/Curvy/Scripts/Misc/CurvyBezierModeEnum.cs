// =====================================================================
// Copyright © 2013 ToolBuddy
// All rights reserved
// 
// http://www.toolbuddy.net
// =====================================================================

using System;

namespace FluffyUnderware.Curvy
{
    /// <summary>
    /// Bezier Handles editing modes
    /// </summary>
    [Flags]
    public enum CurvyBezierModeEnum
    {
        /// <summary>
        /// Don't sync
        /// </summary>
        None = 0,

        /// <summary>
        /// Sync Direction
        /// </summary>
        Direction = 1,

        /// <summary>
        /// Sync Length
        /// </summary>
        Length = 2,

        /// <summary>
        /// Sync connected Control Points
        /// </summary>
        Connections = 4,

        /// <summary>
        /// Combine both Handles of a segment
        /// </summary>
        Combine = 8
    }
}