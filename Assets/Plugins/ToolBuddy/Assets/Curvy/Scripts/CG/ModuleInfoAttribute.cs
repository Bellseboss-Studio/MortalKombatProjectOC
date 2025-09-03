// =====================================================================
// Copyright © 2013 ToolBuddy
// All rights reserved
// 
// http://www.toolbuddy.net
// =====================================================================

using System;

namespace FluffyUnderware.Curvy.Generator
{
    /// <summary>
    /// Attribute defining basic module properties
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class ModuleInfoAttribute : Attribute, IComparable
    {
        /// <summary>
        /// Menu-Name of the module (without '/')
        /// </summary>
        public readonly string MenuName;

        /// <summary>
        /// Default Module name
        /// </summary>
        public string ModuleName;

        /// <summary>
        /// Tooltip Info
        /// </summary>
        public string Description;

        /// <summary>
        /// Whether the module uses Random, i.e. should show Seed options
        /// </summary>
        public bool UsesRandom;

        public ModuleInfoAttribute(string name) =>
            MenuName = name;

        public int CompareTo(object obj)
            => String.Compare(
                MenuName,
                ((ModuleInfoAttribute)obj).MenuName,
                StringComparison.Ordinal
            );


        //TODO code analysis (CA1036) says that Equal, !=, <, == and > should be defined since IComparable is implemented
    }
}