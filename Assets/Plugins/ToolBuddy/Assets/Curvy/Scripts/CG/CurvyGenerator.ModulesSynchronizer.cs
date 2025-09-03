// =====================================================================
// Copyright © 2013 ToolBuddy
// All rights reserved
// 
// http://www.toolbuddy.net
// =====================================================================

using System.Diagnostics;
using JetBrains.Annotations;
using UnityEngine;

namespace FluffyUnderware.Curvy.Generator
{
    public partial class CurvyGenerator
    {
        /// <summary>
        /// Adds to a generators Modules list any modules that are missing from the generator's hierarchy
        /// </summary>
        private class ModulesSynchronizer
        {
            private bool hasPendingRequest;

            [Conditional(CompilationSymbols.UnityEditor)]
            public void RequestSynchronization()
                => hasPendingRequest = true;

            [Conditional(CompilationSymbols.UnityEditor)]
            public void CancelRequests()
                => hasPendingRequest = false;


            [Conditional(CompilationSymbols.UnityEditor)]
            public void ProcessRequests([NotNull] CurvyGenerator curvyGenerator)
            {
                if (hasPendingRequest)
                {
                    hasPendingRequest = false;
                    AddMissingChildModules(curvyGenerator);
                }
            }

            [Conditional(CompilationSymbols.UnityEditor)]
            private static void AddMissingChildModules([NotNull] CurvyGenerator curvyGenerator)
            {
                Transform cachedTransform = curvyGenerator.transform;
                for (int i = 0; i < cachedTransform.childCount; i++)
                {
                    CGModule childModule = cachedTransform.GetChild(i).GetComponent<CGModule>();
                    if (childModule == null)
                        continue;

                    if (curvyGenerator.Modules.Contains(childModule) == false)
                    {
                        childModule.InputLinks.Clear();
                        childModule.OutputLinks.Clear();
#pragma warning disable CS0612
                        childModule.ReInitializeLinkedSlots();
#pragma warning restore CS0612
                        curvyGenerator.AddModule(childModule);
                    }
                }
            }
        }
    }
}