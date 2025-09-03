// =====================================================================
// Copyright © 2023 ToolBuddy
// All rights reserved
// 
// http://www.toolbuddy.net
// =====================================================================

using System;
using System.Collections.Generic;

namespace ToolBuddy.Converter.Common.SplineAbstraction
{
    /// <summary>
    /// Compares two control points based on their associated spline and index within that spline.
    /// </summary>
    /// <typeparam name="T1">The type of the spline identifier, which must implement IComparable.</typeparam>
    public class ControlPointComparer<T1> : IComparer<ControlPoint<T1>>
        where T1 : IComparable
    {
        /// <summary>
        /// Compares two control points first by their associated spline and then by their index within the spline.
        /// </summary>
        /// <param name="x">The first control point to compare.</param>
        /// <param name="y">The second control point to compare.</param>
        /// <returns>
        /// A signed integer that indicates the relative values of x and y, as follows:
        /// Less than zero: x is less than y.
        /// Zero: x equals y.
        /// Greater than zero: x is greater than y.
        /// </returns>
        public int Compare(
            ControlPoint<T1> x,
            ControlPoint<T1> y)
        {
            int result = x.Spline.CompareTo(y.Spline);
            if (result != 0)
                return result;

            return x.Index.CompareTo(y.Index);
        }
    }
}