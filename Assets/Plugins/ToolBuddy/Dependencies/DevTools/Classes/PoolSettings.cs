// =====================================================================
// Copyright 2013-2017 Fluffy Underware
// All rights reserved
// 
// http://www.fluffyunderware.com
// =====================================================================

using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace FluffyUnderware.DevTools
{
    [Serializable]
    public class PoolSettings
    {
        [Header("General")]

        [SerializeField]
        [Label("Auto Create Items")]
        [Tooltip("Automatically create items when an item is requested and none is available")]
        private bool m_AutoCreate;

        [SerializeField]
        [Label("Auto Enable/Disable Items")]
        [Tooltip("Automatically disable objects when entering the pool and enable them when leaving it")]
        private bool m_AutoEnableDisable;

        [Label("Debug mode")]
        [Tooltip("Log operations and show pooled objects in the hierarchy")]
        public bool Debug;

        [Header("Item Count Constraints")]

        [Positive]
        [SerializeField]
        [FormerlySerializedAs("m_MinItems")]
        [Tooltip("Minimum number of items in the pool")]
        private int minimumCount;

        [Positive]
        [SerializeField]
        [FormerlySerializedAs("m_Threshold")]
        [Tooltip("Maximum number of items in the pool")]
        private int maximumCount;

        [Positive]
        [SerializeField]
        [FormerlySerializedAs("m_Speed")]
        [Label("Adjustment Interval")]
        [Tooltip(@"Number of seconds between item count adjustments.
If 0, adjustments are instantaneous.")]
        private float countAdjustmentInterval;

        [SerializeField]
        [FormerlySerializedAs("m_Prewarm")]
        [Label("Initialize Constrained")]
        [Tooltip("Initialize the pool with its item count already within the constraints")]
        private bool initializeCountConstrained;

        /// <summary>
        /// Initialize the pool with its item count already within the constraints
        /// </summary>
        public bool InitializeCountConstrained
        {
            get => initializeCountConstrained;
            set => initializeCountConstrained = value;
        }

        /// <summary>
        /// Automatically create items when an item is requested and none is available.
        /// If set to false, an exception is thrown when no item is available.
        /// </summary>
        public bool AutoCreate
        {
            get => m_AutoCreate;
            set => m_AutoCreate = value;
        }

        /// <summary>
        /// Automatically disable objects when entering the pool, and enable them when leaving it
        /// </summary>
        public bool AutoEnableDisable
        {
            get => m_AutoEnableDisable;
            set => m_AutoEnableDisable = value;
        }

        /// <summary>
        /// Minimum number of items in the pool
        /// </summary>
        public int MinimumCount
        {
            get => minimumCount;
            set => minimumCount = Mathf.Max(0, value);
        }

        /// <summary>
        /// Maximum number of items in the pool
        /// </summary>
        public int MaximumCount
        {
            get => maximumCount;
            set => maximumCount = Mathf.Max(MinimumCount, value);
        }

        /// <summary>
        /// Number of seconds (real time) between item count adjustments.
        /// If 0, adjustments are instantaneous.
        /// </summary>
        public float CountAdjustmentInterval
        {
            get => countAdjustmentInterval;
            set => countAdjustmentInterval = Mathf.Max(0, value);
        }

        #region Obsolete

        [JetBrains.Annotations.UsedImplicitly] [Obsolete("Use InitializeCountConstrained instead")]
        public bool Prewarm
        {
            get => InitializeCountConstrained;
            set => InitializeCountConstrained = value;
        }

        [JetBrains.Annotations.UsedImplicitly] [Obsolete("Use MinimumCount instead")]
        public int MinItems
        {
            get => MinimumCount;
            set => MinimumCount = value;
        }

        [JetBrains.Annotations.UsedImplicitly] [Obsolete("Use MaximumCount instead")]
        public int Threshold
        {
            get => MaximumCount;
            set => MaximumCount = value;
        }

        [JetBrains.Annotations.UsedImplicitly] [Obsolete("Use CountAdjustmentInterval instead")]
        public float Speed
        {
            get => CountAdjustmentInterval;
            set => CountAdjustmentInterval = value;
        }

        #endregion

        public PoolSettings()
        {
            SetToDefault();
        }

        public PoolSettings(PoolSettings src)
        {
            InitializeCountConstrained = src.InitializeCountConstrained;
            AutoCreate = src.AutoCreate;
            AutoEnableDisable = src.AutoEnableDisable;
            MinimumCount = src.MinimumCount;
            MaximumCount = src.MaximumCount;
            CountAdjustmentInterval = src.CountAdjustmentInterval;
            Debug = src.Debug;
        }

        /// <summary>
        /// Sets the settings to their default value
        /// </summary>
        public void SetToDefault()
        {
            MinimumCount = 0;
            MaximumCount = 50;
            CountAdjustmentInterval = 1;
            InitializeCountConstrained = true;
            AutoCreate = true;
            AutoEnableDisable = true;
            Debug = false;

            Validate();
        }


        [JetBrains.Annotations.UsedImplicitly] [Obsolete("Use Validate instead")]
        public void OnValidate()
        {
            Validate();
        }

        /// <summary>
        /// Validate the settings, by making sure they are within their allowed range
        /// </summary>
        public void Validate()
        {
            MinimumCount = minimumCount;
            MaximumCount = maximumCount;
            CountAdjustmentInterval = countAdjustmentInterval;
        }
    }
}