// =====================================================================
// Copyright © 2013 ToolBuddy
// All rights reserved
// 
// http://www.toolbuddy.net
// =====================================================================

using System;
using System.Runtime.CompilerServices;
using FluffyUnderware.Curvy.Pools;
using JetBrains.Annotations;
using ToolBuddy.Pooling.Collections;
using UnityEngine;

namespace FluffyUnderware.Curvy
{
    partial class CurvySplineSegment
    {
        private class Approximations
        {
            public SubArray<Vector3> Positions;
            public SubArray<Vector3> Tangents;
            public SubArray<Vector3> Ups;
            public SubArray<float> Distances;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void ResizePositions(int size) => ArrayPools.Vector3.ResizeCopyless(
                ref Positions,
                size
            );

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void ResizeTangents(int size) => ArrayPools.Vector3.ResizeCopyless(
                ref Tangents,
                size
            );

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void ResizeUps(int size) => ArrayPools.Vector3.ResizeCopyless(
                ref Ups,
                size
            );

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void ResizeDistances(int size) => ArrayPools.Single.ResizeCopyless(
                ref Distances,
                size
            );

            public Approximations() =>
                Initialize();

            public void Clear()
            {
                Free();
                Initialize();
            }

            private void Initialize()
            {
                Positions = new SubArray<Vector3>(Array.Empty<Vector3>());
                Tangents = new SubArray<Vector3>(Array.Empty<Vector3>());
                Ups = new SubArray<Vector3>(Array.Empty<Vector3>());
                Distances = new SubArray<float>(Array.Empty<float>());
            }

            private void Free()
            {
                ArrayPools.Vector3.Free(Positions);
                ArrayPools.Vector3.Free(Tangents);
                ArrayPools.Vector3.Free(Ups);
                ArrayPools.Single.Free(Distances);
            }
        }


        /// <summary>
        /// List of precalculated interpolations
        /// </summary>
        /// <remarks>Based on Spline's CacheDensity</remarks>
        [UsedImplicitly]
        [Obsolete("Use GetPositionsApproximation instead")]

        public Vector3[] Approximation
        {
            get => PositionsApproximation.CopyToArray(ArrayPools.Vector3);
            set
            {
                ArrayPools.Vector3.Free(approximations.Positions);
                approximations.Positions = new SubArray<Vector3>(value);
            }
        }

        /// <summary>
        /// List of precalculated distances
        /// </summary>
        /// <remarks>Based on Spline's CacheDensity</remarks>
        [UsedImplicitly]
        [Obsolete("Use GetDistancesApproximation instead")]

        public float[] ApproximationDistances
        {
            get => DistancesApproximation.CopyToArray(ArrayPools.Single);
            set
            {
                ArrayPools.Single.Free(approximations.Distances);
                approximations.Distances = new SubArray<float>(value);
            }
        }

        /// <summary>
        /// List of precalculated Up-Vectors
        /// </summary>
        /// <remarks>Based on Spline's CacheDensity</remarks>
        [UsedImplicitly]
        [Obsolete("Use GetUpsApproximation instead")]
        public Vector3[] ApproximationUp
        {
            get => UpsApproximation.CopyToArray(ArrayPools.Vector3);
            set
            {
                ArrayPools.Vector3.Free(approximations.Ups);
                approximations.Ups = new SubArray<Vector3>(value);
            }
        }

        /// <summary>
        /// List of precalculated Tangent-Normals
        /// </summary>
        /// <remarks>Based on Spline's CacheDensity</remarks>
        [UsedImplicitly]
        [Obsolete("Use GetTangentsApproximation instead")]

        public Vector3[] ApproximationT
        {
            get => TangentsApproximation.CopyToArray(ArrayPools.Vector3);
            set
            {
                ArrayPools.Vector3.Free(approximations.Tangents);
                approximations.Tangents = new SubArray<Vector3>(value);
            }
        }
    }
}