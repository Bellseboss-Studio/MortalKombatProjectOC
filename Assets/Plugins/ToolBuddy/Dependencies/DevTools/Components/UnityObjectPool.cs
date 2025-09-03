// =====================================================================
// Copyright 2013-2017 Fluffy Underware
// All rights reserved
// 
// http://www.fluffyunderware.com
// =====================================================================

using System;
using FluffyUnderware.DevTools.Extensions;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;

namespace FluffyUnderware.DevTools
{
    /// <summary>
    /// A pool for UnityEngine.Object instances
    /// </summary>
    /// <typeparam name="T">A UnityEngine.Object type</typeparam>
    public abstract class UnityObjectPool<T> : DTVersionedMonoBehaviour, IPool
        where T : UnityEngine.Object //this constraint is here so that the == null test in RetrievePoppedItem also checks if item was deleted or not
    {
        #region public properties

        public virtual PoolSettings Settings
        {
            get => m_Settings;

            [UsedImplicitly] [Obsolete("The setter will be made private. Rather than assigning a new Settings instance, modify the existing one")]  
            set
            {
                if (value == null)
                    throw new ArgumentNullException(nameof(value));

                m_Settings = value;
                m_Settings.Validate();
            }
        }

        [UsedImplicitly] [Obsolete("Use GetComponent<PoolManager>() instead")]
        public PoolManager Manager => GetComponent<PoolManager>();

        public int Count => pooledObjects.Count;

        public abstract string Identifier { get; set; }

#endregion


        #region public methods

        public virtual void Push(T item)
        {
            if (item == null)
                return;

            if (item is IPoolable poolable)
                poolable.OnBeforePush();

            GameObject itemGameObject = GetItemGameObject(item);
            if (Application.isPlaying)
            {
                pooledObjects.Add(item);
                ConfigurePushedGameObject(itemGameObject);
            }
            else
            {
                itemGameObject.Destroy(false, true);
            }

            if (Settings.Debug)
                LogMessage("Push " + item);
        }

        [NotNull]
        public virtual T Pop(Transform parent = null)
        {
            T item = RetrievedPoppedItem();

            GameObject itemGameObject = GetItemGameObject(item);
            ConfigurePoppedGameObject(itemGameObject, parent);

            if (item is IPoolable poolable)
                poolable.OnAfterPop();

            if (Settings.Debug)
                LogMessage("Pop " + item);

            return item;
        }

        public virtual void Clear()
        {
            if (Settings.Debug)
                LogMessage("Clear");
            for (int i = 0; i < Count; i++)
                DestroyObject(pooledObjects[i]);
            pooledObjects.Clear();

        }


        public void Update()
        {
            if (!Application.isPlaying)
                return;

            int maxAdjustmentsCount = GetAdjustmentsCount();
            AdjustItemsCount(Settings.MinimumCount, Settings.MaximumCount, maxAdjustmentsCount, Settings.Debug);
        }

        public new void Reset()
        {
            base.Reset();
            Settings.SetToDefault();
            InstantShit();
        }

        #endregion


        #region protected methods

        [NotNull]
        protected abstract T CreateObject();

        [NotNull]
        protected abstract GameObject GetItemGameObject([NotNull] T item);

        protected override void OnValidate()
        {
            base.OnValidate();
            Settings.Validate();
        }

        protected override void ResetOnEnable()
        {
            base.ResetOnEnable();
            ResetTimeRelatedFields();
        }

        protected void Initialize([NotNull] PoolSettings settings)
        {
#pragma warning disable CS0618
            Settings = settings;
#pragma warning restore CS0618

            ResetTimeRelatedFields();

            if (Settings.InitializeCountConstrained)
                InstantShit();
        }

        protected void ConfigureCreatedGameObject([NotNull] GameObject item, string itemName)
        {
            item.name = itemName;
            item.transform.parent = transform;
            if (Settings.AutoEnableDisable)
                item.SetActive(false);
        }

        #endregion


        #region private members

        [NotNull]
        //todo desing and maybe bug?: is this member needed. There is no guarantee that this list can be not in sync with the list of children of type T
        private readonly List<T> pooledObjects = new List<T>();

        [Inline]
        [SerializeField]
        [NotNull]
        private PoolSettings m_Settings = new PoolSettings();

        [UsedImplicitly]
        private void Start()
        {
            if (Settings.InitializeCountConstrained)
                InstantShit();
        }


        private void DestroyObject([CanBeNull] T item)
        {
            if (item == null)
                return;

            GetItemGameObject(item).Destroy(false, true);
        }

        [NotNull]
        private T RetrievedPoppedItem()
        {
            T item = null;

            //see comment in the class declaration, next to "where T : UnityEngine.Object"
            while (item == null && Count > 0)
            {
                item = pooledObjects[0];
                pooledObjects.RemoveAt(0);
            }

            if (item == null)
            {
                if (Settings.AutoCreate || (Application.isPlaying == false))
                {
                    item = CreateObject();

                    if (Settings.Debug)
                        LogMessage("Auto create item");
                }
                else
                    throw new InvalidOperationException($"[Curvy] Could not pop element of type {typeof(T)} from pool. This is because there are not enough elements in the pool, and AutoCreate is not set to true neither. The pool identifier is {Identifier}");
            }
            return item;
        }


        private void ConfigurePushedGameObject([NotNull] GameObject item)
        {
            item.hideFlags = (Settings.Debug)
                ? HideFlags.DontSave
                : HideFlags.HideAndDontSave;
            if (Settings.AutoEnableDisable)
                item.SetActive(false);

            item.transform.parent = transform;
        }

        private void ConfigurePoppedGameObject([NotNull] GameObject item, [CanBeNull] Transform parent)
        {
            item.transform.parent = parent;

            item.hideFlags = HideFlags.None;
            if (Settings.AutoEnableDisable)
                item.SetActive(true);
        }

        private void LogMessage(string message) => Debug.Log($"({Count} items) {message} [{Identifier}]");

        private void AdjustItemsCount(int minItemsCount, int maxItemsCount, int maxAdjustmentsCount, bool logOperations)
        {
            if (maxAdjustmentsCount < 0)
                throw new ArgumentOutOfRangeException(nameof(maxAdjustmentsCount));
            if (minItemsCount < 0)
                throw new ArgumentOutOfRangeException(nameof(minItemsCount));
            if (maxItemsCount < minItemsCount)
                throw new ArgumentOutOfRangeException(nameof(maxItemsCount));

            if (Count > maxItemsCount)
            {
                maxAdjustmentsCount = Mathf.Min(maxAdjustmentsCount, Count - maxItemsCount);
                while (maxAdjustmentsCount-- > 0)
                {
                    if (logOperations)
                        LogMessage("MaximumCount exceeded: Deleting item");
                    DestroyObject(pooledObjects[0]);
                    pooledObjects.RemoveAt(0);
                }
            }
            else if (Count < minItemsCount)
            {
                maxAdjustmentsCount = Mathf.Min(maxAdjustmentsCount, minItemsCount - Count);
                while (maxAdjustmentsCount-- > 0)
                {
                    if (logOperations)
                        LogMessage("Below MinimumCount: Adding item");
                    pooledObjects.Add(CreateObject());
                }
            }
        }

        private void InstantShit()
        {
            if (!Application.isPlaying)
                return;

            AdjustItemsCount(Settings.MinimumCount, Settings.MaximumCount, int.MaxValue, false);

            if (Settings.Debug)
                LogMessage("Instant adjustment");
        }

        #region Time related stuff

        private double lastProcessingTime;
        private double unprocessedDuration;

        /// <summary>
        /// Expressed in seconds of real time.
        /// </summary>
        private static double Now => DTTime.TimeSinceStartup;

        [UsedImplicitly] private void ResetTimeRelatedFields()
        {
            lastProcessingTime = Now;
            unprocessedDuration = 0;
        }

        private int GetAdjustmentsCount()
        {
            double durationToProcess = unprocessedDuration + (Now - lastProcessingTime);
            float adjustmentInterval = Settings.CountAdjustmentInterval;

            int maxAdjustmentsCount;
            if (adjustmentInterval > 0)
            {
                maxAdjustmentsCount = (int)Math.Floor(durationToProcess / adjustmentInterval);
                unprocessedDuration = durationToProcess % adjustmentInterval;
            }
            else
            {
                maxAdjustmentsCount = int.MaxValue;
                unprocessedDuration = 0;
            }

            lastProcessingTime = Now;
            return maxAdjustmentsCount;
        }

        #endregion

        #endregion
    }
}