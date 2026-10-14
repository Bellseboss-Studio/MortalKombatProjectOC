using UnityEngine;

namespace MK.Rebuild
{
    /// <summary>
    /// M0 marker: this object is a LEDGE — a hang/mantle edge. Implemented as a
    /// thin TRIGGER zone the designer places at the lip (no top-edge math).
    /// Overlapping it while FALLING onto the lip hangs the player (rising jumps
    /// pass through — no auto-hang on the way up); Jump mantles over it. After
    /// a Down-release the ledge ignores the player briefly (drop-through).
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
