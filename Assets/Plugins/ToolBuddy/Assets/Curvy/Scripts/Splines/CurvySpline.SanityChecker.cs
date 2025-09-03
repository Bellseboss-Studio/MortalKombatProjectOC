// =====================================================================
// Copyright © 2013 ToolBuddy
// All rights reserved
// 
// http://www.toolbuddy.net
// =====================================================================

using System.Globalization;
using FluffyUnderware.DevTools;
using JetBrains.Annotations;

namespace FluffyUnderware.Curvy
{
    public partial class CurvySpline
    {
        private class SanityChecker
        {
            [NotNull]
            private readonly CurvySpline spline;

            private int sanityErrorLogsThisFrame;
            private int sanityWaringLogsThisFrame;

            public SanityChecker([NotNull] CurvySpline spline) =>
                this.spline = spline;

            [System.Diagnostics.Conditional(CompilationSymbols.CurvySanityChecks)]
            public void OnUpdate()
            {
                sanityWaringLogsThisFrame = 0;
                sanityErrorLogsThisFrame = 0;
            }


            [System.Diagnostics.Conditional(CompilationSymbols.CurvySanityChecks)]
            public void Check()
            {
                const int limit = 20;
                if (!spline.IsInitialized)
                {
                    if (sanityErrorLogsThisFrame < limit)
                    {
                        if (sanityErrorLogsThisFrame == limit - 1)
                            DTLog.LogError(
                                "[Curvy] Too many errors to display.",
                                spline
                            );
                        else
                            DTLog.LogError(
                                "[Curvy] Calling public method on non initialized spline.",
                                spline
                            );
                        sanityErrorLogsThisFrame++;
                    }
                }
                else if (spline.Dirty)
                    if (sanityWaringLogsThisFrame < limit)
                    {
                        if (sanityWaringLogsThisFrame == limit - 1)
                            DTLog.LogWarning(
                                "[Curvy] Too many warnings to display.",
                                spline
                            );
                        else
                            DTLog.LogWarning(
                                string.Format(
                                    CultureInfo.InvariantCulture,
                                    "[Curvy] Calling public method on a dirty spline. The returned result will not be up to date. Either refresh the spline manually by calling Refresh(), or wait for it to be refreshed automatically at the next {0} call",
                                    spline.UpdateIn.ToString()
                                ),
                                spline
                            );
                        sanityWaringLogsThisFrame++;
                    }
            }
        }
    }
}