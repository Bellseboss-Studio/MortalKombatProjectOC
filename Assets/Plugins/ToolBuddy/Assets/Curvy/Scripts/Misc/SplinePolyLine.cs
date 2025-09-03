// =====================================================================
// Copyright © 2013 ToolBuddy
// All rights reserved
// 
// http://www.toolbuddy.net
// =====================================================================

using System;
using FluffyUnderware.Curvy.Pools;
using FluffyUnderware.Curvy.ThirdParty.LibTessDotNet;
using JetBrains.Annotations;
using ToolBuddy.Pooling.Collections;
using UnityEngine;

namespace FluffyUnderware.Curvy.Utils
{
    /// <summary>
    /// Spline Triangulation Helper Class
    /// </summary>
    [Serializable]
    public class SplinePolyLine
    {
        /// <summary>
        /// How to calculate vertices
        /// </summary>
        public enum VertexCalculation
        {
            /// <summary>
            /// Use Approximation points
            /// </summary>
            ByApproximation,

            /// <summary>
            /// By curvation angle
            /// </summary>
            ByAngle
        }

        /// <summary>
        /// Orientation order
        /// </summary>
        public ContourOrientation Orientation = ContourOrientation.Original;

        /// <summary>
        /// Base Spline
        /// </summary>
        public CurvySpline Spline;

        /// <summary>
        /// Vertex Calculation Mode
        /// </summary>
        public VertexCalculation VertexMode;

        /// <summary>
        /// Angle, used by VertexMode.ByAngle only
        /// </summary>
        public float Angle;

        /// <summary>
        /// Minimum distance, used by VertexMode.ByAngle only
        /// </summary>
        public float Distance;

        public Space Space;

        /// <summary>
        /// Creates a Spline2MeshCurve class using Spline2MeshCurve.VertexMode.ByApproximation
        /// </summary>
        public SplinePolyLine(CurvySpline spline) : this(
            spline,
            VertexCalculation.ByApproximation,
            0,
            0
        ) { }

        /// <summary>
        /// Creates a Spline2MeshCurve class using Spline2MeshCurve.VertexMode.ByAngle
        /// </summary>
        public SplinePolyLine(CurvySpline spline, float angle, float distance) : this(
            spline,
            VertexCalculation.ByAngle,
            angle,
            distance
        ) { }

        private SplinePolyLine(CurvySpline spline, VertexCalculation vertexMode, float angle, float distance,
            Space space = Space.World)
        {
            Spline = spline;
            VertexMode = vertexMode;
            Angle = angle;
            Distance = distance;
            Space = space;
        }

        /// <summary>
        /// Gets whether the spline is closed
        /// </summary>
        public bool IsClosed => Spline && Spline.Closed;

        /// <summary>
        /// Get vertices calculated using the current VertexMode
        /// </summary>
        /// <returns>an array of vertices</returns>
        [UsedImplicitly]
        [Obsolete("No more used in Curvy. Will get removed. Copy it if you still need it")]
        public Vector3[] GetVertices()
        {
            SubArray<Vector3> vertexList = GetVertexList();
            Vector3[] result = vertexList.CopyToArray(ArrayPools.Vector3);
            ArrayPools.Vector3.Free(vertexList);
            return result;
        }

        /// <summary>
        /// Get vertices calculated using the current VertexMode
        /// </summary>
        /// <returns>an array of vertices</returns>
        public SubArray<Vector3> GetVertexList()
        {
            SubArray<Vector3> points;
            switch (VertexMode)
            {
                case VertexCalculation.ByAngle:
                    points = GetPolygon(
                        Spline,
                        0,
                        1,
                        Angle,
                        Distance,
                        -1,
                        false
                    ).ToSubArray();
                    break;
                default:
                    points = Spline.GetPositionsCache(Space.Self);
                    break;
            }

            if (Space == Space.World)
            {
                Vector3[] pointsArray = points.Array;
                int pointsCount = points.Count;
                for (int i = 0; i < pointsCount; i++)
                    pointsArray[i] = Spline.transform.TransformPoint(pointsArray[i]);
            }

            return points;
        }

        /// <summary>
        /// Gets an array of sampled points that follow some restrictions on the distance between two consecutive points, and the angle of tangents between those points
        /// </summary>
        /// <param name="fromTF">start TF</param>
        /// <param name="toTF">end TF</param>
        /// <param name="maxAngle">maximum angle in degrees between tangents</param>
        /// <param name="minDistance">minimum distance between two points</param>
        /// <param name="maxDistance">maximum distance between two points</param>
        /// <param name="vertexTF">Stores the TF of the resulting points</param>
        /// <param name="vertexTangents">Stores the Tangents of the resulting points</param>
        /// <param name="includeEndPoint">Whether the end position should be included</param>
        /// <param name="stepSize">the stepsize to use</param>
        /// <returns>an array of interpolated positions</returns>
        private static SubArrayList<Vector3> GetPolygon(CurvySpline spline, float fromTF, float toTF, float maxAngle,
            float minDistance, float maxDistance, bool includeEndPoint = true, float stepSize = 0.01f)
        {
            stepSize = Mathf.Clamp(
                stepSize,
                0.002f,
                1
            );
            maxDistance = maxDistance == -1
                ? spline.Length
                : Mathf.Clamp(
                    maxDistance,
                    0,
                    spline.Length
                );
            minDistance = Mathf.Clamp(
                minDistance,
                0,
                maxDistance
            );
            if (!spline.Closed)
            {
                toTF = Mathf.Clamp01(toTF);
                fromTF = Mathf.Clamp(
                    fromTF,
                    0,
                    toTF
                );
            }

            SubArrayList<Vector3> vPos = new SubArrayList<Vector3>(
                50,
                ArrayPools.Vector3
            );

            int linearSteps = 0;
            float angleFromLast = 0;
            float distAccu = 0;
            Vector3 curPos = spline.Interpolate(fromTF);
            Vector3 curTangent = spline.GetTangent(fromTF);
            Vector3 lastPos = curPos;
            Vector3 lastTangent = curTangent;

            Action<Vector3> addPoint = position =>
            {
                vPos.Add(position);
                angleFromLast = 0;
                distAccu = 0;

                linearSteps = 0;
            };

            addPoint(curPos);

            float tf = fromTF + stepSize;
            while (tf < toTF)
            {
                // Get Point Pos & Tangent
                spline.InterpolateAndGetTangent(
                    tf % 1,
                    out curPos,
                    out curTangent
                );
                if (curTangent == Vector3.zero)
                    Debug.Log("zero Tangent! Oh no!");

                distAccu += (curPos - lastPos).magnitude;
                if (curTangent == lastTangent)
                    linearSteps++;
                if (distAccu >= minDistance)
                {
                    // Exceeding distance?
                    if (distAccu >= maxDistance)
                        addPoint(curPos);
                    else // Check angle
                    {
                        angleFromLast += Vector3.Angle(
                            lastTangent,
                            curTangent
                        );
                        // Max angle reached or entering/leaving a linear zone
                        if (angleFromLast >= maxAngle || (linearSteps > 0 && angleFromLast > 0))
                            addPoint(curPos);
                    }
                }

                tf += stepSize;
                lastPos = curPos;
                lastTangent = curTangent;
            }

            if (includeEndPoint)
            {
                curPos = spline.Interpolate(toTF % 1);
                vPos.Add(curPos);
            }

            return vPos;
        }
    }
}