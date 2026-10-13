using UnityEngine;

namespace MK.Rebuild
{
    /// <summary>
    /// M1 slice-3 ability data SHELL (PREPARE-only, 2026-10-13): the fields are ready
    /// so H1 (proyectil) / H2 (giro con púas) can enter the combo model without a
    /// refactor, but there is NO behavior here — no projectile, no spin, no cooldown
    /// runtime, no multi-hit. When a <see cref="ComboStep"/> has
    /// <c>stepType == Ability</c>, <see cref="PlayerCombat"/> does NOT probe the generic
    /// hitbox; a future AbilitySystem consumes this definition via the seam.
    ///
    /// H2 = 12 hits × 2 passes does NOT fit the one-hit-per-step model of
    /// <see cref="ComboStep"/>: this definition OWNS the hit pattern (<see cref="hits"/>),
    /// the cooldown and the effect. IDs: ASCII rename-ready convention ('ABIL_H1',
    /// 'ABIL_H2' — doc 04).
    /// </summary>
    [CreateAssetMenu(fileName = "Ability", menuName = "MK/Rebuild/AbilityDefinition")]
    public sealed class AbilityDefinition : ScriptableObject
    {
        [Tooltip("Stable id (e.g. 'ABIL_H1', 'ABIL_H2'). Not used for logic in slice 3 — rename-ready.")]
        public string id;

        [Tooltip("Cooldown in SECONDS between uses (H1 proyectil / H2 giro — doc 05). PREPARE-only: not enforced yet.")]
        public float cooldown;

        [Tooltip("Damage dealt to a HitReceiver on a single hit (H1 15, H2 per-hit — doc 05).")]
        public int dano;

        [Tooltip("Ability animation clip (Generic rig, no root motion, no Animation Events). Assigned later.")]
        public AnimationClip clip;

        [Tooltip("Active hit window as NORMALIZED time of the clip: x = t0, y = t1 (both in [0,1]).")]
        public Vector2 hitNorm;

        [Tooltip("Total hits of the ability (H2 = 12x2 passes, deferred; 1 = single hit). The AbilityDefinition owns the hit pattern, NOT the combo step.")]
        public int hits;

        [Tooltip("Optional visual/gameplay effect reference (VFX prefab, etc.). PREPARE-only.")]
        public Object effectRef;
    }
}