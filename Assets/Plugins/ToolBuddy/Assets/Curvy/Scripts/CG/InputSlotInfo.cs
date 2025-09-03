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
    /// Attribute to define input sot properties
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class InputSlotInfo : SlotInfo
    {
        /// <summary>
        /// This slot gets data from its linked output slots only when this slot requests it.
        /// </summary>
        public bool RequestDataOnly = false;
        public bool Optional = false;

        /// <summary>
        /// Whether this data is altered by the module.
        /// If true, the module providing data to this slot will return a copy of its data, and not the original copy, so you can safely modify it.
        /// </summary>
        // DESIGN should this be removed, and ask users to just clone the data when they need to modify it?
        public bool ModifiesData = false;

        [Obsolete("Multiple types are no more supported. Use the other constructor.")]
        public InputSlotInfo(
            string name,
            params Type[] type) : base(
            name,
            type
        ) { }

        public InputSlotInfo(
            string name,
            [NotNull] Type type) : base(
            name,
            type
        ) { }

        [Obsolete("Multiple types are no more supported. Use the other constructor.")]
        public InputSlotInfo(
            params Type[] type) : this(
            null,
            type
        ) { }

        public InputSlotInfo(
            [NotNull] Type type) : this(
            (string)null,
            type
        ) { }

        /// <summary>
        /// Gets whether outType is of same type or a subtype of one of our input types
        /// </summary>
        public bool IsValidFrom(
            [NotNull] Type outType) =>
            outType == DataType || outType.IsSubclassOf(DataType);
    }
}