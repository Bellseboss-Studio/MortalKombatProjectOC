// =====================================================================
// Copyright © 2013 ToolBuddy
// All rights reserved
// 
// http://www.toolbuddy.net
// =====================================================================

namespace FluffyUnderware.Curvy
{
    /// <summary>
    /// Used as a parameter for dirtying methods. Instructs if only the spline's orientation cache should be recomputed, or the whole approximations cache
    /// </summary>
    public enum SplineDirtyingType
    {
        /// <summary>
        /// Orientation approximations cache will be dirtied. Positions approximations cache will be ignored.
        /// </summary>
        OrientationOnly,

        /// <summary>
        /// Orientation approximations cache and positions approximations cache will be dirtied.
        /// </summary>
        Everything
    }
}