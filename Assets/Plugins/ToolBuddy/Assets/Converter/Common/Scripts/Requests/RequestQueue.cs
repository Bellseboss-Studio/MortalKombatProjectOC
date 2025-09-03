// =====================================================================
// Copyright © 2023 ToolBuddy
// All rights reserved
// 
// http://www.toolbuddy.net
// =====================================================================

using System;
using System.Collections.Generic;
using JetBrains.Annotations;

namespace ToolBuddy.Converter.Common.Requests
{
    /// <summary>
    /// A request queue that holds unique requests of type T
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class RequestQueue<T> where T : IEquatable<T>
    {
        [NotNull]
        private readonly Queue<T> requests = new Queue<T>();

        /// <summary>
        /// Pushes a request to the queue if not present already
        /// </summary>
        /// <param name="request"></param>
        public void Push(
            T request)
        {
            if (requests.Contains(request))
                return;

            requests.Enqueue(request);
        }

        [NotNull]
        public T[] Get() =>
            requests.ToArray();

        public void Clear() =>
            requests.Clear();
    }
}