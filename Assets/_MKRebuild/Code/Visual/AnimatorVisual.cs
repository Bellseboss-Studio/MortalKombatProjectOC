using UnityEngine;

namespace MK.Rebuild
{
    /// <summary>
    /// Real-art driver (2026-10-07): pushes movement signals into Leena.controller
    /// and rotates the facing root at turnSpeed — same rotation contract the
    /// ProxyVisualAdapter had (it stays as the hidden fallback).
    /// The Animator lives on the model root (ArtRoot/Leena_Model); the facing root
    /// defaults to this transform (the Player root), so the whole visual turns.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class AnimatorVisual : MonoBehaviour, ICharacterVisual
    {
        [Tooltip("Animator that plays Leena.controller (on the model root, child of ArtRoot). Defaults to GetComponent on Awake.")]
        [SerializeField] private Animator animator;

        [Tooltip("Root that rotates to face movement. Prefer a non-physics container (ArtRoot): " +
                 "Unity 6000 physics reverts direct transform.rotation on the dynamic Player root. " +
                 "Defaults to the ArtRoot child, else this transform (M0 behavior).")]
        [SerializeField] private Transform facingRoot;

        [Tooltip("Degrees per second for facing rotation. <=0 reads PlayerParams.turnSpeed at runtime.")]
        [SerializeField] private float turnSpeed = 0f;

        private static readonly int SpeedHash = Animator.StringToHash("Speed");
        private static readonly int GroundedHash = Animator.StringToHash("Grounded");
        private static readonly int MoveStateHash = Animator.StringToHash("MoveState");
        private static readonly int VerticalSpeedHash = Animator.StringToHash("VerticalSpeed");
        private static readonly int RunningHash = Animator.StringToHash("Running");
        private static readonly int JumpStartSpeedHash = Animator.StringToHash("JumpStartSpeed");
        private static readonly int FlipMidAirSpeedHash = Animator.StringToHash("FlipMidAirSpeed");
        private static readonly int AttackStepHash = Animator.StringToHash("AttackStep");
        private static readonly int AttackP1SpeedHash = Animator.StringToHash("AttackP1Speed");
        private static readonly int AttackP2SpeedHash = Animator.StringToHash("AttackP2Speed");
        private static readonly int AttackP3SpeedHash = Animator.StringToHash("AttackP3Speed");

        private PlayerParams paramsRef;
        private MoveState moveState = MoveState.Free;

        public Vector3 Facing => (facingRoot != null ? facingRoot : transform).forward;

        /// <summary>Last movement state pushed by PlayerMotor (animation hook seam).</summary>
        public MoveState CurrentMoveState => moveState;

        /// <summary>Inject params so turnSpeed stays data-driven (called by PlayerMotor).</summary>
        public void Bind(PlayerParams paramsAsset)
        {
            paramsRef = paramsAsset;
            ApplyJumpClipSpeeds();
        }

        /// <summary>
        /// Pushes the SO jump-clip speed multipliers into the Animator state speed
        /// parameters (TEMPORARY retiming — 1 means normal playback). Static values:
        /// set once, not per frame. Easy revert: set the SO fields back to 1.
        /// </summary>
        private void ApplyJumpClipSpeeds()
        {
            if (animator == null || paramsRef == null) return;
            animator.SetFloat(JumpStartSpeedHash, paramsRef.jumpStartSpeed);
            animator.SetFloat(FlipMidAirSpeedHash, paramsRef.flipMidAirSpeed);
        }

        private void Awake()
        {
            if (animator == null) animator = GetComponent<Animator>();
            if (facingRoot == null)
            {
                // Unity 6000 physics reverts transform.rotation on the dynamic root:
                // rotate the art container when present (ArtRoot), else fall back to
                // this transform to keep the M0 contract.
                var artRoot = transform.Find("ArtRoot");
                facingRoot = artRoot != null ? artRoot : transform;
            }
            // No root motion in this rig (clips have none baked); enforce it anyway.
            if (animator != null) animator.applyRootMotion = false;
            // In case Bind ran before Awake (component order), apply the jump speeds.
            ApplyJumpClipSpeeds();
        }

        public void SetFacing(Vector3 worldDirection)
        {
            if (worldDirection.sqrMagnitude < 0.0001f) return;
            var root = facingRoot != null ? facingRoot : transform;
            float speed = turnSpeed > 0f ? turnSpeed
                : (paramsRef != null ? paramsRef.turnSpeed : 720f);
            var target = Quaternion.LookRotation(worldDirection.normalized, Vector3.up);
            root.rotation = Quaternion.RotateTowards(root.rotation, target, speed * Time.deltaTime);
        }

        public void SetGrounded(bool grounded)
        {
            if (animator != null) animator.SetBool(GroundedHash, grounded);
        }

        public void SetMoveState(MoveState state)
        {
            moveState = state;
            if (animator != null) animator.SetInteger(MoveStateHash, (int)state);
        }

        public void SetSpeed(float metersPerSecond)
        {
            if (animator != null) animator.SetFloat(SpeedHash, metersPerSecond);
        }

        public void SetVerticalSpeed(float verticalVelocity)
        {
            if (animator != null) animator.SetFloat(VerticalSpeedHash, verticalVelocity);
        }

        public void SetRunning(bool running)
        {
            if (animator != null) animator.SetBool(RunningHash, running);
        }

        /// <summary>
        /// Code-driven entry to the Attack_P{n} states (m1-combos, decision b):
        /// CrossFade into the state and push the dedicated AttackStep param so the
        /// per-state exits stay gated until <see cref="ReleaseAttack"/>. The
        /// presentation layer owns the Animator — the combat logic only asks.
        /// </summary>
        public void PlayAttack(int step, float crossFadeDuration)
        {
            if (animator == null) return;
            if (step < 1)
            {
                ReleaseAttack();
                return;
            }
            animator.CrossFade("Attack_P" + step, Mathf.Max(0f, crossFadeDuration), 0, 0f);
            animator.SetInteger(AttackStepHash, step);
        }

        /// <summary>Sets AttackStep to 0 (none) so the landing-mirror exits return to locomotion.</summary>
        public void ReleaseAttack()
        {
            if (animator != null) animator.SetInteger(AttackStepHash, 0);
        }

        /// <summary>
        /// Per-step clip speed multiplier mapped to the state's speed parameter
        /// (decision c, 1 = normal playback). Static: set when a step starts.
        /// </summary>
        public void SetAttackSpeed(int step, float multiplier)
        {
            if (animator == null) return;
            float speed = Mathf.Max(0.01f, multiplier);
            switch (step)
            {
                case 1: animator.SetFloat(AttackP1SpeedHash, speed); break;
                case 2: animator.SetFloat(AttackP2SpeedHash, speed); break;
                case 3: animator.SetFloat(AttackP3SpeedHash, speed); break;
            }
        }
    }
}