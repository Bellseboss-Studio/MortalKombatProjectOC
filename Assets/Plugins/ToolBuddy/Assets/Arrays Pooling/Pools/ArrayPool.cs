// =====================================================================
// Copyright © 2020 ToolBuddy
// All rights reserved
// 
// http://www.toolbuddy.net
// =====================================================================

using System;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;
using ToolBuddy.Pooling.Collections;
using UnityEngine;
using Random = System.Random;

namespace ToolBuddy.Pooling.Pools
{
    /// <summary>
    /// A pool of allowing the re-usage of previously allocated and discarded arrays.
    /// Helps reducing garbage collection.
    /// </summary>
    /// <remarks>Is thread safe</remarks>
    /// <seealso cref="SubArray{T}"/>
    public class ArrayPool<T>
    {
        private readonly SubArray<T> emptySubArray = new SubArray<T>(new T[0]);

        private readonly Random random = new Random();

        private const int keysInitialCapacity = 200;

        //Optim: inserting and removing elements from those arrays leads to array copies. That takes a lot of time. Enhance this if needed
        private int[] poolKeys = new int[keysInitialCapacity];
        private T[][] poolValues = new T[keysInitialCapacity][];
        private int arraysCount;

        private long elementsCount;
        private long elementsCapacity;

        /// <summary>
        /// The maximal number of elements that the pool will keep, after they have been freed, to be available for future usage.
        /// Once this limit is reached, every freed array will simply get ignored, allowing the garbage collector to collect it
        /// </summary>
        /// <remarks>This is not the maximal number of arrays, but the maximal sum of the arrays' lengths</remarks>
        public long ElementsCapacity
        {
            get => elementsCapacity;
            set
            {
                if (elementsCapacity != value)
                    lock (this)
                    {
                        elementsCapacity = value;
                        ApplyCapacity(elementsCapacity);
                    }
            }
        }

        /// <summary>
        /// Log in the console each time a new array is allocated in memory
        /// </summary>
        public bool LogAllocations { get; set; }

        /// <summary>
        /// Returns data about the pool's usage.
        /// </summary>
        /// <see cref="ArrayPoolUsageData"/>
        public ArrayPoolUsageData UsageData =>
            new ArrayPoolUsageData(
                elementsCount,
                arraysCount,
                elementsCapacity
            );

        /// <summary>
        /// Creates a new pool
        /// </summary>
        /// <param name="elementsCapacity"><see cref="ElementsCapacity"/>
        /// </param>
        public ArrayPool(long elementsCapacity)
        {
            if (elementsCapacity < 0)
                throw new ArgumentOutOfRangeException(
                    nameof(elementsCapacity),
                    "Must be strictly positive."
                );

            this.elementsCapacity = elementsCapacity;
        }

        /// <summary>
        /// Allocates a new array if none available, or reuses an existing one otherwise
        /// </summary>
        /// <param name="minimalSize">The array's guaranteed minimal size</param>
        /// <param name="clearArray">Whether the returned array's elements will be guaranteed to be set to their default value</param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public SubArray<T> Allocate(int minimalSize, bool clearArray = true)
            //OPTIM set all calls that don't need a cleared array to clearArray == false
            => Allocate(
                minimalSize,
                false,
                clearArray,
                out _
            );

        /// <summary>
        /// Allocates a new array if none available, or reuses an existing one otherwise
        /// </summary>
        /// <param name="exactSize">The array's exact size</param>
        /// <param name="clearArray">Whether the returned array's elements will be guaranteed to be set to their default value</param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public SubArray<T> AllocateExactSize(int exactSize, bool clearArray = true)
            => Allocate(
                exactSize,
                true,
                clearArray,
                out _
            );


        /// <summary>
        /// Returns an array to the pool, ready to be reused
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Free(SubArray<T> subArray)
        {
            if (subArray.Array != null)
                Free(subArray.Array);
#if CURVY_SANITY_CHECKS
            subArray.IsDisposed = true;
#endif
        }

        /// <summary>
        /// Returns an array to the pool, ready to be reused
        /// </summary>
        public void Free([NotNull] T[] array)
        {
            if (array.Length > elementsCapacity || array.Length == 0)
                return;

            lock (this)
            {
#if CURVY_SANITY_CHECKS
                for (int i = 0; i < arraysCount; i++)
                    if (poolValues[i] == array)
                        throw new InvalidOperationException();
#endif

                ApplyCapacity(elementsCapacity - array.Length);

                int indexToInsertInto;
                {
                    int index = BinarySearch(
                        poolKeys,
                        arraysCount,
                        array.Length
                    );
                    indexToInsertInto = index >= 0
                        ? index
                        : ~index;
                }

                if (arraysCount == poolKeys.Length)
                {
                    //no overflow check. If you reach the point of overflowing, then you have way too much arrays
                    int newSize = 2 * (arraysCount + 1);
                    Array.Resize(
                        ref poolValues,
                        newSize
                    );
                    Array.Resize(
                        ref poolKeys,
                        newSize
                    );
                }

                if (indexToInsertInto < arraysCount)
                {
                    Array.Copy(
                        poolKeys,
                        indexToInsertInto,
                        poolKeys,
                        indexToInsertInto + 1,
                        arraysCount - indexToInsertInto
                    );
                    Array.Copy(
                        poolValues,
                        indexToInsertInto,
                        poolValues,
                        indexToInsertInto + 1,
                        arraysCount - indexToInsertInto
                    );
                }

                poolKeys[indexToInsertInto] = array.Length;
                poolValues[indexToInsertInto] = array;

                ++arraysCount;

                elementsCount += array.Length;
            }
        }

        /// <summary>
        /// Resizes the given array
        /// </summary>
        /// <param name="subArray"> The array to resize</param>
        /// <param name="newMinimalSize">The new size</param>
        /// <param name="clearNewSpace">When resizing an array to make it bigger, should the newly available space be cleared or not.</param>
        public void Resize(ref SubArray<T> subArray, int newMinimalSize, bool clearNewSpace = true)
        {
            //OPTIM set all calls that don't need a cleared new space to clearNewSpace == false
            //OPTIM use ResizeCopyless instead when possible
            if (subArray.Count == newMinimalSize)
                return;

            if (newMinimalSize < 0)
                throw new ArgumentOutOfRangeException(
                    nameof(newMinimalSize),
                    "Must be positive."
                );

            if (newMinimalSize == 0)
            {
                Free(subArray);
                subArray = emptySubArray;
            }
            else
            {
                int oldSize = subArray.Count;

                bool isArrayCleared;
                if (newMinimalSize > subArray.Array.Length)
                {
                    SubArray<T> destinationSubArray = Allocate(
                        newMinimalSize,
                        false,
                        false,
                        out isArrayCleared
                    );
                    Array.Copy(
                        subArray.Array,
                        0,
                        destinationSubArray.Array,
                        0,
                        subArray.Count
                    );
                    Free(subArray);
                    subArray = destinationSubArray;
                }
                else
                {
                    subArray = new SubArray<T>(
                        subArray.Array,
                        newMinimalSize
                    );
                    isArrayCleared = false;
                }

                if (clearNewSpace && isArrayCleared == false && newMinimalSize > oldSize)
                    Array.Clear(
                        subArray.Array,
                        oldSize,
                        newMinimalSize - oldSize
                    );
            }
        }

        /// <summary>
        /// Resize an array to a new size and clears it. Similar to calling <see cref="Free(ToolBuddy.Pooling.Collections.SubArray{T})"/> then calling <see cref="Allocate"/>, but done in a more optimized way
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ResizeAndClear(ref SubArray<T> subArray, int newMinimalSize)
        {
            if (subArray.Count == newMinimalSize)
            {
                Array.Clear(
                    subArray.Array,
                    0,
                    newMinimalSize
                );
                return;
            }

            if (newMinimalSize < 0)
                throw new ArgumentOutOfRangeException(
                    nameof(newMinimalSize),
                    "Must be positive."
                );

            if (newMinimalSize == 0)
            {
                Free(subArray);
                subArray = emptySubArray;
            }
            else
            {
                if (newMinimalSize > subArray.Array.Length)
                {
                    SubArray<T> destinationSubArray = Allocate(
                        newMinimalSize,
                        false,
                        true,
                        out _
                    );
                    Free(subArray);
                    subArray = destinationSubArray;
                }
                else
                {
                    subArray = new SubArray<T>(
                        subArray.Array,
                        newMinimalSize
                    );
                    Array.Clear(
                        subArray.Array,
                        0,
                        newMinimalSize
                    );
                }
            }
        }

        /// <summary>
        /// Resizes the array, without preserving the content of the array. You basically end up with an array with no guarantee on its content. This is faster than <see cref="Resize(ref SubArray{T}, int, bool)"/> because it doesn't need to copy the content of the array.
        /// </summary>
        /// <param name="subArray"></param>
        /// <param name="newMinimalSize"></param>
        public void ResizeCopyless(ref SubArray<T> subArray, int newMinimalSize)
        {
            if (subArray.Count == newMinimalSize)
                return;

            if (newMinimalSize < 0)
                throw new ArgumentOutOfRangeException(
                    nameof(newMinimalSize),
                    "Must be positive."
                );

            if (newMinimalSize == 0)
            {
                Free(subArray);
                subArray = emptySubArray;
            }
            else if (newMinimalSize > subArray.Array.Length)
            {
                Free(subArray);
                subArray = Allocate(
                    newMinimalSize,
                    false,
                    false,
                    out _
                );
            }
            else
                subArray = new SubArray<T>(
                    subArray.Array,
                    newMinimalSize
                );
        }

        /// <summary>
        /// Return a new <see cref="SubArray{T}"/> instance that will use a copy of the given input array
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public SubArray<T> Clone(T[] source)
        {
            SubArray<T> clone = Allocate(
                source.Length,
                false
            );
            Array.Copy(
                source,
                0,
                clone.Array,
                0,
                source.Length
            );

            return clone;
        }

        /// <summary>
        /// Return a new <see cref="SubArray{T}"/> instance that will use a copy of the given input array
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public SubArray<T> Clone(SubArray<T> source)
        {
            SubArray<T> clone = Allocate(
                source.Count,
                false
            );
            Array.Copy(
                source.Array,
                0,
                clone.Array,
                0,
                source.Count
            );

            return clone;
        }

        /// <summary>
        /// Allocates a new array if none available, or reuses an existing one otherwise
        /// </summary>
        /// <param name="size">The array's minimal or exact size, depending on <see cref="exactSize"/></param>
        /// <param name="exactSize">Whether the <see cref="size"/> parameter should be considered as an exact size or a minimal one</param>
        /// <param name="clearArray">Whether the returned array's elements will be guaranteed to be set to their default value</param>
        /// <param name="isArrayCleared">Whether the returned array is cleared. This is different from <see cref="clearArray"/> because even if <see cref="clearArray"/> is set to false, a newly created array will have its content cleared by definition</param>
        /// <returns></returns>
        private SubArray<T> Allocate(int size, bool exactSize, bool clearArray, out bool isArrayCleared)
        {
            if (size > elementsCapacity)
            {
                isArrayCleared = true;
                if (LogAllocations)
                    Debug.Log(
                        $"[ArrayPools] Type: {typeof(T).Name}. Allocated array size {size}. The requested size is bigger than the pool's capacity {elementsCapacity}"
                    );
                return new SubArray<T>(
                    new T[size],
                    size
                );
            }

            if (size == 0)
            {
                isArrayCleared = true;
                return emptySubArray;
            }

            if (size < 0)
                throw new ArgumentOutOfRangeException(
                    nameof(size),
                    "Must be positive."
                );

            lock (this)
            {
                int indexToRemoveFrom;
                {
                    int elementIndex = BinarySearch(
                        poolKeys,
                        arraysCount,
                        size
                    );

                    indexToRemoveFrom = elementIndex >= 0
                        ? elementIndex
                        : exactSize
                            ? arraysCount
                            : ~elementIndex;
                }

                T[] array;
                if (indexToRemoveFrom < arraysCount)
                {
                    array = RemoveElementAt(indexToRemoveFrom);
                    if (clearArray)
                        Array.Clear(
                            array,
                            0,
                            array.Length
                        );
                    isArrayCleared = clearArray;
                }
                else
                {
                    if (LogAllocations)
                        Debug.Log(
                            $"[ArrayPools] Type: {typeof(T).Name}. Allocated array size {size}. The size of the biggest array available is {(arraysCount == 0 ? "None" : poolKeys[arraysCount - 1].ToString())}"
                        );
                    array = new T[size];
                    isArrayCleared = true;
                }

                return new SubArray<T>(
                    array,
                    size
                );
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ApplyCapacity(long capacity)
        {
            while (elementsCount > capacity)
                RemoveElementAt(
                    random.Next(
                        0,
                        arraysCount
                    )
                );
        }

        private T[] RemoveElementAt(int elementIndex)
        {
            T[] array = poolValues[elementIndex];

            --arraysCount;
            if (elementIndex < arraysCount)
            {
                Array.Copy(
                    poolKeys,
                    elementIndex + 1,
                    poolKeys,
                    elementIndex,
                    arraysCount - elementIndex
                );
                Array.Copy(
                    poolValues,
                    elementIndex + 1,
                    poolValues,
                    elementIndex,
                    arraysCount - elementIndex
                );
            }

            elementsCount -= array.Length;
            return array;
        }

        /// <summary>
        /// <see cref="Array.BinarySearch(System.Array,int,int,object)"/>
        /// </summary>
        private static int BinarySearch(
            int[] array,
            int length,
            int value)
        {
            int num1 = 0;
            int num2 = length - 1;
            while (num1 <= num2)
            {
                int index1 = num1 + ((num2 - num1) >> 1);
                int num3 = array[index1] - value;
                if (num3 == 0)
                    return index1;
                if (num3 < 0)
                    num1 = index1 + 1;
                else
                    num2 = index1 - 1;
            }

            return ~num1;
        }
    }
}