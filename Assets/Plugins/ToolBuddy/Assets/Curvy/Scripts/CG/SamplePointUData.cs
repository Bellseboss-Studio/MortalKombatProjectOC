// =====================================================================
// Copyright © 2013 ToolBuddy
// All rights reserved
// 
// http://www.toolbuddy.net
// =====================================================================

using System;
using System.Globalization;
using JetBrains.Annotations;

namespace FluffyUnderware.Curvy.Generator
{
    public struct SamplePointUData : IEquatable<SamplePointUData>
    {
        public int Vertex;
        public bool UVEdge;
        public bool HardEdge;
        public float FirstU;
        public float SecondU;

        [UsedImplicitly]
        [Obsolete("Use other constructors")]
        public SamplePointUData(int vertexIndex, bool uvEdge, float firstU, float secondU) : this(
            vertexIndex,
            uvEdge,
            false,
            firstU,
            secondU
        ) { }

        public SamplePointUData(int vertexIndex, bool uvEdge, bool hardEdge, float firstU, float secondU)
        {
            Vertex = vertexIndex;
            UVEdge = uvEdge;
            HardEdge = hardEdge;
            FirstU = firstU;
            SecondU = secondU;
        }

        public SamplePointUData(int vertexIndex, ControlPointOption controlPointsOption) : this(
            vertexIndex,
            controlPointsOption.UVEdge,
            controlPointsOption.HardEdge,
            controlPointsOption.FirstU,
            controlPointsOption.SecondU
        ) { }


        public override string ToString()
            => string.Format(
                CultureInfo.InvariantCulture,
                "SamplePointUData (Vertex={0}, UVEdge={1}, HardEdge={4}, FirstU={2}, SecondU={3}",
                Vertex,
                UVEdge,
                FirstU,
                SecondU,
                HardEdge
            );

        public bool Equals(SamplePointUData other)
            => Vertex == other.Vertex
               && UVEdge == other.UVEdge
               && HardEdge == other.HardEdge
               && FirstU.Equals(other.FirstU)
               && SecondU.Equals(other.SecondU);

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(
                    null,
                    obj
                ))
                return false;
            return obj is SamplePointUData && Equals((SamplePointUData)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = Vertex;
                hashCode = (hashCode * 397) ^ UVEdge.GetHashCode();
                hashCode = (hashCode * 397) ^ HardEdge.GetHashCode();
                hashCode = (hashCode * 397) ^ FirstU.GetHashCode();
                hashCode = (hashCode * 397) ^ SecondU.GetHashCode();
                return hashCode;
            }
        }

        public static bool operator ==(SamplePointUData left, SamplePointUData right)
            => left.Equals(right);

        public static bool operator !=(SamplePointUData left, SamplePointUData right)
            => !left.Equals(right);
    }
}