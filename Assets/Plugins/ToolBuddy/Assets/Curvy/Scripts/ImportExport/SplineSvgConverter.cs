// =====================================================================
// Copyright © 2013 ToolBuddy
// All rights reserved
// 
// http://www.toolbuddy.net
// =====================================================================

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FluffyUnderware.DevTools;
using JetBrains.Annotations;
using ToolBuddy.ThirdParty.VectorGraphics;

namespace FluffyUnderware.Curvy.ImportExport
{
    /// <summary>
    /// Converts SVG strings to splines
    /// </summary>
    public static class SplineSvgConverter
    {
        /// <summary>
        /// Converts an SVG string to an array of splines
        /// </summary>
        /// <param name="svg">The SVG to deserialize</param>
        /// <param name="coordinatesSpace">How to interpret the coordinates in the SVG: local ones or global ones?</param>
        public static CurvySpline[] SvgToSplines(string svg,
            CurvySerializationSpace coordinatesSpace = CurvySerializationSpace.Global)
        {
            List<SerializedCurvySpline> serializedSplines = SvgToSerializedSplines(svg);

            CurvySpline[] result = new CurvySpline[serializedSplines.Count];

            for (int index = 0; index < serializedSplines.Count; index++)
            {
                SerializedCurvySpline spline = serializedSplines[index];
                CurvySpline deserializedSpline = result[index] = CurvySpline.Create();
                spline.WriteIntoSpline(
                    deserializedSpline,
                    coordinatesSpace
                );
            }

            return result;
        }

        /// <summary>
        /// Converts an SVG string to a spline
        /// </summary>
        /// <param name="svg">The SVG to deserialize</param>
        /// <param name="coordinatesSpace">How to interpret the coordinates in the SVG: local ones or global ones?</param>
        public static CurvySpline SvgToSpline(string svg,
            CurvySerializationSpace coordinatesSpace = CurvySerializationSpace.Global)
            => SvgToSplines(
                svg,
                coordinatesSpace
            ).Single();

        /// <summary>
        /// Converts an SVG string to an array of instances of <see cref="SerializedCurvySpline"/>
        /// </summary>
        /// <param name="svg">The SVG to deserialize</param>
        /// <param name="invertY">Inverts the Y coordinates to match unity's Y axis</param>
        public static List<SerializedCurvySpline> SvgToSerializedSplines([NotNull] string svg, bool invertY = true)
        {
            if (svg == null)
                throw new ArgumentNullException(nameof(svg));
            if (string.IsNullOrWhiteSpace(svg))
                throw new ArgumentException(
                    "Value cannot be null or whitespace.",
                    nameof(svg)
                );
            if (string.IsNullOrEmpty(svg))
                throw new ArgumentException(
                    "Value cannot be null or empty.",
                    nameof(svg)
                );

            List<SerializedCurvySpline> serializedSplines = new List<SerializedCurvySpline>();

            using (StringReader stringReader = new StringReader(svg))
            {
                SVGParser.SceneInfo sceneInfo = SVGParser.ImportSVG(stringReader);
                DrawNode(
                    sceneInfo.Scene.Root,
                    sceneInfo.Scene.Root.Transform,
                    serializedSplines
                );
            }

            if (invertY)
                foreach (SerializedCurvySpline spline in serializedSplines)
                {
                    foreach (SerializedCurvySplineSegment controlPoint in spline.ControlPoints)
                    {
                        controlPoint.Position.y *= -1;
                        controlPoint.HandleIn.y *= -1;
                        controlPoint.HandleOut.y *= -1;
                    }
                }

            return serializedSplines;
        }

        private static void DrawNode(SceneNode node, Matrix2D rootTransform, List<SerializedCurvySpline> splines)
        {
            if (node.Clipper != null)
                DTLog.LogWarning("[Curvy] SVG Import: A clipper was encountered. Clippers are not supported.");

            if (node.Shapes != null)
            {
                Matrix2D transform = rootTransform * node.Transform;

                foreach (Shape shape in node.Shapes)
                {
                    foreach (BezierContour bezierContour in shape.Contours)
                    {
                        BezierPathSegment[] segments = bezierContour.Segments;
                        List<SerializedCurvySplineSegment>
                            controlPoints = new List<SerializedCurvySplineSegment>(segments.Length);

                        if (segments.Length == 0)
                            continue;

                        if (segments.Length == 1)
                        {
                            DTLog.LogError(
                                "[Curvy] SVG Import: A segments array had only one element. This is unexpected. That contour was ignored. Please raise a bug report."
                            );
                            continue;
                        }

                        SerializedCurvySpline spline = new SerializedCurvySpline();
                        spline.Interpolation = CurvyInterpolation.Bezier;
                        spline.Closed = bezierContour.Closed;
                        spline.Name = $"SVG Spline {splines.Count}";
                        splines.Add(spline);

                        BezierPathSegment firstSegment = segments.First();
                        BezierPathSegment lastSegment = segments.Last();

                        SerializedCurvySplineSegment firstCurvySegment = new SerializedCurvySplineSegment();
                        firstCurvySegment.Position = transform.MultiplyPoint(firstSegment.P0);
                        firstCurvySegment.AutoHandles = false;
                        firstCurvySegment.HandleIn = transform.MultiplyVector(lastSegment.P2 - firstSegment.P0);
                        firstCurvySegment.HandleOut = transform.MultiplyVector(firstSegment.P1 - firstSegment.P0);
                        controlPoints.Add(firstCurvySegment);


                        for (int index = 1; index < segments.Length; index++)
                        {
                            BezierPathSegment previousSegment = segments[index - 1];
                            BezierPathSegment segment = segments[index];

                            SerializedCurvySplineSegment curvySegment = new SerializedCurvySplineSegment();
                            curvySegment.Position = transform.MultiplyPoint(segment.P0);
                            curvySegment.AutoHandles = false;
                            curvySegment.HandleIn = transform.MultiplyVector(previousSegment.P2 - segment.P0);
                            curvySegment.HandleOut = transform.MultiplyVector(segment.P1 - segment.P0);
                            controlPoints.Add(curvySegment);
                        }

                        spline.ControlPoints = controlPoints.ToArray();
                    }
                }
            }

            if (node.Children != null)
                foreach (SceneNode childNode in node.Children)
                    DrawNode(
                        childNode,
                        rootTransform * childNode.Transform,
                        splines
                    );
        }
    }
}