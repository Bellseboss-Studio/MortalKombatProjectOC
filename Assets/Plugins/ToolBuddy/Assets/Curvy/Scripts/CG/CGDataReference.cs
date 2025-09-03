// =====================================================================
// Copyright © 2013 ToolBuddy
// All rights reserved
// 
// http://www.toolbuddy.net
// =====================================================================

using System;
using UnityEngine;

namespace FluffyUnderware.Curvy.Generator
{
    /// <summary>
    /// Class referencing a particular module's output slot
    /// </summary>
    /// <remarks>When using, be sure to add the <see cref="CGDataReferenceSelectorAttribute"/> to the field</remarks>
    [Serializable]
    public class CGDataReference
    {
        [SerializeField]
        private CGModule m_Module;

        [SerializeField]
        private string m_SlotName;

        private CGModuleOutputSlot mSlot;

        public CGData[] Data => Slot != null
            ? Slot.Data
            : new CGData[0];

        public CGModuleOutputSlot Slot
        {
            get
            {
                if ((mSlot == null || mSlot.Module != m_Module || mSlot.Info == null || mSlot.Info.Name != m_SlotName)
                    && m_Module != null
                    && m_Module.Generator != null
                    && m_Module.Generator.IsInitialized
                    && !string.IsNullOrEmpty(m_SlotName))
                    mSlot = m_Module.GetOutputSlot(m_SlotName);

                return mSlot;
            }
        }

        public bool HasValue
        {
            get
            {
                CGModuleOutputSlot cgModuleOutputSlot = Slot;
                return cgModuleOutputSlot != null && cgModuleOutputSlot.Data.Length > 0;
            }
        }

        public bool IsEmpty => string.IsNullOrEmpty(SlotName);

        public CGModule Module => m_Module;

        public string SlotName => m_SlotName;

        public CGDataReference() { }

        public CGDataReference(CGModule module, string slotName) =>
            setINTERNAL(
                module,
                slotName
            );

        public CGDataReference(CurvyGenerator generator, string moduleName, string slotName) =>
            setINTERNAL(
                generator,
                moduleName,
                slotName
            );

        public void Clear() =>
            setINTERNAL(
                null,
                string.Empty
            );

        public T GetData<T>() where T : CGData
            => Data.Length == 0
                ? null
                : Data[0] as T;

        public T[] GetAllData<T>() where T : CGData
            => Data as T[];

        #region ### Privates & Internals ###

#if DOCUMENTATION___FORCE_IGNORE___CURVY == false

        public void setINTERNAL(CGModule module, string slotName)
        {
            m_Module = module;
            m_SlotName = slotName;
            mSlot = null;
        }

        public void setINTERNAL(CurvyGenerator generator, string moduleName, string slotName)
        {
            m_Module = generator.GetModule(
                moduleName,
                false
            );
            m_SlotName = slotName;
            mSlot = null;
        }

#endif

        #endregion
    }
}