using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MK.Rebuild
{
    /// <summary>
    /// M3.1 grey-box melee enemy: four live states (Idle → Chase → Attack →
    /// Stagger, plus terminal Dead) driven by Rigidbody steering — NO NavMesh,
    /// NO root motion and NO animation dependency. Every number comes from the
    /// assigned <see cref="EnemyDefinition"/>.
    ///
    /// Attacks are token-gated by <see cref="CombatCoordinator"/> so only a
    /// capped number of enemies press at once. The hurt/death seams belong to
    /// <see cref="HitReceiver"/>: non-lethal damage fires <c>Damaged</c> (this
    /// controller staggers) and the killing blow fires <c>OnDied</c> (this
    /// controller owns the grey-box death visual/destroy). <c>AnyDied</c> still
    /// reaches the style meter, so the +25 kill bonus is unchanged.
    ///
    /// Separation is a lightweight sum of repulsion vectors from other living
    /// enemies (ring slots are M3.2).
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody), typeof(HitReceiver))]
    public sealed class EnemyController : MonoBehaviour
    {
        private const float SeparationRadius = 1.2f;
        private const float SeparationWeight = 0.5f;

        private enum State { Idle, Chase, Attack, Stagger, Dead }

        [Tooltip("Archetype data. All gameplay numbers come from here — never hardcoded per enemy.")]
        [SerializeField] private EnemyDefinition definition;

        [Tooltip("Transform scaled by the death routine. Defaults to this transform.")]
        [SerializeField] private Transform visualRoot;

        private static readonly List<EnemyController> Active = new();

        private Rigidbody rb;
        private HitReceiver receiver;
        private Collider bodyCollider;
        private PlayerDamage player;
        private CombatCoordinator coordinator;
        private MusicStateController music;

        private State state = State.Idle;
        private float cooldownTimer;
        private float attackTimer;
        private float staggerTimer;
        private bool attackHitApplied;
        private bool hasToken;
        private bool combatNotified;
        private bool dead;
        private bool warnedMissingDefinition;

        private void Awake()
        {
            if (visualRoot == null) visualRoot = transform;
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
        }

        private void OnDisable()
        {
            Active.Remove(this);
            ReleaseToken();
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

            float dt = Time.fixedDeltaTime;
            if (cooldownTimer > 0f) cooldownTimer -= dt;

            // No player / dead player: hold position (physics still owns Y) and
            // never keep an attack token while the player cannot fight back.
            if (player == null || !player.IsAlive)
            {
                ReleaseToken();
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
            if (coordinator == null) coordinator = CombatCoordinator.Instance;
            if (music == null) music = FindFirstObjectByType<MusicStateController>();
        }

        // --- States ----------------------------------------------------------

        private void EnterChase()
        {
            state = State.Chase;
            if (combatNotified) return;
            combatNotified = true;
            if (music != null) music.NotifyCombat();
        }

        private void TickChase(float dt)
        {
            Vector3 toPlayer = FlatDirectionTo(player.transform.position);
            Face(toPlayer, dt);

            float distance = FlatDistanceTo(player.transform.position);
            if (distance <= definition.attackRange && cooldownTimer <= 0f && TryAcquireToken())
            {
                EnterAttack();
                return;
            }

            if (distance <= definition.attackRange)
            {
                StopHorizontal();
                return;
            }

            Vector3 separation = SeparationFromAllies();
            Vector3 direction = (toPlayer + separation * SeparationWeight).normalized;
            Steer(direction);
        }

        private void EnterAttack()
        {
            state = State.Attack;
            attackTimer = 0f;
            attackHitApplied = false;
            StopHorizontal();
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

        private void TickStagger(float dt)
        {
            // Deliberately does NOT write horizontal velocity: knockback/steering
            // momentum plays out while staggering.
            staggerTimer -= dt;
            if (staggerTimer <= 0f) state = State.Chase;
        }

        // --- HitReceiver seams ----------------------------------------------

        private void OnDamaged(int damage, Vector3 dir, float launchImpulse)
        {
            if (dead || definition == null) return;
            if (damage >= definition.poiseThreshold) EnterStagger();
        }

        private void EnterStagger()
        {
            ReleaseToken();
            state = State.Stagger;
            staggerTimer = definition.staggerDuration;
        }

        private void OnDied()
        {
            if (dead) return;
            dead = true;
            ReleaseToken();
            state = State.Dead;
            Active.Remove(this);

            if (music != null) music.NotifyCombatEnd();

            if (bodyCollider != null) bodyCollider.enabled = false;
            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.isKinematic = true;
            }

            StartCoroutine(DeathRoutine());
        }

        /// <summary>
        /// Grey-box death: scales the visual to 0.05 over <c>deathDelay</c> on
        /// UNSCALED time (reads during hitstop), then destroys the object. No
        /// animation, no ragdoll.
        /// </summary>
        private IEnumerator DeathRoutine()
        {
            Transform target = visualRoot != null ? visualRoot : transform;
            Vector3 startScale = target.localScale;
            Vector3 endScale = Vector3.one * 0.05f;
            float duration = definition != null ? Mathf.Max(0f, definition.deathDelay) : 0.35f;
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

        // --- Movement helpers ------------------------------------------------

        private void Face(Vector3 flatDirection, float dt)
        {
            if (flatDirection.sqrMagnitude < 0.000001f) return;
            Quaternion target = Quaternion.LookRotation(flatDirection, Vector3.up);
            rb.MoveRotation(Quaternion.RotateTowards(rb.rotation, target, definition.turnSpeed * dt));
        }

        private void Steer(Vector3 direction)
        {
            Vector3 velocity = rb.linearVelocity;
            velocity.x = direction.x * definition.moveSpeed;
            velocity.z = direction.z * definition.moveSpeed;
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

        private Vector3 SeparationFromAllies()
        {
            Vector3 sum = Vector3.zero;
            Vector3 position = transform.position;

            for (int i = 0; i < Active.Count; i++)
            {
                var other = Active[i];
                if (other == null || other == this || other.dead) continue;

                Vector3 away = position - other.transform.position;
                away.y = 0f;
                float distance = away.magnitude;
                if (distance > 0.0001f && distance < SeparationRadius)
                {
                    sum += away / distance;
                }
            }

            return sum.sqrMagnitude > 0.000001f ? sum.normalized : Vector3.zero;
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
    }
}
