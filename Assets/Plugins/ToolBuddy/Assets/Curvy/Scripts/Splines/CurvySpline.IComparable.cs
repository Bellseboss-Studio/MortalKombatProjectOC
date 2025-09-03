// =====================================================================
// Copyright © 2013 ToolBuddy
// All rights reserved
// 
// http://www.toolbuddy.net
// =====================================================================

using System;

namespace FluffyUnderware.Curvy
{
    public partial class CurvySpline : IComparable
    {
        /// <inheritdoc cref="IComparable.CompareTo"/>
        public int CompareTo(
            object obj)
        {
            if (obj == null)
                return 1;

            if (obj is CurvySpline other)
                return GetInstanceID().CompareTo(other.GetInstanceID());

            throw new ArgumentException($"Object must be of type {nameof(CurvySpline)}");

        }
    }
}