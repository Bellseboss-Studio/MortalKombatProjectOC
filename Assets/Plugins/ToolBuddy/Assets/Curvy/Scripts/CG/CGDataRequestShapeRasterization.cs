// =====================================================================
// Copyright © 2013 ToolBuddy
// All rights reserved
// 
// http://www.toolbuddy.net
// =====================================================================

using System;
using FluffyUnderware.Curvy.Pools;
using JetBrains.Annotations;
using ToolBuddy.Pooling.Collections;
using ToolBuddy.Pooling.Pools;

namespace FluffyUnderware.Curvy.Generator
{
    /// <summary>
    /// Shape Rasterization Request parameters
    /// </summary>
    public class CGDataRequestShapeRasterization : CGDataRequestRasterization
    {
        /// <summary>
        /// The <see cref="CGShape.RelativeDistances"/> array of the <see cref="CGPath"/> instance used for the shape extrusion that requests the current Shape rasterization
        /// </summary>
        /// <remarks>Setting a new <see cref="SubArray{T}"/> will <see cref="ArrayPool{T}.Free(ToolBuddy.Pooling.Collections.SubArray{T})"/> the current <see cref="SubArray{T}"/>  instance</remarks>
        public SubArray<float> RelativeDistances
        {
            get => relativeDistances;
            set => relativeDistances = value;
        }

        /// <summary>
        /// The <see cref="CGShape.RelativeDistances"/> array of the <see cref="CGPath"/> instance used for the shape extrusion that requests the current Shape rasterization
        /// </summary>
        /// <remarks>This getter returns a copy of the actual array. For performance reasons, use the equivalent getter returning a <see cref="SubArray{T}"/> instance, which allows you to directly access and modify the underlying array</remarks>
        [UsedImplicitly]
        [Obsolete("Use RelativeDistances instead")]
        public float[] PathF
        {
            get => RelativeDistances.CopyToArray(ArrayPools.Single);
            set => RelativeDistances = new SubArray<float>(value);
        }

        private SubArray<float> relativeDistances;

        public CGDataRequestShapeRasterization(SubArray<float> relativeDistance, float start, float rasterizedRelativeLength,
            int resolution, float angle, ModeEnum mode = ModeEnum.Even) : base(
            start,
            rasterizedRelativeLength,
            resolution,
            angle,
            mode
        ) =>
            relativeDistances = ArrayPools.Single.Clone(relativeDistance);

        [UsedImplicitly]
        [Obsolete("Use another constructor instead")]
        public CGDataRequestShapeRasterization(float[] pathF, float start, float rasterizedRelativeLength, int resolution,
            float angle, ModeEnum mode = ModeEnum.Even) : base(
            start,
            rasterizedRelativeLength,
            resolution,
            angle,
            mode
        ) =>
            relativeDistances = ArrayPools.Single.Clone(pathF);

        public override bool Equals(object obj)
        {
            CGDataRequestShapeRasterization other = obj as CGDataRequestShapeRasterization;
            if (other == null)
                return false;

            if (!base.Equals(obj) || other.relativeDistances.Count != relativeDistances.Count)
                return false;

            for (int i = 0; i < relativeDistances.Count; i++)
                if (other.relativeDistances.Array[i].Equals(relativeDistances.Array[i]) == false)
                    return false;

            return true;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (base.GetHashCode() * 397)
                       ^ (relativeDistances != null
                           ? relativeDistances.GetHashCode()
                           : 0);
            }
        }

        public override string ToString()
            => $"{base.ToString()}, {nameof(RelativeDistances)}: {relativeDistances}";
    }
}