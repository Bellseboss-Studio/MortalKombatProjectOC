// =====================================================================
// Copyright © 2013 ToolBuddy
// All rights reserved
// 
// http://www.toolbuddy.net
// =====================================================================

using System;
using UnityEngine;

namespace FluffyUnderware.Curvy.Utils
{
    /// <summary>
    /// Taken from my asset Frame Rate Booster
    /// https://assetstore.unity.com/packages/tools/utilities/frame-rate-booster-120660
    /// </summary>
    public static class OptimizedOperators
    {
        public static Vector3 Addition(this Vector3 a, Vector3 b)
        {
            a.x += b.x;
            a.y += b.y;
            a.z += b.z;
            return a;
        }

        public static Vector3 UnaryNegation(this Vector3 a)
        {
            Vector3 result;
            result.x = -a.x;
            result.y = -a.y;
            result.z = -a.z;
            return result;
        }

        public static Vector3 Subtraction(this Vector3 a, Vector3 b)
        {
            a.x -= b.x;
            a.y -= b.y;
            a.z -= b.z;
            return a;
        }

        public static Vector3 Multiply(this Vector3 a, float d)
        {
            a.x *= d;
            a.y *= d;
            a.z *= d;
            return a;
        }

        public static Vector3 Multiply(this float d, Vector3 a)
        {
            a.x *= d;
            a.y *= d;
            a.z *= d;
            return a;
        }

        public static Vector3 Division(this Vector3 a, float d)
        {
            float inversed = 1 / d;
            a.x *= inversed;
            a.y *= inversed;
            a.z *= inversed;
            return a;
        }

        public static Vector3 Normalize(this Vector3 value)
        {
            Vector3 result;
            float num = (float)Math.Sqrt((value.x * (double)value.x) + (value.y * (double)value.y) + (value.z * (double)value.z));
            if (num > 9.99999974737875E-06)
            {
                float inversed = 1 / num;
                result.x = value.x * inversed;
                result.y = value.y * inversed;
                result.z = value.z * inversed;
            }
            else
            {
                result.x = 0;
                result.y = 0;
                result.z = 0;
            }

            return result;
        }

        public static Vector3 LerpUnclamped(this Vector3 a, Vector3 b, float t)
        {
            a.x += (b.x - a.x) * t;
            a.y += (b.y - a.y) * t;
            a.z += (b.z - a.z) * t;
            return a;
        }

        public static Color Multiply(this Color a, float b)
        {
            a.r *= b;
            a.g *= b;
            a.b *= b;
            a.a *= b;
            return a;
        }

        public static Color Multiply(this float b, Color a)
        {
            a.r *= b;
            a.g *= b;
            a.b *= b;
            a.a *= b;
            return a;
        }

        public static Quaternion Multiply(this Quaternion lhs, Quaternion rhs)
        {
            Quaternion result;
            result.x = ((lhs.w * rhs.x) + (lhs.x * rhs.w) + (lhs.y * rhs.z)) - (lhs.z * rhs.y);
            result.y = ((lhs.w * rhs.y) + (lhs.y * rhs.w) + (lhs.z * rhs.x)) - (lhs.x * rhs.z);
            result.z = ((lhs.w * rhs.z) + (lhs.z * rhs.w) + (lhs.x * rhs.y)) - (lhs.y * rhs.x);
            result.w = (lhs.w * rhs.w) - (lhs.x * rhs.x) - (lhs.y * rhs.y) - (lhs.z * rhs.z);
            return result;
        }
    }
}