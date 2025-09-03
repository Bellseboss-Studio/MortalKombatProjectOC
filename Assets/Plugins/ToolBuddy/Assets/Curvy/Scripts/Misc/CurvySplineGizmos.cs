// =====================================================================
// Copyright © 2013 ToolBuddy
// All rights reserved
// 
// http://www.toolbuddy.net
// =====================================================================

using System;

namespace FluffyUnderware.Curvy
{
    /// <summary>
    /// Sceneview viewing modes
    /// </summary>
    [Flags]
    public enum CurvySplineGizmos
    {
        None = 0,
        Connections = 1,
        Curve = 1 << 1,
        Approximation = 1 << 2,
        Tangents = 1 << 3,
        Orientation = 1 << 4,
        Labels = 1 << 5,
        Metadata = 1 << 6,
        Bounds = 1 << 7,
        TFs = 1 << 8,
        RelativeDistances = 1 << 9,
        OrientationAnchors = 1 << 10,
        All = 65535
    }
}