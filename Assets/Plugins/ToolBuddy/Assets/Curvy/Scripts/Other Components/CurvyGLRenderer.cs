// =====================================================================
// Copyright © 2013 ToolBuddy
// All rights reserved
// 
// http://www.toolbuddy.net
// =====================================================================

using System;
using System.Collections.Generic;
using FluffyUnderware.DevTools;
using JetBrains.Annotations;
using UnityEngine;

namespace FluffyUnderware.Curvy.Components
{
    /// <summary>
    /// Class to render a spline using GL.Draw
    /// Add this script to a camera
    /// </summary>
    /// <remarks>Useful for debugging</remarks>
    [HelpURL(AssetInformation.DocsRedirectionBaseUrl + "curvyglrenderer")]
    [AddComponentMenu("Curvy/Misc/Curvy GL Renderer")]
    public class CurvyGLRenderer : DTVersionedMonoBehaviour
    {
        [ArrayEx(
            ShowAdd = false,
            Draggable = false
        )]
        public List<GLSlotData> Splines = new List<GLSlotData>();

        //optim: make this material global to all CurvyGlRenderers, and not per instance
        private readonly Lazy<Material> lineMaterial
            = new Lazy<Material>(
                () =>
                {
                    Material material = new Material(Shader.Find("Hidden/Internal-Colored"));
                    material.hideFlags = HideFlags.HideAndDontSave;
                    material.shader.hideFlags = HideFlags.HideAndDontSave;
                    return material;
                }
            );


        #region ### Unity Callbacks ###

#if DOCUMENTATION___FORCE_IGNORE___UNITY == false

        protected override void OnValidate()
        {
            base.OnValidate();

            if (IsActiveAndEnabled) sanitize();
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            sanitize();
        }

        [UsedImplicitly]
        private void OnPostRender()
        {
            sanitize();
            for (int i = Splines.Count - 1; i >= 0; i--)
            {
                Splines[i].Spline.OnRefresh.AddListenerOnce(OnSplineRefresh);
                if (Splines[i].VertexData.Count == 0)
                    Splines[i].GetVertexData();

                Splines[i].Render(lineMaterial.Value);
            }
        }

#endif

        #endregion

        private void sanitize()
        {
            for (int i = Splines.Count - 1; i >= 0; i--)
                if (Splines[i] == null || Splines[i].Spline == null)
                    Splines.RemoveAt(i);
        }

        private void OnSplineRefresh(CurvySplineEventArgs e)
        {
            GLSlotData slot = getSlot((CurvySpline)e.Sender);
            if (slot == null)
                ((CurvySpline)e.Sender).OnRefresh.RemoveListener(OnSplineRefresh);
            else
                slot.VertexData.Clear();
        }

        private GLSlotData getSlot(CurvySpline spline)
        {
            if (spline)
                foreach (GLSlotData slot in Splines)
                    if (slot.Spline == spline)
                        return slot;

            return null;
        }

        public void Add(CurvySpline spline)
        {
            if (spline != null)
                Splines.Add(new GLSlotData { Spline = spline });
        }

        [UsedImplicitly]
        [Obsolete("No more used in Curvy. Will get removed. Copy it if you still need it")]
        public void Remove(CurvySpline spline)
        {
            for (int i = Splines.Count - 1; i >= 0; i--)
                if (Splines[i].Spline == spline)
                    Splines.RemoveAt(i);
        }
    }
}