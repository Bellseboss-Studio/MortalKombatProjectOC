using System;
using System.Collections;
using UnityEngine;

namespace MK.Rebuild
{
    /// <summary>
    /// M1 slice-1 hurtbox: the component attacks look for (marker pattern, same
    /// as <see cref="Grabbable"/>) — layer-agnostic, everything stays Default.
    ///
    /// HP defaults to 20 (doc 05 esbirro). <see cref="TakeHit"/> applies damage
    /// and drives the two grey-box feedbacks: a COLOR FLASH and a KNOCKBACK NUDGE.
    /// Both run on UNSCALED time so they stay visible during hitstop
    /// (<c>Time.timeScale = 0</c>) — scaled time would freeze them.
    ///
    /// Defaults keep the scene/debug Dummies exactly as before: a victim with no
    /// Rigidbody gets a manual damped push, and a LAUNCHER hit drives the
    /// juggle-lite vertical arc (rise -> fall -> land), manual velocity + gravity.
    /// Enemies opt into <c>rigidbodyKnockback</c> (horizontal physics velocity,
    /// no manual arc) and <c>destroyOnDeath = false</c> (their controller owns
    /// the death visual/destroy).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class HitReceiver : MonoBehaviour
    {
        [Tooltip("Max HP. 20 default (doc 05 esbirro); raise in the inspector if the dummy dies too fast.")]
        public int maxHp = 20;

        [Tooltip("Death behavior. TRUE (dummies): destroy the object instantly. FALSE (enemies): disable the root collider and let the controller own the death visual/destroy.")]
        [SerializeField] private bool destroyOnDeath = true;

        [Tooltip("Feedback mode. FALSE (dummies): manual damped nudge. TRUE (enemies with a Rigidbody): horizontal physics velocity instead.")]
        [SerializeField] private bool rigidbodyKnockback = false;

        [Tooltip("Flash duration in seconds (UNSCALED — visible during hitstop).")]
        [SerializeField] private float flashDuration = 0.12f;

        [Tooltip("Flash tint applied to the renderer while hit (over original color).")]
        [SerializeField] private Color flashTint = new Color(1f, 0.35f, 0.35f);

        [Tooltip("Knockback start speed in m/s (UNSCALED).")]
        [SerializeField] private float knockbackSpeed = 6f;

        [Tooltip("Knockback exponential damping (1/s). Higher = shorter, snappier push.")]
        [SerializeField] private float knockbackDamping = 18f;

        [Tooltip("JUGGLE-LITE gravity in m/s^2 (placeholder). The launched victim rises then falls back to the ground plane under this manual gravity.")]
        [SerializeField] private float launchGravity = 18f;

        private int hp;
        private Renderer cachedRenderer;
        private Color originalColor;
        private Coroutine feedbackRoutine;
        private Coroutine arcRoutine;
        private bool groundCaptured;
        private float groundY;

        /// <summary>Current HP (read-only for the debug/verify seam).</summary>
        public int HP => hp;

        /// <summary>Max HP of the current configure/serialized value (read-only debug seam).</summary>
        public int MaxHp => maxHp;

        /// <summary>
        /// Phase-gate seam (boss): while TRUE, <see cref="TakeHit"/> is a no-op.
        /// The boss turns this on during the roar transition and off when phase 2
        /// begins. Dummies/enemies never set it, so their behavior is unchanged.
        /// </summary>
        public bool Invulnerable { get; set; }

        /// <summary>
        /// Lethal-hit seam (boss): fired INSTEAD of <see cref="OnDied"/> when HP
        /// reaches 0. A listener may restore HP (phase transition) and the object
        /// then stays alive; no death events fire. With no listener (dummies /
        /// enemies) the behavior is identical to before.
        /// </summary>
        public event Action<HitReceiver> LethalHit;

        /// <summary>True while HP is above zero.</summary>
        public bool IsAlive => hp > 0;

        /// <summary>
        /// Non-lethal hit seam (enemy controller): damage, horizontal push
        /// direction and launch impulse. Fired BEFORE the grey-box feedback so a
        /// stagger can react on the same frame. The killing blow does NOT fire
        /// this — it fires <see cref="OnDied"/> instead.
        /// </summary>
        public event Action<int, Vector3, float> Damaged;

        /// <summary>
        /// Style-meter seam (slice style-meter): fired in <see cref="Die"/> BEFORE
        /// <c>Destroy</c> so the +25 kill bonus can still observe the death.
        /// </summary>
        public event Action OnDied;

        /// <summary>
        /// Static echo of <see cref="OnDied"/> for observers that cannot track
        /// dynamically spawned receivers (debug dummies). Same timing: pre-Destroy.
        /// </summary>
        public static event Action AnyDied;

        private void OnEnable()
        {
            hp = maxHp;
            ResolveRenderer();
        }

        /// <summary>
        /// Applies a data-driven HP value (enemy archetypes) instead of relying on
        /// the serialized default. Clamped to at least 1 so a misconfigured asset
        /// never creates an already-dead target.
        /// </summary>
        public void Configure(int hpValue)
        {
            maxHp = Mathf.Max(1, hpValue);
            hp = maxHp;
        }

        private void ResolveRenderer()
        {
            if (cachedRenderer != null) return;
            cachedRenderer = GetComponentInChildren<Renderer>();
            if (cachedRenderer != null) originalColor = cachedRenderer.material.color;
        }

        /// <summary>
        /// Applies <paramref name="damage"/> and shows the grey-box feedback.
        /// <paramref name="dir"/> is the FULL horizontal push vector (direction already
        /// scaled by the attack's knockback distance) — see <see cref="PlayerCombat"/>.
        /// <paramref name="launchImpulse"/> (m/s) drives the juggle-lite arc when the
        /// hit is a LAUNCHER: the victim rises, falls under manual gravity and lands
        /// back on the ground plane. 0 = normal hit (feedback only).
        /// </summary>
        public void TakeHit(int damage, Vector3 dir, float launchImpulse = 0f)
        {
            if (Invulnerable) return; // boss phase transition: no damage, no feedback
            if (!IsAlive) return;
            hp = Mathf.Max(0, hp - damage);

            // Grey-box death: at 0 HP the dummy disappears immediately. Applies to
            // the scene Dummy and debug-spawned dummies alike (shared component).
            // The `!IsAlive` guard above makes a dying target stop taking hits —
            // even during the frame Destroy is deferred to end-of-frame.
            //
            // Boss seam: LethalHit fires first. If a listener restores HP (phase 1
            // -> phase 2 transition) the object survives and this frame ends with
            // no death/feedback. Without a listener, Die() runs exactly as before.
            if (hp <= 0)
            {
                LethalHit?.Invoke(this);
                if (hp > 0) return; // listener restored phase HP
                Die();
                return; // no feedback on the killing blow — the object is gone
            }

            // Enemy seam: let the controller react (stagger) BEFORE the feedback
            // coroutine starts. Dummies have no listener, so nothing changes there.
            Damaged?.Invoke(damage, dir, launchImpulse);

            if (feedbackRoutine != null) StopCoroutine(feedbackRoutine);
            feedbackRoutine = StartCoroutine(Feedback(dir));

            // JUGGLE-LITE (slice 3nonies): only a launcher (impulse > 0) starts the
            // vertical arc. The ground Y is captured ONCE (the resting height) so
            // re-launches while airborne still land on the same plane. A Rigidbody
            // victim (enemies) owns its vertical movement, so the manual arc is skipped.
            if (launchImpulse > 0f && !rigidbodyKnockback)
            {
                if (!groundCaptured) { groundY = transform.position.y; groundCaptured = true; }
                if (arcRoutine != null) StopCoroutine(arcRoutine);
                arcRoutine = StartCoroutine(LaunchArc(launchImpulse));
            }
        }

        /// <summary>
        /// JUGGLE-LITE placeholder: a manual vertical arc (velocity + gravity, UNSCALED
        /// so it reads during hitstop). The victim rises to an apex (≈ impulse² / 2g)
        /// then falls; on reaching the ground plane it snaps to it and stops — it LANDS,
        /// it does NOT float. NO Rigidbody, NO aerial state machine (deferred).
        /// </summary>
        private IEnumerator LaunchArc(float launchImpulse)
        {
            float vy = launchImpulse;
            float g = Mathf.Max(0.001f, launchGravity);
            while (true)
            {
                float dt = Time.unscaledDeltaTime;
                vy -= g * dt;
                Vector3 p = transform.position;
                p.y += vy * dt;
                if (p.y <= groundY && vy < 0f)
                {
                    p.y = groundY; // landed — snap to the ground plane
                    transform.position = p;
                    break;
                }
                transform.position = p;
                yield return null;
            }
            arcRoutine = null;
        }

        /// <summary>
        /// INSTANT destroy (no death anim/delay) — M1 placeholder; real enemy
        /// death is M3. Destroy (not Deactivate) so respawn/spawn counts are honest.
        /// The death events fire BEFORE Destroy (a destroyed object notifies nobody).
        /// </summary>
        private void Die()
        {
            OnDied?.Invoke();
            AnyDied?.Invoke();

            // Dummies: instant destroy, exactly as before. Enemies: the controller
            // owns the death visual/destroy, so this only removes the hurtbox.
            if (!destroyOnDeath)
            {
                var rootCollider = GetComponent<Collider>();
                if (rootCollider != null) rootCollider.enabled = false;
                enabled = false;
                return;
            }

            Destroy(gameObject);
        }

        private IEnumerator Feedback(Vector3 push)
        {
            // Enemy path: a Rigidbody steers the push, preserving vertical motion.
            if (rigidbodyKnockback && TryGetComponent<Rigidbody>(out var body))
            {
                Vector3 velocity = body.linearVelocity;
                velocity.x = push.x * knockbackSpeed;
                velocity.z = push.z * knockbackSpeed;
                body.linearVelocity = velocity;
                yield return Flash();
                yield break;
            }

            yield return Flash();

            // Knockback: damped manual nudge, unscaled so it reads in hitstop/slow-mo.
            float elapsed = 0f;
            while (elapsed < flashDuration)
            {
                float dt = Time.unscaledDeltaTime;
                elapsed += dt;
                Vector3 step = push * knockbackSpeed * Mathf.Exp(-knockbackDamping * elapsed) * dt;
                transform.position += step;
                yield return null;
            }
        }

        private IEnumerator Flash()
        {
            ResolveRenderer();
            if (cachedRenderer == null) yield break;

            var material = cachedRenderer.material; // instance — never clobber shared mats
            Color previous = originalColor;
            material.color = FlashColor(previous);
            yield return new WaitForSecondsRealtime(flashDuration * 0.5f);
            material.color = previous;
        }

        private Color FlashColor(Color baseColor)
        {
            // White-ish pulse: lerp the base toward the tint, keeping alpha.
            return Color.Lerp(baseColor, flashTint, 0.8f);
        }
    }
}