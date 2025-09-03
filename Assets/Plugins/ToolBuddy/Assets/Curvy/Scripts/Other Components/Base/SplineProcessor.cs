// =====================================================================
// Copyright © 2013 ToolBuddy
// All rights reserved
// 
// http://www.toolbuddy.net
// =====================================================================

using System;
using FluffyUnderware.DevTools;
using JetBrains.Annotations;
using UnityEngine;

namespace FluffyUnderware.Curvy
{
    /// <summary>
    /// Base class for components that process a spline to produce something else, a line renderer or an edge collider for example
    /// </summary>
    [ExecuteAlways]
    public abstract class SplineProcessor : DTVersionedMonoBehaviour
    {
        /// <summary>
        /// The source spline
        /// </summary>
        public CurvySpline Spline
        {
            get => m_Spline;
            set
            {
                if (m_Spline != value)
                {
                    UnbindEvents();
                    m_Spline = value;
                    if (IsActiveAndEnabled)
                    {
                        BindEvents();
                        Refresh(); //properties should not execute heavy code, this is bad design
                    }
                }
            }
        }

        /// <summary>
        /// Method that processes the associated <see cref="CurvySpline"/>
        /// </summary>
        public abstract void Refresh();

        #region private

#if DOCUMENTATION___FORCE_IGNORE___CURVY == false

        [SerializeField]
        protected CurvySpline m_Spline;

#endif

        private void OnSplineRefresh(CurvySplineEventArgs e) =>
            ProcessEvent(e.Spline);

        private void OnSplineCoordinatesChanged(CurvySpline spline) =>
            ProcessEvent(spline);

        private void ProcessEvent([NotNull] CurvySpline spline)
        {
            if (Spline != spline)
                UnbindEvents(spline);
            else if (IsActiveAndEnabled)
                Refresh();
        }

        #endregion

        #region protected

        #region Unity callbacks

#if DOCUMENTATION___FORCE_IGNORE___UNITY == false

        [UsedImplicitly]
        protected virtual void Awake()
        {
            if (m_Spline == null)
            {
                m_Spline = GetComponent<CurvySpline>();
                if (ReferenceEquals(
                        m_Spline,
                        null
                    )
                    == false)
                    DTLog.Log(
                        String.Format(
                            "[Curvy] Spline '{0}' was assigned to the {1} by default.",
                            name,
                            GetType().Name
                        ),
                        this
                    );
            }
        }

        protected override void OnEnable()
        {
            base.OnEnable();

            UnbindEvents();
            BindEvents();
            Refresh();
        }

        protected override void OnDisable()
        {
            base.OnDisable();

            UnbindEvents();
        }

        protected override void OnValidate()
        {
            base.OnValidate();

            if (IsActiveAndEnabled)
            {
                BindEvents();
                Refresh();
            }
        }

        [UsedImplicitly]
        protected virtual void Start() =>
            Refresh();

#endif

        #endregion

        protected void BindEvents()
        {
            if (Spline)
            {
                Spline.OnRefresh.AddListenerOnce(OnSplineRefresh);

                //To avoid setting OnSplineCoordinatesChanged as a listener multiple times. This should not happen, because BindEvents should not be able to be called multiple successive times on the same spline, but I am handling it anyway for two reasons:
                // 1 - Unity proved me that their assumptions do not hold, like what they did with Enter Play Mode options. Who knows when OnEnabled will be called twice with no OnDisabled in between
                // 2 - In case I introduce a bug
                Spline.OnGlobalCoordinatesChanged -= OnSplineCoordinatesChanged;

                Spline.OnGlobalCoordinatesChanged += OnSplineCoordinatesChanged;
            }
        }

        protected void UnbindEvents()
        {
            if (Spline)
                UnbindEvents(Spline);
        }

        private void UnbindEvents([NotNull] CurvySpline spline)
        {
            spline.OnRefresh.RemoveListener(OnSplineRefresh);
            spline.OnGlobalCoordinatesChanged -= OnSplineCoordinatesChanged;
        }

        #endregion
    }
}