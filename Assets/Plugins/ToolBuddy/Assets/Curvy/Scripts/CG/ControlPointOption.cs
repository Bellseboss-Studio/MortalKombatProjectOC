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
    /// Rasterization helper
    /// </summary>
    public struct ControlPointOption : IEquatable<ControlPointOption>
    {
        public float TF;
        public float Distance;
        public bool Include;
        public int MaterialID;
        public bool HardEdge;
        public float MaxStepDistance;
        public bool UVEdge;

        /// <summary>
        /// Also known as ExplicitU
        /// </summary>
        public bool UVShift;

        public float FirstU;
        public float SecondU;


        public ControlPointOption(float tf, float dist, bool includeAnyways, int materialID, bool hardEdge, float maxStepDistance,
            bool uvEdge, bool uvShift, float firstU, float secondU)
        {
            TF = tf;
            Distance = dist;
            Include = includeAnyways;
            MaterialID = materialID;
            HardEdge = hardEdge;
            if (maxStepDistance == 0)
                MaxStepDistance = float.MaxValue;
            else
                MaxStepDistance = maxStepDistance;
            UVEdge = uvEdge;
            UVShift = uvShift;
            FirstU = firstU;
            SecondU = secondU;
        }

        public bool Equals(ControlPointOption other)
            => TF.Equals(other.TF)
               && Distance.Equals(other.Distance)
               && Include == other.Include
               && MaterialID == other.MaterialID
               && HardEdge == other.HardEdge
               && MaxStepDistance.Equals(other.MaxStepDistance)
               && UVEdge == other.UVEdge
               && UVShift == other.UVShift
               && FirstU.Equals(other.FirstU)
               && SecondU.Equals(other.SecondU);

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(
                    null,
                    obj
                ))
                return false;
            return obj is ControlPointOption && Equals((ControlPointOption)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = TF.GetHashCode();
                hashCode = (hashCode * 397) ^ Distance.GetHashCode();
                hashCode = (hashCode * 397) ^ Include.GetHashCode();
                hashCode = (hashCode * 397) ^ MaterialID;
                hashCode = (hashCode * 397) ^ HardEdge.GetHashCode();
                hashCode = (hashCode * 397) ^ MaxStepDistance.GetHashCode();
                hashCode = (hashCode * 397) ^ UVEdge.GetHashCode();
                hashCode = (hashCode * 397) ^ UVShift.GetHashCode();
                hashCode = (hashCode * 397) ^ FirstU.GetHashCode();
                hashCode = (hashCode * 397) ^ SecondU.GetHashCode();
                return hashCode;
            }
        }

        public static bool operator ==(ControlPointOption left, ControlPointOption right)
            => left.Equals(right);

        public static bool operator !=(ControlPointOption left, ControlPointOption right)
            => !left.Equals(right);
    }
}