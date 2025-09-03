// =====================================================================
// Copyright © 2013 ToolBuddy
// All rights reserved
// 
// http://www.toolbuddy.net
// =====================================================================

using System;
using JetBrains.Annotations;

namespace FluffyUnderware.Curvy
{
    public partial class CurvySplineSegment
    {
        /// <summary>
        /// Contains data about a control point related to it's parent spline. For example, is a control point a valid segment in the spline or not.
        /// </summary>
        internal readonly struct ControlPointExtrinsicProperties : IEquatable<ControlPointExtrinsicProperties>
        {
            private readonly bool isVisible;

            /// <summary>
            /// Is the control point part of a segment (whether starting it or ending it)
            /// </summary>
            internal bool IsVisible => isVisible;

            private readonly float tf;

            /// <summary>
            /// Gets the TF of this Control Point
            /// TF stands for Total Fragment. It's a value ranging from 0 to 1 inclusive. 0 means the spline's start and 1 means the spline's end.
            /// This is the "time" parameter used in the splines' formulas.
            /// A point's TF is not proportional to its distance from the spline's start. Depending on the spline, a value of 0.5 does not always mean the middle, distance wise, of the spline
            /// </summary>
            internal float TF => tf;

            private readonly short segmentIndex;

            /// <summary>
            /// Index of the segment that this control point starts. -1 if control point does not start a segment.
            /// </summary>
            internal short SegmentIndex => segmentIndex;

            private readonly short controlPointIndex;

            /// <summary>
            /// Index of the control point
            /// </summary>
            internal short ControlPointIndex => controlPointIndex;

            private readonly short nextControlPointIndex;

            /// <summary>
            /// The index of the next control point on the spline. Is -1 if none. Follow-Up not considered
            /// </summary>
            internal short NextControlPointIndex => nextControlPointIndex;

            private readonly short previousControlPointIndex;

            /// <summary>
            /// The index of the previous control point on the spline. Is -1 if none. Follow-Up not considered. 
            /// </summary>
            internal short PreviousControlPointIndex => previousControlPointIndex;

            private readonly bool previousControlPointIsSegment;

            /// <summary>
            /// Is previous Control Point a segment start?
            /// </summary>
            internal bool PreviousControlPointIsSegment => previousControlPointIsSegment;

            private readonly bool nextControlPointIsSegment;

            /// <summary>
            /// Is next Control Point a segment start?
            /// </summary>
            internal bool NextControlPointIsSegment => nextControlPointIsSegment;

            private readonly bool canHaveFollowUp;

            /// <summary>
            /// Can this control point have a Follow-Up? This is true if the control point is visible and does not have a previous or next control point on its spline
            /// </summary>
            internal bool CanHaveFollowUp => canHaveFollowUp;

            /// <summary>
            /// Is the control point the start of a segment?
            /// </summary>
            internal bool IsSegment => SegmentIndex != -1;

            /// <summary>
            /// The index of the control point being the orientation anchor for the anchor group containing the current control point
            /// Is -1 for non visible control points
            /// </summary>
            [UsedImplicitly]
            [Obsolete("Use CurvySpline.GetControlPointOrientationAnchorIndex() instead")]
            internal short OrientationAnchorIndex =>
                throw new NotSupportedException("Use CurvySpline.GetControlPointOrientationAnchorIndex() instead");

            [UsedImplicitly]
            [Obsolete("Use the other constructor")]
            internal ControlPointExtrinsicProperties(bool isVisible, float tf, short segmentIndex, short controlPointIndex,
                short previousControlPointIndex, short nextControlPointIndex, bool previousControlPointIsSegment,
                bool nextControlPointIsSegment, bool canHaveFollowUp, short orientationAnchorIndex) : this(
                isVisible,
                tf,
                segmentIndex,
                controlPointIndex,
                previousControlPointIndex,
                nextControlPointIndex,
                previousControlPointIsSegment,
                nextControlPointIsSegment,
                canHaveFollowUp
            ) { }

            internal ControlPointExtrinsicProperties(bool isVisible, float tf, short segmentIndex, short controlPointIndex,
                short previousControlPointIndex, short nextControlPointIndex, bool previousControlPointIsSegment,
                bool nextControlPointIsSegment, bool canHaveFollowUp)
            {
                this.isVisible = isVisible;
                this.tf = tf;
                this.segmentIndex = segmentIndex;
                this.controlPointIndex = controlPointIndex;
                this.nextControlPointIndex = nextControlPointIndex;
                this.previousControlPointIndex = previousControlPointIndex;
                this.previousControlPointIsSegment = previousControlPointIsSegment;
                this.nextControlPointIsSegment = nextControlPointIsSegment;
                this.canHaveFollowUp = canHaveFollowUp;
            }

            public bool Equals(ControlPointExtrinsicProperties other)
                => IsVisible == other.IsVisible
                   && TF == other.TF
                   && SegmentIndex == other.SegmentIndex
                   && ControlPointIndex == other.ControlPointIndex
                   && NextControlPointIndex == other.NextControlPointIndex
                   && PreviousControlPointIndex == other.PreviousControlPointIndex
                   && PreviousControlPointIsSegment == other.PreviousControlPointIsSegment
                   && NextControlPointIsSegment == other.NextControlPointIsSegment
                   && CanHaveFollowUp == other.CanHaveFollowUp;

            public override bool Equals(object obj)
            {
                if (ReferenceEquals(
                        null,
                        obj
                    ))
                    return false;
                return obj is ControlPointExtrinsicProperties && Equals((ControlPointExtrinsicProperties)obj);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    int hashCode = IsVisible.GetHashCode();
                    hashCode = (hashCode * 397) ^ TF.GetHashCode();
                    hashCode = (hashCode * 397) ^ SegmentIndex.GetHashCode();
                    hashCode = (hashCode * 397) ^ ControlPointIndex.GetHashCode();
                    hashCode = (hashCode * 397) ^ NextControlPointIndex.GetHashCode();
                    hashCode = (hashCode * 397) ^ PreviousControlPointIndex.GetHashCode();
                    hashCode = (hashCode * 397) ^ PreviousControlPointIsSegment.GetHashCode();
                    hashCode = (hashCode * 397) ^ NextControlPointIsSegment.GetHashCode();
                    hashCode = (hashCode * 397) ^ CanHaveFollowUp.GetHashCode();
                    return hashCode;
                }
            }

            public static bool operator ==(ControlPointExtrinsicProperties left, ControlPointExtrinsicProperties right)
                => left.Equals(right);

            public static bool operator !=(ControlPointExtrinsicProperties left, ControlPointExtrinsicProperties right)
                => !left.Equals(right);
        }
    }
}