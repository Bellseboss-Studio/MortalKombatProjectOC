// =====================================================================
// Copyright © 2013 ToolBuddy
// All rights reserved
// 
// http://www.toolbuddy.net
// =====================================================================

namespace FluffyUnderware.Curvy
{
    /// <summary>
    /// Determines the interpolation method
    /// </summary>
    public enum CurvyInterpolation
    {
        /// <summary>
        ///  Linear interpolation
        /// </summary>
        Linear = 0,

        /// <summary>
        /// Catmul-Rom splines
        /// </summary>
        CatmullRom = 1,

        /// <summary>
        /// Kochanek-Bartels (TCB)-Splines
        /// TCB stands for Tension, Continuity, Bias.
        /// Tension controls the amount of curvature in the spline.
        /// Continuity controls the smoothness of the spline.
        /// Bias controls the direction of the curvature.
        /// </summary>
        TCB = 2,

        /// <summary>
        /// Cubic Bezier-Splines
        /// </summary>
        Bezier = 3,

        /// <summary>
        /// B-Splines
        /// </summary>
        BSpline = 4
    }
}