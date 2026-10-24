using UnityEngine;

namespace MK.Rebuild
{
    /// <summary>What a hazard does to the player on contact / threshold crossing.</summary>
    public enum HazardConsequence
    {
        /// <summary>Teleport the player to the zone's authored safe point. NO damage (S4).</summary>
        ReturnToEdge = 0,

        /// <summary>Kill via an authored Y-threshold kill-plane (S4).</summary>
        KillAndRespawn = 1,

        /// <summary>Apply damage + knockback + i-frames (wall hazards, S3).</summary>
        Damage = 2,
    }

    /// <summary>How a hazard announces itself before contact. Pits use None (spatial read).</summary>
    public enum HazardTelegraph
    {
        None = 0,
        Visible = 1,
    }

    /// <summary>
    /// m2-hazards: the scene-agnostic DATA for one hazard. One definition is read by one
    /// <see cref="HazardZone"/>; every consequence is expressed by this object, never by a
    /// per-consequence class. The zone owns the scene-specific pieces (trigger collider and
    /// the ReturnToEdge safe-point Transform) because a ScriptableObject cannot reference a
    /// scene object.
    ///
    /// House style matches <see cref="StyleMeterDefinition"/>: public, inspector-tuned fields
    /// with the v0.1 numbers living in the authored asset — no hardcoded hazard constants.
    /// </summary>
    [CreateAssetMenu(fileName = "HazardDefinition", menuName = "MK/Rebuild/HazardDefinition")]
    public sealed class HazardDefinition : ScriptableObject
    {
        [Header("Identity")]
        [Tooltip("Stable, rename-ready hazard id (e.g. Gym_Wall_01). Debug/telemetry label — " +
                 "renaming the ASSET does not change this id.")]
        public string id = "hazard";

        [Header("Consequence")]
        [Tooltip("What the zone enforces: ReturnToEdge (S4), KillAndRespawn (S4) or Damage (S3).")]
        public HazardConsequence consequence = HazardConsequence.Damage;

        [Header("Damage (consequence = Damage)")]
        [Tooltip("Damage per contact. Wall v0.1 = 10 against the 100 HP player baseline " +
                 "(ten unguarded grazes kill).")]
        public int damage = 10;

        [Tooltip("Knockback DISTANCE in meters (linear decay honours this exactly). Wall v0.1 = 2.5.")]
        public float knockbackDistance = 2.5f;

        [Tooltip("Knockback duration override in seconds. 0 = DERIVE from the hit-reaction " +
                 "animation (AnimatorVisual.HitReactionDuration) — the default, so the shove " +
                 "and the clip end together. >0 pins an explicit duration (still linear).")]
        public float knockbackDuration = 0f;

        [Tooltip("Invulnerability window after a hit, in seconds. Wall v0.1 = 0.6: sliding " +
                 "along the wall cannot re-damage inside this window. Reuses the single " +
                 "PlayerDamage.GrantInvulnerability knob (Kill is never gated, so pits are unaffected).")]
        public float iframeSeconds = 0.6f;

        [Tooltip("Damage only: when true, the hit cancels the player's upward momentum and jump buffer (wall hits). Leave false for shoves that must preserve the ballistic arc.")]
        public bool cancelVerticalOnHit = false;

        [Header("Kill plane (consequence = KillAndRespawn)")]
        [Tooltip("World Y at or below which the player dies (S4 kill-plane). Ignored by Damage/ReturnToEdge.")]
        public float killY = -20f;

        [Header("Telegraph")]
        [Tooltip("None = the read is spatial (pits). Visible = the hazard is authored as visible " +
                 "geometry. Wall v0.1 = Visible and STATIC (it never moves or pops out).")]
        public HazardTelegraph telegraph = HazardTelegraph.Visible;
    }
}
