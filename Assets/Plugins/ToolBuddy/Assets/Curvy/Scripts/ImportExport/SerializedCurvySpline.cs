// =====================================================================
// Copyright © 2013 ToolBuddy
// All rights reserved
// 
// http://www.toolbuddy.net
// =====================================================================

using System;
using JetBrains.Annotations;
using UnityEngine;

namespace FluffyUnderware.Curvy.ImportExport
{
    /// <summary>
    /// A wrapper to the CurvySpline class
    /// </summary>
    [Serializable]
    public class SerializedCurvySpline
    {
        public string Name;
        public Vector3 Position;
        public Vector3 Rotation;
        public CurvyInterpolation Interpolation;
        public bool RestrictTo2D;
        public bool Closed;
        public bool AutoEndTangents;
        public CurvyOrientation Orientation;
        public float AutoHandleDistance;
        public int CacheDensity;
        public float MaxPointsPerUnit;
        public bool UsePooling;
        public bool UseThreading;
        public bool CheckTransform;
        public CurvyUpdateMethod UpdateIn;
        public bool IsBSplineClamped;
        public int BSplineDegree;
        public SerializedCurvySplineSegment[] ControlPoints;

        public SerializedCurvySpline()
        {
            Interpolation = CurvyGlobalManager.DefaultInterpolation;
            AutoEndTangents = CurvySplineDefaultValues.AutoEndTangents;
            Orientation = CurvySplineDefaultValues.Orientation;
            AutoHandleDistance = CurvySplineDefaultValues.AutoHandleDistance;
            CacheDensity = CurvySplineDefaultValues.CacheDensity;
            MaxPointsPerUnit = CurvySplineDefaultValues.MaxPointsPerUnit;
            UsePooling = CurvySplineDefaultValues.UsePooling;
            CheckTransform = CurvySplineDefaultValues.CheckTransform;
            UpdateIn = CurvySplineDefaultValues.UpdateIn;
            BSplineDegree = CurvySplineDefaultValues.BSplineDegree;
            IsBSplineClamped = CurvySplineDefaultValues.IsBSplineClamped;
            ControlPoints = new SerializedCurvySplineSegment[0];
        }

        public SerializedCurvySpline([NotNull] CurvySpline spline, CurvySerializationSpace space)
        {
            Name = spline.name;
            Position = space == CurvySerializationSpace.Local
                ? spline.transform.localPosition
                : spline.transform.position;
            Rotation = space == CurvySerializationSpace.Local
                ? spline.transform.localRotation.eulerAngles
                : spline.transform.rotation.eulerAngles;
            Interpolation = spline.Interpolation;
            RestrictTo2D = spline.RestrictTo2D;
            Closed = spline.Closed;
            AutoEndTangents = spline.AutoEndTangents;
            Orientation = spline.Orientation;
            AutoHandleDistance = spline.AutoHandleDistance;
            CacheDensity = spline.CacheDensity;
            MaxPointsPerUnit = spline.MaxPointsPerUnit;
            UsePooling = spline.UsePooling;
            UseThreading = spline.UseThreading;
            CheckTransform = spline.CheckTransform;
            UpdateIn = spline.UpdateIn;
            BSplineDegree = spline.BSplineDegree;
            IsBSplineClamped = spline.IsBSplineClamped;
            ControlPoints = new SerializedCurvySplineSegment[spline.ControlPointCount];
            for (int i = 0; i < spline.ControlPointCount; i++)
                ControlPoints[i] = new SerializedCurvySplineSegment(
                    spline.ControlPointsList[i],
                    space
                );
        }

        /// <summary>
        /// Fills an existing spline with data from this instance
        /// </summary>
        /// <remarks>This method will dirty the spline</remarks>
        public void WriteIntoSpline([NotNull] CurvySpline deserializedSpline, CurvySerializationSpace space)
        {
            deserializedSpline.name = Name;
            if (space == CurvySerializationSpace.Local)
            {
                deserializedSpline.transform.localPosition = Position;
                deserializedSpline.transform.localRotation = Quaternion.Euler(Rotation);
            }
            else
            {
                deserializedSpline.transform.position = Position;
                deserializedSpline.transform.rotation = Quaternion.Euler(Rotation);
            }

            deserializedSpline.Interpolation = Interpolation;
            deserializedSpline.RestrictTo2D = RestrictTo2D;
            deserializedSpline.Closed = Closed;
            deserializedSpline.AutoEndTangents = AutoEndTangents;
            deserializedSpline.Orientation = Orientation;
            deserializedSpline.AutoHandleDistance = AutoHandleDistance;
            deserializedSpline.CacheDensity = CacheDensity;
            deserializedSpline.MaxPointsPerUnit = MaxPointsPerUnit;
            deserializedSpline.UsePooling = UsePooling;
            deserializedSpline.UseThreading = UseThreading;
            deserializedSpline.CheckTransform = CheckTransform;
            deserializedSpline.UpdateIn = UpdateIn;

            foreach (SerializedCurvySplineSegment serializedControlPoint in ControlPoints)
                serializedControlPoint.WriteIntoControlPoint(
                    deserializedSpline.InsertAfter(
                        null,
                        true
                    ),
                    space
                );

            //degree is assigned after the control points insertion, because the actual value of the degree depends on the number of control points
            deserializedSpline.BSplineDegree = BSplineDegree;
            deserializedSpline.IsBSplineClamped = IsBSplineClamped;

            deserializedSpline.SetDirtyAll();
        }
    }
}