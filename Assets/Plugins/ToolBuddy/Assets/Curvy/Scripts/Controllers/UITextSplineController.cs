// =====================================================================
// Copyright © 2013 ToolBuddy
// All rights reserved
// 
// http://www.toolbuddy.net
// =====================================================================

using System.Collections.Generic;
using FluffyUnderware.Curvy.Pools;
using FluffyUnderware.DevTools;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UI;

namespace FluffyUnderware.Curvy.Controllers
{
    /// <summary>
    /// SplineController modifying uGUI text
    /// </summary>
    [RequireComponent(typeof(Text))]
    [AddComponentMenu("Curvy/Controllers/UI Text Spline Controller")]
    [HelpURL(AssetInformation.DocsRedirectionBaseUrl + "uitextsplinecontroller")]
    public partial class UITextSplineController : SplineController, IMeshModifier
    {
        #region ### Serialized Fields ###

        [Section("Orientation")]
        [Tooltip("If true, the text characters will keep the same orientation regardless of the spline they follow")]
        [SerializeField]
        private bool staticOrientation;

        #endregion

        #region Public properties

        /// <summary>
        /// If true, the text characters will keep the same orientation regardless of the spline they follow
        /// </summary>
        public bool StaticOrientation
        {
            get => staticOrientation;
            set => staticOrientation = value;
        }

        #endregion


        #region Conditional display in the inspector of CurvyController properties

        protected override bool ShowOrientationSection => false;

        protected override bool ShowOffsetSection => false;

        #endregion

        private Graphic m_Graphic;
        private RectTransform rectTransform;
        private Text text;


        protected Text Text
        {
            get
            {
                if (text == null)
                    text = GetComponent<Text>();
                return text;
            }
        }

        protected RectTransform Rect
        {
            get
            {
                if (rectTransform == null)
                    rectTransform = GetComponent<RectTransform>();
                return rectTransform;
            }
        }

        protected Graphic graphic
        {
            get
            {
                if (m_Graphic == null)
                    m_Graphic = GetComponent<Graphic>();

                return m_Graphic;
            }
        }

        protected override void InitializedApplyDeltaTime(float deltaTime)
        {
            base.InitializedApplyDeltaTime(deltaTime);
            graphic.SetVerticesDirty();
        }

        public void ModifyMesh(Mesh verts)
        {
            if (IsActiveAndEnabled && isInitialized)
            {
                Vector3[] vtArray = verts.vertices;
                GlyphPlain glyph = new GlyphPlain();
                for (int c = 0; c < Text.text.Length; c++)
                {
                    glyph.Load(
                        ref vtArray,
                        c * 4
                    );
                    UpdateGlyph(glyph);
                    glyph.Save(
                        ref vtArray,
                        c * 4
                    );
                }

                verts.vertices = vtArray;
                ArrayPools.Vector3.Free(vtArray);
            }
        }

        public void ModifyMesh(VertexHelper vertexHelper)
        {
            if (IsActiveAndEnabled && isInitialized)
            {
                List<UIVertex> verts = new List<UIVertex>();
                GlyphQuad glyph = new GlyphQuad();

                vertexHelper.GetUIVertexStream(verts);
                vertexHelper.Clear();

                int readingIndex = 0;
                for (int letterIndex = 0; letterIndex < Text.text.Length; letterIndex++)
                {
                    if (Text.text[letterIndex] == ' ')
                        continue;

                    glyph.LoadTris(
                        verts,
                        readingIndex * 6
                    );
                    readingIndex++;
                    UpdateGlyph(glyph);
                    glyph.Save(vertexHelper);
                }
            }
        }

        [UsedImplicitly]
        private void UpdateGlyph(IGlyph glyph)
        {
            //OPTIM use InterpolateAndGetTangent
            float glyphTf = AbsoluteToRelative(
                GetClampedPosition(
                    AbsolutePosition + glyph.Center.x,
                    CurvyPositionMode.WorldUnits,
                    Clamping,
                    Length
                )
            );

            // shift to match baseline
            glyph.Transpose(
                new Vector3(
                    0,
                    glyph.Center.y,
                    0
                )
            );

            // Rotate
            if (StaticOrientation == false)
            {
                Vector3 glyphTangent = GetTangent(glyphTf);
                glyph.Rotate(
                    Quaternion.AngleAxis(
                        (Mathf.Atan2(
                             glyphTangent.x,
                             -glyphTangent.y
                         )
                         * Mathf.Rad2Deg)
                        - 90,
                        Vector3.forward
                    )
                );
            }

            // Center on controller's position
            glyph.Transpose(-glyph.Center);

            // Move on the corresponding position on the spline
            float controllerTf = AbsoluteToRelative(
                GetClampedPosition(
                    AbsolutePosition,
                    CurvyPositionMode.WorldUnits,
                    Clamping,
                    Length
                )
            );
            Vector3 controllerPosition = UseCache
                ? Spline.InterpolateFast(controllerTf)
                : Spline.Interpolate(controllerTf);
            Vector3 glyphPosition = UseCache
                ? Spline.InterpolateFast(glyphTf)
                : Spline.Interpolate(glyphTf);
            glyph.Transpose(Spline.transform.TransformDirection(glyphPosition - controllerPosition));
        }

        #region ### Unity Callbacks ###

#if DOCUMENTATION___FORCE_IGNORE___UNITY == false

        protected override void OnEnable()
        {
            base.OnEnable();

            BindSplineRelatedEvents();
            if (graphic != null)
                graphic.SetVerticesDirty();
        }

        protected override void OnDisable()
        {
            base.OnDisable();

            UnbindSplineRelatedEvents();
            if (graphic != null)
                graphic.SetVerticesDirty();
        }

        protected override void OnValidate()
        {
            base.OnValidate();

            if (IsActiveAndEnabled == false)
                return;

            BindSplineRelatedEvents();

            if (graphic != null)
                graphic.SetVerticesDirty();
        }

#endif

        #endregion

        #region Spline refreshing

        public override CurvySpline Spline
        {
            get => m_Spline;
            set
            {
                if (m_Spline != value)
                {
                    UnbindSplineRelatedEvents();

                    m_Spline = value;
                    if (IsActiveAndEnabled)
                        BindSplineRelatedEvents();
                }
            }
        }

        protected override void BindEvents()
        {
            base.BindEvents();
            BindSplineRelatedEvents();
        }

        protected override void UnbindEvents()
        {
            base.UnbindEvents();
            UnbindSplineRelatedEvents();
        }

        private void BindSplineRelatedEvents()
        {
            if (Spline)
            {
                UnbindSplineRelatedEvents();
                Spline.OnRefresh.AddListener(OnSplineRefreshed);
            }
        }

        private void UnbindSplineRelatedEvents()
        {
            if (Spline)
                Spline.OnRefresh.RemoveListener(OnSplineRefreshed);
        }

        private void OnSplineRefreshed(CurvySplineEventArgs e)
        {
            CurvySpline senderSpline = e.Sender as CurvySpline;
#if CURVY_SANITY_CHECKS
            Assert.IsTrue(senderSpline != null);
#endif
            if (senderSpline != Spline)
                senderSpline.OnRefresh.RemoveListener(OnSplineRefreshed);
            else
                graphic.SetVerticesDirty();
        }

        #endregion
    }
}