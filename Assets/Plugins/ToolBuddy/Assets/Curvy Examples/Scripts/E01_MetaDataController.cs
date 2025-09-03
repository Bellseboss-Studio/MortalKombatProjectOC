// =====================================================================
// Copyright © 2013 ToolBuddy
// All rights reserved
// 
// http://www.toolbuddy.net
// =====================================================================

using System;
using FluffyUnderware.Curvy.Controllers;
using FluffyUnderware.DevTools;
using UnityEngine;

namespace FluffyUnderware.Curvy.Examples
{
    /// <summary>
    /// Example custom Controller
    /// </summary>
    public class E01_MetaDataController : SplineController
    {
        //The section attribute renders our field inside it's own category!
        [Section(
            "MetaController",
            Sort = 0
        )]
        [RangeEx(
            0,
            30
        )]
        [SerializeField]
        private float m_MaxHeight = 5f; // The height over ground to use as default


        public float MaxHeight
        {
            get => m_MaxHeight;
            set
            {
                if (m_MaxHeight != value)
                    m_MaxHeight = value;
            }
        }

        /// <summary>
        /// This is called just after the SplineController has been initialized
        /// </summary>
        protected override void UserAfterInit() =>
            setHeight();

        /// <summary>
        /// This is called just after the SplineController updates
        /// </summary>
        protected override void UserAfterUpdate() =>
            setHeight();


        private void setHeight()
        {
            if (Spline.Dirty)
                Spline.Refresh();

            // Get the interpolated Metadata value for the current position (for SplineController, RelativePosition means TF)
            float height = Spline.GetInterpolatedMetadata<E01_HeightMetadata, float>(RelativePosition);

            // In our case we store a percentage (0..1) in our custom MetaData class, so we multiply with MaxHeight to set the actual height.
            // Note that position and rotation  has been set by the SplineController previously, so we just translate here using the local y-axis

            if (TargetComponent != TargetComponent.Transform)
                throw new NotSupportedException(
                    $"Only controllers with {nameof(TargetComponent)} set to {TargetComponent.Transform} are supported"
                );
            transform.Translate(
                0,
                height * MaxHeight,
                0,
                Space.Self
            );
        }
    }
}