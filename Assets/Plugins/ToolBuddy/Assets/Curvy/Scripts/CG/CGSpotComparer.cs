// =====================================================================
// Copyright © 2013 ToolBuddy
// All rights reserved
// 
// http://www.toolbuddy.net
// =====================================================================

using System.Collections;

namespace FluffyUnderware.Curvy.Generator
{
    /// <summary>
    /// An <see cref="IComparer"/> that compares instances of <see cref="CGSpot"/> based on their <see cref="CGSpot.Index"/>
    /// </summary>
    public class CGSpotComparer : IComparer
    {
        public int Compare(object x, object y)
            => ((CGSpot)x).Index.CompareTo(((CGSpot)y).Index);
    }
}