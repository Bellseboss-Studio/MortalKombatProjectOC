// =====================================================================
// Copyright © 2013 ToolBuddy
// All rights reserved
// 
// http://www.toolbuddy.net
// =====================================================================

namespace FluffyUnderware.Curvy.Generator
{
    /// <summary>
    /// Aspect Mode correction modes enum
    /// </summary>
    public enum CGKeepAspectMode
    {
        /// <summary>
        ///  No Aspect correction is applied
        /// </summary>
        Off,

        /// <summary>
        /// U is scaled to keep texel size proportional
        /// </summary>
        ScaleU,

        /// <summary>
        /// V is scaled to keep texel size proportional
        /// </summary>
        ScaleV
    }
}