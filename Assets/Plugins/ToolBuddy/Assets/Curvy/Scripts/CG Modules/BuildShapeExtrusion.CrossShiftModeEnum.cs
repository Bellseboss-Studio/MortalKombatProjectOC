// =====================================================================
// Copyright © 2013 ToolBuddy
// All rights reserved
// 
// http://www.toolbuddy.net
// =====================================================================

namespace FluffyUnderware.Curvy.Generator.Modules
{
    public partial class BuildShapeExtrusion
    {
        public enum CrossShiftModeEnum
        {
            /// <summary>
            /// The start of the Shape is used
            /// </summary>
            None,

            /// <summary>
            /// The starting point is shifted to the collision point of the Path's orientation with the cross shape
            /// </summary>
            ByOrientation, //TODO rename to ByPathOrientation?

            /// <summary>
            /// The starting point is shifted by a user defined value
            /// </summary>
            Custom
        }
    }
}