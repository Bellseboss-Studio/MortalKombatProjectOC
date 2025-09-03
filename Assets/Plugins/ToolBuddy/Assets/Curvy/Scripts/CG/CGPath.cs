// =====================================================================
// Copyright © 2013 ToolBuddy
// All rights reserved
// 
// http://www.toolbuddy.net
// =====================================================================

using System;
using FluffyUnderware.Curvy.Pools;
using FluffyUnderware.Curvy.Utils;
using JetBrains.Annotations;
using ToolBuddy.Pooling.Collections;
using ToolBuddy.Pooling.Pools;
using UnityEngine;

namespace FluffyUnderware.Curvy.Generator
{
    /// <summary>
    /// Path Data (Shape + Direction (Spline Tangents) + Orientation/Up)
    /// </summary>
    [CGDataInfo(
        0.13f,
        0.59f,
        0.95f
    )]
    public class CGPath : CGShape
    {
        /// <summary>
        /// Tangents of the path's points, in the path's local space
        /// </summary>
        /// <remarks>Setting a new <see cref="SubArray{T}"/> will <see cref="ArrayPool{T}.Free(ToolBuddy.Pooling.Collections.SubArray{T})"/> the current <see cref="SubArray{T}"/>  instance</remarks>
        public SubArray<Vector3> Directions
        {
            get => directions;
            set
            {
                ArrayPools.Vector3.Free(directions);
                directions = value;
            }
        }

        /// <summary>
        /// Tangents of the path's points, in the path's local space
        /// </summary>
        /// <remarks>This getter returns a copy of the actual array. For performance reasons, use the equivalent getter returning a <see cref="SubArray{T}"/> instance, which allows you to directly access and modify the underlying array</remarks>
        [UsedImplicitly]
        [Obsolete("Use Directions instead")]
        public Vector3[] Direction
        {
            get => Directions.CopyToArray(ArrayPools.Vector3);
            set => Directions = new SubArray<Vector3>(value);
        }

        private SubArray<Vector3> directions;

        public CGPath() =>
            directions = ArrayPools.Vector3.Allocate(0);

        public CGPath(CGPath source) : base(source) =>
            directions = ArrayPools.Vector3.Clone(source.directions);

        protected override bool Dispose(bool disposing)
        {
            bool result = base.Dispose(disposing);
            if (result)
                ArrayPools.Vector3.Free(directions);
            return result;
        }

        public override T Clone<T>()
            => new CGPath(this) as T;

        public static void Copy(CGPath dest, CGPath source)
        {
            //todo optim use ResizeCopyless?

            CGShape.Copy(
                dest,
                source
            );
            ArrayPools.Vector3.Resize(
                ref dest.directions,
                source.directions.Count
            );
            Array.Copy(
                source.directions.Array,
                0,
                dest.directions.Array,
                0,
                source.directions.Count
            );
        }

        /// <summary>
        /// Interpolates Position, Direction and Normal by F
        /// </summary>
        /// <param name="f">0..1</param>
        /// <param name="position"></param>
        /// <param name="direction">a.k.a tangent</param>
        /// <param name="up">a.k.a normal</param>
        public void Interpolate(float f, out Vector3 position, out Vector3 direction, out Vector3 up)
        {
            float frag;
            int idx = GetFIndex(
                f,
                out frag
            );
            position = OptimizedOperators.LerpUnclamped(
                Positions.Array[idx],
                Positions.Array[idx + 1],
                frag
            );
            direction = Vector3.SlerpUnclamped(
                directions.Array[idx],
                directions.Array[idx + 1],
                frag
            );
            up = Vector3.SlerpUnclamped(
                Normals.Array[idx],
                Normals.Array[idx + 1],
                frag
            );
        }

        [UsedImplicitly]
        [Obsolete("Method is no more used by Curvy and will get removed. Copy its content if you still need it")]
        public void Interpolate(float f, float angleF, out Vector3 pos, out Vector3 dir, out Vector3 up)
        {
            Interpolate(
                f,
                out pos,
                out dir,
                out up
            );
            if (angleF != 0)
            {
                Quaternion R = Quaternion.AngleAxis(
                    angleF * -360,
                    dir
                );
                up = R * up;
            }
        }

        /// <summary>
        /// Interpolates Direction by F
        /// </summary>
        /// <param name="f">0..1</param>
        public Vector3 InterpolateDirection(float f)
        {
            float frag;
            int idx = GetFIndex(
                f,
                out frag
            );
            return Vector3.SlerpUnclamped(
                directions.Array[idx],
                directions.Array[idx + 1],
                frag
            );
        }
    }
}