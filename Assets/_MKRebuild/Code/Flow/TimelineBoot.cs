using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace MK.Rebuild
{
    /// <summary>
    /// Intro01 boot: plays a finite timeline and then hands control back to the flow.
    ///
    /// WHY explicit bindings instead of matching by name: the legacy intro01 timeline has four
    /// tracks literally named "Animation Track", "(1)", "(2)" and "(3)". Their names say nothing
    /// about the text object they animate, so any name-based or index-based guess would silently
    /// drive the wrong label. The serialized table below is authored in the scene, is inspectable
    /// in the Inspector, and is validated by the editor audit.
    ///
    /// WHY <c>UnityEngine.UI.Text</c> drives this scene (not TMP): the four recorded clips animate
    /// the properties of the legacy <c>Text</c> component (<c>m_Text</c>, <c>m_Color</c>). Swapping
    /// to TMP would require re-recording the clips; keeping the legacy component preserves the
    /// 10 s animation exactly.
    ///
    /// WHY a realtime duration wait as well as <c>stopped</c>: the event only fires when the
    /// director actually stops. If the graph is already invalid or playback never started, the
    /// event never arrives and the chain would stall forever. The timer is the safety net; both
    /// paths land on the idempotent <see cref="Finish"/>.
    /// </summary>
    public sealed class TimelineBoot : SceneBoot
    {
        /// <summary>
        /// One authored track-to-target pair. Public fields keep the struct cheap to serialize and
        /// readable in the scene YAML, which is what makes the binding table auditable.
        /// </summary>
        [Serializable]
        public struct TrackBinding
        {
            [Tooltip("Track from the assigned TimelineAsset.")]
            public TrackAsset track;

            [Tooltip("Animator that receives the track. One per text object.")]
            public Animator target;
        }

        [Header("Timeline")]
        [Tooltip("Director that owns the intro01 timeline. Its asset must be assigned.")]
        [SerializeField] private PlayableDirector director;

        [Tooltip("Explicit track-to-Animator bindings. Never matched by track name or index.")]
        [SerializeField] private TrackBinding[] bindings = Array.Empty<TrackBinding>();

        [Header("Skip")]
        [Tooltip("When true, skip evaluates the last frame before stopping so the final text state is shown.")]
        [SerializeField] private bool skipToLastFrame = true;

        [Tooltip("Seconds of playback before the skip prompt appears.")]
        [SerializeField, Min(0f)] private float skipPromptDelay = 1f;

        [Tooltip("Skip action from the rebuild's own UI.inputactions. Never the legacy PlayerInput.")]
        [SerializeField] private InputActionReference skipAction;

        private SceneFlowController flow;
        private bool begun;
        private bool finished;
        private Coroutine durationWait;
        private SkipPromptOverlay prompt;

        /// <summary>
        /// Binds the authored tracks, starts playback once and schedules the safety timer. Repeated
        /// calls are ignored because the flow controller and the direct-play fallback in
        /// <see cref="Start"/> can both reach this method.
        /// </summary>
        public override void Begin(SceneFlowController controller)
        {
            if (begun) return;
            begun = true;
            flow = controller;

            if (director == null)
            {
                Debug.LogWarning($"[{nameof(TimelineBoot)}] No PlayableDirector assigned; advancing instead of stalling.", this);
                Finish();
                return;
            }

            if (director.playableAsset == null)
            {
                Debug.LogWarning($"[{nameof(TimelineBoot)}] The PlayableDirector has no TimelineAsset; advancing instead of stalling.", this);
                Finish();
                return;
            }

            director.playOnAwake = false;
            director.extrapolationMode = DirectorWrapMode.None;

            BindTracks();

            director.stopped += OnStopped;
            director.time = 0d;
            director.Play();

            durationWait = StartCoroutine(WaitForDuration());
            prompt = SkipPromptOverlay.Create(skipPromptDelay);
            EnableSkip();
        }

        /// <summary>
        /// Jumps to the final frame (optional) and advances immediately. Safe to call twice: the
        /// second call sees <see cref="Finish"/>'s guard and does nothing.
        /// </summary>
        public override void Skip()
        {
            if (finished) return;

            if (director != null)
            {
                if (skipToLastFrame && director.playableAsset != null)
                {
                    // Stop just short of the duration so the timeline's own last frame is evaluated
                    // instead of a clamped end that some tracks would not sample.
                    director.time = Math.Max(0d, director.duration - 0.01d);
                    director.Evaluate();
                }

                director.Stop();
            }

            Finish();
        }

        /// <summary>
        /// WHY this fallback exists: entering Play Mode with Intro01 as the open scene never runs a
        /// <see cref="SceneFlowController.Load"/>, so nothing would call <see cref="Begin"/>. The
        /// controller lives in the scene and its <c>Awake</c> already ran (execution order -300),
        /// so the instance is available. When Intro01 is reached through a load, the controller
        /// calls <see cref="Begin"/> and this guard makes the second call a no-op.
        /// </summary>
        private void Start()
        {
            if (!begun) Begin(SceneFlowController.Instance);
        }

        private void OnDisable()
        {
            // The overlay is unparented and must not outlive the boot.
            if (prompt != null)
            {
                prompt.Hide();
                prompt = null;
            }
        }

        /// <summary>
        /// Applies the serialized table. A half-authored row is reported and skipped: one missing
        /// Animator must not prevent the other three tracks from binding.
        /// </summary>
        private void BindTracks()
        {
            for (int i = 0; i < bindings.Length; i++)
            {
                TrackBinding binding = bindings[i];
                if (binding.track == null || binding.target == null)
                {
                    Debug.LogWarning(
                        $"[{nameof(TimelineBoot)}] Binding {i} is incomplete (track or target missing); skipping it.",
                        this);
                    continue;
                }

                director.SetGenericBinding(binding.track, binding.target);
            }
        }

        private void OnStopped(PlayableDirector stoppedDirector)
        {
            Finish();
        }

        private void OnSkip(InputAction.CallbackContext context)
        {
            Skip();
        }

        /// <summary>
        /// Safety net for a director that never emits <c>stopped</c>. Realtime keeps this honest
        /// even when the game clock is paused or slowed.
        /// </summary>
        private IEnumerator WaitForDuration()
        {
            double seconds = director != null ? director.duration : 0d;
            float deadline = Time.realtimeSinceStartup + (float)seconds;

            while (!finished && Time.realtimeSinceStartup < deadline) yield return null;

            durationWait = null;
            if (!finished) Finish();
        }

        /// <summary>
        /// Single exit point for "the intro is over". Idempotent by design: the stopped event, the
        /// timer and skip can race, and the flow must only be advanced once.
        /// </summary>
        private void Finish()
        {
            if (finished) return;
            finished = true;

            if (durationWait != null)
            {
                StopCoroutine(durationWait);
                durationWait = null;
            }

            if (prompt != null)
            {
                prompt.Hide();
                prompt = null;
            }

            if (director != null)
            {
                director.stopped -= OnStopped;
                if (director.state == PlayState.Playing) director.Stop();
            }

            DisableSkip();

            // Guarded by finished; LoadNext is the only transition this boot ever requests.
            flow?.LoadNext();
        }

        private void EnableSkip()
        {
            if (skipAction == null || skipAction.action == null)
            {
                Debug.LogWarning($"[{nameof(TimelineBoot)}] No skip action assigned; the intro can only finish on its own.", this);
                return;
            }

            skipAction.action.performed += OnSkip;
            skipAction.action.Enable();
        }

        private void DisableSkip()
        {
            if (skipAction == null || skipAction.action == null) return;

            skipAction.action.performed -= OnSkip;
            skipAction.action.Disable();
        }
    }
}
