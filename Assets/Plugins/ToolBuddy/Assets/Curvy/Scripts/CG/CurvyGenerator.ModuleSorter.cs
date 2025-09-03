// =====================================================================
// Copyright © 2013 ToolBuddy
// All rights reserved
// 
// http://www.toolbuddy.net
// =====================================================================

using System.Collections.Generic;
using System.Linq;
using FluffyUnderware.DevTools;
using JetBrains.Annotations;

namespace FluffyUnderware.Curvy.Generator
{
    public partial class CurvyGenerator
    {
        private class ModuleSorter
        {
            //todo design make circular reference impossible?
            [ItemNotNull]
            [NotNull]
            private readonly HashSet<CGModule> modulesWithCircularReferences = new HashSet<CGModule>();

            [NotNull]
            private readonly Dictionary<CGModule, int> modulesAncestorCount = new Dictionary<CGModule, int>();

            public bool SortingNeeded { get; set; } = true;

            public bool HasCircularReference(
                [NotNull] CGModule module)
                => modulesWithCircularReferences.Contains(module);

            /// <summary>
            /// <see cref="Sort"/>
            /// </summary>
            /// <param name="modules"></param>
            public void EnsureIsSorted(
                List<CGModule> modules)
            {
                if (!SortingNeeded)
                    return;

                //BUG this does not sort modules correctly
                Sort(
                    modules
                ); //This is supposed to sort a module in a way that for each module, all its input modules are set in the modules list (which defines the updating order) before the said module
                SortingNeeded = false;
            }

            /// <summary>
            /// Updates item order in the modules list, also modifies their sibling index
            /// </summary>
            /// <param name="modules"></param>
            private void Sort(
                [NotNull] List<CGModule> modules)
            {
                modulesWithCircularReferences.Clear();
                modulesAncestorCount.Clear();

                if (modules.Count == 0)
                    return;

                List<CGModule> unsortedModules = new List<CGModule>(modules);

                List<CGModule> noAncestorModules = new List<CGModule>();
                List<CGModule> noProcessingModules = new List<CGModule>();
                List<CGModule> sortedModules = new List<CGModule>(modules.Count);

                //ensure all modules have their LinkedSlots not null
                for (int i = 0; i < modules.Count; i++)
                    EnsureLinkedSlotsBuild(modules[i]);

                // initialize
                for (int moduleIndex = unsortedModules.Count - 1; moduleIndex >= 0; moduleIndex--)
                {
                    CGModule module = unsortedModules[moduleIndex];

                    int ancestorCount = module.Input
                        .Where(t => t.IsLinked)
                        .Sum(t => t.LinkedSlots.Count);

                    modulesAncestorCount[module] = ancestorCount;

                    if (module is INoProcessing)
                    {
                        noProcessingModules.Add(module);
                        unsortedModules.RemoveAt(moduleIndex);
                    }
                    else if (ancestorCount == 0)
                    {
                        noAncestorModules.Add(module);
                        unsortedModules.RemoveAt(moduleIndex);
                    }
                }

                noAncestorModules.Sort(
                    (
                        a,
                        b) => a.UniqueID.CompareTo(b.UniqueID)
                );

                // Sort
                int index = 0;
                while (noAncestorModules.Count > 0)
                {
                    // get a module without ancestors
                    CGModule noAncestorModule = noAncestorModules[0];
                    noAncestorModules.RemoveAt(0);

                    IEnumerable<CGModuleSlot> outputLinkedSlots =
                        noAncestorModule.Output.SelectMany(outputSlot => outputSlot.LinkedSlots);

                    List<CGModule> newModulesWithoutAncestors = new List<CGModule>();
                    foreach (CGModuleSlot linkedSlot in outputLinkedSlots)
                    {
                        CGModule linkedModule = linkedSlot.Module;

                        if (modulesAncestorCount[linkedModule] <= 0)
                        {
                            DTLog.LogError("[Curvy] Modules sorting encountered an unexpected error. Please raise a bug report.");
                            if (newModulesWithoutAncestors.Contains(linkedModule) == false)
                                newModulesWithoutAncestors.Add(linkedModule);
                            modulesAncestorCount[linkedModule] = 0;
                        }
                        else
                        {
                            int newAncestorCount = modulesAncestorCount[linkedModule] - 1;
                            if (newAncestorCount == 0)
                                newModulesWithoutAncestors.Add(linkedModule);
                            modulesAncestorCount[linkedModule] = newAncestorCount;
                        }
                    }

                    noAncestorModules.AddRange(newModulesWithoutAncestors);

                    for (int i = 0; i < newModulesWithoutAncestors.Count; i++)
                        unsortedModules.Remove(newModulesWithoutAncestors[i]);

                    sortedModules.Add(noAncestorModule);
                    noAncestorModule.transform.SetSiblingIndex(index++);
                }

                // These modules got errors!
                modulesWithCircularReferences.UnionWith(unsortedModules);

                modules.Clear();
                modules.AddRange(sortedModules);
                modules.AddRange(unsortedModules);
                modules.AddRange(noProcessingModules);
            }

            private static void EnsureLinkedSlotsBuild(
                [NotNull] CGModule cgModule)
            {
                //todo design bug: Linked Slots being lazy loaded creates issues. A rework of module links initialization is needed. Until then, this is a workaround.
                foreach (CGModuleInputSlot moduleSlot in cgModule.Input)
                {
                    int x = moduleSlot.LinkedSlots.Count;
                }

                foreach (CGModuleOutputSlot moduleSlot in cgModule.Output)
                {
                    int x = moduleSlot.LinkedSlots.Count;
                }
            }
        }
    }
}