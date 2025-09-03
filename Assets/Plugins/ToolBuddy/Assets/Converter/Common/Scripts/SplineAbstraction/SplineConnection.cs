// =====================================================================
// Copyright © 2023 ToolBuddy
// All rights reserved
// 
// http://www.toolbuddy.net
// =====================================================================

using System;
using System.Collections.Generic;
using JetBrains.Annotations;

namespace ToolBuddy.Converter.Common.SplineAbstraction
{
    public readonly struct SplineConnection<T> : IEquatable<SplineConnection<T>>
        where T : IComparable
    {
        [NotNull]
        private readonly SortedSet<ControlPoint<T>> controlPoints;

        [NotNull] public ICollection<ControlPoint<T>> ControlPoints => controlPoints;

        public SplineConnection(
            [NotNull] IEnumerable<ControlPoint<T>> controlPoints)
        {
            if (controlPoints == null)
                throw new ArgumentNullException(nameof(controlPoints));

            this.controlPoints = new SortedSet<ControlPoint<T>>(
                controlPoints,
                new ControlPointComparer<T>()
            );
        }

        public bool Equals(
            SplineConnection<T> other) =>
            controlPoints.SetEquals(other.controlPoints);

        public override bool Equals(
            object obj) =>
            obj is SplineConnection<T> other && Equals(other);

        public override int GetHashCode() =>
            GetElementsHashCode(controlPoints);

        public static bool operator ==(
            SplineConnection<T> left,
            SplineConnection<T> right) =>
            left.Equals(right);

        public static bool operator !=(
            SplineConnection<T> left,
            SplineConnection<T> right) =>
            !left.Equals(right);

        private static int GetElementsHashCode(
            [NotNull] SortedSet<ControlPoint<T>> elements)
        {
            if (elements == null)
                throw new ArgumentNullException(nameof(elements));

            HashCode hashCode = new HashCode();
            foreach (ControlPoint<T> element in elements)
                hashCode.Add(element);
            return hashCode.ToHashCode();
        }
    }
}