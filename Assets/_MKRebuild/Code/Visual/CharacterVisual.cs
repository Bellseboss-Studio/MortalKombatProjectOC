using UnityEngine;

namespace MK.Rebuild
{
    /// <summary>
    /// Lightweight animation facade for the X Bot enemy visuals (M3.6).
    ///
    /// The gameplay controllers never touch an <see cref="Animator"/> directly: they
    /// push semantic signals (moving, melee, charge, hit, death, ...) into this
    /// component, which cross-fades the matching state of the assigned
    /// <c>XBotBase.controller</c> (or one of its per-archetype override controllers).
    ///
    /// Everything here is null-safe: a visual without an animator simply ignores the
    /// calls, so grey-box prefabs keep working. <see cref="CrossFadeSafe"/> also
    /// falls back to the <c>T-Pose</c> state when a requested state is missing from
    /// the controller, so a half-configured override can never throw or leave the
    /// character invisible.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CharacterVisual : MonoBehaviour
    {
        private const string IdleState = "Idle";
        private const string MoveState = "Move";
        private const string MeleeState = "Melee";
        private const string ChargeState = "Charge";
        private const string AimState = "Aim";
        private const string ShootState = "Shoot";
        private const string StabState = "Stab";
        private const string SlamState = "Slam";
        private const string RoarState = "Roar";
        private const string FleeState = "Flee";
        private const string HitState = "Hit";
        private const string DeathState = "Death";
        private const string SpawnState = "Spawn";
        private const string FallbackState = "T-Pose";

        [Tooltip("Animator that plays XBotBase (or an AnimatorOverrideController). Defaults to GetComponentInChildren.")]
        [SerializeField] private Animator animator;

        [Tooltip("Fallback death duration when the Death clip cannot be resolved, in seconds.")]
        [SerializeField] private float deathDurationFallback = 1.5f;

        private float playbackSpeed = 1f;
        private string currentState;

        /// <summary>
        /// Death animation length in seconds, resolved from the controller's Death
        /// clip at Awake. Falls back to <c>1.5</c> when no clip is available.
        /// </summary>
        public float DeathDuration { get; private set; } = 1.5f;

        /// <summary>Last state requested through this facade (debug/verify seam).</summary>
        public string CurrentState => currentState;

        private void Awake()
        {
            if (animator == null) animator = GetComponentInChildren<Animator>(true);
            ResolveDeathDuration();
        }

        private void ResolveDeathDuration()
        {
            DeathDuration = Mathf.Max(0f, deathDurationFallback);
            if (animator == null || animator.runtimeAnimatorController == null) return;

            AnimationClip[] clips = animator.runtimeAnimatorController.animationClips;
            if (clips == null) return;

            float longest = 0f;
            for (int i = 0; i < clips.Length; i++)
            {
                AnimationClip clip = clips[i];
                if (clip == null) continue;
                if (clip.name.IndexOf("Death", System.StringComparison.OrdinalIgnoreCase) < 0) continue;
                if (clip.length > longest) longest = clip.length;
            }

            if (longest > 0f) DeathDuration = longest;
        }

        /// <summary>
        /// Idle ↔ Move. Repeated calls with the same target are cheap no-ops. The
        /// speed argument is kept as the data-driven hook for future locomotion
        /// scaling, but the global <see cref="SetPlaybackSpeed"/> remains the single
        /// owner of the animator rate (boss phase 2 must never be overwritten here).
        /// </summary>
        public void SetMoving(bool moving, float speed)
        {
            string target = moving ? MoveState : IdleState;
            if (currentState != target) CrossFadeSafe(target);
            ApplyAnimatorSpeed(1f);
        }

        /// <summary>Restarts the spawn state (spawned enemies / respawn reset).</summary>
        public void PlaySpawn()
        {
            PlayOneShot(SpawnState);
        }

        /// <summary>Melee attack state (Cross Punch on the X Bot base).</summary>
        public void PlayMelee()
        {
            PlayOneShot(MeleeState);
        }

        /// <summary>Charge state. Placeholder clip until a real shoulder-charge is imported.</summary>
        public void PlayCharge()
        {
            PlayOneShot(ChargeState);
        }

        /// <summary>Ranged aim hold (loop state).</summary>
        public void PlayAim()
        {
            PlayOneShot(AimState);
        }

        /// <summary>Ranged shot. Placeholder clip until a real bow/cast animation is imported.</summary>
        public void PlayShoot()
        {
            PlayOneShot(ShootState);
        }

        /// <summary>Backstab state (Stabbing).</summary>
        public void PlayStab()
        {
            PlayOneShot(StabState);
        }

        /// <summary>Slam state. Placeholder clip until a real ground pound is imported.</summary>
        public void PlaySlam()
        {
            PlayOneShot(SlamState);
        }

        /// <summary>Boss phase-transition roar.</summary>
        public void PlayRoar()
        {
            PlayOneShot(RoarState);
        }

        /// <summary>
        /// Flee state (Puñalada after a backstab). Idempotent because the flee tick
        /// runs every physics step and must not restart the loop.
        /// </summary>
        public void PlayFlee()
        {
            if (currentState == FleeState)
            {
                ApplyAnimatorSpeed(1f);
                return;
            }

            PlayOneShot(FleeState);
        }

        /// <summary>Hit reaction.</summary>
        public void PlayHit()
        {
            PlayOneShot(HitState);
        }

        /// <summary>Death collapse. Read <see cref="DeathDuration"/> to know how long to wait.</summary>
        public void PlayDeath()
        {
            PlayOneShot(DeathState);
        }

        /// <summary>Returns to the idle loop (spawn/reset seams).</summary>
        public void PlayIdle()
        {
            PlayOneShot(IdleState);
        }

        /// <summary>
        /// Maps an archetype attack kind to its animation state. Melee → Melee,
        /// Charge → Charge, Ranged → Aim, Backstab → Stab.
        /// </summary>
        public void PlayAttack(EnemyAttackKind kind)
        {
            switch (kind)
            {
                case EnemyAttackKind.Charge:
                    PlayCharge();
                    break;
                case EnemyAttackKind.Ranged:
                    PlayAim();
                    break;
                case EnemyAttackKind.Backstab:
                    PlayStab();
                    break;
                default:
                    PlayMelee();
                    break;
            }
        }

        /// <summary>
        /// Cross-fades to <paramref name="state"/> on layer 0. If the state is not
        /// present in the current controller, falls back to <c>T-Pose</c>. Never throws.
        /// </summary>
        public void CrossFadeSafe(string state, float fade = 0.1f)
        {
            if (animator == null)
            {
                currentState = state;
                return;
            }

            if (animator.layerCount <= 0)
            {
                currentState = state;
                return;
            }

            if (string.IsNullOrEmpty(state)) state = FallbackState;

            int hash = Animator.StringToHash(state);
            if (!animator.HasState(0, hash))
            {
                state = FallbackState;
                hash = Animator.StringToHash(state);
                if (!animator.HasState(0, hash))
                {
                    currentState = state;
                    return;
                }
            }

            currentState = state;
            animator.CrossFade(hash, Mathf.Max(0f, fade), 0, 0f);
        }

        /// <summary>
        /// Global animator playback speed (boss phase 2 uses 1.25). Idle/move/one-shot
        /// calls multiply this base instead of overwriting it.
        /// </summary>
        public void SetPlaybackSpeed(float speed)
        {
            playbackSpeed = Mathf.Max(0.01f, speed);
            ApplyAnimatorSpeed(1f);
        }

        private void PlayOneShot(string state)
        {
            ApplyAnimatorSpeed(1f);
            CrossFadeSafe(state);
        }

        private void ApplyAnimatorSpeed(float moveFactor)
        {
            if (animator != null) animator.speed = playbackSpeed * moveFactor;
        }
    }
}
