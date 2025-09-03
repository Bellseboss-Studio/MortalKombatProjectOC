// =====================================================================
// Copyright © 2013 ToolBuddy
// All rights reserved
// 
// http://www.toolbuddy.net
// =====================================================================

using System;

namespace FluffyUnderware.Curvy.Controllers
{
    /// <summary>
    /// Defines what motions are to be frozen
    /// </summary>
    [Flags]
    public enum MotionConstraints
    {
        /// <summary>
        /// No constraints.
        /// </summary>
        None = 0,

        /// <summary>
        /// Freeze motion along the X-axis.
        /// </summary>
        FreezePositionX = 1 << 0,

        /// <summary>
        /// Freeze motion along the Y-axis.
        /// </summary>
        FreezePositionY = 1 << 1,

        /// <summary>
        /// Freeze motion along the Z-axis.
        /// </summary>
        FreezePositionZ = 1 << 2,

        /// <summary>
        /// Freeze rotation along the X-axis.
        /// </summary>
        FreezeRotationX = 1 << 3,

        /// <summary>
        /// Freeze rotation along the Y-axis.
        /// </summary>
        FreezeRotationY = 1 << 4,

        /// <summary>
        /// Freeze rotation along the Z-axis.
        /// </summary>
        FreezeRotationZ = 1 << 5,

        #region Hidden because of inspector not handling it properly

        /*
        /// <summary>
        /// Freeze motion along all axes.
        /// </summary>
        FreezePosition = FreezePositionX | FreezePositionY | FreezePositionZ,
        /// <summary>
        /// Freeze rotation along all axes.
        /// </summary>
        FreezeRotation = FreezeRotationX | FreezeRotationY | FreezeRotationZ,
        /// <summary>
        /// Freeze motion along all axes.
        /// </summary>
        FreezeAll = FreezePosition | FreezeRotation
        */

        #endregion
    }
}