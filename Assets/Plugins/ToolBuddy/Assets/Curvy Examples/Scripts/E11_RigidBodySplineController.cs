// =====================================================================
// Copyright © 2013 ToolBuddy
// All rights reserved
// 
// http://www.toolbuddy.net
// =====================================================================

using FluffyUnderware.Curvy.Controllers;
using JetBrains.Annotations;
using UnityEngine;

namespace FluffyUnderware.Curvy.Examples
{
    [RequireComponent(typeof(Rigidbody))]
    public class E11_RigidBodySplineController : MonoBehaviour
    {
        public CurvySpline Spline;
        public SplineController CameraController;
        public float VSpeed = 10;
        public float HSpeed = 0.5f;
        public float CenterDrag = 0.5f;
        public float JumpForce = 10;

        private Rigidbody mRigidBody;
        private float mTF;
        private float velocity;


        // Use this for initialization
        [UsedImplicitly]
        private void Start() =>
            mRigidBody = GetComponent<Rigidbody>();

        [UsedImplicitly]
        private void LateUpdate()
        {
            if (CameraController)
            {
                // Camera has a regular SplineController attached. Set it to 5 units behind the player
                float newCamAbs = Spline.TFToDistance(mTF) - 5;
                CameraController.AbsolutePosition = Mathf.SmoothDamp(
                    CameraController.AbsolutePosition,
                    newCamAbs,
                    ref velocity,
                    0.5f
                );
            }
        }

        [UsedImplicitly]
        private void FixedUpdate()
        {
            if (Spline)
            {
                float v = Input.GetAxis("Vertical") * VSpeed;
                float h = Input.GetAxis("Horizontal") * HSpeed;

                Vector3 p;
                // get nearest TF and point on spline
                mTF = Spline.GetNearestPointTF(
                    transform.localPosition,
                    out p
                );
                // apply forward thrust along spline direction (tangent)
                if (v != 0)
                    mRigidBody.AddForce(
                        Spline.GetTangentFast(mTF) * v,
                        ForceMode.Force
                    );
                // apply side thrust to left/right from the spline's "forward" vector
                if (h != 0)
                {
                    Vector3 offset = Spline.InterpolateFast(mTF)
                    + (Quaternion.AngleAxis(
                           90,
                           Spline.GetTangentFast(mTF)
                       )
                       * Spline.GetOrientationUpFast(mTF));
                    Vector3 hdir = p - offset;
                    mRigidBody.AddForce(
                        hdir * h,
                        ForceMode.Force
                    );
                }

                if (Input.GetKeyDown(KeyCode.Space))
                    mRigidBody.AddForce(
                        Vector3.up * JumpForce,
                        ForceMode.Impulse
                    );

                // continously drag toward the spline to add some magic gravity
                mRigidBody.AddForce(
                    (Spline.Interpolate(mTF) - transform.localPosition) * CenterDrag,
                    ForceMode.VelocityChange
                );
            }
        }
    }
}