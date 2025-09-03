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
    /// Rectangle Shape (2D)
    /// </summary>
    [CurvyShapeInfo("2D/Rectangle")]
    [RequireComponent(typeof(CurvySpline))]
    [AddComponentMenu("Curvy/Shapes/Rectangle")]
    public class CSRectangle : CurvyShape2D
    {
        [Positive]
        [SerializeField]
        private float m_Width = 1;

        public float Width
        {
            get => m_Width;
            set
            {
                float v = Mathf.Max(
                    0,
                    value
                );
                if (m_Width != v)
                {
                    m_Width = v;
                    Dirty = true;
                }
            }
        }

        [Positive]
        [SerializeField]
        private float m_Height = 1;

        public float Height
        {
            get => m_Height;
            set
            {
                float v = Mathf.Max(
                    0,
                    value
                );
                if (m_Height != v)
                {
                    m_Height = v;
                    Dirty = true;
                }
            }
        }


        protected override void ApplyShape()
        {
            base.ApplyShape();
            PrepareSpline(
                CurvyInterpolation.Linear,
                CurvyOrientation.Dynamic,
                1
            );
            PrepareControlPoints(4);
            float hw = Width / 2;
            float hh = Height / 2;
            SetCGHardEdges();

            SetPosition(
                0,
                new Vector3(
                    -hw,
                    -hh
                )
            );
            SetPosition(
                1,
                new Vector3(
                    -hw,
                    hh
                )
            );
            SetPosition(
                2,
                new Vector3(
                    hw,
                    hh
                )
            );
            SetPosition(
                3,
                new Vector3(
                    hw,
                    -hh
                )
            );
        }
    }
}