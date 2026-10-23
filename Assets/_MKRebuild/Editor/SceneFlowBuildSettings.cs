using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace MK.Rebuild.EditorTools
{
    /// <summary>
    /// Generates the EditorBuildSettings scene list from the <see cref="SceneFlow"/>
    /// asset so the demo chain never depends on manual build-index edits.
    ///
    /// This is the single sanctioned write to ProjectSettings/EditorBuildSettings.asset:
    /// the rebuild's end state is a self-contained _MKRebuild, and the player build must
    /// start at Intro00 and reach Playground in the flow's order.
    /// </summary>
    public static class SceneFlowBuildSettings
    {
        private const string FlowPath = "Assets/_MKRebuild/Data/SceneFlow.asset";
        private const string SceneFolder = "Assets/_MKRebuild/Scenes";

        [MenuItem("MK/Migration/Sync Build Settings")]
        public static void Sync()
        {
            SceneFlow flow = AssetDatabase.LoadAssetAtPath<SceneFlow>(FlowPath);
            if (flow == null)
            {
                Debug.LogError("[SceneFlow] Missing flow asset at " + FlowPath, null);
                return;
            }

            var scenes = new List<EditorBuildSettingsScene>();
            IReadOnlyList<string> names = flow.Scenes;
            for (int i = 0; i < names.Count; i++)
            {
                string name = names[i];
                string path = SceneFolder + "/" + name + ".unity";
                if (AssetDatabase.LoadAssetAtPath<SceneAsset>(path) == null)
                {
                    Debug.LogWarning("[SceneFlow] Scene not found, skipped: " + path, null);
                    continue;
                }

                scenes.Add(new EditorBuildSettingsScene(path, true));
            }

            EditorBuildSettings.scenes = scenes.ToArray();
            Debug.Log("[SceneFlow] Build Settings synced: " + scenes.Count + " scene(s).", null);
        }

        [MenuItem("MK/Migration/Validate Build Settings")]
        public static void Validate()
        {
            SceneFlow flow = AssetDatabase.LoadAssetAtPath<SceneFlow>(FlowPath);
            EditorBuildSettingsScene[] current = EditorBuildSettings.scenes;
            if (flow == null)
            {
                Debug.LogError("[SceneFlow] Missing flow asset at " + FlowPath, null);
                return;
            }

            int index = 0;
            IReadOnlyList<string> names = flow.Scenes;
            for (int i = 0; i < names.Count; i++)
            {
                string path = SceneFolder + "/" + names[i] + ".unity";
                if (AssetDatabase.LoadAssetAtPath<SceneAsset>(path) == null) continue;

                if (index >= current.Length || current[index].path != path || !current[index].enabled)
                {
                    Debug.LogError("[SceneFlow] Build Settings drift at index " + index + ": expected " + path, null);
                    return;
                }

                index++;
            }

            if (index != current.Length)
            {
                Debug.LogError("[SceneFlow] Build Settings has " + (current.Length - index) + " extra scene(s).", null);
                return;
            }

            Debug.Log("[SceneFlow] Build Settings OK: " + index + " scene(s).", null);
        }
    }
}
