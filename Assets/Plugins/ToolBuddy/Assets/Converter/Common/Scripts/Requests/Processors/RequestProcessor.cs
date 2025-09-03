// =====================================================================
// Copyright © 2023 ToolBuddy
// All rights reserved
// 
// http://www.toolbuddy.net
// =====================================================================

#if CURVY_SPLINES

using System;
using System.Linq;
using FluffyUnderware.Curvy;
using JetBrains.Annotations;
using ToolBuddy.Converter.Common.Converters;
using ToolBuddy.Converter.Common.SplineAbstraction;
using UnityEngine;
using UnityEngine.Assertions;

namespace ToolBuddy.Converter.Common.Requests.Processors
{
    public abstract class RequestProcessor<TProcessor, TSource, TDestination>
        where TSource : class
        where TDestination : class
        where TProcessor : class, new()
    {
        private static TProcessor _instance;
        private readonly BaseConverter<TSource, TDestination> converter;

        public static TProcessor Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new TProcessor();

                return _instance;
            }
        }

        protected RequestProcessor()
        {
            converter = GetConverter();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.update += OnEditorUpdate;
#endif
        }

        protected abstract BaseConverter<TSource, TDestination> GetConverter();


        public event Action<SplineConversionRequest<TSource, TDestination>[]> BeforeSplineConversion = delegate { };
        public event Action<SplineConversionRequest<TSource, TDestination>[]> AfterSplineConversion = delegate { };

        private void OnEditorUpdate()
        {
            ConversionRequestQueue<TSource, TDestination> conversionRequests =
                ConversionRequestQueue<TSource, TDestination>.Instance;
            SplineConversionRequest<TSource, TDestination>[] splineRequests = conversionRequests.GetSplineRequests();
            ConnectionConversionRequest[] connectionRequests = conversionRequests.GetConnectionRequests();
            conversionRequests.ClearRequests();

            if (splineRequests.Length == 0)
                //check that the scenario where there are connection requests but no spline requests is really not happening
                Assert.IsTrue(connectionRequests.Length == 0);
            else
            {
                BeforeSplineConversion(splineRequests);
                ProcessSplineRequests(splineRequests);
                ProcessConnectionRequests(connectionRequests);
                AfterSplineConversion(splineRequests);
            }
        }

        private void ProcessSplineRequests(
            [NotNull] SplineConversionRequest<TSource, TDestination>[] requests)
        {
            foreach (SplineConversionRequest<TSource, TDestination> request in requests)
                try
                {
                    if (IsRequestValid(request) == false)
                        Debug.LogWarning($"Spline conversion request ignored: {request}");
                    else
                        converter.Convert(
                            request.Source,
                            request.Destination
                        );
                }
                catch (Exception exception)
                {
                    Debug.LogException(exception);
                }
        }

        private static void ProcessConnectionRequests(
            [NotNull] ConnectionConversionRequest[] requests)
        {
            foreach (ConnectionConversionRequest request in requests)
                try
                {
                    ProcessConnectionRequest(request);
                }
                catch (Exception exception)
                {
                    Debug.LogException(exception);
                }
        }

        private static void ProcessConnectionRequest(
            ConnectionConversionRequest request)
        {
            if (IsRequestInValid(request))
            {
                Debug.LogWarning($"Connection conversion request ignored: {request}");
                return;
            }

            CurvySplineSegment[] controlPoints = new CurvySplineSegment[request.ControlPoints.Count];
            for (int i = 0; i < request.ControlPoints.Count; i++)
            {
                ControlPoint<CurvySpline> controlPoint = request.ControlPoints.ElementAt(i);
                if (controlPoint.Index >= controlPoint.Spline.ControlPointCount)
                    throw new ArgumentException(
                        $"Control point index {controlPoint.Index} is out of range for spline {controlPoint.Spline}",
                        nameof(request)
                    );

                controlPoints[i] = controlPoint.Spline.ControlPointsList[controlPoint.Index];
            }

            CurvyConnection.Create(controlPoints).SetSynchronizationOptions(
                true,
                true
            );
        }


        private bool IsRequestValid(
            SplineConversionRequest<TSource, TDestination> request) =>
            //todo handle nulls in request (unity null). Probably happens when request was queued before the component was destroyed (scene reload, or as bellow removed as part of removing old unity spline resource)
            // todo found a repro case, it is related to connections. I loaded scene 12, removed everything except for the two splines and the curvy global. Then converted all Curvy to Unity, then all Unity to Curvy, then all Curvy to Unity. While having the convert all still on, I moved the root curvy spline RailtrackPathB.
            // ReSharper disable ConditionIsAlwaysTrueOrFalse
            //todo inline the false ==, and do similar work to IsRequestInvalid in ConnectionConversionRequest
            false
            == (request.Source == null
                || request.Destination == null
                //handle Unity's null
                || (request.Source is Component sourceComponent && sourceComponent == null)
                || (request.Destination is Component destinationComponent && destinationComponent == null));
        // ReSharper restore ConditionIsAlwaysTrueOrFalse

        private static bool IsRequestInValid(
            ConnectionConversionRequest request) =>
            //todo handle nulls in request (unity null). Probably happens when request was queued before the component was destroyed (scene reload, or as in the method above, removed as part of removing old unity spline resource)
            request.ControlPoints.Any(cp => cp.Spline == null);
    }
}
#endif