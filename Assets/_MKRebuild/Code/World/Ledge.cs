using UnityEngine;

namespace MK.Rebuild
{
    /// <summary>
    /// M0 marker: this object is a LEDGE — a hang/mantle edge. Implemented as a
    /// thin TRIGGER zone the designer places at the lip (no top-edge math).
    /// Overlapping it while airborne hangs the player; Jump mantles over it.
    /// Composition with <see cref="Grabbable"/>:
    /// - Grabbable only = climbable sides, NO mantle at the top.
    /// - Ledge only = hang/mantle that edge without it being a climbable wall.
    /// - Both = climb + mantle (full behaviour).
    /// Intentionally a marker component (not a layer) to keep the wall-motion
    /// feature isolated from ProjectSettings.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class Ledge : MonoBehaviour
    {
    }
}
