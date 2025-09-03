// =====================================================================
// Copyright © 2023 ToolBuddy
// All rights reserved
// 
// http://www.toolbuddy.net
// =====================================================================

#if DREAMTECK_SPLINES

using Dreamteck.Splines;
using ToolBuddy.Converter.Common.Listeners;

namespace ToolBuddy.Converter.Dreamteck.Listeners
{
    public class DreamteckSplineListener : BaseSplineListener<SplineComputer>
    {
        protected override void StartListeningToSpline(
            SplineComputer spline) =>
            spline.onRebuild += OnSplineChanged;

        protected override void StopListeningToSpline(
            SplineComputer spline) =>
            spline.onRebuild -= OnSplineChanged;

        protected override void StartListeningToSplineType() { }

        protected override void StopListeningToSplineType() { }

        private void OnSplineChanged()
        {
            //No way to know which spline triggered the event
            foreach (SplineComputer registeredSpline in RegisteredSplines)
                TriggerSplineChangedEvent(registeredSpline);
        }
    }
}
#endif