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
    /// An <see cref="OutputSlotInfo"/> preset for modules that output CGShape data. Allows modules to output a <see cref="CGShape"/> that varies along a shape extrusion. See also <see cref="CGDataRequestShapeRasterization"/>
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class ShapeOutputSlotInfo : OutputSlotInfo
    {
        /// <summary>
        /// Whether this module outputs a <see cref="CGShape"/> that varies along a shape extrusion
        /// </summary>
        public bool OutputsVariableShape = false;

        public ShapeOutputSlotInfo() : this(null) { }

        public ShapeOutputSlotInfo(string name) : base(
            name,
            typeof(CGShape)
        ) { }
    }
}