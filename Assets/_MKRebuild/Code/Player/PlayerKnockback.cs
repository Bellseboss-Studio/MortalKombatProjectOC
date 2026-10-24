using UnityEngine;

namespace MK.Rebuild
{
    /// <summary>
    /// m2-hazards S3: the player's pushed reaction. Owns the HORIZONTAL axis for the
    /// duration of a shove and never touches the vertical axis (the motor keeps gravity /
    /// jump / arc). This is the important composition rule: a mid-air shove must ADD
    /// horizontal velocity while the ballistic arc completes (the `fix-walljump-arc` bug
    /// class was exactly a horizontal writer stomping vertical).
    ///
    /// Duration: <see cref="AnimatorVisual.HitReactionDuration"/> (trimmed impact clip length
    /// / clip speed) unless the hazard authored an explicit override. The lock, the clip and
    /// the shove therefore share ONE source of truth and cannot drift.
    ///
    /// Shape: v(t) = v0 * (1 - t/T), v0 = 2 * distance / T — LINEAR decay, so the integral
    /// (the distance actually travelled) is EXACTLY the authored distance. (HitReceiver's
    /// exponential damping does not preserve an authored distance, so it is deliberately not
    /// copied.) Applied as RIGIDBODY VELOCITY, never a teleport.
    ///
    /// Body ownership: acquires <see cref="BodyPriority.Knockback"/> from the S2 arbiter and
    /// releases that same token when the shove ends or is cancelled. The motor's derived
    /// InputLocked branch preserves the horizontal velocity written here instead of applying
    /// its own damping. <see cref="DefaultExecutionOrder"/> guarantees this runs AFTER the
    /// motor (default order 0) so its X/Z write is the last one in each physics step.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody))]
    [DefaultExecutionOrder(100)]
    public sealed class PlayerKnockback : MonoBehaviour
    {
        [Tooltip("Fallback knockback distance (meters) for callers that do not pass an authored " +
                 "distance (e.g. the F6 debug hurt driver). Hazards always pass their own.")]
        [SerializeField] private float defaultDistance = 2.5f;

        private Rigidbody rb;
        private PlayerMotor motor;
        private AnimatorVisual animatorVisual;
        private BodyOwnership.Token token;

        private bool active;
        private float elapsed;
        private float duration;
        private float initialSpeed;
        private Vector3 direction = Vector3.forward;

        /// <summary>Fallback only when neither the animation nor an override resolves.</summary>
        private const float FallbackSeconds = 0.4f;

        /// <summary>True while the shove is writing horizontal velocity (read-only debug seam).</summary>
        public bool IsActive => active;

        /// <summary>Resolved shove duration in seconds (read-only debug seam).</summary>
        public float Duration => duration;

        /// <summary>Resolved v0 in m/s (read-only debug seam). Expected 2 * distance / duration.</summary>
        public float InitialSpeed => initialSpeed;

        /// <summary>Authored distance used by the 2-arg <see cref="PlayerDamage.TakeHit"/> caller.</summary>
        public float DefaultDistance => defaultDistance;

        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
            motor = GetComponent<PlayerMotor>();
            animatorVisual = GetComponent<AnimatorVisual>();
        }

        /// <summary>
        /// Starts (or restarts) a shove with the component's default distance. Kept as the
        /// 2-arg contract used by the pre-existing hurt seam.
        /// </summary>
        public void Apply(Vector3 pushDir, float distance)
        {
            Apply(pushDir, distance, 0f);
        }

        /// <summary>
        /// Starts (or restarts) a shove of <paramref name="distance"/> meters along the
        /// horizontal <paramref name="pushDir"/>. <paramref name="durationOverride"/> &gt; 0
        /// pins the duration; 0 derives it from the hit-reaction animation.
        /// </summary>
        public void Apply(Vector3 pushDir, float distance, float durationOverride)
        {
            pushDir.y = 0f;
            if (pushDir.sqrMagnitude < 0.000001f) return;

            // Acquire ONLY once: a second overlap during the same shove restarts the profile
            // with the SAME token (releasing a fresh token per hit would orphan the old one).
            if (token == null && motor != null && motor.Body != null)
            {
                if (!motor.Body.CanAcquire(BodyPriority.Knockback)) return;
                token = motor.Body.Acquire(BodyPriority.Knockback);
            }

            direction = pushDir.normalized;
            float t = durationOverride > 0f
                ? durationOverride
                : (animatorVisual != null ? animatorVisual.HitReactionDuration : 0f);
            duration = Mathf.Max(0.0001f, t > 0f ? t : FallbackSeconds);
            elapsed = 0f;
            // Integral of v0*(1-t/T) over [0,T] == v0*T/2 == distance.
            initialSpeed = 2f * Mathf.Max(0f, distance) / duration;
            active = true;

            // First impulse immediately (Apply may be called from a trigger callback between
            // FixedUpdates). Y is preserved by WriteHorizontal.
            WriteHorizontal(initialSpeed);
        }

        private void FixedUpdate()
        {
            if (!active) return;

            // Higher-priority owner (death) took the body: stop contributing. HandleDied
            // calls Cancel() explicitly; this is the belt-and-braces path.
            if (motor != null && motor.Body != null
                && motor.Body.Owner != BodyPriority.Knockback)
            {
                active = false;
                ReleaseToken();
                return;
            }

            elapsed += Time.fixedDeltaTime;
            if (elapsed >= duration)
            {
                WriteHorizontal(0f);
                active = false;
                elapsed = 0f;
                ReleaseToken();
                return;
            }

            float speed = initialSpeed * (1f - elapsed / duration);
            WriteHorizontal(speed);
        }

        /// <summary>
        /// Stops the shove, zeroes ONLY the horizontal axis (so a following
        /// <see cref="PlayerMotor.Teleport"/> sees a dead body) and releases the token.
        /// Idempotent; called by the death path.
        /// </summary>
        public void Cancel()
        {
            active = false;
            elapsed = 0f;
            if (rb != null) WriteHorizontal(0f);
            ReleaseToken();
        }

        private void OnDisable()
        {
            Cancel();
        }

        /// <summary>Writes the shove speed on X/Z. NEVER touches Y (vertical composes).</summary>
        private void WriteHorizontal(float speed)
        {
            if (rb == null) return;
            Vector3 v = rb.linearVelocity;
            v.x = direction.x * speed;
            v.z = direction.z * speed;
            rb.linearVelocity = v;
        }

        private void ReleaseToken()
        {
            if (token != null && motor != null && motor.Body != null)
                motor.Body.Release(token);
            token = null;
        }
    }
}
