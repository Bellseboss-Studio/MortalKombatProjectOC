// =====================================================================
// Copyright © 2013 ToolBuddy
// All rights reserved
// 
// http://www.toolbuddy.net
// =====================================================================

using System;
using System.Linq;
using FluffyUnderware.Curvy;
using FluffyUnderware.DevTools.Extensions;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;
using Random = System.Random;

namespace FluffyUnderware.CurvyEditor.Network
{
    /// <summary>
    /// Sends metrics to Curvy's server about version usage of Curvy, Unity and Scripting
    /// </summary>
    [InitializeOnLoad]
    internal class Metrics
    {
        private UnityWebRequest WebRequest { get; set; }

        private static string CurvyVersion => AssetInformation.Version;

        private static string UnityVersion => Application.unityVersion;

        private static string ScriptingRuntimeVersion => "Latest";

        static Metrics()
        {
            if (CurvyProject.Instance.EnableMetrics == false)
                return;

            const string preferenceName = "TrackedVersions";
            string[] trackedVersions = CurvyProject.Instance.GetEditorPrefs(preferenceName);
            string version_id = String.Format(
                "{0}_{1}_{2}",
                CurvyVersion,
                UnityVersion,
                ScriptingRuntimeVersion
            );
            if (trackedVersions.Contains(version_id) == false)
            {
                new Metrics().Send(trackedVersions.Any() == false);
                CurvyProject.Instance.SetEditorPrefs(
                    preferenceName,
                    trackedVersions.Add(version_id)
                );
            }
        }

        /// <summary>
        /// Sends metrics to Curvy's server about version usage of Curvy, Unity and Scripting
        /// </summary>
        private void Send(bool isFirstTime)
        {
            string url = "https://analytics.curvyeditor.com/piwik.php?"
                         + "idsite=1"
                         + "&rec=1"
                         + "&apiv=1"
                         + "&rand="
                         + new Random().Next(
                             0,
                             1000000
                         ).ToString("000000")
                         + "&dimension1="
                         + CurvyVersion
                         + "&dimension2="
                         + UnityVersion
                         + "&dimension3="
                         + ScriptingRuntimeVersion
                         + "&dimension4="
                         + isFirstTime
                         + "&_id="
                         + SystemInfo.deviceUniqueIdentifier.Substring(
                             0,
                             16
                         )
                         + "&action_name=Curvy_Splines";


#if CURVY_DEBUG
            Debug.Log(url);
#endif
            WebRequest = UnityWebRequest.Get(url);
            WebRequest.SendWebRequest();
            EditorApplication.update += CheckWebRequest;
        }

        private void CheckWebRequest()
        {
            if (WebRequest.isDone)
            {
                EditorApplication.update -= CheckWebRequest;
#if CURVY_DEBUG
                if (WebRequest.IsError())
                    Debug.LogError("Error: " + WebRequest.error);
                else
                    Debug.Log("Received: " + WebRequest.downloadHandler.text);
#endif

                WebRequest.Dispose();
            }
        }
    }
}