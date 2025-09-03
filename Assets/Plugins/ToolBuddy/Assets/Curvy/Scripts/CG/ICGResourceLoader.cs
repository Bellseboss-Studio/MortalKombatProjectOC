// =====================================================================
// Copyright © 2013 ToolBuddy
// All rights reserved
// 
// http://www.toolbuddy.net
// =====================================================================

using JetBrains.Annotations;
using UnityEngine;

namespace FluffyUnderware.Curvy.Generator
{
    /// <summary>
    /// Resource Loader Interface
    /// </summary>
    public interface ICGResourceLoader
    {
        [NotNull]
        Component Create(CGModule cgModule, [NotNull] string context);

        void Destroy(CGModule cgModule, Component obj, [NotNull] string context, bool kill);
    }
}