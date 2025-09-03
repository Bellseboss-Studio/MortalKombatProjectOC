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
using UnityEngine;

namespace FluffyUnderware.Curvy.Generator
{
    /// <summary>
    /// SubMesh data (triangles, material)
    /// </summary>
    public class CGVSubMesh : CGData
    {
        /// <summary>
        /// Vertex indices constituting the mesh's triangles
        /// </summary>
        /// <remarks>Setting a new <see cref="SubArray{T}"/> will <see cref="ArrayPool{T}.Free(ToolBuddy.Pooling.Collections.SubArray{T})"/> the current <see cref="SubArray{T}"/>  instance</remarks>
        public SubArray<int> TrianglesList
        {
            get => triangles;
            set
            {
                ArrayPools.Int32.Free(triangles);
                triangles = value;
            }
        }

        /// <summary>
        /// Vertex indices constituting the mesh's triangles
        /// </summary>
        /// <remarks>This getter returns a copy of the actual array. For performance reasons, use the equivalent getter returning a <see cref="SubArray{T}"/> instance, which allows you to directly access and modify the underlying array</remarks>
        [UsedImplicitly]
        [Obsolete("Use TrianglesList instead")]
        public int[] Triangles
        {
            get => TrianglesList.CopyToArray(ArrayPools.Int32);
            set => TrianglesList = new SubArray<int>(value);
        }

        public Material Material;
        private SubArray<int> triangles;

        public override int Count => triangles.Count;

        public CGVSubMesh(Material material = null)
        {
            Material = material;
            triangles = ArrayPools.Int32.Allocate(0);
        }

        public CGVSubMesh(int[] triangles, Material material = null)
        {
            Material = material;
            this.triangles = new SubArray<int>(triangles);
        }

        public CGVSubMesh(SubArray<int> triangles, Material material = null)
        {
            Material = material;
            this.triangles = triangles;
        }

        public CGVSubMesh(int triangleCount, Material material = null)
        {
            Material = material;
            triangles = ArrayPools.Int32.Allocate(triangleCount);
        }

        public CGVSubMesh(CGVSubMesh source)
        {
            Material = source.Material;
            triangles = ArrayPools.Int32.Clone(source.triangles);
        }

        protected override bool Dispose(bool disposing)
        {
            bool result = base.Dispose(disposing);
            if (result)
                ArrayPools.Int32.Free(triangles);
            return result;
        }

        public override T Clone<T>()
            => new CGVSubMesh(this) as T;

        public static CGVSubMesh Get(CGVSubMesh data, int triangleCount, Material material = null)
        {
            if (data == null)
                return new CGVSubMesh(
                    triangleCount,
                    material
                );

            //todo optim use ResizeCopyless?

            ArrayPools.Int32.Resize(
                ref data.triangles,
                triangleCount
            );
            data.Material = material;
            return data;
        }

        public void ShiftIndices(int offset, int startIndex = 0)
        {
            for (int i = startIndex; i < triangles.Count; i++)
                triangles.Array[i] += offset;
        }

        public void Add(CGVSubMesh other, int shiftIndexOffset = 0)
        {
            int trianglesLength = triangles.Count;
            int otherTriangleLength = other.triangles.Count;

            if (otherTriangleLength == 0)
                return;

            //todo optim use ResizeCopyless?

            ArrayPools.Int32.Resize(
                ref triangles,
                trianglesLength + otherTriangleLength
            );

            Array.Copy(
                other.triangles.Array,
                0,
                triangles.Array,
                trianglesLength,
                otherTriangleLength
            );

            if (shiftIndexOffset != 0)
                ShiftIndices(
                    shiftIndexOffset,
                    trianglesLength
                );
        }
    }
}