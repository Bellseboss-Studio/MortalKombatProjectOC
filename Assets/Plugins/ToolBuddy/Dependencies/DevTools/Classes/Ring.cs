// =====================================================================
// Copyright 2013-2017 Fluffy Underware
// All rights reserved
// 
// http://www.fluffyunderware.com
// =====================================================================

using System.Collections;
using System.Collections.Generic;

namespace FluffyUnderware.DevTools
{
    public class Ring<T> : IList<T>
    {
        private readonly List<T> mList;
        public int Size { get; private set; }

        private int mIndex;

        public Ring(int size)
        {
            mList = new List<T>(size);
            Size = size;
        }

        public void Add(T item)
        {
            if (mList.Count == Size)
            {
                mList[mIndex++] = item;
                if (mIndex == mList.Count)
                    mIndex = 0;
            }
            else
                mList.Add(item);
        }

        public void Clear()
        {
            mList.Clear();
            mIndex = 0;
        }


        public int IndexOf(T item)
            => mList.IndexOf(item);

        public void Insert(int index, T item)
        {
            throw new System.NotSupportedException();
        }

        public void RemoveAt(int index)
        {
            throw new System.NotSupportedException();
        }

        public T this[int index]
        {
            get => mList[index];
            set => mList[index] = value;
        }

        public IEnumerator GetEnumerator()
            => mList.GetEnumerator();


        public bool Contains(T item)
            => mList.Contains(item);

        public void CopyTo(T[] array, int arrayIndex)
        {
            mList.CopyTo(array, arrayIndex);
        }

        public int Count => mList.Count;

        public bool IsReadOnly => throw new System.NotSupportedException();

        public bool Remove(T item)
            => mList.Remove(item);

        IEnumerator<T> IEnumerable<T>.GetEnumerator()
            => throw new System.NotImplementedException();
    }
}