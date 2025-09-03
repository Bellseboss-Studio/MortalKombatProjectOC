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
    /// Spiral Spline Shape
    /// </summary>
    [CurvyShapeInfo(
        "3D/Spiral",
        false
    )]
    [RequireComponent(typeof(CurvySpline))]
    [AddComponentMenu("Curvy/Shapes/Spiral")]
    public class CSSpiral : CurvyShape2D
    {
        [Positive(Tooltip = "Number of Control Points per full Circle")]
        [SerializeField]
        private int m_Count = 8;

        public int Count
        {
            get => m_Count;
            set
            {
                int v = Mathf.Max(
                    0,
                    value
                );
                if (m_Count != v)
                {
                    m_Count = v;
                    Dirty = true;
                }
            }
        }

        [Positive(Tooltip = "Number of Full Circles")]
        [SerializeField]
        private float m_Circles = 3;

        public float Circles
        {
            get => m_Circles;
            set
            {
                float v = Mathf.Max(
                    0,
                    value
                );
                if (m_Circles != v)
                {
                    m_Circles = v;
                    Dirty = true;
                }
            }
        }

        [Positive(Tooltip = "Base Radius")]
        [SerializeField]
        private float m_Radius = 5;

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

        [Label(Tooltip = "Radius Multiplicator")]
        [SerializeField]
        private AnimationCurve m_RadiusFactor = AnimationCurve.Linear(
            0,
            1,
            1,
            1
        );

        public AnimationCurve RadiusFactor
        {
            get => m_RadiusFactor;
            set
            {
                if (m_RadiusFactor != value)
                {
                    m_RadiusFactor = value;
                    Dirty = true;
                }
            }
        }

        [SerializeField]
        private AnimationCurve m_Z = AnimationCurve.Linear(
            0,
            0f,
            1,
            10f
        );

        public AnimationCurve Z
        {
            get => m_Z;
            set
            {
                if (m_Z != value)
                {
                    m_Z = value;
                    Dirty = true;
                }
            }
        }


        protected override void ApplyShape()
        {
            base.ApplyShape();
            PrepareSpline(
                CurvyInterpolation.CatmullRom,
                CurvyOrientation.Dynamic,
                50,
                false
            );
            Spline.RestrictTo2D = false;
            int cpCount = Mathf.FloorToInt(Count * Circles);
            PrepareControlPoints(cpCount);
            if (cpCount == 0)
                return;
            float d = (360f * Mathf.Deg2Rad) / Count;

            for (int i = 0; i < cpCount; i++)
            {
                float frag = i / (float)cpCount;
                float rad = Radius * RadiusFactor.Evaluate(frag);
                SetPosition(
                    i,
                    new Vector3(
                        Mathf.Sin(d * i) * rad,
                        Mathf.Cos(d * i) * rad,
                        m_Z.Evaluate(frag)
                    )
                );
            }
        }
    }
}