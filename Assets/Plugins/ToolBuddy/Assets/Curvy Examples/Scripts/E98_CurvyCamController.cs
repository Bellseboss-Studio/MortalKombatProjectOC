// =====================================================================
// Copyright © 2013 ToolBuddy
// All rights reserved
// 
// http://www.toolbuddy.net
// =====================================================================

using FluffyUnderware.Curvy.Controllers;
using FluffyUnderware.DevTools;
using UnityEngine;

namespace FluffyUnderware.Curvy.Examples
{
    public class E98_CurvyCamController : SplineController
    {
        [Section("Curvy Cam")]
        public float MinSpeed;

        public float MaxSpeed;
        public float Mass;
        public float Down;
        public float Up;
        public float Fric = 0.9f;


        protected override void OnEnable()
        {
            base.OnEnable();
            Speed = MinSpeed;
        }

        protected override void Advance(float speed, float deltaTime)
        {
            base.Advance(
                speed,
                deltaTime
            );
            // Get directional vector    
            Vector3 tan = GetTangent(RelativePosition);
            float acc;
            // accelerate when going down, deccelerate when going up
            if (tan.y < 0)
                acc = Down * tan.y * Fric;
            else
                acc = Up * -tan.y * Fric;

            // alter speed
            Speed = Mathf.Clamp(
                Speed + (Mass * acc * deltaTime),
                MinSpeed,
                MaxSpeed
            );
            // stop at spline's end
            if (RelativePosition == 1)
                Speed = 0;
        }
    }
}