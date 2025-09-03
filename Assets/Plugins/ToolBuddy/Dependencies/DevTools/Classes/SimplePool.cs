// =====================================================================
// Copyright 2013-2017 Fluffy Underware
// All rights reserved
// 
// http://www.fluffyunderware.com
// =====================================================================

using System;
using System.Collections.Generic;

namespace FluffyUnderware.DevTools
{
    internal class SimplePool<T> where T : new()
    {
        private readonly List<T> freeItemsBackfield;
#if ENABLE_IL2CPP == false
        private static readonly Func<T> OptimizedInstantiator = System.Linq.Expressions.Expression.Lambda<Func<T>>(
            System.Linq.Expressions.Expression.New(typeof(T))
        ).Compile();
#endif

        public SimplePool(int preCreatedElementsCount)
        {
            freeItemsBackfield = new List<T>();
            for (int i = 0; i < preCreatedElementsCount; i++)
                freeItemsBackfield.Add(
#if ENABLE_IL2CPP == false
                    OptimizedInstantiator.Invoke()
#else
                    new T()
#endif
                );
        }

        public T GetItem()
        {
            T item;
            if (freeItemsBackfield.Count == 0)
                item =
#if ENABLE_IL2CPP == false
                    OptimizedInstantiator.Invoke()
#else
                    new T()
#endif
                    ;
            else
            {
                int lastIndex = freeItemsBackfield.Count - 1;
                item = freeItemsBackfield[lastIndex];
                freeItemsBackfield.RemoveAt(lastIndex);
            }
            return item;
        }

        public void ReleaseItem(T item)
        {
            freeItemsBackfield.Add(item);
        }
    }
}