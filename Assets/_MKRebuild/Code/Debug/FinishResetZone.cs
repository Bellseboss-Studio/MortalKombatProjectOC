using UnityEngine;

namespace MK.Rebuild.Debugging
{
    /// <summary>
    /// M0 track-only finish reset for <c>Playground.unity</c>: a <see cref="PlayerMotor"/>
    /// body touching this trigger (the S8 arch) is teleported back to the S0 track start
    /// AND the section is restored via the RUNTIME <see cref="SectionReset"/> (dummies at
    /// full HP / initial pose, extras removed) so the track is replayable. FULL reset: no
    /// checkpoints, no menus, no fades.
    ///
    /// m2-respawn slice 2: this is now a THIN WRAPPER. The dummy snapshot/restore logic
    /// that used to live here was PROMOTED (moved unchanged) into the runtime
    /// <see cref="SectionReset"/>; this Debug component keeps only its trigger + the S0
    /// teleport pose and delegates the restore. Runtime never references Debug.
    ///
    /// Wiring: one trigger box at the S8 arch (x ~78, covering the passage). The
    /// <c>sectionReset</c> reference is serialized; when unset it is resolved lazily with
    /// <see cref="Object.FindFirstObjectByType{T}()"/>.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class FinishResetZone : MonoBehaviour
    {
        [Tooltip("S0 track-start pose (Rigidbody center, feet on the slab top).")]
        [SerializeField] private Vector3 respawnPosition = new Vector3(0f, 1f, 0f);

        [Tooltip("Runtime section reset to run on finish. Null = auto-resolved (first in scene).")]
        [SerializeField] private SectionReset sectionReset;

        private void OnTriggerEnter(Collider other)
        {
            if (other == null) return;
            var motor = other.GetComponentInParent<PlayerMotor>();
            if (motor == null) return;
            var rb = motor.GetComponent<Rigidbody>();
            if (rb == null) return;

            rb.position = respawnPosition;
            rb.linearVelocity = Vector3.zero;

            if (sectionReset == null) sectionReset = FindFirstObjectByType<SectionReset>();
            if (sectionReset != null) sectionReset.ResetSection();
        }
    }
}
