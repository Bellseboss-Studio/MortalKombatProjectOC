// =====================================================================
// Copyright © 2013 ToolBuddy
// All rights reserved
// 
// http://www.toolbuddy.net
// =====================================================================

using System;
using FluffyUnderware.Curvy.Pools;
using ToolBuddy.Pooling.Collections;
using UnityEngine;

namespace FluffyUnderware.Curvy
{
    /// <summary>
    /// Converts a <see cref="CurvySpline"/> to an <see cref="EdgeCollider2D"/> 
    /// </summary>
    [AddComponentMenu(ComponentPath)]
    [RequireComponent(typeof(EdgeCollider2D))]
    [HelpURL(AssetInformation.DocsRedirectionBaseUrl + "edgecollider2d")]
    public class CurvySplineToEdgeCollider2D : SplineProcessor
    {
        public const string ComponentPath = "Curvy/Converters/Curvy Spline To Edge Collider 2D";

        private EdgeCollider2D cachedEdgeCollider2D;


        private EdgeCollider2D EdgeCollider
        {
            get
            {
                if (cachedEdgeCollider2D == null)
                    cachedEdgeCollider2D = GetComponent<EdgeCollider2D>();
                return cachedEdgeCollider2D;
            }
        }

        /// <summary>
        /// Update the <see cref="EdgeCollider2D.points"/> with the cache points of the <see cref="CurvySpline"/>
        /// </summary>
        public override void Refresh()
        {
            if (Spline)
            {
                if (Spline.IsInitialized && Spline.Dirty == false)
                {
                    SubArray<Vector3> positions = Spline.GetPositionsCache(Space.Self);
                    SubArray<Vector2> positions2D = ArrayPools.Vector2.AllocateExactSize(positions.Count);
                    Vector3[] positionsArray = positions.Array;
                    Vector2[] positions2DArray = positions2D.Array;
                    for (int i = 0; i < positions.Count; i++)
                    {
                        positions2DArray[i].x = positionsArray[i].x;
                        positions2DArray[i].y = positionsArray[i].y;
                    }

                    EdgeCollider.points = positions2DArray;
                    ArrayPools.Vector2.Free(positions2D);
                    ArrayPools.Vector3.Free(positions);
                }
                else
                    EdgeCollider.points = Array.Empty<Vector2>();
            }
        }
    }
}