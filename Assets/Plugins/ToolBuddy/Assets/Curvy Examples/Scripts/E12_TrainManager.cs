// =====================================================================
// Copyright © 2013 ToolBuddy
// All rights reserved
// 
// http://www.toolbuddy.net
// =====================================================================

using JetBrains.Annotations;
using UnityEngine;

namespace FluffyUnderware.Curvy.Examples
{
    [ExecuteAlways]
    public class E12_TrainManager : MonoBehaviour
    {
        public CurvySpline Spline;
        public float Speed;


        public float Position;
        public float CarSize = 10;
        public float AxisDistance = 8;
        public float CarGap = 1;
        public float Limit = 0.2f;

        private bool isSetup;
        private E12_TrainCarManager[] Cars;

        [UsedImplicitly]
        private void Start() =>
            setup();

        [UsedImplicitly]
        private void OnDisable() =>
            isSetup = false;

#if UNITY_EDITOR
        [UsedImplicitly]
        private void OnValidate()
        {
            if (isSetup)
                setup();
        }
#endif

        [UsedImplicitly]
        private void LateUpdate()
        {
            if (isSetup == false)
                setup();
            if (Cars.Length > 1)
            {
                E12_TrainCarManager first = Cars[0];
                E12_TrainCarManager last = Cars[Cars.Length - 1];
                if (first.FrontAxis.Spline == last.BackAxis.Spline
                    && first.FrontAxis.RelativePosition > last.BackAxis.RelativePosition)
                    for (int i = 1; i < Cars.Length; i++)
                    {
                        float delta = Cars[i - 1].Position - Cars[i].Position - CarSize - CarGap;
                        if (Mathf.Abs(delta) >= Limit)
                            Cars[i].Position += delta;
                    }
            }
        }

        private void setup()
        {
            if (Spline.Dirty)
                Spline.Refresh();

            Cars = GetComponentsInChildren<E12_TrainCarManager>();
            float pos = Position - (CarSize / 2);

            for (int i = 0; i < Cars.Length; i++)
            {
                Cars[i].setup();
                if (Cars[i].BackAxis
                    && Cars[i].FrontAxis
                    && Cars[i].Waggon)
                    Cars[i].Position = pos;
                pos -= CarSize + CarGap;
            }

            isSetup = true;
        }
    }
}