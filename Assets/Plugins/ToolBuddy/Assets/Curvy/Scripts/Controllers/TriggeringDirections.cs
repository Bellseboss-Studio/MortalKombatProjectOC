// =====================================================================
// Copyright © 2013 ToolBuddy
// All rights reserved
// 
// http://www.toolbuddy.net
// =====================================================================

namespace FluffyUnderware.Curvy.Controllers
{
    /// <summary>
    /// Defines what travel directions should trigger an event
    /// </summary>
    public enum TriggeringDirections
    {
        /// <summary>
        /// All directions
        /// </summary>
        All,

        /// <summary>
        /// Same direction as spline's tangent
        /// </summary>
        Forward,

        /// <summary>
        /// Opposite direction as spline's tangent
        /// </summary>
        Backward
    }
}