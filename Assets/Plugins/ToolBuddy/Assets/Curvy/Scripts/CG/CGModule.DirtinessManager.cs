// =====================================================================
// Copyright © 2013 ToolBuddy
// All rights reserved
// 
// http://www.toolbuddy.net
// =====================================================================

using System;
using System.Collections.Generic;
using JetBrains.Annotations;

namespace FluffyUnderware.Curvy.Generator
{
    public abstract partial class CGModule
    {
        private class DirtinessManager
        {
            [NotNull]
            private readonly CGModule module;

            private bool isDirty = true;
            private bool isStateChangeDirty;
            private bool lastIsConfiguredState;
            private static readonly Action<CGModule> SetDirtyAction = m => m.Dirty = true;

            private static readonly Action<CGModule> SetTreeDirtyStateChangeAction =
                m => m.dirtinessManager.SetTreeDirtyStateChange();

            public DirtinessManager([NotNull] CGModule module) =>
                this.module = module;

            public bool IsDirty
            {
                get => isDirty;
                set
                {
                    isDirty = value;

                    if (isDirty)
                    {
                        bool isConfigured = module.IsConfigured;
                        if (lastIsConfiguredState != isConfigured)
                            isStateChangeDirty = true;
                        lastIsConfiguredState = isConfigured;

                        ForEachValidOutputModule(SetDirtyAction);
                    }

                    //todo design: handle this outside of the setter, see comment of UnsetDirtyFlag
                    if (module is IOnRequestProcessing || module is INoProcessing)
                    {
                        isDirty = false;
                        module.slots.ResetLasRequestedParameters();
                    }
                }
            }

            //todo design: get rid of this, since there is already a IsDirty setter. The problem with this setter is that it does a lot of things, making the simple action of setting isDirty to false impossible
            public void UnsetDirtyFlag() =>
                isDirty = false;

            public void Reset()
            {
                isDirty = true;
                isStateChangeDirty = false;
                lastIsConfiguredState = false;
            }


            public void CheckOnStateChanged()
            {
                //            Debug.Log(ModuleName+".Check: " + mStateChangeDirty);
                if (isStateChangeDirty)
                    module.OnStateChange();
                isStateChangeDirty = false;
            }

            public void OnDestroy() => SetTreeDirtyStateChange();

            private void SetTreeDirtyStateChange()
            {
                isStateChangeDirty = true;
                ForEachValidOutputModule(SetTreeDirtyStateChangeAction);
            }

            private void ForEachValidOutputModule(Action<CGModule> action)
            {
                List<CGModuleOutputSlot> moduleOutputSlots = module.Output;

                for (int slotIndex = 0; slotIndex < moduleOutputSlots.Count; slotIndex++)
                {
                    CGModuleOutputSlot outputSlot = moduleOutputSlots[slotIndex];

                    if (!outputSlot.IsLinked)
                        continue;

                    List<CGModule> modules = outputSlot.GetLinkedModules();
                    for (int moduleIndex = 0; moduleIndex < modules.Count; moduleIndex++)
                    {
                        CGModule outputModule = modules[moduleIndex];

                        if (outputModule != module
                            // prevent circular reference
                            //BUG? does this create infinite dirtying logique?
                            || outputModule.Generator.HasCircularReference(outputModule))
                            action(outputModule);
                    }
                }
            }
        }
    }
}