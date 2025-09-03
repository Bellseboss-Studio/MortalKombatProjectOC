// =====================================================================
// Copyright © 2013 ToolBuddy
// All rights reserved
// 
// http://www.toolbuddy.net
// =====================================================================

namespace FluffyUnderware.Curvy.Controllers
{
    public abstract partial class CurvyController
    {
        /// <summary>
        /// Movement method options
        /// </summary>
        public enum MoveModeEnum
        {
            /// <summary>
            /// Move by Percentage or TF (SplineController only)
            /// </summary>
            Relative = 0,

            /// <summary>
            /// Move by calculated distance
            /// </summary>
            AbsolutePrecise = 1
        }
    }
}