// =====================================================================
// Copyright © 2013 ToolBuddy
// All rights reserved
// 
// http://www.toolbuddy.net
// =====================================================================

using UnityEngine;

namespace FluffyUnderware.Curvy.Examples
{
    public class E25_CameraLook : MonoBehaviour
    {
        [Range(
            0f,
            10f
        )]
        [SerializeField]
        private float m_TurnSpeed = 1.5f;

        protected void Update()
        {
            if (Time.timeScale < float.Epsilon)
                return;

            // Read the user input
            float x = Input.GetAxis("Mouse X");
            float y = -Input.GetAxis("Mouse Y");

            transform.Rotate(
                y * m_TurnSpeed,
                0,
                0,
                Space.Self
            );
            transform.Rotate(
                0,
                x * m_TurnSpeed,
                0,
                Space.World
            );
        }
    }
}