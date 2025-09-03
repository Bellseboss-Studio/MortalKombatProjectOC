// =====================================================================
// Copyright © 2023 ToolBuddy
// All rights reserved
// 
// http://www.toolbuddy.net
// =====================================================================

#if CURVY_SPLINES

using System;
using System.Linq;
using JetBrains.Annotations;

namespace ToolBuddy.Converter.Common.Requests
{
    public class ConversionRequestQueue<TSource, TDestination> : Singleton<ConversionRequestQueue<TSource, TDestination>>
        where TSource : class where TDestination : class
    {
        [NotNull]
        private readonly RequestQueue<ConnectionConversionRequest> connectionRequests =
            new RequestQueue<ConnectionConversionRequest>();

        [NotNull]
        private readonly RequestQueue<SplineConversionRequest<TSource, TDestination>> splineRequests =
            new RequestQueue<SplineConversionRequest<TSource, TDestination>>();

        public void PushSplineRequest(
            SplineConversionRequest<TSource, TDestination> request) =>
            splineRequests.Push(request);

        public void PushConnectionRequest(
            ConnectionConversionRequest request)
        {
            if (request.ControlPoints.Any() == false)
                throw new ArgumentException("Connection request is empty");

            connectionRequests.Push(request);
        }

        [NotNull]
        public SplineConversionRequest<TSource, TDestination>[] GetSplineRequests() =>
            splineRequests.Get();

        [NotNull]
        public ConnectionConversionRequest[] GetConnectionRequests() =>
            connectionRequests.Get();

        public void ClearRequests()
        {
            splineRequests.Clear();
            connectionRequests.Clear();
        }
    }
}
#endif