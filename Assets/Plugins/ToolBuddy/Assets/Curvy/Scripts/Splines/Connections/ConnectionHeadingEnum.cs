// =====================================================================
// Copyright © 2013 ToolBuddy
// All rights reserved
// 
// http://www.toolbuddy.net
// =====================================================================

namespace FluffyUnderware.Curvy
{
    /// <summary>
    /// Connection's Follow-Up heading direction
    /// </summary>
    public enum ConnectionHeadingEnum
    {
        /// <summary>
        /// Head towards the targets start (negative F)
        /// </summary>
        Minus = -1,

        /// <summary>
        /// Do not head anywhere, stay still
        /// </summary>
        Sharp = 0,

        /// <summary>
        /// Head towards the targets end (positive F)
        /// </summary>
        Plus = 1,

        /// <summary>
        /// Automatically choose the appropriate value
        /// </summary>
        Auto = 2
    }
}