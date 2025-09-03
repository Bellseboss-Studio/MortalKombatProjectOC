// =====================================================================
// Copyright © 2013 ToolBuddy
// All rights reserved
// 
// http://www.toolbuddy.net
// =====================================================================

using System;
using JetBrains.Annotations;

namespace FluffyUnderware.Curvy
{
    /// <summary>
    /// CG Resource Collection Attribute
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class CGResourceCollectionManagerAttribute : CGResourceManagerAttribute
    {
        public bool ShowCount;

        public CGResourceCollectionManagerAttribute([NotNull] string resourceName)
            : base(resourceName) =>
            ReadOnly = true;
    }
}