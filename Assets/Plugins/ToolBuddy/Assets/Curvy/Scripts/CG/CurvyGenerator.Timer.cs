// =====================================================================
// Copyright © 2013 ToolBuddy
// All rights reserved
// 
// http://www.toolbuddy.net
// =====================================================================

using FluffyUnderware.DevTools;
using UnityEngine;

namespace FluffyUnderware.Curvy.Generator
{
    public partial class CurvyGenerator
    {
        private class Timer
        {
            /// <summary>
            /// Expressed in seconds of real time.
            /// </summary>
            private double lastTimestamp;

#if UNITY_EDITOR
            /// <summary>
            /// Expressed in seconds of real time.
            /// </summary>
            private double lastEditorTimestamp;
#endif

            /// <summary>
            /// Expressed in seconds of real time
            /// </summary>
            private static double Now => DTTime.TimeSinceStartup;


            /// <summary>
            /// 
            /// </summary>
            /// <param name="timeLimit">Expressed in seconds of real time</param>
            /// <param name="editorTimeLimit">Expressed in seconds of real time</param>
            private void ValidateTimes(float timeLimit, float editorTimeLimit)
            {
                //The reason behind the validations bellow is probably solved, so the validations can probably be removed, but I left them here just in case

                double now = Now;

#if UNITY_EDITOR
                if (lastEditorTimestamp > now)
                {
#if CURVY_SANITY_CHECKS
                    DTLog.LogWarning($"[Curvy] Timer: Timestamp is too big: lastEditorTimestamp {lastEditorTimestamp} now {now}");
#endif
                    lastEditorTimestamp = now - editorTimeLimit;
                }
#endif

                if (lastTimestamp > now)
                {
#if CURVY_SANITY_CHECKS
                    DTLog.LogWarning($"[Curvy] Timer: Timestamp is too big: lastTimestamp {lastTimestamp} now {now}");
#endif
                    lastTimestamp = now - timeLimit;
                }
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="timeLimit">Expressed in seconds of real time</param>
            /// <param name="editorTimeLimit">Expressed in seconds of real time</param>
            /// <returns>Was a time limit reached this update</returns>
            public bool Update(float timeLimit, float editorTimeLimit)
            {
                double now = Now;

                ValidateTimes(
                    timeLimit,
                    editorTimeLimit
                );

                bool hasTimeElapsed;
                if (Application.isPlaying)
                {
                    if (now - lastTimestamp > timeLimit)
                    {
                        lastTimestamp = now;
                        hasTimeElapsed = true;
                    }
                    else
                        hasTimeElapsed = false;
                }
                else
                {
#if UNITY_EDITOR
                    if (now - lastEditorTimestamp > editorTimeLimit)
                    {
                        lastEditorTimestamp = now;
                        hasTimeElapsed = true;
                    }
                    else
                        hasTimeElapsed = false;

#else
                    hasTimeElapsed = false;
#endif
                }

                return hasTimeElapsed;
            }

            public void Reset()
            {
                lastTimestamp = 0;
#if UNITY_EDITOR
                lastEditorTimestamp = 0;
#endif
            }
        }
    }
}