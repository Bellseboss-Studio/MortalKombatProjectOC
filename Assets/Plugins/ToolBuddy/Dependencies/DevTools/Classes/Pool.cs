// =====================================================================
// Copyright 2013-2017 Fluffy Underware
// All rights reserved
// 
// http://www.fluffyunderware.com
// =====================================================================

using System;
using System.Collections.Generic;
using UnityEngine;

namespace FluffyUnderware.DevTools
{
    //todo get rid of IPool once Pool<T> is removed?
    [JetBrains.Annotations.UsedImplicitly] [Obsolete]
    public class Pool<T> : IPool
    {
        private readonly List<T> mObjects = new List<T>();

        public string Identifier { get; set; }

        public PoolSettings Settings { get; protected set; }

        public Type Type => typeof(T);

        private double mLastTime;
        private double mDeltaTime;


        public Pool(PoolSettings settings = null)
        {
            Settings = settings ?? new PoolSettings();
            Identifier = typeof(T).FullName;
            mLastTime = DTTime.TimeSinceStartup + UnityEngine.Random.Range(0, Settings.CountAdjustmentInterval);
            if (Settings.InitializeCountConstrained)
                Reset();
        }

        public void Update()
        {
            mDeltaTime += DTTime.TimeSinceStartup - mLastTime;
            mLastTime = DTTime.TimeSinceStartup;

            if (Settings.CountAdjustmentInterval > 0)
            {
                int c = (int)(mDeltaTime / Settings.CountAdjustmentInterval);
                mDeltaTime -= c;

                if (Count > Settings.MaximumCount)
                {
                    c = Mathf.Min(c, Count - Settings.MaximumCount);
                    while (c-- > 0)
                    {
                        destroy(mObjects[0]);
                        mObjects.RemoveAt(0);
                        log("MaximumCount exceeded: Deleting item");
                    }
                }
                else if (Count < Settings.MinimumCount)
                {
                    c = Mathf.Min(c, Settings.MinimumCount - Count);
                    while (c-- > 0)
                    {
                        mObjects.Add(create());
                        log("Below MinimumCount: Adding item");
                    }
                }
            }
            else
                mDeltaTime = 0;
        }

        public void Reset()
        {
            if (Application.isPlaying)
            {
                while (Count < Settings.MinimumCount)
                {
                    mObjects.Add(create());
                }
                while (Count > Settings.MaximumCount)
                {
                    destroy(mObjects[0]);
                    mObjects.RemoveAt(0);
                }
                log("Prewarm/Reset");
            }
        }

        public void Clear()
        {
            log("Clear");
            for (int i = 0; i < Count; i++)
                destroy(mObjects[i]);
            mObjects.Clear();
        }

        public int Count => mObjects.Count;

        public virtual T Pop(Transform parent = null)
        {
            T item = default;
            if (Count > 0)
            {
                item = mObjects[0];
                mObjects.RemoveAt(0);

            }
            else
            {
                if (Settings.AutoCreate || !Application.isPlaying)
                {
                    log("Auto create item");
                    item = create();

                }
            }
            if (item != null)
            {
                sendAfterPop(item);
                setParent(item, parent);
                log("Pop " + item);
            }

            return item;
        }

        public virtual void Push(T item)
        {
            log("Push " + item);
            if (Application.isPlaying && item != null)
            {
                sendBeforePush(item);
                mObjects.Add(item);
            }
        }

        protected virtual void sendBeforePush(T item)
        {
            if (item is IPoolable poolable)
                poolable.OnBeforePush();
        }

        protected virtual void sendAfterPop(T item)
        {
            if (item is IPoolable poolable)
                poolable.OnAfterPop();
        }

        protected virtual void setParent(T item, Transform parent)
        {
        }

        protected virtual T create()
            => Activator.CreateInstance<T>();

        protected virtual void destroy(T item)
        {
        }

        private void log(string msg)
        {
            if (Settings.Debug)
                Debug.Log(string.Format("[{0}] ({1} items) {2}", Identifier, Count, msg));
        }
    }
}