// =====================================================================
// Copyright © 2013 ToolBuddy
// All rights reserved
// 
// http://www.toolbuddy.net
// =====================================================================

using System;
using FluffyUnderware.DevTools;
using UnityEngine;

namespace FluffyUnderware.Curvy.Generator
{
    /// <summary>
    /// An item that has a weight associated to it
    /// </summary>
    [Serializable]
    public class CGWeightedItem
    {
        [RangeEx(
            0,
            1,
            Slider = true,
            Precision = 1
        )]
        [SerializeField]
        private float m_Weight = 0.5f;

        public float Weight
        {
            get => m_Weight;
            set
            {
                float v = Mathf.Clamp01(value);
                if (m_Weight != v)
                    m_Weight = v;
            }
        }
    }
}