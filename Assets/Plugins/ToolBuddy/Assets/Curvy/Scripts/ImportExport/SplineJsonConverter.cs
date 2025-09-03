// =====================================================================
// Copyright © 2013 ToolBuddy
// All rights reserved
// 
// http://www.toolbuddy.net
// =====================================================================

using System;
using System.Collections.Generic;
using System.Linq;
using FluffyUnderware.Curvy.Utils;
using JetBrains.Annotations;
using UnityEngine;

namespace FluffyUnderware.Curvy.ImportExport
{
    /// <summary>
    /// Converts splines to JSON strings, and vice versa
    /// </summary>
    public static class SplineJsonConverter
    {
        /// <summary>
        /// Converts splines to a JSON string
        /// </summary>
        /// <param name="splines">The splines to serialize</param>
        /// <param name="coordinatesSpace">What coordinates of the spline should be serialized: local ones or global ones?</param>
        /// <param name="prettify">Set to true to make the JSON string easy to read. If false, the spline will be compacted to make it small</param>
        public static string SplinesToJson(IEnumerable<CurvySpline> splines,
            CurvySerializationSpace coordinatesSpace = CurvySerializationSpace.Global, bool prettify = true)
        {
            SerializedCurvySpline[] serializedSplines = splines.Select(
                s => new SerializedCurvySpline(
                    s,
                    coordinatesSpace
                )
            ).ToArray();
            return JsonUtility.ToJson(
                new SerializableArray<SerializedCurvySpline> { Array = serializedSplines },
                prettify
            );
        }

        /// <summary>
        /// Converts a spline to a JSON string
        /// </summary>
        /// <param name="spline">The spline to serialize</param>
        /// <param name="coordinatesSpace">What coordinates of the spline should be serialized: local ones or global ones?</param>
        /// <param name="prettify">Set to true to make the JSON string easy to read. If false, the spline will be compacted to make it small</param>
        public static string SplineToJson(CurvySpline spline,
            CurvySerializationSpace coordinatesSpace = CurvySerializationSpace.Global, bool prettify = true) =>
            SplinesToJson(
                new[] { spline },
                coordinatesSpace,
                prettify
            );

        /// <summary>
        /// Converts a JSON string to an array of splines
        /// </summary>
        /// <param name="json">The JSON to deserialize</param>
        /// <param name="coordinatesSpace">How to interpret the coordinates in the JSON: local ones or global ones?</param>
        public static CurvySpline[] JsonToSplines(string json,
            CurvySerializationSpace coordinatesSpace = CurvySerializationSpace.Global)
        {
            SerializedCurvySpline[] serializedSplines = JsonToSerializedSplines(json);

            CurvySpline[] result = new CurvySpline[serializedSplines.Length];

            for (int index = 0; index < serializedSplines.Length; index++)
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
        /// Converts a JSON string to a spline
        /// </summary>
        /// <param name="json">The JSON to deserialize</param>
        /// <param name="coordinatesSpace">How to interpret the coordinates in the JSON: local ones or global ones?</param>
        public static CurvySpline JsonToSpline(string json,
            CurvySerializationSpace coordinatesSpace = CurvySerializationSpace.Global)
            => JsonToSplines(
                json,
                coordinatesSpace
            ).Single();

        /// <summary>
        /// Converts a JSON string to an array of instances of <see cref="SerializedCurvySpline"/>
        /// </summary>
        /// <param name="json">The JSON to deserialize</param>
        public static SerializedCurvySpline[] JsonToSerializedSplines([NotNull] string json)
        {
            if (json == null)
                throw new ArgumentNullException(nameof(json));
            if (string.IsNullOrWhiteSpace(json))
                throw new ArgumentException(
                    "Value cannot be null or whitespace.",
                    nameof(json)
                );
            if (string.IsNullOrEmpty(json))
                throw new ArgumentException(
                    "Value cannot be null or empty.",
                    nameof(json)
                );

            SerializedCurvySpline[] serializedSplines;
            //The following deserializes the JSON text, but instead of doing with a simple and nice one line of code, it is done in a complex way. The reason to that is that JsonUtility doesn't handle default values for JSON fields.
            {
                //First we deserialize the JSON in the sole goal to know how much elements there are in the arrays
                SerializableArray<SerializedCurvySpline> serializableArray =
                    JsonUtility.FromJson<SerializableArray<SerializedCurvySpline>>(json);

                //Knowing the number of array elements, we assign a new instance for each element. By creating the new instances ourselves, through the constructor, we have control on the default value of fields
                for (int index = 0; index < serializableArray.Array.Length; index++)
                {
                    int controlPointsCount = serializableArray.Array[index].ControlPoints.Length;

                    SerializedCurvySpline splineWithCorrectDefaultValue = new SerializedCurvySpline();
                    splineWithCorrectDefaultValue.ControlPoints = new SerializedCurvySplineSegment[controlPointsCount];
                    for (int controlPointIndex = 0; controlPointIndex < controlPointsCount; controlPointIndex++)
                        splineWithCorrectDefaultValue.ControlPoints[controlPointIndex] = new SerializedCurvySplineSegment();

                    serializableArray.Array[index] = splineWithCorrectDefaultValue;
                }

                //Then, through FromJsonOverwrite, we overwrite the fields that are existing in the JSON text
                JsonUtility.FromJsonOverwrite(
                    json,
                    serializableArray
                );

                serializedSplines = serializableArray.Array;
            }

            return serializedSplines;
        }
    }
}