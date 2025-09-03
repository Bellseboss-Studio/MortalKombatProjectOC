// =====================================================================
// Copyright © 2013 ToolBuddy
// All rights reserved
// 
// http://www.toolbuddy.net
// =====================================================================

using System;
using FluffyUnderware.DevTools;
using JetBrains.Annotations;

namespace FluffyUnderware.Curvy
{
    /// <summary>
    /// CG Data Reference Selector Attribute
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class CGDataReferenceSelectorAttribute : DTPropertyAttribute
    {
        [NotNull]
        public readonly Type DataType;

        public CGDataReferenceSelectorAttribute([NotNull] Type dataType) =>
            DataType = dataType;
    }
}