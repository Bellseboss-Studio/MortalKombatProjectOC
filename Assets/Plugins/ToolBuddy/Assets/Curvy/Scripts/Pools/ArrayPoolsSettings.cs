// =====================================================================
// Copyright © 2013 ToolBuddy
// All rights reserved
// 
// http://www.toolbuddy.net
// =====================================================================

using System;
using FluffyUnderware.DevTools;
using FluffyUnderware.DevTools.Extensions;
using JetBrains.Annotations;
using UnityEngine;

namespace FluffyUnderware.Curvy.Pools
{
    /// <summary>
    /// A component that allows setting, via the editor, the settings of the used <see cref="ToolBuddy.Pooling.Pools.ArrayPool{T}"/>s
    /// </summary>
    [HelpURL(DTUtility.HelpUrlBase + "arraypoolsettings")]
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public class ArrayPoolsSettings : DTVersionedMonoBehaviour
    {
        [SerializeField]
        [Tooltip("The maximal number of elements of type Vector2 allowed to be stored in the arrays' pool waiting to be reused")]
        private long vector2Capacity = 100_000;

        [SerializeField]
        [Tooltip("The maximal number of elements of type Vector3 allowed to be stored in the arrays' pool waiting to be reused")]
        private long vector3Capacity = 100_000;

        [SerializeField]
        [Tooltip("The maximal number of elements of type Vector4 allowed to be stored in the arrays' pool waiting to be reused")]
        private long vector4Capacity = 100_000;

        [SerializeField]
        [Tooltip("The maximal number of elements of type Int32 allowed to be stored in the arrays' pool waiting to be reused")]
        private long intCapacity = 100_000;

        [SerializeField]
        [Tooltip(
            "The maximal number of elements of type Single (a.k.a float) allowed to be stored in the arrays' pool waiting to be reused"
        )]
        private long floatCapacity = 10_000;

        [SerializeField]
        [Tooltip("The maximal number of elements of type CGSpots allowed to be stored in the arrays' pool waiting to be reused")]
        private long cgSpotCapacity = 10_000;

        [Tooltip("Log in the console each time an array pool allocates a new array in memory")]
        [SerializeField]
        private bool logAllocations;

        /// <summary>
        /// The maximal number of elements of type Vector2 allowed to be stored in the arrays' pool waiting to be reused
        /// </summary>
        public long Vector2Capacity
        {
            get => vector2Capacity;
            set
            {
                vector2Capacity = Math.Max(
                    0,
                    value
                );
                if (IsActiveAndEnabled) ArrayPools.Vector2.ElementsCapacity = vector2Capacity;
            }
        }

        /// <summary>
        /// The maximal number of elements of type Vector3 allowed to be stored in the arrays' pool waiting to be reused
        /// </summary>
        public long Vector3Capacity
        {
            get => vector3Capacity;
            set
            {
                vector3Capacity = Math.Max(
                    0,
                    value
                );
                if (IsActiveAndEnabled) ArrayPools.Vector3.ElementsCapacity = vector3Capacity;
            }
        }

        /// <summary>
        /// The maximal number of elements of type Vector4 allowed to be stored in the arrays' pool waiting to be reused
        /// </summary>
        public long Vector4Capacity
        {
            get => vector4Capacity;
            set
            {
                vector4Capacity = Math.Max(
                    0,
                    value
                );
                if (IsActiveAndEnabled) ArrayPools.Vector4.ElementsCapacity = vector4Capacity;
            }
        }

        /// <summary>
        /// The maximal number of elements of type Int32 allowed to be stored in the arrays' pool waiting to be reused
        /// </summary>
        public long IntCapacity
        {
            get => intCapacity;
            set
            {
                intCapacity = Math.Max(
                    0,
                    value
                );
                if (IsActiveAndEnabled) ArrayPools.Int32.ElementsCapacity = IntCapacity;
            }
        }

        /// <summary>
        /// The maximal number of elements of type Single allowed to be stored in the arrays' pool waiting to be reused
        /// </summary>
        public long FloatCapacity
        {
            get => floatCapacity;
            set
            {
                floatCapacity = Math.Max(
                    0,
                    value
                );
                if (IsActiveAndEnabled) ArrayPools.Single.ElementsCapacity = floatCapacity;
            }
        }

        /// <summary>
        /// The maximal number of elements of type CGSpot allowed to be stored in the arrays' pool waiting to be reused
        /// </summary>
        public long CGSpotCapacity
        {
            get => cgSpotCapacity;
            set
            {
                cgSpotCapacity = Math.Max(
                    0,
                    value
                );
                if (IsActiveAndEnabled) ArrayPools.CGSpot.ElementsCapacity = cgSpotCapacity;
            }
        }

        /// <summary>
        /// Log in the console each time an array pool allocates a new array in memory
        /// </summary>
        public bool LogAllocations
        {
            get => logAllocations;
            set
            {
                logAllocations = value;
                ArrayPools.CGSpot.LogAllocations = logAllocations;
                ArrayPools.Int32.LogAllocations = logAllocations;
                ArrayPools.Single.LogAllocations = logAllocations;
                ArrayPools.Vector2.LogAllocations = logAllocations;
                ArrayPools.Vector3.LogAllocations = logAllocations;
                ArrayPools.Vector4.LogAllocations = logAllocations;
            }
        }

        #region ### Unity Callbacks ###

#if DOCUMENTATION___FORCE_IGNORE___UNITY == false

        protected override void OnValidate()
        {
            base.OnValidate();

            ValidateAndApply();
        }

        protected override void OnEnable()
        {
            base.OnEnable();

#if UNITY_EDITOR

            if (ObjectExt.FindObjectsOfTypeInCurrentStage<ArrayPoolsSettings>(
                    false,
                    false
                ).Length > 1)
                DTLog.LogWarning(
                    "[Curvy] More than one instance of 'Array Pools Settings' detected. You should keep only one instance of this script.",
                    this
                );
#endif
            ValidateAndApply();
        }


        [UsedImplicitly]
        private void Start() => ValidateAndApply();

#endif

        #endregion


        private void ValidateAndApply()
        {
            Vector2Capacity = vector2Capacity;
            Vector3Capacity = vector3Capacity;
            Vector4Capacity = vector4Capacity;
            IntCapacity = intCapacity;
            FloatCapacity = floatCapacity;
            CGSpotCapacity = cgSpotCapacity;
            LogAllocations = logAllocations;
        }
    }
}