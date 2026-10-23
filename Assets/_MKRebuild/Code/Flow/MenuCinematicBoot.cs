using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace MK.Rebuild
{
    /// <summary>
    /// Boot for the 5:31 main-menu cinematic.
    ///
    /// WHY an explicit table instead of letting the director resolve its own bindings:
    /// six tracks in <c>TL_Menu</c> are literally named "Animation Track" and the rest are
    /// "(1)".."(6)". Names carry no meaning, so the only trustworthy key is the track object
    /// itself. The serialized table below has one row per output and is validated against the
    /// asset's output count, which makes a desynced table fail loudly instead of silently
    /// animating the wrong object.
    ///
    /// WHY the boot never advances the flow: the menu is the destination, not a cutscene. The
    /// timeline is ambiance; Play and Quit belong to <see cref="MainMenuController"/>. This boot
    /// therefore only starts playback and leaves the scene interactive for as long as the player
    /// wants to sit in it.
    ///
    /// WHY the music loop: the timeline's music clip is a 105 s one-shot that ends around
    /// 108.8 s, while the cinematic runs for 331.55 s. A looping source is started exactly when
    /// the authored clip ends so the menu never becomes silent for the last ~3.7 minutes.
    /// </summary>
    public sealed class MenuCinematicBoot : SceneBoot
    {
        /// <summary>
        /// One timeline output to one scene object. <see cref="target"/> is typed as
        /// <see cref="UnityEngine.Object"/> because the 15 outputs do not share a target type:
        /// Animators for animation tracks, GameObjects for activation tracks, AudioSources for
        /// audio tracks and a CinemachineBrain for the camera track.
        /// </summary>
        [Serializable]
        public struct TrackBinding
        {
            [Tooltip("Output track from TL_Menu. Bind by reference, never by name or index.")]
            public TrackAsset track;

            [Tooltip("Receiver for the track: Animator, GameObject, AudioSource or CinemachineBrain.")]
            public UnityEngine.Object target;
        }

        /// <summary>
        /// A timeline exposed reference (the Cinemachine shot's virtual camera) resolved by its
        /// authored id. The id is stable inside TL_Menu; renaming the shot track does not affect it.
        /// </summary>
        [Serializable]
        public struct ExposedRefBinding
        {
            [Tooltip("Exposed reference id authored inside the timeline asset.")]
            public string exposedName;

            [Tooltip("Object that resolves the exposed reference at play time.")]
            public UnityEngine.Object target;
        }

        [Header("Timeline")]
        [Tooltip("Director that owns TL_Menu. The asset must be assigned.")]
        [SerializeField] private PlayableDirector director;

        [Tooltip("One row per timeline output. The editor validator asserts the count matches.")]
        [SerializeField] private TrackBinding[] bindings = Array.Empty<TrackBinding>();

        [Tooltip("Exposed references (Cinemachine shot camera) resolved before playback.")]
        [SerializeField] private ExposedRefBinding[] exposedReferences = Array.Empty<ExposedRefBinding>();

        [Header("Music continuity")]
        [Tooltip("Looping AudioSource with the menu music. Started when the timeline's one-shot music clip ends.")]
        [SerializeField] private AudioSource menuMusicLoop;

        [Tooltip("Fallback start time if the music clip cannot be discovered from the timeline.")]
        [SerializeField, Min(0f)] private float fallbackMusicLoopStart = 108.8f;

        [Header("Skip")]
        [Tooltip("Skip action from the rebuild UI map. A during the cinematic jumps to the last frame.")]
        [SerializeField] private InputActionReference skipAction;

        private bool begun;
        private Coroutine loopRoutine;

        /// <summary>True while the cinematic is still playing (skip is allowed).</summary>
        public bool IsPlaying { get; private set; }

        /// <summary>Raised once when the cinematic ends naturally or is skipped.</summary>
        public event Action Finished;

        /// <summary>
        /// Rebinds every output, resolves the shot camera and starts the cinematic. Idempotent
        /// because both the flow controller and the direct-play fallback in <see cref="Start"/>
        /// can reach this method.
        /// </summary>
        public override void Begin(SceneFlowController flow)
        {
            if (begun) return;
            begun = true;

            if (director == null)
            {
                Debug.LogWarning($"[{nameof(MenuCinematicBoot)}] No PlayableDirector assigned; the menu stays static.", this);
                Finished?.Invoke();
                return;
            }

            if (director.playableAsset == null)
            {
                Debug.LogWarning($"[{nameof(MenuCinematicBoot)}] The PlayableDirector has no TimelineAsset; the menu stays static.", this);
                Finished?.Invoke();
                return;
            }

            // Authored-director settings are asserted here so an inspector change cannot make the
            // cinematic play on awake or run on a paused game clock.
            director.playOnAwake = false;
            director.timeUpdateMode = DirectorUpdateMode.GameTime;
            director.extrapolationMode = DirectorWrapMode.None;

            BindAll();

            director.time = 0d;
            director.Play();

            StartMusicLoop();

            IsPlaying = true;
            director.stopped += OnStopped;
            EnableSkip();
        }

        /// <summary>
        /// Jumps the cinematic to its last frame and stops it. The menu controller waits for
        /// <see cref="Finished"/> before selecting a button, so A during the intro only skips and
        /// never activates Play by accident.
        /// </summary>
        public override void Skip()
        {
            if (!IsPlaying) return;

            if (director != null && director.playableAsset != null)
            {
                director.time = Math.Max(0d, director.duration - 0.01d);
                director.Evaluate();
                director.Stop();
            }

            Finish();
        }

        private void OnSkip(InputAction.CallbackContext context) => Skip();

        private void OnStopped(PlayableDirector stoppedDirector) => Finish();

        /// <summary>Single exit for "the cinematic is over", whether it ended or was skipped.</summary>
        private void Finish()
        {
            if (!IsPlaying) return;
            IsPlaying = false;

            if (director != null) director.stopped -= OnStopped;
            DisableSkip();
            Finished?.Invoke();
        }

        private void EnableSkip()
        {
            if (skipAction == null || skipAction.action == null) return;
            skipAction.action.performed += OnSkip;
            skipAction.action.Enable();
        }

        private void DisableSkip()
        {
            if (skipAction == null || skipAction.action == null) return;
            skipAction.action.performed -= OnSkip;
        }

        /// <summary>
        /// Direct-play fallback: opening MainMenu_ in the editor never goes through a load, so no
        /// controller calls <see cref="Begin"/>. The guard keeps the load path authoritative.
        /// </summary>
        private void Start()
        {
            if (!begun) Begin(SceneFlowController.Instance);
        }

        private void OnDestroy()
        {
            DisableSkip();

            if (loopRoutine != null)
            {
                StopCoroutine(loopRoutine);
                loopRoutine = null;
            }
        }

        /// <summary>
        /// Applies the serialized table and checks it against the asset. A short table is reported
        /// as an error: a missing row would leave one track silently unbound and the cinematic
        /// would lose a limb without any obvious symptom.
        /// </summary>
        private void BindAll()
        {
            int applied = 0;
            for (int i = 0; i < bindings.Length; i++)
            {
                TrackBinding binding = bindings[i];
                if (binding.track == null || binding.target == null)
                {
                    Debug.LogWarning(
                        $"[{nameof(MenuCinematicBoot)}] Binding {i} is incomplete (track or target missing); skipping it.",
                        this);
                    continue;
                }

                director.SetGenericBinding(binding.track, binding.target);
                applied++;
            }

            for (int i = 0; i < exposedReferences.Length; i++)
            {
                ExposedRefBinding reference = exposedReferences[i];
                if (string.IsNullOrEmpty(reference.exposedName) || reference.target == null)
                {
                    Debug.LogWarning(
                        $"[{nameof(MenuCinematicBoot)}] Exposed reference {i} is incomplete; skipping it.",
                        this);
                    continue;
                }

                director.SetReferenceValue(new PropertyName(reference.exposedName), reference.target);
            }

            director.RebindPlayableGraphOutputs();

            var asset = director.playableAsset as TimelineAsset;
            int expected = 0;
            if (asset != null)
            {
                foreach (PlayableBinding unused in asset.outputs) expected++;
            }

            if (asset != null && applied != expected)
            {
                Debug.LogError(
                    $"[{nameof(MenuCinematicBoot)}] Bound {applied} of {expected} timeline outputs. " +
                    "The binding table is out of sync with TL_Menu.",
                    this);
            }
        }

        /// <summary>
        /// Starts the continuity loop. The start time is discovered from the timeline itself so a
        /// re-authored music clip moves the hand-off with it; the serialized value is only a
        /// fallback when the audio track cannot be inspected.
        /// </summary>
        private void StartMusicLoop()
        {
            if (menuMusicLoop == null || menuMusicLoop.clip == null) return;

            menuMusicLoop.playOnAwake = false;
            menuMusicLoop.loop = true;

            if (loopRoutine != null) StopCoroutine(loopRoutine);
            loopRoutine = StartCoroutine(WaitForMusicHandOff(ResolveMusicEnd()));
        }

        private IEnumerator WaitForMusicHandOff(float startAt)
        {
            while (director != null
                   && director.state == PlayState.Playing
                   && director.time < startAt)
            {
                yield return null;
            }

            loopRoutine = null;

            // The cinematic ended or was stopped before the hand-off: do not start a loop over a
            // menu that is no longer showing this cinematic.
            if (director == null || director.state != PlayState.Playing) yield break;
            if (!menuMusicLoop.isPlaying) menuMusicLoop.Play();
        }

        /// <summary>
        /// End of the longest music clip in the asset, or the serialized fallback. The name check
        /// is intentionally narrow: the sting and the ambience are one-shots and must not move the
        /// hand-off.
        /// </summary>
        private float ResolveMusicEnd()
        {
            var asset = director != null ? director.playableAsset as TimelineAsset : null;
            if (asset == null) return fallbackMusicLoopStart;

            double end = -1d;
            foreach (TrackAsset track in asset.GetOutputTracks())
            {
                var audioTrack = track as AudioTrack;
                if (audioTrack == null) continue;

                foreach (TimelineClip clip in audioTrack.GetClips())
                {
                    var playable = clip.asset as AudioPlayableAsset;
                    if (playable == null || playable.clip == null) continue;
                    if (playable.clip.name.IndexOf("Music", StringComparison.OrdinalIgnoreCase) < 0) continue;

                    double clipEnd = clip.start + clip.duration;
                    if (clipEnd > end) end = clipEnd;
                }
            }

            return end > 0d ? (float)end : fallbackMusicLoopStart;
        }
    }
}
