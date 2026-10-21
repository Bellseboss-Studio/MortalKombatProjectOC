using UnityEngine;

namespace MK.Rebuild
{
    /// <summary>
    /// M1 slice-1 combat data: one ScriptableObject per ISOLATED attack (light,
    /// heavy). Grey-box model of the documented <c>ComboStep</c> — the doc's
    /// normalized timing is the target, but slice 1 has no clips, so the active
    /// hit window is data-driven (<see cref="duration"/> seconds + normalized
    /// <see cref="hitNorm"/> [t0,t1]). Hits are timed by code, NEVER by
    /// Animation Events (doc pillar: timing parametrizado).
    /// All numbers live here — no hardcoded combat constants in code.
    /// </summary>
    [CreateAssetMenu(fileName = "Attack", menuName = "MK/Rebuild/AttackDefinition")]
    public sealed class AttackDefinition : ScriptableObject
    {
        [Tooltip("Stable id (e.g. 'light', 'heavy'). Not used for logic in slice 1 — rename-ready.")]
        public string id;

        [Tooltip("Damage dealt to a HitReceiver on connect (light 6, heavy 9 — doc 05).")]
        public int damage;

        [Tooltip("Hitstop in MILLISECONDS on connect (light 40, heavy 90 — doc 05).")]
        public int hitstopMs;

        [Tooltip("Total attack duration in SECONDS (grey-box swing).")]
        public float duration;

        [Tooltip("Active hit window as NORMALIZED time of duration: x = t0, y = t1 (both in [0,1]).")]
        public Vector2 hitNorm;

        [Tooltip("Hitbox sphere radius in meters (doc 08: hitboxes BIGGER than the visual, in your favor).")]
        public float hitboxRadius;

        [Tooltip("Hitbox origin offset in the attacker's local space (x = sideways, y = height, z = forward).")]
        public Vector3 hitboxOffset;

        [Tooltip("Victims this attack may damage (1 = honest single target, 3 = cleave). Clamped to >= 1 at runtime.")]
        [Min(1)] public int maxTargets = 1;

        [Tooltip("Knockback push distance in meters applied to the victim on connect (manual nudge — dummy has no Rigidbody).")]
        public float knockback;
    }
}