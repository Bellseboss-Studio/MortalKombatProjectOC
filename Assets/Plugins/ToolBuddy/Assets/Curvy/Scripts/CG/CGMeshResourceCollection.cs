// =====================================================================
// Copyright © 2013 ToolBuddy
// All rights reserved
// 
// http://www.toolbuddy.net
// =====================================================================

using System;
using System.Collections.Generic;
using UnityEngine;

namespace FluffyUnderware.Curvy.Generator
{
    /// <summary>
    /// Collection of Mesh Resources
    /// </summary>
    [Serializable]
    public class CGMeshResourceCollection : ICGResourceCollection
    {
        public List<CGMeshResource> Items = new List<CGMeshResource>();

        public int Count => Items.Count;

        public Component[] ItemsArray => Items.ToArray();
    }
}