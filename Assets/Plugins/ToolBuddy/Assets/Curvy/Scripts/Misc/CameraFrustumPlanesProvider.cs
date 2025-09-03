// =====================================================================
// Copyright © 2013 ToolBuddy
// All rights reserved
// 
// http://www.toolbuddy.net
// =====================================================================

using System;
using UnityEngine;

namespace FluffyUnderware.Curvy
{
    /// <summary>
    /// A class to retrieve camera's frustum planes in an efficient way
    /// </summary>
    public class CameraFrustumPlanesProvider
    {
        #region Singleton and thread safety

        private static readonly Lazy<CameraFrustumPlanesProvider> instance = new Lazy<CameraFrustumPlanesProvider>(() => new CameraFrustumPlanesProvider());

        public static CameraFrustumPlanesProvider Instance
            => instance.Value;

        private static object lockObject = new object();

        #endregion

        private readonly Plane[] cachedPlanes = new Plane[6];

        private Vector3 cachedPosition = new Vector3(
            Single.NaN,
            Single.NaN,
            Single.NaN
        );

        private Vector3 cachedForward = new Vector3(
            Single.NaN,
            Single.NaN,
            Single.NaN
        );

        private float cachedFov = Single.NaN;
        private int cachedPixelWidth = -1;
        private int cachedPixelHeight = -1;

        /// <summary>
        /// Get the frustum planes of the given camera
        /// </summary>
        public Plane[] GetFrustumPlanes(Camera camera)
        {
            Transform cameraTransform = camera.transform;
            Vector3 cameraPosition = cameraTransform.position;
            Vector3 cameraZDirection = cameraTransform.forward;
            int cameraPixelWidth = camera.pixelWidth;
            int cameraPixelHeight = camera.pixelHeight;
            float cameraFieldOfView = camera.fieldOfView;

            //Update cachedPlanes if camera changed
            if (!IsCacheOutdated(
                    cameraPosition,
                    cameraZDirection,
                    cameraPixelWidth,
                    cameraPixelHeight,
                    cameraFieldOfView
                ))
                return cachedPlanes;

            lock (lockObject)
            {
                if (IsCacheOutdated(
                        cameraPosition,
                        cameraZDirection,
                        cameraPixelWidth,
                        cameraPixelHeight,
                        cameraFieldOfView
                    ))
                {
                    cachedPosition = cameraPosition;
                    cachedForward = cameraZDirection;
                    cachedPixelWidth = cameraPixelWidth;
                    cachedPixelHeight = cameraPixelHeight;
                    cachedFov = cameraFieldOfView;
                    GeometryUtility.CalculateFrustumPlanes(
                        camera,
                        cachedPlanes
                    );
                }
            }

            return cachedPlanes;
        }

        private bool IsCacheOutdated(Vector3 cameraPosition, Vector3 cameraZDirection, int cameraPixelWidth,
            int cameraPixelHeight, float cameraFieldOfView) =>
            cachedPosition != cameraPosition
            || cachedForward != cameraZDirection
            || cachedPixelWidth != cameraPixelWidth
            || cachedPixelHeight != cameraPixelHeight
            || Mathf.Approximately(
                cachedFov,
                cameraFieldOfView
            )
            == false;
    }
}