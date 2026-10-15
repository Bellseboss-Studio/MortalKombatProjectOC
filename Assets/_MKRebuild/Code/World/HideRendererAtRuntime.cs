using UnityEngine;

namespace MK.Rebuild
{
    /// <summary>
    /// Dev/editor helper for the M0 functional markers (<see cref="Grabbable"/>,
    /// <see cref="Ledge"/>): hides the marker's Renderer(s) when the game STARTS
    /// (Awake), so the color-coded reference geometry is visible/editable in the
    /// Editor but produces NO render noise in Play mode.
    /// Colliders and the marker components stay active — only the VISUALS are
    /// hidden, so the grab/ledge detection keeps working off the marker geometry.
    /// Runtime-only: no ExecuteInEditMode, so stopping Play restores visibility.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class HideRendererAtRuntime : MonoBehaviour
    {
        [Tooltip("Renderers hidden on Awake. Empty = this object's own Renderer(s).")]
        [SerializeField] private Renderer[] targetRenderers;

        private void Awake()
        {
            if (targetRenderers == null || targetRenderers.Length == 0)
                targetRenderers = GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < targetRenderers.Length; i++)
                if (targetRenderers[i] != null)
                    targetRenderers[i].enabled = false;
        }
    }
}
