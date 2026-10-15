using System;
using UnityEngine;

namespace MK.Rebuild
{
    /// <summary>
    /// M1 slice anim-packs-integration (slice 3-uno): the PLAYER's hurt path. Until now
    /// the player had NONE — <see cref="HitReceiver"/> is victim-side only (enemies/dummies).
    /// This is the one genuinely new seam of the slice, kept deliberately SIGNAL-ONLY:
    /// it plays the hit-reaction one-shot or the death one-shot and fires
    /// <see cref="OnDamaged"/>/<see cref="OnDied"/>. NO knockback, NO hitstop, NO health UI,
    /// NO respawn/Destroy — all future work.
    ///
    /// Minimal live/dead gate only: <see cref="TakeHit"/> subtracts HP and plays the
    /// HitReaction state (which returns to locomotion on its own exit-time transition);
    /// at 0 HP it dies (<see cref="Kill"/> path) and plays the Death state, whose clip runs
    /// once and HOLDS its final pose (the controller's Death state has NO outgoing
    /// transition). The <c>maxHp</c> field is the smallest thing that makes "damage brings
    /// HP to 0 -> death" real — it is NOT a damage system.
    ///
    /// A hit also CANCELS the active combo (<see cref="PlayerCombat.CancelCombo"/>) so a
    /// swing does not fight the reaction, and notifies <see cref="StyleMeter.NotifyDamageTaken"/>
    /// (the pre-existing dormant stub — no rank-down effect yet, but the call site is real now).
    ///
    /// Presentation goes through the concrete <see cref="AnimatorVisual"/> driver; the
    /// generic proxy fallback simply shows nothing.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PlayerDamage : MonoBehaviour
    {
        [Tooltip("Max HP. 20 mirrors HitReceiver's dummy default. TakeHit subtracts; at 0 the death clip plays. Minimal live/dead gate — NOT a health system, no HUD.")]
        [SerializeField] private int maxHp = 20;

        [Tooltip("CrossFade duration (seconds) into the HitReaction/Death states (~0.1, same as the attack seams).")]
        [SerializeField] private float crossFadeDuration = 0.1f;

        private int hp;
        private PlayerCombat combat;
        private StyleMeter styleMeter;
        private AnimatorVisual animatorVisual;

        /// <summary>True while HP is above zero (read-only debug seam).</summary>
        public bool IsAlive => hp > 0;

        /// <summary>Current HP (read-only debug seam).</summary>
        public int HP => hp;

        /// <summary>
        /// Hurt signal (read-only seam): fired on a NON-lethal hit, alongside the hit
        /// reaction. No payload — observers read <see cref="HP"/>. Nobody listening means
        /// nobody notices.
        /// </summary>
        public event Action OnDamaged;

        /// <summary>
        /// Death signal (read-only seam): fired ONCE when HP reaches 0, alongside the
        /// death animation. No payload.
        /// </summary>
        public event Action OnDied;

        private void Awake()
        {
            hp = maxHp;
            combat = GetComponent<PlayerCombat>();
            styleMeter = GetComponent<StyleMeter>();
            animatorVisual = GetComponent<AnimatorVisual>();
        }

        /// <summary>
        /// Applies <paramref name="damage"/> to the player. A non-lethal hit plays the
        /// hit-reaction one-shot and fires <see cref="OnDamaged"/>; damage that brings HP
        /// to 0 dies (death one-shot + <see cref="OnDied"/>) instead. No-op once dead.
        /// <paramref name="direction"/> is RESERVED for the future knockback/push seam —
        /// this slice is signal-only and does not move the body.
        /// </summary>
        public void TakeHit(int damage, Vector3 direction)
        {
            if (!IsAlive) return;
            hp = Mathf.Max(0, hp - Mathf.Max(0, damage));

            if (hp <= 0)
            {
                Die();
                return;
            }

            CancelAttacks();
            if (styleMeter != null) styleMeter.NotifyDamageTaken();
            OnDamaged?.Invoke();
            if (animatorVisual != null) animatorVisual.PlayHitReaction(crossFadeDuration);
        }

        /// <summary>
        /// Direct "damage to 0" entry: kills the player if alive (death one-shot +
        /// <see cref="OnDied"/>). No-op when already dead.
        /// </summary>
        public void Kill()
        {
            if (!IsAlive) return;
            hp = 0;
            Die();
        }

        private void Die()
        {
            CancelAttacks();
            if (styleMeter != null) styleMeter.NotifyDamageTaken();
            OnDied?.Invoke();
            if (animatorVisual != null) animatorVisual.PlayDeath(crossFadeDuration);
        }

        /// <summary>
        /// Aborts the active combo/swing so the hurt reaction is not fought by a swing
        /// (PlayerCombat releases its attack lock and the Attack_* states). No-op when idle.
        /// </summary>
        private void CancelAttacks()
        {
            if (combat != null) combat.CancelCombo();
        }
    }
}
