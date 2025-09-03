// =====================================================================
// Copyright © 2023 ToolBuddy
// All rights reserved
// 
// http://www.toolbuddy.net
// =====================================================================

using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using UnityEngine;

namespace ToolBuddy.Converter.Common.Listeners
{
    /// <summary>
    /// Represents a listener for spline objects of type T.
    /// </summary>
    /// <typeparam name="T">The type of spline object needs to be a Unity component.</typeparam>
    public abstract class BaseSplineListener<T> where T : Component
    {
        [NotNull]
        [ItemNotNull]
        private readonly HashSet<T> registeredSplines = new HashSet<T>();

        /// <summary>
        /// Gets the collection of registered splines.
        /// </summary>
        public IReadOnlyCollection<T> RegisteredSplines => registeredSplines;

        protected BaseSplineListener()
        {
            StartListeningToSplineType();
        }

        ~BaseSplineListener()
        {
            StopListeningToSplineType();
        }

        /// <summary>
        /// Event that is triggered when a spline is changed.
        /// </summary>
        public event Action<T> SplineChanged = delegate { };

        /// <summary>
        /// Registers a spline object.
        /// </summary>
        /// <param name="splineContainer">The spline to register.</param>
        public void Register(
            T spline)
        {
            if (spline == null)
                throw new ArgumentNullException(nameof(spline));

            registeredSplines.Add(spline);

            StartListeningToSpline(spline);
        }

        /// <summary>
        /// Unregisters all splines.
        /// </summary>
        public void UnregisterAll()
        {
            foreach (T spline in RegisteredSplines)
                StopListeningToSpline(spline);

            registeredSplines.Clear();
        }

        /// <summary>
        /// Unregisters any deleted spline objects.
        /// </summary>
        public void UnregisterDeletedSplines()
        {
            IEnumerable<T> invalidSplines = Enumerable.Where(
                RegisteredSplines,
                // ReSharper disable once ConditionIsAlwaysTrueOrFalse
                c => c == null
                     && ReferenceEquals(
                         c,
                         null
                     )
                     == false
            );

            foreach (T spline in invalidSplines)
                StopListeningToSpline(spline);

            registeredSplines.RemoveWhere(c => c == null);
        }

        protected abstract void StartListeningToSpline(
            [NotNull] T spline);

        protected abstract void StopListeningToSpline(
            [NotNull] T spline);

        protected abstract void StartListeningToSplineType();

        protected abstract void StopListeningToSplineType();

        protected bool IsRegistered(
            [NotNull] T spline)
        {
            if (spline == null)
                throw new ArgumentNullException(nameof(spline));

            return registeredSplines.Contains(spline);
        }

        protected void TriggerSplineChangedEvent(
            [NotNull] T spline)
        {
            if (spline == null)
                throw new ArgumentNullException(nameof(spline));

            SplineChanged(spline);
        }
    }
}