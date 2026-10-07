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
    }
}
