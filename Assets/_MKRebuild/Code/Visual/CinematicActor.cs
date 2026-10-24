using UnityEngine;
using UnityEngine.Playables;

namespace MK.Rebuild
{
    /// <summary>
    /// Plays one raw <see cref="AnimationClip"/> on an actor OUTSIDE its Animator
    /// Controller, so cinematic poses do not have to live inside Leena's gameplay
    /// state machine. This is the seam future beats plug their clips into: the clip
    /// is authored on the <see cref="CinematicDefinition"/>, not in the controller.
    ///
    /// A null clip freezes the current pose instead of resuming gameplay. Everything
    /// is undone by <see cref="Stop"/>, which is also called on disable/destroy, so a
    /// destroyed cinematic can never leave the Animator at speed 0 or out of its graph.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CinematicActor : MonoBehaviour
    {
        [Tooltip("Animator that plays gameplay states. Null = first Animator under this GameObject.")]
        [SerializeField] private Animator animator;

        private PlayableGraph graph;
        private float cachedSpeed = 1f;
        private bool speedCached;
        private bool isPlaying;

        /// <summary>True while a clip or a frozen pose owns the Animator.</summary>
        public bool IsPlaying => isPlaying;

        private void Awake()
        {
            if (animator == null) animator = GetComponentInChildren<Animator>();
            if (animator == null) return;

            cachedSpeed = animator.speed;
            speedCached = true;
        }

        /// <summary>
        /// Takes over the Animator. A clip plays through a dedicated graph (outside the
        /// controller); a null clip freezes whatever pose is showing. Calling it again
        /// stops the previous takeover first.
        /// </summary>
        public void Play(AnimationClip clip)
        {
            Stop();

            if (animator == null)
            {
                Debug.LogWarning($"[{nameof(CinematicActor)}] No Animator to drive; cinematic clip skipped.", this);
                return;
            }

            if (clip == null)
            {
                cachedSpeed = animator.speed;
                speedCached = true;
                animator.speed = 0f;
                isPlaying = true;
                return;
            }

            AnimationPlayableUtilities.PlayClip(animator, clip, out PlayableGraph created);
            graph = created;
            isPlaying = true;
        }

        /// <summary>Releases the takeover: destroys the graph if there is one, restores the
        /// Animator speed and lets the controller drive again. Idempotent.</summary>
        public void Stop()
        {
            if (graph.IsValid())
            {
                graph.Stop();
                graph.Destroy();
            }
            graph = default;

            if (animator != null && speedCached) animator.speed = cachedSpeed;

            isPlaying = false;
        }

        private void OnDisable()
        {
            Stop();
        }

        private void OnDestroy()
        {
            Stop();
        }
    }
}
