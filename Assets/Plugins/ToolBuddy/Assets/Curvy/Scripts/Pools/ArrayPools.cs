// =====================================================================
// Copyright © 2013 ToolBuddy
// All rights reserved
// 
// http://www.toolbuddy.net
// =====================================================================

using FluffyUnderware.Curvy.Generator;
using ToolBuddy.Pooling;
using ToolBuddy.Pooling.Pools;
using UnityEngine;

namespace FluffyUnderware.Curvy.Pools
{
    /// <summary>
    /// A class which sole purpose is to centralize references to the various <see cref="ArrayPool{T}"/>s instances
    /// </summary>
    public static class ArrayPools
    {
        static ArrayPools()
        {
            Int32 = ArrayPoolsProvider.GetPool<int>();
            Single = ArrayPoolsProvider.GetPool<float>();
            Vector2 = ArrayPoolsProvider.GetPool<Vector2>();
            Vector3 = ArrayPoolsProvider.GetPool<Vector3>();
            Vector4 = ArrayPoolsProvider.GetPool<Vector4>();
            CGSpot = ArrayPoolsProvider.GetPool<CGSpot>();
        }

        /// <summary>
        /// Gets the reference to the unique <see cref="ArrayPool{T}"/> of said type
        /// </summary>
        public static ArrayPool<Vector2> Vector2 { get; }

        /// <summary>
        /// Gets the reference to the unique <see cref="ArrayPool{T}"/> of said type
        /// </summary>
        public static ArrayPool<Vector3> Vector3 { get; }

        /// <summary>
        /// Gets the reference to the unique <see cref="ArrayPool{T}"/> of said type
        /// </summary>
        public static ArrayPool<Vector4> Vector4 { get; }

        /// <summary>
        /// Gets the reference to the unique <see cref="ArrayPool{T}"/> of said type
        /// </summary>
        public static ArrayPool<int> Int32 { get; }

        /// <summary>
        /// Gets the reference to the unique <see cref="ArrayPool{T}"/> of said type
        /// </summary>
        public static ArrayPool<float> Single { get; }

        /// <summary>
        /// Gets the reference to the unique <see cref="ArrayPool{T}"/> of said type
        /// </summary>
        public static ArrayPool<CGSpot> CGSpot { get; }
    }
}