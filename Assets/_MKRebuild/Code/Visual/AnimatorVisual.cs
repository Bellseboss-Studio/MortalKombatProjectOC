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
        private static readonly int MantleClipSpeedHash = Animator.StringToHash("MantleClipSpeed");
        private static readonly int DashSpeedHash = Animator.StringToHash("DashSpeed");
        private static readonly int AttackDashSpeedHash = Animator.StringToHash("AttackDashSpeed");
        private static readonly int LauncherSpeedHash = Animator.StringToHash("LauncherSpeed");
        private static readonly int DeathSpeedHash = Animator.StringToHash("DeathSpeed");
        private static readonly int HitReactionSpeedHash = Animator.StringToHash("HitReactionSpeed");
        private static readonly int AttackStepHash = Animator.StringToHash("AttackStep");
        private static readonly int DashStateHash = Animator.StringToHash("DashState");
        private static readonly int DashAttackStateHash = Animator.StringToHash("DashAttackState");
        private static readonly int LauncherStateHash = Animator.StringToHash("LauncherState");
        private static readonly int AbilityStateHash = Animator.StringToHash("AbilityState");
        private static readonly int AbilitySpeedHash = Animator.StringToHash("AbilitySpeed");
        private static readonly int AttackP1SpeedHash = Animator.StringToHash("AttackP1Speed");
        private static readonly int AttackP2SpeedHash = Animator.StringToHash("AttackP2Speed");
        private static readonly int AttackP3SpeedHash = Animator.StringToHash("AttackP3Speed");

        private PlayerParams paramsRef;
        private MoveState moveState = MoveState.Free;
        private bool loggedLandingRecovery;
        private bool loggedHitReaction;
        private bool loggedDeath;

        /// <summary>
        /// Fallback only: used when the trimmed HitReaction clip cannot be resolved (e.g. a
        /// proxy visual / missing controller). The LIVE lock is always derived from the clip.
        /// </summary>
        private const float HitReactionFallbackSeconds = 0.4f;

        /// <summary>
        /// Fallback only: used when the trimmed Death clip cannot be resolved. The LIVE respawn
        /// delay is always derived from the clip (2.5 s collapse / 3.5 ≈ 0.714 s).
        /// </summary>
        private const float DeathFallbackSeconds = 0.714f;

        /// <summary>
        /// Landing recovery (seconds) for the jump lockout, DERIVED from the real Animator:
        /// the Landing clip's effective duration (<c>clipLength / landingClipSpeed</c>) plus
        /// <see cref="PlayerParams.jumpLockoutExtra"/>. Falls back to
        /// <see cref="PlayerParams.jumpLockout"/> when the clip is not resolved. Read by
        /// PlayerMotor on touchdown (the motor owns no Animator — this layer does).
        /// </summary>
        public float LandingRecoverySeconds { get; private set; }

        /// <summary>
        /// Hit-reaction body-lock duration (seconds), DERIVED from the Animator exactly like
        /// <see cref="LandingRecoverySeconds"/>: the trimmed impact clip's effective duration
        /// (<c>clipLength / hitReactionClipSpeed</c>). This is the ONE source of truth for the
        /// lock: <see cref="PlayerKnockback"/> reads it so the lock and the played clip can
        /// never drift apart. Falls back to a sane constant when the clip is unresolved.
        /// </summary>
        public float HitReactionDuration { get; private set; } = HitReactionFallbackSeconds;

        /// <summary>
        /// Death collapse duration (seconds), DERIVED from the Animator exactly like
        /// <see cref="HitReactionDuration"/>: the trimmed collapse clip's effective duration
        /// (<c>clipLength / deathClipSpeed</c>). This is the ONE source of truth for how long
        /// the corpse holds before the respawn teleport: <see cref="CheckpointSystem"/> reads
        /// it, so the revive can never drift from the played animation. Falls back to a sane
        /// constant when the clip is unresolved.
        /// </summary>
        public float DeathDuration { get; private set; } = DeathFallbackSeconds;

        public Vector3 Facing => (facingRoot != null ? facingRoot : transform).forward;

        /// <summary>Last movement state pushed by PlayerMotor (animation hook seam).</summary>
        public MoveState CurrentMoveState => moveState;

        /// <summary>Inject params so turnSpeed stays data-driven (called by PlayerMotor).</summary>
        public void Bind(PlayerParams paramsAsset)
        {
            paramsRef = paramsAsset;
            ApplyClipSpeeds();
        }

        /// <summary>
        /// Pushes the SO clip speed multipliers into the Animator state speed
        /// parameters (TEMPORARY retiming — 1 means normal playback). Static values:
        /// set once, not per frame. Easy revert: set the SO fields back to 1.
        /// </summary>
        private void ApplyClipSpeeds()
        {
            if (animator == null || paramsRef == null) return;
            animator.SetFloat(JumpStartSpeedHash, paramsRef.jumpStartSpeed);
            animator.SetFloat(FlipMidAirSpeedHash, paramsRef.flipMidAirSpeed);
            animator.SetFloat(MantleClipSpeedHash, paramsRef.mantleClipSpeed);
            animator.SetFloat(DashSpeedHash, paramsRef.dashClipSpeed);
            animator.SetFloat(DeathSpeedHash, DeathClipSpeed);
            animator.SetFloat(HitReactionSpeedHash, HitReactionClipSpeed);
            ResolveLandingRecovery();
            ResolveHitReaction();
            ResolveDeath();
        }

        /// <summary>
        /// Data-driven playback multiplier for the HitReaction state (defaults to 1 when no
        /// params asset is bound, so the clip still plays at natural speed).
        /// </summary>
        private float HitReactionClipSpeed =>
            paramsRef != null ? Mathf.Max(0.0001f, paramsRef.hitReactionClipSpeed) : 1f;

        /// <summary>
        /// Data-driven playback multiplier for the Death state (defaults to 1 when no params
        /// asset is bound, so the clip still plays at natural speed).
        /// </summary>
        private float DeathClipSpeed =>
            paramsRef != null ? Mathf.Max(0.0001f, paramsRef.deathClipSpeed) : 1f;

        /// <summary>
        /// Derives <see cref="DeathDuration"/> from the trimmed collapse clip length at runtime
        /// (same retiming home as <see cref="ResolveHitReaction"/>): the clip is found by name
        /// (contains "dizzy_death_collapse") in the controller's clips, then divided by
        /// <c>deathClipSpeed</c>. This is what makes the respawn delay EQUAL to the played
        /// collapse by construction — no hand-tuned duration anywhere.
        /// </summary>
        private void ResolveDeath()
        {
            float speed = DeathClipSpeed;
            RuntimeAnimatorController controller = animator.runtimeAnimatorController;
            if (controller != null)
            {
                AnimationClip[] clips = controller.animationClips;
                for (int i = 0; i < clips.Length; i++)
                {
                    AnimationClip clip = clips[i];
                    if (clip == null || !clip.name.Contains("dizzy_death_collapse")) continue;
                    DeathDuration = clip.length / speed;
                    if (!loggedDeath)
                    {
                        loggedDeath = true;
                        Debug.Log($"[Death] collapse '{clip.name}' {clip.length:0.000}s / " +
                                  $"speed {speed:0.##} = {DeathDuration:0.000}s");
                    }
                    return;
                }
            }
            DeathDuration = DeathFallbackSeconds; // fallback: clip not found
        }

        /// <summary>
        /// Derives <see cref="HitReactionDuration"/> from the trimmed impact clip length at
        /// runtime (same retiming home as <see cref="ResolveLandingRecovery"/>): the clip is
        /// found by name (contains "Damaged_push_impact") in the controller's clips, then
        /// divided by <c>hitReactionClipSpeed</c>. This is what makes the body lock EQUAL to
        /// the played animation by construction — no hardcoded duration anywhere.
        /// </summary>
        private void ResolveHitReaction()
        {
            float speed = HitReactionClipSpeed;
            RuntimeAnimatorController controller = animator.runtimeAnimatorController;
            if (controller != null)
            {
                AnimationClip[] clips = controller.animationClips;
                for (int i = 0; i < clips.Length; i++)
                {
                    AnimationClip clip = clips[i];
                    if (clip == null || !clip.name.Contains("Damaged_push_impact")) continue;
                    HitReactionDuration = clip.length / speed;
                    if (!loggedHitReaction)
                    {
                        loggedHitReaction = true;
                        Debug.Log($"[HitReaction] impact '{clip.name}' {clip.length:0.000}s / " +
                                  $"speed {speed:0.##} = {HitReactionDuration:0.000}s");
                    }
                    return;
                }
            }
            HitReactionDuration = HitReactionFallbackSeconds; // fallback: clip not found
        }

        /// <summary>
        /// Derives <see cref="LandingRecoverySeconds"/> from the Landing clip length at runtime
        /// (same retiming home as the other clip speeds): the clip is found by name
        /// (contains "Jump_Landing") in the controller's clips, then divided by
        /// <c>landingClipSpeed</c> and offset by <c>jumpLockoutExtra</c>. Falls back to the
        /// serialized <c>jumpLockout</c> when the clip cannot be found.
        /// </summary>
        private void ResolveLandingRecovery()
        {
            float speed = Mathf.Max(0.0001f, paramsRef.landingClipSpeed);
            float extra = Mathf.Max(0f, paramsRef.jumpLockoutExtra);
            RuntimeAnimatorController controller = animator.runtimeAnimatorController;
            if (controller != null)
            {
                AnimationClip[] clips = controller.animationClips;
                for (int i = 0; i < clips.Length; i++)
                {
                    AnimationClip clip = clips[i];
                    if (clip == null || !clip.name.Contains("Jump_Landing")) continue;
                    LandingRecoverySeconds = clip.length / speed + extra;
                    if (!loggedLandingRecovery)
                    {
                        loggedLandingRecovery = true;
                        Debug.Log($"[JumpLockout] landing '{clip.name}' {clip.length:0.000}s / " +
                                  $"speed {speed:0.##} + extra {extra:0.###} = {LandingRecoverySeconds:0.000}s");
                    }
                    return;
                }
            }
            LandingRecoverySeconds = paramsRef.jumpLockout; // fallback: clip not found
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
            // In case Bind ran before Awake (component order), apply the clip speeds.
            ApplyClipSpeeds();
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
            // Ledge-grab entry (ledge-anim-integration, decision b): code-driven
            // CrossFade into LedgeGrab on the rising edge, mirroring PlayAttack —
            // the presentation layer owns the Animator. The controller then flows
            // LedgeGrab → LedgeHang on exit time while MoveState stays 3.
            bool grabEdge = state == MoveState.LedgeHang && moveState != MoveState.LedgeHang;
            moveState = state;
            if (animator == null) return;
            if (grabEdge) animator.CrossFade("LedgeGrab", 0.1f, 0, 0f);
            animator.SetInteger(MoveStateHash, (int)state);
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
        /// DORMANT dash-state hook (m1-aim-assist, decision f): mirrors
        /// <see cref="PlayAttack"/> for the EMPTY <c>Dash</c> state. NOT called by
        /// <see cref="PlayerDash"/> yet — entering the motion-less state FREEZES the
        /// pose, so this waits until a dash clip exists (then: set m_Motion + call
        /// this from StartDash/EndDash). Pushing DashState = 1 lets the controller's
        /// mirror exits (gated <c>DashState == 0</c>) return to locomotion.
        /// </summary>
        public void PlayDash(float crossFadeDuration)
        {
            if (animator == null) return;
            animator.CrossFade("Dash", Mathf.Max(0f, crossFadeDuration), 0, 0f);
            animator.SetInteger(DashStateHash, 1);
        }

        /// <summary>DORMANT: sets DashState back to 0 (none) so the mirror exits return to locomotion.</summary>
        public void ReleaseDash()
        {
            if (animator != null) animator.SetInteger(DashStateHash, 0);
        }

        /// <summary>
        /// Dash-attack opener slot (slice 3octies; clip wired in anim-packs slice 2):
        /// mirrors <see cref="PlayAttack"/> for the <c>Attack_Dash</c> state (the
        /// dash-attack OPENER's own clip slot). Called by <see cref="PlayerCombat"/>
        /// ONLY when the opener step has an assigned clip (<c>ComboStep.clip</c>).
        /// The step's <paramref name="clipSpeed"/> (ComboStep.speedMultiplier) is the
        /// SINGLE retime source: it is pushed into the state's dedicated
        /// <c>AttackDashSpeed</c> float BEFORE the CrossFade. Pushing DashAttackState = 1
        /// lets the controller's mirror exits (gated <c>DashAttackState == 0</c>) return
        /// to locomotion.
        /// </summary>
        public void PlayDashAttack(float crossFadeDuration, float clipSpeed)
        {
            if (animator == null) return;
            animator.SetFloat(AttackDashSpeedHash, Mathf.Max(0.01f, clipSpeed));
            animator.CrossFade("Attack_Dash", Mathf.Max(0f, crossFadeDuration), 0, 0f);
            animator.SetInteger(DashAttackStateHash, 1);
        }

        /// <summary>DORMANT: sets DashAttackState back to 0 (none) so the mirror exits return to locomotion.</summary>
        public void ReleaseDashAttack()
        {
            if (animator != null) animator.SetInteger(DashAttackStateHash, 0);
        }

        /// <summary>
        /// Launcher slot (slice 3nonies; clip wired in anim-packs slice 2): mirrors
        /// <see cref="PlayDashAttack"/> for the <c>Attack_Launcher</c> state (the
        /// launcher/uppercut variant's own clip slot). Called by <see cref="PlayerCombat"/>
        /// ONLY when the launcher step has an assigned clip (<c>ComboStep.clip</c>).
        /// The step's <paramref name="clipSpeed"/> (ComboStep.speedMultiplier) is the
        /// SINGLE retime source: pushed into the state's dedicated <c>LauncherSpeed</c>
        /// float BEFORE the CrossFade. Pushing LauncherState = 1 lets the controller's
        /// mirror exits (gated <c>LauncherState == 0</c>) return to locomotion. Uses a
        /// DEDICATED param (NOT MoveState): the AnyState <c>MoveState != 0</c> would hijack it.
        /// </summary>
        public void PlayLauncher(float crossFadeDuration, float clipSpeed)
        {
            if (animator == null) return;
            animator.SetFloat(LauncherSpeedHash, Mathf.Max(0.01f, clipSpeed));
            animator.CrossFade("Attack_Launcher", Mathf.Max(0f, crossFadeDuration), 0, 0f);
            animator.SetInteger(LauncherStateHash, 1);
        }

        /// <summary>DORMANT: sets LauncherState back to 0 (none) so the mirror exits return to locomotion.</summary>
        public void ReleaseLauncher()
        {
            if (animator != null) animator.SetInteger(LauncherStateHash, 0);
        }

        /// <summary>
        /// Ability slot (m1-abilities; clips wired via anim_tarkatan_shoot / anim_spinEdge):
        /// mirrors <see cref="PlayLauncher"/> for the <c>Attack_H1</c> / <c>Attack_H2</c>
        /// states. <paramref name="kind"/> maps to the state (1 = H1 projectile, 2 = H2 area)
        /// and is pushed into the dedicated <c>AbilityState</c> int so the controller's mirror
        /// exits (gated <c>AbilityState == 0</c>) return to locomotion. A DEDICATED param (NOT
        /// MoveState) is required: the AnyState <c>MoveState != 0</c> would hijack it.
        /// Called by <see cref="PlayerAbilities"/>; the state exists only because a clip was
        /// assigned (never enters a clip-less state that would freeze the pose).
        /// The ability's <paramref name="clipSpeed"/> (AbilityDefinition.clipSpeedMultiplier)
        /// is the SINGLE retime source: pushed into the dedicated <c>AbilitySpeed</c> float
        /// BEFORE the CrossFade (mirrors <see cref="PlayLauncher"/>).
        /// </summary>
        public void PlayAbility(int kind, float crossFadeDuration, float clipSpeed)
        {
            if (animator == null) return;
            if (kind < 1) { ReleaseAbility(); return; }
            animator.SetFloat(AbilitySpeedHash, Mathf.Max(0.01f, clipSpeed));
            animator.CrossFade(kind == 1 ? "Attack_H1" : "Attack_H2",
                Mathf.Max(0f, crossFadeDuration), 0, 0f);
            animator.SetInteger(AbilityStateHash, kind);
        }

        /// <summary>Sets AbilityState back to 0 (none) so the mirror exits return to locomotion.</summary>
        public void ReleaseAbility()
        {
            if (animator != null) animator.SetInteger(AbilityStateHash, 0);
        }

        /// <summary>
        /// Player HURT seam (anim-packs-integration slice 3): code-driven CrossFade into
        /// the <c>HitReaction</c> state — the same pattern as <see cref="PlayAttack"/>.
        /// NO new Animator params: the state's own exit-time transition returns to
        /// locomotion when the one-shot ends. Called by <see cref="PlayerDamage"/>.
        /// </summary>
        public void PlayHitReaction(float crossFadeDuration)
        {
            if (animator == null) return;
            // Push the retime BEFORE entering, so the played clip matches the duration the
            // lock was derived from (same retiming home as DeathSpeed / DashSpeed).
            animator.SetFloat(HitReactionSpeedHash, HitReactionClipSpeed);
            animator.CrossFade("HitReaction", Mathf.Max(0f, crossFadeDuration), 0, 0f);
        }

        /// <summary>
        /// Air-hit seam (m3-build-feedback): code-driven CrossFade into the <c>AirHit</c>
        /// state — the airborne counterpart of <see cref="PlayHitReaction"/>. The controller's
        /// own transition chains <c>AirHit -&gt; GetUp</c> on <c>Grounded == true</c>, so no new
        /// Animator params are pushed from code. Falls back to the ground hit reaction when the
        /// state has not been wired yet, so the hurt flow never freezes on a missing state.
        /// Called by <see cref="PlayerDamage"/>.
        /// </summary>
        public void PlayAirHit(float crossFadeDuration)
        {
            if (animator == null) return;
            if (!HasState("AirHit")) { PlayHitReaction(crossFadeDuration); return; }
            animator.CrossFade("AirHit", Mathf.Max(0f, crossFadeDuration), 0, 0f);
        }

        /// <summary>
        /// Get-up seam (m3-build-feedback): code-driven CrossFade into the <c>GetUp</c> state
        /// when the player lands from an air hit. As with <see cref="PlayHitReaction"/>, the
        /// state's own exit-time transition returns to locomotion when the one-shot ends.
        /// Falls back to <see cref="ResetToLocomotion"/> when the state has not been wired yet.
        /// Called by <see cref="PlayerDamage"/>.
        /// </summary>
        public void PlayGetUp(float crossFadeDuration)
        {
            if (animator == null) return;
            if (!HasState("GetUp")) { ResetToLocomotion(crossFadeDuration); return; }
            animator.CrossFade("GetUp", Mathf.Max(0f, crossFadeDuration), 0, 0f);
        }

        private bool HasState(string stateName)
        {
            return animator != null && animator.HasState(0, Animator.StringToHash(stateName));
        }

        /// <summary>
        /// Player DEATH seam (anim-packs-integration slice 3): code-driven CrossFade into
        /// the <c>Death</c> state. The state has NO outgoing transition, so the clip plays
        /// once and HOLDS its final pose (no T-pose, no snap). Called by
        /// <see cref="PlayerDamage"/>. The state's playback rate is the dedicated
        /// <c>DeathSpeed</c> float param, driven by <c>PlayerParams.deathClipSpeed</c> (data,
        /// not code). It is re-pushed BEFORE entering (mirrors <see cref="PlayHitReaction"/>),
        /// so the played clip matches the duration <see cref="DeathDuration"/> was derived
        /// from — the respawn delay can never drift from the animation.
        /// </summary>
        public void PlayDeath(float crossFadeDuration)
        {
            if (animator == null) return;
            animator.SetFloat(DeathSpeedHash, DeathClipSpeed);
            animator.CrossFade("Death", Mathf.Max(0f, crossFadeDuration), 0, 0f);
        }

        /// <summary>
        /// REVIVE seam (m2-respawn slice 1): forces the Animator OUT of the held Death
        /// pose and back into locomotion. The <c>Death</c> state has NO outgoing transition
        /// (Leena.controller), so this uses a forced <see cref="Animator.CrossFade"/> to
        /// the real <c>Idle</c> state — bypassing the authored transition graph, no
        /// controller asset edit and no new parameter. <c>MoveState</c>/<c>Speed</c> are
        /// settled to Free/0 so the controller enters Idle (not a stale Walk/Run) and the
        /// next FixedUpdate resumes normally from input. Called by
        /// <see cref="PlayerDamage.Revive"/>.
        /// </summary>
        public void ResetToLocomotion(float crossFadeDuration)
        {
            if (animator == null) return;
            moveState = MoveState.Free;
            animator.SetInteger(MoveStateHash, (int)MoveState.Free);
            animator.SetFloat(SpeedHash, 0f);
            animator.SetFloat(VerticalSpeedHash, 0f);
            animator.SetBool(RunningHash, false);
            animator.CrossFade("Idle", Mathf.Max(0f, crossFadeDuration), 0, 0f);
        }

        /// <summary>
        /// Instantly snaps the facing root to a world direction (m2-respawn slice 1):
        /// unlike <see cref="SetFacing"/> (which turns at <c>turnSpeed</c> per frame),
        /// this sets the rotation in one shot so a respawn restores the checkpoint's
        /// captured facing immediately. Used by <see cref="PlayerMotor.Teleport"/>.
        /// </summary>
        public void SnapFacing(Vector3 worldDirection)
        {
            if (worldDirection.sqrMagnitude < 0.0001f) return;
            var root = facingRoot != null ? facingRoot : transform;
            root.rotation = Quaternion.LookRotation(worldDirection.normalized, Vector3.up);
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