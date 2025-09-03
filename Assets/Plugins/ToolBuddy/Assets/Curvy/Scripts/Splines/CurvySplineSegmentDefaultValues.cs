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
    /// Here you can find all the default values for CurvySplineSegment's serialized fields. If you don't find a field here, this means that it's type's default value is the same than the field's default value
    /// </summary>
    public static class CurvySplineSegmentDefaultValues
    {
        public const CurvyOrientationSwirl Swirl = CurvyOrientationSwirl.None;
        public const bool SynchronizeTCB = true;
        public const bool AutoHandles = true;
        public const float AutoHandleDistance = 0.39f;

        public static readonly Vector3 HandleIn = new Vector3(
            -1,
            0,
            0
        );

        public static readonly Vector3 HandleOut = new Vector3(
            1,
            0,
            0
        );
    }
}