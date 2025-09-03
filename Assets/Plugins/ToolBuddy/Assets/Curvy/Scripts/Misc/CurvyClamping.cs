// =====================================================================
// Copyright © 2013 ToolBuddy
// All rights reserved
// 
// http://www.toolbuddy.net
// =====================================================================

namespace FluffyUnderware.Curvy
{
    /// <summary>
    /// Determines the clamping method used by Move-methods
    /// </summary>
    public enum CurvyClamping
    {
        /// <summary>
        /// Stop at splines ends
        /// </summary>
        Clamp = 0,

        /// <summary>
        /// Start over
        /// </summary>
        Loop = 1,

        /// <summary>
        /// Switch direction
        /// </summary>
        PingPong = 2
    }
}