using UnityEngine;

namespace MK.Rebuild
{
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
    }
}
