using UnityEngine;

namespace MK.Rebuild
{
    /// <summary>
    /// Logical movement state exposed to the visual layer so the animator can pick a
    /// clip without inferring it. <see cref="Free"/> covers both ground and air;
    /// keep <see cref="ICharacterVisual.SetGrounded"/> in parallel for that split.
    /// </summary>
    public enum MoveState
    {
        /// <summary>Grounded or airborne with no wall/ledge contact.</summary>
        Free,
        /// <summary>Sticky wall grab (Grabbable side).</summary>
        WallGrab,
        /// <summary>Scripted wall-jump flight (facing held toward the destination wall).</summary>
        WallJump,
        /// <summary>Hanging from a Ledge trigger.</summary>
        LedgeHang,
        /// <summary>Animation-driven timed warp over a ledge.</summary>
        Mantle
    }

    /// <summary>
    /// Boundary between gameplay logic and presentation.
    /// Logic (PlayerMotor) NEVER touches meshes — it talks to this interface.
    /// M0 implementation: <see cref="ProxyVisualAdapter"/> over the Gym capsule.
    /// Real art: <see cref="AnimatorVisual"/> (drives Leena.controller).
    /// </summary>
    public interface ICharacterVisual
    {
        /// <summary>World-space facing direction (Face marks the front).</summary>
        Vector3 Facing { get; }

        /// <summary>Rotate the visual toward a world-space direction.</summary>
        void SetFacing(Vector3 worldDirection);

        /// <summary>Grounded flag for animation hooks (subdivides <see cref="MoveState.Free"/>).</summary>
        void SetGrounded(bool grounded);

        /// <summary>Movement state for animation hooks (clip selection).</summary>
        void SetMoveState(MoveState state);

        /// <summary>
        /// Horizontal speed in m/s (XZ magnitude of <c>rb.linearVelocity</c>).
        /// Drives Idle ↔ Walk on the Animator. Added 2026-10-07 (art real).
        /// </summary>
        void SetSpeed(float metersPerSecond);

        /// <summary>
        /// Vertical velocity in m/s (<c>rb.linearVelocity.y</c>).
        /// Drives the jump sub-phases (JumpStart → FlipMidAir → Landing) via
        /// controller transitions. Added 2026-10-07 (art real).
        /// </summary>
        void SetVerticalSpeed(float verticalVelocity);

        /// <summary>
        /// True while the move input is in the RUN zone (fixed <c>runSpeed</c>).
        /// Lets the controller pick Run ↔ Walk/RunStop from the discrete input zone
        /// instead of inferring it from speed. Added 2026-10-08.
        /// </summary>
        void SetRunning(bool running);
    }
}
