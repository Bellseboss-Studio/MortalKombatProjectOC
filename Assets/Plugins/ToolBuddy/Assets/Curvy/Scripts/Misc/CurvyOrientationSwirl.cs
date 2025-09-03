// =====================================================================
// Copyright © 2013 ToolBuddy
// All rights reserved
// 
// http://www.toolbuddy.net
// =====================================================================

namespace FluffyUnderware.Curvy
{
    /// <summary>
    /// Swirl mode
    /// </summary>
    public enum CurvyOrientationSwirl
    {
        /// <summary>
        /// No Swirl
        /// </summary>
        None = 0,

        /// <summary>
        /// Swirl over each segment of anchor group
        /// </summary>
        Segment = 1,

        /// <summary>
        /// Swirl equal over current anchor group's segments
        /// </summary>
        AnchorGroup = 2,

        /// <summary>
        /// Swirl equal over anchor group's length
        /// </summary>
        AnchorGroupAbs = 3
    }
}