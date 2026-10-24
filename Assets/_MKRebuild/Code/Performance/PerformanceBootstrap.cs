using UnityEngine;

namespace MK.Rebuild
{
    /// <summary>
    /// Single place for the rebuild's frame contract. Runs before the first
    /// scene loads and needs no scene object, so builds and the editor share
    /// the same pacing: an explicit 60 FPS cap.
    ///
    /// vSync is disabled on purpose: <see cref="Application.targetFrameRate"/>
    /// is ignored while vSync is on, so the explicit cap (not the display
    /// refresh rate) is the contract.
    ///
    /// Post-processing and occlusion culling are intentionally OUT OF SCOPE
    /// here; this class only owns the frame-rate contract.
    /// </summary>
    public static class PerformanceBootstrap
    {
        private const int TargetFrameRate = 60;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void ApplyFrameContract()
        {
            Application.targetFrameRate = TargetFrameRate;
            QualitySettings.vSyncCount = 0;
        }
    }
}
