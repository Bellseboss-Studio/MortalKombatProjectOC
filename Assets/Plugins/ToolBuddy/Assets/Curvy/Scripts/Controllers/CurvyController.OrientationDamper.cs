// =====================================================================
// Copyright © 2013 ToolBuddy
// All rights reserved
// 
// http://www.toolbuddy.net
// =====================================================================

using System;
using JetBrains.Annotations;
using UnityEngine;

namespace FluffyUnderware.Curvy.Controllers
{
    public abstract partial class CurvyController
    {
        /// <summary>
        /// A class that handles the damping of the orientation of the controller
        /// </summary>
        protected class OrientationDamper
        {
            [NotNull]
            private readonly CurvyController controller;

            public OrientationDamper([NotNull] CurvyController controller) =>
                this.controller = controller;

            /// <summary>
            /// The damping velocity used in the Direction damping
            /// <seealso cref="DirectionDampingTime"/>
            /// <seealso cref="Vector3.SmoothDamp(Vector3, Vector3, ref Vector3, float, float, float)"/>
            /// </summary>
            [UsedImplicitly]
            [Obsolete]
            public Vector3 DirectionDampingVelocity;

            /// <summary>
            /// The damping velocity used in the Up damping
            /// <seealso cref="UpDampingTime"/>
            /// <seealso cref="Vector3.SmoothDamp(Vector3, Vector3, ref Vector3, float, float, float)"/>
            /// </summary>
            [UsedImplicitly]
            [Obsolete]
            public Vector3 UpDampingVelocity;

            public Quaternion Damp(
                Quaternion sourceOrientation,
                Vector3 targetForward,
                Vector3 targetUp,
                float deltaTime)
            {
                Vector3 postDampingForward = DampenVector(
                    sourceOrientation * Vector3.forward,
                    targetForward,
                    deltaTime,
                    controller.DirectionDampingTime,
#pragma warning disable CS0612
                    ref DirectionDampingVelocity
                );
#pragma warning restore CS0612

                Vector3 postDampingUp = DampenVector(
                    sourceOrientation * Vector3.up,
                    targetUp,
                    deltaTime,
                    controller.UpDampingTime,
#pragma warning disable CS0612
                    ref UpDampingVelocity
                );
#pragma warning restore CS0612

                return Quaternion.LookRotation(
                    postDampingForward,
                    postDampingUp
                );
            }

            private Vector3 DampenVector(Vector3 current, Vector3 target, float deltaTime, float dampingTime,
                ref Vector3 velocity)
            {
                Vector3 direction;
                if (dampingTime > 0 && controller.State == CurvyControllerState.Playing)
                    direction = deltaTime > 0
                        ? Vector3.SmoothDamp(
                            current,
                            target,
                            ref velocity,
                            dampingTime,
                            float.PositiveInfinity,
                            deltaTime
                        )
                        : current;
                else
                    direction = target;

                return direction;
            }

            public void Reset()
            {
#pragma warning disable CS0612
                DirectionDampingVelocity = UpDampingVelocity = Vector3.zero;
#pragma warning restore CS0612
            }
        }

        /// <summary>
        /// The damping velocity used in the Direction damping
        /// <seealso cref="DirectionDampingTime"/>
        /// <seealso cref="Vector3.SmoothDamp(Vector3, Vector3, ref Vector3, float, float, float)"/>
        /// </summary>
        [UsedImplicitly]
        [Obsolete]
        protected Vector3 DirectionDampingVelocity
        {
            get => Damper.DirectionDampingVelocity;
            set => Damper.DirectionDampingVelocity = value;
        }


        /// <summary>
        /// The damping velocity used in the Up damping
        /// <seealso cref="UpDampingTime"/>
        /// <seealso cref="Vector3.SmoothDamp(Vector3, Vector3, ref Vector3, float, float, float)"/>
        /// </summary>
        [UsedImplicitly]
        [Obsolete]
        protected Vector3 UpDampingVelocity
        {
            get => Damper.UpDampingVelocity;
            set => Damper.UpDampingVelocity = value;
        }
    }
}