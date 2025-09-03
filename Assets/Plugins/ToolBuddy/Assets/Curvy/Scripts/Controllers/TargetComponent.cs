// =====================================================================
// Copyright © 2013 ToolBuddy
// All rights reserved
// 
// http://www.toolbuddy.net
// =====================================================================

namespace FluffyUnderware.Curvy.Controllers
{
    /// <summary>
    /// Defines what is the component controlled by the controller
    /// </summary>
    public enum TargetComponent
    {
        /// <summary>
        /// A transform
        /// </summary>
        Transform,

        /// <summary>
        /// A Rigidbody that is set to be kinematic
        /// </summary>
        KinematicRigidbody,

        /// <summary>
        /// A Rigidbody2D that is set to be kinematic
        /// </summary>
        KinematicRigidbody2D
    }
}