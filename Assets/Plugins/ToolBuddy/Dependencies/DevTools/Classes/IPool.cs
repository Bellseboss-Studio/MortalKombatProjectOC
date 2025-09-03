// =====================================================================
// Copyright 2013-2017 Fluffy Underware
// All rights reserved
// 
// http://www.fluffyunderware.com
// =====================================================================

using System;
using JetBrains.Annotations;

namespace FluffyUnderware.DevTools
{
    public interface IPool
    {
        [NotNull]
        string Identifier { get; set; }
        [NotNull]
        PoolSettings Settings { get; }
        [UsedImplicitly] [Obsolete]
        void Clear();
        [UsedImplicitly] [Obsolete]
        void Reset();
        /// <summary>
        /// Updated the number of pooled objects based on <see cref="Settings"/> and elapsed time
        /// </summary>
        [UsedImplicitly] [Obsolete]
        void Update();
        /// <summary>
        /// The number of pooled objects
        /// </summary>
        int Count { get; }
    }
}