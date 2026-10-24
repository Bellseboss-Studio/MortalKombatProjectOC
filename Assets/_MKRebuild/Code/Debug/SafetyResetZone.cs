using UnityEngine;

namespace MK.Rebuild.Debugging
{
    /// <summary>
    /// M0 track-only safety net for <c>Playground.unity</c>: any
    /// <see cref="PlayerMotor"/> body that touches this trigger volume is
    /// teleported back to the S0 track start (feet pose). This REPLACES the
    /// "land on the slab and jump back up" behavior — the reset fires on
    /// touch, so the player never rests on the S9 slab.
    ///
    /// Track-only: lives in the isolated <c>MK.Rebuild.Debug</c> assembly and
    /// never touches game assemblies' behavior. It only writes the player's
    /// Rigidbody pose/velocity (same seam the debug traverse probes use).
    ///
    /// Wiring: two thin trigger boxes (S9_ResetZoneW/E) at y −1.5..−0.5 under
    /// the track, EXCLUDING the S6 pit column (x 63..66) so the pit still
    /// catches the player instead of resetting them.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SafetyResetZone : MonoBehaviour
    {
        [Tooltip("S0 track-start pose (Rigidbody center, feet on the slab top).")]
        [SerializeField] private Vector3 respawnPosition = new Vector3(0f, 1f, 0f);

        private void OnTriggerEnter(Collider other)
        {
            if (other == null) return;
            var motor = other.GetComponentInParent<PlayerMotor>();
            if (motor == null) return;
            var rb = motor.GetComponent<Rigidbody>();
            if (rb == null) return;

            // Capture before the move: WarpBy needs the delta, and after the teleport the
            // target already sits at the destination (zero delta).
            Vector3 previousPosition = motor.transform.position;
            rb.position = respawnPosition;
            rb.linearVelocity = Vector3.zero;

            Vector3 delta = respawnPosition - previousPosition;
            CameraDirector director = CameraDirector.Instance;
            if (director != null)
            {
                director.WarpBy(delta);
            }
            else
            {
                var rig = FindFirstObjectByType<CameraRig>();
                rig?.WarpBy(delta);
            }
        }
    }
}
