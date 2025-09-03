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
    /// Volume Data (Path + Vertex, VertexNormal, Cross)
    /// </summary>
    [CGDataInfo(
        0.08f,
        0.4f,
        0.75f
    )]
    public class CGVolume : CGPath
    {
        /// <summary>
        /// Positions of the points, in the local space
        /// </summary>
        /// <remarks>Setting a new <see cref="SubArray{T}"/> will <see cref="ArrayPool{T}.Free(ToolBuddy.Pooling.Collections.SubArray{T})"/> the current <see cref="SubArray{T}"/>  instance</remarks>
        public SubArray<Vector3> Vertices
        {
            get => vertices;
            set
            {
                ArrayPools.Vector3.Free(vertices);
                vertices = value;
            }
        }

        /// <summary>
        /// Notmals of the points, in the local space
        /// </summary>
        /// <remarks>Setting a new <see cref="SubArray{T}"/> will <see cref="ArrayPool{T}.Free(ToolBuddy.Pooling.Collections.SubArray{T})"/> the current <see cref="SubArray{T}"/>  instance</remarks>
        public SubArray<Vector3> VertexNormals
        {
            get => vertexNormals;
            set
            {
                ArrayPools.Vector3.Free(vertexNormals);
                vertexNormals = value;
            }
        }

        /// <summary>
        /// The <see cref="CGShape.RelativeDistances"/> of the <see cref="CGShape"/> used in the extrusion of this volume
        /// </summary>
        /// <remarks>Setting a new <see cref="SubArray{T}"/> will <see cref="ArrayPool{T}.Free(ToolBuddy.Pooling.Collections.SubArray{T})"/> the current <see cref="SubArray{T}"/>  instance</remarks>
        public SubArray<float> CrossRelativeDistances
        {
            get => crossRelativeDistances;
            set
            {
                ArrayPools.Single.Free(crossRelativeDistances);
                crossRelativeDistances = value;
            }
        }

        /// <summary>
        /// The <see cref="CGShape.CustomValues"/> of the <see cref="CGShape"/> used in the extrusion of this volume
        /// </summary>
        /// <remarks>Setting a new <see cref="SubArray{T}"/> will <see cref="ArrayPool{T}.Free(ToolBuddy.Pooling.Collections.SubArray{T})"/> the current <see cref="SubArray{T}"/>  instance</remarks>
        public SubArray<float> CrossCustomValues
        {
            get => crossCustomValues;
            set
            {
                ArrayPools.Single.Free(crossCustomValues);
                crossCustomValues = value;
            }
        }

        /// <summary>
        /// The 2D scale of the mesh at each sample point of the volume's path
        /// </summary>
        public SubArray<Vector2> Scales
        {
            get => scales;
            set
            {
                ArrayPools.Vector2.Free(scales);
                scales = value;
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
        /// Normals of the points, in the local space
        /// </summary>
        /// <remarks>This getter returns a copy of the actual array. For performance reasons, use the equivalent getter returning a <see cref="SubArray{T}"/> instance, which allows you to directly access and modify the underlying array</remarks>
        [UsedImplicitly]
        [Obsolete("Use VertexNormals instead")]
        public Vector3[] VertexNormal
        {
            get => VertexNormals.CopyToArray(ArrayPools.Vector3);
            set => VertexNormals = new SubArray<Vector3>(value);
        }

        /// <summary>
        /// The <see cref="CGShape.RelativeDistances"/> of the <see cref="CGShape"/> used in the extrusion of this volume
        /// </summary>
        /// <remarks>This getter returns a copy of the actual array. For performance reasons, use the equivalent getter returning a <see cref="SubArray{T}"/> instance, which allows you to directly access and modify the underlying array</remarks>
        [UsedImplicitly]
        [Obsolete("Use CrossRelativeDistances instead")]
        public float[] CrossF
        {
            get => CrossRelativeDistances.CopyToArray(ArrayPools.Single);
            set => CrossRelativeDistances = new SubArray<float>(value);
        }

        /// <summary>
        /// The <see cref="CGShape.CustomValues"/> of the <see cref="CGShape"/> used in the extrusion of this volume
        /// </summary>
        /// <remarks>This getter returns a copy of the actual array. For performance reasons, use the equivalent getter returning a <see cref="SubArray{T}"/> instance, which allows you to directly access and modify the underlying array</remarks>
        [UsedImplicitly]
        [Obsolete("Use CrossCustomValues instead")]
        public float[] CrossMap
        {
            get => CrossCustomValues.CopyToArray(ArrayPools.Single);
            set => CrossCustomValues = new SubArray<float>(value);
        }

        #endregion

        /// <summary>
        /// Length of a given cross segment. Will be calculated on demand only!
        /// </summary>
        [UsedImplicitly]
        [Obsolete("Do not use this. Use the GetCrossLength method instead")]
        public float[] SegmentLength
        {
            get
            {
                if (_segmentLength == null)
                    _segmentLength = new float[Count];
                return _segmentLength;
            }
            set => _segmentLength = value;
        }

        /// <summary>
        /// Gets the number of cross shape's sample points
        /// </summary>
        public int CrossSize => crossRelativeDistances.Count;

        /// <summary>
        /// Whether the Cross base spline is closed or not
        /// </summary>
        public bool CrossClosed; //TODO make obsolete then remove this, it is not needed by Curvy

        /// <summary>
        /// Whether the Cross shape covers the whole length of the base spline
        /// </summary>
        public bool CrossSeamless;

        /// <summary>
        /// A shift of the <see cref="CrossRelativeDistances"/> value that is applied when using the interpolation methods on the volume, like <see cref="InterpolateVolume"/>
        /// </summary>
        public float CrossFShift;

        public SamplePointsMaterialGroupCollection CrossMaterialGroups;

        public int VertexCount => vertices.Count;

        #region private fields

        private SubArray<Vector3> vertices;
        private SubArray<Vector3> vertexNormals;
        private SubArray<float> crossRelativeDistances;
        private SubArray<float> crossCustomValues;
        private SubArray<Vector2> scales;

        [UsedImplicitly]
        [Obsolete("Do not use this. Use the GetCrossLength method instead")]
        private float[] _segmentLength;

        #endregion

        #region ### Constructors ###

        [UsedImplicitly]
        [Obsolete("Use one of the other constructors")]
        public CGVolume() { }

        public CGVolume(int samplePoints, CGShape crossShape)
        {
            crossRelativeDistances = ArrayPools.Single.Clone(crossShape.RelativeDistances);
            crossCustomValues = ArrayPools.Single.Clone(crossShape.CustomValues);
            scales = ArrayPools.Vector2.Allocate(samplePoints);
            CrossClosed = crossShape.Closed;
            CrossSeamless = crossShape.Seamless;
            CrossMaterialGroups = new SamplePointsMaterialGroupCollection(crossShape.MaterialGroups);
            vertices = ArrayPools.Vector3.Allocate(CrossSize * samplePoints);
            vertexNormals = ArrayPools.Vector3.Allocate(vertices.Count);
        }

        public CGVolume(CGPath path, CGShape crossShape)
            : base(path)
        {
            crossRelativeDistances = ArrayPools.Single.Clone(crossShape.RelativeDistances);
            crossCustomValues = ArrayPools.Single.Clone(crossShape.CustomValues);
            scales = ArrayPools.Vector2.Allocate(Count);
            CrossClosed = crossShape.Closed;
            CrossSeamless = crossShape.Seamless;
            CrossMaterialGroups = new SamplePointsMaterialGroupCollection(crossShape.MaterialGroups);
            vertices = ArrayPools.Vector3.Allocate(CrossSize * Count);
            vertexNormals = ArrayPools.Vector3.Allocate(vertices.Count);
        }

        public CGVolume(CGVolume source)
            : base(source)
        {
            vertices = ArrayPools.Vector3.Clone(source.vertices);
            vertexNormals = ArrayPools.Vector3.Clone(source.vertexNormals);
            crossRelativeDistances = ArrayPools.Single.Clone(source.crossRelativeDistances);
            crossCustomValues = ArrayPools.Single.Clone(source.crossCustomValues);
            scales = ArrayPools.Vector2.Clone(source.scales);
            CrossClosed = source.Closed;
            CrossSeamless = source.CrossSeamless;
            CrossFShift = source.CrossFShift;
            CrossMaterialGroups = new SamplePointsMaterialGroupCollection(source.CrossMaterialGroups);
        }

        #endregion

        protected override bool Dispose(bool disposing)
        {
            bool result = base.Dispose(disposing);
            if (result)
            {
                ArrayPools.Vector3.Free(vertices);
                ArrayPools.Vector3.Free(vertexNormals);
                ArrayPools.Single.Free(crossRelativeDistances);
                ArrayPools.Single.Free(crossCustomValues);
                ArrayPools.Vector2.Free(scales);
#pragma warning disable 618
                if (SegmentLength != null)
                    ArrayPools.Single.Free(SegmentLength);
#pragma warning restore 618
            }

            return result;
        }

        /// <summary>
        /// Returns a CGVolume made from the given CGPath and CGShape
        /// </summary>
        /// <param name="data">If not null, the returned instance will be the one but with its fields updated. If null, a new instance will be created</param>
        /// <param name="path">The path used in the creation of the volume</param>
        /// <param name="crossShape">The shape used in the creation of the volume</param>
        /// <returns></returns>
        [NotNull]
        public static CGVolume Get([CanBeNull] CGVolume data, CGPath path, CGShape crossShape)
        {
            if (data == null)
                return new CGVolume(
                    path,
                    crossShape
                );

            Copy(
                data,
                path
            );

#pragma warning disable 618
            if (data._segmentLength != null)
                data.SegmentLength = new float[data.Count];
#pragma warning restore 618

            //todo optim use ResizeCopyless?

            // Volume
            ArrayPools.Single.Resize(
                ref data.crossRelativeDistances,
                crossShape.RelativeDistances.Count,
                false
            );
            Array.Copy(
                crossShape.RelativeDistances.Array,
                0,
                data.crossRelativeDistances.Array,
                0,
                crossShape.RelativeDistances.Count
            );

            ArrayPools.Single.Resize(
                ref data.crossCustomValues,
                crossShape.CustomValues.Count,
                false
            );
            Array.Copy(
                crossShape.CustomValues.Array,
                0,
                data.crossCustomValues.Array,
                0,
                crossShape.CustomValues.Count
            );

            ArrayPools.Vector2.Resize(
                ref data.scales,
                path.Count,
                false
            );

            data.CrossClosed = crossShape.Closed;
            data.CrossSeamless = crossShape.Seamless;
            data.CrossMaterialGroups = new SamplePointsMaterialGroupCollection(crossShape.MaterialGroups);
            ArrayPools.Vector3.Resize(
                ref data.vertices,
                data.CrossSize * data.Positions.Count,
                false
            );
            ArrayPools.Vector3.Resize(
                ref data.vertexNormals,
                data.vertices.Count,
                false
            );
            return data;
        }


        public override T Clone<T>()
            => new CGVolume(this) as T;


        public void InterpolateVolume(float f, float crossF, out Vector3 pos, out Vector3 dir, out Vector3 up)
        {
            float frag;
            float cfrag;
            int v0Idx = GetVertexIndex(
                f,
                crossF,
                out frag,
                out cfrag
            );

            // (2)-(3)
            //  | \ |
            // (0)-(1)
            Vector3 xd, zd;
            Vector3 v0 = vertices.Array[v0Idx];
            Vector3 v1 = vertices.Array[v0Idx + 1];
            Vector3 v2 = vertices.Array[v0Idx + CrossSize];

            if (frag + cfrag > 1)
            {
                Vector3 v3 = vertices.Array[v0Idx + CrossSize + 1];
                xd = v3 - v2;
                zd = v3 - v1;
                pos = (v2 - (zd * (1 - frag))) + (xd * cfrag);
            }
            else
            {
                xd = v1 - v0;
                zd = v2 - v0;
                pos = v0 + (zd * frag) + (xd * cfrag);
            }

            dir = zd.normalized;
            up = Vector3.Cross(
                zd,
                xd
            );
        }

        public Vector3 InterpolateVolumePosition(float f, float crossF)
        {
            float frag;
            float cfrag;
            int v0Idx = GetVertexIndex(
                f,
                crossF,
                out frag,
                out cfrag
            );
            // (2)-(3)
            //  | \ |
            // (0)-(1)
            Vector3 xd, zd;
            Vector3 v0 = vertices.Array[v0Idx];
            Vector3 v1 = vertices.Array[v0Idx + 1];
            Vector3 v2 = vertices.Array[v0Idx + CrossSize];

            if (frag + cfrag > 1)
            {
                Vector3 v3 = vertices.Array[v0Idx + CrossSize + 1];
                xd = v3 - v2;
                zd = v3 - v1;
                return (v2 - (zd * (1 - frag))) + (xd * cfrag);
            }

            xd = v1 - v0;
            zd = v2 - v0;
            return v0 + (zd * frag) + (xd * cfrag);
        }

        public Vector3 InterpolateVolumeDirection(float f, float crossF)
        {
            float frag;
            float cfrag;
            int v0Idx = GetVertexIndex(
                f,
                crossF,
                out frag,
                out cfrag
            );

            // (2)-(3)
            //  | \ |
            // (0)-(1)
            if (frag + cfrag > 1)
            {
                Vector3 v1 = vertices.Array[v0Idx + 1];
                Vector3 v3 = vertices.Array[v0Idx + CrossSize + 1];
                return (v3 - v1).normalized;
            }

            Vector3 v0 = vertices.Array[v0Idx];
            Vector3 v2 = vertices.Array[v0Idx + CrossSize];
            return (v2 - v0).normalized;
        }

        public Vector3 InterpolateVolumeUp(float f, float crossF)
        {
            float frag;
            float cfrag;
            int v0Idx = GetVertexIndex(
                f,
                crossF,
                out frag,
                out cfrag
            );

            // (2)-(3)
            //  | \ |
            // (0)-(1)
            Vector3 xd, zd;

            Vector3 v1 = vertices.Array[v0Idx + 1];
            Vector3 v2 = vertices.Array[v0Idx + CrossSize];

            if (frag + cfrag > 1)
            {
                Vector3 v3 = vertices.Array[v0Idx + CrossSize + 1];
                xd = v3 - v2;
                zd = v3 - v1;
            }
            else
            {
                Vector3 v0 = vertices.Array[v0Idx];
                xd = v1 - v0;
                zd = v2 - v0;
            }

            return Vector3.Cross(
                zd,
                xd
            );
        }

        public float GetCrossLength(float pathF)
        {
            int s0;
            int s1;
            float frag;
#pragma warning disable 618
            GetSegmentIndices(
                pathF,
                out s0,
                out s1,
                out frag
            );
#pragma warning restore 618

#pragma warning disable 618
            if (SegmentLength[s0] == 0)
                SegmentLength[s0] = calcSegmentLength(s0);
            if (SegmentLength[s1] == 0)
                SegmentLength[s1] = calcSegmentLength(s1);

            return Mathf.LerpUnclamped(
                SegmentLength[s0],
                SegmentLength[s1],
                frag
            );
#pragma warning restore 618
        }


        public float CrossFToDistance(float f, float crossF, CurvyClamping crossClamping = CurvyClamping.Clamp)
            => GetCrossLength(f)
            * CurvyUtility.ClampTF(
                crossF,
                crossClamping
            );

        public float CrossDistanceToF(float f, float distance, CurvyClamping crossClamping = CurvyClamping.Clamp)
        {
            float cl = GetCrossLength(f);
            return CurvyUtility.ClampDistance(
                       distance,
                       crossClamping,
                       cl
                   )
                   / cl;
        }

        /// <summary>
        /// Get the indices of the two points on the path that are surrounding the point at pathF
        /// </summary>
        /// <param name="pathF">The relative distance of the input point on the path</param>
        /// <param name="segment0Index">Index of the path point just before the input point </param>
        /// <param name="segment1Index">Index of the path point just after the input point</param>
        /// <param name="frag">The interpolation value between segment0Index and segment1Index, defining the exact position of the input point between those two points</param>
        [UsedImplicitly]
        [Obsolete("Method will get removed. Copy its content if you still need it")]
        public void GetSegmentIndices(float pathF, out int segment0Index, out int segment1Index, out float frag)
        {
            segment0Index = GetFIndex(
                Mathf.Repeat(
                    pathF,
                    1
                ),
                out frag
            );
            segment1Index = segment0Index + 1;
        }

        public int GetSegmentIndex(int segment)
            => segment * CrossSize;

        public int GetCrossFIndex(float crossF, out float frag)
        {
            float f = crossF + CrossFShift;
            //OPTIM if f is always positive, replace repeat with %. Right now crossF can be negative
            f = f == 1
                ? f
                : Mathf.Repeat(
                    f,
                    1
                );
            int index = getGenericFIndex(
                crossRelativeDistances,
                f,
                out frag
            );

            return index;
        }

        /// <summary>
        /// Get the index of the first vertex belonging to the segment a certain F is part of
        /// </summary>
        /// <param name="pathF">position on the path (0..1)</param>
        /// <param name="pathFrag">remainder between the returned segment and the next segment</param>
        /// <returns>a vertex index</returns>
        public int GetVertexIndex(float pathF, out float pathFrag)
        {
            int pIdx = GetFIndex(
                pathF,
                out pathFrag
            );
            return pIdx * CrossSize;
        }

        /// <summary>
        /// Get the index of the first vertex of the edge a certain F and CrossF is part of
        /// </summary>
        /// <param name="pathF">position on the path (0..1)</param>
        /// <param name="crossF">position on the cross (0..1)</param>
        /// <param name="pathFrag">remainder between the segment and the next segment</param>
        /// <param name="crossFrag">remainder between the returned vertex and the next vertex</param>
        /// <returns>a vertex index</returns>
        public int GetVertexIndex(float pathF, float crossF, out float pathFrag, out float crossFrag)
        {
            int pIdx = GetVertexIndex(
                pathF,
                out pathFrag
            );
            int cIdx = GetCrossFIndex(
                crossF,
                out crossFrag
            );
            return pIdx + cIdx;
        }

        /// <summary>
        /// Gets all vertices belonging to one or more extruded shape segments
        /// </summary>
        /// <param name="segmentIndices">indices of segments in question</param>
        public Vector3[] GetSegmentVertices(params int[] segmentIndices)
        {
            SubArray<Vector3> verts = ArrayPools.Vector3.Allocate(CrossSize * segmentIndices.Length);
            for (int i = 0; i < segmentIndices.Length; i++)
            {
                int sourceIndex = segmentIndices[i] * CrossSize;
                int destinationIndex = i * CrossSize;
                Array.Copy(
                    vertices.Array,
                    sourceIndex,
                    verts.Array,
                    destinationIndex,
                    CrossSize
                );
            }

            return verts.CopyToArray(ArrayPools.Vector3);
        }


        private float calcSegmentLength(int segmentIndex)
        {
            int vstart = segmentIndex * CrossSize;
            int vend = (vstart + CrossSize) - 1;
            float l = 0;
            for (int i = vstart; i < vend; i++)
                l += (vertices.Array[i + 1] - vertices.Array[i]).magnitude;

            return l;
        }
    }
}