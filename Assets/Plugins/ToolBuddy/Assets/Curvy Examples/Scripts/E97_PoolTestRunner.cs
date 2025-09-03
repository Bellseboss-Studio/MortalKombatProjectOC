// =====================================================================
// Copyright © 2013 ToolBuddy
// All rights reserved
// 
// http://www.toolbuddy.net
// =====================================================================

using FluffyUnderware.Curvy.Components;
using FluffyUnderware.DevTools.Extensions;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.UI;

namespace FluffyUnderware.Curvy.Examples
{
    public class E97_PoolTestRunner : MonoBehaviour
    {
        public CurvySpline Spline;
        public Text PoolCountInfo;

        [UsedImplicitly]
        private void Start() =>
            checkForSpline();

        [UsedImplicitly]
        private void Update()
        {
            CurvyGlobalManager curvyGlobalManager = CurvyGlobalManager.Instance;
            PoolCountInfo.text = curvyGlobalManager != null
                ? $"Control Points in Pool: {curvyGlobalManager.ControlPointPool.Count}"
                : "CurvyGlobalManager not found";
        }

        private void checkForSpline()
        {
            if (Spline == null)
            {
                Spline = CurvySpline.Create();
                Camera.main.GetComponent<CurvyGLRenderer>().Add(Spline);
                for (int i = 0; i < 4; i++)
                    AddCP();
            }
        }

        public void AddCP()
        {
            checkForSpline();
            Spline.Add(Random.insideUnitCircle * 50);
            Spline.Refresh();
        }

        public void DeleteCP()
        {
            if (Spline && Spline.ControlPointCount > 0)
            {
                int idx = Random.Range(
                    0,
                    Spline.ControlPointCount - 1
                );
                Spline.Delete(Spline.ControlPointsList[idx]);
            }
        }

        public void ClearSpline()
        {
            if (Spline)
                Spline.Clear();
        }

        public void DeleteSpline()
        {
            if (Spline)
                Spline.gameObject.Destroy(
                    false,
                    true
                );
        }
    }
}