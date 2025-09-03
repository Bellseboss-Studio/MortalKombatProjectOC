// =====================================================================
// Copyright © 2013 ToolBuddy
// All rights reserved
// 
// http://www.toolbuddy.net
// =====================================================================

using System;
using System.Collections.Generic;
using System.Globalization;
using JetBrains.Annotations;
using UnityEngine;

namespace FluffyUnderware.Curvy.Generator
{
    public abstract partial class CGModule
    {
        [UsedImplicitly]
        [Obsolete] //todo make editor only when made private?
        //todo remove reference to INTERNAL in documentation (coding guidelines)
        public void renameManagedResourcesINTERNAL()
        {
            foreach ((Component resourceManager, string resourceName) in resourceManagers)
            {
                if (resourceManager == null)
                    continue;
                if (resourceManager.transform.parent != transform)
                    continue;
                RenameResource(
                    resourceName,
                    resourceManager
                );
            }
        }

        private class ResourceNamer
        {
            private readonly CGModule cgModule;

            private readonly Dictionary<string, Dictionary<int, string>> resourcesNameCache =
                new Dictionary<string, Dictionary<int, string>>();

            public ResourceNamer(CGModule cgModule) =>
                this.cgModule = cgModule;

            public void ClearCache() =>
                resourcesNameCache.Clear();

            [NotNull]
            private string GetResourceName([NotNull] string resourceName, int index)
            {
                Dictionary<int, string> resourceDictionary;
                if (resourcesNameCache.TryGetValue(
                        resourceName,
                        out resourceDictionary
                    )
                    == false)
                    resourcesNameCache[resourceName] = resourceDictionary = new Dictionary<int, string>();

                string newName;
                if (resourceDictionary.TryGetValue(
                        index,
                        out newName
                    )
                    == false)
                    resourceDictionary[index] = newName = index > -1
                        ? string.Format(
                            CultureInfo.InvariantCulture,
                            "{0}_{1}_{2}{3:000}",
                            cgModule.ModuleName,
                            cgModule.identifier.StringID,
                            resourceName,
                            index
                        )
                        : string.Format(
                            CultureInfo.InvariantCulture,
                            "{0}_{1}_{2}",
                            cgModule.ModuleName,
                            cgModule.identifier.StringID,
                            resourceName
                        );

                return newName;
            }

            public void Rename([NotNull] string resourceName, [NotNull] Component resource, int index)
            {
                string newName = GetResourceName(
                    resourceName,
                    index
                );
                //This check is necessary because when CurvyGenerator.ForceFrequentUpdates is true, this bug happens
                //[FIXED] When a scene has input spline path or input spline shape module, renaming objects from the hierarchy or though the F2 shortcut does not work
                if (resource.name != newName)
                    resource.name = newName;
            }
        }
    }
}