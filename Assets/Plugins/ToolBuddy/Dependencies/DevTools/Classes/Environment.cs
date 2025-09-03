// =====================================================================
// Copyright 2013-2022 ToolBuddy
// All rights reserved
// 
// http://www.toolbuddy.net
// =====================================================================

#if !UNITY_WSA && !UNITY_WEBGL
#define THREADING_SUPPORTED
#endif

using System.Runtime.CompilerServices;

namespace FluffyUnderware.DevTools
{
    
    public static class Environment
    {
        public static bool IsThreadingSupported
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
#if THREADING_SUPPORTED
                return true;
#else
                return false;
#endif
            }
        }
    }
}