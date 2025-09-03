// =====================================================================
// Copyright © 2013 ToolBuddy
// All rights reserved
// 
// http://www.toolbuddy.net
// =====================================================================

using System;
using System.Collections.ObjectModel;
using JetBrains.Annotations;

namespace FluffyUnderware.Curvy
{
    public partial class CurvySplineSegment
    {
        private struct BSplineApproximationParameters : IEquatable<BSplineApproximationParameters>
        {
            public int Degree { get; }
            public bool IsClamped { get; }
            public bool IsClosed { get; }
            public float StartTf { get; }
            public float EndTf { get; }
            [NotNull] public ReadOnlyCollection<CurvySplineSegment> ControlPoints { get; }
            public int SegmentsCount { get; }

            public BSplineApproximationParameters([NotNull] CurvySplineSegment segment)
            {
                CurvySpline spline = segment.Spline;

                Degree = spline.BSplineDegree;
                IsClamped = spline.IsBSplineClamped;
                IsClosed = spline.Closed;
                StartTf = spline.SegmentToTF(segment);
                EndTf = spline.SegmentToTF(
                    segment,
                    1f
                );
                ControlPoints = spline.ControlPointsList;
                SegmentsCount = spline.Count;
            }

            public bool Equals(BSplineApproximationParameters other)
                => Degree == other.Degree
                   && IsClamped == other.IsClamped
                   && IsClosed == other.IsClosed
                   && StartTf.Equals(other.StartTf)
                   && EndTf.Equals(other.EndTf)
                   && ControlPoints.Equals(other.ControlPoints)
                   && SegmentsCount == other.SegmentsCount;

            public override bool Equals(object obj)
                => obj is BSplineApproximationParameters other && Equals(other);

            public override int GetHashCode()
            {
                unchecked
                {
                    int hashCode = Degree;
                    hashCode = (hashCode * 397) ^ IsClamped.GetHashCode();
                    hashCode = (hashCode * 397) ^ IsClosed.GetHashCode();
                    hashCode = (hashCode * 397) ^ StartTf.GetHashCode();
                    hashCode = (hashCode * 397) ^ EndTf.GetHashCode();
                    hashCode = (hashCode * 397) ^ ControlPoints.GetHashCode();
                    hashCode = (hashCode * 397) ^ SegmentsCount;
                    return hashCode;
                }
            }

            public static bool operator ==(BSplineApproximationParameters left, BSplineApproximationParameters right)
                => left.Equals(right);

            public static bool operator !=(BSplineApproximationParameters left, BSplineApproximationParameters right)
                => !left.Equals(right);
        }
    }
}