// =====================================================================
// Copyright © 2013 ToolBuddy
// All rights reserved
// 
// http://www.toolbuddy.net
// =====================================================================

namespace FluffyUnderware.Curvy
{
    /// <summary>
    /// Position Mode 
    /// </summary>
    public enum CurvyPositionMode
    {
        /// <summary>
        /// Valid positions are from 0 (Start) to 1 (End)
        /// </summary>
        Relative = 0,

        /// <summary>
        /// Valid positions are from 0 (Start) to Length (End). Also know as Absolute.
        /// </summary>
        WorldUnits = 1
    }
}