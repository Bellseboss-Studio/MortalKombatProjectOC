using Unity.Cinemachine;
using UnityEngine;

namespace MK.Rebuild
{
    /// <summary>
    /// Thin code owner for the scene-authored Cinemachine rig (m3-build-feedback camera
    /// baseline). It does NOT replace Cinemachine: the vcam keeps its authored offset,
    /// damping, aim and lens. This component only owns the seams gameplay needs — target
    /// reassignment and the respawn teleport warp — so the rig stops being YAML-only.
    ///
    /// Execution order 200 keeps the target/basis queries stable for movement and combat
    /// (which resolve their input basis in <c>Awake</c>). No per-frame work.
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(200)]
    public sealed class CameraRig : MonoBehaviour
    {
        [Tooltip("Virtual camera this rig owns. Null = auto-resolved (same GameObject, then first in scene).")]
        [SerializeField] private CinemachineCamera vcam;

        /// <summary>
        /// Tracked target of the owned vcam (null when the vcam has no follow target).
        /// Read-only seam: the rig may change the target, but never the authored feel.
        /// </summary>
        public Transform Target => vcam != null ? vcam.Follow : null;

        /// <summary>
        /// Transform that defines the camera-relative input basis. Uses the vcam's own
        /// transform (not <c>Camera.main</c>) so movement/combat stay in sync with the
        /// authored rig, including while the Brain blends.
        /// </summary>
        public Transform Basis => vcam != null ? vcam.transform : transform;

        private void Awake()
        {
            if (vcam == null) vcam = GetComponent<CinemachineCamera>();
            if (vcam == null) vcam = FindFirstObjectByType<CinemachineCamera>();
        }

        /// <summary>Changes the tracked target without touching the authored lens/damping.</summary>
        public void SetTarget(Transform newTarget)
        {
            if (newTarget == null || vcam == null) return;
            vcam.Follow = newTarget;
            vcam.LookAt = newTarget;
        }

        /// <summary>
        /// Teleport seam in delta form: shifts the vcam by the same delta the tracked target
        /// moved. Callers must capture the delta BEFORE moving the player: an absolute warp
        /// after the move computes zero and the camera drags across the level.
        /// </summary>
        public void WarpBy(Vector3 delta)
        {
            if (vcam == null) return;
            Transform t = Target;
            if (t == null) return;
            vcam.OnTargetObjectWarped(t, delta);
            vcam.PreviousStateIsValid = false;
        }

        /// <summary>Teleport seam: shifts the vcam by the same delta as the target, then invalidates the previous state.</summary>
        public void WarpTo(Vector3 worldPosition)
        {
            Transform t = Target;
            if (t == null) return;
            WarpBy(worldPosition - t.position);
        }
    }
}
