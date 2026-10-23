using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace MK.Rebuild
{
    /// <summary>
    /// Persistent owner of every scene transition.
    ///
    /// WHY one controller: the active scene changes, but the loading state, the fade surface and
    /// the "is a load already running" guard must survive it. A per-scene loader would duplicate
    /// that state and two scene-local copies could both start a load. The first instance wins;
    /// later copies destroy themselves.
    ///
    /// WHY no build indices: <see cref="SceneManager.LoadSceneAsync(string, LoadSceneMode)"/> takes
    /// the scene name, so a reordered build list can never silently send the player elsewhere. The
    /// name is validated against <see cref="SceneFlow"/> before the load starts.
    ///
    /// Place it on a dedicated root: <c>DontDestroyOnLoad</c> is applied to the root, and the fade
    /// canvas is parented there so no scene has to author it.
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(-300)]
    public sealed class SceneFlowController : MonoBehaviour
    {
        private const float FadeSeconds = 0.25f;

        [Tooltip("Ordered scene chain. Loads are rejected when the target is not listed.")]
        [SerializeField] private SceneFlow flow;

        private CanvasGroup fadeGroup;
        private bool isLoading;

        /// <summary>The persistent controller, or <c>null</c> before the first scene boots.</summary>
        public static SceneFlowController Instance { get; private set; }

        /// <summary>True from the start of a load until the new scene's boot has run.</summary>
        public bool IsLoading => isLoading;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning(
                    $"[{nameof(SceneFlowController)}] Duplicate controller on '{name}' destroyed; '{Instance.name}' owns the flow.",
                    this);
                Destroy(this);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(transform.root.gameObject);
            BuildFade();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        /// <summary>
        /// Loads the successor of the active scene. The active scene must be in the flow; an
        /// unknown or final scene is an error, never an implicit jump to the first entry.
        /// </summary>
        public void LoadNext()
        {
            if (flow == null)
            {
                Debug.LogError(
                    $"[{nameof(SceneFlowController)}] No {nameof(SceneFlow)} assigned; cannot load the next scene.",
                    this);
                return;
            }

            string current = SceneManager.GetActiveScene().name;
            string next = flow.NextAfter(current);

            if (string.IsNullOrEmpty(next))
            {
                Debug.LogError(
                    $"[{nameof(SceneFlowController)}] Scene '{current}' has no successor in flow '{flow.name}'.",
                    this);
                return;
            }

            Load(next);
        }

        /// <summary>
        /// Loads <paramref name="sceneName"/> by name. A load already in flight is ignored, and a
        /// name outside the flow is rejected with a clear error instead of reaching the loader.
        /// </summary>
        public void Load(string sceneName)
        {
            if (isLoading)
            {
                Debug.LogWarning(
                    $"[{nameof(SceneFlowController)}] Ignoring load of '{sceneName}': another scene is already loading.",
                    this);
                return;
            }

            if (string.IsNullOrWhiteSpace(sceneName))
            {
                Debug.LogError($"[{nameof(SceneFlowController)}] Refusing a load with a blank scene name.", this);
                return;
            }

            if (flow == null)
            {
                Debug.LogError(
                    $"[{nameof(SceneFlowController)}] No {nameof(SceneFlow)} assigned; refusing to load '{sceneName}'.",
                    this);
                return;
            }

            if (!flow.Contains(sceneName))
            {
                Debug.LogError(
                    $"[{nameof(SceneFlowController)}] '{sceneName}' is not part of flow '{flow.name}'.",
                    this);
                return;
            }

            AsyncOperation operation;
            try
            {
                operation = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
            }
            catch (System.Exception exception)
            {
                Debug.LogError(
                    $"[{nameof(SceneFlowController)}] Could not start loading '{sceneName}': {exception.Message}",
                    this);
                return;
            }

            if (operation == null)
            {
                Debug.LogError(
                    $"[{nameof(SceneFlowController)}] LoadSceneAsync returned null for '{sceneName}'. Is it in Build Settings?",
                    this);
                return;
            }

            operation.allowSceneActivation = false;
            isLoading = true;
            StartCoroutine(LoadRoutine(operation));
        }

        /// <summary>Reloads the active scene by name (no build index) through the same chain.</summary>
        public void RestartCurrent()
        {
            string current = SceneManager.GetActiveScene().name;
            if (string.IsNullOrWhiteSpace(current))
            {
                Debug.LogError($"[{nameof(SceneFlowController)}] The active scene has no name; cannot restart.", this);
                return;
            }

            Load(current);
        }

        /// <summary>
        /// Fade out, wait for the load to finish, fade in, then hand the new scene to its own
        /// <see cref="SceneBoot"/>. The loading guard is released BEFORE <see cref="SceneBoot.Begin"/>
        /// so a boot that immediately chains (intro to intro) is not rejected as a double load.
        /// </summary>
        private IEnumerator LoadRoutine(AsyncOperation operation)
        {
            yield return Fade(1f);

            while (operation.progress < 0.9f) yield return null;

            operation.allowSceneActivation = true;
            while (!operation.isDone) yield return null;

            yield return Fade(0f);

            isLoading = false;

            SceneBoot boot = FindFirstObjectByType<SceneBoot>();
            if (boot != null) boot.Begin(this);
        }

        private IEnumerator Fade(float targetAlpha)
        {
            if (fadeGroup == null) yield break;

            float start = fadeGroup.alpha;
            if (Mathf.Approximately(start, targetAlpha)) yield break;

            float elapsed = 0f;
            while (elapsed < FadeSeconds)
            {
                elapsed += Time.unscaledDeltaTime;
                fadeGroup.alpha = Mathf.Lerp(start, targetAlpha, Mathf.Clamp01(elapsed / FadeSeconds));
                yield return null;
            }

            fadeGroup.alpha = targetAlpha;
        }

        private void BuildFade()
        {
            if (fadeGroup != null) return;

            var canvasObject = new GameObject("SceneFlowFade", typeof(RectTransform));
            canvasObject.transform.SetParent(transform, false);

            Canvas canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = short.MaxValue;

            fadeGroup = canvasObject.AddComponent<CanvasGroup>();
            fadeGroup.alpha = 0f;
            fadeGroup.blocksRaycasts = false;
            fadeGroup.interactable = false;

            var imageObject = new GameObject("Fade", typeof(RectTransform));
            imageObject.transform.SetParent(canvasObject.transform, false);

            Image image = imageObject.AddComponent<Image>();
            image.color = Color.black;
            image.raycastTarget = false;

            RectTransform rect = (RectTransform)imageObject.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}
