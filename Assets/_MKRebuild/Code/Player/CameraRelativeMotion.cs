using UnityEngine;

namespace MK.Rebuild
{
    /// <summary>
    /// M1 slice-3octies shared CAMERA-RELATIVE input direction helper (2026-10-13).
    /// Extracted from <see cref="PlayerMotor"/> so the SAME stick-to-world mapping
    /// serves both the walk wish direction AND the DASH direction — one source of
    /// truth, no new system.
    ///
    /// Pure query: given the camera transform and the raw stick (x = right, y =
    /// forward, as read from the Move action), it returns a normalized XZ world
    /// direction on the camera's ground plane. Zero when there is no input / no
    /// camera basis (the caller then falls back to its own facing).
    /// </summary>
    public static class CameraRelativeMotion
    {
        /// <summary>
        /// Camera-relative wish direction on XZ (UNIT vector, zero with no input).
        /// <paramref name="stick"/> is the raw Move value (y = forward, x = right);
        /// <paramref name="camera"/> is the reference transform (Camera.main).
        /// </summary>
        public static Vector3 WishDirection(Transform camera, Vector2 stick)
        {
            if (stick.sqrMagnitude < 0.000001f) return Vector3.zero;
            Vector3 forward = camera != null ? camera.forward : Vector3.forward;
            forward.y = 0f;
            if (forward.sqrMagnitude < 0.000001f) forward = Vector3.forward;
            forward.Normalize();
            Vector3 right = new Vector3(forward.z, 0f, -forward.x);
            Vector3 dir = forward * stick.y + right * stick.x;
            return dir.sqrMagnitude > 0.000001f ? dir.normalized : Vector3.zero;
        }
    }
}
