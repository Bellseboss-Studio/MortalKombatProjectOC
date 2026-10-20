using UnityEngine;

namespace MK.Rebuild
{
    /// <summary>
    /// M3.4 boss data: one ScriptableObject per boss. Every gameplay number lives
    /// here — <see cref="BossController"/> reads the definition and never hardcodes
    /// per-boss values. <see cref="id"/> is a stable, rename-ready identifier.
    ///
    /// The fight is two phases of <see cref="phaseHp"/> HP each (400 + 400 for the
    /// brute). Phase 2 repeats the same slam/charge kit at
    /// <see cref="phase2SpeedMultiplier"/> (25% faster tells, movement, cooldowns).
    /// </summary>
    [CreateAssetMenu(fileName = "BossDefinition", menuName = "MK/Rebuild/BossDefinition")]
    public sealed class BossDefinition : ScriptableObject
    {
        [Tooltip("Stable data id (e.g. 'boss_brute'). Rename-ready: logic never switches on display names.")]
        public string id = "boss_brute";

        [Tooltip("HP per phase. The boss restores this value at the phase-1 -> phase-2 transition.")]
        public int phaseHp = 400;

        [Tooltip("Horizontal steering speed in m/s while approaching (no NavMesh, Rigidbody steering).")]
        public float moveSpeed = 2.5f;

        [Tooltip("Flat distance at which the boss notices the player (reserved for the boss bar/encounter gate).")]
        public float aggroRadius = 30f;

        [Tooltip("Facing turn rate in degrees per second (Quaternion.RotateTowards).")]
        public float turnSpeed = 180f;

        [Tooltip("Flat distance from the spawn/leash center past which the boss disengages and walks back.")]
        public float leashRadius = 10f;

        [Header("Slam")]
        [Tooltip("Wind-up before the slam lands, in seconds (phase 1; divided by the phase-2 multiplier).")]
        public float slamTell = 0.8f;

        [Tooltip("Punish window after the slam lands, in seconds (phase 1).")]
        public float slamRecovery = 2f;

        [Tooltip("Flat radius at which the slam lands.")]
        public float slamRadius = 2.8f;

        [Tooltip("Damage applied to PlayerDamage by a landed slam.")]
        public int slamDamage = 25;

        [Header("Charge")]
        [Tooltip("Wind-up before the charge commits, in seconds (phase 1).")]
        public float chargeTell = 0.8f;

        [Tooltip("Punish window after the charge stops, in seconds (phase 1).")]
        public float chargeRecovery = 2f;

        [Tooltip("Horizontal speed while charging, in m/s.")]
        public float chargeSpeed = 8f;

        [Tooltip("Distance travelled from the launch point before the charge stops.")]
        public float chargeDistance = 8f;

        [Tooltip("Extra travel past chargeDistance before stopping (overshoot).")]
        public float chargeOvershoot = 2f;

        [Tooltip("Flat distance at which the charge lands its single hit.")]
        public float chargeHitRadius = 1.2f;

        [Tooltip("Damage applied to PlayerDamage by a landed charge.")]
        public int chargeDamage = 15;

        [Tooltip("Flat distance at which a ready boss commits to a charge.")]
        public float chargeStartRange = 8f;

        [Header("Timing")]
        [Tooltip("Seconds before the boss may attack again after recovering (phase 1).")]
        public float attackCooldown = 2.5f;

        [Tooltip("Phase-2 speed multiplier applied to tells, recovery, cooldown, move and charge speed.")]
        public float phase2SpeedMultiplier = 1.25f;

        [Tooltip("Roar/pause duration at the phase transition, in UNSCALED seconds.")]
        public float roarDuration = 1.2f;

        [Tooltip("Delay in UNSCALED seconds between death and the visual/destroy (scale-down).")]
        public float deathDelay = 1.5f;
    }
}
