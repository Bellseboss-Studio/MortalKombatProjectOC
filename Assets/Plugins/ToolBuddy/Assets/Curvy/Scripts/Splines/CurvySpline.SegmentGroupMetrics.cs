// =====================================================================
// Copyright © 2013 ToolBuddy
// All rights reserved
// 
// http://www.toolbuddy.net
// =====================================================================

using System;
using JetBrains.Annotations;
using UnityEngine.Assertions;

namespace FluffyUnderware.Curvy
{
    public partial class CurvySpline
    {
        private struct SegmentGroupMetrics : IEquatable<SegmentGroupMetrics>
        {
            public int CacheSize;
            public int SegmentCount;
            public float Length;

            public void Increment([NotNull] CurvySplineSegment segment)
            {
#if CURVY_SANITY_CHECKS
                Assert.IsTrue(segment.Spline != null);
                Assert.IsTrue(segment.Spline.IsControlPointASegment(segment));
#endif

                CacheSize += segment.CacheSize;
                SegmentCount++;
                Length += segment.Length;
            }


            public bool Equals(SegmentGroupMetrics other)
                => CacheSize == other.CacheSize && SegmentCount == other.SegmentCount && Length.Equals(other.Length);

            public override bool Equals(object obj)
                => obj is SegmentGroupMetrics other && Equals(other);

            public override int GetHashCode()
            {
                unchecked
                {
                    int hashCode = CacheSize;
                    hashCode = (hashCode * 397) ^ SegmentCount;
                    hashCode = (hashCode * 397) ^ Length.GetHashCode();
                    return hashCode;
                }
            }


            public static bool operator ==(SegmentGroupMetrics left, SegmentGroupMetrics right)
                => left.Equals(right);

            public static bool operator !=(SegmentGroupMetrics left, SegmentGroupMetrics right)
                => !left.Equals(right);
        }
    }
}