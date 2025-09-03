// =====================================================================
// Copyright © 2013 ToolBuddy
// All rights reserved
// 
// http://www.toolbuddy.net
// =====================================================================

#if CONTRACTS_FULL
using System.Diagnostics.Contracts;
#endif


namespace FluffyUnderware.Curvy.Generator
{
    /// <summary>
    /// Request Parameter base class
    /// </summary>
    public abstract class CGDataRequestParameter
    {
        public static implicit operator bool(CGDataRequestParameter a)
            => !ReferenceEquals(
                a,
                null
            );
    }
}