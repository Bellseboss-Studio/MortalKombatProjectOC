using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace MK.Rebuild
{
    /// <summary>
    /// Turns the animated menu art into the two actions the demo actually supports, and owns the
    /// controller-only presentation the demo ships with.
    ///
    /// WHY only Play and Quit: the legacy menu had Options, Extras and Credits panels backed by
    /// PlayFab, FMOD and rebinding services. None of those survive the rebuild, so the buttons stay
    /// on screen as cinematic art but are made non-interactive. Faking them would promise features
    /// the demo cannot deliver.
    ///
    /// WHY <c>LoadNext</c> and not a scene name: from MainMenu_ the flow's successor is Playground,
    /// and the controller owns the fade and the "is this scene in the build list" guard. A hard
    /// name here would duplicate that decision. The direct <c>LoadScene</c> exists only so the
    /// scene still works when opened by itself in the editor, and it logs loudly when used.
    ///
    /// WHY the focus/cursor block: the demo is controller-oriented. A canvas can have a working
    /// input module and still be navigation-dead if nothing is selected, and the EventSystem
    /// silently drops the selection when the focused object disappears. The menu would then look
    /// interactive and respond only to a mouse that the player is not supposed to see. Selecting
    /// Play on boot and re-selecting whenever the focus is lost keeps D-pad/stick + A working for
    /// the whole lifetime of the scene.
    /// </summary>
    public sealed class MainMenuController : MonoBehaviour
    {
        [Header("Actions")]
        [Tooltip("Advances to the next scene in the flow (Playground from MainMenu_).")]
        [SerializeField] private Button play;

        [Tooltip("Quits the player.")]
        [SerializeField] private Button quit;

        [Header("Cinematic-only art")]
        [Tooltip("Buttons that exist for the timeline composition but have no demo behaviour.")]
        [SerializeField] private Button[] unavailable = System.Array.Empty<Button>();

        [Header("Controller focus")]
        [Tooltip("Frames to wait after the first selection attempt succeeds, so the canvas and the input module finish waking up.")]
        [SerializeField, Min(0)] private int focusSettleFrames = 1;

        [Tooltip("Seconds to wait for the EventSystem before giving up and logging. The menu still runs; only navigation is lost.")]
        [SerializeField, Min(0.1f)] private float focusTimeoutSeconds = 5f;

        private Coroutine focusRoutine;
        private bool focusWarned;

        private void Awake()
        {
            if (play != null) play.onClick.AddListener(Play);
            if (quit != null) quit.onClick.AddListener(Quit);

            for (int i = 0; i < unavailable.Length; i++)
            {
                if (unavailable[i] == null) continue;
                unavailable[i].interactable = false;
            }

            // D-pad/stick must be able to travel between Play and Quit. Automatic navigation is
            // what turns Navigate into movement; an Explicit/None button silently ignores it.
            SetAutomaticNavigation(play);
            SetAutomaticNavigation(quit);
        }

        private void OnEnable()
        {
            HideCursor();
            focusRoutine = StartCoroutine(FocusWhenReady());
        }

        private void OnDisable()
        {
            if (focusRoutine != null)
            {
                StopCoroutine(focusRoutine);
                focusRoutine = null;
            }
        }

        private void Update()
        {
            // Re-asserted every frame on purpose: a mouse click, an editor focus change or the
            // OS unlocking the cursor would otherwise bring the pointer back mid-menu, and the
            // menu must never show it.
            HideCursor();
            EnsureSelection();
        }

        private void OnDestroy()
        {
            if (play != null) play.onClick.RemoveListener(Play);
            if (quit != null) quit.onClick.RemoveListener(Quit);
        }

        /// <summary>
        /// Starts the game. Public and parameterless so the demo verifier can call it directly
        /// from the editor while in Play Mode.
        /// </summary>
        public void Play()
        {
            if (SceneFlowController.Instance != null)
            {
                SceneFlowController.Instance.LoadNext();
                return;
            }

            // The scene was opened directly, so there is no flow owner in memory. This is a debug
            // path, not a normal transition; the log makes that obvious in the console.
            Debug.LogWarning(
                $"[{nameof(MainMenuController)}] No SceneFlowController in the scene (opened directly); " +
                "loading 'Playground' through SceneManager as a debug fallback.",
                this);
            SceneManager.LoadScene("Playground");
        }

        /// <summary>Quits the player. In the editor this only logs, which is expected.</summary>
        public void Quit()
        {
            Debug.Log($"[{nameof(MainMenuController)}] Quit requested.");
            Application.Quit();
        }

        /// <summary>
        /// Waits for the EventSystem and the Play button to be alive, then focuses Play. The wait
        /// matters: the scene boots the cinematic in the same frame, and selecting before the
        /// module exists is a silent no-op that would leave the menu mouse-only.
        /// </summary>
        private IEnumerator FocusWhenReady()
        {
            float deadline = Time.unscaledTime + focusTimeoutSeconds;

            while (EventSystem.current == null || !IsActive(play))
            {
                if (Time.unscaledTime > deadline)
                {
                    focusWarned = true;
                    Debug.LogWarning(
                        $"[{nameof(MainMenuController)}] No EventSystem or no active Play button after " +
                        $"{focusTimeoutSeconds:0.#}s; gamepad navigation is unavailable.",
                        this);
                    focusRoutine = null;
                    yield break;
                }

                yield return null;
            }

            for (int i = 0; i < focusSettleFrames; i++)
            {
                yield return null;
            }

            EnsureNavigationActions();
            SelectDefault();
            focusRoutine = null;
        }

        /// <summary>
        /// Focuses Play, falling back to Quit if the primary action is missing. Clearing first is
        /// deliberate: re-selecting the already-selected object is a no-op, so without the
        /// <c>null</c> step the highlight would not re-apply after a lost selection.
        /// </summary>
        private void SelectDefault()
        {
            if (EventSystem.current == null) return;

            Button target = IsActive(play) ? play : (IsActive(quit) ? quit : null);
            if (!IsActive(target)) return;

            EventSystem.current.SetSelectedGameObject(null);
            EventSystem.current.SetSelectedGameObject(target.gameObject);
        }

        /// <summary>
        /// Self-heals a lost focus. The EventSystem clears the selection when the selected object
        /// disappears or a pointer click lands on nothing, and from that moment D-pad/stick and A
        /// do nothing. Restoring it keeps the menu controller-only for its whole lifetime.
        /// </summary>
        private void EnsureSelection()
        {
            if (focusWarned) return;
            if (EventSystem.current == null) return;
            if (EventSystem.current.currentSelectedGameObject != null) return;

            SelectDefault();
        }

        private static bool IsActive(Button button)
        {
            return button != null && button.gameObject.activeInHierarchy;
        }

        private static void SetAutomaticNavigation(Button button)
        {
            if (button == null) return;

            Navigation navigation = button.navigation;
            if (navigation.mode == Navigation.Mode.Automatic) return;

            navigation.mode = Navigation.Mode.Automatic;
            button.navigation = navigation;
        }

        /// <summary>
        /// The cursor is hidden, so the pointer actions cannot be used. Navigate/Submit/Cancel must
        /// therefore be live on the module; a disabled action would leave the gamepad dead even
        /// though the scene is wired correctly. Point/Click are left alone.
        /// </summary>
        private void EnsureNavigationActions()
        {
            var module = EventSystem.current != null
                ? EventSystem.current.GetComponent<InputSystemUIInputModule>()
                : null;

            if (module == null ||
                module.move == null || module.move.action == null ||
                module.submit == null || module.submit.action == null ||
                module.cancel == null || module.cancel.action == null)
            {
                Debug.LogWarning(
                    $"[{nameof(MainMenuController)}] The EventSystem has no InputSystemUIInputModule with " +
                    "Navigate/Submit/Cancel assigned; gamepad navigation will not work.",
                    this);
                return;
            }

            module.move.action.Enable();
            module.submit.action.Enable();
            module.cancel.action.Enable();
        }

        private static void HideCursor()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }
}
