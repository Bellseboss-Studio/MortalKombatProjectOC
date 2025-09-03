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
    public struct IntRegion : IEquatable<IntRegion>
    {
        public int From;
        public int To;
        public bool SimpleValue;


        public IntRegion(int value)
        {
            From = value;
            To = value;
            SimpleValue = true;

        }

        public IntRegion(int A, int B)
        {
            From = A;
            To = B;
            SimpleValue = false;

        }

        public static IntRegion ZeroOne =>
            new IntRegion(
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

        public void Clamp(int low, int high)
        {
            Low = Mathf.Clamp(Low, low, high);
            High = Mathf.Clamp(High, low, high);
        }

        public bool Positive => From <= To;

        public int Low
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

        public int High
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

        public int Random => UnityEngine.Random.Range(From, To);

        public int Length => To - From;

        public int LengthPositive => (Positive) ? To - From : From - To;

        public override string ToString()
            => string.Format("({0}-{1})", From, To);

        public override int GetHashCode()
            => From.GetHashCode() ^ To.GetHashCode() << 2;

        public bool Equals(IntRegion other)
            => From.Equals(other.From) && To.Equals(other.To);

        public override bool Equals(object other)
        {
            if (!(other is IntRegion))
            {
                return false;
            }
            IntRegion r = (IntRegion)other;
            return From.Equals(r.From) && To.Equals(r.To);
        }

        public static IntRegion operator +(IntRegion a, IntRegion b) =>
            new IntRegion(
                a.From + b.From,
                a.To + b.To
            );

        public static IntRegion operator -(IntRegion a, IntRegion b) =>
            new IntRegion(
                a.From - b.From,
                a.To - b.To
            );

        public static IntRegion operator -(IntRegion a) =>
            new IntRegion(
                -a.From,
                -a.To
            );

        public static IntRegion operator *(IntRegion a, int v) =>
            new IntRegion(
                a.From * v,
                a.To * v
            );

        public static IntRegion operator *(int v, IntRegion a) =>
            new IntRegion(
                a.From * v,
                a.To * v
            );

        public static IntRegion operator /(IntRegion a, int v) =>
            new IntRegion(
                a.From / v,
                a.To / v
            );

        public static bool operator ==(IntRegion lhs, IntRegion rhs)
            => lhs.From == rhs.From && lhs.To == rhs.To && lhs.SimpleValue != rhs.SimpleValue;

        public static bool operator !=(IntRegion lhs, IntRegion rhs)
            => lhs.From != rhs.From || lhs.To != rhs.To || lhs.SimpleValue != rhs.SimpleValue;
    }
}