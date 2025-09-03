// =====================================================================
// Copyright © 2013 ToolBuddy
// All rights reserved
// 
// http://www.toolbuddy.net
// =====================================================================

using JetBrains.Annotations;
using UnityEngine;

namespace FluffyUnderware.Curvy.Examples
{
    /// <summary>
    /// Moves control points in a circular fashion
    /// </summary>
    public class E29_ControlPointMover : MonoBehaviour
    {
        private Vector3 originalPosition;
        public float Variation;
        public float Magnitude = 3;
        public float Period = 3;

        [UsedImplicitly]
        private void Start() =>
            originalPosition = transform.position;

        [UsedImplicitly]
        private void Update()
        {
            Vector3 position = originalPosition;
            position.x += Magnitude * Mathf.Sin(Variation + (Time.time * Period));
            position.z += Magnitude * Mathf.Cos(Variation + (Time.time * Period));
            transform.position = position;
        }
    }
}