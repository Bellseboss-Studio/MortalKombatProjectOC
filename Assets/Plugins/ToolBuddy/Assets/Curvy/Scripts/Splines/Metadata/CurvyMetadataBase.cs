// =====================================================================
// Copyright © 2013 ToolBuddy
// All rights reserved
// 
// http://www.toolbuddy.net
// =====================================================================

using FluffyUnderware.DevTools;
using JetBrains.Annotations;
using UnityEngine;

namespace FluffyUnderware.Curvy
{
    /// <summary>
    /// Base class for all Metadata classes.
    /// A Metadata is a custom data attached to a Control Point
    /// </summary>
    [RequireComponent(typeof(CurvySplineSegment))]
    [ExecuteAlways]
    public abstract class CurvyMetadataBase : DTVersionedMonoBehaviour
    {
        #region ### Public Properties ###

        public CurvySplineSegment ControlPoint => mCP;

        public CurvySpline Spline =>
            //DESIGN should this throw an exception if mCP is null?
            mCP
                ? mCP.Spline
                : null;

        #endregion

        #region ### Private Fields & Properties ###

        private CurvySplineSegment mCP;

        #endregion

        #region ### Unity Callbacks ###

#if DOCUMENTATION___FORCE_IGNORE___UNITY == false

        protected virtual void Awake()
        {
            mCP = GetComponent<CurvySplineSegment>();
            mCP.RegisterMetaData(this);
        }

        [UsedImplicitly]
        private void OnDestroy() =>
            mCP.UnregisterMetaData(this);

#endif

        #endregion

        #region ### Public Methods ###

        public T GetPreviousData<T>(bool autoCreate = true, bool segmentsOnly = true, bool useFollowUp = false)
            where T : CurvyMetadataBase
        {
            if (ControlPoint)
            {
                CurvySplineSegment controlPoint = ControlPoint;
                CurvySpline spline = Spline;


                CurvySplineSegment previousControlPoint;
                if (!spline || spline.ControlPointsList.Count == 0)
                    previousControlPoint = null;
                else
                {
                    previousControlPoint = useFollowUp
                        ? spline.GetPreviousControlPointUsingFollowUp(controlPoint)
                        : spline.GetPreviousControlPoint(controlPoint);

                    if (segmentsOnly
                        && previousControlPoint
                        && previousControlPoint.Spline.IsControlPointASegment(previousControlPoint) == false)
                        previousControlPoint = null;
                }

                if (previousControlPoint)
                    return previousControlPoint.GetMetadata<T>(autoCreate);
            }

            return default;
        }

        public T GetNextData<T>(bool autoCreate = true, bool segmentsOnly = true, bool useFollowUp = false)
            where T : CurvyMetadataBase
        {
            if (ControlPoint)
            {
                CurvySplineSegment controlPoint = ControlPoint;
                CurvySpline spline = Spline;

                CurvySplineSegment nextControlPoint;
                if (!spline || spline.ControlPointsList.Count == 0)
                    nextControlPoint = null;
                else
                {
                    nextControlPoint = useFollowUp
                        ? spline.GetNextControlPointUsingFollowUp(controlPoint)
                        : spline.GetNextControlPoint(controlPoint);

                    if (segmentsOnly
                        && nextControlPoint
                        && nextControlPoint.Spline.IsControlPointASegment(nextControlPoint) == false)
                        nextControlPoint = null;
                }

                if (nextControlPoint)
                    return nextControlPoint.GetMetadata<T>(autoCreate);
            }

            return default;
        }

        /// <summary>
        /// Call this to make the owner spline send an event to notify its listeners of the change in the spline data.
        /// </summary>
        protected void NotifyModification()
        {
            CurvySpline spline = Spline;
            if (spline && spline.IsInitialized)
                spline.NotifyMetaDataModification();
        }

        #endregion
    }
}