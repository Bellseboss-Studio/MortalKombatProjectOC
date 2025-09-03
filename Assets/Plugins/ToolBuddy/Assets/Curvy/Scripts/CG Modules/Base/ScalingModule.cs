// =====================================================================
// Copyright © 2013 ToolBuddy
// All rights reserved
// 
// http://www.toolbuddy.net
// =====================================================================

using System;
using System.Runtime.CompilerServices;
using FluffyUnderware.DevTools;
using ToolBuddy.Pooling.Collections;
using UnityEngine;

namespace FluffyUnderware.Curvy.Generator.Modules
{
    /// <summary>
    /// A base class for CG modules that wish to scale objects along a path or shape
    /// </summary>
    public abstract class ScalingModule : CGModule
    {
        #region ### Serialized Fields ###

        [Tab(
            "Scale",
            Sort = 101
        )]
        [Label("Mode")]
        [SerializeField]
        [Tooltip("What type of scaling should be applied")]
        private ScaleMode m_ScaleMode = ScaleMode.Simple;

        [FieldCondition(
            nameof(m_ScaleMode),
            ScaleMode.Advanced
        )]
        [Label("Reference")]
        [SerializeField]
        [Tooltip(
            @"Determines on what range the scale is applied:
Self: the scale is applied over the Path's active range
Source: the scale is applied over the Path's total length"
        )]
        private CGReferenceMode m_ScaleReference = CGReferenceMode.Self;

        [FieldCondition(
            nameof(m_ScaleMode),
            ScaleMode.Advanced
        )]
        [Label("Offset")]
        [SerializeField]
        [Tooltip("Scale is applied starting at this offset")]
        private float m_ScaleOffset;

        [SerializeField, Label("Uniform Scaling")]
        [Tooltip("If enabled, the same scale is applied to both X and Y axis of the cross section")]
        private bool m_ScaleUniform = true;

        [SerializeField]
        [Tooltip("The (base) value of the scaling along the cross section's X axis, and Y axis if Uniform Scaling is disabled")]
        private float m_ScaleX = 1;

        [SerializeField]
        [FieldCondition(
            nameof(m_ScaleMode),
            ScaleMode.Advanced
        )]
        [AnimationCurveEx("    Multiplier")]
        [Tooltip("Defines scale multiplier, depending on the Relative Distance (between 0 and 1) of a point on the path")]
        private AnimationCurve m_ScaleCurveX = AnimationCurve.Linear(
            0,
            1,
            1,
            1
        );

        [SerializeField]
        [FieldCondition(
            nameof(m_ScaleUniform),
            false
        )]
        [Tooltip("The (base) value of the scaling along the cross section's Y axis")]
        private float m_ScaleY = 1;

        [SerializeField]
        [FieldCondition(
            nameof(m_ScaleUniform),
            false,
            false,
            ConditionalAttribute.OperatorEnum.AND,
            "m_ScaleMode",
            ScaleMode.Advanced,
            false
        )]
        [AnimationCurveEx("    Multiplier")]
        [Tooltip("Defines scale multiplier, depending on the Relative Distance (between 0 and 1) of a point on the path")]
        private AnimationCurve m_ScaleCurveY = AnimationCurve.Linear(
            0,
            1,
            1,
            1
        );

        #endregion

        #region ### Public Properties ###

        /// <summary>
        /// <see cref="Modules.ScaleMode"/>
        /// </summary>
        public ScaleMode ScaleMode
        {
            get => m_ScaleMode;
            set
            {
                if (m_ScaleMode != value)
                {
                    m_ScaleMode = value;
                    Dirty = true;
                }
            }
        }

        /// <summary>
        /// Determines on what range the scale is applied:
        /// <see cref="CGReferenceMode.Self"/>: the scale is applied over the Path's active range
        /// <see cref="CGReferenceMode.Source"/>: the scale is applied over the Path's total length
        /// </summary>
        /// <remarks>Considered only when <see cref="ScaleMode"/> is set to <see cref="Modules.ScaleMode.Advanced"/> </remarks>
        public CGReferenceMode ScaleReference
        {
            get => m_ScaleReference;
            set
            {
                if (m_ScaleReference != value)
                {
                    m_ScaleReference = value;
                    Dirty = true;
                }
            }
        }

        /// <summary>
        /// If enabled, the same scale is applied to both X and Y axis of the cross section
        /// </summary>
        public bool ScaleUniform
        {
            get => m_ScaleUniform;
            set
            {
                if (m_ScaleUniform != value)
                {
                    m_ScaleUniform = value;
                    Dirty = true;
                }
            }
        }

        /// <summary>
        /// Scale is applied starting at this offset 
        /// </summary>
        /// <remarks>Considered only when <see cref="ScaleMode"/> is set to <see cref="Modules.ScaleMode.Advanced"/> </remarks>
        public float ScaleOffset
        {
            get => m_ScaleOffset;
            set
            {
                if (m_ScaleOffset != value)
                {
                    m_ScaleOffset = value;
                    Dirty = true;
                }
            }
        }

        /// <summary>
        /// The (base) value of the scaling along the cross section's X axis
        /// </summary>
        public float ScaleX
        {
            get => m_ScaleX;
            set
            {
                if (m_ScaleX != value)
                {
                    m_ScaleX = value;
                    Dirty = true;
                }
            }
        }

        /// <summary>
        /// Defines a scale multiplier relatively to the Relative Distance of a point on the path.
        /// </summary>
        /// <remarks>Considered only when <see cref="ScaleMode"/> is set to <see cref="Modules.ScaleMode.Advanced"/> </remarks>
        /// <remarks>You will need to set this module's Dirty to true yourself if you modify the AnimationCurve without setting a new one</remarks>
        public AnimationCurve ScaleMultiplierX
        {
            get => m_ScaleCurveX;
            set
            {
                if (m_ScaleCurveX != value)
                {
                    m_ScaleCurveX = value;
                    Dirty = true;
                }
            }
        }

        /// <summary>
        /// The (base) value of the scaling along the cross section's Y axis if <see cref="ScaleUniform"/> is set to false, otherwise the <see cref="ScaleX"/> value is used instead
        /// </summary>
        public float ScaleY
        {
            get => m_ScaleY;
            set
            {
                if (m_ScaleY != value)
                {
                    m_ScaleY = value;
                    Dirty = true;
                }
            }
        }

        /// <summary>
        /// Defines a scale multiplier relatively to the Relative Distance of a point on the path.
        /// </summary>
        /// <remarks>Considered only when <see cref="ScaleMode"/> is set to <see cref="Modules.ScaleMode.Advanced"/> </remarks>
        /// <remarks>You will need to set this module's Dirty to true yourself if you modify the AnimationCurve without setting a new one</remarks>
        public AnimationCurve ScaleMultiplierY
        {
            get => m_ScaleCurveY;
            set
            {
                if (m_ScaleCurveY != value)
                {
                    m_ScaleCurveY = value;
                    Dirty = true;
                }
            }
        }

        #endregion

        #region ### Unity Callbacks ###

#if DOCUMENTATION___FORCE_IGNORE___UNITY == false

        public override void Reset()
        {
            base.Reset();
            ScaleMode = ScaleMode.Simple;
            ScaleUniform = true;
            ScaleX = 1;
            ScaleY = 1;
            ScaleMultiplierX = AnimationCurve.Linear(
                0,
                1,
                1,
                1
            );
            ScaleMultiplierY = AnimationCurve.Linear(
                0,
                1,
                1,
                1
            );
            ScaleReference = CGReferenceMode.Self;
            ScaleOffset = 0;
        }


#endif

        #endregion

        #region ### Public Methods ###

        /// <summary>
        /// Gets the scale vector of a cross section at a specific position on a path
        /// </summary>
        /// <param name="relativeDistance">A value between 0 and 1 representing how far the point is on a pah.
        /// A value of 0 means the start of the path, and a value of 1 means the end of it. It is defined as: (the point's distance from the path's start) / (the total length of the path)</param>
        /// <returns> The X and Y value are the scale value along those axis</returns>
        public Vector2 GetScale(float relativeDistance)
            => GetScale(
                relativeDistance,
                ScaleMode,
                ScaleOffset,
                ScaleUniform,
                ScaleX,
                ScaleMultiplierX,
                ScaleY,
                ScaleMultiplierY
            );

        #endregion

        #region ### Protected ###

        /// <summary>
        /// Get the scale value along the x and y axis for a point on a path
        /// </summary>
        /// <param name="sampleIndex">the index of the point in the path's <see cref="CGShape.RelativeDistances"/> and <see cref="CGShape.SourceRelativeDistances"/></param>
        /// <param name="relativeDistances"><see cref="CGShape.RelativeDistances"/></param>
        /// <param name="sourceRelativeDistances"><see cref="CGShape.SourceRelativeDistances"/></param>
        /// <returns></returns>
        /// <exception cref="ArgumentOutOfRangeException">If <see cref="ScaleMode"/> has an invalid value</exception>
        protected Vector2 GetScale(int sampleIndex, SubArray<float> relativeDistances, SubArray<float> sourceRelativeDistances)
        {
            Vector2 result;

            switch (ScaleMode)
            {
                case ScaleMode.Advanced:
                    float relativeDistance = GetRelativeDistance(
                        sampleIndex,
                        ScaleReference,
                        relativeDistances,
                        sourceRelativeDistances
                    );

                    result = GetAdvancedScale(
                        relativeDistance,
                        ScaleOffset,
                        ScaleUniform,
                        ScaleX,
                        ScaleMultiplierX,
                        ScaleY,
                        ScaleMultiplierY
                    );
                    break;
                case ScaleMode.Simple:
                    result = GetSimpleScale(
                        ScaleUniform,
                        ScaleX,
                        ScaleY
                    );
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            return result;
        }

        /// <summary>
        /// Get the scale value along the x and y axis for a point on a path
        /// </summary>
        /// <param name="relativeDistance">A value between 0 and 1 representing how far the point is on a pah.
        /// A value of 0 means the start of the path, and a value of 1 means the end of it. It is defined as: (the point's distance from the path's start) / (the total length of the path)</param>
        /// <param name="mode"><see cref="ScaleMode"/></param>
        /// <param name="offset"><see cref="ScaleOffset"/></param>
        /// <param name="isUniform"><see cref="ScaleUniform"/></param>
        /// <param name="scaleX"><see cref="ScaleX"/></param>
        /// <param name="scaleMultiplierX"><see cref="ScaleMultiplierX"/></param>
        /// <param name="scaleY"><see cref="ScaleY"/></param>
        /// <param name="scaleMultiplierY"><see cref="ScaleMultiplierY"/></param>
        /// <exception cref="ArgumentOutOfRangeException">If <paramref name="mode"/>
        ///     has an invalid value</exception>
        protected static Vector2 GetScale(float relativeDistance, ScaleMode mode, float offset, bool isUniform, float scaleX,
            AnimationCurve scaleMultiplierX, float scaleY, AnimationCurve scaleMultiplierY)
        {
            Vector2 result;

            switch (mode)
            {
                case ScaleMode.Advanced:
                    result = GetAdvancedScale(
                        relativeDistance,
                        offset,
                        isUniform,
                        scaleX,
                        scaleMultiplierX,
                        scaleY,
                        scaleMultiplierY
                    );
                    break;
                case ScaleMode.Simple:
                    result = GetSimpleScale(
                        isUniform,
                        scaleX,
                        scaleY
                    );
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            return result;
        }

        /// <summary>
        /// Get the relative distance of a sample point
        /// </summary>
        /// <param name="sampleIndex">the index of the sample point in the path's <see cref="CGShape.RelativeDistances"/> and <see cref="CGShape.SourceRelativeDistances"/></param>
        /// <param name="cgReferenceMode"><see cref="ScaleReference"/></param>
        /// <param name="relativeDistances"><see cref="CGShape.RelativeDistances"/></param>
        /// <param name="sourceRelativeDistances"><see cref="CGShape.SourceRelativeDistances"/></param>
        /// <returns></returns>
        /// <exception cref="ArgumentOutOfRangeException">If <paramref name="cgReferenceMode"/> has an invalid value</exception>
        protected static float GetRelativeDistance(int sampleIndex, CGReferenceMode cgReferenceMode,
            SubArray<float> relativeDistances, SubArray<float> sourceRelativeDistances)
        {
            float relativeDistance;
            {
                SubArray<float> scaleRelativeDistancesArray;
                switch (cgReferenceMode)
                {
                    case CGReferenceMode.Source:
                        scaleRelativeDistancesArray = sourceRelativeDistances;
                        break;
                    case CGReferenceMode.Self:
                        scaleRelativeDistancesArray = relativeDistances;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                relativeDistance = scaleRelativeDistancesArray.Array[sampleIndex];
            }
            return relativeDistance;
        }

        /// <summary>
        /// Get the scale value along the x and y axis for a point on a path
        /// </summary>
        /// <param name="relativeDistance">A value between 0 and 1 representing how far the point is on a pah.
        /// A value of 0 means the start of the path, and a value of 1 means the end of it. It is defined as: (the point's distance from the path's start) / (the total length of the path)</param>
        /// <param name="scaleOffset"><see cref="ScaleOffset"/></param>
        /// <param name="isUniform"><see cref="ScaleUniform"/></param>
        /// <param name="scaleX"><see cref="ScaleX"/></param>
        /// <param name="scaleMultiplierX"><see cref="ScaleMultiplierX"/></param>
        /// <param name="scaleY"><see cref="ScaleY"/></param>
        /// <param name="scaleMultiplierY"><see cref="ScaleMultiplierY"/></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected static Vector2 GetAdvancedScale(float relativeDistance, float scaleOffset, bool isUniform, float scaleX,
            AnimationCurve scaleMultiplierX, float scaleY, AnimationCurve scaleMultiplierY)
        {
            //Optim: avoid calling unnecessarily. This means that if the curve is a constant 1, there is no need to evaluate the curve. Use AnimationCurveExt.ValueIsOne to know if this condition is true.
            //Of course, calling ValueIsOne everytime will be not worth it. You will need to cache the value of ValueIsOne at every modification of the curves

            Vector2 result;
            float scaleFValue = DTMath.Repeat(
                relativeDistance - scaleOffset,
                1
            );
            float x = scaleX * scaleMultiplierX.Evaluate(scaleFValue);

            result.x = x;
            result.y = isUniform
                ? x
                : scaleY * scaleMultiplierY.Evaluate(scaleFValue);
            return result;
        }

        /// <summary>
        /// Get the scale value along the x and y axis
        /// </summary>
        /// <param name="isUniform"><see cref="ScaleUniform"/></param>
        /// <param name="scaleX"><see cref="ScaleX"/></param>
        /// <param name="scaleY"><see cref="ScaleY"/></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected static Vector2 GetSimpleScale(bool isUniform, float scaleX, float scaleY)
        {
            Vector2 result;
            result.x = scaleX;
            result.y = isUniform
                ? scaleX
                : scaleY;
            return result;
        }

        #endregion
    }
}