using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.Video;

namespace MK.Rebuild
{
    /// <summary>
    /// Intro00 boot: plays the opening cinematic and then hands control back to the flow.
    ///
    /// WHY a VideoClip and not a URL: the legacy scene loaded
    /// <c>StreamingAssets/Goro_intro00.mp4</c> through <c>Path.Combine</c> at runtime, which keeps
    /// the video outside the asset database and outside the flow's dependency graph. The clip now
    /// lives under <c>Migrated/Intro00/Video/</c>, so it is imported once, referenced by GUID and
    /// audited like every other asset. No path string exists in this class.
    ///
    /// WHY the clip is re-assigned from this component's own field: a scene author can leave the
    /// <see cref="VideoPlayer"/> clip empty by accident. Assigning the fallback clip keeps a broken
    /// reference from turning into a silent black screen; if both are null the boot warns and
    /// advances instead of stalling the chain.
    ///
    /// WHY both <c>loopPointReached</c> and a realtime wait: <c>loopPointReached</c> only fires for
    /// non-looping clips that actually reach their end, and it never fires when playback is
    /// interrupted. The timer is the safety net; whichever finishes first advances, and
    /// <see cref="Finish"/> is idempotent so the chain never loads twice.
    /// </summary>
    public sealed class IntroVideoBoot : SceneBoot
    {
        private const string SkipPromptText = "PRESIONÁ ESPACIO / A PARA SALTAR";
        private const int PromptCanvasSortingOrder = 50;
        private const float PromptRevealDelay = 1f;
        private const float PromptFadeSeconds = 0.3f;

        private static Font cachedFont;

        [Header("Playback")]
        [Tooltip("Player that renders the cinematic. Its clip is used unless it is empty.")]
        [SerializeField] private VideoPlayer videoPlayer;

        [Tooltip("Fallback clip assigned when the player has none.")]
        [SerializeField] private VideoClip clip;

        [Tooltip("How long to wait when the clip reports a length of zero.")]
        [SerializeField, Min(0f)] private float fallbackSeconds = 10f;

        [Tooltip("If the player has not prepared in this many seconds, warn and advance.")]
        [SerializeField, Min(0.5f)] private float prepareTimeoutSeconds = 5f;

        [Header("Skip")]
        [Tooltip("Skip action from the rebuild's own UI.inputactions. Never the legacy PlayerInput.")]
        [SerializeField] private InputActionReference skipAction;

        [Header("Fade (optional)")]
        [Tooltip("Optional code fade painted on top of the video before the next scene loads.")]
        [SerializeField] private CanvasGroup fadeGroup;

        [SerializeField, Min(0f)] private float fadeSeconds = 0.35f;

        private SceneFlowController flow;
        private bool begun;
        private bool finished;
        private bool prepared;
        private Coroutine playback;
        private Coroutine prepareTimeout;
        private Coroutine promptRoutine;
        private GameObject promptRoot;
        private CanvasGroup promptGroup;

        /// <summary>
        /// Starts the cinematic once. Repeated calls are ignored because the flow controller and the
        /// direct-play fallback in <see cref="Start"/> can both reach this method.
        /// </summary>
        public override void Begin(SceneFlowController controller)
        {
            if (begun) return;
            begun = true;
            flow = controller;

            if (videoPlayer == null)
            {
                Debug.LogWarning($"[{nameof(IntroVideoBoot)}] No VideoPlayer assigned; advancing instead of stalling.", this);
                Finish();
                return;
            }

            if (videoPlayer.clip == null) videoPlayer.clip = clip;

            if (videoPlayer.clip == null)
            {
                Debug.LogWarning($"[{nameof(IntroVideoBoot)}] No VideoClip assigned; advancing instead of stalling.", this);
                Finish();
                return;
            }

            videoPlayer.playOnAwake = false;
            videoPlayer.loopPointReached += OnLoopPointReached;
            videoPlayer.prepareCompleted += OnPrepared;
            videoPlayer.errorReceived += OnError;

            BuildSkipPrompt();
            EnableSkip();
            prepareTimeout = StartCoroutine(WaitForPrepare());
            videoPlayer.Prepare();
        }

        /// <summary>
        /// Stops the cinematic and advances immediately. Safe to call twice: the second call sees
        /// <see cref="Finish"/>'s guard and does nothing.
        /// </summary>
        public override void Skip()
        {
            if (finished) return;

            if (videoPlayer != null && videoPlayer.isPlaying) videoPlayer.Stop();
            Finish();
        }

        /// <summary>
        /// WHY this fallback exists: entering Play Mode with Intro00 as the open scene never runs a
        /// <see cref="SceneFlowController.Load"/>, so nothing would call <see cref="Begin"/>. The
        /// controller lives in the same scene and its <c>Awake</c> already ran (execution order
        /// -300), so the instance is available. When Intro00 is reached through a load, the
        /// controller calls <see cref="Begin"/> and this guard makes the second call a no-op.
        /// </summary>
        private void Start()
        {
            if (!begun) Begin(SceneFlowController.Instance);
        }

        private void OnDisable()
        {
            Unsubscribe();
            DestroySkipPrompt();
        }

        private void OnPrepared(VideoPlayer player)
        {
            if (finished) return;

            prepared = true;
            StopPrepareTimeout();

            player.Play();
            playback = StartCoroutine(WaitForPlayback(player));
            promptRoutine = StartCoroutine(RevealSkipPrompt());
        }

        private void OnLoopPointReached(VideoPlayer player)
        {
            Finish();
        }

        private void OnError(VideoPlayer player, string message)
        {
            Debug.LogWarning($"[{nameof(IntroVideoBoot)}] VideoPlayer error '{message}'; advancing.", this);
            Finish();
        }

        private void OnSkip(InputAction.CallbackContext context)
        {
            Skip();
        }

        /// <summary>
        /// Waits out the real playback length on the realtime clock, so a paused or slowed game
        /// clock cannot stretch the cinematic. A zero length means the clip is still unresolved;
        /// the fallback keeps the chain moving.
        /// </summary>
        private IEnumerator WaitForPlayback(VideoPlayer player)
        {
            double seconds = player.length > 0d ? player.length : fallbackSeconds;
            float deadline = Time.realtimeSinceStartup + (float)seconds;

            while (!finished && Time.realtimeSinceStartup < deadline) yield return null;

            playback = null;
            if (!finished) Finish();
        }

        /// <summary>
        /// Guards against a prepare that never completes (a missing codec, a locked file). One
        /// warning plus one advance is better than a black screen the player cannot skip.
        /// </summary>
        private IEnumerator WaitForPrepare()
        {
            float deadline = Time.realtimeSinceStartup + prepareTimeoutSeconds;
            while (!prepared && !finished && Time.realtimeSinceStartup < deadline) yield return null;

            prepareTimeout = null;
            if (prepared || finished) yield break;

            Debug.LogWarning(
                $"[{nameof(IntroVideoBoot)}] VideoPlayer did not prepare in {prepareTimeoutSeconds:0.#}s; advancing.",
                this);
            Finish();
        }

        /// <summary>
        /// Single exit point for "the intro is over". Idempotent by design: loop end, timer, skip
        /// and error can race, and the flow must only be advanced once.
        /// </summary>
        private void Finish()
        {
            if (finished) return;
            finished = true;

            StopPrepareTimeout();
            DestroySkipPrompt();

            if (playback != null)
            {
                StopCoroutine(playback);
                playback = null;
            }

            if (videoPlayer != null && videoPlayer.isPlaying) videoPlayer.Stop();

            Unsubscribe();
            DisableSkip();

            if (fadeGroup != null && fadeSeconds > 0f) StartCoroutine(FadeThenAdvance());
            else Advance();
        }

        /// <summary>
        /// Optional code fade. The legacy intro used a one-track timeline just to fade to black;
        /// that asset is not moved. This keeps the same visual result without the timeline asset,
        /// and <see cref="SceneFlowController"/> still owns its own transition fade.
        /// </summary>
        private IEnumerator FadeThenAdvance()
        {
            float start = fadeGroup.alpha;
            float elapsed = 0f;

            while (elapsed < fadeSeconds)
            {
                elapsed += Time.unscaledDeltaTime;
                fadeGroup.alpha = Mathf.Lerp(start, 1f, Mathf.Clamp01(elapsed / fadeSeconds));
                yield return null;
            }

            fadeGroup.alpha = 1f;
            Advance();
        }

        private void Advance()
        {
            // Guarded by Finish(); LoadNext is the only transition this boot ever requests.
            flow?.LoadNext();
        }

        private void EnableSkip()
        {
            if (skipAction == null || skipAction.action == null)
            {
                Debug.LogWarning($"[{nameof(IntroVideoBoot)}] No skip action assigned; the intro can only finish on its own.", this);
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

        private void StopPrepareTimeout()
        {
            if (prepareTimeout == null) return;

            StopCoroutine(prepareTimeout);
            prepareTimeout = null;
        }

        private void Unsubscribe()
        {
            if (videoPlayer == null) return;

            videoPlayer.loopPointReached -= OnLoopPointReached;
            videoPlayer.prepareCompleted -= OnPrepared;
            videoPlayer.errorReceived -= OnError;
        }

        // --- Skip prompt (runtime UI, never authored in the scene) -----------

        /// <summary>
        /// Builds a non-interactive overlay that tells the player the cinematic can be skipped.
        /// It is created in code on purpose: the scene YAML stays clean, the prompt follows the
        /// boot's lifetime, and it works even when no HUD exists (same approach as PauseMenu).
        /// No GraphicRaycaster and no EventSystem: this overlay must never consume an event.
        /// </summary>
        private void BuildSkipPrompt()
        {
            if (promptRoot != null) return;

            promptRoot = new GameObject("IntroSkipPrompt", typeof(RectTransform));
            promptRoot.transform.SetParent(null, false);

            Canvas canvas = promptRoot.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = PromptCanvasSortingOrder;

            CanvasScaler scaler = promptRoot.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            promptGroup = promptRoot.AddComponent<CanvasGroup>();
            promptGroup.alpha = 0f;
            promptGroup.interactable = false;
            promptGroup.blocksRaycasts = false;

            var textObject = new GameObject("Label", typeof(RectTransform));
            textObject.transform.SetParent(promptRoot.transform, false);

            Text label = textObject.AddComponent<Text>();
            label.font = ResolveFont();
            label.text = SkipPromptText;
            label.fontSize = 30;
            label.alignment = TextAnchor.MiddleCenter;
            label.color = Color.white;
            label.raycastTarget = false;

            // Dark outline keeps the white label readable over a bright video frame.
            Outline outline = textObject.AddComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 0.85f);
            outline.effectDistance = new Vector2(2f, -2f);

            RectTransform rect = (RectTransform)textObject.transform;
            rect.anchorMin = new Vector2(0.5f, 0f);
            rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.anchoredPosition = new Vector2(0f, 72f);
            rect.sizeDelta = new Vector2(1200f, 64f);
        }

        /// <summary>
        /// Holds the prompt hidden for the first second of playback, fades it in, then lets it
        /// breathe. Every step checks <see cref="finished"/> so a skip kills it immediately.
        /// Realtime deltas are used because the cinematic must not depend on the game clock.
        /// </summary>
        private IEnumerator RevealSkipPrompt()
        {
            float elapsed = 0f;
            while (elapsed < PromptRevealDelay && !finished)
            {
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            if (finished || promptGroup == null) yield break;

            elapsed = 0f;
            while (elapsed < PromptFadeSeconds && !finished)
            {
                elapsed += Time.unscaledDeltaTime;
                if (promptGroup != null) promptGroup.alpha = Mathf.Clamp01(elapsed / PromptFadeSeconds);
                yield return null;
            }

            if (finished || promptGroup == null) yield break;
            promptGroup.alpha = 1f;

            float pulse = 0f;
            while (!finished && promptGroup != null)
            {
                pulse += Time.unscaledDeltaTime;
                promptGroup.alpha = Mathf.Lerp(
                    0.7f, 1f, (Mathf.Sin(pulse * Mathf.PI * 1.25f) + 1f) * 0.5f);
                yield return null;
            }
        }

        private void DestroySkipPrompt()
        {
            if (promptRoutine != null)
            {
                StopCoroutine(promptRoutine);
                promptRoutine = null;
            }

            promptGroup = null;

            if (promptRoot != null)
            {
                Destroy(promptRoot);
                promptRoot = null;
            }
        }

        /// <summary>
        /// Unity 6 ships one built-in legacy font; "Arial.ttf" throws now, so runtime text must
        /// use the legacy runtime face (verified in the editor).
        /// </summary>
        private static Font ResolveFont()
        {
            if (cachedFont == null)
            {
                cachedFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            }

            return cachedFont;
        }
    }
}
