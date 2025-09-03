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
    /// Rounded Rectangle (2D)
    /// </summary>
    [CurvyShapeInfo("2D/Rounded Rectangle")]
    [RequireComponent(typeof(CurvySpline))]
    [AddComponentMenu("Curvy/Shapes/Rounded Rectangle")]
    public class CSRoundedRectangle : CurvyShape2D
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

        [Range(
            0,
            1
        )]
        [SerializeField]
        private float m_Roundness = 0.5f;

        public float Roundness
        {
            get => m_Roundness;
            set
            {
                float v = Mathf.Clamp01(value);
                if (m_Roundness != v)
                {
                    m_Roundness = v;
                    Dirty = true;
                }
            }
        }


        protected override void ApplyShape()
        {
            base.ApplyShape();
            PrepareSpline(CurvyInterpolation.Bezier);
            bool isSquare = Roundness == 0;
            PrepareControlPoints(
                isSquare
                    ? 4
                    : 8
            );

            float hw = Width / 2;
            float hh = Height / 2;

            if (isSquare)
            {
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

                SetBezierHandles(
                    0,
                    Vector3.zero,
                    Vector3.zero,
                    Space.Self
                );
                SetBezierHandles(
                    1,
                    Vector3.zero,
                    Vector3.zero,
                    Space.Self
                );
                SetBezierHandles(
                    2,
                    Vector3.zero,
                    Vector3.zero,
                    Space.Self
                );
                SetBezierHandles(
                    3,
                    Vector3.zero,
                    Vector3.zero,
                    Space.Self
                );
            }
            else
            {
                float off = Mathf.Min(
                                hw,
                                hh
                            )
                            * Roundness;
                SetPosition(
                    0,
                    new Vector3(
                        -hw,
                        -hh + off
                    )
                );
                SetPosition(
                    1,
                    new Vector3(
                        -hw,
                        hh - off
                    )
                );
                SetPosition(
                    2,
                    new Vector3(
                        -hw + off,
                        hh
                    )
                );
                SetPosition(
                    3,
                    new Vector3(
                        hw - off,
                        hh
                    )
                );
                SetPosition(
                    4,
                    new Vector3(
                        hw,
                        hh - off
                    )
                );
                SetPosition(
                    5,
                    new Vector3(
                        hw,
                        -hh + off
                    )
                );
                SetPosition(
                    6,
                    new Vector3(
                        hw - off,
                        -hh
                    )
                );
                SetPosition(
                    7,
                    new Vector3(
                        -hw + off,
                        -hh
                    )
                );

                SetBezierHandles(
                    0,
                    Vector3.down * off,
                    Vector3.zero,
                    Space.Self
                );
                SetBezierHandles(
                    1,
                    Vector3.zero,
                    Vector3.up * off,
                    Space.Self
                );
                SetBezierHandles(
                    2,
                    Vector3.left * off,
                    Vector3.right * off,
                    Space.Self
                );
                SetBezierHandles(
                    3,
                    Vector3.zero,
                    Vector3.right * off,
                    Space.Self
                );
                SetBezierHandles(
                    4,
                    Vector3.up * off,
                    Vector3.zero,
                    Space.Self
                );
                SetBezierHandles(
                    5,
                    Vector3.zero,
                    Vector3.down * off,
                    Space.Self
                );
                SetBezierHandles(
                    6,
                    Vector3.right * off,
                    Vector3.zero,
                    Space.Self
                );
                SetBezierHandles(
                    7,
                    Vector3.zero,
                    Vector3.left * off,
                    Space.Self
                );
            }
        }
    }
}