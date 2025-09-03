// =====================================================================
// Copyright © 2013 ToolBuddy
// All rights reserved
// 
// http://www.toolbuddy.net
// =====================================================================

using System.Collections;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.UI;

namespace FluffyUnderware.Curvy.Examples
{
    [ExecuteAlways]
    public class E05_PanelUpdater : MonoBehaviour
    {
        public CurvySpline Spline;
        public Text StatisticsText;
        public Slider Density;

        [UsedImplicitly]
        private void Start() =>
            StartCoroutine(UpdateCoroutine());

        private IEnumerator UpdateCoroutine()
        {
            while (true)
            {
                TryUpdateDisplay();
                yield return new WaitForSeconds(0.25f);
            }
            // ReSharper disable once IteratorNeverReturns
        }

        private void TryUpdateDisplay()
        {
            if (!Spline || !Spline.IsInitialized || Spline.Dirty)
                return;

            StatisticsText.text =
                string.Format(
                    "Red Curve Cache Points: {0} \nFrame rate: {1:000}",
                    Spline.CacheSize,
                    1f / Time.smoothDeltaTime
                );
        }

        public void OnSliderChange()
        {
            Spline.CacheDensity = (int)Density.value;
        }
    }
}