// =====================================================================
// Copyright © 2013 ToolBuddy
// All rights reserved
// 
// http://www.toolbuddy.net
// =====================================================================

using FluffyUnderware.Curvy.Generator;
using JetBrains.Annotations;
using UnityEngine;

namespace FluffyUnderware.Curvy.Examples
{
    public class E24_TerrainUpdater : MonoBehaviour
    {
        /// <summary>
        /// The generator that should be updated after the terrain is moved
        /// </summary>
        public CurvyGenerator CurvyGenerator;

        [UsedImplicitly]
        private void Update()
        {
            //Move the terrain
            Vector3 position = transform.position;
            position.x = 1 * Mathf.Sin(Time.time);
            position.z = 1 * Mathf.Cos(Time.time);
            transform.position = position;

            //Update the generator
            CurvyGenerator.Refresh(true);
        }
    }
}