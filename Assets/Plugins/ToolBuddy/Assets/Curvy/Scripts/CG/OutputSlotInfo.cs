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
    /// Attribute to define output slot properties
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class OutputSlotInfo : SlotInfo
    {
        public OutputSlotInfo(
            [NotNull] Type type) : this(
            null,
            type
        ) { }

        public OutputSlotInfo(
            string name,
            [NotNull] Type type) : base(
            name,
            type
        ) { }
    }
}