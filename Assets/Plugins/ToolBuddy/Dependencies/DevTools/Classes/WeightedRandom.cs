// =====================================================================
// Copyright 2013-2017 Fluffy Underware
// All rights reserved
// 
// http://www.fluffyunderware.com
// =====================================================================

using System.Collections.Generic;
using UnityEngine;

namespace FluffyUnderware.DevTools
{
    /// <summary>
    /// Simple but effective ShuffleBag implementation
    /// </summary>
    public class WeightedRandom<T>
    {
        private readonly List<T> mData;
        private int mCurrentPosition = -1;
        private T mCurrentItem;

        public int Seed { get; set; }

        public bool RandomizeSeed { get; set; }

        public int Size => mData.Count;

        public WeightedRandom(int initCapacity = 0)
        {
            mData = new List<T>(initCapacity);
        }

        public WeightedRandom(int initCapacity, int seed) : this(initCapacity)
        {
            Seed = seed;
        }

        /// <summary>
        /// Adds items to the bag
        /// </summary>
        /// <param name="item">the item</param>
        /// <param name="amount">number of times this item should be added</param>
        public void Add(T item, int amount)
        {
            for (int i = 0; i < amount; i++)
                mData.Add(item);
            mCurrentPosition = Size - 1;
        }

        /// <summary>
        /// Gets a random item from the bag
        /// </summary>
        /// <returns>an item</returns>
        public T Next()
        {
            if (mCurrentPosition < 1)
            {
                mCurrentPosition = Size - 1;
                mCurrentItem = mData[0];
                return mCurrentItem;
            }
            Random.State s = Random.state;
            if (RandomizeSeed)
                Seed = Random.Range(0, int.MaxValue);
            Random.InitState(Seed);
            int idx = Random.Range(0, mCurrentPosition);
            Random.state = s;

            mCurrentItem = mData[idx];
            mData[idx] = mData[mCurrentPosition];
            mData[mCurrentPosition] = mCurrentItem;
            mCurrentPosition--;
            return mCurrentItem;
        }

        /// <summary>
        /// Refill the bag
        /// </summary>
        public void Reset()
        {
            mCurrentPosition = Size - 1;
        }

        /// <summary>
        /// Clear the bag
        /// </summary>
        public void Clear()
        {
            mData.Clear();
            mCurrentPosition = -1;
        }

    }
}