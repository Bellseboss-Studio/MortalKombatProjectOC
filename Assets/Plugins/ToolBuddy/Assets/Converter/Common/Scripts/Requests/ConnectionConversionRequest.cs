// =====================================================================
// Copyright © 2023 ToolBuddy
// All rights reserved
// 
// http://www.toolbuddy.net
// =====================================================================

#if CURVY_SPLINES

using System;
using System.Collections.Generic;
using FluffyUnderware.Curvy;
using JetBrains.Annotations;
using ToolBuddy.Converter.Common.SplineAbstraction;

namespace ToolBuddy.Converter.Common.Requests
{
    public readonly struct ConnectionConversionRequest : IEquatable<ConnectionConversionRequest>
    {
        private readonly SplineConnection<CurvySpline> connection;

        public ConnectionConversionRequest(
            SplineConnection<CurvySpline> connection) =>
            this.connection = connection;

        [NotNull] public ICollection<ControlPoint<CurvySpline>> ControlPoints => connection.ControlPoints;


        public bool Equals(
            ConnectionConversionRequest other) =>
            connection.Equals(other.connection);

        public override bool Equals(
            object obj) =>
            obj is ConnectionConversionRequest other && Equals(other);

        public override int GetHashCode() =>
            connection.GetHashCode();

        public static bool operator ==(
            ConnectionConversionRequest left,
            ConnectionConversionRequest right) =>
            left.Equals(right);

        public static bool operator !=(
            ConnectionConversionRequest left,
            ConnectionConversionRequest right) =>
            !left.Equals(right);
    }
}
#endif