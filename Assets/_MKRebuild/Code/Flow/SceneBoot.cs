using UnityEngine;

namespace MK.Rebuild
{
    /// <summary>
    /// Per-scene entry point owned by the flow chain.
    ///
    /// WHY this seam exists: the controller loads a scene asynchronously and then has no idea
    /// whether the scene starts a video, a timeline or an interactive menu. Each scene declares
    /// its own boot, so the controller stays a transport and the concrete boots (WU2-WU4) stay
    /// independent of one another.
    /// </summary>
    public abstract class SceneBoot : MonoBehaviour
    {
        /// <summary>
        /// Called once, after the scene is active and the transition has finished.
        /// The boot starts its content and decides when to call the controller again.
        /// </summary>
        public abstract void Begin(SceneFlowController flow);

        /// <summary>
        /// Optional advance used by skip input. Default does nothing so interactive scenes
        /// (the menu) can ignore it without a dummy override.
        /// </summary>
        public virtual void Skip()
        {
        }
    }
}
