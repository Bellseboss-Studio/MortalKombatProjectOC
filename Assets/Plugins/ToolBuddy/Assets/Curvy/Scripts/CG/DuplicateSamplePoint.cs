// =====================================================================
// Copyright © 2013 ToolBuddy
// All rights reserved
// 
// http://www.toolbuddy.net
// =====================================================================

using System;

namespace FluffyUnderware.Curvy.Generator
{
    /// <summary>
    /// Data about duplicated points, meaning a couple of points sharing the same position. Such duplicated points are used to store different normals or different U coordinates at the same position
    /// </summary>
    public readonly struct DuplicateSamplePoint : IEquatable<DuplicateSamplePoint>
    {
        /// <summary>
        /// The index of the first point
        /// </summary>
        public int StartIndex { get; }

        /// <summary>
        /// The index of the second point
        /// </summary>
        public int EndIndex { get; }

        /// <summary>
        /// When true, both points don't share the same normal
        /// </summary>
        public bool IsHardEdge { get; }

        public DuplicateSamplePoint(int startIndex, int endIndex, bool isHardEdge)
        {
            StartIndex = startIndex;
            EndIndex = endIndex;
            IsHardEdge = isHardEdge;
        }

        public bool Equals(DuplicateSamplePoint other)
            => StartIndex == other.StartIndex && EndIndex == other.EndIndex && IsHardEdge == other.IsHardEdge;

        public override bool Equals(object obj)
            => obj is DuplicateSamplePoint other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = StartIndex;
                hashCode = (hashCode * 397) ^ EndIndex;
                hashCode = (hashCode * 397) ^ IsHardEdge.GetHashCode();
                return hashCode;
            }
        }

        public static bool operator ==(DuplicateSamplePoint left, DuplicateSamplePoint right)
            => left.Equals(right);

        public static bool operator !=(DuplicateSamplePoint left, DuplicateSamplePoint right)
            => !left.Equals(right);

        public override string ToString()
            => $"{nameof(StartIndex)}: {StartIndex}, {nameof(EndIndex)}: {EndIndex}, {nameof(IsHardEdge)}: {IsHardEdge}";
    }
}