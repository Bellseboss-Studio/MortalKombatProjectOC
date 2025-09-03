// =====================================================================
// Copyright © 2013 ToolBuddy
// All rights reserved
// 
// http://www.toolbuddy.net
// =====================================================================

using System;
using System.Globalization;
using UnityEngine;

namespace FluffyUnderware.Curvy.Generator
{
    /// <summary>
    /// A patch of vertices to be connected by triangles (i.e. same Material and no hard edges within a patch)
    /// </summary>
    /// <remarks>The index values refer to rasterized points of CGShape</remarks>
    public struct SamplePointsPatch : IEquatable<SamplePointsPatch>
    {
        /// <summary>
        /// First Sample Point Index of the patch
        /// </summary>
        public int Start;

        /// <summary>
        /// Number of Sample Points of the patch
        /// </summary>
        public int Count;

        /// <summary>
        /// Last Sample Point Index of the patch
        /// </summary>
        public int End
        {
            get => Start + Count;
            set => Count = Mathf.Max(
                0,
                value - Start
            );
        }

        public int TriangleCount => Count * 2;


        public SamplePointsPatch(int start)
        {
            Start = start;
            Count = 0;
        }

        public override string ToString()
            => string.Format(
                CultureInfo.InvariantCulture,
                "Size={0} ({1}-{2}, {3} Tris)",
                Count,
                Start,
                End,
                TriangleCount
            );

        public bool Equals(SamplePointsPatch other)
            => Start == other.Start && Count == other.Count;

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(
                    null,
                    obj
                ))
                return false;
            return obj is SamplePointsPatch && Equals((SamplePointsPatch)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (Start * 397) ^ Count;
            }
        }

        public static bool operator ==(SamplePointsPatch left, SamplePointsPatch right)
            => left.Equals(right);

        public static bool operator !=(SamplePointsPatch left, SamplePointsPatch right)
            => !left.Equals(right);
    }
}