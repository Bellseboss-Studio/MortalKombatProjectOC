// =====================================================================
// Copyright © 2013 ToolBuddy
// All rights reserved
// 
// http://www.toolbuddy.net
// =====================================================================

using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using FluffyUnderware.DevTools;
using UnityEngine;

namespace FluffyUnderware.Curvy.Utils
{
    /// <summary>
    /// Curvy Utility class
    /// </summary>
    public static class CurvyUtility
    {
        #region ### Clamping Methods ###

        /// <summary>
        /// Clamps relative position
        /// </summary>
        public static float ClampTF(float tf, CurvyClamping clamping)
        {
            switch (clamping)
            {
                case CurvyClamping.Loop:
                    return Mathf.Repeat(
                        tf,
                        1
                    );
                case CurvyClamping.PingPong:
                    return Mathf.PingPong(
                        tf,
                        1
                    );
                case CurvyClamping.Clamp:
                    return Mathf.Clamp01(tf);
                default:
                    throw new InvalidEnumArgumentException();
            }
        }


        /// <summary>
        /// Clamps relative position and sets new direction
        /// </summary>
        public static float ClampTF(float tf, ref int dir, CurvyClamping clamping)
        {
            switch (clamping)
            {
                case CurvyClamping.Loop:
                    return Mathf.Repeat(
                        tf,
                        1
                    );
                case CurvyClamping.PingPong:
                    if (Mathf.FloorToInt(tf) % 2 != 0)
                        dir *= -1;
                    return Mathf.PingPong(
                        tf,
                        1
                    );
                case CurvyClamping.Clamp:
                    return Mathf.Clamp01(tf);
                default:
                    throw new InvalidEnumArgumentException();
            }
        }

        /// <summary>
        /// Clamps a float to a range
        /// </summary>
        public static float ClampValue(float tf, CurvyClamping clamping, float minTF, float maxTF)
        {
            switch (clamping)
            {
                case CurvyClamping.Loop:
                    float v1 = DTMath.MapValue(
                        0,
                        1,
                        tf,
                        minTF,
                        maxTF
                    );
                    return DTMath.MapValue(
                        minTF,
                        maxTF,
                        Mathf.Repeat(
                            v1,
                            1
                        ),
                        0
                    );
                case CurvyClamping.PingPong:
                    float v2 = DTMath.MapValue(
                        0,
                        1,
                        tf,
                        minTF,
                        maxTF
                    );
                    return DTMath.MapValue(
                        minTF,
                        maxTF,
                        Mathf.PingPong(
                            v2,
                            1
                        ),
                        0
                    );
                case CurvyClamping.Clamp:
                    return Mathf.Clamp(
                        tf,
                        minTF,
                        maxTF
                    );
                default:
                    throw new InvalidEnumArgumentException();
            }
        }

        /// <summary>
        /// Clamps absolute position
        /// </summary>
        public static float ClampDistance(float distance, CurvyClamping clamping, float length)
        {
            if (length == 0)
                return 0;
            switch (clamping)
            {
                case CurvyClamping.Loop:
                    return Mathf.Repeat(
                        distance,
                        length
                    );
                case CurvyClamping.PingPong:
                    return Mathf.PingPong(
                        distance,
                        length
                    );
                case CurvyClamping.Clamp:
                    return Mathf.Clamp(
                        distance,
                        0,
                        length
                    );
                default:
                    throw new InvalidEnumArgumentException();
            }
        }

        /// <summary>
        /// Clamps absolute position
        /// </summary>
        public static float ClampDistance(float distance, CurvyClamping clamping, float length, float min, float max)
        {
            if (length == 0)
                return 0;
            min = Mathf.Clamp(
                min,
                0,
                length
            );
            max = Mathf.Clamp(
                max,
                min,
                length
            );
            switch (clamping)
            {
                case CurvyClamping.Loop:
                    return min
                    + Mathf.Repeat(
                        distance,
                        max - min
                    );
                case CurvyClamping.PingPong:
                    return min
                    + Mathf.PingPong(
                        distance,
                        max - min
                    );
                case CurvyClamping.Clamp:
                    return Mathf.Clamp(
                        distance,
                        min,
                        max
                    );
                default:
                    throw new InvalidEnumArgumentException();
            }
        }

        /// <summary>
        /// Clamps absolute position and sets new direction
        /// </summary>
        public static float ClampDistance(float distance, ref int dir, CurvyClamping clamping, float length)
        {
            if (length == 0)
                return 0;
            switch (clamping)
            {
                case CurvyClamping.Loop:
                    return Mathf.Repeat(
                        distance,
                        length
                    );
                case CurvyClamping.PingPong:
                    if (Mathf.FloorToInt(distance / length) % 2 != 0)
                        dir *= -1;
                    return Mathf.PingPong(
                        distance,
                        length
                    );
                case CurvyClamping.Clamp:
                    return Mathf.Clamp(
                        distance,
                        0,
                        length
                    );
                default:
                    throw new InvalidEnumArgumentException();
            }
        }

        /// <summary>
        /// Clamps absolute position and sets new direction
        /// </summary>
        public static float ClampDistance(float distance, ref int dir, CurvyClamping clamping, float length, float min, float max)
        {
            if (length == 0)
                return 0;
            min = Mathf.Clamp(
                min,
                0,
                length
            );
            max = Mathf.Clamp(
                max,
                min,
                length
            );
            switch (clamping)
            {
                case CurvyClamping.Loop:
                    return min
                    + Mathf.Repeat(
                        distance,
                        max - min
                    );
                case CurvyClamping.PingPong:
                    if (Mathf.FloorToInt(distance / (max - min)) % 2 != 0)
                        dir *= -1;
                    return min
                    + Mathf.PingPong(
                        distance,
                        max - min
                    );
                case CurvyClamping.Clamp:
                    return Mathf.Clamp(
                        distance,
                        min,
                        max
                    );
                default:
                    throw new InvalidEnumArgumentException();
            }
        }

        #endregion

        /// <summary>
        /// Gets the default material, i.e. Curvy/Resources/CurvyDefaultMaterial
        /// </summary>
        public static Material GetDefaultMaterial()
        {
            Material mat = Resources.Load("CurvyDefaultMaterial") as Material;
            if (mat == null)
            {
                Shader shader = Shader.Find("Standard");
                if (shader != null) //this can happen, for example in an HDRP build
                    mat = new Material(shader);
            }

            if (mat == null)
                DTLog.LogWarning("[Curvy] Couldn't find Curvy's default material. Please raise a bug report.");

            return mat;
        }


        /// <summary>
        /// Does the same things as Mathf.Approximately, but with different handling of case where one of the two values is 0
        /// Considering inputs of 0 and 1E-7, Mathf.Approximately will return false, while this method will return true.
        /// </summary>
        public static bool Approximately(this float x, float y)
        {
            bool result;
            const float zeroComparisionMargin = 0.000009f;

            float nearlyZero = Mathf.Epsilon * 8f;

            float absX = Math.Abs(x);
            float absY = Math.Abs(y);

            if (absY < nearlyZero)
                result = absX < zeroComparisionMargin;
            else if (absX < nearlyZero)
                result = absY < zeroComparisionMargin;
            else
                result = Mathf.Approximately(
                    x,
                    y
                );
            return result;
        }

        /// <summary>
        /// Finds the index of x in an array of sorted values (ascendant order). If x not found, the closest smaller value's index is returned if any, -1 otherwise
        /// </summary>
        ///  <param name="array">The array to search into</param>
        ///  <param name="x">The element to search for</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int InterpolationSearch(float[] array, float x)
            => InterpolationSearch(
                array,
                array.Length,
                x
            );

        ///  <summary>
        ///  Finds the index of x in an array of sorted values (ascendant order). If x not found, the closest smaller value's index is returned if any, -1 otherwise
        ///  </summary>
        ///  <param name="array">The array to search into</param>
        ///  <param name="elementsCount">The number of elements of the array to search into</param>
        ///  <param name="x">The element to search for</param>
        public static int InterpolationSearch(float[] array, int elementsCount, float x)
        {
            int low = 0, high = elementsCount - 1;

            while (low <= high && array[low] <= x && x <= array[high])
            {
                if (low == high)
                {
                    if (array[low] == x)
                        return low;
                    break;
                }

                int index = low + (int)(((high - low) / (array[high] - array[low])) * (x - array[low]));
                if (array[index] == x)
                    return index;
                if (array[index] < x)
                    low = index + 1;
                else
                    high = index - 1;
            }

            if (low > high)
                (low, high) = (high, low);

            if (x <= array[low])
            {
                while (low >= 0)
                {
                    if (array[low] <= x)
                        return low;
                    low--;
                }

                return 0;
            }

            if (array[high] < x)
            {
                while (high < elementsCount)
                {
                    if (x < array[high])
                        return high - 1;
                    high++;
                }

                return elementsCount - 1;
            }

            return -1;
        }

        /// <summary>
        /// Returns a mesh which boundaries are the input spline, similarly to what the Spline To Mesh window does, but simpler and less configurable.
        /// </summary>
        public static Mesh SplineToMesh(this CurvySpline spline)
        {
            Mesh result;

            Spline2Mesh splineToMesh = new Spline2Mesh();
            splineToMesh.Lines.Add(new SplinePolyLine(spline));
            splineToMesh.Apply(out result);

            if (String.IsNullOrEmpty(splineToMesh.Error) == false)
                Debug.Log(splineToMesh.Error);

            return result;
        }


        /// <summary>
        /// Given an input point, gets the index of the point in the array that is closest to the input point.
        /// </summary>
        /// <param name="point">the input point</param>
        /// <param name="points">A list of points to test against</param>
        /// <param name="pointsCount">The number of points to test against</param>
        /// <param name="index">the index of the closest point</param>
        /// <param name="fragement">a value between 0 and 1 indicating how close the input point is close to the point of index: index + 1</param>
        public static void GetNearestPointIndex(Vector3 point, Vector3[] points, int pointsCount, out int index,
            out float fragement)
        {
            float nearestSquaredDistance = float.MaxValue;
            int nearestIndex = 0;
            // get the nearest index
            for (int i = 0; i < pointsCount; i++)
            {
                Vector3 delta;
                delta.x = points[i].x - point.x;
                delta.y = points[i].y - point.y;
                delta.z = points[i].z - point.z;
                float squaredDistance = (delta.x * delta.x) + (delta.y * delta.y) + (delta.z * delta.z);
                if (squaredDistance <= nearestSquaredDistance)
                {
                    nearestSquaredDistance = squaredDistance;
                    nearestIndex = i;
                }
            }

            // collide p against the lines build by the index
            int leftIdx = nearestIndex > 0
                ? nearestIndex - 1
                : -1;
            int rightIdx = nearestIndex < pointsCount - 1
                ? nearestIndex + 1
                : -1;

            float leftFrag = 0;
            float rightFrag = 0;
            float leftSquaredDistance = float.MaxValue;
            float rightSquareDistance = float.MaxValue;
            {
                if (leftIdx > -1)
                    leftSquaredDistance = DTMath.LinePointDistanceSqr(
                        points[leftIdx],
                        points[nearestIndex],
                        point,
                        out leftFrag
                    );
                if (rightIdx > -1)
                    rightSquareDistance = DTMath.LinePointDistanceSqr(
                        points[nearestIndex],
                        points[rightIdx],
                        point,
                        out rightFrag
                    );
            }

            if (leftSquaredDistance < rightSquareDistance)
            {
                fragement = leftFrag;
                index = leftIdx;
            }
            else
            {
                fragement = rightFrag;
                index = nearestIndex;
            }
        }
    }

    #region ### Spline2Mesh ###

    #endregion
}