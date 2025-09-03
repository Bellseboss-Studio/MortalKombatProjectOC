// =====================================================================
// Copyright © 2013 ToolBuddy
// All rights reserved
// 
// http://www.toolbuddy.net
// =====================================================================

using System;
using UnityEngine;

namespace FluffyUnderware.Curvy.Generator
{
    /// <summary>
    /// Helper class used by various Curvy Generator modules
    /// </summary>
    [Serializable]
    public class CGMaterialSettings
    {
        public bool SwapUV = false;

        [Tooltip("Options to keep texel size proportional")]
        public CGKeepAspectMode KeepAspect = CGKeepAspectMode.Off;

        public float UVRotation = 0;
        public Vector2 UVOffset = Vector2.zero;
        public Vector2 UVScale = Vector2.one;
    }
}