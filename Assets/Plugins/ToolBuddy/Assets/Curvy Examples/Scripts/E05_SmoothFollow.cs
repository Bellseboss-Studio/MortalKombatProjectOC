// =====================================================================
// Copyright © 2013 ToolBuddy
// All rights reserved
// 
// http://www.toolbuddy.net
// =====================================================================

using JetBrains.Annotations;
using UnityEngine;

// This is the regular SmoothFollow script, but renamed and put into Curvy.Examples namespace to keep it separated from the often changing Standard Assets
namespace FluffyUnderware.Curvy.Examples
{
    public class E05_SmoothFollow : MonoBehaviour
    {
        // The target we are following
        [SerializeField]
#pragma warning disable 649
        private Transform target;
#pragma warning restore 649
        // The distance in the x-z plane to the target
        [SerializeField]
        private float distance = 10.0f;

        // the height we want the camera to be above the target
        [SerializeField]
        private float height = 5.0f;

        [SerializeField]
#pragma warning disable 649
        private float rotationDamping;

        [SerializeField]
        private float heightDamping;
#pragma warning restore 649


        // Update is called once per frame
        [UsedImplicitly]
        private void LateUpdate()
        {
            // Early out if we don't have a target
            if (!target)
                return;

            // Calculate the current rotation angles
            float wantedRotationAngle = target.eulerAngles.y;
            float wantedHeight = target.position.y + height;

            float currentRotationAngle = transform.eulerAngles.y;
            float currentHeight = transform.position.y;

            // Damp the rotation around the y-axis
            currentRotationAngle = Mathf.LerpAngle(
                currentRotationAngle,
                wantedRotationAngle,
                rotationDamping * Time.deltaTime
            );

            // Damp the height
            currentHeight = Mathf.Lerp(
                currentHeight,
                wantedHeight,
                heightDamping * Time.deltaTime
            );

            // Convert the angle into a rotation
            Quaternion currentRotation = Quaternion.Euler(
                0,
                currentRotationAngle,
                0
            );

            // Set the position of the camera on the x-z plane to:
            // distance meters behind the target
            transform.position = target.position;
            transform.position -= currentRotation * Vector3.forward * distance;

            // Set the height of the camera
            transform.position = new Vector3(
                transform.position.x,
                currentHeight,
                transform.position.z
            );

            // Always look at the target
            transform.LookAt(target);
        }
    }
}