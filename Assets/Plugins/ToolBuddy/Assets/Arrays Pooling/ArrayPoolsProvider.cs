// =====================================================================
// Copyright © 2020 ToolBuddy
// All rights reserved
// 
// http://www.toolbuddy.net
// =====================================================================

using System;
using System.Collections.Generic;
using ToolBuddy.Pooling.Pools;

namespace ToolBuddy.Pooling
{
    /// <summary>
    /// Provides instances of <see cref="ArrayPool{T}"/>
    /// </summary>
    /// <remarks>Is thread safe</remarks>
    public class ArrayPoolsProvider
    {
        private static readonly Dictionary<Type, object> arrayPools = new Dictionary<Type, object>();
        private static readonly object lockObject = new object();

        /// <summary>
        /// Returns an instance of <see cref="ArrayPool{T}"/> if previously created, otherwise creates a new one.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        public static ArrayPool<T> GetPool<T>()
        {
            Type type = typeof(T);
            object pool;
            if (arrayPools.TryGetValue(
                    type,
                    out pool
                )
                == false)
                lock (lockObject)
                {
                    if (arrayPools.TryGetValue(
                            type,
                            out pool
                        )
                        == false)
                        arrayPools[type] = pool = new ArrayPool<T>(1_000_000);
                }

            return (ArrayPool<T>)pool;
        }
    }
}