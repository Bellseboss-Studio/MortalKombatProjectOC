// =====================================================================
// Copyright © 2013 ToolBuddy
// All rights reserved
// 
// http://www.toolbuddy.net
// =====================================================================

using System;
using System.Collections.Generic;
using System.Reflection;
using FluffyUnderware.DevTools;
using FluffyUnderware.DevTools.Extensions;
using JetBrains.Annotations;

namespace FluffyUnderware.Curvy.Generator
{
    partial class CGModule
    {
        [UsedImplicitly]
        [Obsolete]
        public void ReInitializeLinkedSlots() => slots.ReInitializeLinkedSlots();

        [UsedImplicitly]
        [Obsolete("Will be removed. Copy the method's implementation if needed")]
        public List<CGModuleInputSlot> GetInputSlots(Type filterType = null)
        {
            if (filterType == null)
                return new List<CGModuleInputSlot>(Input);
            List<CGModuleInputSlot> res = new List<CGModuleInputSlot>();
            for (int i = 0; i < Output.Count; i++)
                if (Output[i].Info.DataType == filterType || Output[i].Info.DataType.IsSubclassOf(filterType))
                    res.Add(Input[i]);

            return res;
        }

        [UsedImplicitly]
        [Obsolete("Will be removed. Copy the method's implementation if needed")]
        public List<CGModuleOutputSlot> GetOutputSlots(Type filterType = null)
        {
            if (filterType == null)
                return new List<CGModuleOutputSlot>(Output);
            List<CGModuleOutputSlot> res = new List<CGModuleOutputSlot>();
            for (int i = 0; i < Output.Count; i++)
                if (Output[i].Info.DataType == filterType || Output[i].Info.DataType.IsSubclassOf(filterType))
                    res.Add(Output[i]);

            return res;
        }

        private class Slots
        {
            [NotNull]
            private readonly CGModule module;

            [NotNull]
            public Dictionary<string, CGModuleInputSlot> InputSlotsByName { get; } = new Dictionary<string, CGModuleInputSlot>();

            [NotNull]
            public Dictionary<string, CGModuleOutputSlot> OutputSlotsByName { get; } =
                new Dictionary<string, CGModuleOutputSlot>();

            [NotNull] public List<CGModuleInputSlot> InputSlots { get; } = new List<CGModuleInputSlot>();
            [NotNull] public List<CGModuleOutputSlot> OutputSlots { get; } = new List<CGModuleOutputSlot>();

            public bool IsConfigured
            {
                get
                {
                    int validTotalLinks = 0;
                    foreach (CGModuleInputSlot inputSlot in InputSlots)
                    {
                        InputSlotInfo myInfo = inputSlot.InputInfo;
                        if (inputSlot.IsLinked)
                        {
                            for (int linkIndex = 0; linkIndex < inputSlot.Count; linkIndex++)
                                if (inputSlot.SourceSlot(linkIndex) != null)
                                {
                                    if (inputSlot.SourceSlot(linkIndex).Module.IsConfigured)
                                        validTotalLinks++;
                                    else if (!myInfo.Optional)
                                        return false;
                                }
                        }
                        else if (myInfo == null || !myInfo.Optional)
                            return false;
                    }

                    return validTotalLinks > 0 || InputSlots.Count == 0;
                }
            }


            #region Construction

            public Slots([NotNull] CGModule module)
            {
                this.module = module;
                Setup();
            }

            private void Setup()
            {
                Type type = module.GetType();
                FieldInfo[] fields = type.GetAllFields();
                foreach (FieldInfo fieldInfo in fields)
                {
                    CGModuleSlot slot = GetSlot(fieldInfo);
                    if (slot == null)
                        continue;
                    slot.Module = module;
                    slot.SetInfoFromField(fieldInfo);
                    Store(slot);
                }
            }

            [CanBeNull]
            private CGModuleSlot GetSlot([NotNull] FieldInfo fieldInfo)
            {
                CGModuleSlot slot;
                if (fieldInfo.FieldType == typeof(CGModuleInputSlot))
                {
                    CGModuleInputSlot inputSlot = (CGModuleInputSlot)fieldInfo.GetValue(module);
                    slot = inputSlot;
                }
                else if (fieldInfo.FieldType == typeof(CGModuleOutputSlot))
                {
                    CGModuleOutputSlot outputSlot = (CGModuleOutputSlot)fieldInfo.GetValue(module);
                    slot = outputSlot;
                }
                else
                    slot = null;

                return slot;
            }

            private void Store([NotNull] CGModuleSlot slot)
            {
                switch (slot)
                {
                    case CGModuleInputSlot inputSlot:
                        InputSlotsByName.Add(
                            inputSlot.Info.Name,
                            inputSlot
                        );
                        InputSlots.Add(inputSlot);
                        break;
                    case CGModuleOutputSlot outputSlot:
                        OutputSlotsByName.Add(
                            outputSlot.Info.Name,
                            outputSlot
                        );
                        OutputSlots.Add(outputSlot);
                        break;
                }
            }

            #endregion

            #region resetting/clearing

            #region Liked slots reset

            public void ReinitializeLinkedModulesLinkedSlots()
            {
                foreach (CGModuleInputSlot slot in InputSlots)
                    ReinitializeLinkedModulesLinkedSlots(slot);

                foreach (CGModuleOutputSlot slot in OutputSlots)
                    ReinitializeLinkedModulesLinkedSlots(slot);
            }

            private static void ReinitializeLinkedModulesLinkedSlots([NotNull] CGModuleSlot slot)
            {
                List<CGModule> linkedModules = slot.GetLinkedModules();
                for (int index = 0; index < linkedModules.Count; index++)
                {
                    CGModule module = linkedModules[index];
                    if (module != null)
                        module.slots.ReInitializeLinkedSlots();
                }
            }

            public void ReInitializeLinkedSlots()
            {
                foreach (CGModuleInputSlot s in InputSlots)
                    s.ReInitializeLinkedSlots();

                foreach (CGModuleOutputSlot s in OutputSlots)
                    s.ReInitializeLinkedSlots();
            }

            #endregion

#if DOCUMENTATION___FORCE_IGNORE___CURVY == false
            [System.Diagnostics.Conditional(CompilationSymbols.UnityEditor)]
            public void ResetInputSlotsLastDataCount()
            {
#if UNITY_EDITOR
                foreach (CGModuleInputSlot slot in InputSlots)
                    slot.ResetLastDataCount();
#endif
            }
#endif
            public void ResetLasRequestedParameters()
            {
                foreach (CGModuleOutputSlot s in OutputSlots)
                    s.LastRequestParameters = null;
            }

            public void ClearOutputData()
            {
                foreach (CGModuleOutputSlot s in OutputSlots)
                    s.ClearData();
            }

            #endregion

            #region Get slots by name

            public CGModuleInputSlot GetInputSlot(string name) => InputSlotsByName.ContainsKey(name)
                ? InputSlotsByName[name]
                : null;

            public CGModuleOutputSlot GetOutputSlot(string name) => OutputSlotsByName.ContainsKey(name)
                ? OutputSlotsByName[name]
                : null;

            #endregion

            public void CheckInputModulesNotDirty()
            {
                foreach (CGModuleInputSlot inputSlot in InputSlots)
                {
                    foreach (CGModuleSlot linkedSlot in inputSlot.LinkedSlots)
                        if (linkedSlot.Module.IsConfigured && linkedSlot.Module.Dirty)
                            DTLog.LogError(
                                $"[Curvy] Getting data from a dirty module. This shouldn't happen at all. Please raise a bug report. Source module is {linkedSlot.Module}",
                                module
                            );
                }
            }
        }
    }
}