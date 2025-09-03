// =====================================================================
// Copyright © 2013 ToolBuddy
// All rights reserved
// 
// http://www.toolbuddy.net
// =====================================================================

namespace FluffyUnderware.Curvy.Generator
{
    /// <summary>
    /// For modules that rely on external input (Splines, Meshes etc..)
    /// </summary>
    public interface IExternalInput
    {
        /// <summary>
        /// Whether the module currently supports an IPE session
        /// </summary>
        bool SupportsIPE { get; }
    }
}