// =====================================================================
// Copyright © 2013 ToolBuddy
// All rights reserved
// 
// http://www.toolbuddy.net
// =====================================================================

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using FluffyUnderware.Curvy.Pools;
using FluffyUnderware.DevTools;
using FluffyUnderware.DevTools.Extensions;
using JetBrains.Annotations;
using ToolBuddy.Pooling.Collections;
using ToolBuddy.Pooling.Pools;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Rendering;

namespace FluffyUnderware.Curvy.Generator
{
    /// <summary>
    /// Mesh Data (Bounds + Vertex,UV,UV2,Normal,Tangents,SubMehes)
    /// </summary>
    [CGDataInfo(
        0.98f,
        0.5f,
        0
    )]
    public class CGVMesh : CGBounds
    {
#if CONTRACTS_FULL
        [ContractInvariantMethod]
        [System.Diagnostics.CodeAnalysis.SuppressMessage(
            "Microsoft.Performance",
            "CA1822:MarkMembersAsStatic",
            Justification =
                "Required for code contracts."
        )]
        private void ObjectInvariant()
        {
            Contract.Invariant(Vertex != null);
            Contract.Invariant(UV != null);
            Contract.Invariant(UV2 != null);
            Contract.Invariant(Normal != null);
            Contract.Invariant(Tangents != null);
            Contract.Invariant(SubMeshes != null);

            Contract.Invariant(UV.Length == 0 || UV.Length == Vertex.Length);
            Contract.Invariant(UV2.Length == 0 || UV2.Length == Vertex.Length);
            Contract.Invariant(Normal.Length == 0 || Normal.Length == Vertex.Length);
            Contract.Invariant(Tangents.Length == 0 || Tangents.Length == Vertex.Length);
        }
#endif

        /// <summary>
        /// Positions of the points, in the local space
        /// </summary>
        /// <remarks>Setting a new <see cref="SubArray{T}"/> will <see cref="ArrayPool{T}.Free(ToolBuddy.Pooling.Collections.SubArray{T})"/> the current <see cref="SubArray{T}"/>  instance</remarks>
        /// <remarks>If you modify the content of the returned array, the content of <see cref="GetCachedSortedVertexIndices"/> will be outdated</remarks>
        public SubArray<Vector3> Vertices
        {
            get => vertices;
            set
            {
                ArrayPools.Vector3.Free(vertices);
                vertices = value;
                OnVerticesChanged();
            }
        }

        /// <summary>
        /// UVs of the points
        /// </summary>
        /// <remarks>Setting a new <see cref="SubArray{T}"/> will <see cref="ArrayPool{T}.Free(ToolBuddy.Pooling.Collections.SubArray{T})"/> the current <see cref="SubArray{T}"/>  instance</remarks>
        public SubArray<Vector2> UVs
        {
            get => uvs;
            set
            {
                ArrayPools.Vector2.Free(uvs);
                uvs = value;
            }
        }

        /// <summary>
        /// UV2s of the points
        /// </summary>
        /// <remarks>Setting a new <see cref="SubArray{T}"/> will <see cref="ArrayPool{T}.Free(ToolBuddy.Pooling.Collections.SubArray{T})"/> the current <see cref="SubArray{T}"/>  instance</remarks>
        public SubArray<Vector2> UV2s
        {
            get => uv2s;
            set
            {
                ArrayPools.Vector2.Free(uv2s);
                uv2s = value;
            }
        }

        /// <summary>
        /// Normals of the points, in the local space
        /// </summary>
        /// <remarks>Setting a new <see cref="SubArray{T}"/> will <see cref="ArrayPool{T}.Free(ToolBuddy.Pooling.Collections.SubArray{T})"/> the current <see cref="SubArray{T}"/>  instance</remarks>
        public SubArray<Vector3> NormalsList
        {
            get => normals;
            set
            {
                ArrayPools.Vector3.Free(normals);
                normals = value;
            }
        }

        /// <summary>
        /// Tangents of the points, in the local space
        /// </summary>
        /// <remarks>Setting a new <see cref="SubArray{T}"/> will <see cref="ArrayPool{T}.Free(ToolBuddy.Pooling.Collections.SubArray{T})"/> the current <see cref="SubArray{T}"/>  instance</remarks>
        public SubArray<Vector4> TangentsList
        {
            get => tangents;
            set
            {
                ArrayPools.Vector4.Free(tangents);
                tangents = value;
            }
        }

        #region Obsolete

        /// <summary>
        /// Positions of the points, in the local space
        /// </summary>
        /// <remarks>This getter returns a copy of the actual array. For performance reasons, use the equivalent getter returning a <see cref="SubArray{T}"/> instance, which allows you to directly access and modify the underlying array</remarks>
        [UsedImplicitly]
        [Obsolete("Use Vertices instead")]
        public Vector3[] Vertex
        {
            get => Vertices.CopyToArray(ArrayPools.Vector3);
            set => Vertices = new SubArray<Vector3>(value);
        }

        /// <summary>
        /// UVs of the points
        /// </summary>
        /// <remarks>This getter returns a copy of the actual array. For performance reasons, use the equivalent getter returning a <see cref="SubArray{T}"/> instance, which allows you to directly access and modify the underlying array</remarks>
        [UsedImplicitly]
        [Obsolete("Use UVs instead")]
        public Vector2[] UV
        {
            get => UVs.CopyToArray(ArrayPools.Vector2);
            set => UVs = new SubArray<Vector2>(value);
        }

        /// <summary>
        /// UV2s of the points
        /// </summary>
        /// <remarks>This getter returns a copy of the actual array. For performance reasons, use the equivalent getter returning a <see cref="SubArray{T}"/> instance, which allows you to directly access and modify the underlying array</remarks>
        [UsedImplicitly]
        [Obsolete("Use UV2s instead")]
        public Vector2[] UV2
        {
            get => UV2s.CopyToArray(ArrayPools.Vector2);
            set => UV2s = new SubArray<Vector2>(value);
        }


        /// <summary>
        /// Normals of the points, in the local space
        /// </summary>
        /// <remarks>This getter returns a copy of the actual array. For performance reasons, use the equivalent getter returning a <see cref="SubArray{T}"/> instance, which allows you to directly access and modify the underlying array</remarks>
        [UsedImplicitly]
        [Obsolete("Use NormalList instead")]
        public Vector3[] Normals
        {
            get => NormalsList.CopyToArray(ArrayPools.Vector3);
            set => NormalsList = new SubArray<Vector3>(value);
        }

        /// <summary>
        /// Tangents of the points, in the local space
        /// </summary>
        /// <remarks>This getter returns a copy of the actual array. For performance reasons, use the equivalent getter returning a <see cref="SubArray{T}"/> instance, which allows you to directly access and modify the underlying array</remarks>
        [UsedImplicitly]
        [Obsolete("Use TangentsList instead")]
        public Vector4[] Tangents
        {
            get => TangentsList.CopyToArray(ArrayPools.Vector4);
            set => TangentsList = new SubArray<Vector4>(value);
        }

        #endregion

        public CGVSubMesh[] SubMeshes;

        /// <summary>
        /// Gets the number of vertices
        /// </summary>
        public override int Count => vertices.Count;

        public bool HasUV => uvs.Count > 0;
        public bool HasUV2 => uv2s.Count > 0;

        /// <summary>
        /// True if at least one vertex has a normal
        /// </summary>
        public bool HasNormals => normals.Count > 0;

        /// <summary>
        /// True if <see cref="HasNormals"/> but not all vertices have normals
        /// </summary>
        public bool HasPartialNormals
        {
            get
            {
#if CURVY_SANITY_CHECKS
                Assert.IsTrue(hasPartialNormals == false || HasNormals);
#endif
                return hasPartialNormals;
            }
            private set => hasPartialNormals = value;
        }

        /// <summary>
        /// True if at least one vertex has a tangent
        /// </summary>
        public bool HasTangents => tangents.Count > 0;

        /// <summary>
        /// True if <see cref="HasTangents"/> but not all vertices have tangents
        /// </summary>
        public bool HasPartialTangents
        {
            get
            {
#if CURVY_SANITY_CHECKS
                Assert.IsTrue(hasPartialTangents == false || HasTangents);
#endif
                return hasPartialTangents;
            }
            private set => hasPartialTangents = value;
        }

        public int TriangleCount
        {
            get
            {
                int cnt = 0;
                for (int i = 0; i < SubMeshes.Length; i++)
                    cnt += SubMeshes[i].TrianglesList.Count;
                return cnt / 3;
            }
        }

        #region Private fields

        /// <summary>
        /// An array of the index of vertices when sorted by Z coordinate, from smaller to bigger
        /// </summary>
        private SubArray<int>? sortedVertexIndices;

        /// <summary>
        /// Lock used when generating <see cref="sortedVertexIndices"/>
        /// </summary>
        private readonly object vertexIndicesLock = new object();

        private SubArray<Vector3> vertices;

        private SubArray<Vector2> uvs;

        private SubArray<Vector2> uv2s;

        private SubArray<Vector3> normals;

        private SubArray<Vector4> tangents;

        private bool hasPartialNormals;

        private bool hasPartialTangents;

        #endregion

        public CGVMesh() : this(0) { }

        public CGVMesh(int vertexCount, bool addUV = false, bool addUV2 = false, bool addNormals = false,
            bool addTangents = false)
        {
            vertices = ArrayPools.Vector3.Allocate(vertexCount);
            uvs = addUV
                ? ArrayPools.Vector2.Allocate(vertexCount)
                : ArrayPools.Vector2.Allocate(0);
            uv2s = addUV2
                ? ArrayPools.Vector2.Allocate(vertexCount)
                : ArrayPools.Vector2.Allocate(0);
            normals = addNormals
                ? ArrayPools.Vector3.Allocate(vertexCount)
                : ArrayPools.Vector3.Allocate(0);
            tangents = addTangents
                ? ArrayPools.Vector4.Allocate(vertexCount)
                : ArrayPools.Vector4.Allocate(0);
            hasPartialNormals = false;
            hasPartialTangents = false;
            SubMeshes = new CGVSubMesh[0];
        }

        public CGVMesh(CGVolume volume) : this(volume.Vertices.Count) =>
            Array.Copy(
                volume.Vertices.Array,
                0,
                vertices.Array,
                0,
                volume.Vertices.Count
            );

        public CGVMesh(CGVolume volume, IntRegion subset)
            : this(
                (subset.LengthPositive + 1) * volume.CrossSize,
                false,
                false,
                true
            )
        {
            int start = subset.Low * volume.CrossSize;
            Array.Copy(
                volume.Vertices.Array,
                start,
                vertices.Array,
                0,
                vertices.Count
            );
            Array.Copy(
                volume.VertexNormals.Array,
                start,
                normals.Array,
                0,
                normals.Count
            );
        }

        public CGVMesh(CGVMesh source) : base(source)
        {
            vertices = ArrayPools.Vector3.Clone(source.vertices);
            uvs = ArrayPools.Vector2.Clone(source.uvs);
            uv2s = ArrayPools.Vector2.Clone(source.uv2s);
            normals = ArrayPools.Vector3.Clone(source.normals);
            tangents = ArrayPools.Vector4.Clone(source.tangents);
            hasPartialNormals = source.HasPartialNormals;
            hasPartialTangents = source.HasPartialTangents;
            SubMeshes = new CGVSubMesh[source.SubMeshes.Length];
            for (int i = 0; i < source.SubMeshes.Length; i++)
                SubMeshes[i] = new CGVSubMesh(source.SubMeshes[i]);
        }

        public CGVMesh([NotNull] CGMeshProperties meshProperties) : this(
            meshProperties.Mesh,
            meshProperties.Material,
            meshProperties.Matrix
        ) { }

        public CGVMesh([NotNull] Mesh source, Material[] materials, Matrix4x4 trsMatrix)
        {
            Name = source.name;
            vertices = new SubArray<Vector3>(source.vertices);
            normals = new SubArray<Vector3>(source.normals);
            tangents = new SubArray<Vector4>(source.tangents);
            hasPartialNormals = false;
            hasPartialTangents = false;
            uvs = new SubArray<Vector2>(source.uv);
            uv2s = new SubArray<Vector2>(source.uv2);
            SubMeshes = new CGVSubMesh[source.subMeshCount];
            for (int s = 0; s < source.subMeshCount; s++)
                SubMeshes[s] = new CGVSubMesh(
                    source.GetTriangles(s),
                    materials.Length > s
                        ? materials[s]
                        : null
                );

            Bounds = source.bounds;

            if (!trsMatrix.isIdentity)
                TRS(trsMatrix);
        }

        protected override bool Dispose(bool disposing)
        {
            bool result = base.Dispose(disposing);
            if (result)
            {
                if (sortedVertexIndices != null)
                    ArrayPools.Int32.Free(sortedVertexIndices.Value);
                ArrayPools.Vector3.Free(vertices);
                ArrayPools.Vector2.Free(uvs);
                ArrayPools.Vector2.Free(uv2s);
                ArrayPools.Vector3.Free(normals);
                ArrayPools.Vector4.Free(tangents);

                //Do not dispose SubMeshes if the call is due to finalization, since submeshes are disposable by themselves.
                if (disposing)
                    for (int i = 0; i < SubMeshes.Length; i++)
                        SubMeshes[i].Dispose();
            }

            return result;
        }

        public override T Clone<T>()
            => new CGVMesh(this) as T;

        [UsedImplicitly]
        [Obsolete("Member not used by Curvy, will get removed next major version. Use another overload of this method")]
        public static CGVMesh Get(CGVMesh data, CGVolume source, bool addUV, bool reverseNormals)
            => Get(
                data,
                source,
                new IntRegion(
                    0,
                    source.Count - 1
                ),
                addUV,
                reverseNormals
            );

        [UsedImplicitly]
        [Obsolete("Member not used by Curvy, will get removed next major version. Use another overload of this method")]
        public static CGVMesh Get(CGVMesh data, CGVolume source, IntRegion subset, bool addUV, bool reverseNormals)
            => Get(
                data,
                source,
                subset,
                addUV,
                false,
                reverseNormals
            );

        [NotNull]
        public static CGVMesh Get([CanBeNull] CGVMesh data, CGVolume source, IntRegion subset, bool addUV, bool addUV2,
            bool reverseNormals)
        {
            int start = subset.Low * source.CrossSize;
            int size = (subset.LengthPositive + 1) * source.CrossSize;

            if (data == null)
                data = new CGVMesh(
                    size,
                    addUV,
                    addUV2,
                    true
                );
            else
            {
                //todo optim use ResizeCopyless?

                if (data.vertices.Count != size)
                    ArrayPools.Vector3.Resize(
                        ref data.vertices,
                        size,
                        false
                    );

                if (data.normals.Count != size)
                    ArrayPools.Vector3.Resize(
                        ref data.normals,
                        size,
                        false
                    );

                int uvSize = addUV
                    ? size
                    : 0;
                if (data.uvs.Count != uvSize)
                    ArrayPools.Vector2.ResizeAndClear(
                        ref data.uvs,
                        uvSize
                    );

                int uv2Size = addUV2
                    ? size
                    : 0;
                if (data.uv2s.Count != uv2Size)
                    ArrayPools.Vector2.ResizeAndClear(
                        ref data.uv2s,
                        uv2Size
                    );

                //data.SubMeshes = new CGVSubMesh[0];//BUG? why is this commented?

                if (data.tangents.Count != 0)
                    ArrayPools.Vector4.Resize(
                        ref data.tangents,
                        0
                    );
                data.HasPartialTangents = false;
            }

            Array.Copy(
                source.Vertices.Array,
                start,
                data.vertices.Array,
                0,
                size
            );
            Array.Copy(
                source.VertexNormals.Array,
                start,
                data.normals.Array,
                0,
                size
            );
            data.hasPartialNormals = false;

            if (reverseNormals)
            {
                Vector3[] normalsArray = data.normals.Array;

                //OPTIM merge loop with normals copy
                for (int n = 0; n < data.normals.Count; n++)
                {
                    normalsArray[n].x = -normalsArray[n].x;
                    normalsArray[n].y = -normalsArray[n].y;
                    normalsArray[n].z = -normalsArray[n].z;
                }
            }

            data.OnVerticesChanged();

            return data;
        }


        public void SetSubMeshCount(int count) =>
            Array.Resize(
                ref SubMeshes,
                count
            );

        public void AddSubMesh(CGVSubMesh submesh = null) =>
            SubMeshes = SubMeshes.Add(submesh);

        /// <summary>
        /// Combine/Merge another VMesh into this
        /// </summary>
        /// <param name="source"></param>
        public void MergeVMesh(CGVMesh source) => MergeVMesh(
            source,
            Matrix4x4.identity
        );

        /// <summary>
        /// Combine/Merge another VMesh into this, applying a matrix
        /// </summary>
        /// <param name="source"></param>
        /// <param name="matrix"></param>
        public void MergeVMesh(CGVMesh source, Matrix4x4 matrix)
        {
            //TODO Design: unify implementation with MergeVMeshes
            int preMergeVertexCount = Count;
            // Add base data
            if (source.Count != 0)
            {
                int postMergeVertexCount = preMergeVertexCount + source.Count;
                ArrayPools.Vector3.Resize(
                    ref vertices,
                    postMergeVertexCount
                );
                if (matrix == Matrix4x4.identity)
                    Array.Copy(
                        source.vertices.Array,
                        0,
                        vertices.Array,
                        preMergeVertexCount,
                        source.Count
                    );
                else
                    for (int v = preMergeVertexCount; v < postMergeVertexCount; v++)
                        vertices.Array[v] = matrix.MultiplyPoint3x4(source.vertices.Array[v - preMergeVertexCount]);

                MergeUVsNormalsAndTangents(
                    source,
                    preMergeVertexCount
                );

                // Add Submeshes
                for (int sm = 0; sm < source.SubMeshes.Length; sm++)
                    GetMaterialSubMesh(source.SubMeshes[sm].Material).Add(
                        source.SubMeshes[sm],
                        preMergeVertexCount
                    );

                OnVerticesChanged();
            }
        }

        /// <summary>
        /// Combine/Merge multiple CGVMeshes into this
        /// </summary>
        /// <param name="vMeshes">list of CGVMeshes</param>
        /// <param name="startIndex">Index of the first element of the list to merge</param>
        /// <param name="endIndex">Index of the last element of the list to merge</param>
        public void MergeVMeshes(List<CGVMesh> vMeshes, int startIndex, int endIndex)
        {
            Assert.IsTrue(endIndex < vMeshes.Count);
            int totalVertexCount = 0;
            bool hasNormals = false;
            bool partialNormals = false;
            bool hasTangents = false;
            bool partialTangents = false;
            bool hasUV = false;
            bool hasUV2 = false;
            Dictionary<Material, List<SubArray<int>>> submeshesByMaterial = new Dictionary<Material, List<SubArray<int>>>();
            Dictionary<Material, int> trianglesIndexPerMaterial = new Dictionary<Material, int>();
            //dictionaries can't have null as a key, so to handle null materials, here are the fields equivalent to these dictionaries for null
            List<SubArray<int>> noMaterialSubmeshes = null;
            int noMaterialTrianglesIndex = 0;

            for (int i = startIndex; i <= endIndex; i++)
            {
                CGVMesh cgvMesh = vMeshes[i];
                totalVertexCount += cgvMesh.Count;
                hasNormals |= cgvMesh.HasNormals;
                partialNormals |= cgvMesh.HasNormals == false || cgvMesh.HasPartialNormals;
                hasTangents |= cgvMesh.HasTangents;
                partialTangents |= cgvMesh.HasTangents == false || cgvMesh.hasPartialTangents;
                hasUV |= cgvMesh.HasUV;
                hasUV2 |= cgvMesh.HasUV2;

                //initialize per material data
                for (int sm = 0; sm < cgvMesh.SubMeshes.Length; sm++)
                {
                    CGVSubMesh subMesh = cgvMesh.SubMeshes[sm];
                    if (subMesh.Material != null)
                    {
                        Material subMeshMaterial = subMesh.Material;
                        if (submeshesByMaterial.ContainsKey(subMeshMaterial) == false)
                        {
                            submeshesByMaterial[subMeshMaterial] = new List<SubArray<int>>(1);
                            trianglesIndexPerMaterial[subMeshMaterial] = 0;
                        }

                        submeshesByMaterial[subMeshMaterial].Add(subMesh.TrianglesList);
                    }
                    else
                    {
                        if (noMaterialSubmeshes == null)
                        {
                            noMaterialSubmeshes = new List<SubArray<int>>(1);
                            noMaterialTrianglesIndex = 0;
                        }

                        noMaterialSubmeshes.Add(subMesh.TrianglesList);
                    }
                }
            }

            //todo optim use ResizeCopyless?


            ArrayPools.Vector3.Resize(
                ref vertices,
                totalVertexCount
            );
            if (hasNormals)
                ArrayPools.Vector3.Resize(
                    ref normals,
                    totalVertexCount
                );
            hasPartialNormals = partialNormals;

            if (hasTangents)
                ArrayPools.Vector4.Resize(
                    ref tangents,
                    totalVertexCount
                );
            hasPartialTangents = partialTangents;

            if (hasUV)
                ArrayPools.Vector2.Resize(
                    ref uvs,
                    totalVertexCount
                );

            if (hasUV2)
                ArrayPools.Vector2.Resize(
                    ref uv2s,
                    totalVertexCount
                );

            foreach (KeyValuePair<Material, List<SubArray<int>>> pair in submeshesByMaterial)
                ProcessTriangleArrays(
                    pair.Value,
                    pair.Key
                );
            if (noMaterialSubmeshes != null)
                ProcessTriangleArrays(
                    noMaterialSubmeshes,
                    null
                );

            int currentVertexCount = 0;
            for (int i = startIndex; i <= endIndex; i++)
            {
                CGVMesh source = vMeshes[i];

                Array.Copy(
                    source.vertices.Array,
                    0,
                    vertices.Array,
                    currentVertexCount,
                    source.vertices.Count
                );
                if (hasNormals)
                {
                    if (source.HasNormals)
                        Array.Copy(
                            source.normals.Array,
                            0,
                            normals.Array,
                            currentVertexCount,
                            source.normals.Count
                        );
                    else
                        Array.Clear(
                            normals.Array,
                            currentVertexCount,
                            source.vertices.Count
                        );
                }

                if (hasTangents)
                {
                    if (source.HasTangents)
                        Array.Copy(
                            source.tangents.Array,
                            0,
                            tangents.Array,
                            currentVertexCount,
                            source.tangents.Count
                        );
                    else
                        Array.Clear(
                            tangents.Array,
                            currentVertexCount,
                            source.vertices.Count
                        );
                }

                if (hasUV)
                {
                    if (source.HasUV)
                        Array.Copy(
                            source.uvs.Array,
                            0,
                            uvs.Array,
                            currentVertexCount,
                            source.uvs.Count
                        );
                    else
                        Array.Clear(
                            uvs.Array,
                            currentVertexCount,
                            source.vertices.Count
                        );
                }

                if (hasUV2)
                {
                    if (source.HasUV2)
                        Array.Copy(
                            source.uv2s.Array,
                            0,
                            uv2s.Array,
                            currentVertexCount,
                            source.uv2s.Count
                        );
                    else
                        Array.Clear(
                            uv2s.Array,
                            currentVertexCount,
                            source.vertices.Count
                        );
                }

                // Add Submeshes
                for (int subMeshIndex = 0; subMeshIndex < source.SubMeshes.Length; subMeshIndex++)
                {
                    CGVSubMesh sourceSubMesh = source.SubMeshes[subMeshIndex];
                    Material sourceMaterial = sourceSubMesh.Material;
                    SubArray<int> sourceTriangles = sourceSubMesh.TrianglesList;
                    int sourceTrianglesLength = sourceTriangles.Count;

                    SubArray<int> destinationTriangles = GetMaterialSubMesh(sourceMaterial).TrianglesList;

                    int trianglesIndex = sourceMaterial == null
                        ? noMaterialTrianglesIndex
                        : trianglesIndexPerMaterial[sourceMaterial];

                    if (sourceTrianglesLength != 0)
                    {
                        if (currentVertexCount == 0)
                            Array.Copy(
                                sourceTriangles.Array,
                                0,
                                destinationTriangles.Array,
                                trianglesIndex,
                                sourceTrianglesLength
                            );
                        else
                            for (int j = 0; j < sourceTrianglesLength; j++)
                                destinationTriangles.Array[trianglesIndex + j] = sourceTriangles.Array[j] + currentVertexCount;

                        int materialTrianglesIndex = trianglesIndex + sourceTrianglesLength;

                        if (sourceMaterial == null)
                            noMaterialTrianglesIndex = materialTrianglesIndex;
                        else
                            trianglesIndexPerMaterial[sourceMaterial] = materialTrianglesIndex;
                    }
                }

                currentVertexCount += source.vertices.Count;
            }

            OnVerticesChanged();

            void ProcessTriangleArrays(List<SubArray<int>> subArrays, Material material1)
            {
                int totalTrianglesCount = 0;
                for (int arraysIndex = 0; arraysIndex < subArrays.Count; arraysIndex++)
                    totalTrianglesCount += subArrays[arraysIndex].Count;

                AddSubMesh(
                    new CGVSubMesh(
                        totalTrianglesCount,
                        material1
                    )
                );
            }
        }

        private void MergeUVsNormalsAndTangents(CGVMesh source, int preMergeVertexCount)
        {
            int sourceLength = source.Count;
            if (sourceLength == 0)
                return;

            int postMergeVetexCount = preMergeVertexCount + sourceLength;
            if (HasUV || source.HasUV)
            {
                SubArray<Vector2> newUVs = ArrayPools.Vector2.Allocate(
                    postMergeVetexCount,
                    false
                );

                if (HasUV)
                    Array.Copy(
                        uvs.Array,
                        0,
                        newUVs.Array,
                        0,
                        preMergeVertexCount
                    );
                else
                    Array.Clear(
                        newUVs.Array,
                        0,
                        preMergeVertexCount
                    );

                if (source.HasUV)
                    Array.Copy(
                        source.uvs.Array,
                        0,
                        newUVs.Array,
                        preMergeVertexCount,
                        sourceLength
                    );
                else
                    Array.Clear(
                        newUVs.Array,
                        preMergeVertexCount,
                        sourceLength
                    );

                UVs = newUVs;
            }

            if (HasUV2 || source.HasUV2)
            {
                SubArray<Vector2> newUV2s = ArrayPools.Vector2.Allocate(
                    postMergeVetexCount,
                    false
                );

                if (HasUV2)
                    Array.Copy(
                        uv2s.Array,
                        0,
                        newUV2s.Array,
                        0,
                        preMergeVertexCount
                    );
                else
                    Array.Clear(
                        newUV2s.Array,
                        0,
                        preMergeVertexCount
                    );

                if (source.HasUV2)
                    Array.Copy(
                        source.uv2s.Array,
                        0,
                        newUV2s.Array,
                        preMergeVertexCount,
                        sourceLength
                    );
                else
                    Array.Clear(
                        newUV2s.Array,
                        preMergeVertexCount,
                        sourceLength
                    );

                UV2s = newUV2s;
            }

            if (HasNormals || source.HasNormals)
            {
                HasPartialNormals = HasNormals ^ source.HasNormals;

                SubArray<Vector3> newNormals = ArrayPools.Vector3.Allocate(
                    postMergeVetexCount,
                    false
                );

                if (HasNormals)
                    Array.Copy(
                        normals.Array,
                        0,
                        newNormals.Array,
                        0,
                        preMergeVertexCount
                    );
                else
                    Array.Clear(
                        newNormals.Array,
                        0,
                        preMergeVertexCount
                    );

                if (source.HasNormals)
                    Array.Copy(
                        source.normals.Array,
                        0,
                        newNormals.Array,
                        preMergeVertexCount,
                        sourceLength
                    );
                else
                    Array.Clear(
                        newNormals.Array,
                        preMergeVertexCount,
                        sourceLength
                    );

                NormalsList = newNormals;
            }

            if (HasTangents || source.HasTangents)
            {
                HasPartialTangents = HasTangents ^ source.HasTangents;

                SubArray<Vector4> newTangents = ArrayPools.Vector4.Allocate(
                    postMergeVetexCount,
                    false
                );

                if (HasTangents)
                    Array.Copy(
                        tangents.Array,
                        0,
                        newTangents.Array,
                        0,
                        preMergeVertexCount
                    );
                else
                    Array.Clear(
                        newTangents.Array,
                        0,
                        preMergeVertexCount
                    );

                if (source.HasTangents)
                    Array.Copy(
                        source.tangents.Array,
                        0,
                        newTangents.Array,
                        preMergeVertexCount,
                        sourceLength
                    );
                else
                    Array.Clear(
                        newTangents.Array,
                        preMergeVertexCount,
                        sourceLength
                    );

                TangentsList = newTangents;
            }
        }

        /// <summary>
        /// Gets the submesh using a certain material
        /// </summary>
        /// <param name="mat">the material the submesh should use</param>
        /// <param name="createIfMissing">whether to create the submesh if no existing one matches</param>
        /// <returns>a submesh using the given material</returns>
        public CGVSubMesh GetMaterialSubMesh(Material mat, bool createIfMissing = true)
        {
            // already having submesh with matching material?
            for (int sm = 0; sm < SubMeshes.Length; sm++)
                if (SubMeshes[sm].Material == mat)
                    return SubMeshes[sm];

            // else create new
            if (createIfMissing)
            {
                CGVSubMesh sm = new CGVSubMesh(mat);
                AddSubMesh(sm);
                return sm;
            }

            return null;
        }

        /// <summary>
        /// Creates a Mesh from the data
        /// </summary>
        [UsedImplicitly]
        [Obsolete("Use ToMesh instead")]
        public Mesh AsMesh()
        {
            Mesh msh = new Mesh();
            ToMesh(ref msh);
            return msh;
        }

        /// <summary>
        /// Copies the data into an existing Mesh
        /// </summary>
        /// <param name="mesh">The mesh to copy the data from this CGVMesh into</param>
        /// <param name="includeNormals">should normals be copied or set to empty?</param>
        /// <param name="includeTangents">should tangents be copied or set to empty?</param>
        public void ToMesh(ref Mesh mesh, bool includeNormals = true, bool includeTangents = true)
        {
            mesh.indexFormat = Count >= UInt16.MaxValue
                ? IndexFormat.UInt32
                : IndexFormat.UInt16;

            mesh.SetVertices(
                vertices.Array,
                0,
                vertices.Count
            );
            mesh.SetUVs(
                0,
                uvs.Array,
                0,
                HasUV
                    ? uvs.Count
                    : 0
            );
            mesh.SetUVs(
                1,
                uv2s.Array,
                0,
                HasUV2
                    ? uv2s.Count
                    : 0
            );
            mesh.SetNormals(
                normals.Array,
                0,
                includeNormals && HasNormals
                    ? normals.Count
                    : 0
            );
            mesh.SetTangents(
                tangents.Array,
                0,
                includeTangents && HasTangents
                    ? tangents.Count
                    : 0
            );

            mesh.subMeshCount = SubMeshes.Length;
            for (int s = 0; s < SubMeshes.Length; s++)
            {
                SubArray<int> subArray = SubMeshes[s].TrianglesList;
                mesh.SetTriangles(
                    subArray.Array,
                    0,
                    subArray.Count,
                    s
                );
            }
        }

        /// <summary>
        /// Gets a list of all Materials used
        /// </summary>
        public Material[] GetMaterials()
        {
            List<Material> mats = new List<Material>();
            for (int s = 0; s < SubMeshes.Length; s++)
                mats.Add(SubMeshes[s].Material);
            return mats.ToArray();
        }

        public override void RecalculateBounds()
        {
            if (Count == 0)
                mBounds = new Bounds(
                    Vector3.zero,
                    Vector3.zero
                );
            else
            {
                int vertexCount = vertices.Count;
                Vector3 min = vertices.Array[0], max = vertices.Array[0];
                for (int i = 1; i < vertexCount; i++)
                {
                    Vector3 vertex = vertices.Array[i];

                    if (vertex.x < min.x)
                        min.x = vertex.x;
                    else if (vertex.x > max.x)
                        max.x = vertex.x;

                    if (vertex.y < min.y)
                        min.y = vertex.y;
                    else if (vertex.y > max.y)
                        max.y = vertex.y;

                    if (vertex.z < min.z)
                        min.z = vertex.z;
                    else if (vertex.z > max.z)
                        max.z = vertex.z;
                }

                Bounds bounds = new Bounds();
                bounds.SetMinMax(
                    min,
                    max
                );
                mBounds = bounds;
            }
        }

        [UsedImplicitly]
        [Obsolete("Method will get remove in next major update. Copy its content if you need it")]
        public void RecalculateUV2()
        {
            ArrayPools.Vector2.Resize(
                ref uv2s,
                UVs.Count
            );
            CGUtility.CalculateUV2(
                uvs.Array,
                uv2s.Array,
                uvs.Count
            );
        }

        /// <summary>
        /// Applies the translation, rotation and scale defined by the given matrix
        /// </summary>
        public void TRS(Matrix4x4 matrix)
        {
            int count = Count;
            for (int vertexIndex = 0; vertexIndex < count; vertexIndex++)
                vertices.Array[vertexIndex] = matrix.MultiplyPoint3x4(vertices.Array[vertexIndex]);

            count = normals.Count;
            for (int vertexIndex = 0; vertexIndex < count; vertexIndex++)
                normals.Array[vertexIndex] = matrix.MultiplyVector(normals.Array[vertexIndex]);

            count = tangents.Count;
            for (int vertexIndex = 0; vertexIndex < count; vertexIndex++)
            {
                //Keep in mind that Tangents is a Vector4 array
                Vector4 tangent4 = tangents.Array[vertexIndex];
                Vector3 tangent3;
                tangent3.x = tangent4.x;
                tangent3.y = tangent4.y;
                tangent3.z = tangent4.z;
                tangents.Array[vertexIndex] = matrix.MultiplyVector(tangent3);
            }

            OnVerticesChanged();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void OnVerticesChanged()
        {
            mBounds = null;
            ClearCachedSortedVertexIndices();
        }

        /// <summary>
        /// Gets an array of the index of vertices when sorted by Z coordinate, from smaller to bigger.
        /// This array is cached. Curvy Splines makes sure the cache is valid when it modifies <see cref="Vertices"/>. Whoever, if you modify yourself <see cref="Vertices"/> through its getter, the cache will become outdated.
        /// </summary>
        /// <remarks>Is thread safe</remarks>
        public SubArray<int> GetCachedSortedVertexIndices()
        {
            if (sortedVertexIndices == null)
                lock (vertexIndicesLock)
                {
                    if (sortedVertexIndices == null)
                    {
                        int verticesCount = vertices.Count;

                        SubArray<int> result = ArrayPools.Int32.Allocate(verticesCount);
                        SubArray<float> verticesZ = ArrayPools.Single.Allocate(verticesCount);
                        for (int k = 0; k < verticesCount; k++)
                        {
                            result.Array[k] = k;
                            verticesZ.Array[k] = vertices.Array[k].z;
                        }

                        Array.Sort(
                            verticesZ.Array,
                            result.Array,
                            0,
                            verticesCount
                        );
                        ArrayPools.Single.Free(verticesZ);

                        sortedVertexIndices = result;
                    }
                }

#if CURVY_SANITY_CHECKS
            Assert.IsTrue(sortedVertexIndices.Value.Count == Vertices.Count);
#endif
            return sortedVertexIndices.Value;
        }

        /// <summary>
        /// Clears the cached value computed by <see cref="GetCachedSortedVertexIndices"/>
        /// </summary>
        /// <remarks>Is thread safe</remarks>
        private void ClearCachedSortedVertexIndices()
        {
            if (sortedVertexIndices != null)
                lock (vertexIndicesLock)
                {
                    if (sortedVertexIndices != null)
                    {
                        ArrayPools.Int32.Free(sortedVertexIndices.Value);
                        sortedVertexIndices = null;
                    }
                }
        }
    }
}