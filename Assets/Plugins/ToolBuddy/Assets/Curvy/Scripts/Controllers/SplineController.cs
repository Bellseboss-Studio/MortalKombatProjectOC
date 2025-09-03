// =====================================================================
// Copyright © 2013 ToolBuddy
// All rights reserved
// 
// http://www.toolbuddy.net
// =====================================================================

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using FluffyUnderware.Curvy.Utils;
using FluffyUnderware.DevTools;
using UnityEngine;
using UnityEngine.Assertions;
using Random = UnityEngine.Random;

namespace FluffyUnderware.Curvy.Controllers
{
    /// <summary>
    /// Controller working with Splines
    /// </summary>
    [AddComponentMenu("Curvy/Controllers/Spline Controller")]
    [HelpURL(AssetInformation.DocsRedirectionBaseUrl + "splinecontroller")]
    public partial class SplineController : CurvyController
    {
        public SplineController()
        {
            preAllocatedEventArgs = new CurvySplineMoveEventArgs(
                this,
                Spline,
                null,
                Single.NaN,
                false,
                Single.NaN,
                MovementDirection.Forward
            );
            Switcher = new SplineSwitcher();
        }

        #region ### Serialized Fields ###

#if DOCUMENTATION___FORCE_IGNORE___CURVY == false

        /// <summary>
        /// The spline to use. It is best to set/get the spline through the <see cref="Spline"/> property instead
        /// </summary>
        [Section(
            "General",
            Sort = 0
        )]
        [FieldCondition(
            nameof(m_Spline),
            null,
            false,
            ActionAttribute.ActionEnum.ShowError,
            "Missing source Spline"
        )]
        [SerializeField]
        protected CurvySpline m_Spline;

#endif

        [SerializeField]
        [Tooltip(
            "Whether spline's cache data should be used. Set this to true to gain performance if precision is not required."
        )]
        private bool m_UseCache;

        #region Connections handling

        [Section(
            "Connections Handling",
            Sort = 250,
            HelpURL = AssetInformation.DocsRedirectionBaseUrl + "curvycontroller_connectionshandling"
        )]
        [SerializeField, Label(
             "At connection, use",
             "What spline should the controller use when reaching a Connection"
         )]
        private SplineControllerConnectionBehavior connectionBehavior = SplineControllerConnectionBehavior.CurrentSpline;

        #region Random Connection and Follow-Up options

        [SerializeField, Label(
             "Allow direction change",
             "When true, the controller will modify its direction to best fit the connected spline"
         )]
#if UNITY_EDITOR
        [FieldCondition(
            nameof(connectionBehavior),
            SplineControllerConnectionBehavior.FollowUpSpline,
            false,
            ConditionalAttribute.OperatorEnum.OR,
            "ShowRandomConnectionOptions",
            true,
            false
        )]
#endif

        private bool allowDirectionChange = true;

        #endregion

        #region Random Connection options

        [SerializeField, Label(
             "Reject current spline",
             "Whether the current spline should be excluded from the randomly selected splines"
         )]
        [FieldCondition(
            nameof(ShowRandomConnectionOptions),
            true
        )]
        private bool rejectCurrentSpline = true;

        [SerializeField, Label(
             "Reject divergent splines",
             "Whether splines that diverge from the current spline with more than a specific angle should be excluded from the randomly selected splines"
         )]
        [FieldCondition(
            nameof(ShowRandomConnectionOptions),
            true
        )]
        private bool rejectTooDivergentSplines;

        [SerializeField, Label(
             "Max allowed angle",
             "Maximum allowed divergence angle in degrees"
         )]
#if UNITY_EDITOR
        [FieldCondition(
            nameof(ShowRandomConnectionOptions),
            true,
            false,
            ConditionalAttribute.OperatorEnum.AND,
            "rejectTooDivergentSplines",
            true,
            false
        )]
#endif
        [Range(
            0,
            180
        )]
        private float maxAllowedDivergenceAngle = 90;

        #endregion

        #region Custom options

        [SerializeField, Label(
             "Custom Selector",
             "A custom logic to select which connected spline to follow. Select a Script inheriting from SplineControllerConnectionBehavior"
         )]
        [FieldCondition(
            nameof(connectionBehavior),
            SplineControllerConnectionBehavior.Custom
        )]
        [FieldCondition(
            nameof(connectionCustomSelector),
            null,
            false,
            ActionAttribute.ActionEnum.ShowWarning,
            "Missing custom selector"
        )]
        private ConnectedControlPointsSelector connectionCustomSelector;

        #endregion

        #endregion

#if DOCUMENTATION___FORCE_IGNORE___CURVY == false

        [Section(
            "Events",
            false,
            false,
            1000,
            HelpURL = AssetInformation.DocsRedirectionBaseUrl + "splinecontroller_events"
        )]
        [SerializeField]
        [ArrayEx]
        protected List<OnPositionReachedSettings> onPositionReachedList = new List<OnPositionReachedSettings>();

        [SerializeField]
        protected CurvySplineMoveEvent m_OnControlPointReached = new CurvySplineMoveEvent();

        [SerializeField]
        protected CurvySplineMoveEvent m_OnEndReached = new CurvySplineMoveEvent();

        [SerializeField]
        protected CurvySplineMoveEvent m_OnSwitch = new CurvySplineMoveEvent();

#endif

        #endregion

        #region ### Public Properties ###

        /// <summary>
        /// Gets or sets the spline to use
        /// </summary>
        public virtual CurvySpline Spline
        {
            get => m_Spline;
            set => m_Spline = value;
        }

        /// <summary>
        /// Gets or sets whether spline's cache data should be used
        /// </summary>
        public bool UseCache
        {
            get => m_UseCache;
            set => m_UseCache = value;
        }


        #region Connections handling

        /// <summary>
        /// Connections handling: What spline should the controller use when reaching a Connection
        /// </summary>
        public SplineControllerConnectionBehavior ConnectionBehavior
        {
            get => connectionBehavior;
            set => connectionBehavior = value;
        }

        /// <summary>
        /// Connections handling: A custom logic to select which connected spline to follow. Select a Script inheriting from SplineControllerConnectionBehavior. Is used when <see cref="ConnectionBehavior"/> is equal to <see cref="SplineControllerConnectionBehavior.Custom"/>
        /// </summary>
        public ConnectedControlPointsSelector ConnectionCustomSelector
        {
            get => connectionCustomSelector;
            set => connectionCustomSelector = value;
        }

        /// <summary>
        /// Connections handling: When true, the controller will modify its direction to best fit the connected spline. Is used when <see cref="ConnectionBehavior"/> is equal to <see cref="SplineControllerConnectionBehavior.FollowUpSpline"/>,  <see cref="SplineControllerConnectionBehavior.RandomSpline"/>, or <see cref="SplineControllerConnectionBehavior.FollowUpOtherwiseRandom"/>
        /// </summary>
        public bool AllowDirectionChange
        {
            get => allowDirectionChange;
            set => allowDirectionChange = value;
        }

        /// <summary>
        /// Connections handling: Whether the current spline should be excluded from the randomly selected splines. Is used when <see cref="ConnectionBehavior"/> is equal to <see cref="SplineControllerConnectionBehavior.RandomSpline"/>, or <see cref="SplineControllerConnectionBehavior.FollowUpOtherwiseRandom"/>
        /// </summary>
        public bool RejectCurrentSpline
        {
            get => rejectCurrentSpline;
            set => rejectCurrentSpline = value;
        }

        /// <summary>
        /// Connections handling: Whether splines that diverge from the current spline with more than <see cref="MaxAllowedDivergenceAngle"/> should be excluded from the randomly selected splines. Is used when <see cref="ConnectionBehavior"/> is equal to <see cref="SplineControllerConnectionBehavior.RandomSpline"/>, or <see cref="SplineControllerConnectionBehavior.FollowUpOtherwiseRandom"/>
        /// </summary>
        public bool RejectTooDivergentSplines
        {
            get => rejectTooDivergentSplines;
            set => rejectTooDivergentSplines = value;
        }

        /// <summary>
        /// Connections handling: Maximum allowed divergence angle in degrees. Considered when <see cref="MaxAllowedDivergenceAngle"/> is true. Is used when <see cref="ConnectionBehavior"/> is equal to <see cref="SplineControllerConnectionBehavior.RandomSpline"/>, or <see cref="SplineControllerConnectionBehavior.FollowUpOtherwiseRandom"/>
        /// </summary>
        public float MaxAllowedDivergenceAngle
        {
            get => maxAllowedDivergenceAngle;
            set => maxAllowedDivergenceAngle = value;
        }

        #endregion


        /// <summary>
        /// Settings of events raised when moving over specific positions on the spline
        /// </summary>
        public List<OnPositionReachedSettings> OnPositionReachedList
        {
            get => onPositionReachedList;
            set => onPositionReachedList = value;
        }

        /// <summary>
        /// Event raised when moving over a Control Point
        /// </summary>
        public CurvySplineMoveEvent OnControlPointReached
        {
            get => m_OnControlPointReached;
            set => m_OnControlPointReached = value;
        }

        /// <summary>
        /// Event raised when reaching the extends (i.e. the start or end) of the source spline
        /// </summary>
        public CurvySplineMoveEvent OnEndReached
        {
            get => m_OnEndReached;
            set => m_OnEndReached = value;
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
                return ReferenceEquals(
                           Spline,
                           null
                       )
                       == false
                    ? Spline.Length
                    : 0;
            }
        }

        #region ### Switcher ###

        /// <summary>
        /// Gets whether the Controller is switching splines
        /// </summary>
        public bool IsSwitching => Switcher.IsSwitching;

        /// <summary>
        /// The ratio (value between 0 and 1) expressing the progress of the current spline switch. 0 means the switch just started, 1 means the switch ended.
        /// Its value is 0 if no spline switching is in progress. Spline switching is triggered by calling <see cref="SwitchTo(FluffyUnderware.Curvy.CurvySpline,float,float)"/>
        /// </summary>
        public float SwitchProgress => Switcher.Progress;

        /// <summary>
        /// Event raised while switching splines. Splines switching is triggered via the <see cref="SwitchTo"/> method.
        /// </summary>
        public CurvySplineMoveEvent OnSwitch
        {
            get => m_OnSwitch;
            set => m_OnSwitch = value;
        }

        #endregion

        #endregion

        #region ### Private & Protected Fields & Properties ###

        protected readonly SplineSwitcher Switcher;
        private CurvySpline prePlaySpline;
        private readonly CurvySplineMoveEventArgs preAllocatedEventArgs;

        #endregion

        #region ## Unity Callbacks ###

#if DOCUMENTATION___FORCE_IGNORE___UNITY == false

        protected override void OnValidate()
        {
            base.OnValidate();

            if (IsReady)
                foreach (OnPositionReachedSettings settings in OnPositionReachedList)
                    settings.Position = Mathf.Min(
                        Mathf.Max(
                            settings.Position,
                            0
                        ),
                        GetMaxPosition(settings.PositionMode)
                    );
        }

#endif

        #endregion

        #region ### Public Methods ###

        #region ### Switcher ###

        /// <summary>
        /// Start a spline switch. Should be called only on non stopped controllers.
        /// </summary>
        /// <remarks>While switching is not finished, movement on destination spline will not fire events nor consider connections</remarks>
        /// <param name="destinationSpline">the target spline to switch to</param>
        /// <param name="destinationTf">the target TF</param>
        /// <param name="duration">duration of the switch phase</param>
        public virtual void SwitchTo(CurvySpline destinationSpline, float destinationTf, float duration)
        {
#if CURVY_SANITY_CHECKS
            Assert.IsTrue(
                isInitialized,
                ControllerNotReadyMessage
            );
#endif
            if (PlayState == CurvyControllerState.Stopped)
            {
                DTLog.LogError(
                    "[Curvy] Controller can not switch when stopped. The switch call will be ignored",
                    this
                );
                return;
            }

            if (duration <= 0)
            {
                DTLog.LogWarning(
                    $"[Curvy] Controller switch has a duration set to {duration}. Duration should be a strictly positive value",
                    this
                );
                Spline = destinationSpline;
                RelativePosition = destinationTf;
            }
            else
                Switcher.Start(
                    destinationSpline,
                    destinationTf,
                    duration,
                    MovementDirection
                );
        }

        /// <summary>
        /// If is switching splines, instantly finishes the current switch.
        /// </summary>
        public void FinishCurrentSwitch()
        {
#if CURVY_SANITY_CHECKS
            Assert.IsTrue(
                isInitialized,
                ControllerNotReadyMessage
            );
#endif
            if (!Switcher.IsSwitching)
                return;

            Spline = Switcher.Spline;
            RelativePosition = Switcher.Tf;

            Switcher.Stop();
        }

        /// <summary>
        /// If is switching splines, cancels the current switch.
        /// </summary>
        public void CancelCurrentSwitch()
        {
#if CURVY_SANITY_CHECKS
            Assert.IsTrue(
                isInitialized,
                ControllerNotReadyMessage
            );
#endif
            if (!Switcher.IsSwitching)
                return;

            Switcher.Stop();
        }

        #endregion


        /// <summary>
        /// Get the direction change, in degrees, of controller caused by the crossing of a connection.
        /// </summary>
        /// <param name="before">The control point the controller is on before crossing the connection</param>
        /// <param name="movementMode">The movement mode the controller has before crossing the connection</param>
        /// <param name="after">The control point the controller is on after crossing the connection</param>
        /// <param name="allowMovementModeChange">If true, the controller will change movemen mode to best fit the after control point. <see cref="AllowDirectionChange"/></param>
        /// <returns>A positif angle in degrees</returns>
        public static float GetAngleBetweenConnectedSplines(CurvySplineSegment before, MovementDirection movementMode,
            CurvySplineSegment after, bool allowMovementModeChange)
        {
            Vector3 currentTangent = before.GetTangentFast(0) * movementMode.ToInt();
            Vector3 newTangent = after.GetTangentFast(0)
            * GetPostConnectionDirection(
                after,
                movementMode,
                allowMovementModeChange
            ).ToInt();
            return Vector3.Angle(
                currentTangent,
                newTangent
            );
        }

        #endregion

        #region ### Protected Methods ###

        public override bool IsReady => ReferenceEquals(
                                            Spline,
                                            null
                                        )
                                        == false
                                        && Spline.IsInitialized;

        #region Preplay state

        protected override void SavePrePlayState()
        {
            prePlaySpline = Spline;
            base.SavePrePlayState();
        }

        protected override void RestorePrePlayState()
        {
            Spline = prePlaySpline;
            base.RestorePrePlayState();
        }

        protected override void ResetPrePlayState()
        {
            prePlaySpline = default;
            base.ResetPrePlayState();
        }

        #endregion

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
            return Spline.TFToDistance(
                relativeDistance,
                Clamping
            );
        }


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
            return Spline.DistanceToTF(
                worldUnitDistance,
                Clamping
            );
        }

        protected override Vector3 GetInterpolatedSourcePosition(float tf)
        {
            Vector3 p = UseCache
                ? Spline.InterpolateFast(tf)
                : Spline.Interpolate(tf);

            return Spline.transform.TransformPoint(p);
        }


        protected override void GetInterpolatedSourcePosition(float tf, out Vector3 interpolatedPosition, out Vector3 tangent,
            out Vector3 up)
        {
            CurvySpline spline = Spline;
            Transform splineTransform = spline.transform;

            float localF;
            CurvySplineSegment currentSegment = spline.TFToSegment(
                tf,
                out localF
            );
            if (ReferenceEquals(
                    currentSegment,
                    null
                )
                == false)
            {
                if (UseCache)
                    currentSegment.InterpolateAndGetTangentFast(
                        localF,
                        out interpolatedPosition,
                        out tangent
                    );
                else
                {
                    interpolatedPosition = currentSegment.Interpolate(
                        localF,
                        Space.Self
                    );
                    tangent = currentSegment.GetTangent(
                        localF,
                        Space.Self
                    );
                }

                up = currentSegment.GetOrientationUpFast(localF);
            }

            else
            {
                interpolatedPosition = Vector3.zero;
                tangent = Vector3.zero;
                up = Vector3.zero;
            }

            interpolatedPosition = splineTransform.TransformPoint(interpolatedPosition);
            tangent = splineTransform.TransformDirection(tangent);
            up = splineTransform.TransformDirection(up);
        }

        protected override Vector3 GetTangent(float tf)
        {
            Vector3 t = UseCache
                ? Spline.GetTangentFast(tf)
                : Spline.GetTangent(tf);
            return Spline.transform.TransformDirection(t);
        }

        protected override Vector3 GetOrientation(float tf)
            => Spline.transform.TransformDirection(Spline.GetOrientationUpFast(tf));

        protected override void Advance(float speed, float deltaTime)
        {
            float distance = speed * deltaTime;

#if CURVY_SANITY_CHECKS
            Assert.IsTrue(distance > 0);
#endif

            if (Spline.Count != 0)
                EventAwareMove(distance);

            if (Switcher.IsSwitching && Switcher.Spline.Count > 0)
                AdvanceSwitching(distance);
        }

        protected override void SimulateAdvance(ref float tf, ref MovementDirection direction, float speed, float deltaTime)
        {
            float distance = speed * deltaTime;
            SimulateAdvanceOnSpline(
                Spline,
                ref tf,
                ref direction,
                distance,
                MoveMode,
                Clamping
            );
        }

        private static void SimulateAdvanceOnSpline(CurvySpline spline, ref float tf, ref MovementDirection direction,
            float distance, MoveModeEnum moveModeEnum, CurvyClamping curvyClamping)
        {
            if (spline.Count > 0)
            {
                int directionInt = direction.ToInt();
                switch (moveModeEnum)
                {
                    case MoveModeEnum.AbsolutePrecise:
                        tf = spline.DistanceToTF(
                            spline.ClampDistance(
                                spline.TFToDistance(tf) + (distance * directionInt),
                                ref directionInt,
                                curvyClamping
                            )
                        );
                        break;
                    case MoveModeEnum.Relative:
                        tf = CurvyUtility.ClampTF(
                            tf + (distance * directionInt),
                            ref directionInt,
                            curvyClamping
                        );
                        break;
                    default:
                        throw new NotSupportedException();
                }

                direction = MovementDirectionMethods.FromInt(directionInt);
            }
        }

        protected override void InitializedApplyDeltaTime(float deltaTime)
        {
            if (Spline.Dirty)
                Spline.Refresh();

            base.InitializedApplyDeltaTime(deltaTime);

            if (Switcher.IsSwitching && Switcher.Progress >= 1)
                FinishCurrentSwitch();
        }

        protected override void ComputeTargetPositionAndRotation(out Vector3 targetPosition, out Vector3 targetUp,
            out Vector3 targetForward)
        {
            Vector3 positionOnCurrentSpline;
            Vector3 upOnCurrentSpline;
            Vector3 forwardOnCurrentSpline;

            base.ComputeTargetPositionAndRotation(
                out positionOnCurrentSpline,
                out upOnCurrentSpline,
                out forwardOnCurrentSpline
            );

            if (Switcher.IsSwitching)
            {
                GetSwitchingPositionAndRotation(
                    forwardOnCurrentSpline,
                    upOnCurrentSpline,
                    positionOnCurrentSpline,
                    out Vector3 switchingPosition,
                    out Quaternion switchingRotation
                );

                targetPosition = switchingPosition;
                targetUp = switchingRotation * Vector3.up;
                targetForward = switchingRotation * Vector3.forward;
            }
            else
            {
                targetPosition = positionOnCurrentSpline;
                targetUp = upOnCurrentSpline;
                targetForward = forwardOnCurrentSpline;
            }
        }
#if DOCUMENTATION___FORCE_IGNORE___CURVY == false
        protected override void ResetOnEnable()
        {
            base.ResetOnEnable();

            preAllocatedEventArgs.Set_INTERNAL(
                this,
                Spline,
                null,
                Single.NaN,
                Single.NaN,
                MovementDirection.Forward,
                false
            );
            Switcher.Stop();
        }
#endif

        #endregion

        #region ### Privates ###

        #region ### Switcher  ###

        private void AdvanceSwitching(float distance)
        {
            Switcher.Advance(
                Switcher.Spline,
                MoveMode,
                distance,
                Clamping
            );

            preAllocatedEventArgs.Set_INTERNAL(
                this,
                Switcher.Spline,
                null,
                Switcher.Tf,
                Switcher.Progress,
                Switcher.Direction,
                false
            );

            OnSwitch.Invoke(preAllocatedEventArgs);

            if (preAllocatedEventArgs.Cancel)
                CancelCurrentSwitch();
        }

        private void GetSwitchingPositionAndRotation(Vector3 forwardOnCurrentSpline, Vector3 upOnCurrentSpline,
            Vector3 positionOnCurrentSpline, out Vector3 interpolatedPosition, out Quaternion interpolatedRotation)
        {
            Quaternion rotationOnCurrentSpline = Quaternion.LookRotation(
                forwardOnCurrentSpline,
                upOnCurrentSpline
            );

            ComputePositionAndRotationOnSwitchTarget(
                out Vector3 positionOnSwitchTarget,
                out Quaternion rotationOnSwitchTarget
            );

            interpolatedPosition = OptimizedOperators.LerpUnclamped(
                positionOnCurrentSpline,
                positionOnSwitchTarget,
                Switcher.Progress
            );

            interpolatedRotation = Quaternion.LerpUnclamped(
                rotationOnCurrentSpline,
                rotationOnSwitchTarget,
                Switcher.Progress
            );
        }

        private void ComputePositionAndRotationOnSwitchTarget(out Vector3 positionOnSwitchToSpline,
            out Quaternion rotationOnSwitchToSpline)
        {
            CurvySpline preSwitchSpline = Spline;
            float preSwitchSplineTf = RelativePosition;

            m_Spline = Switcher.Spline;
            RelativePosition = Switcher.Tf;

            Vector3 upOnSwitchToSpline;
            Vector3 forwardOnSwitchToSpline;
            base.ComputeTargetPositionAndRotation(
                out positionOnSwitchToSpline,
                out upOnSwitchToSpline,
                out forwardOnSwitchToSpline
            );

            rotationOnSwitchToSpline = Quaternion.LookRotation(
                forwardOnSwitchToSpline,
                upOnSwitchToSpline
            );

            m_Spline = preSwitchSpline;
            RelativePosition = preSwitchSplineTf;
        }

        #endregion


        /// <summary>
        /// This method gets the controller position, but handles the looping differently than usual (it does not change a relative position of 1 to 0), which avoids hardly solvable ambiguities in the movement logic.
        /// </summary>
        /// <remarks>This is to make controller logic simpler, since it does not need anymore to guess if a position of 0 meant controller on the end of the spline and needed looping, or meant that the controller is on the start of the spline.</remarks>
        /// <param name="positionMode"> The one of the returned position</param>
        /// <param name="clampedPosition"> Uses the controller's <see cref="CurvyController.PositionMode"/></param>
        private static float MovementCompatibleGetPosition(SplineController controller, float clampedPosition,
            CurvyPositionMode positionMode, out CurvySplineSegment controlPoint, out bool isOnControlPoint)
        {
            float resultPosition;
            CurvySpline spline = controller.Spline;

            bool isOnSegmentLastCp;
            bool isOnSegmentFirstCp;
            float unconvertedLocalPosition;
            switch (controller.PositionMode)
            {
                case CurvyPositionMode.Relative:
                    controlPoint = spline.TFToSegment(
                        clampedPosition,
                        out unconvertedLocalPosition,
                        out isOnSegmentFirstCp,
                        out isOnSegmentLastCp,
                        CurvyClamping.Clamp
                    ); //CurvyClamping.Clamp to cancel looping handling
                    break;
                case CurvyPositionMode.WorldUnits:
                    controlPoint = spline.DistanceToSegment(
                        clampedPosition,
                        out unconvertedLocalPosition,
                        out isOnSegmentFirstCp,
                        out isOnSegmentLastCp
                    ); //CurvyClamping.Clamp to cancel looping handling
                    break;
                default:
                    throw new NotSupportedException();
            }

            if (positionMode == controller.PositionMode)
                resultPosition = clampedPosition;
            else
                switch (positionMode)
                {
                    case CurvyPositionMode.Relative:
                        resultPosition = spline.SegmentToTF(
                            controlPoint,
                            controlPoint.DistanceToLocalF(unconvertedLocalPosition)
                        );
                        break;
                    case CurvyPositionMode.WorldUnits:
                        resultPosition = controlPoint.Distance + controlPoint.LocalFToDistance(unconvertedLocalPosition);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

            if (isOnSegmentLastCp) //Case of last cp of an open spline
                controlPoint = spline.GetNextControlPoint(controlPoint);

            isOnControlPoint = isOnSegmentFirstCp || isOnSegmentLastCp;

            return resultPosition;
        }

        /// <summary>
        /// This method sets the controller position, but handles the looping differently than usual (it does not change a realtive position of 1 to 0), which avoids hardly solvable ambiguities in the movement logic.
        /// </summary>
        /// <remarks>This is to make controller logic simpler, since it does not need anymore to guess if a position of 0 meant controller on the end of the spline and needed looping, or meant that the controller is on the start of the spline.</remarks>
        private static void MovementCompatibleSetPosition(SplineController controller, CurvyPositionMode positionMode,
            float specialClampedPosition)
        {
            float clampedPosition = specialClampedPosition;

            if (positionMode == controller.PositionMode)
                controller.m_Position = clampedPosition;
            else
                switch (positionMode)
                {
                    case CurvyPositionMode.Relative:
                        controller.m_Position = controller.Spline.TFToDistance(
                            clampedPosition,
                            controller.Clamping
                        );
                        break;
                    case CurvyPositionMode.WorldUnits:
                        controller.m_Position = controller.Spline.DistanceToTF(
                            clampedPosition,
                            controller.Clamping
                        );
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
        }

        private const string InvalidSegmentErrorMessage =
            "[Curvy] Controller {0} reached segment {1} which is invalid segment because it has a length of 0. Please fix the invalid segment to avoid issues with the controller";

        /// <summary>
        /// Updates position and direction while triggering events when reaching a control point
        /// </summary>
        /// <param name="distance"></param>
        /// <param name="positionMode">The position mode used in the computations. Could be different than SplineController.PositionMode</param>
        private void EventAwareMove(float distance)
        {
#if CURVY_SANITY_CHECKS
            MoveModeEnum moveModeAtMethodStart = MoveMode;
            Assert.IsTrue(distance > 0);
#endif
            CurvyPositionMode movementRelatedPositionMode;
            switch (MoveMode)
            {
                case MoveModeEnum.AbsolutePrecise:
                    movementRelatedPositionMode = CurvyPositionMode.WorldUnits;
                    break;
                case MoveModeEnum.Relative:
                    movementRelatedPositionMode = CurvyPositionMode.Relative;
                    break;
                default:
                    throw new NotSupportedException();
            }

            float currentDelta = distance;

            bool cancelMovement = false;

            //Handle when controller starts at special position
            switch (MovementDirection)
            {
                case MovementDirection.Backward:
                    if (m_Position == 0)
                        if (Clamping == CurvyClamping.PingPong)
                            MovementDirection = MovementDirection.GetOpposite();
                        else if (Clamping == CurvyClamping.Clamp)
                            return;
                    break;
                case MovementDirection.Forward:
                    float upperLimit;
                {
                    switch (PositionMode)
                    {
                        case CurvyPositionMode.Relative:
                            upperLimit = 1f;
                            break;
                        case CurvyPositionMode.WorldUnits:
                            upperLimit = m_Spline.Length;
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }
                    if (m_Position == upperLimit)
                        if (Clamping == CurvyClamping.PingPong)
                            MovementDirection = MovementDirection.GetOpposite();
                        else if (Clamping == CurvyClamping.Clamp)
                            return;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            CurvySplineSegment currentCp;
            bool isOnCp;
            float movementCompatibleCurrentPosition;
            movementCompatibleCurrentPosition = MovementCompatibleGetPosition(
                this,
                m_Position,
                movementRelatedPositionMode,
                out currentCp,
                out isOnCp
            );

            if (currentCp.Length == 0 && Spline.IsControlPointASegment(currentCp))
                DTLog.LogWarning(
                    String.Format(
                        InvalidSegmentErrorMessage,
                        name,
                        currentCp
                    ),
                    this
                );

            int infiniteLoopSafety = 10000;
            while (!cancelMovement && currentDelta > 0 && infiniteLoopSafety-- > 0)
            {
#if CURVY_SANITY_CHECKS
                Assert.IsTrue(Spline.Count > 0);
                Assert.IsTrue(
                    moveModeAtMethodStart == MoveMode
                ); // MoveMode is not allowed to be modified while moving a Spline Controller;
#endif
                CurvySplineSegment candidateControlPoint;
                {
                    if (MovementDirection == MovementDirection.Forward)
                        candidateControlPoint = Spline.GetNextControlPoint(currentCp);
                    else
                        candidateControlPoint = isOnCp
                            ? Spline.GetPreviousControlPoint(currentCp)
                            : currentCp;
                }

                if (ReferenceEquals(
                        candidateControlPoint,
                        null
                    )
                    == false
                    && Spline.IsControlPointVisible(candidateControlPoint))
                {
                    float candidateControlPointPosition;
                    {
                        candidateControlPointPosition = GetControlPointPosition(
                            candidateControlPoint,
                            movementRelatedPositionMode
                        );
                        //handles first cp of closed spline having two values: 0 and max value
                        if (MovementDirection == MovementDirection.Forward
                            && m_Spline.Closed
                            && candidateControlPointPosition == 0)
                            candidateControlPointPosition = GetMaxPosition(movementRelatedPositionMode);
                    }

                    float distanceToCandidate = Mathf.Abs(candidateControlPointPosition - movementCompatibleCurrentPosition);

                    float postEventsEndPosition;

                    if (distanceToCandidate > currentDelta) //If no more control point to reach, move the controller and exit
                    {
                        float movementCompatibleNewPosition_Unclamped =
                            movementCompatibleCurrentPosition + (currentDelta * MovementDirection.ToInt());

                        float movementCompatibleNewPosition_Clamped = GetClampedPosition(
                            movementCompatibleNewPosition_Unclamped,
                            movementRelatedPositionMode,
                            Clamping,
                            m_Spline.Length
                        );

                        HandleOnPositionReachedEvents(
                            movementRelatedPositionMode,
                            movementCompatibleCurrentPosition,
                            movementCompatibleNewPosition_Clamped,
                            movementCompatibleNewPosition_Unclamped,
                            out postEventsEndPosition,
                            currentDelta,
                            currentCp,
                            ref cancelMovement
                        );

                        MovementCompatibleSetPosition(
                            this,
                            movementRelatedPositionMode,
                            postEventsEndPosition
                        );

                        break;
                    }

                    HandleOnPositionReachedEvents(
                        movementRelatedPositionMode,
                        movementCompatibleCurrentPosition,
                        candidateControlPointPosition,
                        candidateControlPointPosition,
                        out postEventsEndPosition,
                        currentDelta,
                        currentCp,
                        ref cancelMovement
                    );

                    if (postEventsEndPosition.Approximately(candidateControlPointPosition) == false)
                        DTLog.LogWarning(
                            $"[Curvy] Spline Controller {name}: Position was modified in an {nameof(OnPositionReachedList)} event handler. That modification will be ignored to prioritize the controller reaching a new control point. You can use the {nameof(OnControlPointReached)} event or {nameof(OnEndReached)} instead. If this behavior is problematic, please contact the developers.",
                            this
                        );

                    currentDelta -= distanceToCandidate;

                    //Move to next control point
                    HandleReachingNewControlPoint(
                        candidateControlPoint,
                        candidateControlPointPosition,
                        movementRelatedPositionMode,
                        currentDelta,
                        ref cancelMovement,
                        out currentCp,
                        out isOnCp,
                        out movementCompatibleCurrentPosition
                    );
                }

                //handle connection
                {
                    if (isOnCp && currentCp.Connection && currentCp.Connection.ControlPointsList.Count > 1)
                    {
                        MovementDirection newDirection;
                        CurvySplineSegment postConnectionHandlingControlPoint;
                        switch (ConnectionBehavior)
                        {
                            case SplineControllerConnectionBehavior.CurrentSpline:
                                postConnectionHandlingControlPoint = currentCp;
                                newDirection = MovementDirection;
                                break;
                            case SplineControllerConnectionBehavior.FollowUpSpline:
                                postConnectionHandlingControlPoint = HandleFollowUpConnectionBehavior(
                                    currentCp,
                                    MovementDirection,
                                    out newDirection
                                );
                                break;
                            case SplineControllerConnectionBehavior.FollowUpOtherwiseRandom:
                                postConnectionHandlingControlPoint = currentCp.FollowUp
                                    ? HandleFollowUpConnectionBehavior(
                                        currentCp,
                                        MovementDirection,
                                        out newDirection
                                    )
                                    : HandleRandomConnectionBehavior(
                                        currentCp,
                                        MovementDirection,
                                        out newDirection,
                                        currentCp.Connection.ControlPointsList
                                    );
                                break;
                            case SplineControllerConnectionBehavior.RandomSpline:
                                postConnectionHandlingControlPoint = HandleRandomConnectionBehavior(
                                    currentCp,
                                    MovementDirection,
                                    out newDirection,
                                    currentCp.Connection.ControlPointsList
                                );
                                break;
                            case SplineControllerConnectionBehavior.Custom:
                                if (ConnectionCustomSelector == null)
                                {
                                    DTLog.LogError(
                                        "[Curvy] You need to set a non null ConnectionCustomSelector when using SplineControllerConnectionBehavior.Custom",
                                        this
                                    );
                                    postConnectionHandlingControlPoint = currentCp;
                                }
                                else
                                    postConnectionHandlingControlPoint = ConnectionCustomSelector.SelectConnectedControlPoint(
                                        this,
                                        currentCp.Connection,
                                        currentCp
                                    );

                                newDirection = MovementDirection;
                                break;
                            default:
                                throw new ArgumentOutOfRangeException();
                        }

                        if (ReferenceEquals(
                                postConnectionHandlingControlPoint,
                                currentCp
                            )
                            == false)
                        {
                            MovementDirection = newDirection;
                            float postConnectionHandlingControlPointPosition = GetControlPointPosition(
                                postConnectionHandlingControlPoint,
                                movementRelatedPositionMode
                            );
                            HandleReachingNewControlPoint(
                                postConnectionHandlingControlPoint,
                                postConnectionHandlingControlPointPosition,
                                movementRelatedPositionMode,
                                currentDelta,
                                ref cancelMovement,
                                out currentCp,
                                out isOnCp,
                                out movementCompatibleCurrentPosition
                            );
                        }
                    }
                }

                //handle clamping
                {
                    if (isOnCp)
                        switch (Clamping)
                        {
                            case CurvyClamping.Loop:
                                if (Spline.Closed == false)
                                {
                                    CurvySplineSegment newControlPoint;
                                    if (MovementDirection == MovementDirection.Backward
                                        && ReferenceEquals(
                                            currentCp,
                                            Spline.FirstVisibleControlPoint
                                        ))
                                        newControlPoint = Spline.LastVisibleControlPoint;
                                    else if (MovementDirection == MovementDirection.Forward
                                             && ReferenceEquals(
                                                 currentCp,
                                                 Spline.LastVisibleControlPoint
                                             ))
                                        newControlPoint = Spline.FirstVisibleControlPoint;
                                    else
                                        newControlPoint = null;

                                    if (ReferenceEquals(
                                            newControlPoint,
                                            null
                                        )
                                        == false)
                                    {
                                        float newControlPointPosition = GetControlPointPosition(
                                            newControlPoint,
                                            movementRelatedPositionMode
                                        );
                                        HandleReachingNewControlPoint(
                                            newControlPoint,
                                            newControlPointPosition,
                                            movementRelatedPositionMode,
                                            currentDelta,
                                            ref cancelMovement,
                                            out currentCp,
                                            out isOnCp,
                                            out movementCompatibleCurrentPosition
                                        );
                                    }
                                }

                                break;
                            case CurvyClamping.Clamp:
                                if ((MovementDirection == MovementDirection.Backward
                                    && ReferenceEquals(
                                        currentCp,
                                        Spline.FirstVisibleControlPoint
                                    ))
                                    || (MovementDirection == MovementDirection.Forward
                                    && ReferenceEquals(
                                        currentCp,
                                        Spline.LastVisibleControlPoint
                                    )))
                                    currentDelta = 0;
                                break;
                            case CurvyClamping.PingPong:
                                if ((MovementDirection == MovementDirection.Backward
                                    && ReferenceEquals(
                                        currentCp,
                                        Spline.FirstVisibleControlPoint
                                    ))
                                    || (MovementDirection == MovementDirection.Forward
                                    && ReferenceEquals(
                                        currentCp,
                                        Spline.LastVisibleControlPoint
                                    )))
                                    MovementDirection = MovementDirection.GetOpposite();
                                break;
                            default:
                                throw new ArgumentOutOfRangeException();
                        }
                }
            }

            if (infiniteLoopSafety <= 0)
                DTLog.LogError(
                    String.Format(
                        "[Curvy] Unexpected behavior in Spline Controller '{0}'. Please raise a Bug Report.",
                        name
                    ),
                    this
                );
        }

        /// <summary>
        /// Triggers relevant OnPositionReached events if any.
        /// </summary>
        private void HandleOnPositionReachedEvents(
            CurvyPositionMode positionMode,
            float startPosition,
            float endPosition,
            float endPositionUnclamped,
            out float postEventsEndPosition,
            float currentDelta,
            CurvySplineSegment currentCp,
            ref bool cancelMovement)
        {
#if CURVY_SANITY_CHECKS
            switch (MovementDirection)
            {
                case MovementDirection.Forward:
                    Assert.IsTrue(startPosition <= endPositionUnclamped);
                    break;
                case MovementDirection.Backward:
                    Assert.IsTrue(startPosition > endPositionUnclamped);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
#endif

            float? nullablePostEventsEndPosition = null;
            foreach (OnPositionReachedSettings eventSettings in OnPositionReachedList)
            {
                nullablePostEventsEndPosition = HandleOnPositionReachedEvent(
                    positionMode,
                    startPosition,
                    endPositionUnclamped,
                    currentDelta,
                    currentCp,
                    ref cancelMovement,
                    eventSettings,
                    nullablePostEventsEndPosition
                );

                if (Spline.Closed)
                {
                    //handles first cp of closed spline having two values: 0 and max value. In the controller logic, that point has a value of max value when going forward, and 0 when going backwards. To handle this, we create two events, one for each value of the ambiguous point 

                    OnPositionReachedSettings extraEventSettings;
                    if (MovementDirection == MovementDirection.Forward
                        && eventSettings.Position == 0)
                    {
                        extraEventSettings = eventSettings.Clone();
                        extraEventSettings.Position = GetMaxPosition(eventSettings.PositionMode);
                    }
                    else if (MovementDirection == MovementDirection.Backward
                             && Mathf.Approximately(
                                 eventSettings.Position,
                                 GetMaxPosition(eventSettings.PositionMode)
                             ))
                    {
                        extraEventSettings = eventSettings.Clone();
                        extraEventSettings.Position = 0;
                    }
                    else
                        extraEventSettings = null;

                    if (extraEventSettings != null)
                        nullablePostEventsEndPosition = HandleOnPositionReachedEvent(
                            positionMode,
                            startPosition,
                            endPositionUnclamped,
                            currentDelta,
                            currentCp,
                            ref cancelMovement,
                            extraEventSettings,
                            nullablePostEventsEndPosition
                        );
                }
            }


            postEventsEndPosition = nullablePostEventsEndPosition ?? endPosition;
        }

        private float? HandleOnPositionReachedEvent(
            CurvyPositionMode positionMode,
            float startPosition,
            float endPositionUnclamped,
            float currentDelta,
            CurvySplineSegment currentCp,
            ref bool cancelMovement,
            OnPositionReachedSettings settings,
            float? postEventEndPosition)
        {
            //Debug.Log($"{customEvent.PositionMode} : {startPosition} {endPosition}");

            float eventPosition;
            {
                if (positionMode == settings.PositionMode)
                    eventPosition = settings.Position;
                else
                    switch (positionMode)
                    {
                        case CurvyPositionMode.Relative:
                            eventPosition = Spline.DistanceToTF(settings.Position);
                            break;
                        case CurvyPositionMode.WorldUnits:
                            eventPosition = Spline.TFToDistance(settings.Position);
                            break;
                        default:
                            throw new ArgumentOutOfRangeException(
                                nameof(positionMode),
                                positionMode,
                                null
                            );
                    }
            }

            TriggeringDirections triggeringDirections = settings.TriggeringDirections;

            bool isForwardEventTriggered =
                (triggeringDirections == TriggeringDirections.All || triggeringDirections == TriggeringDirections.Forward)
                && startPosition < eventPosition
                && eventPosition <= endPositionUnclamped;
            bool isBackwardEventTriggered =
                (triggeringDirections == TriggeringDirections.All || triggeringDirections == TriggeringDirections.Backward)
                && endPositionUnclamped <= eventPosition
                && eventPosition < startPosition;

            if (isForwardEventTriggered || isBackwardEventTriggered)
            {
                float delta = Math.Abs(eventPosition - startPosition);

                //every custom event triggering will modify the controller's position to the event's position. This will override any possible position modification by a prior event handler
                MovementCompatibleSetPosition(
                    this,
                    settings.PositionMode,
                    eventPosition
                );

                preAllocatedEventArgs.Set_INTERNAL(
                    this,
                    Spline,
                    currentCp,
                    eventPosition,
                    currentDelta - delta,
                    MovementDirection,
                    settings.PositionMode == CurvyPositionMode.WorldUnits
                );

                InvokeEventHandler(
                    settings.Event,
                    preAllocatedEventArgs,
                    positionMode,
                    out _,
                    out _,
                    out postEventEndPosition
                );

                cancelMovement |= preAllocatedEventArgs.Cancel;
            }

            return postEventEndPosition;
        }

        /// <summary>
        /// Do operations necessary when controller reaches a new control point: setting the controller position, update its spline if necessary, and send events if necessary
        /// </summary>
        private void HandleReachingNewControlPoint(CurvySplineSegment controlPoint,
            float controlPointPosition,
            CurvyPositionMode positionMode,
            float currentDelta,
            ref bool cancelMovement,
            out CurvySplineSegment postEventsControlPoint,
            out bool postEventsIsControllerOnControlPoint,
            out float postEventsControlPointPosition)
        {
            //update state
            MovementCompatibleSetPosition(
                this,
                positionMode,
                controlPointPosition
            );
            Spline = controlPoint.Spline;
            postEventsControlPoint = controlPoint;
            postEventsIsControllerOnControlPoint = true;
            postEventsControlPointPosition = controlPointPosition;

            //handle invalid situation
            if (controlPoint.Length == 0 && Spline.IsControlPointASegment(controlPoint))
                DTLog.LogWarning(
                    String.Format(
                        InvalidSegmentErrorMessage,
                        name,
                        controlPoint
                    ),
                    this
                );


            //setup event param
            preAllocatedEventArgs.Set_INTERNAL(
                this,
                Spline,
                controlPoint,
                controlPointPosition,
                currentDelta,
                MovementDirection,
                positionMode == CurvyPositionMode.WorldUnits
            );

            //handle OnControlPointReached
            InvokeEventHandler(
                OnControlPointReached,
                preAllocatedEventArgs,
                positionMode,
                ref postEventsControlPoint,
                ref postEventsIsControllerOnControlPoint,
                ref postEventsControlPointPosition
            );

            //handle OnEndReached
            if (ReferenceEquals(
                    preAllocatedEventArgs.Spline.FirstVisibleControlPoint,
                    preAllocatedEventArgs.ControlPoint
                )
                || ReferenceEquals(
                    preAllocatedEventArgs.Spline.LastVisibleControlPoint,
                    preAllocatedEventArgs.ControlPoint
                ))
                InvokeEventHandler(
                    OnEndReached,
                    preAllocatedEventArgs,
                    positionMode,
                    ref postEventsControlPoint,
                    ref postEventsIsControllerOnControlPoint,
                    ref postEventsControlPointPosition
                );

            cancelMovement |= preAllocatedEventArgs.Cancel;
        }

        private void InvokeEventHandler(CurvySplineMoveEvent @event,
            CurvySplineMoveEventArgs eventArgument,
            CurvyPositionMode positionMode,
            ref CurvySplineSegment postEventsControlPoint,
            ref bool postEventsIsControllerOnControlPoint,
            ref float postEventPosition)
        {
            InvokeEventHandler(
                @event,
                eventArgument,
                positionMode,
                out CurvySplineSegment outControlPoint,
                out bool? outIsControllerOnControlPoint,
                out float? outPosition
            );

            if (outPosition != null)
                postEventPosition = outPosition.Value;
            if (outIsControllerOnControlPoint != null)
                postEventsIsControllerOnControlPoint = outIsControllerOnControlPoint.Value;
            if (outControlPoint != null)
                postEventsControlPoint = outControlPoint;
        }

        private void InvokeEventHandler(CurvySplineMoveEvent @event,
            CurvySplineMoveEventArgs eventArgument,
            CurvyPositionMode positionMode,
            out CurvySplineSegment postEventsControlPoint,
            out bool? postEventsIsControllerOnControlPoint,
            out float? postEventPosition)
        {
            //save some data before calling events to know if event handlers changed important state
            float preEventPosition = m_Position;
            CurvyPositionMode preEventPositionMode = PositionMode;
            CurvySpline preEventPositionSpline = m_Spline;
            //call event handler
            @event.Invoke(eventArgument);
            //update state if event handler changed important things
            if (m_Position != preEventPosition
                || PositionMode != preEventPositionMode
                || ReferenceEquals(
                    m_Spline,
                    preEventPositionSpline
                )
                == false)
            {
                postEventPosition = MovementCompatibleGetPosition(
                    this,
                    m_Position,
                    positionMode,
                    out postEventsControlPoint,
                    out bool outIsOnCP
                );
                postEventsIsControllerOnControlPoint = outIsOnCP;
            }
            else
            {
                postEventsControlPoint = null;
                postEventsIsControllerOnControlPoint = null;
                postEventPosition = null;
            }
        }

        /// <summary>
        /// Get the correct control point and direction from applying the Random connection handling logic
        /// </summary>
        private CurvySplineSegment HandleRandomConnectionBehavior(CurvySplineSegment currentControlPoint,
            MovementDirection currentDirection, out MovementDirection newDirection,
            ReadOnlyCollection<CurvySplineSegment> connectedControlPoints)
        {
            //OPTIM avoid allocation
            List<CurvySplineSegment> validConnectedControlPoints = new List<CurvySplineSegment>(connectedControlPoints.Count);

            for (int index = 0; index < connectedControlPoints.Count; index++)
            {
                CurvySplineSegment controlPoint = connectedControlPoints[index];
                if (RejectCurrentSpline && controlPoint == currentControlPoint)
                    continue;

                if (RejectTooDivergentSplines)
                    if (GetAngleBetweenConnectedSplines(
                            currentControlPoint,
                            currentDirection,
                            controlPoint,
                            AllowDirectionChange
                        )
                        > MaxAllowedDivergenceAngle)
                        continue;

                validConnectedControlPoints.Add(controlPoint);
            }

            CurvySplineSegment newControlPoint = validConnectedControlPoints.Count == 0
                ? currentControlPoint
                : validConnectedControlPoints[Random.Range(
                    0,
                    validConnectedControlPoints.Count
                )];

            newDirection = GetPostConnectionDirection(
                newControlPoint,
                currentDirection,
                AllowDirectionChange
            );

            return newControlPoint;
        }

        /// <summary>
        /// Get the direction the controller should have if moving through a specific connected Control Point
        /// </summary>
        private static MovementDirection GetPostConnectionDirection(CurvySplineSegment connectedControlPoint,
            MovementDirection currentDirection, bool directionChangeAllowed)
            => directionChangeAllowed && connectedControlPoint.Spline.Closed == false
                ? HeadingToDirection(
                    ConnectionHeadingEnum.Auto,
                    connectedControlPoint,
                    currentDirection
                )
                : currentDirection;

        /// <summary>
        /// Get the correct control point and direction from applying the FollowUp connection handling logic
        /// </summary>
        private CurvySplineSegment HandleFollowUpConnectionBehavior(CurvySplineSegment currentControlPoint,
            MovementDirection currentDirection, out MovementDirection newDirection)
        {
            CurvySplineSegment newControlPoint = currentControlPoint.FollowUp
                ? currentControlPoint.FollowUp
                : currentControlPoint;

            newDirection = AllowDirectionChange && currentControlPoint.FollowUp
                ? HeadingToDirection(
                    currentControlPoint.FollowUpHeading,
                    currentControlPoint.FollowUp,
                    currentDirection
                )
                : currentDirection;

            return newControlPoint;
        }

        /// <summary>
        /// Translates a heading value to a controller direction, based on the current control point situation
        /// </summary>
        private static MovementDirection HeadingToDirection(ConnectionHeadingEnum heading, CurvySplineSegment controlPoint,
            MovementDirection currentDirection)
        {
            MovementDirection newDirection;
            ConnectionHeadingEnum resolveHeading = heading.ResolveAuto(controlPoint);

            switch (resolveHeading)
            {
                case ConnectionHeadingEnum.Minus:
                    newDirection = MovementDirection.Backward;
                    break;
                case ConnectionHeadingEnum.Sharp:
                    newDirection = currentDirection;
                    break;
                case ConnectionHeadingEnum.Plus:
                    newDirection = MovementDirection.Forward;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            return newDirection;
        }


        /// <summary>
        /// Get the controller position corresponding to a specific control point
        /// </summary>
        private static float GetControlPointPosition(CurvySplineSegment controlPoint, CurvyPositionMode positionMode)
        {
            float position;
            switch (positionMode)
            {
                case CurvyPositionMode.Relative:
                    position = controlPoint.TF;
                    break;
                case CurvyPositionMode.WorldUnits:
                    position = controlPoint.Distance;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            return position;
        }

        /// <summary>
        /// Used as a field condition
        /// </summary>
        private bool ShowRandomConnectionOptions =>
            ConnectionBehavior == SplineControllerConnectionBehavior.FollowUpOtherwiseRandom
            || ConnectionBehavior == SplineControllerConnectionBehavior.RandomSpline;

        #endregion
    }
}