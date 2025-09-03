// =====================================================================
// Copyright 2013-2017 Fluffy Underware
// All rights reserved
// 
// http://www.fluffyunderware.com
// =====================================================================

namespace FluffyUnderware.DevTools
{
    public interface IPoolable
    {
        /// <summary>
        /// Is called before the object is pushed into its associated pool
        /// </summary>
        void OnBeforePush();

        /// <summary>
        /// Is called after the object is popped from its associated pool
        /// </summary>
        void OnAfterPop();
    }
}