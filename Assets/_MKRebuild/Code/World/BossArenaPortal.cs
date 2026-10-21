using UnityEngine;

namespace MK.Rebuild
{
    /// <summary>
    /// One-way teleport gate between the S10 landing and the boss arena.
    /// A trigger volume moves the player to <see cref="destination"/> and warps
    /// the camera by the same delta so the Cinemachine blend does not drag the
    /// view across the level. A short cooldown prevents double-firing when the
    /// player is teleported into another portal trigger.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class BossArenaPortal : MonoBehaviour
    {
        [Tooltip("Where the player appears. The forward axis is used as facing.")]
        [SerializeField] private Transform destination;

        [Tooltip("Seconds before this portal can fire again.")]
        [SerializeField] private float cooldownSeconds = 1f;

        // Shared across every portal so a return teleport into another gate's
        // trigger cannot bounce the player straight back.
        private static float nextGlobalUse;

        private float nextUse;

        private void OnTriggerEnter(Collider other)
        {
            if (other == null || destination == null) return;
            if (Time.time < nextUse || Time.time < nextGlobalUse) return;

            var player = other.GetComponentInParent<PlayerDamage>();
            if (player == null) return;

            var motor = player.GetComponent<PlayerMotor>();
            if (motor == null) return;

            motor.Teleport(destination.position, destination.forward);

            var rig = FindFirstObjectByType<CameraRig>();
            rig?.WarpTo(destination.position);

            nextUse = Time.time + cooldownSeconds;
            nextGlobalUse = nextUse;
        }
    }
}
