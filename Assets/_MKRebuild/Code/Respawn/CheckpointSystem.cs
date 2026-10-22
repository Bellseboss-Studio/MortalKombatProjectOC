using System.Collections;
using UnityEngine;

namespace MK.Rebuild
{
    /// <summary>
    /// m2-respawn slice 1: the REVIVE SUBSTRATE. Owns the current respawn target and,
    /// on <see cref="PlayerDamage.OnDied"/>, drives a DEFERRED respawn back to it.
    ///
    /// Implicit checkpoint 0: until a real checkpoint is touched (slice 2), the target
    /// is the player's Awake pose (position + facing, section id 0), captured in
    /// <c>Awake</c>. So a fresh session that dies respawns exactly where it started.
    ///
    /// Why DEFERRED: <see cref="PlayerDamage.Die"/> fires <c>OnDied</c> BEFORE it calls
    /// <c>PlayDeath</c>. Reviving synchronously inside the event would be immediately
    /// clobbered by the death CrossFade. Instead the handler locks input, zeroes the
    /// corpse (teleport-in-place) and starts a coroutine that waits so the death clip
    /// can read, then teleports, heals, grants invulnerability and revives.
    ///
    /// Runtime-only: this type never references the Debug assembly. The SectionReset
    /// seam (slice 2) and the StyleMeter rank→D reset (slice 3) both plug into
    /// <see cref="RespawnRoutine"/>.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CheckpointSystem : MonoBehaviour
    {
        [Tooltip("Player hurt path that signals death. Null = auto-resolved (this GameObject).")]
        [SerializeField] private PlayerDamage damage;

        [Tooltip("Player movement owner the respawn teleports. Null = auto-resolved (this GameObject).")]
        [SerializeField] private PlayerMotor motor;

        [Tooltip("In-flight shove cleared on death so no residual knockback velocity survives. Null = auto-resolved (this GameObject).")]
        [SerializeField] private PlayerKnockback knockback;

        [Tooltip("Runtime section reset run on death (dummies + IResettables). Null = auto-resolved (first in scene), or skipped if none.")]
        [SerializeField] private SectionReset sectionReset;

        [Tooltip("Style meter dropped to rank D on respawn (banked charges kept). Null = auto-resolved (this GameObject).")]
        [SerializeField] private StyleMeter styleMeter;

        [Tooltip("Camera rig to warp on respawn. Null = auto-resolved (first CameraRig in scene).")]
        [SerializeField] private CameraRig cameraRig;

        [Tooltip("OPTIONAL override (seconds) for the wait before the respawn teleport. 0 (default) = " +
                 "DERIVE from AnimatorVisual.DeathDuration (trimmed death clip length / deathClipSpeed), " +
                 "so the death always completes before the revive. Mirrors HazardDefinition.knockbackDuration.")]
        [SerializeField] private float respawnDelay = 0f;

        [Tooltip("CrossFade duration out of the held Death pose into locomotion on revive.")]
        [SerializeField] private float respawnCrossFade = 0.15f;

        [Tooltip("Brief post-respawn invulnerability window (seconds).")]
        [SerializeField] private float invulnSeconds = 1f;

        private AnimatorVisual animatorVisual;
        private Vector3 checkpointPosition;
        private Vector3 checkpointFacing;
        private int checkpointSectionId;
        private bool captured;
        private bool respawning;
        private BodyOwnership.Token deathToken; // body ownership held for the death → respawn window

        /// <summary>
        /// Small margin (seconds) added on top of the DERIVED death duration: the coroutine
        /// starts on the OnDied frame, but the Death CrossFade is fired right after it, so the
        /// clip begins ~one frame later. 0.05 s guarantees the collapse finishes (last frame
        /// shown) before the teleport. Only used on the derived path.
        /// </summary>
        private const float DeathRespawnMargin = 0.05f;

        /// <summary>Current respawn position (implicit checkpoint 0 until one is recorded).</summary>
        public Vector3 CheckpointPosition => checkpointPosition;

        /// <summary>Current respawn facing (implicit checkpoint 0 until one is recorded).</summary>
        public Vector3 CheckpointFacing => checkpointFacing;

        /// <summary>Current respawn section id (0 for the implicit checkpoint).</summary>
        public int CheckpointSectionId => checkpointSectionId;

        /// <summary>True while a death → respawn flow is in flight.</summary>
        public bool IsRespawning => respawning;

        /// <summary>True once a respawn target is held (always true after Awake — the implicit checkpoint 0).</summary>
        public bool HasCheckpoint => captured;

        /// <summary>
        /// Effective wait (seconds) between death and the respawn teleport. Derived from the
        /// played animation by default (<see cref="AnimatorVisual.DeathDuration"/> + a one-frame
        /// margin), so the collapse reads fully and the two can never drift; the serialized
        /// <c>respawnDelay</c> is an explicit override (0 = derive).
        /// </summary>
        public float RespawnDelay
        {
            get
            {
                if (respawnDelay > 0f) return respawnDelay;
                if (animatorVisual != null) return animatorVisual.DeathDuration + DeathRespawnMargin;
                return 0.9f; // fallback: no visual driver (proxy) — pre-slice behavior
            }
        }

        private void Awake()
        {
            if (damage == null) damage = GetComponent<PlayerDamage>();
            if (motor == null) motor = GetComponent<PlayerMotor>();
            if (knockback == null) knockback = GetComponent<PlayerKnockback>();
            if (sectionReset == null) sectionReset = FindFirstObjectByType<SectionReset>();
            if (styleMeter == null) styleMeter = GetComponent<StyleMeter>();
            if (cameraRig == null) cameraRig = FindFirstObjectByType<CameraRig>();
            if (animatorVisual == null) animatorVisual = GetComponent<AnimatorVisual>();
            CaptureImplicitCheckpoint();
        }

        private void OnEnable()
        {
            // OnEnable runs after Awake; resolve defensively for domain-reload safety.
            if (damage == null) damage = GetComponent<PlayerDamage>();
            if (damage != null) damage.OnDied += HandleDied;
        }

        private void OnDisable()
        {
            if (damage != null) damage.OnDied -= HandleDied;
        }

        /// <summary>
        /// Implicit checkpoint 0: the player's Awake pose (position + facing) at section 0.
        /// Captured once in Awake; a later <see cref="RecordCheckpoint"/> (slice 2 trigger)
        /// overrides it.
        /// </summary>
        private void CaptureImplicitCheckpoint()
        {
            RecordCheckpoint(transform.position, transform.forward, 0);
        }

        /// <summary>
        /// Sets the current respawn target. Public seam for the slice-2 Checkpoint trigger
        /// (grey-box flash) — the implicit checkpoint lives here for now.
        /// </summary>
        public void RecordCheckpoint(Vector3 position, Vector3 facing, int sectionId)
        {
            checkpointPosition = position;
            checkpointFacing = facing;
            checkpointSectionId = sectionId;
            captured = true;
        }

        /// <summary>
        /// Death handler: locks the corpse (no control during the death → respawn window)
        /// and freezes it in place, then defers the actual respawn to a coroutine so the
        /// death CrossFade (fired right after this event) is not clobbered.
        /// </summary>
        private void HandleDied()
        {
            if (respawning) return;
            respawning = true;

            // DEATH OUTRANKS KNOCKBACK IN PRACTICE (documented exception): the arbiter ranks
            // Knockback (4) ABOVE Death (3), so merely acquiring Death while a shove token is
            // held would leave the shove as the owner. Cancel the shove FIRST — that releases
            // its token and zeroes its horizontal contribution — then Death owns the body.
            // This is exactly the modified checkpoint-respawn requirement "clear any in-flight
            // knockback" / "no residual knockback velocity persists".
            if (knockback != null) knockback.Cancel();

            if (motor != null)
            {
                // Acquire Death (outranks Dash/Attack): the corpse keeps the body lock for
                // the WHOLE respawn window, and each system's own release can no longer
                // clobber it (W1). Released by RespawnRoutine after Revive.
                if (motor.Body != null) deathToken = motor.Body.Acquire(BodyPriority.Death);
                // Teleport-in-place: zero the corpse velocity so a mid-air death does not
                // slide during the delay. Facing (zero arg) is left untouched.
                motor.Teleport(transform.position);
            }

            StartCoroutine(RespawnRoutine());
        }

        /// <summary>
        /// The respawn flow (1:1 with the design order): wait so the death clip reads,
        /// reset the current section, teleport to the checkpoint pose, restore full HP,
        /// open the invulnerability window, revive out of the Death pose, drop the style
        /// rank to D (banked charges kept), then hand control back.
        /// </summary>
        private IEnumerator RespawnRoutine()
        {
            // REALTIME: a pause (timeScale = 0) must not hang the respawn behind the
            // death clip's scaled delay; the corpse stays locked until this completes.
            yield return new WaitForSecondsRealtime(Mathf.Max(0f, RespawnDelay));

            if (sectionReset != null) sectionReset.ResetSection();
            if (motor != null) motor.Teleport(checkpointPosition, checkpointFacing);
            // Shift the vcam by the same delta as the teleport BEFORE the next camera
            // update, so the respawn does not glide across the level (or through geometry).
            if (cameraRig != null) cameraRig.WarpTo(checkpointPosition);
            if (damage != null)
            {
                damage.Heal();
                damage.GrantInvulnerability(invulnSeconds);
                damage.Revive(respawnCrossFade);
            }
            if (styleMeter != null) styleMeter.ResetRank();
            // Release ONLY the death token: any other in-flight owner keeps its lock.
            if (motor != null && motor.Body != null) motor.Body.Release(deathToken);
            deathToken = null;

            respawning = false;
        }
    }
}
