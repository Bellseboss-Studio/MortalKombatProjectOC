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
    /// Collection of GameObject resources
    /// </summary>
    [Serializable]
    public class CGGameObjectResourceCollection : ICGResourceCollection
    {
        public List<Transform> Items = new List<Transform>();
        public List<string> PoolNames = new List<string>();

        public int Count => Items.Count;

        public Component[] ItemsArray => Items.ToArray();
    }
}