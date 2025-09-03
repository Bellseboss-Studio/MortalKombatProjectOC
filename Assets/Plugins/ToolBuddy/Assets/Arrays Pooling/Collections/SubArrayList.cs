// =====================================================================
// Copyright © 2020 ToolBuddy
// All rights reserved
// 
// http://www.toolbuddy.net
// =====================================================================

using ToolBuddy.Pooling.Pools;

namespace ToolBuddy.Pooling.Collections
{
    /// <summary>
    /// A class that simulates very minimal features of a List, but using a <see cref="SubArray{T}"/> instead of an <see cref="System.Array"/> as a storage
    /// </summary>
    /// <seealso cref="SubArray{T}"/>
    public struct SubArrayList<T>
    {
        private readonly ArrayPool<T> typePool;
        private SubArray<T> subArray;

        /// <summary>
        /// The <see cref="System.Array"/> used by the underlying <see cref="SubArray{T}"/> for storage
        /// </summary>
        public T[] Array => subArray.Array;

        /// <summary>
        /// The number of elements occupied in the storage
        /// </summary>
        public int Count { get; private set; }

        /// <summary>
        /// Creates an instance
        /// </summary>
        /// <param name="initialCapacity">The initial capacity of the underlying storage</param>
        /// <param name="typePool"> A pool that will be used to, if needed, allocate a bigger array if elements </param>
        public SubArrayList(int initialCapacity, ArrayPool<T> typePool)
        {
            this.typePool = typePool;
            subArray = typePool.Allocate(
                initialCapacity,
                false
            );
            Count = 0;
        }

        /// <summary>
        /// Adds a new element to the array
        /// </summary>
        public void Add(T element)
        {
            if (Count == subArray.Count)
            {
                int newSize = subArray.Count == 0
                    ? 4
                    : subArray.Count * 2;
                typePool.Resize(
                    ref subArray,
                    newSize,
                    false
                );
            }

            subArray.Array[Count] = element;
            Count++;
        }

        /// <summary>
        /// Returns a <see cref="SubArray{T}"/> instance that will have <see cref="Array"/> as an array (not a copy of it), and <see cref="Count"/> as its <see cref="SubArray{T}.Count"/>
        /// </summary>
        /// <returns></returns>
        public SubArray<T> ToSubArray() =>
            new SubArray<T>(
                subArray.Array,
                Count
            );

        public bool Equals(SubArrayList<T> other)
            => subArray.Equals(other.subArray) && Count == other.Count;

        public override bool Equals(object obj)
            => obj is SubArrayList<T> other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                return (subArray.GetHashCode() * 397) ^ Count;
            }
        }

        public static bool operator ==(SubArrayList<T> a, SubArrayList<T> b)
            => a.Equals(b);

        public static bool operator !=(SubArrayList<T> a, SubArrayList<T> b)
            => !(a == b);
    }
}