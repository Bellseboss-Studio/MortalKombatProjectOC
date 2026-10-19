using System;
using UnityEngine;

namespace MK.Rebuild
{
    /// <summary>
    /// M1 slice anim-packs-integration (slice 3-uno): the PLAYER's hurt path. Until now
    /// the player had NONE — <see cref="HitReceiver"/> is victim-side only (enemies/dummies).
    /// The hurt path plays the hit-reaction one-shot or the death one-shot, faces the hit
    /// source, applies the authored horizontal shove (<see cref="PlayerKnockback"/>, S3) and
    /// fires <see cref="OnDamaged"/>/<see cref="OnDied"/>. No hitstop, no health UI, no
    /// respawn/Destroy here — those are other systems.
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
        [Tooltip("Max HP. 100 is the GDD player baseline (dummy HitReceiver HP is separate). TakeHit subtracts; at 0 the death clip plays. Minimal live/dead gate — NOT a health system, no HUD.")]
        [SerializeField] private int maxHp = 100;

        [Tooltip("CrossFade duration (seconds) into the HitReaction/Death states (~0.1, same as the attack seams).")]
        [SerializeField] private float crossFadeDuration = 0.1f;

        [Tooltip("Get-up lock duration (seconds) after landing from an air hit. Match the GetUp clip length.")]
        [SerializeField] private float getUpDuration = 2f;

        private enum HitPhase { None, AirHit, GetUp }

        private int hp;
        private float invulnerableUntil;
        private PlayerCombat combat;
        private StyleMeter styleMeter;
        private AnimatorVisual animatorVisual;
        private PlayerKnockback knockback;
        private PlayerAbilities abilities;
        private PlayerMotor motor;

        private HitPhase hitPhase;
        private BodyOwnership.Token recoveryToken;
        private float getUpTimer;

        /// <summary>True while HP is above zero (read-only debug seam).</summary>
        public bool IsAlive => hp > 0;

        /// <summary>True while the post-respawn invulnerability window is open (read-only debug seam).</summary>
        public bool IsInvulnerable => Time.time < invulnerableUntil;

        /// <summary>Current HP (read-only debug seam).</summary>
        public int HP => hp;

        /// <summary>Maximum HP (read-only seam, mirrors <see cref="HP"/>).</summary>
        public int MaxHp => maxHp;

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
            knockback = GetComponent<PlayerKnockback>();
            abilities = GetComponent<PlayerAbilities>();
            motor = GetComponent<PlayerMotor>();
        }

        private void Update()
        {
            if (hitPhase == HitPhase.AirHit)
            {
                if (motor != null && motor.IsGrounded)
                {
                    hitPhase = HitPhase.GetUp;
                    getUpTimer = Mathf.Max(0.01f, getUpDuration);
                    if (animatorVisual != null) animatorVisual.PlayGetUp(crossFadeDuration);
                }
            }
            else if (hitPhase == HitPhase.GetUp)
            {
                getUpTimer -= Time.deltaTime;
                if (getUpTimer <= 0f) CancelRecovery();
            }
        }

        /// <summary>
        /// Applies <paramref name="damage"/> to the player. A non-lethal hit plays the
        /// hit-reaction one-shot, faces the hit source, applies the shove and fires
        /// <see cref="OnDamaged"/>; damage that brings HP to 0 dies (death one-shot +
        /// <see cref="OnDied"/>) instead. No-op once dead OR while the invulnerability
        /// window is open (<see cref="GrantInvulnerability"/>).
        ///
        /// <paramref name="direction"/> is the PUSH direction (away from the source); the
        /// body faces its OPPOSITE (the attacker/wall) and <see cref="PlayerKnockback"/>
        /// moves it. This 2-arg overload uses the knockback component's fallback distance;
        /// hazards use the 4-arg overload with their authored numbers.
        /// </summary>
        public void TakeHit(int damage, Vector3 direction)
        {
            TakeHit(damage, direction, -1f, 0f);
        }

        /// <summary>
        /// Hazard entry (m2-hazards S3): same hurt path, with an AUTHORED knockback distance
        /// (meters) and an optional duration override (0 = derive from the hit animation).
        /// <paramref name="knockbackDistance"/> &lt; 0 means "use the component fallback".
        /// </summary>
        public void TakeHit(int damage, Vector3 direction, float knockbackDistance, float knockbackDuration)
        {
            if (!IsAlive || IsInvulnerable) return;
            CancelRecovery();
            hp = Mathf.Max(0, hp - Mathf.Max(0, damage));

            if (hp <= 0)
            {
                Die();
                return;
            }

            AudioDirector.Play(AudioCueId.Hurt, transform.position);
            CancelAttacks();
            if (styleMeter != null) styleMeter.NotifyDamageTaken();
            OnDamaged?.Invoke();

            // m2-hazards S3: the reserved direction is now real. Face the SOURCE (opposite
            // the push), then hand the horizontal shove to PlayerKnockback. Ordered before
            // PlayHitReaction so the shove clip is already oriented at the threat.
            Vector3 push = direction;
            push.y = 0f;
            if (push.sqrMagnitude > 0.000001f)
            {
                if (animatorVisual != null) animatorVisual.SnapFacing(-push);
                if (knockback != null)
                {
                    float distance = knockbackDistance >= 0f ? knockbackDistance : knockback.DefaultDistance;
                    knockback.Apply(push, distance, knockbackDuration);
                }
            }

            if (motor != null && !motor.IsGrounded)
            {
                hitPhase = HitPhase.AirHit;
                AcquireRecoveryToken();
                if (animatorVisual != null) animatorVisual.PlayAirHit(crossFadeDuration);
            }
            else if (animatorVisual != null)
            {
                animatorVisual.PlayHitReaction(crossFadeDuration);
            }
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

        /// <summary>
        /// Restores FULL HP (hp = maxHp). Does NOT revive the Animator — pair with
        /// <see cref="Revive"/> (the respawn routine calls both). No other state is
        /// touched: this is the smallest "back to full" seam, not a health system.
        /// </summary>
        public void Heal()
        {
            hp = maxHp;
        }

        /// <summary>
        /// Opens a brief invulnerability window of <paramref name="duration"/> seconds.
        /// While open, <see cref="TakeHit"/> early-returns (no damage, no hit reaction) —
        /// the respawn grace after a revive. <see cref="Kill"/> is deliberately NOT gated,
        /// so the F7 debug driver stays deterministic.
        /// </summary>
        public void GrantInvulnerability(float duration)
        {
            invulnerableUntil = Time.time + Mathf.Max(0f, duration);
        }

        /// <summary>
        /// Revives from the held Death pose: forces the visual back to locomotion
        /// (<see cref="AnimatorVisual.ResetToLocomotion"/>) using this component's
        /// serialized <c>crossFadeDuration</c>. The hurt-entry owns the anim driver
        /// (same as <see cref="Die"/>), so the respawn routine asks here, not directly.
        /// </summary>
        public void Revive()
        {
            Revive(crossFadeDuration);
        }

        /// <summary>
        /// Revives from the held Death pose with an explicit crossfade duration
        /// (<paramref name="crossFadeDuration"/> seconds into locomotion).
        /// </summary>
        public void Revive(float crossFadeDuration)
        {
            CancelRecovery();
            if (animatorVisual != null) animatorVisual.ResetToLocomotion(crossFadeDuration);
        }

        /// <summary>Hit entry: cancels upward momentum/jump buffer (wall hits).</summary>
        public void CancelAirMomentum()
        {
            if (motor == null) motor = GetComponent<PlayerMotor>();
            if (motor != null) motor.CancelVerticalMomentum();
        }

        private void Die()
        {
            AudioDirector.Play(AudioCueId.Death, transform.position);
            CancelRecovery();
            CancelAttacks();
            if (styleMeter != null) styleMeter.NotifyDamageTaken();
            OnDied?.Invoke();
            if (animatorVisual != null) animatorVisual.PlayDeath(crossFadeDuration);
        }

        private void OnDisable()
        {
            CancelRecovery();
        }

        private void AcquireRecoveryToken()
        {
            if (motor == null || motor.Body == null || recoveryToken != null) return;
            recoveryToken = motor.Body.Acquire(BodyPriority.HitRecovery);
        }

        private void CancelRecovery()
        {
            hitPhase = HitPhase.None;
            getUpTimer = 0f;
            if (recoveryToken != null && motor != null && motor.Body != null)
                motor.Body.Release(recoveryToken);
            recoveryToken = null;
        }

        /// <summary>
        /// Aborts the active combo/swing so the hurt reaction is not fought by a swing
        /// (PlayerCombat releases its attack lock and the Attack_* states). No-op when idle.
        /// </summary>
        private void CancelAttacks()
        {
            if (combat != null) combat.CancelCombo();
            if (abilities != null) abilities.Stop();
        }
    }
}
