// =====================================================================
// Copyright © 2013 ToolBuddy
// All rights reserved
// 
// http://www.toolbuddy.net
// =====================================================================

using System;
using JetBrains.Annotations;

namespace FluffyUnderware.Curvy.Generator
{
    /// <summary>
    /// Resource attribute
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    [UsedImplicitly]
    [Obsolete(
        "Curvy Splines does not use this attribute anymore. You have now to register your resource loaders using CGResourceHandler.RegisterResourceLoader"
    )]
    public sealed class ResourceLoaderAttribute : Attribute
    {
        public readonly string ResourceName;

        public ResourceLoaderAttribute([NotNull] string resName) =>
            ResourceName = resName;
    }
}