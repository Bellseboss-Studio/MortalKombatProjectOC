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
    public partial class SplineController
    {
        protected class SplineSwitcher
        {
            /// <summary>
            /// The time at which the current spline switching started.
            /// Its value is invalid if no spline switching is in progress. Spline switching is done by calling <see cref="Start"/>
            /// </summary>
            public float StartTime { get; set; }

            /// <summary>
            /// The duration of the the current spline switching.
            /// Its value is invalid if no spline switching is in progress. Spline switching is done by calling <see cref="Start"/>
            /// </summary>
            public float Duration { get; set; }

            /// <summary>
            /// The spline to which the controller is switching.
            /// Its value is invalid if no spline switching is in progress. Spline switching is done by calling <see cref="Start"/>
            /// </summary>
            public CurvySpline Spline { get; set; }

            /// <summary>
            /// The controller's current TF on the <see cref="Spline"/>.
            /// Its value is invalid if no spline switching is in progress. Spline switching is done by calling <see cref="Start"/>
            /// </summary>
            public float Tf { get; set; }

            /// <summary>
            /// The controller's current Direction on the <see cref="Spline"/>.
            /// Its value is invalid if no spline switching is in progress. Spline switching is done by calling <see cref="Start"/>
            /// </summary>
            public MovementDirection Direction { get; set; }

            /// <summary>
            /// Gets whether the Controller is switching splines
            /// </summary>
            public bool IsSwitching { get; set; }

            /// <summary>
            /// The ratio (value between 0 and 1) expressing the progress of the current spline switch. 0 means the switch just started, 1 means the switch ended.
            /// Its value is 0 if no spline switching is in progress. Spline switching is done by calling <see cref="Start"/>
            /// </summary>
            public float Progress => IsSwitching
                ? Mathf.Clamp01((Time.time - StartTime) / Duration)
                : 0;

            /// <summary>
            /// Start a spline switch. Should be called only on non stopped controllers.
            /// </summary>
            /// <remarks>While switching is not finished, movement on destination spline will not fire events nor consider connections</remarks>
            /// <param name="spline">the target spline to switch to</param>
            /// <param name="tf">the target TF</param>
            /// <param name="duration">duration of the switch phase</param>
            /// <param name="direction"></param>
            public void Start([NotNull] CurvySpline spline, float tf, float duration, MovementDirection direction)
            {
                if (duration <= 0)
                    throw new ArgumentOutOfRangeException(
                        nameof(duration),
                        "Duration must be greater than 0"
                    );
                if (tf < 0 || tf > 1)
                    throw new ArgumentOutOfRangeException(
                        nameof(tf),
                        "Destination TF must be between 0 and 1"
                    );

                StartTime = Time.time;
                Duration = duration;
                Spline = spline;
                Tf = tf;
                Direction = direction;
                IsSwitching = true;
            }

            /// <summary>
            /// Advance the current spline switch. Updates <see cref="Tf"/> and <see cref="Direction"/> accordingly.
            /// </summary>
            /// <param name="spline">Spline on witch advancing</param>
            /// <param name="moveMode">movement type</param>
            /// <param name="distance">distance of the advance. Its unit depends on the value of <paramref name="moveMode"/></param>
            /// <param name="clamping">Defines the behaviour when reaching a spline end</param>
            public void Advance(CurvySpline spline, MoveModeEnum moveMode, float distance, CurvyClamping clamping)
            {
                float switcherTf = Tf;
                MovementDirection switcherDirection = Direction;
                SimulateAdvanceOnSpline(
                    spline,
                    ref switcherTf,
                    ref switcherDirection,
                    distance,
                    moveMode,
                    clamping
                );
                Tf = switcherTf;
                Direction = switcherDirection;
            }

            /// <summary>
            /// Stop the current spline switch
            /// </summary>
            public void Stop()
            {
                StartTime = default;
                Duration = default;
                Spline = default;
                Tf = default;
                Direction = default;
                IsSwitching = false;
            }
        }


        /// <summary>
        /// The time at which the current spline switching started.
        /// Its value is invalid if no spline switching is in progress. Spline switching is triggered by calling <see cref="SwitchTo"/>
        /// </summary>
        [UsedImplicitly]
        [Obsolete("Use Switcher instead")]
        protected float SwitchStartTime => Switcher.StartTime;

        /// <summary>
        /// The duration of the the current spline switching.
        /// Its value is invalid if no spline switching is in progress. Spline switching is triggered by calling <see cref="SwitchTo"/>
        /// </summary>
        [UsedImplicitly]
        [Obsolete("Use Switcher instead")]
        protected float SwitchDuration => Switcher.Duration;

        /// <summary>
        /// The spline to which the controller is switching.
        /// Its value is invalid if no spline switching is in progress. Spline switching is triggered by calling <see cref="SwitchTo"/>
        /// </summary>
        [UsedImplicitly]
        [Obsolete("Use Switcher instead")]
        protected CurvySpline SwitchTarget => Switcher.Spline;

        /// <summary>
        /// The controller's current TF on the <see cref="SwitchTarget"/>.
        /// Its value is invalid if no spline switching is in progress. Spline switching is triggered by calling <see cref="SwitchTo"/>
        /// </summary>
        [UsedImplicitly]
        [Obsolete("Use Switcher instead")]
        protected float TfOnSwitchTarget => Switcher.Tf;

        /// <summary>
        /// The controller's current Direction on the <see cref="SwitchTarget"/>.
        /// Its value is invalid if no spline switching is in progress. Spline switching is triggered by calling <see cref="SwitchTo"/>
        /// </summary>
        [UsedImplicitly]
        [Obsolete("Use Switcher instead")]
        protected MovementDirection DirectionOnSwitchTarget => Switcher.Direction;
    }
}