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
    /// The victim has no Rigidbody (scene Dummy), so knockback is a manual
    /// damped push, not a physics impulse.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class HitReceiver : MonoBehaviour
    {
        [Tooltip("Max HP. 20 default (doc 05 esbirro); raise in the inspector if the dummy dies too fast.")]
        public int maxHp = 20;

        [Tooltip("Flash duration in seconds (UNSCALED — visible during hitstop).")]
        [SerializeField] private float flashDuration = 0.12f;

        [Tooltip("Flash tint applied to the renderer while hit (over original color).")]
        [SerializeField] private Color flashTint = new Color(1f, 0.35f, 0.35f);

        [Tooltip("Knockback start speed in m/s (UNSCALED).")]
        [SerializeField] private float knockbackSpeed = 6f;

        [Tooltip("Knockback exponential damping (1/s). Higher = shorter, snappier push.")]
        [SerializeField] private float knockbackDamping = 18f;

        private int hp;
        private Renderer cachedRenderer;
        private Color originalColor;
        private Coroutine feedbackRoutine;

        /// <summary>Current HP (read-only for the debug/verify seam).</summary>
        public int HP => hp;

        /// <summary>True while HP is above zero.</summary>
        public bool IsAlive => hp > 0;

        private void OnEnable()
        {
            hp = maxHp;
            ResolveRenderer();
        }

        private void ResolveRenderer()
        {
            if (cachedRenderer != null) return;
            cachedRenderer = GetComponentInChildren<Renderer>();
            if (cachedRenderer != null) originalColor = cachedRenderer.material.color;
        }

        /// <summary>
        /// Applies <paramref name="damage"/> and shows the grey-box feedback.
        /// <paramref name="dir"/> is the FULL push vector (direction already scaled
        /// by the attack's knockback distance) — see <see cref="PlayerCombat"/>.
        /// </summary>
        public void TakeHit(int damage, Vector3 dir)
        {
            if (!IsAlive) return;
            hp = Mathf.Max(0, hp - damage);

            // Grey-box death: at 0 HP the dummy disappears immediately. Applies to
            // the scene Dummy and debug-spawned dummies alike (shared component).
            // The `!IsAlive` guard above makes a dying target stop taking hits —
            // even during the frame Destroy is deferred to end-of-frame.
            if (hp <= 0)
            {
                Die();
                return; // no feedback on the killing blow — the object is gone
            }

            if (feedbackRoutine != null) StopCoroutine(feedbackRoutine);
            feedbackRoutine = StartCoroutine(Feedback(dir));
        }

        /// <summary>
        /// INSTANT destroy (no death anim/delay) — M1 placeholder; real enemy
        /// death is M3. Destroy (not Deactivate) so respawn/spawn counts are honest.
        /// </summary>
        private void Die()
        {
            Destroy(gameObject);
        }

        private IEnumerator Feedback(Vector3 push)
        {
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