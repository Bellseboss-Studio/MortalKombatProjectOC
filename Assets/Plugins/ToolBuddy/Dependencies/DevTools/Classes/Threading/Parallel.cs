// =====================================================================
// Copyright 2013-2022 ToolBuddy
// All rights reserved
// 
// http://www.toolbuddy.net
// =====================================================================

using System.Runtime.CompilerServices;

namespace FluffyUnderware.DevTools.Threading
{
    /// <summary>
    /// Provides functionality for parallel execution. Handles the fact that Unity's WebGL build doesn't support System.Threading.Tasks.
    /// </summary>
    public static class Parallel
    {
        /// <summary>
        /// Executes a for loop with the specified range and body in parallel.
        /// </summary>
        /// <param name="fromInclusive">The start index, inclusive.</param>
        /// <param name="toExclusive">The end index, exclusive.</param>
        /// <param name="body">The action to perform for each iteration.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void For(int fromInclusive, int toExclusive, System.Action<int> body)
        {
            if (Environment.IsThreadingSupported)
                System.Threading.Tasks.Parallel.For(fromInclusive,
                    toExclusive,
                    body);
            else
                for (int i = fromInclusive; i < toExclusive; i++)
                    body(i);
        }
    }
}