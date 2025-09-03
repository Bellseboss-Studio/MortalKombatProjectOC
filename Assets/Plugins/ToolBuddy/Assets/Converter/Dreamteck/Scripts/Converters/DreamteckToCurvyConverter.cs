// =====================================================================
// Copyright © 2023 ToolBuddy
// All rights reserved
// 
// http://www.toolbuddy.net
// =====================================================================

#if CURVY_SPLINES
#if DREAMTECK_SPLINES

using System;
using System.Linq;
using Dreamteck.Splines;
using FluffyUnderware.Curvy;
using ToolBuddy.Converter.Common.Converters;
using UnityEngine.Assertions;

namespace ToolBuddy.Converter.Dreamteck.Converters
{
    public class DreamteckToCurvyConverter : BaseConverter<SplineComputer, CurvySpline>
    {
        public const string Id = "Dreamteck To Curvy";

        protected override string GetConverterId => Id;

        public override void Convert(
            SplineComputer source,
            CurvySpline destination)
        {
            destination.Interpolation = ToCurvyType(source.type);
            destination.transform.position = source.transform.position;
            destination.transform.rotation = source.transform.rotation;
            destination.transform.localScale = source.transform.localScale;
            destination.Closed = source.isClosed;

            SetControlPoints(
                source.GetPoints(),
                destination
            );

            if (destination.Interpolation == CurvyInterpolation.BSpline)
                SetBSplineSpecificSettings(destination);

            destination.Refresh();

            //assert no connections to the spline
            Assert.IsTrue(destination.ControlPointsList.All(cp => cp.Connection == null));
        }

        private static void SetBSplineSpecificSettings(
            CurvySpline destination)
        {
            if (destination.ControlPointCount <= 2)
                return;

            int duplicationCount = destination.ControlPointCount == 3
                ? 2
                : 1;

            for (int i = 0; i < duplicationCount; i++)
            {
                destination.InsertBefore(
                    destination.ControlPointsList[0],
                    destination.ControlPointsList[0].transform.position,
                    true
                );
                destination.InsertAfter(
                    destination.ControlPointsList[destination.ControlPointsList.Count - 1],
                    destination.ControlPointsList[destination.ControlPointsList.Count - 1].transform.position,
                    true
                );
            }

            destination.BSplineDegree = 3;
            destination.IsBSplineClamped = false;
        }

        private CurvyInterpolation ToCurvyType(
            Spline.Type dreamteckType) =>
            dreamteckType switch
            {
                Spline.Type.Bezier => CurvyInterpolation.Bezier,
                Spline.Type.Linear => CurvyInterpolation.Linear,
                Spline.Type.BSpline => CurvyInterpolation.BSpline,
                Spline.Type.CatmullRom => CurvyInterpolation.CatmullRom,
                _ => throw new ArgumentOutOfRangeException(
                    nameof(dreamteckType),
                    dreamteckType,
                    null
                )
            };

        private static void SetControlPoints(
            SplinePoint[] source,
            CurvySpline destination)
        {
            destination.SetControlPointCount(source.Length);

            for (int i = 0; i < source.Length; i++)
                SetControlPointFromSplinePoint(
                    source[i],
                    destination.ControlPointsList[i]
                );
        }

        private static void SetControlPointFromSplinePoint(
            SplinePoint source,
            CurvySplineSegment destination)
        {
            destination.SetPosition(source.position);
            destination.HandleInPosition = source.tangent;
            destination.HandleOutPosition = source.tangent2;
            destination.AutoHandles = false;
        }
    }
}
#endif
#endif