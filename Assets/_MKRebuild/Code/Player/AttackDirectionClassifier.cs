using UnityEngine;

namespace MK.Rebuild
{
    /// <summary>
    /// M1 slice-3nonies shared STICK -> <see cref="AttackDirection"/> classifier.
    /// Extracted next to <see cref="CameraRelativeMotion"/> for the same reason: one
    /// pure query, one source of truth for the directional-attack model.
    ///
    /// Classifies the RAW stick (x = right, y = forward, exactly as read from the
    /// Move action) in the FRAME OF THE PRESS. The stick is intrinsically
    /// camera-relative (the camera is fixed by design), so classifying here avoids
    /// depending on the character facing (which already follows the stick while
    /// moving and would degenerate "forward").
    ///
    /// Rule (locked decision a): magnitude below the deadzone -> Neutral;
    /// otherwise the dominant axis decides: |y| &gt;= |x| -> Forward (y &gt; 0) or
    /// Back (y &lt; 0); |x| &gt; |y| -> Side (left/right collapse into one).
    /// </summary>
    public static class AttackDirectionClassifier
    {
        /// <summary>Default deadzone (locked 0.35): above walkInputThreshold so a
        /// half-tilted stick walks without triggering a variant.</summary>
        public const float DefaultDeadzone = 0.35f;

        /// <summary>
        /// Maps <paramref name="stick"/> to a directional variant (never returns
        /// <see cref="AttackDirection.Any"/> — that is the wildcard, not a stick state).
        /// </summary>
        public static AttackDirection FromStick(Vector2 stick, float deadzone = DefaultDeadzone)
        {
            if (stick.magnitude < deadzone) return AttackDirection.Neutral;

            float ax = Mathf.Abs(stick.x);
            float ay = Mathf.Abs(stick.y);
            if (ay >= ax) return stick.y >= 0f ? AttackDirection.Forward : AttackDirection.Back;
            return AttackDirection.Side;
        }
    }
}
