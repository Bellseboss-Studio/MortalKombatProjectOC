// =====================================================================
// Copyright © 2013 ToolBuddy
// All rights reserved
// 
// http://www.toolbuddy.net
// =====================================================================

using System;
using FluffyUnderware.DevTools;
#if CONTRACTS_FULL
using System.Diagnostics.Contracts;
#endif

namespace FluffyUnderware.Curvy
{
    /// <summary>
    /// Class used by spline related events
    /// </summary>
    [Serializable]
    public class CurvySplineEvent : UnityEventEx<CurvySplineEventArgs> { }
}