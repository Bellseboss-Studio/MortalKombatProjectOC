// =====================================================================
// Copyright © 2020 ToolBuddy
// All rights reserved
// 
// http://www.toolbuddy.net
// =====================================================================

namespace ToolBuddy.Pooling.Pools
{
    /// <summary>
    /// Data about how full an <see cref="ArrayPool{T}"/> is
    /// </summary>
    public readonly struct ArrayPoolUsageData
    {
        /// <summary>
        /// The number of elements that the pool will keep, after they have been freed, to be available for future usage.
        /// </summary>
        /// <remarks>This is not the maximal number of arrays, but the maximal sum of the arrays' lengths</remarks>
        public long ElementsCount { get; }

        /// <summary>
        /// The number of arrays stored in the pool
        /// </summary>
        public int ArraysCount { get; }

        /// <summary>
        /// The maximal number of elements that the pool will keep, after they have been freed, to be available for future usage.
        /// Once this limit is reached, every freed array will simply get ignored, allowing the garbage collector to collect it
        /// </summary>
        /// <remarks>This is not the maximal number of arrays, but the maximal sum of the arrays' lengths</remarks>
        public long ElementsCapacity { get; }


        public ArrayPoolUsageData(long elementsCount, int arraysCount, long elementsCapacity)
        {
            ElementsCount = elementsCount;
            ArraysCount = arraysCount;
            ElementsCapacity = elementsCapacity;
        }

        public bool Equals(ArrayPoolUsageData other)
            => ElementsCount == other.ElementsCount
               && ArraysCount == other.ArraysCount
               && ElementsCapacity == other.ElementsCapacity;

        public override bool Equals(object obj)
            => obj is ArrayPoolUsageData other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = ElementsCount.GetHashCode();
                hashCode = (hashCode * 397) ^ ArraysCount;
                hashCode = (hashCode * 397) ^ ElementsCapacity.GetHashCode();
                return hashCode;
            }
        }

        public static bool operator ==(ArrayPoolUsageData a, ArrayPoolUsageData b)
            => a.Equals(b);

        public static bool operator !=(ArrayPoolUsageData a, ArrayPoolUsageData b)
            => !(a == b);

        public override string ToString()
            =>
                $"{nameof(ElementsCount)}: {ElementsCount}, {nameof(ArraysCount)}: {ArraysCount}, {nameof(ElementsCapacity)}: {ElementsCapacity}";
    }
}