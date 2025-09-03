// =====================================================================
// Copyright © 2013 ToolBuddy
// All rights reserved
// 
// http://www.toolbuddy.net
// =====================================================================

using System;
using System.Collections.Generic;
using FluffyUnderware.Curvy.Pools;
using FluffyUnderware.Curvy.ThirdParty.LibTessDotNet;
using ToolBuddy.Pooling;
using ToolBuddy.Pooling.Collections;
using ToolBuddy.Pooling.Pools;
using UnityEngine;

namespace FluffyUnderware.Curvy.Utils
{
    /// <summary>
    /// Class to create a Mesh from a set of splines
    /// </summary>
    public class Spline2Mesh
    {
        #region ### Public Fields & Properties ###

        /// <summary>
        /// A list of splines (X/Y only) forming the resulting mesh
        /// </summary>
        public List<SplinePolyLine> Lines = new List<SplinePolyLine>();

        /// <summary>
        /// Winding rule used by triangulator
        /// </summary>
        public WindingRule Winding = WindingRule.EvenOdd;

        public Vector2 UVTiling = Vector2.one;
        public Vector2 UVOffset = Vector2.zero;
        public bool SuppressUVMapping;

        /// <summary>
        /// Whether UV2 should be set
        /// </summary>
        public bool UV2;

        /// <summary>
        /// Name of the returned mesh
        /// </summary>
        public string MeshName = string.Empty;

        /// <summary>
        /// Whether only vertices of the outline spline should be created
        /// </summary>
        public bool VertexLineOnly;

        public string Error { get; private set; }

        #endregion

        #region ### Private Fields ###

        private Tess mTess;
        private Mesh mMesh;

        #endregion

        #region ### Public Methods ###

        /// <summary>
        /// Create the Mesh using the current settings
        /// </summary>
        /// <param name="result">the resulting Mesh</param>
        /// <returns>true on success. If false, check the Error property!</returns>
        public bool Apply(out Mesh result)
        {
            ArrayPool<Vector3> pool = ArrayPoolsProvider.GetPool<Vector3>();

            mTess = null;
            mMesh = null;
            Error = string.Empty;
            bool triangulationSucceeded = triangulate();
            if (triangulationSucceeded)
            {
                mMesh = new Mesh();
                mMesh.name = MeshName;

                if (VertexLineOnly && Lines.Count > 0 && Lines[0] != null)
                {
                    SubArray<Vector3> vertices = Lines[0].GetVertexList();
                    mMesh.SetVertices(
                        vertices.Array,
                        0,
                        vertices.Count
                    );
                    pool.Free(vertices);
                }
                else
                {
                    ContourVertex[] vertices = mTess.Vertices;
                    SubArray<Vector3> vector3s = pool.Allocate(vertices.Length);
                    UnityLibTessUtility.FromContourVertex(
                        vertices,
                        vector3s
                    );
                    mMesh.SetVertices(
                        vector3s.Array,
                        0,
                        vector3s.Count
                    );
                    mMesh.SetTriangles(
                        mTess.ElementsArray.Value.Array,
                        0,
                        mTess.ElementsArray.Value.Count,
                        0
                    );
                    pool.Free(vector3s);
                }

                mMesh.RecalculateBounds();
                mMesh.RecalculateNormals();
                if (!SuppressUVMapping && !VertexLineOnly)
                {
                    Vector3 boundsSize = mMesh.bounds.size;
                    Vector3 boundsMin = mMesh.bounds.min;

                    float minSize = Mathf.Min(
                        boundsSize.x,
                        Mathf.Min(
                            boundsSize.y,
                            boundsSize.z
                        )
                    );

                    bool minSizeIsX = minSize == boundsSize.x;
                    bool minSizeIsY = minSize == boundsSize.y;
                    bool minSizeIsZ = minSize == boundsSize.z;

                    Vector3[] vertices = mMesh.vertices;
                    int vertexCount = vertices.Length;

                    //set uv and uv2
                    SubArray<Vector2> uv;
                    SubArray<Vector2> uv2;
                    {
                        uv = ArrayPools.Vector2.Allocate(vertexCount);
                        Vector2[] uvArray = uv.Array;

                        uv2 = ArrayPools.Vector2.Allocate(
                            UV2
                                ? vertexCount
                                : 0
                        );
                        Vector2[] uv2Array = uv2.Array;

                        for (int i = 0; i < vertexCount; i++)
                        {
                            float u;
                            float v;
                            Vector3 vertex = vertices[i];

                            if (minSizeIsX)
                            {
                                u = (vertex.y - boundsMin.y) / boundsSize.y;
                                v = (vertex.z - boundsMin.z) / boundsSize.z;
                            }
                            else if (minSizeIsY)
                            {
                                u = (vertex.z - boundsMin.z) / boundsSize.z;
                                v = (vertex.x - boundsMin.x) / boundsSize.x;
                            }
                            else if (minSizeIsZ)
                            {
                                u = (vertex.x - boundsMin.x) / boundsSize.x;
                                v = (vertex.y - boundsMin.y) / boundsSize.y;
                            }
                            else
                                throw new InvalidOperationException("Couldn't find the minimal bound dimension");

                            if (UV2)
                            {
                                uv2Array[i].x = u;
                                uv2Array[i].y = v;
                            }

                            u += UVOffset.x;
                            v += UVOffset.y;

                            u *= UVTiling.x;
                            v *= UVTiling.y;
                            uvArray[i].x = u;
                            uvArray[i].y = v;
                        }

                        mMesh.SetUVs(
                            0,
                            uv.Array,
                            0,
                            uv.Count
                        );
                        mMesh.SetUVs(
                            1,
                            uv2.Array,
                            0,
                            uv2.Count
                        );
                    }

                    ArrayPools.Vector2.Free(uv);
                    ArrayPools.Vector2.Free(uv2);
                    ArrayPools.Vector3.Free(vertices);
                }
            }

            result = mMesh;
            return triangulationSucceeded;
        }

        #endregion

        #region ### Privates ###

#if DOCUMENTATION___FORCE_IGNORE___CURVY == false

        private bool triangulate()
        {
            if (Lines.Count == 0)
            {
                Error = "Missing splines to triangulate";
                return false;
            }

            if (VertexLineOnly)
                return true;

            mTess = new Tess();

            for (int i = 0; i < Lines.Count; i++)
            {
                if (Lines[i].Spline == null)
                {
                    Error = "Missing Spline";
                    return false;
                }

                if (!polyLineIsValid(Lines[i]))
                {
                    Error = Lines[i].Spline.name + ": Angle must be >0";
                    return false;
                }

                SubArray<Vector3> vertices = Lines[i].GetVertexList();
                if (vertices.Count < 3)
                {
                    Error = Lines[i].Spline.name + ": At least 3 Vertices needed!";
                    return false;
                }

                mTess.AddContour(
                    UnityLibTessUtility.ToContourVertex(vertices),
                    Lines[i].Orientation
                );
                ArrayPoolsProvider.GetPool<Vector3>().Free(vertices);
            }

            try
            {
                mTess.Tessellate(
                    Winding,
                    ElementType.Polygons,
                    3
                );
                return true;
            }
            catch (Exception e)
            {
                Error = e.Message;
            }

            return false;
        }

        private static bool polyLineIsValid(SplinePolyLine pl)
            => (pl != null && pl.VertexMode == SplinePolyLine.VertexCalculation.ByApproximation)
            || !Mathf.Approximately(
                0,
                pl.Angle
            );

#endif

        #endregion
    }
}