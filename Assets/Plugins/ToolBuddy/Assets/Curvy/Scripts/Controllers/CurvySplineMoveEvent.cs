// =====================================================================
// Copyright © 2013 ToolBuddy
// All rights reserved
// 
// http://www.toolbuddy.net
// =====================================================================

using System;
using FluffyUnderware.DevTools;

namespace FluffyUnderware.Curvy.Controllers
{
    /// <summary>
    /// EventArgs used by spline controller movements
    /// </summary>
    [Serializable]
    public class CurvySplineMoveEvent : UnityEventEx<CurvySplineMoveEventArgs> { }
}