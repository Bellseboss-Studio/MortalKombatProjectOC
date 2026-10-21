using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MK.Rebuild
{
    /// <summary>
    /// M3 grey-box enemy AI: Idle → Chase → (Attack | Charge → Recovery |
    /// RangedAttack | Backstab → Flee) → Stagger, plus terminal Dead. Rigidbody
    /// steering only — NO NavMesh, NO root motion and NO animation dependency.
    /// Every number comes from the assigned <see cref="EnemyDefinition"/>.
    ///
    /// The <see cref="CombatCoordinator"/> gates both aggression and space:
    /// attack tokens cap how many enemies press at once, and ring slots give the
    /// non-attackers distinct angles around the player so a crowd circles instead
    /// of stacking. Melee archetypes use the attack state; <c>EnemyAttackKind.Charge</c>
    /// archetypes use a telegraphed line charge with an overshoot and a punish
    /// recovery window. Ranged archetypes hold a separate ranged token and fire
    /// projectiles; backstab archetypes stalk outside the player's frontal cone
    /// and flee after striking.
    ///
    /// The hurt/death seams belong to <see cref="HitReceiver"/>: non-lethal damage
    /// fires <c>Damaged</c> (this controller staggers) and the killing blow fires
    /// <c>OnDied</c> (this controller owns the grey-box death visual/destroy).
    /// <c>AnyDied</c> still reaches the style meter, so the +25 kill bonus is
    /// unchanged.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody), typeof(HitReceiver))]
    public sealed class EnemyController : MonoBehaviour
    {
        private const float SlotStopDistance = 0.35f;

        private enum State { Idle, Chase, Attack, Charge, Recovery, RangedAttack, Backstab, Flee, Stagger, Dead }

        [Tooltip("Archetype data. All gameplay numbers come from here — never hardcoded per enemy.")]
        [SerializeField] private EnemyDefinition definition;

        [Tooltip("Legacy grey-box visual root, scaled only when no CharacterVisual is present. Defaults to this transform.")]
        [SerializeField] private Transform visualRoot;

        [Tooltip("X Bot animation facade. Resolved from the children when left empty.")]
        [SerializeField] private CharacterVisual characterVisual;

        [Tooltip("Projectile prefab spawned by Ranged archetypes (Tirador). Optional for other kinds.")]
        [SerializeField] private EnemyProjectile projectilePrefab;

        [Tooltip("Muzzle offset from the enemy root, in local space (Y up, Z forward).")]
        [SerializeField] private Vector3 projectileMuzzleOffset = new Vector3(0f, 1.2f, 0.5f);

        private static readonly List<EnemyController> Active = new();

        private Rigidbody rb;
        private HitReceiver receiver;
        private Collider bodyCollider;
        private PlayerDamage player;
        private AnimatorVisual playerVisual;
        private CombatCoordinator coordinator;
        private MusicStateController music;

        private State state = State.Idle;
        private float cooldownTimer;
        private float attackTimer;
        private float staggerTimer;
        private bool attackHitApplied;
        private bool hasToken;
        private bool hasRangedToken;
        private bool rangedShotApplied;
        private bool backstabHitApplied;
        private bool combatNotified;
        private bool dead;
        private bool warnedMissingDefinition;

        /// <summary>
        /// Debug seam: freezes this enemy's AI without disabling the component, so the
        /// HitReceiver <c>Damaged</c>/<c>OnDied</c> subscriptions stay alive. Set by the
        /// debug overlay only; false in normal play.
        /// </summary>
        public bool Frozen { get; set; }

        // Backstab archetype (EnemyAttackKind.Backstab).
        private float fleeTimer;

        // Charge archetype (EnemyAttackKind.Charge).
        private bool chargeLaunched;
        private bool chargeHitApplied;
        private Vector3 chargeStart;
        private Vector3 chargeDir;
        private float recoveryTimer;

        private void Awake()
        {
            if (visualRoot == null) visualRoot = transform;
            if (characterVisual == null) characterVisual = GetComponentInChildren<CharacterVisual>(true);
            rb = GetComponent<Rigidbody>();
            receiver = GetComponent<HitReceiver>();
            bodyCollider = GetComponent<Collider>();

            if (definition != null)
            {
                receiver.Configure(definition.maxHp);
            }

            receiver.Damaged += OnDamaged;
            receiver.OnDied += OnDied;

            rb.freezeRotation = true;
        }

        private void OnEnable()
        {
            if (!Active.Contains(this)) Active.Add(this);
            if (characterVisual == null) characterVisual = GetComponentInChildren<CharacterVisual>(true);
            if (characterVisual != null) characterVisual.PlaySpawn();
        }

        private void OnDisable()
        {
            Active.Remove(this);
            ReleaseToken();
            ReleaseRangedToken();
            ReleaseRingSlot();
            if (receiver != null)
            {
                receiver.Damaged -= OnDamaged;
                receiver.OnDied -= OnDied;
            }
        }

        private void OnDestroy()
        {
            // OnDisable runs on destroy too, but be explicit for the static registry.
            Active.Remove(this);
            ReleaseToken();
            ReleaseRangedToken();
            ReleaseRingSlot();
            if (receiver != null)
            {
                receiver.Damaged -= OnDamaged;
                receiver.OnDied -= OnDied;
            }
        }

        private void FixedUpdate()
        {
            if (definition == null)
            {
                if (!warnedMissingDefinition)
                {
                    warnedMissingDefinition = true;
                    Debug.LogWarning("EnemyController: no EnemyDefinition assigned — controller inert.", this);
                }
                return;
            }

            ResolveRefs();

            // Debug AI freeze: stop acting without losing event subscriptions. Release
            // any held token so the fight does not soft-lock while frozen.
            if (Frozen)
            {
                ReleaseToken();
                ReleaseRangedToken();
                StopHorizontal();
                return;
            }

            float dt = Time.fixedDeltaTime;
            if (cooldownTimer > 0f) cooldownTimer -= dt;

            // No player / dead player: hold position (physics still owns Y) and
            // never keep an attack token while the player cannot fight back.
            if (player == null || !player.IsAlive)
            {
                ReleaseToken();
                ReleaseRangedToken();
                StopHorizontal();
                return;
            }

            switch (state)
            {
                case State.Idle:
                    if (FlatDistanceTo(player.transform.position) <= definition.aggroRadius) EnterChase();
                    break;

                case State.Chase:
                    TickChase(dt);
                    break;

                case State.Attack:
                    TickAttack(dt);
                    break;

                case State.Charge:
                    TickCharge(dt);
                    break;

                case State.Recovery:
                    TickRecovery(dt);
                    break;

                case State.RangedAttack:
                    TickRangedAttack(dt);
                    break;

                case State.Backstab:
                    TickBackstab(dt);
                    break;

                case State.Flee:
                    TickFlee(dt);
                    break;

                case State.Stagger:
                    TickStagger(dt);
                    break;

                case State.Dead:
                    if (!rb.isKinematic) StopHorizontal();
                    break;
            }
        }

        private void ResolveRefs()
        {
            if (player == null) player = FindFirstObjectByType<PlayerDamage>();
            if (playerVisual == null) playerVisual = FindFirstObjectByType<AnimatorVisual>();
            if (coordinator == null) coordinator = CombatCoordinator.Instance;
            if (music == null) music = FindFirstObjectByType<MusicStateController>();
        }

        // --- States ----------------------------------------------------------

        private void EnterChase()
        {
            state = State.Chase;
            if (characterVisual != null) characterVisual.SetMoving(true, definition.moveSpeed);

            // Ring slots only shape the melee crowd. Ranged and backstab
            // archetypes use their own spacing/stalk behaviour instead.
            bool usesRing = definition.attackKind == EnemyAttackKind.Melee ||
                            definition.attackKind == EnemyAttackKind.Charge;
            if (usesRing && coordinator != null)
            {
                coordinator.AcquireSlot(this, player.transform.position);
            }

            if (combatNotified) return;
            combatNotified = true;
            if (music != null) music.NotifyCombat();
        }

        private void TickChase(float dt)
        {
            if (characterVisual != null) characterVisual.SetMoving(true, definition.moveSpeed);

            Vector3 playerPosition = player.transform.position;
            Vector3 toPlayer = FlatDirectionTo(playerPosition);
            Face(toPlayer, dt);

            float distance = FlatDistanceTo(playerPosition);

            if (definition.attackKind == EnemyAttackKind.Ranged)
            {
                TickRangedChase(dt, toPlayer, distance);
                return;
            }

            if (definition.attackKind == EnemyAttackKind.Backstab)
            {
                TickBackstabChase(dt, toPlayer, distance);
                return;
            }

            if (hasToken)
            {
                // Token holder: press. Melee closes to reach; a charger commits
                // as soon as the player is inside its line-charge start range.
                if (definition.attackKind == EnemyAttackKind.Charge)
                {
                    if (distance <= definition.chargeStartRange)
                    {
                        EnterCharge();
                        return;
                    }
                }
                else if (distance <= definition.attackRange)
                {
                    EnterAttack();
                    return;
                }

                Steer(toPlayer);
                return;
            }

            // Waiting for a turn: keep circling the player on a ring slot.
            if (cooldownTimer <= 0f && TryAcquireToken()) return;

            Vector3 slotPosition = coordinator != null
                ? coordinator.GetSlotPosition(this, playerPosition)
                : playerPosition;

            Vector3 toSlot = FlatDirectionTo(slotPosition);
            if (toSlot.sqrMagnitude < 0.000001f || FlatDistanceTo(slotPosition) <= SlotStopDistance)
            {
                StopHorizontal();
                return;
            }

            Steer(toSlot);
        }

        // --- Ranged archetype (Tirador) --------------------------------------

        /// <summary>
        /// Keeps the preferred band around the player and commits to a shot as
        /// soon as a ranged token, the cooldown and the projectile cap allow it.
        /// Ranged archetypes never use the crowd ring.
        /// </summary>
        private void TickRangedChase(float dt, Vector3 toPlayer, float distance)
        {
            if (!hasRangedToken &&
                cooldownTimer <= 0f &&
                EnemyProjectile.ActiveCount < definition.maxProjectilesInFlight &&
                distance <= definition.attackRange)
            {
                if (TryAcquireRangedToken())
                {
                    EnterRangedAttack();
                    return;
                }
            }

            if (distance < definition.rangedMinDistance)
            {
                Steer(-toPlayer);
                return;
            }

            if (distance > definition.rangedMaxDistance)
            {
                Steer(toPlayer);
                return;
            }

            Vector3 strafe = Vector3.Cross(Vector3.up, toPlayer);
            if (strafe.sqrMagnitude < 0.000001f) strafe = transform.right;
            Steer(strafe.normalized, 0.5f);
        }

        private void EnterRangedAttack()
        {
            state = State.RangedAttack;
            attackTimer = 0f;
            rangedShotApplied = false;
            StopHorizontal();
            Face(FlatDirectionTo(player.transform.position), Time.fixedDeltaTime);
            if (characterVisual != null)
            {
                characterVisual.SetMoving(false, 0f);
                characterVisual.PlayAim();
            }
            if (music != null) music.NotifyCombat();
        }

        private void TickRangedAttack(float dt)
        {
            StopHorizontal();
            Vector3 toPlayer = FlatDirectionTo(player.transform.position);
            Face(toPlayer, dt);

            attackTimer += dt;

            if (!rangedShotApplied && attackTimer >= definition.attackTell)
            {
                rangedShotApplied = true;
                FireProjectile(toPlayer);
            }

            if (attackTimer >= definition.attackTell + definition.attackRecovery)
            {
                ReleaseRangedToken();
                cooldownTimer = definition.attackCooldown;
                state = State.Chase;
            }
        }

        private void FireProjectile(Vector3 toPlayer)
        {
            if (characterVisual != null) characterVisual.PlayShoot();
            if (projectilePrefab == null) return;

            Vector3 direction = toPlayer.sqrMagnitude > 0.000001f ? toPlayer : transform.forward;
            Vector3 origin = transform.position
                + Vector3.up * projectileMuzzleOffset.y
                + transform.forward * projectileMuzzleOffset.z;

            EnemyProjectile projectile = Instantiate(
                projectilePrefab, origin, Quaternion.LookRotation(direction, Vector3.up));
            if (projectile != null)
            {
                projectile.Launch(
                    direction,
                    definition.projectileSpeed,
                    definition.projectileLifetime,
                    definition.projectileRadius,
                    definition.attackDamage);
            }
        }

        // --- Backstab archetype (Puñalada) -----------------------------------

        /// <summary>
        /// Stalks the player just outside their frontal cone, then strikes
        /// once a melee token is held AND the player is not looking at it.
        /// </summary>
        private void TickBackstabChase(float dt, Vector3 toPlayer, float distance)
        {
            bool outside = !InsidePlayerFrontalCone();

            if (hasToken)
            {
                if (outside && distance <= definition.attackRange)
                {
                    EnterBackstab(dt);
                    return;
                }

                SteerToStalkTarget(dt);
                return;
            }

            if (cooldownTimer <= 0f && TryAcquireToken()) return;

            SteerToStalkTarget(dt);
        }

        private void SteerToStalkTarget(float dt)
        {
            // Always keep the player in view while repositioning.
            Face(FlatDirectionTo(player.transform.position), dt);

            Vector3 target = StalkTarget();
            Vector3 toTarget = FlatDirectionTo(target);
            if (toTarget.sqrMagnitude < 0.000001f || FlatDistanceTo(target) <= SlotStopDistance)
            {
                StopHorizontal();
                return;
            }

            Steer(toTarget);
        }

        /// <summary>
        /// Point behind the player's facing, offset sideways when the straight
        /// path there would cut through the player's body.
        /// </summary>
        private Vector3 StalkTarget()
        {
            Vector3 playerPosition = player.transform.position;
            Vector3 facing = PlayerFacing();
            Vector3 target = playerPosition - facing * definition.stalkDistance;

            Vector3 toTarget = target - transform.position;
            toTarget.y = 0f;
            Vector3 toPlayer = playerPosition - transform.position;
            toPlayer.y = 0f;

            if (toPlayer.sqrMagnitude > 0.000001f && toTarget.sqrMagnitude > 0.000001f)
            {
                Vector3 dir = toPlayer.normalized;
                Vector3 side = Vector3.Cross(Vector3.up, dir);
                float along = Vector3.Dot(toTarget, dir);
                float lateral = Vector3.Dot(toTarget, side);

                // The path crosses the player: side-step around them.
                if (along > 0f && Mathf.Abs(lateral) < 1f)
                {
                    target += side * (lateral >= 0f ? 1.2f : -1.2f);
                }
            }

            return target;
        }

        private void EnterBackstab(float dt)
        {
            state = State.Backstab;
            attackTimer = 0f;
            backstabHitApplied = false;
            StopHorizontal();
            Face(FlatDirectionTo(player.transform.position), dt);
            if (characterVisual != null)
            {
                characterVisual.SetMoving(false, 0f);
                characterVisual.PlayStab();
            }
            if (music != null) music.NotifyCombat();
        }

        private void TickBackstab(float dt)
        {
            StopHorizontal();
            Vector3 toPlayer = FlatDirectionTo(player.transform.position);
            Face(toPlayer, dt);

            attackTimer += dt;

            if (!backstabHitApplied && attackTimer >= definition.attackTell)
            {
                backstabHitApplied = true;

                // Landed only if the player has not turned to face the enemy.
                if (!InsidePlayerFrontalCone() &&
                    FlatDistanceTo(player.transform.position) <= definition.attackRange + 0.3f)
                {
                    player.TakeHit(definition.attackDamage, toPlayer);
                }
            }

            if (attackTimer >= definition.attackTell + definition.attackRecovery)
            {
                ReleaseToken();
                cooldownTimer = definition.attackCooldown;
                fleeTimer = definition.fleeDuration;
                state = State.Flee;
            }
        }

        private void TickFlee(float dt)
        {
            if (characterVisual != null) characterVisual.PlayFlee();

            Vector3 away = -FlatDirectionTo(player.transform.position);
            if (away.sqrMagnitude < 0.000001f) away = -transform.forward;

            Face(away, dt);
            Steer(away);

            fleeTimer -= dt;
            if (fleeTimer <= 0f) state = State.Chase;
        }

        private bool InsidePlayerFrontalCone()
        {
            Vector3 facing = PlayerFacing();
            Vector3 toEnemy = transform.position - player.transform.position;
            toEnemy.y = 0f;

            if (toEnemy.sqrMagnitude < 0.000001f) return true;

            return Vector3.Angle(facing, toEnemy) <= definition.backstabCone;
        }

        private Vector3 PlayerFacing()
        {
            Vector3 facing = playerVisual != null ? playerVisual.Facing : player.transform.forward;
            facing.y = 0f;
            return facing.sqrMagnitude > 0.000001f ? facing.normalized : Vector3.forward;
        }

        private void EnterAttack()
        {
            state = State.Attack;
            attackTimer = 0f;
            attackHitApplied = false;
            StopHorizontal();
            if (characterVisual != null)
            {
                characterVisual.SetMoving(false, 0f);
                characterVisual.PlayMelee();
            }
            if (music != null) music.NotifyCombat();
        }

        private void TickAttack(float dt)
        {
            // Stand still and keep facing the player during the tell/recovery.
            StopHorizontal();
            Face(FlatDirectionTo(player.transform.position), dt);

            attackTimer += dt;

            if (!attackHitApplied && attackTimer >= definition.attackTell)
            {
                attackHitApplied = true;
                Vector3 toPlayer = FlatDirectionTo(player.transform.position);
                if (FlatDistanceTo(player.transform.position) <= definition.attackRange + 0.3f)
                {
                    player.TakeHit(definition.attackDamage, toPlayer);
                }
            }

            if (attackTimer >= definition.attackTell + definition.attackRecovery)
            {
                ReleaseToken();
                cooldownTimer = definition.attackCooldown;
                state = State.Chase;
            }
        }

        // --- Charge archetype ------------------------------------------------

        /// <summary>
        /// Telegraphed line charge. Stands still and faces the player during the
        /// tell, then commits to a locked direction for the launch.
        /// </summary>
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
            if (characterVisual != null)
            {
                characterVisual.SetMoving(false, 0f);
                characterVisual.PlayCharge();
            }
            if (music != null) music.NotifyCombat();
        }

        private void TickCharge(float dt)
        {
            attackTimer += dt;

            if (!chargeLaunched)
            {
                StopHorizontal();
                Face(FlatDirectionTo(player.transform.position), dt);

                if (attackTimer >= definition.attackTell)
                {
                    chargeDir = FlatDirectionTo(player.transform.position);
                    if (chargeDir.sqrMagnitude < 0.000001f) chargeDir = transform.forward;
                    chargeStart = transform.position;
                    chargeLaunched = true;
                }

                return;
            }

            Vector3 velocity = rb.linearVelocity;
            velocity.x = chargeDir.x * definition.chargeSpeed;
            velocity.z = chargeDir.z * definition.chargeSpeed;
            rb.linearVelocity = velocity;
            Face(chargeDir, dt);

            if (!chargeHitApplied &&
                FlatDistanceTo(player.transform.position) <= definition.chargeHitRadius)
            {
                chargeHitApplied = true;
                player.TakeHit(definition.attackDamage, chargeDir);
            }

            float travelled = Vector3.Distance(chargeStart, transform.position);
            if (travelled >= definition.chargeDistance + definition.chargeOvershoot || ChargeBlocked())
            {
                EnterRecovery();
            }
        }

        /// <summary>
        /// A charge is blocked by solid geometry/props ahead. Allies and the player
        /// are explicitly ignored: the crowd must never cancel the charger's own
        /// commitment (the player is hit by proximity, not by the cast).
        /// </summary>
        private bool ChargeBlocked()
        {
            if (rb == null) return false;

            RaycastHit[] hits = Physics.SphereCastAll(
                transform.position + Vector3.up,
                0.4f,
                chargeDir,
                0.6f,
                Physics.AllLayers,
                QueryTriggerInteraction.Ignore);

            for (int i = 0; i < hits.Length; i++)
            {
                var hit = hits[i];
                if (hit.collider == null) continue;
                if (hit.rigidbody == rb) continue;
                if (hit.collider.GetComponentInParent<PlayerDamage>() != null) continue;
                if (hit.collider.GetComponentInParent<EnemyController>() != null) continue;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Long punish window after a charge: the token is gone, the enemy stands
        /// still, and the cooldown starts counting so it cannot immediately re-charge.
        /// </summary>
        private void EnterRecovery()
        {
            StopHorizontal();
            if (characterVisual != null) characterVisual.SetMoving(false, 0f);
            ReleaseToken();
            cooldownTimer = definition.attackCooldown;
            recoveryTimer = definition.attackRecovery;
            state = State.Recovery;
        }

        private void TickRecovery(float dt)
        {
            StopHorizontal();
            recoveryTimer -= dt;
            if (recoveryTimer <= 0f) state = State.Chase;
        }

        private void TickStagger(float dt)
        {
            // Deliberately does NOT write horizontal velocity: knockback/steering
            // momentum plays out while staggering.
            if (characterVisual != null) characterVisual.SetMoving(false, 0f);
            staggerTimer -= dt;
            if (staggerTimer <= 0f) state = State.Chase;
        }

        // --- HitReceiver seams ----------------------------------------------

        private void OnDamaged(int damage, Vector3 dir, float launchImpulse)
        {
            if (dead || definition == null) return;
            if (characterVisual != null) characterVisual.PlayHit();
            if (damage >= definition.poiseThreshold) EnterStagger();
        }

        private void EnterStagger()
        {
            // Interrupts Attack/Charge/Recovery/RangedAttack/Backstab/Flee; the
            // ring slot is kept so the crowd does not reshuffle every time
            // someone gets poked. Both attack tokens go back to the gate.
            ReleaseToken();
            ReleaseRangedToken();
            StopHorizontal();
            if (characterVisual != null)
            {
                characterVisual.SetMoving(false, 0f);
                characterVisual.PlayHit();
            }
            state = State.Stagger;
            staggerTimer = definition.staggerDuration;
        }

        private void OnDied()
        {
            if (dead) return;
            dead = true;
            ReleaseToken();
            ReleaseRangedToken();
            ReleaseRingSlot();
            state = State.Dead;
            Active.Remove(this);

            if (music != null) music.NotifyCombatEnd();

            if (characterVisual != null) characterVisual.PlayDeath();

            if (bodyCollider != null) bodyCollider.enabled = false;
            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.isKinematic = true;
            }

            StartCoroutine(DeathRoutine());
        }

        /// <summary>
        /// Death cleanup on UNSCALED time (reads during hitstop). With an animated
        /// <see cref="CharacterVisual"/>, the Death clip plays for its full length
        /// (or <c>deathDelay</c>, whichever is longer) and the corpse is destroyed.
        /// Without one, the legacy grey-box scale-down is kept unchanged.
        /// </summary>
        private IEnumerator DeathRoutine()
        {
            float animationDuration = characterVisual != null ? characterVisual.DeathDuration : 0f;
            float duration = definition != null ? Mathf.Max(0f, definition.deathDelay) : 0.35f;
            duration = Mathf.Max(duration, animationDuration);

            if (characterVisual == null)
            {
                Transform target = visualRoot != null ? visualRoot : transform;
                Vector3 startScale = target.localScale;
                Vector3 endScale = Vector3.one * 0.05f;
                float elapsed = 0f;

                while (elapsed < duration)
                {
                    elapsed += Time.unscaledDeltaTime;
                    float t = duration > 0f ? Mathf.Clamp01(elapsed / duration) : 1f;
                    target.localScale = Vector3.Lerp(startScale, endScale, t);
                    yield return null;
                }

                target.localScale = endScale;
            }
            else
            {
                float elapsed = 0f;
                while (elapsed < duration)
                {
                    elapsed += Time.unscaledDeltaTime;
                    yield return null;
                }
            }

            Destroy(gameObject);
        }

        /// <summary>
        /// Player-death reset seam (the spawner currently recreates enemies, so this
        /// is only used by callers that keep a live instance). Repositions the enemy,
        /// clears steering and replays the spawn animation.
        /// </summary>
        public void ResetForRespawn(Vector3 spawnPosition)
        {
            if (dead) return;

            transform.position = spawnPosition;
            StopHorizontal();
            state = State.Idle;

            if (characterVisual != null) characterVisual.PlaySpawn();
        }

        // --- Movement helpers ------------------------------------------------

        private void Face(Vector3 flatDirection, float dt)
        {
            if (flatDirection.sqrMagnitude < 0.000001f) return;
            Quaternion target = Quaternion.LookRotation(flatDirection, Vector3.up);
            rb.MoveRotation(Quaternion.RotateTowards(rb.rotation, target, definition.turnSpeed * dt));
        }

        private void Steer(Vector3 direction)
        {
            Steer(direction, 1f);
        }

        private void Steer(Vector3 direction, float speedMultiplier)
        {
            Vector3 velocity = rb.linearVelocity;
            velocity.x = direction.x * definition.moveSpeed * speedMultiplier;
            velocity.z = direction.z * definition.moveSpeed * speedMultiplier;
            rb.linearVelocity = velocity;
        }

        private void StopHorizontal()
        {
            // A kinematic body (dead enemy) must not be written a velocity.
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

        // --- Combat tokens ----------------------------------------------------

        private bool TryAcquireToken()
        {
            if (hasToken) return true;

            var gate = coordinator != null ? coordinator : CombatCoordinator.Instance;
            coordinator = gate;
            hasToken = gate == null || gate.TryAcquire(this);
            return hasToken;
        }

        private void ReleaseToken()
        {
            if (!hasToken) return;
            hasToken = false;
            var gate = coordinator != null ? coordinator : CombatCoordinator.Instance;
            if (gate != null) gate.Release(this);
        }

        private bool TryAcquireRangedToken()
        {
            if (hasRangedToken) return true;

            var gate = coordinator != null ? coordinator : CombatCoordinator.Instance;
            coordinator = gate;
            hasRangedToken = gate == null || gate.TryAcquireRanged(this);
            return hasRangedToken;
        }

        private void ReleaseRangedToken()
        {
            if (!hasRangedToken) return;
            hasRangedToken = false;
            var gate = coordinator != null ? coordinator : CombatCoordinator.Instance;
            if (gate != null) gate.ReleaseRanged(this);
        }

        private void ReleaseRingSlot()
        {
            var gate = coordinator != null ? coordinator : CombatCoordinator.Instance;
            if (gate != null) gate.ReleaseSlot(this);
        }
    }
}
