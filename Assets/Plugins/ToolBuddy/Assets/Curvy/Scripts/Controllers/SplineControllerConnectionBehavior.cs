// =====================================================================
// Copyright © 2013 ToolBuddy
// All rights reserved
// 
// http://www.toolbuddy.net
// =====================================================================

namespace FluffyUnderware.Curvy.Controllers
{
    /// <summary>
    /// Defines what spline a <see cref="SplineController"/> will use when reaching a <see cref="CurvyConnection"/>.
    /// </summary>
    public enum SplineControllerConnectionBehavior
    {
        /// <summary>
        /// Continue moving on the current spline, ignoring the connection.
        /// </summary>
        CurrentSpline,

        /// <summary>
        /// Move to the spline containing the Follow-Up if any. If none, continue moving on the current spline, ignoring the connection.
        /// </summary>
        FollowUpSpline,

        /// <summary>
        /// Move to the spline of a randomly selected control point from all the connected control points.
        /// </summary>
        RandomSpline,

        /// <summary>
        /// Move to the spline containing the Follow-Up if any. If none, move to the spline of a randomly selected control point from all the connected control points.
        /// </summary>
        FollowUpOtherwiseRandom,

        /// <summary>
        /// Use a custom defined selection logic
        /// </summary>
        Custom
    }
}