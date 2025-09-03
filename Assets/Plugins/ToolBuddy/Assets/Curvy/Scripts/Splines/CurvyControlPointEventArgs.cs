// =====================================================================
// Copyright © 2013 ToolBuddy
// All rights reserved
// 
// http://www.toolbuddy.net
// =====================================================================

using UnityEngine;

namespace FluffyUnderware.Curvy
{
    /// <summary>
    /// EventArgs used by CurvyControlPointEvent events
    /// </summary>
    public class CurvyControlPointEventArgs : CurvySplineEventArgs
    {
        /// <summary>
        /// Event Mode
        /// </summary>
        public enum ModeEnum
        {
            /// <summary>
            /// Send for events that are not related to control points adding or removal
            /// </summary>
            None,

            /// <summary>
            /// Send when a Control point is added before an existing one
            /// </summary>
            AddBefore,

            /// <summary>
            /// Send when a Control point is added after an existing one
            /// </summary>
            AddAfter,

            /// <summary>
            /// Send when a Control point is deleted
            /// </summary>
            Delete
        }

        /// <summary>
        /// Determines the action this event was raised for
        /// </summary>
        public readonly ModeEnum Mode;

        /// <summary>
        /// Related Control Point
        /// </summary>
        public readonly CurvySplineSegment ControlPoint;

        public CurvyControlPointEventArgs(MonoBehaviour sender, CurvySpline spline, CurvySplineSegment cp,
            ModeEnum mode = ModeEnum.None, object data = null) : base(
            sender,
            spline,
            data
        )
        {
            ControlPoint = cp;
            Mode = mode;
        }
    }
}