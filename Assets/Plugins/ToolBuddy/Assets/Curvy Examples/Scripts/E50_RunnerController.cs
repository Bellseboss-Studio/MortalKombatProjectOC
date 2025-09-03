// =====================================================================
// Copyright © 2013 ToolBuddy
// All rights reserved
// 
// http://www.toolbuddy.net
// =====================================================================

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using FluffyUnderware.Curvy.Controllers;
using FluffyUnderware.DevTools;
using UnityEngine;

namespace FluffyUnderware.Curvy.Examples
{
    /// <summary>
    /// Example custom Controller
    /// </summary>
    public class E50_RunnerController : SplineController
    {
        [Section("Jump")]
        public float JumpHeight = 20;

        public float JumpSpeed = 0.5f;

        public AnimationCurve JumpCurve = AnimationCurve.Linear(
            0,
            0,
            1,
            1
        );

        public float Gravity = 10;

        private enum GuideMode
        {
            Guided,
            Jumping,
            FreeFall
        }

        private GuideMode mMode;
        private float jumpHeight;
        private float fallingSpeed;

        private E50_SplineRefMetadata mPossibleSwitchTarget;
        private int mSwitchInProgress; // 0=No, 1=to right, -1=to left

        protected override void OnDisable()
        {
            base.OnDisable();
            StopAllCoroutines();
        }


        protected override void InitializedApplyDeltaTime(float deltaTime)
        {
            // Jump?
            if (Input.GetButtonDown("Fire1") && mMode == GuideMode.Guided)
                StartCoroutine(Jump());

            // If allowed to switch and player wants to, initiate switching
            if (mPossibleSwitchTarget != null && mSwitchInProgress == 0)
            {
                float xAxis = Input.GetAxisRaw("Horizontal");
                if (mPossibleSwitchTarget.Options == "Right" && xAxis > 0)
                    Switch(1);
                else if (mPossibleSwitchTarget.Options == "Left" && xAxis < 0)
                    Switch(-1);
            }
            // else check if we need to finalize the switch process
            else if (mSwitchInProgress != 0 && !Switcher.IsSwitching)
            {
                mSwitchInProgress = 0;
                OnCPReached(
                    new CurvySplineMoveEventArgs(
                        this,
                        Spline,
                        Spline.TFToSegment(RelativePosition),
                        0,
                        false,
                        0,
                        MovementDirection.Forward
                    )
                );
            }

            base.InitializedApplyDeltaTime(deltaTime);

            // If falling
            if (mMode == GuideMode.FreeFall)
            {
                // handling falling
                fallingSpeed += Gravity * deltaTime;
                OffsetRadius -= fallingSpeed;
                // check distance to falling target and reenter "guided" mode when near
                if (OffsetRadius <= 0)
                {
                    mMode = GuideMode.Guided;
                    fallingSpeed = 0;
                    OffsetRadius = 0;
                }
            }

            // if we're jumping, translate back to the wanted height above track
            if (mMode == GuideMode.Jumping)
                OffsetRadius = jumpHeight;
        }

        // Initiate Lane Switching
        private void Switch(int dir)
        {
            mSwitchInProgress = dir;
            Vector3 posInTargetSpace = mPossibleSwitchTarget.Spline.transform.InverseTransformPoint(transform.position);
            Vector3 nearestPoint;
            float nearestPointTF = mPossibleSwitchTarget.Spline.GetNearestPointTF(
                posInTargetSpace,
                out nearestPoint,
                mPossibleSwitchTarget.CP.Spline.GetSegmentIndex(mPossibleSwitchTarget.CP)
            );
            float swSpeed = (nearestPoint - posInTargetSpace).magnitude / Speed;
            SwitchTo(
                mPossibleSwitchTarget.Spline,
                nearestPointTF,
                swSpeed
            );
        }

        // Do a Jump
        private IEnumerator Jump()
        {
            mMode = GuideMode.Jumping;
            float start = Time.time;
            float f = 0;
            while (f < 1)
            {
                if (mMode != GuideMode.Jumping) //If other code exited the jump
                    break;

                f = (Time.time - start) / JumpSpeed;
                jumpHeight = JumpCurve.Evaluate(f) * JumpHeight;
                yield return new WaitForEndOfFrame();
            }

            if (mMode == GuideMode.Jumping)
                mMode = GuideMode.Guided;
        }

        // Retrieve switch metadata
        public void OnCPReached(CurvySplineMoveEventArgs e)
        {
            mPossibleSwitchTarget = e.ControlPoint.GetMetadata<E50_SplineRefMetadata>();
            // if not properly configured, ignore!
            if (mPossibleSwitchTarget && !mPossibleSwitchTarget.Spline)
                mPossibleSwitchTarget = null;
        }

        //Start falling when reaching the start of new spline that is not a Follow-Up
        public void UseFollowUpOrFall(CurvySplineMoveEventArgs e)
        {
            CurvySplineSegment controlPoint = e.ControlPoint;
            if (controlPoint == e.Spline.FirstVisibleControlPoint && controlPoint.Connection && controlPoint.FollowUp == false)
            {
                IEnumerable<CurvySplineSegment> otherControlPoints = controlPoint.Connection.ControlPointsList
                    .Where(cp => cp != controlPoint);
                CurvySplineSegment otherControlPoint = otherControlPoints.First();
                float deltaY = controlPoint.transform.position.y - otherControlPoint.transform.position.y;
                mMode = GuideMode.FreeFall;
                fallingSpeed = 0;
                OffsetRadius += Mathf.Abs(deltaY);
            }
        }
    }
}