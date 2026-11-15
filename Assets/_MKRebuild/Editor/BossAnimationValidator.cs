using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace MK.Rebuild.EditorTools
{
    /// <summary>
    /// Validates the boss animation contract: every state the controller exposes MUST match
    /// the duration its <see cref="BossDefinition"/> declares (one frame of tolerance at
    /// 60 fps), every looping state MUST be marked as such, and none may be missing or
    /// carry a null motion.
    ///
    /// The contract matters because a clip that disagrees with its data desynchronizes the
    /// visual from the hitbox: the boss either lands a hit with a frozen pose or holds its
    /// last frame while the controller is still counting down.
    ///
    /// Run it by hand before committing a re-exported FBX. It never runs in a build.
    /// </summary>
    public static class BossAnimationValidator
    {
        private const float Fps = 60f;
        private const float Tolerance = 1f / Fps;

        private static readonly string[] ExpectedStates =
        {
            "Spawn", "Idle", "Move", "Hit", "Death",
            "Slam", "Charge", "ChargeRun", "ChargeRecover", "Roar",
        };

        // ChargeRun loops because the controller ends the travel on distance OR on geometry.
        private static readonly string[] LoopStates = { "Idle", "Move", "ChargeRun" };

        private sealed class BossTarget
        {
            public string label;
            public string definitionPath;
            public string controllerPath;
        }

        private static readonly BossTarget[] Targets =
        {
            new BossTarget
            {
                label = "Guardia",
                definitionPath = "Assets/_MKRebuild/Data/Boss_Brute.asset",
                controllerPath = "Assets/_MKRebuild/Animations/Enemies/EB_Guard.controller",
            },
            new BossTarget
            {
                label = "Orochi",
                definitionPath = "Assets/_MKRebuild/Data/Boss_Orochi.asset",
                controllerPath = "Assets/_MKRebuild/Animations/Enemies/EB_Orochi.controller",
            },
        };

        [MenuItem("MK/Validation/Boss Animation Set")]
        public static void ValidateAll()
        {
            int failures = 0;
            int validated = 0;

            for (int i = 0; i < Targets.Length; i++)
            {
                BossTarget target = Targets[i];
                BossDefinition definition = AssetDatabase.LoadAssetAtPath<BossDefinition>(target.definitionPath);
                AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(target.controllerPath);

                if (definition == null || controller == null)
                {
                    string missing = definition == null ? target.definitionPath : target.controllerPath;
                    Debug.LogWarning($"[BossAnim] {target.label}: todavía no existe {missing} — se omite.");
                    continue;
                }

                validated++;
                failures += Validate(target, definition, controller);
            }

            if (failures == 0)
            {
                Debug.Log($"[BossAnim] OK — {validated} boss(es) validados contra su data.");
            }
            else
            {
                Debug.LogError($"[BossAnim] {failures} problema(s) de contrato en {validated} boss(es).");
            }
        }

        private static int Validate(BossTarget target, BossDefinition definition, AnimatorController controller)
        {
            Dictionary<string, AnimationClip> clips = CollectClips(controller);
            int failures = 0;

            for (int i = 0; i < ExpectedStates.Length; i++)
            {
                string stateName = ExpectedStates[i];
                if (!clips.TryGetValue(stateName, out AnimationClip clip))
                {
                    Debug.LogError($"[BossAnim] {target.label}: falta el estado '{stateName}' o no tiene motion.");
                    failures++;
                    continue;
                }

                float expected = ExpectedLength(stateName, definition);
                if (expected > 0f && Mathf.Abs(clip.length - expected) > Tolerance)
                {
                    Debug.LogError(
                        $"[BossAnim] {target.label}/{stateName}: esperado {expected:0.000}s, " +
                        $"medido {clip.length:0.000}s, delta {clip.length - expected:+0.000;-0.000}s");
                    failures++;
                }

                if (ShouldLoop(stateName) && !AnimationUtility.GetAnimationClipSettings(clip).loopTime)
                {
                    Debug.LogError($"[BossAnim] {target.label}/{stateName}: debe tener Loop Time.");
                    failures++;
                }
            }

            return failures;
        }

        private static Dictionary<string, AnimationClip> CollectClips(AnimatorController controller)
        {
            var map = new Dictionary<string, AnimationClip>();
            if (controller.layers.Length == 0) return map;

            AnimatorStateMachine machine = controller.layers[0].stateMachine;
            if (machine == null) return map;

            ChildAnimatorState[] states = machine.states;
            for (int i = 0; i < states.Length; i++)
            {
                AnimatorState state = states[i].state;
                if (state == null) continue;

                if (state.motion is AnimationClip clip && !map.ContainsKey(state.name))
                {
                    map.Add(state.name, clip);
                }
            }

            return map;
        }

        /// <summary>
        /// Expected clip length in seconds, straight from the data. Zero means "no gameplay
        /// window": Spawn, Idle, Move and Hit are free, and ChargeRun is cut by the controller.
        /// </summary>
        private static float ExpectedLength(string stateName, BossDefinition definition)
        {
            switch (stateName)
            {
                case "Slam":
                    return definition.slamTell + definition.slamRecovery;
                case "Charge":
                    return definition.chargeTell;
                case "ChargeRecover":
                    return definition.chargeRecovery;
                case "Roar":
                    return definition.roarDuration;
                case "Death":
                    return definition.deathDelay;
                default:
                    return 0f;
            }
        }

        private static bool ShouldLoop(string stateName)
        {
            for (int i = 0; i < LoopStates.Length; i++)
            {
                if (LoopStates[i] == stateName) return true;
            }

            return false;
        }
    }
}
