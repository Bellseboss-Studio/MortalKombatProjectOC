// =====================================================================
// Copyright © 2013 ToolBuddy
// All rights reserved
// 
// http://www.toolbuddy.net
// =====================================================================

using System.Collections.Generic;
using System.Reflection;
using FluffyUnderware.DevTools;
using FluffyUnderware.DevTools.Extensions;
using JetBrains.Annotations;
using UnityEngine;

namespace FluffyUnderware.Curvy.Examples
{
    public class E99_PerformanceAPI : MonoBehaviour
    {
        private const int LOOPS = 20;
        private readonly List<string> mTests = new List<string>();
        private readonly List<string> mTestResults = new List<string>();

        private CurvyInterpolation mInterpolation = CurvyInterpolation.CatmullRom;
        private CurvyOrientation mOrientation = CurvyOrientation.Dynamic;
        private int mCacheSize = 50;
        private int mControlPointCount = 20;
        private int mTotalSplineLength = 100;
        private bool mUseCache;
        private bool mUseMultiThreads = true;

        private int mCurrentTest = -1;
        private bool mExecuting;
        private readonly TimeMeasure Timer = new TimeMeasure(LOOPS);
        private MethodInfo mGUIMethod;
        private MethodInfo mRunMethod;

        [UsedImplicitly]
        private void Awake()
        {
            mTests.Add("Interpolate");
            mTests.Add("Refresh");
        }

        [UsedImplicitly]
        private void OnGUI()
        {
            GUILayout.BeginVertical(GUI.skin.box);
            GUILayout.Label("Curvy offers various options to fine-tune performance vs. precision balance:");
            // Interpolation
            GUILayout.BeginHorizontal();
            GUILayout.Label("Interpolation: ");
            if (GUILayout.Toggle(
                    mInterpolation == CurvyInterpolation.Linear,
                    "Linear",
                    GUI.skin.button
                ))
                mInterpolation = CurvyInterpolation.Linear;
            if (GUILayout.Toggle(
                    mInterpolation == CurvyInterpolation.Bezier,
                    "Bezier",
                    GUI.skin.button
                ))
                mInterpolation = CurvyInterpolation.Bezier;
            if (GUILayout.Toggle(
                    mInterpolation == CurvyInterpolation.CatmullRom,
                    "CatmullRom",
                    GUI.skin.button
                ))
                mInterpolation = CurvyInterpolation.CatmullRom;
            if (GUILayout.Toggle(
                    mInterpolation == CurvyInterpolation.TCB,
                    "TCB",
                    GUI.skin.button
                ))
                mInterpolation = CurvyInterpolation.TCB;
            GUILayout.EndHorizontal();
            // Orientation
            GUILayout.BeginHorizontal();
            GUILayout.Label("Orientation: ");
            if (GUILayout.Toggle(
                    mOrientation == CurvyOrientation.None,
                    "None",
                    GUI.skin.button
                ))
                mOrientation = CurvyOrientation.None;
            if (GUILayout.Toggle(
                    mOrientation == CurvyOrientation.Static,
                    "Static",
                    GUI.skin.button
                ))
                mOrientation = CurvyOrientation.Static;
            if (GUILayout.Toggle(
                    mOrientation == CurvyOrientation.Dynamic,
                    "Dynamic",
                    GUI.skin.button
                ))
                mOrientation = CurvyOrientation.Dynamic;
            GUILayout.EndHorizontal();
            // CP-Count
            GUILayout.BeginHorizontal();
            GUILayout.Label("Control Points (max): " + mControlPointCount);
            mControlPointCount = (int)GUILayout.HorizontalSlider(
                mControlPointCount,
                2,
                1000
            );
            GUILayout.EndHorizontal();
            // Length
            GUILayout.BeginHorizontal();
            GUILayout.Label("Total spline length: " + mTotalSplineLength);
            mTotalSplineLength = (int)GUILayout.HorizontalSlider(
                mTotalSplineLength,
                5,
                10000
            );
            GUILayout.EndHorizontal();
            // Cache
            GUILayout.BeginHorizontal();
            GUILayout.Label("Cache Density: " + mCacheSize);
            mCacheSize = (int)GUILayout.HorizontalSlider(
                mCacheSize,
                1,
                100
            );
            GUILayout.EndHorizontal();
            mUseCache = GUILayout.Toggle(
                mUseCache,
                "Use Cache (where applicable)"
            );
            mUseMultiThreads = GUILayout.Toggle(
                mUseMultiThreads,
                "Use Multiple Threads (where applicable)"
            );
            GUILayout.Label("Select Test:");

            int sel = GUILayout.SelectionGrid(
                Mathf.Max(
                    0,
                    mCurrentTest
                ),
                mTests.ToArray(),
                4
            );
            if (sel != mCurrentTest)
            {
                mCurrentTest = sel;
                Timer.Clear();
                mTestResults.Clear();

                mGUIMethod = GetType().MethodByName(
                    "GUI_" + mTests[mCurrentTest],
                    false,
                    true
                );
                mRunMethod = GetType().MethodByName(
                    "Test_" + mTests[mCurrentTest],
                    false,
                    true
                );
            }

            GUILayout.Space(5);
            if (mGUIMethod != null)
                mGUIMethod.Invoke(
                    this,
                    null
                );
            GUI.enabled = !mExecuting && mRunMethod != null;
            string label = mExecuting
                ? "Please wait..."
                : "Run (" + LOOPS + " times)";
            if (GUILayout.Button(label))
            {
                mExecuting = true;
                Timer.Clear();
                mTestResults.Clear();
                Invoke(
                    nameof(runTest),
                    .5f
                );
            }

            GUI.enabled = true;
            if (Timer.Count > 0)
            {
                foreach (string s in mTestResults)
                    GUILayout.Label(s);
                GUILayout.Label(
                    string.Format(
                        "Average (ms): {0:0.0000}",
                        Timer.AverageMS
                    )
                );
                GUILayout.Label(
                    string.Format(
                        "Minimum (ms): {0:0.0000}",
                        Timer.MinimumMS
                    )
                );
                GUILayout.Label(
                    string.Format(
                        "Maximum (ms): {0:0.0000}",
                        Timer.MaximumMS
                    )
                );
            }

            GUILayout.EndVertical();
        }


        #region ### Tests ###

        private bool mInterpolate_UseDistance;

        [UsedImplicitly]
        private void GUI_Interpolate()
        {
            GUILayout.Label("Interpolates position");
            mInterpolate_UseDistance = GUILayout.Toggle(
                mInterpolate_UseDistance,
                "By Distance"
            );
        }

        [UsedImplicitly]
        private void Test_Interpolate()
        {
            CurvySpline spl = getSpline();
            AddCPs(
                ref spl,
                mControlPointCount,
                mTotalSplineLength
            );
            mTestResults.Add("Cache Points: " + spl.CacheSize);
            mTestResults.Add(
                string.Format(
                    "Cache Point Distance: {0:0.000}",
                    mTotalSplineLength / (float)spl.CacheSize
                )
            );

            Vector3 v = Vector3.zero;
            if (mInterpolate_UseDistance)
                for (int i = 0; i < LOOPS; i++)
                {
                    float d = Random.Range(
                        0,
                        spl.Length
                    );
                    if (mUseCache)
                    {
                        Timer.Start();
                        v = spl.InterpolateByDistanceFast(d);
                        Timer.Stop();
                    }
                    else
                    {
                        Timer.Start();
                        v = spl.InterpolateByDistance(d);
                        Timer.Stop();
                    }
                }
            else
                for (int i = 0; i < LOOPS; i++)
                {
                    float f = Random.Range(
                        0,
                        1
                    );
                    if (mUseCache)
                    {
                        Timer.Start();
                        v = spl.InterpolateFast(f);
                        Timer.Stop();
                    }
                    else
                    {
                        Timer.Start();
                        v = spl.Interpolate(f);
                        Timer.Stop();
                    }
                }

            Destroy(spl.gameObject);
            // Prevent "unused variable" compiler warning
            v.Set(
                0,
                0,
                0
            );
        }

        private int mRefresh_Mode;

        [UsedImplicitly]
        private void GUI_Refresh()
        {
            GUILayout.Label("Refresh Spline or Single segment!");
            GUILayout.BeginHorizontal();
            GUILayout.Label("Mode:");
            mRefresh_Mode = GUILayout.SelectionGrid(
                mRefresh_Mode,
                new[] { "All", "Single random segment" },
                2
            );
            GUILayout.EndHorizontal();
        }

        [UsedImplicitly]
        private void Test_Refresh()
        {
            CurvySpline spl = getSpline();
            AddCPs(
                ref spl,
                mControlPointCount,
                mTotalSplineLength
            );
            mTestResults.Add("Cache Points: " + spl.CacheSize);
            mTestResults.Add(
                string.Format(
                    "Cache Point Distance: {0:0.000}",
                    mTotalSplineLength / (float)spl.CacheSize
                )
            );

            for (int i = 0; i < LOOPS; i++)
            {
                int idx = Random.Range(
                    0,
                    spl.Count - 1
                );
                if (mRefresh_Mode == 0)
                {
                    Timer.Start();
                    spl.SetDirtyAll(
                        SplineDirtyingType.Everything,
                        true
                    );
                    spl.Refresh();
                    Timer.Stop();
                    //Debug.Log(Timer.LastMS);
                }
                else
                {
                    Timer.Start();
                    spl.SetDirty(
                        spl[idx],
                        SplineDirtyingType.Everything
                    );
                    spl.Refresh();
                    Timer.Stop();
                }
            }

            Destroy(spl.gameObject);
        }

        #endregion

        #region ### Helpers ###

        private CurvySpline getSpline()
        {
            CurvySpline spl = CurvySpline.Create();
            spl.Interpolation = mInterpolation;
            spl.Orientation = mOrientation;
            spl.CacheDensity = mCacheSize;
            spl.UseThreading = mUseMultiThreads;
            spl.Refresh();
            return spl;
        }

        private void AddCPs(ref CurvySpline spline, int count, int totalLength)
        {
            Vector3[] pos = new Vector3[count];
            float segLength = totalLength / (float)(count - 1);
            pos[0] = Vector3.zero;
            for (int i = 1; i < count; i++)
                pos[i] = pos[i - 1]
                + new Vector3(
                    segLength,
                    0,
                    0
                );

            spline.Add(pos);
            spline.ControlPointsList[0].Swirl = CurvyOrientationSwirl.AnchorGroupAbs;
            spline.ControlPointsList[0].SwirlTurns = 1;
            spline.Refresh();
        }

        private void runTest()
        {
            mRunMethod.Invoke(
                this,
                null
            );
            mExecuting = false;
        }

        #endregion
    }
}