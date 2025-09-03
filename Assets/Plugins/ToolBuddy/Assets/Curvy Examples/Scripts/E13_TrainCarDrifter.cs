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
    [ExecuteAlways]
    public class E13_TrainCarDrifter : MonoBehaviour
    {
        public float speed = 30f;
        public float wheelSpacing = 9.72f;

        public Vector3 bodyOffset = new Vector3(
            0f,
            1f,
            0f
        );

        public SplineController controllerWheelLeading;
        public SplineController controllerWheelTrailing;
        public Transform trainCar;


        [UsedImplicitly]
        private void Start() =>
            controllerWheelLeading.Speed = speed;

        [UsedImplicitly]
        private void Update()
        {
            if (controllerWheelLeading
                && controllerWheelTrailing
                && controllerWheelLeading.Spline
                && controllerWheelTrailing.Spline
                && controllerWheelLeading.Spline != controllerWheelTrailing.Spline
                && trainCar)
            {
                Vector3 lookupPos =
                    controllerWheelTrailing.Spline.transform.InverseTransformPoint(controllerWheelLeading.transform.position);
                Vector3 trailingPosition;
                float nearestTF = controllerWheelTrailing.Spline.GetNearestPointTF(
                    lookupPos,
                    out trailingPosition
                );
                controllerWheelTrailing.RelativePosition = nearestTF;

                float trackDistance = Vector3.Distance(
                    controllerWheelLeading.transform.position,
                    trailingPosition
                );
                float wheelOffset = Mathf.Clamp(
                    Mathf.Sqrt((wheelSpacing * wheelSpacing) - (trackDistance * trackDistance)),
                    0f,
                    20f
                );

                controllerWheelTrailing.AbsolutePosition -= wheelOffset;
                trainCar.position =
                    ((controllerWheelLeading.transform.position + controllerWheelTrailing.transform.position) / 2f) + bodyOffset;

                Vector3 targetPostition = new Vector3(
                    controllerWheelLeading.transform.position.x,
                    trainCar.transform.position.y,
                    controllerWheelLeading.transform.position.z
                );
                trainCar.LookAt(
                    targetPostition,
                    controllerWheelLeading.transform.up
                );
            }
        }
    }
}