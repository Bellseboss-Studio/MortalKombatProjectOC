using UnityEngine;

namespace MK.Rebuild
{
    /// <summary>
    /// Boundary between gameplay logic and presentation.
    /// Logic (PlayerMotor) NEVER touches meshes — it talks to this interface.
    /// M0 implementation: <see cref="ProxyVisualAdapter"/> over the Gym capsule.
    /// </summary>
    public interface ICharacterVisual
    {
        /// <summary>World-space facing direction (Face marks the front).</summary>
        Vector3 Facing { get; }

        /// <summary>Rotate the visual toward a world-space direction.</summary>
        void SetFacing(Vector3 worldDirection);

        /// <summary>Grounded flag for future animation hooks (unused in M0).</summary>
        void SetGrounded(bool grounded);
    }
}
