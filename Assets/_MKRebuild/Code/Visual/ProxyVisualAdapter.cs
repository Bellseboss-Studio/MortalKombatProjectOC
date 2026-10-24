using UnityEngine;

namespace MK.Rebuild
{
    /// <summary>
    /// M0 proxy: wraps the existing Gym capsule Renderer + Face child.
    /// Rotation happens on the ROOT transform (Rigidbody-friendly);
    /// meshes are only referenced, never driven by gameplay numbers.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ProxyVisualAdapter : MonoBehaviour, ICharacterVisual
    {
        [Tooltip("Root that rotates to face movement (defaults to this transform).")]
        [SerializeField] private Transform facingRoot;

        [Tooltip("Degrees per second for facing rotation. <=0 reads PlayerParams.turnSpeed at runtime.")]
        [SerializeField] private float turnSpeed = 0f;

        private PlayerParams paramsRef;
        private MoveState moveState = MoveState.Free;

        public Vector3 Facing => (facingRoot != null ? facingRoot : transform).forward;

        /// <summary>Last movement state pushed by PlayerMotor (animation hook seam).</summary>
        public MoveState CurrentMoveState => moveState;

        /// <summary>Inject params so turnSpeed stays data-driven (called by PlayerMotor).</summary>
        public void Bind(PlayerParams paramsAsset) => paramsRef = paramsAsset;

        private void Awake()
        {
            if (facingRoot == null) facingRoot = transform;
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

        public void SetGrounded(bool grounded) { /* M0: no animation hooks */ }

        public void SetMoveState(MoveState state) => moveState = state;

        public void SetSpeed(float metersPerSecond) { /* Fallback proxy: no animator hooks */ }

        public void SetVerticalSpeed(float verticalVelocity) { /* Fallback proxy: no animator hooks */ }

        public void SetRunning(bool running) { /* Fallback proxy: no animator hooks */ }
    }
}
