// =====================================================================
// Copyright 2013-2017 Fluffy Underware
// All rights reserved
// 
// http://www.fluffyunderware.com
// =====================================================================

using System;
using UnityEngine;

namespace FluffyUnderware.DevTools
{
    [Serializable]
    public struct FloatRegion : IEquatable<FloatRegion>
    {
        public float From;
        public float To;
        public bool SimpleValue;

        public FloatRegion(float value)
        {
            From = value;
            To = value;
            SimpleValue = true;
        }

        public FloatRegion(float A, float B)
        {
            From = A;
            To = B;
            SimpleValue = false;
        }

        public static FloatRegion ZeroOne =>
            new FloatRegion(
                0,
                1
            );

        public void MakePositive()
        {
            if (To < From)
            {
                (From, To) = (To, From);
            }
        }

        public void Clamp(float low, float high)
        {
            Low = Mathf.Clamp(Low, low, high);
            High = Mathf.Clamp(High, low, high);
        }

        public bool Positive => From <= To;

        public float Low
        {
            get => (Positive) ? From : To;
            set
            {
                if (Positive)
                    From = value;
                else
                    To = value;
            }
        }

        public float High
        {
            get => (Positive) ? To : From;
            set
            {
                if (Positive)
                    To = value;
                else
                    From = value;
            }
        }

        public float Random => UnityEngine.Random.Range(From, To);

        /// <summary>
        /// Gets the next value in the range
        /// <remarks>Depending on the value of <see cref="SimpleValue"/>, this call will or will not make the Random generator's seed progress</remarks>
        /// </summary>
        public float Next
        {
            get
            {
                if (SimpleValue)
                    return From;
                else
                    return Random;
            }
        }

        public float Length => To - From;

        public float LengthPositive => (Positive) ? To - From : From - To;

        public override string ToString()
            => string.Format("({0:F2}-{1:F2})", From, To);

        public override int GetHashCode()
            => From.GetHashCode() ^ To.GetHashCode() << 2;

        public bool Equals(FloatRegion other)
            => From.Equals(other.From) && To.Equals(other.To);

        public override bool Equals(object other)
        {
            if (!(other is FloatRegion))
            {
                return false;
            }
            FloatRegion r = (FloatRegion)other;
            return From.Equals(r.From) && To.Equals(r.To);
        }

        public static FloatRegion operator +(FloatRegion a, FloatRegion b) =>
            new FloatRegion(
                a.From + b.From,
                a.To + b.To
            );

        public static FloatRegion operator -(FloatRegion a, FloatRegion b) =>
            new FloatRegion(
                a.From - b.From,
                a.To - b.To
            );

        public static FloatRegion operator -(FloatRegion a) =>
            new FloatRegion(
                -a.From,
                -a.To
            );

        public static FloatRegion operator *(FloatRegion a, float v) =>
            new FloatRegion(
                a.From * v,
                a.To * v
            );

        public static FloatRegion operator *(float v, FloatRegion a) =>
            new FloatRegion(
                a.From * v,
                a.To * v
            );

        public static FloatRegion operator /(FloatRegion a, float v) =>
            new FloatRegion(
                a.From / v,
                a.To / v
            );

        public static bool operator ==(FloatRegion lhs, FloatRegion rhs)
            => lhs.SimpleValue == rhs.SimpleValue && Mathf.Approximately(lhs.From, rhs.From) && Mathf.Approximately(lhs.To, rhs.To);

        public static bool operator !=(FloatRegion lhs, FloatRegion rhs)
            => lhs.SimpleValue != rhs.SimpleValue || !Mathf.Approximately(lhs.From, rhs.From) || !Mathf.Approximately(lhs.To, rhs.To);
    }
}