// =====================================================================
// Copyright © 2013 ToolBuddy
// All rights reserved
// 
// http://www.toolbuddy.net
// =====================================================================

using System;

namespace FluffyUnderware.Curvy
{
    /// <summary>
    /// CurvyShape Info Attribute
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class CurvyShapeInfo : Attribute
    {
        public readonly string Name;
        public readonly bool Is2D;

        public CurvyShapeInfo(string name, bool is2D = true)
        {
            Name = name;
            Is2D = is2D;
        }
    }
}