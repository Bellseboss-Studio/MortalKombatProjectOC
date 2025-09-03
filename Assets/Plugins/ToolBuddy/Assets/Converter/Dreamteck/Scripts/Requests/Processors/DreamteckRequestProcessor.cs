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
using ToolBuddy.Converter.Common.Converters;
using ToolBuddy.Converter.Common.Requests.Processors;
using ToolBuddy.Converter.Dreamteck.Converters;

namespace ToolBuddy.Converter.Dreamteck.Requests.Processors
{
#if UNITY_EDITOR
    [UnityEditor.InitializeOnLoad]
#endif
    public class DreamteckRequestProcessor : RequestProcessor<DreamteckRequestProcessor, SplineComputer, CurvySpline>
    {
        static DreamteckRequestProcessor()
        {
            // Ensure the instance is created
            DreamteckRequestProcessor instance = Instance;
        }

        protected override BaseConverter<SplineComputer, CurvySpline> GetConverter() =>
            new DreamteckToCurvyConverter();
    }
}
#endif
#endif