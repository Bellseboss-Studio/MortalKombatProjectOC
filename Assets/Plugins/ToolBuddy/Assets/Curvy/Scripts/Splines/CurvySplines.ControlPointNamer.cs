// =====================================================================
// Copyright © 2013 ToolBuddy
// All rights reserved
// 
// http://www.toolbuddy.net
// =====================================================================

using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using JetBrains.Annotations;

namespace FluffyUnderware.Curvy
{
    public partial class CurvySpline
    {
        private class ControlPointNamer
        {
            [NotNull]
            private readonly CurvySpline spline;

            private bool requestRename;

            public ControlPointNamer([NotNull] CurvySpline curvySpline) =>
                spline = curvySpline;

            [Conditional(CompilationSymbols.UnityEditor)]
            public void RequestRename() =>
                requestRename = true;

            [Conditional(CompilationSymbols.UnityEditor)]
            public void ProcessRequests()
            {
                if (!requestRename)
                    return;

                RenameControlPoints(spline.ControlPoints);

                requestRename = false;
            }

            [Conditional(CompilationSymbols.UnityEditor)]
            public void CancelRequests() =>
                requestRename = false;

            /// <summary>
            /// Rename all control points
            /// </summary>
            /// <param name="splineControlPoints"></param>
            /// <remarks>Is not thread safe</remarks>
            private static void RenameControlPoints([NotNull] List<CurvySplineSegment> splineControlPoints)
            {
                short controlPointsCount = (short)splineControlPoints.Count;
                for (short i = 0; i < controlPointsCount; i++)
                    splineControlPoints[i].name = GetControlPointName(i);
            }

            #region Cache handling

            /// <summary>
            /// A list of precomputed control point names
            /// </summary>
            [NotNull]
            [ItemNotNull]
            private static readonly string[] ControlPointNames = GetControlPointNames();


            /// <summary>
            /// Get the correct control point name that should be displayed in the hierarchy
            /// </summary>
            /// <param name="controlPointIndex"></param>
            [NotNull]
            private static string GetControlPointName(short controlPointIndex)
                => controlPointIndex < CachedControlPointsNameCount
                    ? ControlPointNames[controlPointIndex]
                    : MakeControlPointName(controlPointIndex);

            [NotNull]
            [ItemNotNull]
            private static string[] GetControlPointNames()
            {
                string[] names = new string[CachedControlPointsNameCount];
                for (short i = 0; i < CachedControlPointsNameCount; i++)
                    names[i] = MakeControlPointName(i);
                return names;
            }

            [NotNull]
            private static string MakeControlPointName(short controlPointIndex)
            {
                string cpIndex = controlPointIndex.ToString(
                    "D4",
                    CultureInfo.InvariantCulture
                );
                return $"CP{cpIndex}";
            }

            #endregion
        }
    }
}