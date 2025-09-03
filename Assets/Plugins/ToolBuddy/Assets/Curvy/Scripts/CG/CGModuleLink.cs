// =====================================================================
// Copyright © 2013 ToolBuddy
// All rights reserved
// 
// http://www.toolbuddy.net
// =====================================================================

using System;
using System.Globalization;
using JetBrains.Annotations;
using UnityEngine;

namespace FluffyUnderware.Curvy.Generator
{
    /// <summary>
    /// Class defining a linkage between two modules' slots
    /// </summary>
    [Serializable]
    public class CGModuleLink
    {
        // Self
        [SerializeField]
        private int m_ModuleID;

        [SerializeField]
        private string m_SlotName;

        // Other
        [SerializeField]
        private int m_TargetModuleID;

        [SerializeField]
        private string m_TargetSlotName;

        public int ModuleID => m_ModuleID;
        public string SlotName => m_SlotName;
        public int TargetModuleID => m_TargetModuleID;
        public string TargetSlotName => m_TargetSlotName;


        public CGModuleLink(int sourceID, string sourceSlotName, int targetID, string targetSlotName)
        {
            m_ModuleID = sourceID;
            m_SlotName = sourceSlotName;
            m_TargetModuleID = targetID;
            m_TargetSlotName = targetSlotName;
        }

        public CGModuleLink(CGModuleSlot source, CGModuleSlot target) : this(
            source.Module.UniqueID,
            source.Name,
            target.Module.UniqueID,
            target.Name
        ) { }

        /// <summary>
        /// Can input and output slots be linked together?
        /// </summary>
        /// <seealso cref="CGModuleSlot.IsOnRequest"/>
        /// <returns></returns>
        public static bool AreSlotsCompatible(
            [NotNull] InputSlotInfo inputSlotInfo,
            bool isInputDataOnRequest,
            [NotNull] OutputSlotInfo outputSlotInfo,
            bool isOutputDataOnRequest)
        {
            bool areDataTypesCompatible = inputSlotInfo.IsValidFrom(outputSlotInfo.DataType);

            bool areDataRequestingTypesCompatible = isInputDataOnRequest
                ? isOutputDataOnRequest
                : isOutputDataOnRequest == false;

            return areDataTypesCompatible
                   && areDataRequestingTypesCompatible;
        }

        public bool IsSame(CGModuleLink o)
            => ModuleID == o.ModuleID
               && SlotName == o.SlotName
               && TargetModuleID == o.TargetModuleID
               && TargetSlotName == o.m_TargetSlotName;

        public bool IsSame(CGModuleSlot source, CGModuleSlot target)
            => ModuleID == source.Module.UniqueID
               && SlotName == source.Name
               && TargetModuleID == target.Module.UniqueID
               && TargetSlotName == target.Name;

        public bool IsTo(CGModuleSlot s)
            => s.Module.UniqueID == TargetModuleID && s.Name == TargetSlotName;

        public bool IsFrom(CGModuleSlot s)
            => s.Module.UniqueID == ModuleID && s.Name == SlotName;

        public bool IsUsing(CGModule module)
            => ModuleID == module.UniqueID || TargetModuleID == module.UniqueID;

        public bool IsBetween(CGModuleSlot one, CGModuleSlot another)
            => (IsTo(one) && IsFrom(another)) || (IsTo(another) && IsFrom(one));

        public void SetModuleIDIINTERNAL(int moduleID, int targetModuleID)
        {
            m_ModuleID = moduleID;
            m_TargetModuleID = targetModuleID;
        }


        public static implicit operator bool(CGModuleLink a)
            => !ReferenceEquals(
                a,
                null
            );

        public override string ToString()
            => string.Format(
                CultureInfo.InvariantCulture,
                "{0}({1})->{2}({3})",
                SlotName,
                ModuleID,
                TargetSlotName,
                TargetModuleID
            );
    }
}