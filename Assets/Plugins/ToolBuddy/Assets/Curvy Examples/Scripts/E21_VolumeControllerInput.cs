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
    public class E21_VolumeControllerInput : MonoBehaviour
    {
        public float AngularVelocity = 0.2f;
        public ParticleSystem explosionEmitter;
        public VolumeController volumeController;
        public Transform rotatedTransform;
        public float maxSpeed = 40f;
        public float accelerationForward = 20f;
        public float accelerationBackward = 40f;
        private bool mGameOver;

        [UsedImplicitly]
        private void Awake()
        {
            if (!volumeController)
                volumeController = GetComponent<VolumeController>();
        }

        [UsedImplicitly]
        private void Start()
        {
            if (volumeController.IsReady)
                ResetController();
            else
                volumeController.OnInitialized.AddListener(arg0 => ResetController());
        }

        [UsedImplicitly]
        private void ResetController()
        {
            volumeController.Speed = 0;
            volumeController.RelativePosition = 0;
            volumeController.CrossRelativePosition = 0;
        }

        [UsedImplicitly]
        private void Update()
        {
            if (volumeController && !mGameOver)
            {
                if (volumeController.PlayState != CurvyController.CurvyControllerState.Playing) volumeController.Play();
                Vector2 input = new Vector2(
                    Input.GetAxis("Horizontal"),
                    Input.GetAxis("Vertical")
                ).normalized;

                float speedRaw = volumeController.Speed
                + (input.y
                * Time.deltaTime
                * Mathf.Lerp(
                    accelerationBackward,
                    accelerationForward,
                    (input.y + 1f) / 2f
                ));

                volumeController.Speed = Mathf.Clamp(
                    speedRaw,
                    0f,
                    maxSpeed
                );
                volumeController.CrossRelativePosition += AngularVelocity
                    * Mathf.Clamp(
                        volumeController.Speed / 10f,
                        0.2f,
                        1f
                    )
                    * input.x
                    * Time.deltaTime;


                if (rotatedTransform)
                {
                    float yTarget = Mathf.Lerp(
                        -90f,
                        90f,
                        (input.x + 1f) / 2f
                    );
                    rotatedTransform.localRotation = Quaternion.Euler(
                        0f,
                        yTarget,
                        0f
                    );
                }
            }
        }

        public void OnCollisionEnter(Collision collision) { }


        public void OnTriggerEnter(Collider other)
        {
            if (mGameOver == false)
            {
                explosionEmitter.Emit(200);
                volumeController.Pause();
                mGameOver = true;
                Invoke(
                    nameof(StartOver),
                    1
                );
            }
        }

        [UsedImplicitly]
        private void StartOver()
        {
            ResetController();
            mGameOver = false;
        }
    }
}