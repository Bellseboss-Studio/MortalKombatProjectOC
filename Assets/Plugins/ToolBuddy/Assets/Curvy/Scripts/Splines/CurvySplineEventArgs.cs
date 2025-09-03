// =====================================================================
// Copyright © 2013 ToolBuddy
// All rights reserved
// 
// http://www.toolbuddy.net
// =====================================================================

using UnityEngine;
using UnityEngine.Assertions;

namespace FluffyUnderware.Curvy
{
    /// <summary>
    /// EventArgs used by CurvySplineEvent events
    /// </summary>
    public class CurvySplineEventArgs : CurvyEventArgs
    {
        /// <summary>
        /// The related spline
        /// </summary>
        public readonly CurvySpline Spline;

        public CurvySplineEventArgs(MonoBehaviour sender, CurvySpline spline, object data = null) : base(
            sender,
            data
        )
        {
            Spline = spline;


#if CURVY_SANITY_CHECKS
            Assert.IsTrue(
                ReferenceEquals(
                    Spline,
                    null
                )
                == false
            );
#endif
        }
    }
}