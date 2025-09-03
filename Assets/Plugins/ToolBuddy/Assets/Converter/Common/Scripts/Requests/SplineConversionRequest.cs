// =====================================================================
// Copyright © 2023 ToolBuddy
// All rights reserved
// 
// http://www.toolbuddy.net
// =====================================================================

using System;
using System.Collections.Generic;
using JetBrains.Annotations;

namespace ToolBuddy.Converter.Common.Requests
{
    public readonly struct
        SplineConversionRequest<TSource, TDestination> : IEquatable<SplineConversionRequest<TSource, TDestination>>
        where TSource : class where TDestination : class
    {
        [NotNull]
        private readonly TSource source;

        [NotNull]
        private readonly TDestination destination;

        public SplineConversionRequest(
            [NotNull] TSource source,
            [NotNull] TDestination destination)
        {
            this.source = source ?? throw new ArgumentNullException(nameof(source));
            this.destination = destination ?? throw new ArgumentNullException(nameof(destination));
        }

        [NotNull] public TSource Source => source;

        [NotNull] public TDestination Destination => destination;

        public bool Equals(
            SplineConversionRequest<TSource, TDestination> other) =>
            EqualityComparer<TSource>.Default.Equals(
                source,
                other.source
            )
            && EqualityComparer<TDestination>.Default.Equals(
                destination,
                other.destination
            );

        public override bool Equals(
            object obj) =>
            obj is SplineConversionRequest<TSource, TDestination> other && Equals(other);

        public override int GetHashCode() =>
            HashCode.Combine(
                source,
                destination
            );

        public static bool operator ==(
            SplineConversionRequest<TSource, TDestination> left,
            SplineConversionRequest<TSource, TDestination> right) =>
            left.Equals(right);

        public static bool operator !=(
            SplineConversionRequest<TSource, TDestination> left,
            SplineConversionRequest<TSource, TDestination> right) =>
            !left.Equals(right);

        public override string ToString() =>
            $"{source} -> {destination}";
    }
}