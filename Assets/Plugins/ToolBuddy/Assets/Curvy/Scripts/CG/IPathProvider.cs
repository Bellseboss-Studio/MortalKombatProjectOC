// =====================================================================
// Copyright © 2013 ToolBuddy
// All rights reserved
// 
// http://www.toolbuddy.net
// =====================================================================

namespace FluffyUnderware.Curvy.Generator
{
    /// <summary>
    /// For modules that output instances of <see cref="CGPath"/>
    /// </summary>
    public interface IPathProvider
    {
        bool PathIsClosed { get; }
    }
}