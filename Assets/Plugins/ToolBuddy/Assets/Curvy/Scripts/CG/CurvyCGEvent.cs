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
    /// Curvy Generator related event
    /// </summary>
    [Serializable]
    public class CurvyCGEvent : UnityEventEx<CurvyCGEventArgs> { }
}