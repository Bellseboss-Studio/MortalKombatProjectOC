// =====================================================================
// Copyright © 2013 ToolBuddy
// All rights reserved
// 
// http://www.toolbuddy.net
// =====================================================================

using FluffyUnderware.DevTools;

namespace FluffyUnderware.Curvy.Controllers
{
    /// <summary>
    /// A class used by <see cref="SplineController"/> to define custom selection logic to select between the possible connected splines when the controller reaches a <see cref="CurvyConnection"/>
    /// </summary>
    public abstract class ConnectedControlPointsSelector : DTVersionedMonoBehaviour
    {
        /// <summary>
        /// Select, from the current connection, a Control Point to continue moving through.
        /// </summary>
        /// <param name="caller">The spline controller that is calling this selector</param>
        /// <param name="connection">The connection the caller reached and from which it needs to select a Control Point to continue the movement on</param>
        /// <param name="currentControlPoint">the Control Point, part of the connection, the controller is at.</param>
        /// <returns>The control point that the <param name="caller"></param> should continue its movement on</returns>
        public abstract CurvySplineSegment SelectConnectedControlPoint(SplineController caller, CurvyConnection connection,
            CurvySplineSegment currentControlPoint);
    }
}