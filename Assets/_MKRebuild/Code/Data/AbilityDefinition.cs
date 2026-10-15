using UnityEngine;

namespace MK.Rebuild
{
    /// <summary>
    /// M1 slice-3 ability data SHELL (PREPARE-only, 2026-10-13): the fields are ready
    /// so H1 (proyectil) / H2 (giro con púas) can enter the combo model without a
    /// refactor. m1-abilities (2026-10-15) consumes it at runtime: the projectile
    /// fields (H1) and the area radius (H2) are APPENDED below, and
    /// <see cref="PlayerAbilities"/> executes the ability from this data. The 12x2
    /// multi-hit and the cooldown runtime are STILL deferred. When a
    /// <see cref="ComboStep"/> has <c>stepType == Ability</c>, <see cref="PlayerCombat"/>
    /// does NOT probe the generic hitbox — it delegates to <see cref="PlayerAbilities"/>
    /// with <c>abilityRef</c>.
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

        // ------------------------------------------------------------------
        // m1-abilities runtime fields (APPEND-only, 2026-10-15). The legacy fields
        // above are untouched, so existing assets keep deserializing.
        // H1 (projectile) uses projectileSpeed > 0; H2 (area) uses areaRadius > 0.
        // ------------------------------------------------------------------

        [Tooltip("H1 projectile: travel speed in m/s along the spawn facing. 0 = this definition is NOT a projectile (H2 area).")]
        public float projectileSpeed;

        [Tooltip("H1 projectile: seconds before it despawns when it hits nothing.")]
        public float projectileLifetime;

        [Tooltip("H1 projectile: overlap-sphere radius (m) used to detect the FIRST HitReceiver.")]
        public float projectileRadius;

        [Tooltip("H1 projectile: muzzle spawn offset in the attacker's local space (x = sideways, y = height, z = forward). Same math as the hitbox origin.")]
        public Vector3 muzzleOffset;

        [Tooltip("H2 area: OverlapSphere radius (m), ONE-SHOT at the active window, damages ALL HitReceivers once. 0 = not an area ability. Multi-hit 12x2 deferred.")]
        public float areaRadius;

        // ------------------------------------------------------------------
        // m1-abilities SPEED PASS (2026-10-15, APPEND-only). The whole ability is
        // retimed by ONE data field: the Animator state speed is set to this value
        // AND the ability WINDOW (duration) becomes clip.length / this, so the
        // hit-window stays aligned with the visible animation and the lockout
        // shrinks with it. Mirrors PlayerParams.mantleClipSpeed / mantleDuration
        // (clipLength / duration). 1 = normal playback.
        // ------------------------------------------------------------------

        [Tooltip("Ability animation retime: Animator state speed multiplier for the ability clip (H1 tarkatan_shoot / H2 spinEdge). The ability WINDOW = clip.length / this, keeping the hit-window aligned with the visible impact. 1 = normal.")]
        public float clipSpeedMultiplier = 1f;
    }
}