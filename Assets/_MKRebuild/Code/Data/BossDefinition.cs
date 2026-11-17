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

        [Tooltip("How many phases the fight has. The killing blow of every phase but the last restores a full phase of HP and plays the roar.")]
        public int phases = 2;

        [Tooltip("Phase-3 speed multiplier. Only used when phases >= 3.")]
        public float phase3SpeedMultiplier = 1.45f;

        [Tooltip("Roar/pause duration at the phase transition, in UNSCALED seconds.")]
        public float roarDuration = 1.2f;

        [Tooltip("Delay in UNSCALED seconds between death and the visual/destroy (scale-down).")]
        public float deathDelay = 1.5f;

        [Header("Phase 2 Shockwave")]
        [Tooltip("Esbirros summoned at the phase-1 -> phase-2 transition.")]
        public int minionsToSummon = 2;

        [Tooltip("Damage applied to PlayerDamage by a passing shockwave.")]
        public int shockwaveDamage = 20;

        [Tooltip("Maximum flat radius the shockwave expands to, in metres.")]
        public float shockwaveRadius = 4f;

        [Tooltip("Expansion duration in SCALED seconds at phase-1 speed (divided by the phase-2 multiplier).")]
        public float shockwaveDuration = 0.6f;

        [Tooltip("Thickness of the expanding hit band around the wave front.")]
        public float shockwaveHitRadius = 0.6f;

        // --- Executioner (change executioner-boss) -------------------------------
        // El cerebro por defecto (Orochi) ignora estos campos; el Executioner los exige.
        // El validador avisa si los campos viejos de arriba divergen de E01/E02.

        [Header("Executioner · catálogo")]
        [Tooltip("Ataques del boss. El controller no conoce ningún id: todo sale de acá.")]
        public BossAttackDefinition[] attacks = System.Array.Empty<BossAttackDefinition>();

        [Tooltip("Perfil por fase (índice 0 = fase 1).")]
        public BossPhaseProfile[] phaseProfiles = System.Array.Empty<BossPhaseProfile>();

        [Header("Executioner · iniciativa")]
        [Tooltip("Duración de la postura defensiva, en segundos.")]
        public float guardDuration = 1.2f;

        [Tooltip("Multiplicador de daño recibido mientras guarda. Debe estar en (0,1).")]
        [Range(0f, 1f)] public float guardDamageMultiplier = 0.55f;

        [Tooltip("Multiplicador de daño recibido durante Recover/Punish (1 = sin reducción).")]
        [Range(0f, 1f)] public float punishErrorMultiplier = 1f;

        [Tooltip("Segundos de castigo que abre un parry.")]
        public float parryExposure = 1f;

        [Tooltip("Segundos de castigo que abre un desarme.")]
        public float disarmExposure = 1.5f;

        [Tooltip("Segundos de castigo que abre un rugido.")]
        public float roarExposure = 1f;

        [Tooltip("Piso de legibilidad: ningún ataque puede tener un tell menor, en segundos.")]
        public float minTell = 0.45f;

        [Tooltip("Máximo de veces seguidas que el mismo ataque puede repetirse.")]
        public int maxStreak = 2;

        [Tooltip("Velocidad de caminata hacia un mecanismo, en m/s.")]
        public float mechanismWalkSpeed = 1.2f;

        [Tooltip("Distancia a la que se considera que el boss llegó al mecanismo, en metros.")]
        public float mechanismArrivalRadius = 1.2f;

        [Tooltip("Segundos máximos caminando a un mecanismo antes de abandonar.")]
        public float walkTimeout = 6f;

        [Tooltip("Segundos máximos en CloseIn antes de volver a la ventana del jugador.")]
        public float closeInTimeout = 5f;

        [Tooltip("Segundos entre liberar la celda y que los prisioneros se vuelvan hostiles.")]
        public float prisonerRallyDelay = 1.5f;

        [Tooltip("Prefab de prisionero. Null = no se liberan prisioneros.")]
        public EnemyController prisonerPrefab;

        [Tooltip("Prisioneros por celda.")]
        public int prisonerCount = 3;
    }
}
