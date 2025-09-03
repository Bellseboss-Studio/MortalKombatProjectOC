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
    public struct ControlPoint<T1> : IEquatable<ControlPoint<T1>>
        where T1 : IComparable
    {
        private readonly T1 spline;

        private readonly int controlPointIndex;

        public T1 Spline => spline;
        public int Index => controlPointIndex;

        public ControlPoint(
            T1 spline,
            int controlPointIndex)
        {
            this.spline = spline;
            this.controlPointIndex = controlPointIndex;
        }


        public override string ToString() => $"{{{spline}, {controlPointIndex}}}";


        public bool Equals(
            ControlPoint<T1> other) =>
            EqualityComparer<T1>.Default.Equals(
                spline,
                other.spline
            )
            && EqualityComparer<int>.Default.Equals(
                controlPointIndex,
                other.controlPointIndex
            );

        public override bool Equals(
            object obj) =>
            obj is ControlPoint<T1> other && Equals(other);

        public override int GetHashCode() =>
            HashCode.Combine(
                spline,
                controlPointIndex
            );

        public static bool operator ==(
            ControlPoint<T1> left,
            ControlPoint<T1> right) =>
            left.Equals(right);

        public static bool operator !=(
            ControlPoint<T1> left,
            ControlPoint<T1> right) =>
            !left.Equals(right);
    }
}