using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MK.Rebuild
{
    /// <summary>
    /// M3.4 grey-box boss: a slow brute with a two-phase health bar.
    ///
    /// Phase 1 (400 HP) alternates a telegraphed SLAM and a line CHARGE, both with
    /// long punish windows. The killing blow is intercepted by <see cref="HitReceiver.LethalHit"/>:
    /// the boss roars (invulnerable, unscaled pause), restores a full phase of HP
    /// and enters PHASE 2, which repeats the same kit 25% faster. The phase-2 death
    /// flows through the normal <see cref="HitReceiver.OnDied"/> seam.
    ///
    /// Rigidbody steering only — NO NavMesh, NO animation, NO root motion. A leash
    /// around the spawn point keeps the brute inside the arena; a charge stops on
    /// solid geometry/props but ignores the player and other enemies.
    ///
    /// Player death resets only the CURRENT phase HP and position
    /// (<see cref="ResetForRespawn"/>), never the whole fight.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody), typeof(HitReceiver))]
    public sealed class BossController : MonoBehaviour
    {
        private enum State { Approach, Slam, Charge, Recovery, Roar, Dead }

        [Tooltip("Boss data. All gameplay numbers come from here — never hardcoded.")]
        [SerializeField] private BossDefinition definition;

        [Tooltip("Transform scaled by the death routine. Defaults to this transform.")]
        [SerializeField] private Transform visualRoot;

        [Tooltip("Grey-box Esbirro spawned at the phase transition (M3.5a).")]
        [SerializeField] private EnemyController minionPrefab;

        private readonly List<EnemyController> minions = new();

        private Rigidbody rb;
        private HitReceiver receiver;
        private Collider bodyCollider;
        private PlayerDamage player;
        private PlayerMotor playerMotor;
        private MusicStateController music;
        private bool shockwaveSpawned;
        private GameObject shockwaveRoot;

        private State state = State.Approach;
        private int phase = 1;
        private float cooldownTimer;
        private float attackTimer;
        private float recoveryTimer;
        private bool slamHitApplied;
        private bool chargeLaunched;
        private bool chargeHitApplied;
        private Vector3 chargeStart;
        private Vector3 chargeDir;
        private Vector3 leashCenter;
        private Vector3 originalScale;
        private bool combatEndNotified;
        private bool warnedMissingDefinition;

        /// <summary>Current fight phase: 1 until the first lethal hit, then 2.</summary>
        public int Phase => phase;

        /// <summary>Current phase HP (read-only debug seam).</summary>
        public int HP => receiver != null ? receiver.HP : 0;

        /// <summary>Max HP of the current phase (read-only debug seam).</summary>
        public int MaxHp => receiver != null ? receiver.MaxHp : 0;

        /// <summary>Phase-1 bar fill [0..1]; 0 once phase 2 begins.</summary>
        public float Phase1Normalized =>
            definition != null && phase == 1
                ? Mathf.Clamp01((float)HP / Mathf.Max(1, definition.phaseHp))
                : 0f;

        /// <summary>Phase-2 bar fill [0..1]; full while still in phase 1, then drains.</summary>
        public float Phase2Normalized =>
            definition != null
                ? (phase == 1
                    ? 1f
                    : phase == 2
                        ? Mathf.Clamp01((float)HP / Mathf.Max(1, definition.phaseHp))
                        : 0f)
                : 0f;

        /// <summary>True after the final (phase-2) death blow.</summary>
        public bool IsDead { get; private set; }

        private void Awake()
        {
            if (visualRoot == null) visualRoot = transform;
            rb = GetComponent<Rigidbody>();
            receiver = GetComponent<HitReceiver>();
            bodyCollider = GetComponent<Collider>();

            if (definition != null) receiver.Configure(definition.phaseHp);

            receiver.LethalHit += OnLethalHit;
            receiver.OnDied += OnDied;

            if (rb != null) rb.freezeRotation = true;
            leashCenter = transform.position;
            originalScale = visualRoot != null ? visualRoot.localScale : transform.localScale;
        }

        private void Start()
        {
            playerMotor = FindFirstObjectByType<PlayerMotor>();
            BossArena.Instance?.Bind(this);
            BossHealthBar.Instance?.Bind(this);
        }

        private void OnDisable()
        {
            Unsubscribe();
            StopAllCoroutines();
            DestroyShockwave();
            BossHealthBar.Instance?.Hide();
            BossArena.Instance?.Open();
            ClearMinions();
            NotifyCombatEndOnce();
        }

        private void OnDestroy()
        {
            Unsubscribe();
            StopAllCoroutines();
            DestroyShockwave();
            BossHealthBar.Instance?.Hide();
            BossArena.Instance?.Open();
            ClearMinions();
            NotifyCombatEndOnce();
        }

        private void Unsubscribe()
        {
            if (receiver == null) return;
            receiver.LethalHit -= OnLethalHit;
            receiver.OnDied -= OnDied;
        }

        private void NotifyCombatEndOnce()
        {
            if (combatEndNotified) return;
            combatEndNotified = true;
            if (music != null) music.NotifyCombatEnd();
        }

        private void FixedUpdate()
        {
            if (definition == null)
            {
                if (!warnedMissingDefinition)
                {
                    warnedMissingDefinition = true;
                    Debug.LogWarning("BossController: no BossDefinition assigned — controller inert.", this);
                }
                return;
            }

            ResolveRefs();

            float dt = Time.fixedDeltaTime;
            if (cooldownTimer > 0f) cooldownTimer -= dt;

            if (player == null || !player.IsAlive)
            {
                StopHorizontal();
                return;
            }

            float mult = phase == 2 ? definition.phase2SpeedMultiplier : 1f;

            // Leash: the brute refuses to leave its arena. Roar/Dead are exempt so
            // the transition and the death visual are never interrupted.
            if (state != State.Roar && state != State.Dead)
            {
                Vector3 toCenter = FlatDirectionTo(leashCenter);
                if (FlatDistanceTo(leashCenter) > definition.leashRadius &&
                    toCenter.sqrMagnitude > 0.000001f)
                {
                    state = State.Approach;
                    Face(toCenter, dt);
                    Steer(toCenter, definition.moveSpeed * mult);
                    return;
                }
            }

            switch (state)
            {
                case State.Approach:
                    TickApproach(dt, mult);
                    break;

                case State.Slam:
                    TickSlam(dt, mult);
                    break;

                case State.Charge:
                    TickCharge(dt, mult);
                    break;

                case State.Recovery:
                    TickRecovery(dt);
                    break;

                case State.Roar:
                    StopHorizontal();
                    break;

                case State.Dead:
                    if (rb != null && !rb.isKinematic) StopHorizontal();
                    break;
            }
        }

        private void ResolveRefs()
        {
            if (player == null) player = FindFirstObjectByType<PlayerDamage>();
            if (playerMotor == null) playerMotor = FindFirstObjectByType<PlayerMotor>();
            if (music == null) music = FindFirstObjectByType<MusicStateController>();
        }

        // --- States ----------------------------------------------------------

        private void TickApproach(float dt, float mult)
        {
            Vector3 playerPosition = player.transform.position;
            Vector3 toPlayer = FlatDirectionTo(playerPosition);
            Face(toPlayer, dt);

            float distance = FlatDistanceTo(playerPosition);

            if (cooldownTimer <= 0f && distance <= definition.slamRadius)
            {
                EnterSlam();
                return;
            }

            if (cooldownTimer <= 0f && distance <= definition.chargeStartRange)
            {
                EnterCharge();
                return;
            }

            Steer(toPlayer, definition.moveSpeed * mult);
        }

        private void EnterSlam()
        {
            state = State.Slam;
            attackTimer = 0f;
            slamHitApplied = false;
            shockwaveSpawned = false;
            StopHorizontal();
            Face(FlatDirectionTo(player.transform.position), Time.fixedDeltaTime);
        }

        private void TickSlam(float dt, float mult)
        {
            StopHorizontal();
            Face(FlatDirectionTo(player.transform.position), dt);

            attackTimer += dt;

            if (!slamHitApplied && attackTimer >= definition.slamTell / mult)
            {
                slamHitApplied = true;
                Vector3 toPlayer = FlatDirectionTo(player.transform.position);
                if (FlatDistanceTo(player.transform.position) <= definition.slamRadius)
                {
                    player.TakeHit(definition.slamDamage, toPlayer);
                }

                // M3.5a: phase-2 slams also release a jumpable shockwave.
                if (phase == 2 && !shockwaveSpawned)
                {
                    shockwaveSpawned = true;
                    StartCoroutine(ShockwaveRoutine());
                }
            }

            if (attackTimer >= (definition.slamTell + definition.slamRecovery) / mult)
            {
                cooldownTimer = definition.attackCooldown / mult;
                state = State.Approach;
            }
        }

        /// <summary>
        /// M3.5a phase-2 slam shockwave: a thin grey-box ring expands from the boss on
        /// SCALED time. It only hits a GROUNDED player the first time the expanding
        /// front passes over them, so a jump clears it. Purely visual + one proximity
        /// check — no physics collider, no NavMesh, no animation.
        /// </summary>
        private IEnumerator ShockwaveRoutine()
        {
            var root = new GameObject("BossShockwave");
            shockwaveRoot = root;
            root.transform.position = transform.position;

            var ring = GreyBoxFx.CreateRing(root.transform, 0.5f);
            if (ring == null)
            {
                Destroy(root);
                shockwaveRoot = null;
                yield break;
            }

            float mult = phase == 2 ? definition.phase2SpeedMultiplier : 1f;
            float duration = Mathf.Max(0.0001f, definition.shockwaveDuration / mult);
            float radius = definition.shockwaveRadius;
            float hitBand = definition.shockwaveHitRadius;
            float elapsed = 0f;
            bool hitApplied = false;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float current = Mathf.Lerp(0.5f, radius, t);
                GreyBoxFx.ApplyRingSize(ring.transform, current);

                if (!hitApplied && playerMotor != null && playerMotor.IsGrounded)
                {
                    float distance = FlatDistanceTo(playerMotor.transform.position);
                    if (distance <= current + hitBand && distance >= current - 0.8f)
                    {
                        hitApplied = true;
                        if (player != null)
                        {
                            player.TakeHit(
                                definition.shockwaveDamage,
                                FlatDirectionTo(playerMotor.transform.position));
                        }
                    }
                }

                yield return null;
            }

            Destroy(root);
            if (shockwaveRoot == root) shockwaveRoot = null;
        }

        private void DestroyShockwave()
        {
            if (shockwaveRoot != null) Destroy(shockwaveRoot);
            shockwaveRoot = null;
        }

        private void EnterCharge()
        {
            state = State.Charge;
            attackTimer = 0f;
            chargeLaunched = false;
            chargeHitApplied = false;
            chargeStart = transform.position;
            chargeDir = FlatDirectionTo(player.transform.position);
            if (chargeDir.sqrMagnitude < 0.000001f) chargeDir = transform.forward;
            StopHorizontal();
        }

        private void TickCharge(float dt, float mult)
        {
            attackTimer += dt;

            if (!chargeLaunched)
            {
                StopHorizontal();
                Face(FlatDirectionTo(player.transform.position), dt);

                if (attackTimer >= definition.chargeTell / mult)
                {
                    chargeDir = FlatDirectionTo(player.transform.position);
                    if (chargeDir.sqrMagnitude < 0.000001f) chargeDir = transform.forward;
                    chargeStart = transform.position;
                    chargeLaunched = true;
                }

                return;
            }

            Vector3 velocity = rb.linearVelocity;
            velocity.x = chargeDir.x * definition.chargeSpeed * mult;
            velocity.z = chargeDir.z * definition.chargeSpeed * mult;
            rb.linearVelocity = velocity;
            Face(chargeDir, dt);

            if (!chargeHitApplied &&
                FlatDistanceTo(player.transform.position) <= definition.chargeHitRadius)
            {
                chargeHitApplied = true;
                player.TakeHit(definition.chargeDamage, chargeDir);
            }

            float travelled = Vector3.Distance(chargeStart, transform.position);
            if (travelled >= definition.chargeDistance + definition.chargeOvershoot ||
                ChargeBlocked())
            {
                StopHorizontal();
                cooldownTimer = definition.attackCooldown / mult;
                recoveryTimer = definition.chargeRecovery / mult;
                state = State.Recovery;
            }
        }

        /// <summary>
        /// A charge is stopped by solid geometry/props ahead. The boss itself, the
        /// player and other enemies (allies) are ignored, so a body never cancels
        /// the commitment; rails/walls do.
        /// </summary>
        private bool ChargeBlocked()
        {
            if (rb == null) return false;

            RaycastHit[] hits = Physics.SphereCastAll(
                transform.position + Vector3.up * 1.5f,
                0.7f,
                chargeDir,
                0.8f,
                ~0,
                QueryTriggerInteraction.Ignore);

            for (int i = 0; i < hits.Length; i++)
            {
                var hit = hits[i];
                if (hit.collider == null) continue;
                if (hit.rigidbody == rb) continue;
                if (hit.collider.GetComponentInParent<PlayerDamage>() != null) continue;
                if (hit.collider.GetComponentInParent<EnemyController>() != null) continue;
                if (hit.collider.GetComponentInParent<BossController>() != null) continue;
                return true;
            }

            return false;
        }

        private void TickRecovery(float dt)
        {
            StopHorizontal();
            recoveryTimer -= dt;
            if (recoveryTimer <= 0f) state = State.Approach;
        }

        // --- Phase transition / death ---------------------------------------

        /// <summary>
        /// Intercepts the phase-1 killing blow: restore a full phase of HP, become
        /// invulnerable and roar (unscaled pause). The final phase lets the normal
        /// death path run.
        /// </summary>
        private void OnLethalHit(HitReceiver hitReceiver)
        {
            if (IsDead || definition == null) return;
            if (phase != 1) return;

            phase = 2;
            receiver.Configure(definition.phaseHp);
            receiver.Invulnerable = true;
            StopHorizontal();
            state = State.Roar;

            // M3.5a: the exit is revealed (visual only; the collider stays) and the
            // brute calls two Esbirros while the roar plays.
            BossArena.Instance?.BreakExitWall();
            BossHealthBar.Instance?.Bind(this);
            SummonMinions();

            StartCoroutine(RoarRoutine());
        }

        /// <summary>
        /// M3.5a: spawns <see cref="BossDefinition.minionsToSummon"/> Esbirros 2 m
        /// around the boss on opposite sides. No-op without a prefab.
        /// </summary>
        private void SummonMinions()
        {
            if (minionPrefab == null || definition == null) return;

            int count = Mathf.Max(0, definition.minionsToSummon);
            for (int i = 0; i < count; i++)
            {
                float angle = count > 0 ? (Mathf.PI * 2f * i) / count : 0f;
                Vector3 offset = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * 2f;
                Vector3 position = transform.position + offset;
                Vector3 facing = offset.sqrMagnitude > 0.000001f ? -offset : transform.forward;

                EnemyController minion = Instantiate(
                    minionPrefab, position, Quaternion.LookRotation(facing, Vector3.up));
                if (minion != null) minions.Add(minion);
            }
        }

        /// <summary>M3.5a: destroys every boss-summoned minion and clears the list.</summary>
        private void ClearMinions()
        {
            for (int i = 0; i < minions.Count; i++)
            {
                if (minions[i] != null) Destroy(minions[i].gameObject);
            }
            minions.Clear();
        }

        private IEnumerator RoarRoutine()
        {
            float duration = definition != null ? Mathf.Max(0f, definition.roarDuration) : 0f;
            yield return new WaitForSecondsRealtime(duration);

            if (IsDead) yield break;

            receiver.Invulnerable = false;
            cooldownTimer = 0f;
            state = State.Approach;

            if (music != null) music.NotifyCombat();
        }

        private void OnDied()
        {
            if (IsDead) return;
            IsDead = true;
            state = State.Dead;

            BossHealthBar.Instance?.Hide();
            BossArena.Instance?.Open();
            ClearMinions();

            StopHorizontal();
            if (bodyCollider != null) bodyCollider.enabled = false;
            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.isKinematic = true;
            }

            NotifyCombatEndOnce();
            StartCoroutine(DeathRoutine());
        }

        /// <summary>
        /// Grey-box death: scales the visual to 0.05 over <c>deathDelay</c> on
        /// UNSCALED time, then destroys the object. No animation, no ragdoll.
        /// </summary>
        private IEnumerator DeathRoutine()
        {
            Transform target = visualRoot != null ? visualRoot : transform;
            Vector3 startScale = target.localScale;
            Vector3 endScale = Vector3.one * 0.05f;
            float duration = definition != null ? Mathf.Max(0f, definition.deathDelay) : 1.5f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = duration > 0f ? Mathf.Clamp01(elapsed / duration) : 1f;
                target.localScale = Vector3.Lerp(startScale, endScale, t);
                yield return null;
            }

            target.localScale = endScale;
            Destroy(gameObject);
        }

        /// <summary>
        /// Player-death reset: restore only the CURRENT phase HP and the boss
        /// position. The phase (and therefore the fight structure) is preserved.
        /// A fully dead boss stays dead.
        /// </summary>
        public void ResetForRespawn(Vector3 spawnPosition)
        {
            if (IsDead) return;

            BossArena.Instance?.Open();
            ClearMinions();

            StopAllCoroutines();
            DestroyShockwave();

            transform.position = spawnPosition;
            leashCenter = spawnPosition;

            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.isKinematic = false;
            }

            if (receiver != null)
            {
                receiver.Configure(definition != null ? definition.phaseHp : receiver.MaxHp);
                receiver.Invulnerable = false;
            }

            cooldownTimer = 0f;
            attackTimer = 0f;
            recoveryTimer = 0f;
            chargeLaunched = false;
            chargeHitApplied = false;
            slamHitApplied = false;
            shockwaveSpawned = false;
            state = State.Approach;

            if (visualRoot != null) visualRoot.localScale = originalScale;

            BossHealthBar.Instance?.Bind(this);
        }

        // --- Movement helpers ------------------------------------------------

        private void Face(Vector3 flatDirection, float dt)
        {
            if (flatDirection.sqrMagnitude < 0.000001f) return;
            Quaternion target = Quaternion.LookRotation(flatDirection, Vector3.up);
            rb.MoveRotation(Quaternion.RotateTowards(rb.rotation, target, definition.turnSpeed * dt));
        }

        private void Steer(Vector3 direction, float speed)
        {
            Vector3 velocity = rb.linearVelocity;
            velocity.x = direction.x * speed;
            velocity.z = direction.z * speed;
            rb.linearVelocity = velocity;
        }

        private void StopHorizontal()
        {
            if (rb == null || rb.isKinematic) return;
            Vector3 velocity = rb.linearVelocity;
            velocity.x = 0f;
            velocity.z = 0f;
            rb.linearVelocity = velocity;
        }

        private Vector3 FlatDirectionTo(Vector3 worldPosition)
        {
            Vector3 delta = worldPosition - transform.position;
            delta.y = 0f;
            return delta.sqrMagnitude > 0.000001f ? delta.normalized : Vector3.zero;
        }

        private float FlatDistanceTo(Vector3 worldPosition)
        {
            Vector3 delta = worldPosition - transform.position;
            delta.y = 0f;
            return delta.magnitude;
        }
    }
}
