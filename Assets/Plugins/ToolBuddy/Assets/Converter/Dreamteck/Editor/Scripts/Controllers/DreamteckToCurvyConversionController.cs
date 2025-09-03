// =====================================================================
// Copyright © 2023 ToolBuddy
// All rights reserved
// 
// http://www.toolbuddy.net
// =====================================================================

#if CURVY_SPLINES
#if DREAMTECK_SPLINES

using System;
using System.Collections.Generic;
using Dreamteck.Splines;
using FluffyUnderware.Curvy;
using FluffyUnderware.DevTools.Extensions;
using JetBrains.Annotations;
using ToolBuddy.Converter.Common.Requests;
using ToolBuddy.Converter.Dreamteck.Converters;
using ToolBuddy.Converter.Dreamteck.Listeners;
using ToolBuddy.Converter.Dreamteck.Requests.Makers;
using ToolBuddy.Converter.Editor.Common.Controllers;
using UnityEditor;
using UnityEngine;

namespace ToolBuddy.Converter.Editor.Dreamteck.Controllers
{
    [InitializeOnLoad]
    public class DreamteckToCurvyConversionController : BaseConversionController<SplineComputer>
    {
        private static DreamteckToCurvyConversionController instance;

        static DreamteckToCurvyConversionController() =>
            instance = new DreamteckToCurvyConversionController();

        private DreamteckToCurvyConversionController() : base(
            new DreamteckSplineListener(),
            DreamteckToCurvyConverter.Id
        ) { }

        protected override void ConvertSpline(
            SplineComputer spline)
        {
            CurvySpline curvySpline = RequestResource(spline);

            SplineConversionRequest<SplineComputer, CurvySpline> splineRequest =
                DreamteckRequestMaker.MakeSplineConversionRequests(
                    spline,
                    curvySpline
                );

            ConversionRequestQueue<SplineComputer, CurvySpline>.Instance.PushSplineRequest(
                splineRequest
            );
        }

        [NotNull]
        private static CurvySpline RequestResource(
            [NotNull] SplineComputer spline)
        {
            TransformExt.DeleteChildrenOfType<CurvySpline>(
                spline.transform,
                true,
                true,
                1,
                out List<Transform> skippedChildren
            );

            CurvySpline curvySpline;
            string splineName = $"{spline.name} - Curvy Spline";
            if (skippedChildren.Count == 0)
            {
                curvySpline = AddChildComponent<CurvySpline>(
                    spline.gameObject,
                    splineName
                );
                curvySpline.Start();
            }
            else
            {
                curvySpline = skippedChildren[0].GetComponent<CurvySpline>();
                curvySpline.name = splineName;
                curvySpline.Clear();
            }

            return curvySpline;
        }
    }
}
#endif
#endif