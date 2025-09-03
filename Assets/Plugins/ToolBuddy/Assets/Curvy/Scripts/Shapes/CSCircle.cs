// =====================================================================
// Copyright © 2013 ToolBuddy
// All rights reserved
// 
// http://www.toolbuddy.net
// =====================================================================

using FluffyUnderware.DevTools;
using UnityEngine;

namespace FluffyUnderware.Curvy.Shapes
{
    /// <summary>
    /// Circle Shape (2D)
    /// </summary>
    [CurvyShapeInfo("2D/Circle")]
    [RequireComponent(typeof(CurvySpline))]
    [AddComponentMenu("Curvy/Shapes/Circle")]
    public class CSCircle : CurvyShape2D
    {
        private const int MinCount = 2;

        [Positive(
            MinValue = MinCount,
            Tooltip = "Number of Control Points"
        )]
        [SerializeField]
        private int m_Count = 4;

        public int Count
        {
            get => m_Count;
            set
            {
                int v = Mathf.Max(
                    MinCount,
                    value
                );
                if (m_Count != v)
                {
                    m_Count = v;
                    Dirty = true;
                }
            }
        }

        [Positive]
        [SerializeField]
        private float m_Radius = 1;

        public float Radius
        {
            get => m_Radius;
            set
            {
                float v = Mathf.Max(
                    0,
                    value
                );
                if (m_Radius != v)
                {
                    m_Radius = v;
                    Dirty = true;
                }
            }
        }

        protected override void ApplyShape()
        {
            base.ApplyShape();
            PrepareSpline(CurvyInterpolation.Bezier);
            PrepareControlPoints(Count);
            float d = (360f * Mathf.Deg2Rad) / Count;
            for (int i = 0; i < Count; i++)
                Spline.ControlPointsList[i].transform.localPosition = new Vector3(
                    Mathf.Sin(d * i) * Radius,
                    Mathf.Cos(d * i) * Radius,
                    0
                );
        }
    }
}