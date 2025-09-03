// =====================================================================
// Copyright © 2013 ToolBuddy
// All rights reserved
// 
// http://www.toolbuddy.net
// =====================================================================

using System;
using System.Diagnostics.CodeAnalysis;
using FluffyUnderware.DevTools;

namespace FluffyUnderware.Curvy
{
    #region ### CG related ###

    /// <summary>
    /// CG Resource Attribute
    /// </summary>
    [SuppressMessage(
        "Microsoft.Performance",
        "CA1813:AvoidUnsealedAttributes"
    )]
    [AttributeUsage(AttributeTargets.Field)]
    public class CGResourceManagerAttribute : DTPropertyAttribute
    {
        [JetBrains.Annotations.NotNull]
        public readonly string ResourceName;

        public bool ReadOnly;

        public CGResourceManagerAttribute([JetBrains.Annotations.NotNull] string resourceName) =>
            ResourceName = resourceName;
    }

    #endregion
}