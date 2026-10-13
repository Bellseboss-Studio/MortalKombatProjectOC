using UnityEngine;

namespace MK.Rebuild
{
    /// <summary>
    /// M1 shared STICK -> <see cref="AttackDirection"/> classifier, RELATIVE TO THE
    /// FACING (follow-up slice, 2026-10-13). Extracted next to
    /// <see cref="CameraRelativeMotion"/> for the same reason: one pure query, one
    /// source of truth for the directional-attack model.
    ///
    /// The raw stick (x = right, y = forward, exactly as read from the Move action) is
    /// first mapped to a WORLD XZ direction (camera-relative, the SAME
    /// <see cref="CameraRelativeMotion"/> the walk uses) and then PROJECTED onto the
    /// character's facing basis:
    ///   - Forward = stick toward the facing (the front);
    ///   - Back    = stick AGAINST the facing (away from the enemy in front) — the launcher;
    ///   - Side    = lateral (left/right collapse into one).
    /// Magnitude below the deadzone -> Neutral. Classifying RELATIVE TO THE FACING keeps
    /// "back" meaning "away from the enemy you are hitting" even MID-COMBO, while the
    /// body already faces the target (the raw camera-relative stick would say "down/back"
    /// regardless of where the character looks).
    /// </summary>
    public static class AttackDirectionClassifier
    {
        /// <summary>Default deadzone (locked 0.35): above walkInputThreshold so a
        /// half-tilted stick walks without triggering a variant.</summary>
        public const float DefaultDeadzone = 0.35f;

        /// <summary>
        /// Maps <paramref name="stick"/> to a directional variant RELATIVE TO
        /// <paramref name="facing"/> (never returns <see cref="AttackDirection.Any"/> —
        /// that is the wildcard, not a stick state). <paramref name="camera"/> is the
        /// reference frame (Camera.main) used to turn the raw stick into a world direction;
        /// when null the world-forward axis is assumed.
        /// </summary>
        public static AttackDirection FromStick(Vector2 stick, Vector3 facing, Transform camera, float deadzone = DefaultDeadzone)
        {
            if (stick.magnitude < deadzone) return AttackDirection.Neutral;

            Vector3 dir = CameraRelativeMotion.WishDirection(camera, stick);
            if (dir.sqrMagnitude < 0.000001f) return AttackDirection.Neutral;

            Vector3 fwd = facing;
            fwd.y = 0f;
            if (fwd.sqrMagnitude < 0.000001f) fwd = Vector3.forward;
            fwd.Normalize();
            Vector3 right = new Vector3(fwd.z, 0f, -fwd.x);

            float f = Vector3.Dot(dir, fwd);
            float r = Vector3.Dot(dir, right);
            if (Mathf.Abs(f) >= Mathf.Abs(r))
                return f >= 0f ? AttackDirection.Forward : AttackDirection.Back;
            return AttackDirection.Side;
        }
    }
}
