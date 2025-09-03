// =====================================================================
// Copyright © 2013 ToolBuddy
// All rights reserved
// 
// http://www.toolbuddy.net
// =====================================================================

namespace FluffyUnderware.Curvy
{
    /// <summary>
    /// Determines Orientation mode
    /// </summary>
    public enum CurvyOrientation
    {
        /// <summary>
        /// Ignore rotation
        /// </summary>
        None = 0,

        /// <summary>
        /// Use the splines' tangent and up vectors to create a look rotation 
        /// </summary>
        Dynamic = 1,

        /// <summary>
        /// Interpolate between the Control Point's rotation
        /// </summary>
        Static = 2
    }
}