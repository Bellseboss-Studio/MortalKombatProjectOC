using System;
using System.Collections.Generic;
using UnityEngine;

namespace MK.Rebuild
{
    /// <summary>
    /// Input that triggers/advances a combo step. Slice 2 only needs the two
    /// existing buttons (gamepad X = LightAttack, Y = HeavyAttack); H1/H2/launcher
    /// are deferred (rename-ready enum).
    /// </summary>
    public enum ComboInput
    {
        Ligero = 0,
        Pesado = 1
    }

    /// <summary>Continuation of a step: the input that advances and the target step index.</summary>
    [Serializable]
    public struct ComboLink
    {
        [Tooltip("Input that advances into the next step (Ligero/Pesado).")]
        public ComboInput input;

        [Tooltip("Target step index (0-based). -1 = no continuation (finisher / end of combo).")]
        public int paso;
    }

    /// <summary>
    /// One step of a data-driven combo. Evolution of the grey-box
    /// <see cref="AttackDefinition"/> (doc 04): the SAME hitbox seam
    /// (hitNorm × duracion, hitstopMs, radius/offset) plus the combo metadata
    /// (clip, cancel windows, next step). Hits are timed by NORMALIZED time,
    /// NEVER by Animation Events (doc pillar: timing parametrizado).
    /// Deferred (rename-ready): launch, estiloPts, sfx.
    /// </summary>
    [Serializable]
    public struct ComboStep
    {
        [Tooltip("Input that enters/advances into this step (Ligero = gamepad X / click izq, Pesado = Y / click der).")]
        public ComboInput input;

        [Tooltip("Real attack clip (Generic rig, no root motion, no Animation Events).")]
        public AnimationClip clip;

        [Tooltip("Total step duration in SECONDS = FULL clip length (decision c). The combo logic divides it by speedMultiplier to get the effective (scaled) duration.")]
        public float duracion;

        [Tooltip("Active hit window as NORMALIZED time of duracion: x = t0, y = t1 (both in [0,1]).")]
        public Vector2 hitNorm;

        [Tooltip("Damage on connect (combo 1: 6 / 6 / 9 = 21 — doc 05 kills the 20 HP esbirro).")]
        public int dano;

        [Tooltip("Knockback push distance in meters applied to the victim on connect.")]
        public float knockback;

        [Tooltip("Hitstop in MILLISECONDS on connect (light 40, heavy 90 — doc 05).")]
        public int hitstopMs;

        [Tooltip("Hitbox sphere radius in meters (doc 08: hitboxes bigger than the visual).")]
        public float hitboxRadius;

        [Tooltip("Hitbox origin offset in the attacker's local space (x = sideways, y = height, z = forward).")]
        public Vector3 hitboxOffset;

        [Tooltip("Cancel window as NORMALIZED time when the hit MISSED (whiff): x = a, y = b.")]
        public Vector2 cancelNorm;

        [Tooltip("Cancel window when the hit CONNECTED (hit-cancel) — wider/earlier than cancelNorm. The juice.")]
        public Vector2 cancelHitNorm;

        [Tooltip("Next step: the input that advances and the target step index (paso = -1 ends the combo).")]
        public ComboLink siguiente;

        [Tooltip("Playback speed multiplier for this step's clip (1 = normal). Drives the state speed parameter.")]
        public float speedMultiplier;

        [Tooltip("While this step's swing runs, the motor IGNORES the horizontal wish input (no drift from the stick, no input rotation). Default ON.")]
        public bool moveLock;

        [Tooltip("Forward advance in METERS along the swing facing over the whole step (H&S step-into-the-strike). 0 = full stop. Applied only while moveLock is true.")]
        public float lunge;
    }

    /// <summary>
    /// Ordered list of <see cref="ComboStep"/> (doc: "combo = lista ordenada de pasos
    /// en ScriptableObject"). Adding a combo = a new asset; validation (clips exist,
    /// windows within duration, no dead-ends) is a documented rule, out of this slice.
    /// Combo 1 (demo): L→L→P = quick_P1 / quick_P2 / quick_P3.
    /// </summary>
    [CreateAssetMenu(fileName = "Combo", menuName = "MK/Rebuild/ComboDefinition")]
    public sealed class ComboDefinition : ScriptableObject
    {
        [Tooltip("Ordered combo steps. The first step is entered by its own input; later steps by the previous step's 'siguiente'.")]
        public List<ComboStep> steps = new List<ComboStep>();

        /// <summary>Number of steps (0 when the list is null).</summary>
        public int Length => steps != null ? steps.Count : 0;

        /// <summary>Bounds-checked accessor; false when the index is out of range.</summary>
        public bool TryGetStep(int index, out ComboStep step)
        {
            if (steps != null && index >= 0 && index < steps.Count)
            {
                step = steps[index];
                return true;
            }
            step = default;
            return false;
        }
    }
}
