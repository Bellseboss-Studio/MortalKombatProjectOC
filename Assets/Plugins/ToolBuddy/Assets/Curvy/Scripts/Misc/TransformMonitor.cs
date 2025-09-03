// =====================================================================
// Copyright © 2013 ToolBuddy
// All rights reserved
// 
// http://www.toolbuddy.net
// =====================================================================

using System;
using FluffyUnderware.DevTools.Extensions;
using JetBrains.Annotations;
using UnityEngine;

namespace FluffyUnderware.Curvy
{
    /// <summary>
    /// Monitors changes in global position, rotation, and scale of a Transform.
    /// </summary>
    /// <remarks>
    /// This class gets and potentially sets the Transform.hasChanged property when checking for changes.
    /// </remarks>
    public class TransformMonitor
    {
        [NotNull]
        private readonly Transform transform;

        /// <summary>
        /// The global position of the transform the last time it was checked. Checks are done in CheckForChanges
        /// </summary>
        private Vector3 lastCheckedPosition;

        /// <summary>
        /// The global rotation of the transform the last time it was checked. Checks are done in CheckForChanges
        /// </summary>
        private Quaternion lastCheckedRotation;

        /// <summary>
        /// The global scale of the transform the last time it was checked. Checks are done in CheckForChanges
        /// </summary>
        private Vector3 lastCheckedScale;

        /// <summary>
        /// True if the global position, rotation or scale of the spline was found to have changed at the last check
        /// </summary>
        public bool HasChanged { get; private set; }

        private readonly bool monitorPosition;
        private readonly bool monitorRotation;
        private readonly bool monitorScale;

        /// <summary>
        ///  Creates a new TransformMonitor
        ///  </summary>
        ///  <param name="transformToTrack">The transform to monitor</param>
        ///  <param name="monitorPosition">True if the global position of the transform should be monitored</param>
        ///  <param name="monitorRotation">True if the global rotation of the transform should be monitored</param>
        /// <param name="monitorScale">True if the global scale of the transform should be monitored</param>
        ///  <exception cref="ArgumentNullException">Thrown if transformToTrack is null</exception>
        ///  <exception cref="ArgumentException">Thrown if all tracking parameters are false</exception>
        ///  <remarks>
        ///  The TransformMonitor will check for changes in the global position, rotation and scale of the transformToTrack. If any of these changes, <see cref="HasChanged"/> will be set to true.
        ///  </remarks>
        ///  <example>
        ///  <code>
        ///  TransformMonitor monitor = new TransformMonitor(transform, true, true, true);
        ///  // other operations
        ///  if (monitor.CheckForChanges())
        ///  {
        ///    //do something
        ///  }
        ///  </code>
        ///  </example>
        ///  <seealso cref="CheckForChanges"/>
        ///  <seealso cref="HasChanged"/>
        public TransformMonitor([NotNull] Transform transformToTrack,
            bool monitorPosition,
            bool monitorRotation,
            bool monitorScale)
        {
            if (transformToTrack == null)
                throw new ArgumentNullException(nameof(transformToTrack));
            if (monitorPosition == false && monitorRotation == false && monitorScale == false)
                throw new ArgumentException("TransformMonitor has been initialized with no tracking enabled");

            transform = transformToTrack;
            this.monitorPosition = monitorPosition;
            this.monitorRotation = monitorRotation;
            this.monitorScale = monitorScale;
            ResetMonitoring();
        }

        /// <summary>
        /// Resets the monitoring. <see cref="HasChanged"/> is set to false.
        /// </summary>
        public void ResetMonitoring()
        {
            HasChanged = false;
            MarkCurrentTransformAsChecked();
        }

        /// <summary>
        /// Checks if the global position, rotation or scale of the spline has changed since the last check. If so, <see cref="HasChanged"/> is set to true.  
        /// </summary>
        /// <remarks>
        /// The method gets and potentially sets the Transform.hasChanged property
        /// </remarks>
        /// <returns>
        /// The same value as <see cref="HasChanged"/>
        /// </returns>
        public bool CheckForChanges()
        {
            bool changeDetected;
            if (transform.hasChanged)
            {
                transform.hasChanged = false;

                changeDetected = HaveGlobalCoordinatesChanged();
                if (changeDetected)
                    MarkCurrentTransformAsChecked();
            }
            else
                changeDetected = false;

            HasChanged = changeDetected;

            return HasChanged;
        }

        private bool HaveGlobalCoordinatesChanged()
            //transform.hasChanged is true even when changing parent with no change in both local and global coordinates. And even a change in local coordinates doesn't interest us, since bounds computation only need global coordinates
            => (monitorPosition && transform.position.NotApproximately(lastCheckedPosition))
               || (monitorRotation && transform.rotation.DifferentOrientation(lastCheckedRotation))
               || (monitorScale && transform.lossyScale != lastCheckedScale);

        private void MarkCurrentTransformAsChecked()
        {
            lastCheckedPosition = transform.position;
            lastCheckedRotation = transform.rotation;
            lastCheckedScale = transform.lossyScale;
        }
    }
}