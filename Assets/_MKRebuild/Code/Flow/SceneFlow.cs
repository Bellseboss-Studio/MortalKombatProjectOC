using System;
using System.Collections.Generic;
using UnityEngine;

namespace MK.Rebuild
{
    /// <summary>
    /// Ordered chain of scene NAMES shared by the flow controller and the build-list generator.
    ///
    /// WHY names and not <c>SceneAsset</c>: <c>SceneAsset</c> lives in UnityEditor. Referencing it
    /// from this runtime asset would compile in the editor and break the player build, so the asset
    /// stores plain names and the editor tool validates them against Build Settings instead.
    ///
    /// The array is the single source of order: entry 0 is the boot scene and every
    /// <see cref="NextAfter"/> is a lookup in this list. No build index is ever stored.
    /// </summary>
    [CreateAssetMenu(menuName = "MK/Flow/Scene Flow", fileName = "SceneFlow")]
    public sealed class SceneFlow : ScriptableObject
    {
        [Tooltip("Ordered scene names. The first entry is the build's boot scene.")]
        [SerializeField] private string[] sceneNames = Array.Empty<string>();

        /// <summary>The chain in authored order. Empty until a flow asset is populated (WU5).</summary>
        public IReadOnlyList<string> Scenes => sceneNames ?? Array.Empty<string>();

        /// <summary>Boot scene, or <c>null</c> when the asset has no entries.</summary>
        public string FirstScene
        {
            get
            {
                IReadOnlyList<string> scenes = Scenes;
                return scenes.Count > 0 ? scenes[0] : null;
            }
        }

        /// <summary>
        /// Scene after <paramref name="current"/>, or <c>null</c> when it is unknown or last.
        /// Callers treat <c>null</c> as "end of chain" and log it; this method never guesses.
        /// </summary>
        public string NextAfter(string current)
        {
            if (string.IsNullOrWhiteSpace(current)) return null;

            IReadOnlyList<string> scenes = Scenes;
            for (int i = 0; i < scenes.Count - 1; i++)
            {
                if (Same(scenes[i], current)) return scenes[i + 1];
            }

            return null;
        }

        /// <summary>True when <paramref name="scene"/> is one of the authored names.</summary>
        public bool Contains(string scene)
        {
            if (string.IsNullOrWhiteSpace(scene)) return false;

            IReadOnlyList<string> scenes = Scenes;
            for (int i = 0; i < scenes.Count; i++)
            {
                if (Same(scenes[i], scene)) return true;
            }

            return false;
        }

        /// <summary>
        /// Internal consistency check for editor tooling: no blank names and no duplicates.
        /// Agreement with Build Settings is validated by the editor generator, which can see the
        /// settings file; this runtime class deliberately cannot.
        /// </summary>
        public bool Validate(out string reason)
        {
            IReadOnlyList<string> scenes = Scenes;
            if (scenes.Count == 0)
            {
                reason = "The flow has no scenes.";
                return false;
            }

            var seen = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < scenes.Count; i++)
            {
                string scene = scenes[i];
                if (string.IsNullOrWhiteSpace(scene))
                {
                    reason = $"Entry {i} is blank.";
                    return false;
                }

                if (!seen.Add(scene))
                {
                    reason = $"'{scene}' appears more than once.";
                    return false;
                }
            }

            reason = null;
            return true;
        }

        private static bool Same(string a, string b)
        {
            return string.Equals(a, b, StringComparison.Ordinal);
        }
    }
}
