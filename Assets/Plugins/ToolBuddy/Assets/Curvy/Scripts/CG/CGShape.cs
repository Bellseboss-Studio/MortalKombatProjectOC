// =====================================================================
// Copyright © 2013 ToolBuddy
// All rights reserved
// 
// http://www.toolbuddy.net
// =====================================================================

using System;
using System.Collections.Generic;
using System.Linq;
using FluffyUnderware.Curvy.Pools;
using FluffyUnderware.Curvy.Utils;
using JetBrains.Annotations;
using ToolBuddy.Pooling.Collections;
using ToolBuddy.Pooling.Pools;
using UnityEngine;
using UnityEngine.Assertions;

namespace FluffyUnderware.Curvy.Generator
{
    /// <summary>
    /// Rasterized Shape Data (Polyline)
    /// </summary>
    [CGDataInfo(
        0.73f,
        0.87f,
        0.98f
    )]
    public class CGShape : CGData
    {
        /// <summary>
        /// The relative distance of each point.
        /// A relative distance is a value between 0 and 1 representing how far the point is in a shape.
        /// A value of 0 means the start of the shape, and a value of 1 means the end of it.
        /// It is defined as (the point's distance from the shape's start) / (the total length of the shape)
        /// This is unrelated to the notion of <seealso cref="CurvySplineSegment.TF"/> or F of a spline.
        /// Unfortunately, it is abusively called F in big parts of the the Curvy Generator related code, sorry for the confusion.
        /// </summary>
        /// <remarks>Setting a new <see cref="SubArray{T}"/> will <see cref="ArrayPool{T}.Free(ToolBuddy.Pooling.Collections.SubArray{T})"/> the current <see cref="SubArray{T}"/>  instance</remarks>
        public SubArray<float> RelativeDistances
        {
            get => relativeDistances;
            set
            {
                ArrayPools.Single.Free(relativeDistances);
                relativeDistances = value;
            }
        }

        /// <summary>
        /// The relative distance of each point relative to the source shape.
        /// A relative distance is a value between 0 and 1 representing how far the point is in a shape.
        /// A value of 0 means the start of the shape, and a value of 1 means the end of it.
        /// It is defined as (the point's distance from the shape's start) / (the total length of the shape)
        /// Contrary to <seealso cref="RelativeDistances"/> which is computed based on the actual shape, SourceRelativeDistances is computed based on the source shape.
        /// For example, if a Shape A is defined as the second quarter of a Shape B, A's first point will have a relative distance of 0, but a source relative distance of 0.25. A's last point will have a relative distance of 1, but a source relative distance of 0.5
        /// This is unrelated to the notion of <seealso cref="CurvySplineSegment.TF"/> or F of a spline.
        /// Unfortunately, it is abusively called F in big parts of the the Curvy Generator related code, sorry for the confusion.
        /// </summary>
        /// <remarks>Setting a new <see cref="SubArray{T}"/> will <see cref="ArrayPool{T}.Free(ToolBuddy.Pooling.Collections.SubArray{T})"/> the current <see cref="SubArray{T}"/>  instance</remarks>
        public SubArray<float> SourceRelativeDistances
        {
            get => sourceRelativeDistances;
            set
            {
                ArrayPools.Single.Free(sourceRelativeDistances);
                sourceRelativeDistances = value;
            }
        }

        /// <summary>
        /// Positions of the path's points, in the path's local space
        /// </summary>
        /// <remarks>Setting a new <see cref="SubArray{T}"/> will <see cref="ArrayPool{T}.Free(ToolBuddy.Pooling.Collections.SubArray{T})"/> the current <see cref="SubArray{T}"/>  instance</remarks>
        public SubArray<Vector3> Positions
        {
            get => positions;
            set
            {
                ArrayPools.Vector3.Free(positions);
                positions = value;
            }
        }

        /// <summary>
        /// Normals of the path's points, in the path's local space
        /// </summary>
        /// <remarks>Setting a new <see cref="SubArray{T}"/> will <see cref="ArrayPool{T}.Free(ToolBuddy.Pooling.Collections.SubArray{T})"/> the current <see cref="SubArray{T}"/>  instance</remarks>
        public SubArray<Vector3> Normals
        {
            get => normals;
            set
            {
                ArrayPools.Vector3.Free(normals);
                normals = value;
            }
        }

        /// <summary>
        /// Arbitrary mapped value to each point, usually U coordinate
        /// </summary>
        /// <remarks>Setting a new <see cref="SubArray{T}"/> will <see cref="ArrayPool{T}.Free(ToolBuddy.Pooling.Collections.SubArray{T})"/> the current <see cref="SubArray{T}"/>  instance</remarks>
        public SubArray<float> CustomValues
        {
            get => customValues;
            set
            {
                ArrayPools.Single.Free(customValues);
                customValues = value;
            }
        }

        /// <summary>
        /// The list of the shape's <see cref="DuplicatePoints"/>
        /// </summary>
        public List<DuplicateSamplePoint> DuplicatePoints { get; set; }

        #region Obsolete

        /// <summary>
        /// The relative distance of each point.
        /// A relative distance is a value between 0 and 1 representing how far the point is in a shape.
        /// A value of 0 means the start of the shape, and a value of 1 means the end of it.
        /// It is defined as (the point's distance from the shape's start) / (the total length of the shape)
        /// This is unrelated to the notion of <seealso cref="CurvySplineSegment.TF"/> or F of a spline.
        /// Unfortunately, it is abusively called F in big parts of the the Curvy Generator related code, sorry for the confusion.
        /// </summary>
        /// <remarks>This getter returns a copy of the actual array. For performance reasons, use the equivalent getter returning a <see cref="SubArray{T}"/> instance, which allows you to directly access and modify the underlying array</remarks>
        [UsedImplicitly]
        [Obsolete("Use RelativeDistances instead")]
        public float[] F
        {
            get => RelativeDistances.CopyToArray(ArrayPools.Single);
            set => RelativeDistances = new SubArray<float>(value);
        }

        /// <summary>
        /// The relative distance of each point relative to the source shape.
        /// A relative distance is a value between 0 and 1 representing how far the point is in a shape.
        /// A value of 0 means the start of the shape, and a value of 1 means the end of it.
        /// It is defined as (the point's distance from the shape's start) / (the total length of the shape)
        /// Contrary to <seealso cref="RelativeDistances"/> which is computed based on the actual shape, SourceRelativeDistances is computed based on the source shape.
        /// For example, if a Shape A is defined as the second quarter of a Shape B, A's first point will have a relative distance of 0, but a source relative distance of 0.25. A's last point will have a relative distance of 1, but a source relative distance of 0.5
        /// This is unrelated to the notion of <seealso cref="CurvySplineSegment.TF"/> or F of a spline.
        /// Unfortunately, it is abusively called F in big parts of the the Curvy Generator related code, sorry for the confusion.
        /// </summary>
        /// <remarks>This getter returns a copy of the actual array. For performance reasons, use the equivalent getter returning a <see cref="SubArray{T}"/> instance, which allows you to directly access and modify the underlying array</remarks>
        [UsedImplicitly]
        [Obsolete("Use SourceRelativeDistances instead")]
        public float[] SourceF
        {
            get => SourceRelativeDistances.CopyToArray(ArrayPools.Single);
            set => SourceRelativeDistances = new SubArray<float>(value);
        }

        /// <summary>
        /// Positions of the path's points, in the path's local space
        /// </summary>
        /// <remarks>This getter returns a copy of the actual array. For performance reasons, use the equivalent getter returning a <see cref="SubArray{T}"/> instance, which allows you to directly access and modify the underlying array</remarks>
        [UsedImplicitly]
        [Obsolete("Use Positions instead")]
        public Vector3[] Position
        {
            get => Positions.CopyToArray(ArrayPools.Vector3);
            set => Positions = new SubArray<Vector3>(value);
        }

        /// <summary>
        /// Normals of the path's points, in the path's local space
        /// </summary>
        /// <remarks>This getter returns a copy of the actual array. For performance reasons, use the equivalent getter returning a <see cref="SubArray{T}"/> instance, which allows you to directly access and modify the underlying array</remarks>
        [UsedImplicitly]
        [Obsolete("Use Normals instead")]
        public Vector3[] Normal
        {
            get => Normals.CopyToArray(ArrayPools.Vector3);
            set => Normals = new SubArray<Vector3>(value);
        }

        /// <summary>
        /// Arbitrary mapped value to each point, usually U coordinate
        /// </summary>
        /// <remarks>This getter returns a copy of the actual array. For performance reasons, use the equivalent getter returning a <see cref="SubArray{T}"/> instance, which allows you to directly access and modify the underlying array</remarks>
        [UsedImplicitly]
        [Obsolete("Use CustomValues instead")]
        public float[] Map
        {
            get => CustomValues.CopyToArray(ArrayPools.Single);
            set => CustomValues = new SubArray<float>(value);
        }

        #endregion

        /// <summary>
        /// Groups/Patches
        /// </summary>
        public List<SamplePointsMaterialGroup> MaterialGroups;

        /// <summary>
        /// Whether the source is managed or not
        /// </summary>
        /// <remarks>This could be used to determine if values needs to be transformed into generator space or not</remarks>
        public bool SourceIsManaged;

        /// <summary>
        /// Whether the base spline is closed or not
        /// </summary>
        public bool Closed;

        /// <summary>
        /// Whether the Shape/Path is seamless, i.e. Closed==true and the whole length is covered
        /// </summary>
        public bool Seamless;

        /// <summary>
        /// Length in world units
        /// </summary>
        public float Length;

        /// <summary>
        /// Gets the number of sample points
        /// </summary>
        public override int Count => relativeDistances.Count;

        #region ### Private fields ###

        //TODO Debug time checks that F arrays contain values between 0 and 1
        private SubArray<float> relativeDistances;

        //OPTIM can the storage of this array be avoided by storing only SourceF and the start and end Distance, and infer F values only when needed?
        //OPTIM can we just assign SourceF to F when start and end distances are equal to respectively 0 and 1? (which is the case most of the time)
        private SubArray<float> sourceRelativeDistances;
        private SubArray<Vector3> positions;

        private SubArray<Vector3> normals;

        /*TODO Map is defined in CGShape but:
        1- filling it inside an instance of CGPath (which inherits from CGShape) is useless, since Map is used only by CGVolume when it takes it from a CGShape, and not a CGPath. So an optimization would be to not fill Map for instances not consumed by CGVolume
        2- I hope that storing it might be not needed, and calculating it only when needed might be possible
       */
        private SubArray<float> customValues;

        // Caching
        //TODO DESIGN OPTIM are these still needed, now that GetFIndex was greatly optimized?
        private float mCacheLastF = float.MaxValue;
        private int mCacheLastIndex;
        private float mCacheLastFrag;

        #endregion

        public CGShape()
        {
            sourceRelativeDistances = ArrayPools.Single.Allocate(0);
            relativeDistances = ArrayPools.Single.Allocate(0);
            positions = ArrayPools.Vector3.Allocate(0);
            normals = ArrayPools.Vector3.Allocate(0);
            customValues = ArrayPools.Single.Allocate(0);
            DuplicatePoints = new List<DuplicateSamplePoint>();
            MaterialGroups = new List<SamplePointsMaterialGroup>();
        }

        public CGShape(CGShape source)
        {
            positions = ArrayPools.Vector3.Clone(source.positions);
            normals = ArrayPools.Vector3.Clone(source.normals);
            customValues = ArrayPools.Single.Clone(source.customValues);
            DuplicatePoints = new List<DuplicateSamplePoint>(source.DuplicatePoints);
            relativeDistances = ArrayPools.Single.Clone(source.relativeDistances);
            sourceRelativeDistances = ArrayPools.Single.Clone(source.sourceRelativeDistances);
            MaterialGroups = new List<SamplePointsMaterialGroup>(source.MaterialGroups.Count);
            foreach (SamplePointsMaterialGroup materialGroup in source.MaterialGroups)
                MaterialGroups.Add(materialGroup.Clone());
            Closed = source.Closed;
            Seamless = source.Seamless;
            Length = source.Length;
            SourceIsManaged = source.SourceIsManaged;
        }

        protected override bool Dispose(bool disposing)
        {
            bool result = base.Dispose(disposing);
            if (result)
            {
                ArrayPools.Single.Free(sourceRelativeDistances);
                ArrayPools.Single.Free(relativeDistances);
                ArrayPools.Vector3.Free(positions);
                ArrayPools.Vector3.Free(normals);
                ArrayPools.Single.Free(customValues);
            }

            return result;
        }

        public override T Clone<T>()
            => new CGShape(this) as T;

        public static void Copy(CGShape dest, CGShape source)
        {
            //todo optim use ResizeCopyless?

            ArrayPools.Vector3.Resize(
                ref dest.positions,
                source.positions.Count
            );
            Array.Copy(
                source.positions.Array,
                0,
                dest.positions.Array,
                0,
                source.positions.Count
            );
            ArrayPools.Vector3.Resize(
                ref dest.normals,
                source.normals.Count
            );
            Array.Copy(
                source.normals.Array,
                0,
                dest.normals.Array,
                0,
                source.normals.Count
            );
            ArrayPools.Single.Resize(
                ref dest.customValues,
                source.customValues.Count
            );
            Array.Copy(
                source.customValues.Array,
                0,
                dest.customValues.Array,
                0,
                source.customValues.Count
            );
            ArrayPools.Single.Resize(
                ref dest.relativeDistances,
                source.relativeDistances.Count
            );
            Array.Copy(
                source.relativeDistances.Array,
                0,
                dest.relativeDistances.Array,
                0,
                source.relativeDistances.Count
            );
            ArrayPools.Single.Resize(
                ref dest.sourceRelativeDistances,
                source.sourceRelativeDistances.Count
            );
            Array.Copy(
                source.sourceRelativeDistances.Array,
                0,
                dest.sourceRelativeDistances.Array,
                0,
                source.sourceRelativeDistances.Count
            );
            dest.DuplicatePoints.Clear();
            dest.DuplicatePoints.AddRange(source.DuplicatePoints);
            dest.MaterialGroups = source.MaterialGroups.Select(g => g.Clone()).ToList();
            dest.Closed = source.Closed;
            dest.Seamless = source.Seamless;
            dest.Length = source.Length;
        }

        //TODO documentation and whatnot
        public void Copy(CGShape source) =>
            Copy(
                this,
                source
            );

        /// <summary>
        /// Converts absolute (World Units) to relative (F) distance
        /// </summary>
        /// <param name="distance">distance in world units</param>
        /// <returns>Relative distance (0..1)</returns>
        public float DistanceToF(float distance)
            => Mathf.Clamp(
                   distance,
                   0,
                   Length
               )
               / Length;

        /// <summary>
        /// Converts relative (F) to absolute distance (World Units)
        /// </summary>
        /// <param name="f">relative distance (0..1)</param>
        /// <returns>Distance in World Units</returns>
        public float FToDistance(float f)
            => Mathf.Clamp01(f) * Length;

        /// <summary>
        /// Gets the index of a certain F
        /// </summary>
        /// <param name="f">F (0..1)</param>
        /// <param name="frag">fragment between the resulting and the next index (0..1)</param>
        /// <returns>the resulting index</returns>
        public int GetFIndex(float f, out float frag)
        {
#if CURVY_SANITY_CHECKS_PRIVATE
            Assert.IsTrue(f >= 0);
            if (f > 1)
                Debug.LogWarning(f);
#endif
            if (mCacheLastF != f)
            {
                mCacheLastF = f;
                //OPTIM make sure f is a ratio, then remove the following line
                float fValue = f == 1
                    ? f
                    : f % 1;
                mCacheLastIndex = getGenericFIndex(
                    relativeDistances,
                    fValue,
                    out mCacheLastFrag
                );
            }

            frag = mCacheLastFrag;

            return mCacheLastIndex;
        }

        /*
        /// <summary>
        /// Gets the index of a certain SourceF
        /// </summary>
        /// <param name="sourceF">F (0..1)</param>
        /// <param name="frag">fragment between the resulting and the next index (0..1)</param>
        /// <returns>the resulting index</returns>
        public int GetSourceFIndex(float sourceF, out float frag)
        {
            if (mCacheLastSourceF != sourceF)
            {
                mCacheLastSourceF = sourceF;

                mCacheLastSourceIndex = getGenericFIndex(ref F, sourceF, out mCacheLastSourceFrag);
            }
            frag = mCacheLastSourceFrag;
            return mCacheLastSourceIndex;
        }
        */
        /// <summary>
        /// Interpolates Position by F
        /// </summary>
        /// <param name="f">0..1</param>
        /// <returns>the interpolated position</returns>
        public Vector3 InterpolatePosition(float f)
        {
            float frag;
            int idx = GetFIndex(
                f,
                out frag
            );
            return OptimizedOperators.LerpUnclamped(
                positions.Array[idx],
                positions.Array[idx + 1],
                frag
            );
        }

        /// <summary>
        /// Interpolates Normal by F
        /// </summary>
        /// <param name="f">0..1</param>
        /// <returns>the interpolated normal</returns>
        public Vector3 InterpolateUp(float f)
        {
            float frag;
            int idx = GetFIndex(
                f,
                out frag
            );
            return Vector3.SlerpUnclamped(
                normals.Array[idx],
                normals.Array[idx + 1],
                frag
            );
        }

        /// <summary>
        /// Interpolates Position and Normal by F
        /// </summary>
        /// <param name="f">0..1</param>
        /// <param name="position"></param>
        /// <param name="up">a.k.a normal</param>
        public void Interpolate(float f, out Vector3 position, out Vector3 up)
        {
            float frag;
            int idx = GetFIndex(
                f,
                out frag
            );
            position = OptimizedOperators.LerpUnclamped(
                positions.Array[idx],
                positions.Array[idx + 1],
                frag
            );
            up = Vector3.SlerpUnclamped(
                normals.Array[idx],
                normals.Array[idx + 1],
                frag
            );
        }

        public void Move(ref float f, ref int direction, float speed, CurvyClamping clamping) =>
            f = CurvyUtility.ClampTF(
                f + (speed * direction),
                ref direction,
                clamping
            );

        public void MoveBy(ref float f, ref int direction, float speedDist, CurvyClamping clamping)
        {
            float dist = CurvyUtility.ClampDistance(
                FToDistance(f) + (speedDist * direction),
                ref direction,
                clamping,
                Length
            );
            f = DistanceToF(dist);
        }

        /// <summary>
        /// Recalculate Length and RelativeDistances (by measuring a polyline built from all Position points)
        /// </summary>
        /// <remarks>Call this after TRS'ing a shape</remarks>
        public virtual void Recalculate()
        {
            Length = 0;
            SubArray<float> dist = ArrayPools.Single.Allocate(Count);

            for (int i = 1; i < Count; i++)
                dist.Array[i] = dist.Array[i - 1] + positions.Array[i].Subtraction(positions.Array[i - 1]).magnitude;

            if (Count > 0)
            {
                Length = dist.Array[Count - 1];
                if (Length > 0)
                {
                    relativeDistances.Array[0] = 0;
                    float oneOnLength = 1 / Length;
                    for (int i = 1; i < Count - 1; i++)
                        relativeDistances.Array[i] = dist.Array[i] * oneOnLength;
                    relativeDistances.Array[Count - 1] = 1;
                }
                else
                    ArrayPools.Single.ResizeAndClear(
                        ref relativeDistances,
                        Count
                    );
            }

            ArrayPools.Single.Free(dist);

            //for (int i = 1; i < Count; i++)
            //    Direction[i] = (Position[i] - Position[i - 1]).normalized;
        }

        [UsedImplicitly]
        [Obsolete("Use another overload of RecalculateNormals instead")]
        public void RecalculateNormals(List<int> softEdges)
        {
            //todo optim use ResizeCopyless?

            //TODO this implementation works properly with 2D shapes, but creates invalid results with 3D paths. This is ok for now because the code calls it only on shapes, but it is a ticking bomb
            //TODO document the method after fixing it
            if (normals.Count != positions.Count)
                ArrayPools.Vector3.Resize(
                    ref normals,
                    positions.Count
                );

            for (int mg = 0; mg < MaterialGroups.Count; mg++)
            for (int p = 0; p < MaterialGroups[mg].Patches.Count; p++)
            {
                SamplePointsPatch patch = MaterialGroups[mg].Patches[p];
                Vector3 t;
                for (int vt = 0; vt < patch.Count; vt++)
                {
                    int x = patch.Start + vt;
                    t = (positions.Array[x + 1] - positions.Array[x]).normalized;
                    normals.Array[x] = new Vector3(
                        -t.y,
                        t.x,
                        0
                    );
#if CURVY_SANITY_CHECKS_PRIVATE
                    if (normals.Array[x].magnitude.Approximately(1f) == false)
                        Debug.LogError(
                            $"Normal is not normalized, length was {normals.Array[x].magnitude}"
                        ); //happens if shape is not in the XY plane
#endif
                }

                t = (positions.Array[patch.End] - positions.Array[patch.End - 1]).normalized;
                normals.Array[patch.End] = new Vector3(
                    -t.y,
                    t.x,
                    0
                );
#if CURVY_SANITY_CHECKS_PRIVATE
                if (normals.Array[patch.End].magnitude.Approximately(1f) == false)
                    Debug.LogError("Normal is not normalized"); //happens if shape is not in the XY plane
#endif
            }

            // Handle soft edges
            for (int i = 0; i < softEdges.Count; i++)
            {
                int previous = softEdges.ToArray()[i] - 1;
                if (previous < 0)
                    previous = positions.Count - 1;

                int beforePrevious = previous - 1;
                if (beforePrevious < 0)
                    beforePrevious = positions.Count - 1;

                int next = softEdges.ToArray()[i] + 1;
                if (next == positions.Count)
                    next = 0;

                normals.Array[softEdges.ToArray()[i]] = Vector3.Slerp(
                    normals.Array[beforePrevious],
                    normals.Array[next],
                    0.5f
                );
                normals.Array[previous] = normals.Array[softEdges.ToArray()[i]];
            }
        }

        /// <summary>
        /// Recalculate the shape's <see cref="Normals"/> based on the spline the shape was rasterized from
        /// </summary>
        public void RecalculateNormals([NotNull] CurvySpline spline)
        {
#if CURVY_SANITY_CHECKS
            Assert.IsFalse(spline.Orientation == CurvyOrientation.None);
#endif
            if (normals.Count != positions.Count)
                ArrayPools.Vector3.Resize(
                    ref normals,
                    positions.Count
                );

            Vector3[] normalsArray = normals.Array;
            float[] floats = SourceRelativeDistances.Array;

            for (int mg = 0; mg < MaterialGroups.Count; mg++)
            for (int p = 0; p < MaterialGroups[mg].Patches.Count; p++)
            {
                SamplePointsPatch patch = MaterialGroups[mg].Patches[p];
                for (int vt = 0; vt < patch.Count; vt++)
                {
                    int x = patch.Start + vt;
                    normalsArray[x] = spline.GetOrientationUpFast(spline.DistanceToTF(spline.Length * floats[x]));
#if CURVY_SANITY_CHECKS_PRIVATE
                    if (normalsArray[x].magnitude.Approximately(1f) == false)
                        Debug.LogError(
                            $"Normal is not normalized, length was {normalsArray[x].magnitude}"
                        ); //happens if shape is not in the XY plane
#endif
                }

                normalsArray[patch.End] = spline.GetOrientationUpFast(spline.DistanceToTF(spline.Length * floats[patch.End]));
#if CURVY_SANITY_CHECKS_PRIVATE
                if (normalsArray[patch.End].magnitude.Approximately(1f) == false)
                    Debug.LogError("Normal is not normalized"); //happens if shape is not in the XY plane
#endif
            }

            // Handle soft edges
            foreach (DuplicateSamplePoint duplicateSamplePoint in DuplicatePoints)
                if (duplicateSamplePoint.IsHardEdge)
                {
                    int index = duplicateSamplePoint.StartIndex;
                    normalsArray[index] = normalsArray[Math.Max(
                        0,
                        index - 1
                    )];
                }
        }

        /// <summary>
        /// Recalculate the shape's <see cref="Normals"/> based on shape's rasterized <see cref="Positions"/>
        /// </summary>
        public void RecalculateNormals()
        {
            //TODO this implementation works properly with 2D shapes, but creates invalid results with 3D paths. This is ok for now because the code calls it only on shapes, but it is a ticking bomb
            //TODO document the method after fixing it
            if (normals.Count != positions.Count)
                ArrayPools.Vector3.Resize(
                    ref normals,
                    positions.Count
                );

            Vector3[] positionsArray = positions.Array;
            Vector3[] normalsArray = normals.Array;

            for (int mg = 0; mg < MaterialGroups.Count; mg++)
            for (int p = 0; p < MaterialGroups[mg].Patches.Count; p++)
            {
                SamplePointsPatch patch = MaterialGroups[mg].Patches[p];
                Vector3 t;
                int x;
                for (int vt = 0; vt < patch.Count; vt++)
                {
                    x = patch.Start + vt;
                    t = (positionsArray[x + 1] - positionsArray[x]).normalized;
                    //todo handle case where t = 0
                    normalsArray[x] = new Vector3(
                        -t.y,
                        t.x,
                        0
                    );
#if CURVY_SANITY_CHECKS_PRIVATE
                    if (normalsArray[x].magnitude.Approximately(1f) == false)
                        //todo sanity check fails in scene 21_CGExtrusion
                        Debug.LogError(
                            $"Normal is not normalized, length was {normalsArray[x].magnitude}"
                        ); //happens if shape is not in the XY plane or if length is 0
#endif
                }

                t = (positionsArray[patch.End] - positionsArray[patch.End - 1]).normalized;
                normalsArray[patch.End] = new Vector3(
                    -t.y,
                    t.x,
                    0
                );
#if CURVY_SANITY_CHECKS_PRIVATE
                if (normalsArray[patch.End].magnitude.Approximately(1f) == false)
                    Debug.LogError("Normal is not normalized"); //happens if shape is not in the XY plane
#endif
            }

            // Handle soft edges
            foreach (DuplicateSamplePoint duplicateSamplePoint in DuplicatePoints)
                if (duplicateSamplePoint.IsHardEdge == false)
                {
                    int previous = duplicateSamplePoint.EndIndex - 1;
                    if (previous < 0)
                        previous = positions.Count - 1;

                    int beforePrevious = previous - 1;
                    if (beforePrevious < 0)
                        beforePrevious = positions.Count - 1;

                    int next = duplicateSamplePoint.EndIndex + 1;
                    if (next == positions.Count)
                        next = 0;

                    normalsArray[duplicateSamplePoint.EndIndex] = Vector3.Slerp(
                        normalsArray[beforePrevious],
                        normalsArray[next],
                        0.5f
                    );
                    normalsArray[previous] = normalsArray[duplicateSamplePoint.EndIndex];
                }
        }
    }
}