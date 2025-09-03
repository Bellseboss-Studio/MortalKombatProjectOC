// =====================================================================
// Copyright © 2013 ToolBuddy
// All rights reserved
// 
// http://www.toolbuddy.net
// =====================================================================

using FluffyUnderware.Curvy.Pools;
using FluffyUnderware.DevTools;
using JetBrains.Annotations;
using ToolBuddy.Pooling.Collections;
using UnityEngine;

namespace FluffyUnderware.Curvy.Components
{
    /// <summary>
    /// Class to drive a LineRenderer with a CurvySpline
    /// </summary>
    [AddComponentMenu(ComponentPath)]
    [RequireComponent(typeof(LineRenderer))]
    [HelpURL(AssetInformation.DocsRedirectionBaseUrl + "curvylinerenderer")]
    public class CurvyLineRenderer : SplineProcessor
    {
        public const string ComponentPath = "Curvy/Converters/Curvy Line Renderer";

        private LineRenderer cachedLineRenderer;

        private LineRenderer LineRenderer
        {
            get
            {
                if (cachedLineRenderer == null)
                    cachedLineRenderer = GetComponent<LineRenderer>();
                return cachedLineRenderer;
            }
        }

        [UsedImplicitly]
        private void Update() =>
            EnforceWorldSpaceUsage();

        private void EnforceWorldSpaceUsage()
        {
            if (LineRenderer.useWorldSpace == false)
            {
                LineRenderer.useWorldSpace = true;
                DTLog.Log(
                    "[Curvy] CurvyLineRenderer: Line Renderer's Use World Space was overriden to true. It is required by the CurvyLineRenderer.",
                    this
                );
            }
        }

        /// <summary>
        /// Update the <see cref="UnityEngine.LineRenderer"/>'s points with the cache points of the <see cref="CurvySpline"/>
        /// </summary>
        public override void Refresh()
        {
            if (Spline)
            {
                EnforceWorldSpaceUsage();
                if (Spline.IsInitialized && Spline.Dirty == false)
                {
                    SubArray<Vector3> positions = Spline.GetPositionsCache(Space.World);
                    LineRenderer.positionCount = positions.Count;
                    LineRenderer.SetPositions(positions.Array);
                    ArrayPools.Vector3.Free(positions);
                }
                else
                    LineRenderer.positionCount = 0;
            }
        }
    }
}