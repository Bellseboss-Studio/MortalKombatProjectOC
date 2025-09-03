// =====================================================================
// Copyright © 2013 ToolBuddy
// All rights reserved
// 
// http://www.toolbuddy.net
// =====================================================================

namespace FluffyUnderware.Curvy.Controllers
{
    /// <summary>
    /// Defines if the controller will move the object the same direction that the spline or the opposite one
    /// </summary>
    /// <seealso cref="MovementDirectionMethods"/>
    public enum MovementDirection
    {
        /// <summary>
        /// Same direction than spline's tangent
        /// </summary>
        Forward,

        /// <summary>
        /// Opposite direction than spline's tangent
        /// </summary>
        Backward
    }
}