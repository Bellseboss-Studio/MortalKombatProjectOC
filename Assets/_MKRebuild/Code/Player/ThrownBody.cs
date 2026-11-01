using System.Collections.Generic;
using UnityEngine;

namespace MK.Rebuild
{
    /// <summary>
    /// Turns a thrown victim into a short-lived PROJECTILE (Tarkatana grab follow-up).
    ///
    /// WHY this component exists: the grab throws the victim with plain Rigidbody physics,
    /// but a body that flies and damages others needs an impact query somewhere. It is added
    /// at runtime by <see cref="PlayerGrab"/> and removed when the flight window ends — no
    /// prefab wiring, no scene change.
    ///
    /// It reuses <see cref="HitReceiver"/> as the only hurt seam: every alive receiver inside
    /// the impact sphere takes the throw damage ONCE (a <see cref="HashSet{T}"/> guards
    /// duplicates from multi-collider bodies), the thrower's own receivers are skipped, and
    /// the thrown body itself takes the same damage once on its first real impact (symmetric
    /// body-slam). The self hit is skipped when the body has no <see cref="HitReceiver"/> on
    /// its own GameObject, so nothing can recurse into the same receiver twice.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ThrownBody : MonoBehaviour
    {
        [Tooltip("Damage applied to every victim the flying body touches.")]
        [SerializeField] private int damage = 8;

        [Tooltip("Impact radius in meters (OverlapSphere around the flying body).")]
        [SerializeField] private float radius = 0.9f;

        [Tooltip("Seconds the body keeps damaging on contact after the throw.")]
        [SerializeField] private float duration = 0.5f;

        private readonly HashSet<HitReceiver> alreadyHit = new HashSet<HitReceiver>();

        private GameObject owner;
        private HitReceiver self;
        private EnemyController thrownEnemy;
        private float elapsed;
        private bool initialized;

        private void Awake()
        {
            self = GetComponent<HitReceiver>();
            thrownEnemy = GetComponentInParent<EnemyController>();
        }

        private void OnDisable()
        {
            ClearThrownFlag();
        }

        private void OnDestroy()
        {
            ClearThrownFlag();
        }

        /// <summary>
        /// Arms the projectile. Called once per throw by <see cref="PlayerGrab"/>; a reused
        /// component starts a fresh impact window instead of inheriting the previous one.
        /// </summary>
        /// <param name="owner">Thrower root. Its receivers (and its children's) are never hit.</param>
        /// <param name="damage">Damage per receiver, applied once each.</param>
        /// <param name="radius">Impact query radius in meters.</param>
        /// <param name="duration">Seconds the impact window stays open.</param>
        public void Initialize(GameObject owner, float damage, float radius, float duration)
        {
            this.owner = owner;
            this.damage = Mathf.Max(0, Mathf.RoundToInt(damage));
            this.radius = Mathf.Max(0.01f, radius);
            this.duration = Mathf.Max(0.01f, duration);

            elapsed = 0f;
            alreadyHit.Clear();

            if (self == null) self = GetComponent<HitReceiver>();

            // The enemy controller (when there is one) must stop braking its own horizontal
            // velocity while this body flies — the debug AI freeze and the dead branch both
            // called StopHorizontal every frame, which stalled the throw in the air.
            if (thrownEnemy == null) thrownEnemy = GetComponentInParent<EnemyController>();
            if (thrownEnemy != null) thrownEnemy.Thrown = true;

            initialized = true;
        }

        /// <summary>
        /// Releases the "thrown" ownership flag on the enemy controller. Null-safe and
        /// idempotent: the enemy/body may already be destroyed, in which case there is
        /// nothing to clear.
        /// </summary>
        private void ClearThrownFlag()
        {
            if (thrownEnemy == null) return;

            thrownEnemy.Thrown = false;
            thrownEnemy = null;
        }

        private void FixedUpdate()
        {
            // Not armed yet (added but Initialize not called) or the domain reload left the
            // serialized defaults only: do nothing until a real throw says so.
            if (!initialized) return;

            elapsed += Time.fixedDeltaTime;
            ApplyImpactDamage();

            if (elapsed >= duration) Destroy(this);
        }

        private void ApplyImpactDamage()
        {
            Collider[] colliders = Physics.OverlapSphere(
                transform.position, radius, Physics.AllLayers, QueryTriggerInteraction.Ignore);

            Vector3 selfPosition = transform.position;
            HitReceiver firstOther = null;

            for (int i = 0; i < colliders.Length; i++)
            {
                Collider collider = colliders[i];
                if (collider == null) continue;

                HitReceiver receiver = collider.GetComponentInParent<HitReceiver>();
                if (receiver == null || !receiver.IsAlive) continue;

                // Never damage the flying body itself here: it is handled once below.
                if (receiver == self) continue;
                if (receiver.transform == transform || receiver.transform.IsChildOf(transform)) continue;

                // The thrower must not be hit by its own projectile.
                if (BelongsToOwner(receiver)) continue;

                if (!alreadyHit.Add(receiver)) continue; // one hit per receiver

                Vector3 dir = DirectionTo(receiver.transform.position, selfPosition);
                receiver.TakeHit(damage, dir);

                if (firstOther == null) firstOther = receiver;
            }

            // Symmetric body-slam: the first REAL impact also hurts the thrown body once.
            // Without another receiver there is no impact, so the body is left alone.
            if (firstOther != null) DamageSelfOnce(firstOther.transform.position);
        }

        private void DamageSelfOnce(Vector3 impactPosition)
        {
            if (self == null || !self.IsAlive) return;
            if (!alreadyHit.Add(self)) return;

            Vector3 dir = DirectionTo(impactPosition, transform.position);
            self.TakeHit(damage, dir);
        }

        private bool BelongsToOwner(HitReceiver receiver)
        {
            if (owner == null) return false;
            if (receiver.gameObject == owner) return true;
            return receiver.transform.IsChildOf(owner.transform);
        }

        private Vector3 DirectionTo(Vector3 worldPosition, Vector3 from)
        {
            Vector3 dir = worldPosition - from;
            dir.y = 0f;
            return dir.sqrMagnitude > 0.0001f ? dir.normalized : transform.forward;
        }
    }
}
