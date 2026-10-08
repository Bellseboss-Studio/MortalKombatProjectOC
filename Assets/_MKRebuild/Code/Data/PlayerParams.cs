using UnityEngine;

namespace MK.Rebuild
{
    [CreateAssetMenu(fileName = "PlayerParams", menuName = "MK/Rebuild/PlayerParams")]
    public sealed class PlayerParams : ScriptableObject
    {
        [Header("Locomotion")]
        [Tooltip("Walk speed in m/s.")]
        public float walkSpeed = 6.0f;

        [Tooltip("Takeoff velocity in m/s (NOT height). apex = v^2 / 2g.")]
        public float jumpVelocity = 8.0f;

        [Tooltip("Gravity applied when airborne (negative Y, m/s^2).")]
        public float gravity = -20f;

        [Header("Timing")]
        [Tooltip("Seconds a jump press stays buffered while airborne.")]
        public float jumpBuffer = 0.15f;

        [Tooltip("ACTIVE since M0 feel-calibration (Mario jump): grace time to jump after leaving ground.")]
        public float coyoteTime = 0.12f;

        [Header("Mario jump (tunables)")]
        [Tooltip("Descent gravity multiplier: gravity x this while falling (snap down).")]
        public float fallGravityMultiplier = 2.2f;

        [Tooltip("Releasing Jump while ascending scales upward velocity by this (short hop).")]
        [Range(0f, 1f)]
        public float jumpCutMultiplier = 0.45f;

        [Tooltip("Fastest allowed fall speed (negative Y, m/s).")]
        public float maxFallSpeed = -25f;

        [Header("Feel (tunables)")]
        [Tooltip("Seconds to reach full speed (ground).")]
        public float accelTime = 0.08f;

        [Tooltip("Seconds to stop from full speed (ground).")]
        public float decelTime = 0.10f;

        [Tooltip("Ground accel multiplier while airborne (0-1).")]
        [Range(0f, 1f)]
        public float airControlFactor = 0.66f;

        [Tooltip("Visual facing rotation speed (deg/s).")]
        public float turnSpeed = 720f;

        [Header("Wall motion (M0)")]
        [Tooltip("Forward probe distance for wall contact (m).")]
        public float wallCheckDistance = 0.7f;

        [Tooltip("Max |normal.y| that still counts as a wall (floor/ceiling rejected).")]
        [Range(0f, 1f)]
        public float wallNormalThreshold = 0.5f;

        [Tooltip("Hang-grab vertical speed while in wall contact (0 = full stop; negative = slide).")]
        public float wallSlideSpeed = 0.0f;

        [Tooltip("Wall-jump takeoff velocity (m/s).")]
        public float wallJumpVelocity = 8.0f;

        [Tooltip("Wall-jump horizontal push-off away from the wall (m/s).")]
        public float wallJumpPushOff = 6.0f;

        [Tooltip("Seconds of horizontal control lockout after a wall-jump.")]
        public float wallJumpLockTime = 0.15f;

        [Tooltip("Seconds after a wall-jump before the same wall can be re-grabbed.")]
        public float wallReGrabDelay = 0.20f;

        [Tooltip("Grace time to chain a wall-jump after losing wall contact.")]
        public float wallCoyoteTime = 0.12f;

        [Tooltip("Small up+away hop applied when releasing a wall with Down (m/s).")]
        public float wallReleaseHop = 3.0f;

        [Tooltip("Move.y at or below this while hanging releases the wall (Down).")]
        public float wallReleaseThreshold = -0.5f;

        [Header("Ledge (mantle)")]
        [Tooltip("True = auto-mantle the top on contact; false = hang + Jump to mantle (default).")]
        public bool autoMantle = false;

        [Tooltip("Auto-mantle upward velocity (m/s).")]
        public float ledgeMantleVelocity = 7.0f;

        [Tooltip("Auto-mantle forward push over the top (m/s).")]
        public float ledgeMantlePushOff = 3.0f;

        [Tooltip("Seconds of horizontal control lockout during the mantle.")]
        public float ledgeMantleLockTime = 0.25f;
    }
}
