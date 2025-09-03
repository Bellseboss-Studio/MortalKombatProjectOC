// =====================================================================
// Copyright © 2013 ToolBuddy
// All rights reserved
// 
// http://www.toolbuddy.net
// =====================================================================

using System;

namespace FluffyUnderware.Curvy
{
    /// <summary>
    /// Represents a TCB parameter set for a spline segment. Used by TCB splines.
    /// TCB stands for Tension, Continuity, Bias.
    /// Tension controls the amount of curvature in the spline.
    /// Continuity controls the smoothness of the spline.
    /// Bias controls the direction of the curvature.
    /// </summary>
    /// <seealso cref="CurvyInterpolation.TCB"/>
    public struct TcbParameters : IEquatable<TcbParameters>
    {
        /// <summary>
        /// Tension at the start of the spline segment.
        /// </summary>
        public float StartTension { get; set; }

        /// <summary>
        /// Tension at the end of the spline segment.
        /// </summary>
        public float EndTension { get; set; }

        /// <summary>
        /// Continuity at the start of the spline segment.
        /// </summary>
        public float StartContinuity { get; set; }

        /// <summary>
        /// Continuity at the end of the spline segment.
        /// </summary>
        public float EndContinuity { get; set; }

        /// <summary>
        /// Bias at the start of the spline segment.
        /// </summary>
        public float StartBias { get; set; }

        /// <summary>
        /// Bias at the end of the spline segment.
        /// </summary>
        public float EndBias { get; set; }

        public bool Equals(TcbParameters other) =>
            StartTension.Equals(other.StartTension)
            && EndTension.Equals(other.EndTension)
            && StartContinuity.Equals(other.StartContinuity)
            && EndContinuity.Equals(other.EndContinuity)
            && StartBias.Equals(other.StartBias)
            && EndBias.Equals(other.EndBias);

        public override bool Equals(object obj)
            => obj is TcbParameters other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = StartTension.GetHashCode();
                hashCode = (hashCode * 397) ^ EndTension.GetHashCode();
                hashCode = (hashCode * 397) ^ StartContinuity.GetHashCode();
                hashCode = (hashCode * 397) ^ EndContinuity.GetHashCode();
                hashCode = (hashCode * 397) ^ StartBias.GetHashCode();
                hashCode = (hashCode * 397) ^ EndBias.GetHashCode();
                return hashCode;
            }
        }


        public static bool operator ==(TcbParameters left, TcbParameters right) => left.Equals(right);

        public static bool operator !=(TcbParameters left, TcbParameters right) => !left.Equals(right);
    }
}