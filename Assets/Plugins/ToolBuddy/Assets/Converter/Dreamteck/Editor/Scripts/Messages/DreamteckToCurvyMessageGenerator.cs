// =====================================================================
// Copyright © 2023 ToolBuddy
// All rights reserved
// 
// http://www.toolbuddy.net
// =====================================================================


using System.Linq;
using ToolBuddy.Converter.Editor.Common.Messages;
using UnityEditor;
#if CURVY_SPLINES
using FluffyUnderware.Curvy;
using FluffyUnderware.DevTools.Extensions;
using ToolBuddy.Converter.Common.Requests;
#if DREAMTECK_SPLINES
using ToolBuddy.Converter.Dreamteck.Requests.Processors;
using Dreamteck.Splines;
#endif
#endif

namespace ToolBuddy.Converter.Editor.Unity.Messages
{
    [InitializeOnLoad]
    public static class DreamteckToCurvyMessageGenerator
    {
        static DreamteckToCurvyMessageGenerator()
        {
#if CURVY_SPLINES
#if DREAMTECK_SPLINES
            DreamteckRequestProcessor.Instance.AfterSplineConversion += OnSplineConversionRequestProcessed;
#endif
#endif
        }

#if CURVY_SPLINES
#if DREAMTECK_SPLINES
        private static void OnSplineConversionRequestProcessed(
            SplineConversionRequest<SplineComputer, CurvySpline>[] requests)
        {
            MessageRepository messages = MessageRepository.Instance;

            requests.Where(r => r.Source is { type: Spline.Type.CatmullRom })
                .Where(r => r.Source.knotParametrization > 0)
                .Select(r => r.Source)
                .ForEach(
                    s => messages.AddMessage(
                        "The converted Catmull-Rom spline uses a Non-Uniform Parametrization setting (value greater than 0). Curvy Splines only supports Uniform Catmull-Rom splines. As a result, the converted spline may differ in shape from the original.",
                        MessageType.Warning
                    )
                );
        }
#endif
#endif
    }
}