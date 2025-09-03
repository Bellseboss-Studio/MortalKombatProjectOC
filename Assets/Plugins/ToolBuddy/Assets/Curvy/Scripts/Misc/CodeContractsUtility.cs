// =====================================================================
// Copyright © 2013 ToolBuddy
// All rights reserved
// 
// http://www.toolbuddy.net
// =====================================================================

#if CONTRACTS_FULL
using System;
using System.Diagnostics.Contracts;
using UnityEngine;

namespace FluffyUnderware.Curvy
{
    public static class CodeContractsUtility
    {
        [Pure]
        public static void AssumeInvariant<T>(T assumptionTarget) { }

        [Pure]
        public static bool IsPercentage(this float number)
        {
            return number >= 0 && number <= 100;
        }

        [Pure]
        public static bool IsPercentage(this int number)
        {
            return number >= 0 && number <= 100;
        }

        [Pure]
        public static bool IsValidCollectionIndex(this int number, int collectionSize)
        {
            return number >= 0 && number < collectionSize;
        }

        [Pure]
        public static bool IsRatio(this float number)
        {
            return number >= 0 && number <= 1;
        }

        [Pure]
        public static bool IsRatioOrNegativeRatio(this float number)
        {
            return number >= -1 && number <= 1;
        }

        /// <summary>
        /// Returns true if float is neither an infinity nor a NaN
        /// </summary>
        /// <param name="number"></param>
        /// <returns></returns>
        [Pure]
        public static bool IsANumber(this float number)
        {
            return Single.IsNaN(number) == false && number.IsFinite();
        }

        [Pure]
        public static bool IsPositiveNumber(this float number)
        {
            return number >= 0 && number.IsANumber();
        }

        [Pure]
        public static bool IsStrictelyPositiveNumber(this float number)
        {
            return number > 0 && number.IsANumber();
        }

        [Pure]
        public static bool IsNegativeNumber(this float number)
        {
            return number <= 0 && number.IsANumber();
        }

        [Pure]
        public static bool IsOdd(this int number)
        {
            return number % 2 != 0;
        }

        [Pure]
        public static bool IsEven(this int number)
        {
            return number % 2 == 0;
        }


        /// <summary>
        /// Is the given value between 0 and 180 inclusive
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        [Pure]
        public static bool IsIn0To180Range(this float value)
        {
            return value >= 0 && value <= 180;
        }

        /// <summary>
        /// Is the given value between -180 and 180 inclusive
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        [Pure]
        public static bool IsInMinus180To180Range(this float value)
        {
            return value >= -180 && value <= 180;
        }

        /// <summary>
        /// Is the given value between 0 and 90 inclusive
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        [Pure]
        public static bool IsIn0To90Range(this float value)
        {
            return value >= 0 && value <= 90;
        }

        /// <summary>
        /// Is the given value between -90 and 90 inclusive
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        [Pure]
        public static bool IsInMinus90To90Range(this float value)
        {
            return value >= -90 && value <= 90;
        }

        [Pure]
        public static bool IsInRangeExclusive(this float value, float rangeLowerBound, float rangeUpperBound)
        {
            Contract.Requires(rangeLowerBound < rangeUpperBound);

            return value > rangeLowerBound && value < rangeUpperBound;
        }

        [Pure]
        public static bool IsInRangeInclusive(this float value, float rangeLowerBound, float rangeUpperBound)
        {
            Contract.Requires(rangeLowerBound < rangeUpperBound);

            return value >= rangeLowerBound && value <= rangeUpperBound;
        }

        [Pure]
        public static bool IsNan(this float number)
        {
            return Single.IsNaN(number);
        }

        [Pure]
        public static bool IsNormalized(this Vector3 vector)
        {
            return Approximately(
                vector.magnitude,
                1f
            );
        }

        [Pure]
        public static bool IsNormalized(this Quaternion q)
        {
            return Approximately(
                (float)Math.Sqrt(q.w * q.w + q.x * q.x + q.y * q.y + q.z * q.z)
                ,
                1f
            );
        }

        [Pure]
        public static bool ContainsNan(this Vector3 vector)
        {
            return float.IsNaN(vector.x) || float.IsNaN(vector.y) || float.IsNaN(vector.z);
        }

        [Pure]
        public static bool ContainsInfinity(this Vector3 vector)
        {
            return float.IsInfinity(vector.x) || float.IsInfinity(vector.y) || float.IsInfinity(vector.z);
        }

        [Pure]
        public static float PureDot(this Vector3 vector1, Vector3 vector2)
        {
            return Vector3.Dot(
                vector1,
                vector2
            );
        }

        [Pure]
        public static Vector3 PureCross(this Vector3 vector1, Vector3 vector2)
        {
            return Vector3.Cross(
                vector1,
                vector2
            );
        }

        [Pure]
        public static bool PureIsChildOf(this Transform child, Transform parent)
        {
            return child.IsChildOf(parent);
        }

        [Pure]
        public static bool Approximately(this float a, float b)
        {
            return Mathf.Approximately(
                a,
                b
            );
        }

        [Pure]
        public static bool WithinMargin(this float a, float b, float margin)
        {
            return Mathf.Abs(b - a) <= margin;
        }

        [Pure]
        public static bool Approximately(this Vector3 a, Vector3 b)
        {
            return a.x.Approximately(b.x)
                   && a.y.Approximately(b.y)
                   && a.z.Approximately(b.z);
        }

        [Pure]
        public static bool IsWithinDistance(this Vector3 a, Vector3 b, float maxAllowedDistance)
        {
            Contract.Requires(maxAllowedDistance.IsPositiveNumber());
            return (a - b).magnitude <= maxAllowedDistance;
        }

        //[Pure]
        //public static bool IsWithinAngle(this Quaternion a, Quaternion b, float maxAllowedAngle)
        //{
        //    Contract.Requires(maxAllowedAngle.IsPositiveNumber());

        //    return a.Angle(b) <= maxAllowedAngle;
        //}


        [Pure]
        public static bool IsFinite(this float number)
        {
            Contract.Requires(IsNan(number) == false);

            return float.IsInfinity(number) == false;
        }

        [Pure]
        public static bool IsInfinite(this float number)
        {
            return !IsFinite(number);
        }

        [Pure]
        public static bool IsNotZero(this float number)
        {
            return number.Approximately(0f) == false;
        }

        [Pure]
        public static int MaskNameToLayer(string layerName)
        {
            return LayerMask.NameToLayer(layerName);
        }

        [Pure]
        public static bool AreBarycentricCoordinatesOfPointInTriangle(this Vector3 barycentricCoordinates)
        {
            return
                barycentricCoordinates.x >= 0
                && barycentricCoordinates.y >= 0
                && barycentricCoordinates.z >= 0
                && 1f.Approximately(barycentricCoordinates.x + barycentricCoordinates.y + barycentricCoordinates.z);
        }

        [Pure]
        static public float PureMin(float a, float b)
        {
            Contract.Ensures(Contract.Result<float>() <= a);
            Contract.Ensures(Contract.Result<float>() <= b);
            return Mathf.Min(
                a,
                b
            );
        }

        [Pure]
        static public float PureMax(float a, float b)
        {
            Contract.Ensures(Contract.Result<float>() >= a);
            Contract.Ensures(Contract.Result<float>() >= b);
            return Mathf.Max(
                a,
                b
            );
        }

        [Pure]
        static public T PureGetComponent<T>(this GameObject gameObject)
        {
            return gameObject.GetComponent<T>();
        }

        [Pure]
        static public Component PureGetComponent(this GameObject gameObject, string componentName)
        {
            return gameObject.GetComponent(componentName);
        }
    }
}

#endif