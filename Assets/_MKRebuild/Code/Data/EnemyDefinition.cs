using UnityEngine;

namespace MK.Rebuild
{
    /// <summary>
    /// How an archetype commits its attack: contact melee, a telegraphed line
    /// charge, a ranged projectile, or a backstab from outside the player's
    /// frontal cone.
    /// </summary>
    public enum EnemyAttackKind
    {
        Melee = 0,
        Charge = 1,
        Ranged = 2,
        Backstab = 3
    }

    /// <summary>
    /// M3 enemy data: one ScriptableObject per archetype. Every gameplay number
    /// lives here — the controllers read the definition and never hardcode
    /// per-enemy values. <see cref="id"/> is a stable, rename-ready identifier
    /// (not a display name and never a proper name).
    ///
    /// First archetype: <c>enemy_esbirro</c> (grey-box). Future archetypes add
    /// their own asset, not a branch in the controller.
    /// </summary>
    [CreateAssetMenu(fileName = "EnemyDefinition", menuName = "MK/Rebuild/EnemyDefinition")]
    public sealed class EnemyDefinition : ScriptableObject
    {
        [Tooltip("Stable data id (e.g. 'enemy_esbirro'). Rename-ready: logic never switches on display names.")]
        public string id = "enemy_esbirro";

        [Tooltip("Max HP. Damage reaching 0 kills the enemy (grey-box: instant events, delayed scale death).")]
        public int maxHp = 20;

        [Tooltip("Horizontal steering speed in m/s while chasing (no NavMesh, Rigidbody steering).")]
        public float moveSpeed = 4.5f;

        [Tooltip("Flat distance at which an idle enemy notices the player and starts chasing.")]
        public float aggroRadius = 12f;

        [Tooltip("Facing turn rate in degrees per second (Quaternion.RotateTowards).")]
        public float turnSpeed = 720f;

        [Tooltip("Flat distance at which the enemy stops and starts an attack (also the hit reach).")]
        public float attackRange = 1.8f;

        [Tooltip("Damage applied to PlayerDamage on a landed attack.")]
        public int attackDamage = 8;

        [Tooltip("Wind-up before the hit lands, in seconds (the tell).")]
        public float attackTell = 0.2f;

        [Tooltip("Recovery after the hit lands, in seconds, before the token is released.")]
        public float attackRecovery = 0.4f;

        [Tooltip("Seconds before this enemy may attack again after recovering.")]
        public float attackCooldown = 1.2f;

        [Tooltip("Damage at or above this value staggers (interrupts) the enemy. 1 = every hit.")]
        public int poiseThreshold = 1;

        [Tooltip("Stagger duration in seconds. The enemy does not steer while staggered.")]
        public float staggerDuration = 0.2f;

        [Tooltip("Delay in UNSCALED seconds between death and the visual/destroy (scale-down).")]
        public float deathDelay = 0.35f;

        [Header("Attack kind")]
        [Tooltip("Melee = contact attack at attackRange. Charge = telegraphed line charge.")]
        public EnemyAttackKind attackKind = EnemyAttackKind.Melee;

        [Header("Charge (attackKind = Charge)")]
        [Tooltip("Flat distance at which a token-holding charger commits to the line charge.")]
        public float chargeStartRange = 8f;

        [Tooltip("Horizontal speed while charging, in m/s.")]
        public float chargeSpeed = 10f;

        [Tooltip("Distance travelled from the launch point before the charge stops.")]
        public float chargeDistance = 6f;

        [Tooltip("Extra travel past chargeDistance before stopping (overshoot).")]
        public float chargeOvershoot = 2f;

        [Tooltip("Flat distance at which the charge lands its single hit.")]
        public float chargeHitRadius = 1f;

        [Header("Ranged (attackKind = Ranged)")]
        [Tooltip("Ideal distance the shooter tries to keep from the player.")]
        public float preferredDistance = 4f;

        [Tooltip("Inside this flat distance the shooter backs away.")]
        public float rangedMinDistance = 3f;

        [Tooltip("Beyond this flat distance the shooter closes in.")]
        public float rangedMaxDistance = 6f;

        [Tooltip("Projectile travel speed in m/s.")]
        public float projectileSpeed = 5f;

        [Tooltip("Projectile lifetime in seconds before it despawns.")]
        public float projectileLifetime = 3f;

        [Tooltip("Projectile hit radius in meters (OverlapSphere).")]
        public float projectileRadius = 0.25f;

        [Tooltip("Maximum live projectiles this archetype may have in flight.")]
        public int maxProjectilesInFlight = 3;

        [Header("Backstab (attackKind = Backstab)")]
        [Tooltip("Half-angle (degrees) of the player's frontal cone the backstabber avoids.")]
        public float backstabCone = 60f;

        [Tooltip("Distance behind the player the backstabber stalks.")]
        public float stalkDistance = 2.5f;

        [Tooltip("Seconds spent fleeing after a backstab attempt.")]
        public float fleeDuration = 2f;
    }
}
