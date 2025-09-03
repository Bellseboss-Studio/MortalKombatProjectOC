// =====================================================================
// Copyright © 2013 ToolBuddy
// All rights reserved
// 
// http://www.toolbuddy.net
// =====================================================================

using System;
using System.Collections.Generic;
using FluffyUnderware.DevTools;
using JetBrains.Annotations;
using UnityEngine;

namespace FluffyUnderware.Curvy.Generator
{
    /// <summary>
    /// Resource Helper class used by Curvy Generator
    /// </summary>
    public static class CGResourceHandler
    {
        private static readonly Dictionary<string, ICGResourceLoader> resourceLoadersCache =
            new Dictionary<string, ICGResourceLoader>();

        /// <summary>
        /// Add a resource loader to the list of loaders this resource handler uses
        /// </summary>
        /// <param name="resourceName">The name of the type of resources the loader handles</param>
        /// <param name="loader">The loader</param>
        public static void RegisterResourceLoader(string resourceName, ICGResourceLoader loader)
        {
            if (resourceLoadersCache.ContainsKey(resourceName))
            {
                DTLog.LogError(
                    $"[Curvy] Trying to register a loader for resource '{resourceName}' multiple times. Attempt is ignored."
                );
                return;
            }

            resourceLoadersCache[resourceName] = loader;
        }

        [NotNull]
        public static Component CreateResource(CGModule module, [NotNull] string resName, [NotNull] string context)
        {
            if (resourceLoadersCache.ContainsKey(resName))
            {
                ICGResourceLoader loader = resourceLoadersCache[resName];
                return loader.Create(
                    module,
                    context
                );
            }

            throw new InvalidOperationException(
                $"[Curvy] CGResourceHandler: Missing loader for resource '{resName}'. Make sure the loader registers itself using CGResourceHandler.RegisterResourceLoader"
            );
        }

        public static void DestroyResource(CGModule module, [NotNull] string resName, Component obj, [NotNull] string context,
            bool kill)
        {
            if (resourceLoadersCache.ContainsKey(resName))
            {
                ICGResourceLoader loader = resourceLoadersCache[resName];
                loader.Destroy(
                    module,
                    obj,
                    context,
                    kill
                );
            }
            else
                DTLog.LogError(
                    $"[Curvy] CGResourceHandler: Missing loader for resource '{resName}'. Make sure the loader registers itself using CGResourceHandler.RegisterResourceLoader",
                    module
                );
        }
    }
}