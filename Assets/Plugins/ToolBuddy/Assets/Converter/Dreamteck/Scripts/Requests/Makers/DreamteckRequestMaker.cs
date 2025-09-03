// =====================================================================
// Copyright © 2023 ToolBuddy
// All rights reserved
// 
// http://www.toolbuddy.net
// =====================================================================

#if CURVY_SPLINES
#if DREAMTECK_SPLINES

using Dreamteck.Splines;
using FluffyUnderware.Curvy;
using ToolBuddy.Converter.Common.Requests;

namespace ToolBuddy.Converter.Dreamteck.Requests.Makers
{
    public static class DreamteckRequestMaker
    {
        public static SplineConversionRequest<SplineComputer, CurvySpline> MakeSplineConversionRequests(
            SplineComputer source,
            CurvySpline destination) =>
            new SplineConversionRequest<SplineComputer, CurvySpline>(
                source,
                destination
            );
    }
}
#endif
#endif