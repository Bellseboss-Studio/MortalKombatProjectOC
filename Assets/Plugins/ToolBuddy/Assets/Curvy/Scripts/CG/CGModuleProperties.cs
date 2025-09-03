// =====================================================================
// Copyright © 2013 ToolBuddy
// All rights reserved
// 
// http://www.toolbuddy.net
// =====================================================================

using System;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor.AnimatedValues;
#endif

namespace FluffyUnderware.Curvy.Generator
{
    /// <summary>
    /// CGModule helper class
    /// </summary>
    [Serializable]
    public class CGModuleProperties
    {
        public Rect Dimensions;
#if UNITY_EDITOR
        public AnimBool Expanded;
#endif
        public float MinWidth = 250;
        public float LabelWidth;
        public Color BackgroundColor = Color.black;

        public CGModuleProperties()
        {
#if UNITY_EDITOR
            Expanded = new AnimBool(true);
            Expanded.speed = 3;
#endif
        }
    }
}