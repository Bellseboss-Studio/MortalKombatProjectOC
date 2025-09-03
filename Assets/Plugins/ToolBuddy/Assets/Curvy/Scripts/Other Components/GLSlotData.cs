// =====================================================================
// Copyright © 2013 ToolBuddy
// All rights reserved
// 
// http://www.toolbuddy.net
// =====================================================================

using System;
using System.Collections.Generic;
using FluffyUnderware.Curvy.Pools;
using ToolBuddy.Pooling.Collections;
using ToolBuddy.Pooling.Pools;
using UnityEngine;
using UnityEngine.Assertions;

namespace FluffyUnderware.Curvy.Components
{
    /// <summary>
    /// Helper class used by CurvyGLRenderer
    /// </summary>
    [Serializable]
    public class GLSlotData
    {
        [SerializeField]
        public CurvySpline Spline;

        public Color LineColor = CurvyGlobalManager.DefaultGizmoColor;
        public List<Vector3[]> VertexData = new List<Vector3[]>();

        public void GetVertexData()
        {
#if CURVY_SANITY_CHECKS
            Assert.IsFalse(
                ReferenceEquals(
                    Spline,
                    null
                )
            );
#endif

            VertexData.Clear();

            ArrayPool<Vector3> vector3ArrayPool = ArrayPools.Vector3;

            if (Spline.IsInitialized)
            {
                if (Spline.Dirty)
                    Spline.Refresh();

                SubArray<Vector3> positionsCache = Spline.GetPositionsCache(Space.World);
                //OPTIM avoid the CopyToArray call, make the class work with SubArrays
                VertexData.Add(positionsCache.CopyToArray(vector3ArrayPool));
                vector3ArrayPool.Free(positionsCache);
            }
        }

        public void Render(Material mat)
        {
            for (int i = 0; i < VertexData.Count; i++)
                if (VertexData[i].Length > 0)
                {
                    mat.SetPass(0);
                    GL.Begin(GL.LINES);
                    GL.Color(LineColor);
                    for (int v = 1; v < VertexData[i].Length; v++)
                    {
                        GL.Vertex(VertexData[i][v - 1]);
                        GL.Vertex(VertexData[i][v]);
                    }

                    GL.End();
                }
        }
    }
}