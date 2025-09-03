// =====================================================================
// Copyright © 2013 ToolBuddy
// All rights reserved
// 
// http://www.toolbuddy.net
// =====================================================================

namespace FluffyUnderware.Curvy
{
    /// <summary>
    /// Bezier Handles editing modes for AdvSplines
    /// </summary>
    public enum CurvyAdvBezierModeEnum
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
        /// Combine both Handles of a segment
        /// </summary>
        Combine = 8
    }
}