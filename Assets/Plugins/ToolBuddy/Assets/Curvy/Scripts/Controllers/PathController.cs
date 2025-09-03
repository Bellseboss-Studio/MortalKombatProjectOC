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
using UnityEngine;
using UnityEngine.Assertions;

namespace FluffyUnderware.Curvy.Controllers
{
    /// <summary>
    /// Controller working on Curvy Generator Paths
    /// </summary>
    [AddComponentMenu("Curvy/Controllers/CG Path Controller")]
    [HelpURL(AssetInformation.DocsRedirectionBaseUrl + "pathcontroller")]
    public class PathController : CurvyController
    {
        #region ### Serialized Fields ###

        [Section(
            "General",
            Sort = 0
        )]
        [SerializeField]
        [CGDataReferenceSelector(
            typeof(CGPath),
            Label = "Path/Slot"
        )]
        private CGDataReference m_Path = new CGDataReference();

        #endregion

        #region ### Public Properties ###

        /// <summary>
        /// Gets or sets the path to use
        /// </summary>
        public CGDataReference Path
        {
            get => m_Path;
            set => m_Path = value;
        }

        /// <summary>
        /// Gets the actual CGPath data
        /// </summary>
        public CGPath PathData => Path.HasValue
            ? Path.GetData<CGPath>()
            : null;

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
                return PathData != null
                    ? PathData.Length
                    : 0;
            }
        }

        #endregion

        #region ### Unity Callbacks ###

#if DOCUMENTATION___FORCE_IGNORE___UNITY == false

#endif

        #endregion

        #region ### Protected Methods ###

        public override bool IsReady => Path != null && !Path.IsEmpty && Path.HasValue;


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

            return PathData != null
                ? PathData.FToDistance(relativeDistance)
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
            return PathData != null
                ? PathData.DistanceToF(worldUnitDistance)
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
            return Path.Module.Generator.transform.TransformPoint(PathData.InterpolatePosition(tf));
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
            PathData.Interpolate(
                tf,
                out interpolatedPosition,
                out tangent,
                out up
            );
            Transform generatorTransform = Path.Module.Generator.transform;
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
            return Path.Module.Generator.transform.TransformDirection(PathData.InterpolateDirection(tf));
        }

        protected override Vector3 GetOrientation(float tf)
        {
#if CURVY_SANITY_CHECKS
            Assert.IsTrue(
                IsReady,
                ControllerNotReadyMessage
            );
#endif
            return Path.Module.Generator.transform.TransformDirection(PathData.InterpolateUp(tf));
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
                    PathData.Move(
                        ref tf,
                        ref directionInt,
                        speed * deltaTime,
                        Clamping
                    );
                    break;
                case MoveModeEnum.AbsolutePrecise:
                    PathData.MoveBy(
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
    }
}