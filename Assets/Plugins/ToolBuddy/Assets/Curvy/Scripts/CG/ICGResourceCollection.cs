// =====================================================================
// Copyright © 2013 ToolBuddy
// All rights reserved
// 
// http://www.toolbuddy.net
// =====================================================================

using UnityEngine;

namespace FluffyUnderware.Curvy.Generator
{
    /// <summary>
    /// Resource Collection interface
    /// </summary>
    public interface ICGResourceCollection
    {
        int Count { get; }

        //todo optim: should return an ienumerable instead of array? Right now the implementors use lists internally, and always create new array in the getter.
        Component[] ItemsArray { get; }
    }
}