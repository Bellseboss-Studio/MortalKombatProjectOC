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
    /// Runtime-only: this type never references the Debug assembly. Slice 2 (SectionReset)
    /// and slice 3 (StyleMeter.ResetRank) plug into <see cref="RespawnRoutine"/> later.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CheckpointSystem : MonoBehaviour
    {
        [Tooltip("Player hurt path that signals death. Null = auto-resolved (this GameObject).")]
        [SerializeField] private PlayerDamage damage;

        [Tooltip("Player movement owner the respawn teleports. Null = auto-resolved (this GameObject).")]
        [SerializeField] private PlayerMotor motor;

        [Tooltip("Seconds between death and the respawn teleport, so the death clip reads before the revive crossfade.")]
        [SerializeField] private float respawnDelay = 0.9f;

        [Tooltip("CrossFade duration out of the held Death pose into locomotion on revive.")]
        [SerializeField] private float respawnCrossFade = 0.15f;

        [Tooltip("Brief post-respawn invulnerability window (seconds).")]
        [SerializeField] private float invulnSeconds = 1f;

        private Vector3 checkpointPosition;
        private Vector3 checkpointFacing;
        private int checkpointSectionId;
        private bool captured;
        private bool respawning;

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

        private void Awake()
        {
            if (damage == null) damage = GetComponent<PlayerDamage>();
            if (motor == null) motor = GetComponent<PlayerMotor>();
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

            if (motor != null)
            {
                motor.InputLocked = true;
                // Teleport-in-place: zero the corpse velocity so a mid-air death does not
                // slide during the delay. Facing (zero arg) is left untouched.
                motor.Teleport(transform.position);
            }

            StartCoroutine(RespawnRoutine());
        }

        /// <summary>
        /// The respawn flow (1:1 with the design order): wait so the death clip reads,
        /// teleport to the checkpoint pose, restore full HP, open the invulnerability
        /// window, revive out of the Death pose, then hand control back. Section reset
        /// (slice 2) and rank reset (slice 3) slot in before the teleport later.
        /// </summary>
        private IEnumerator RespawnRoutine()
        {
            yield return new WaitForSeconds(Mathf.Max(0f, respawnDelay));

            if (motor != null) motor.Teleport(checkpointPosition, checkpointFacing);
            if (damage != null)
            {
                damage.Heal();
                damage.GrantInvulnerability(invulnSeconds);
                damage.Revive(respawnCrossFade);
            }
            if (motor != null) motor.InputLocked = false;

            respawning = false;
        }
    }
}
