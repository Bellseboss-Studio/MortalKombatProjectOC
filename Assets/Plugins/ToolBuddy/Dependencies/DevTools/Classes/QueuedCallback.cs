// =====================================================================
// Copyright 2013-2017 Fluffy Underware
// All rights reserved
// 
// http://www.fluffyunderware.com
// =====================================================================

#if !UNITY_WSA && !UNITY_WEBGL
#define THREADING_SUPPORTED
#endif

namespace FluffyUnderware.DevTools
{
    internal class QueuedCallback
    {
#if THREADING_SUPPORTED
        public System.Threading.WaitCallback Callback;
#endif
        public object State;
    }
}
