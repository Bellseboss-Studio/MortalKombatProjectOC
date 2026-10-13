using System;
using System.Collections.Generic;
using UnityEngine;

namespace MK.Rebuild
{
    /// <summary>
    /// Input that triggers/advances a combo step. APPEND ONLY — the int values are
    /// serialized in the combo assets (Combo_1.asset: Ligero=0, Pesado=1) and must
    /// stay stable forever. H1/H2/Launcher/Dash were appended on 2026-10-13 (slice 3)
    /// for the dash-cancel + ability PREPARE.
    /// </summary>
    public enum ComboInput
    {
        Ligero = 0,
        Pesado = 1,
        H1 = 2,
        H2 = 3,
        Launcher = 4,
        Dash = 5
    }

    /// <summary>
    /// What a <see cref="ComboStep"/> executes: a direct attack (same hitbox seam as
    /// slice 2) or an ability, owned by its <see cref="AbilityDefinition"/>. Append-only.
    /// </summary>
    public enum ComboStepType
    {
        Attack = 0,
        Ability = 1
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
        [Tooltip("Input that enters/advances into this step (Ligero = gamepad X / click izq, Pesado = Y / click der, H1/H2/Launcher/Dash = PREPARE-only, sin behavior).")]
        public ComboInput input;

        [Tooltip("What this step executes: Attack = the generic hitbox seam, Ability = delegates to the abilityRef (no generic probe). Default Attack.")]
        public ComboStepType stepType;

        [Tooltip("Ability definition used when stepType == Ability. PREPARE-only in slice 3: no projectile/spin/multi-hit behavior yet (H2 12x2 deferred — the AbilityDefinition owns the hit pattern).")]
        public AbilityDefinition abilityRef;

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

        [Tooltip("Forward-advance speed SHAPE over the step (normalized 1 -> 0): the impulse decays from strong to zero. Empty = default ease-out. Total advance is still 'lunge' (the code integrates the curve).")]
        public AnimationCurve lungeCurve;

        [Tooltip("Animator Attack_P{n} state index for this step (1..N). 0 = auto (step index + 1). Set > 0 to make a 1-step combo play a deeper clip (e.g. Combo_H -> 3 = quick_P3).")]
        public int attackStateIndex;
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

        [Tooltip("Dash CANCELS this combo entirely (doc: 'dash cancela TODO salvo hitstop propio' — the dash is the defense, not a chain link). Default ON.")]
        public bool dashCancelsAll = true;

        /// <summary>Number of steps (0 when the list is null).</summary>
        public int Length => steps != null ? steps.Count : 0;

        /// <summary>
        /// Editor-time defaulting: every step without an authored lunge curve gets
        /// the shared ease-out shape so the inspector is honest and existing assets
        /// (authored before the curve existed) behave as intended. Runtime has the
        /// same fallback in <see cref="CurveMotion.OrDefault"/>.
        /// </summary>
        private void OnValidate()
        {
            if (steps == null) return;
            for (int i = 0; i < steps.Count; i++)
            {
                ComboStep s = steps[i];
                if (!CurveMotion.IsUsable(s.lungeCurve))
                {
                    s.lungeCurve = CurveMotion.DefaultEaseOut();
                    steps[i] = s;
                }
            }
        }

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
