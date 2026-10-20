using System.Collections;
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

        private Rigidbody rb;
        private HitReceiver receiver;
        private Collider bodyCollider;
        private PlayerDamage player;
        private MusicStateController music;

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

        /// <summary>Phase-2 bar fill [0..1]; 0 while still in phase 1.</summary>
        public float Phase2Normalized =>
            definition != null && phase == 2
                ? Mathf.Clamp01((float)HP / Mathf.Max(1, definition.phaseHp))
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

        private void OnDisable()
        {
            Unsubscribe();
            StopAllCoroutines();
            NotifyCombatEndOnce();
        }

        private void OnDestroy()
        {
            Unsubscribe();
            StopAllCoroutines();
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
            }

            if (attackTimer >= (definition.slamTell + definition.slamRecovery) / mult)
            {
                cooldownTimer = definition.attackCooldown / mult;
                state = State.Approach;
            }
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
            StartCoroutine(RoarRoutine());
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

            StopAllCoroutines();

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
            state = State.Approach;

            if (visualRoot != null) visualRoot.localScale = originalScale;
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
