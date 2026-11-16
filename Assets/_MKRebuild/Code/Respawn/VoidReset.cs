using UnityEngine;

namespace MK.Rebuild
{
    /// <summary>
    /// Safety net for the dungeon: any body that falls into this volume is returned to the
    /// player's LAST checkpoint instead of falling forever.
    ///
    /// It reads the checkpoint from <see cref="CheckpointSystem"/> — the same owner the death
    /// respawn uses — so falling and dying always send the player to the same place. The
    /// camera is warped by the same delta, otherwise it would glide across the level.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider))]
    public sealed class VoidReset : MonoBehaviour
    {
        [Tooltip("Respawn position used when the player has no CheckpointSystem (fallback only).")]
        [SerializeField] private Vector3 fallbackPosition = new Vector3(0f, 1.2f, 0f);

        private void Reset()
        {
            var col = GetComponent<Collider>();
            if (col != null) col.isTrigger = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other == null) return;

            var motor = other.GetComponentInParent<PlayerMotor>();
            if (motor == null) return;

            var checkpoints = other.GetComponentInParent<CheckpointSystem>();
            Vector3 target = checkpoints != null ? checkpoints.CheckpointPosition : fallbackPosition;
            Vector3 facing = checkpoints != null ? checkpoints.CheckpointFacing : Vector3.forward;

            // Capture before the move: WarpBy needs the delta the player actually travelled.
            Vector3 previous = motor.transform.position;
            motor.Teleport(target, facing);
            Vector3 delta = target - previous;

            CameraDirector director = CameraDirector.Instance;
            if (director != null)
            {
                director.WarpBy(delta);
                return;
            }

            CameraRig rig = FindFirstObjectByType<CameraRig>();
            if (rig != null) rig.WarpBy(delta);
        }
    }
}
