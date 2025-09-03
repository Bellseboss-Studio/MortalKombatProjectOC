// =====================================================================
// Copyright © 2013 ToolBuddy
// All rights reserved
// 
// http://www.toolbuddy.net
// =====================================================================

using System;
using FluffyUnderware.Curvy.Generator;
using FluffyUnderware.Curvy.Utils;
using FluffyUnderware.DevTools;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Assertions;

namespace FluffyUnderware.Curvy.Controllers
{
    /// <summary>
    /// Controller using a Curvy Generator Volume
    /// </summary>
    [AddComponentMenu("Curvy/Controllers/CG Volume Controller")]
    [HelpURL(AssetInformation.DocsRedirectionBaseUrl + "volumecontroller")]
    public class VolumeController : CurvyController
    {
        private const float CrossPositionRangeMin = -0.5f;
        private const float CrossPositionRangeMax = 0.5f;

        #region ### Serialized Fields ###

        [Section("General")]
        [CGDataReferenceSelector(
            typeof(CGVolume),
            Label = "Volume/Slot"
        )]
        [SerializeField]
        private CGDataReference m_Volume = new CGDataReference();

        [Section(
            "Cross Position",
            Sort = 1,
            HelpURL = AssetInformation.DocsRedirectionBaseUrl + "volumecontroller_crossposition"
        )]
        [SerializeField]
        [FloatRegion(
            UseSlider = true,
            Precision = 4,
            RegionOptionsPropertyName = nameof(CrossRangeOptions),
            Options = AttributeOptionsFlags.Full
        )]
        private FloatRegion m_CrossRange = new FloatRegion(
            CrossPositionRangeMin,
            CrossPositionRangeMax
        );

        [RangeEx(
            nameof(MinCrossRelativePosition),
            nameof(MaxCrossRelativePosition)
        )]
        [SerializeField]
        private float crossRelativePosition;

        [SerializeField]
        private CurvyClamping m_CrossClamping;

        #endregion

        #region ### Public Properties ###

        /// <summary>
        /// Gets or sets the volume to use
        /// </summary>
        public CGDataReference Volume
        {
            get => m_Volume;
            set => m_Volume = value;
        }

        /// <summary>
        /// Gets the actual volume data
        /// </summary>
        [CanBeNull]
        public CGVolume VolumeData => Volume.HasValue
            ? Volume.GetData<CGVolume>()
            : null;

        public float CrossFrom
        {
            get => m_CrossRange.From;
            set => m_CrossRange.From = Mathf.Clamp(
                value,
                CrossPositionRangeMin,
                CrossPositionRangeMax
            );
        }

        public float CrossTo
        {
            get => m_CrossRange.To;
            set => m_CrossRange.To = Mathf.Clamp(
                value,
                CrossFrom,
                CrossPositionRangeMax
            );
        }

        public float CrossLength => m_CrossRange.Length;


        /// <summary>
        /// Gets or sets the clamping mode for lateral movement
        /// </summary>
        public CurvyClamping CrossClamping
        {
            get => m_CrossClamping;
            set => m_CrossClamping = value;
        }

        /// <summary>
        /// Gets or sets the current relative lateral position, respecting clamping. Ranges from <see cref="CrossFrom"/> to <see cref="CrossTo"/>
        /// </summary>
        public float CrossRelativePosition
        {
            get => GetClampedCrossPosition(crossRelativePosition);
            set => crossRelativePosition = GetClampedCrossPosition(value);
        }

        /// <summary>
        /// Gets the source's length
        /// </summary>
        public override float Length
        {
            get
            {
#if CURVY_SANITY_CHECKS
                Assert.IsTrue(
                    IsReady,
                    ControllerNotReadyMessage
                );
#endif
                return VolumeData != null
                    ? VolumeData.Length
                    : 0;
            }
        }

        #endregion

        #region ### Public Methods ###

        /// <summary>
        /// Converts relative lateral to absolute position, respecting clamping, ignoring CrossRange
        /// </summary>
        /// <param name="relativeDistance">the relative position</param>
        /// <returns>the absolute position</returns>
        public float CrossRelativeToAbsolute(float relativeDistance)
        {
#if CURVY_SANITY_CHECKS
            Assert.IsTrue(
                IsReady,
                ControllerNotReadyMessage
            );
#endif
            return VolumeData != null
                ? VolumeData.CrossFToDistance(
                    RelativePosition,
                    relativeDistance,
                    CrossClamping
                )
                : 0;
        }

        /// <summary>
        /// Converts absolute lateral to relative position, respecting clamping, ignoring CrossRange
        /// </summary>
        /// <param name="worldUnitDistance">the absolute position</param>
        /// <returns>the relative position</returns>
        public float CrossAbsoluteToRelative(float worldUnitDistance)
        {
#if CURVY_SANITY_CHECKS
            Assert.IsTrue(
                IsReady,
                ControllerNotReadyMessage
            );
#endif
            return VolumeData != null
                ? VolumeData.CrossDistanceToF(
                    RelativePosition,
                    worldUnitDistance,
                    CrossClamping
                )
                : 0;
        }

        #endregion

        #region ### Protected Methods ###

        public override bool IsReady => Volume != null && !Volume.IsEmpty && Volume.HasValue;

        /// <summary>
        /// Converts distance on source from relative to absolute position.
        /// </summary>
        /// <param name="relativeDistance">relative distance from the source start. Should be already clamped</param>
        /// <returns>distance in world units from the source start</returns>
        protected override float RelativeToAbsolute(float relativeDistance)
        {
#if CURVY_SANITY_CHECKS
            Assert.IsTrue(
                IsReady,
                ControllerNotReadyMessage
            );
            Assert.IsTrue(
                CurvyUtility.Approximately(
                    relativeDistance,
                    GetClampedPosition(
                        relativeDistance,
                        CurvyPositionMode.Relative,
                        Clamping,
                        Length
                    )
                )
            );
#endif
            return VolumeData != null
                ? VolumeData.FToDistance(relativeDistance)
                : 0;
        }

        /// <summary>
        /// Converts distance on source from absolute to relative position.
        /// </summary>
        /// <param name="worldUnitDistance">distance in world units from the source start. Should be already clamped</param>
        /// <returns>relative distance in the range 0..1</returns>
        protected override float AbsoluteToRelative(float worldUnitDistance)
        {
#if CURVY_SANITY_CHECKS
            Assert.IsTrue(
                IsReady,
                ControllerNotReadyMessage
            );
            Assert.IsTrue(
                CurvyUtility.Approximately(
                    worldUnitDistance,
                    GetClampedPosition(
                        worldUnitDistance,
                        CurvyPositionMode.WorldUnits,
                        Clamping,
                        Length
                    )
                )
            );
#endif
            return VolumeData != null
                ? VolumeData.DistanceToF(worldUnitDistance)
                : 0;
        }

        protected override Vector3 GetInterpolatedSourcePosition(float tf)
        {
#if CURVY_SANITY_CHECKS
            Assert.IsTrue(
                IsReady,
                ControllerNotReadyMessage
            );
#endif
            return Volume.Module.Generator.transform.TransformPoint(
                VolumeData.InterpolateVolumePosition(
                    tf,
                    CrossRelativePosition
                )
            );
        }

        protected override void GetInterpolatedSourcePosition(float tf, out Vector3 interpolatedPosition, out Vector3 tangent,
            out Vector3 up)
        {
#if CURVY_SANITY_CHECKS
            Assert.IsTrue(
                IsReady,
                ControllerNotReadyMessage
            );
#endif
            VolumeData.InterpolateVolume(
                tf,
                CrossRelativePosition,
                out interpolatedPosition,
                out tangent,
                out up
            );
            Transform generatorTransform = Volume.Module.Generator.transform;
            interpolatedPosition = generatorTransform.TransformPoint(interpolatedPosition);
            tangent = generatorTransform.TransformDirection(tangent);
            up = generatorTransform.TransformDirection(up);
        }


        protected override Vector3 GetTangent(float tf)
        {
#if CURVY_SANITY_CHECKS
            Assert.IsTrue(
                IsReady,
                ControllerNotReadyMessage
            );
#endif
            return Volume.Module.Generator.transform.TransformDirection(
                VolumeData.InterpolateVolumeDirection(
                    tf,
                    CrossRelativePosition
                )
            );
        }


        protected override Vector3 GetOrientation(float tf)
        {
#if CURVY_SANITY_CHECKS
            Assert.IsTrue(
                IsReady,
                ControllerNotReadyMessage
            );
#endif
            return Volume.Module.Generator.transform.TransformDirection(
                VolumeData.InterpolateVolumeUp(
                    tf,
                    CrossRelativePosition
                )
            );
        }

        protected override void Advance(float speed, float deltaTime)
        {
            float tf = RelativePosition;
            MovementDirection direction = MovementDirection;

            SimulateAdvance(
                ref tf,
                ref direction,
                speed,
                deltaTime
            );

            MovementDirection = direction;
            RelativePosition = tf;
        }

        protected override void SimulateAdvance(ref float tf, ref MovementDirection direction, float speed, float deltaTime)
        {
#if CURVY_SANITY_CHECKS
            Assert.IsTrue(
                IsReady,
                ControllerNotReadyMessage
            );
#endif
            int directionInt = direction.ToInt();

            switch (MoveMode)
            {
                case MoveModeEnum.Relative:
                    VolumeData.Move(
                        ref tf,
                        ref directionInt,
                        speed * deltaTime,
                        Clamping
                    );
                    break;
                case MoveModeEnum.AbsolutePrecise:
                    VolumeData.MoveBy(
                        ref tf,
                        ref directionInt,
                        speed * deltaTime,
                        Clamping
                    );
                    break;
                default:
                    throw new NotSupportedException();
            }

            direction = MovementDirectionMethods.FromInt(directionInt);
        }

        #endregion

        #region ### Privates & Internals ###

        private RegionOptions<float> CrossRangeOptions => RegionOptions<float>.MinMax(
            CrossPositionRangeMin,
            CrossPositionRangeMax
        );

        private float MinCrossRelativePosition => m_CrossRange.From;

        private float MaxCrossRelativePosition => m_CrossRange.To;

        private float GetClampedCrossPosition(float position)
            => CurvyUtility.ClampValue(
                position,
                CrossClamping,
                CrossFrom,
                CrossTo
            );

        #endregion

        #region RetroCompatibility code

        [SerializeField, HideInInspector]
        [UsedImplicitly]
        [Obsolete("Use crossRelativePosition instead. This field is kept for retro compatibility reasons")]
        private float m_CrossInitialPosition;

#if DOCUMENTATION___FORCE_IGNORE___CURVY == false
        public override void OnAfterDeserialize()
        {
            base.OnAfterDeserialize();
#pragma warning disable 618
            if (float.IsNaN(m_CrossInitialPosition) == false)
            {
#pragma warning disable 612
                //Converts from the obsolete way of representing cross relative position to the usual one.
                crossRelativePosition = DTMath.MapValue(
                    CrossFrom,
                    CrossTo,
                    m_CrossInitialPosition,
                    CrossPositionRangeMin,
                    CrossPositionRangeMax
                );
#pragma warning restore 612
                m_CrossInitialPosition = Single.NaN;
            }
#pragma warning restore 618
        }
#endif

        #endregion
    }
}