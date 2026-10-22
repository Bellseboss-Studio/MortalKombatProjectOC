using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace MK.Rebuild
{
    /// <summary>
    /// Pause card built entirely at runtime: its own overlay canvas, dim layer, title and
    /// Resume/Restart buttons. Nothing is authored in the scene, so the menu exists wherever
    /// a <see cref="GameFlowController"/> needs an end card and works even when the HUD is
    /// hidden by a cinematic.
    ///
    /// Pause is deliberately ALLOWED during cinematics (design §8): it stacks a
    /// <see cref="ControlReason.Pause"/> handle on top of the cinematic's handle, so time
    /// stops while the Timeline holds its frame and resumes exactly where it left off. The
    /// menu never consults <see cref="CinematicDirector.IsPlaying"/> — blocking there would
    /// trap the player in a beat that cannot be skipped.
    ///
    /// Input is polled straight from the devices (Esc / Start) instead of adding a Pause
    /// action: no action-map edit, no <c>PlayerInput.cs</c> regeneration, and it keeps
    /// working while the Player map owns the keyboard.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PauseMenu : MonoBehaviour
    {
        private const int CanvasSortingOrder = 100;

        private static readonly Color DimColor = new Color(0f, 0f, 0f, 0.72f);
        private static readonly Color ButtonColor = new Color(0.12f, 0.12f, 0.15f, 0.95f);
        private static readonly Color ButtonHighlighted = new Color(0.32f, 0.36f, 0.42f, 1f);
        private static readonly Color ButtonSelected = new Color(0.24f, 0.55f, 0.85f, 1f);
        private static readonly Color ButtonPressed = new Color(0.07f, 0.07f, 0.09f, 1f);

        private static Font cachedFont;

        private GameObject panel;
        private Text title;
        private Button resumeButton;
        private Button restartButton;

        private PlayerMotor motor;
        private BodyOwnership.Token bodyToken;
        private ControlLock.Handle pauseHandle;

        private bool isOpen;
        private bool restartOnly;
        private CursorLockMode previousLock;
        private bool previousCursorVisible;

        /// <summary>True while the card is visible and pause holds time.</summary>
        public bool IsOpen => isOpen;

        private void Awake()
        {
            BuildUi();
            SetRestartOnly(false);
            panel.SetActive(false);
        }

        private void Update()
        {
            EnsureSelection();

            Keyboard keyboard = Keyboard.current;
            if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
            {
                Toggle();
                return;
            }

            Gamepad gamepad = Gamepad.current;
            if (gamepad != null && gamepad.startButton.wasPressedThisFrame)
            {
                Toggle();
            }
        }

        private void OnDisable()
        {
            // Safety net: a disabled menu must not keep time frozen or the audio muted.
            if (!isOpen) return;
            isOpen = false;
            if (panel != null) panel.SetActive(false);
            ReleaseGameplay();
        }

        /// <summary>Opens the card when closed, closes it when open.</summary>
        public void Toggle()
        {
            if (isOpen) Close();
            else Open();
        }

        /// <summary>
        /// Shows the card and freezes gameplay. <paramref name="restartOnly"/> is the
        /// end-of-demo variant: no Resume, "DEMO COMPLETE" title, Restart only.
        /// Safe to call while already open (only the mode changes).
        /// </summary>
        public void Open(bool restartOnly = false)
        {
            if (panel == null) return;

            SetRestartOnly(restartOnly);

            if (!isOpen)
            {
                isOpen = true;

                motor = FindFirstObjectByType<PlayerMotor>();
                if (motor != null)
                {
                    bodyToken = motor.Body.Acquire(BodyPriority.Cinematic);
                    motor.FlushJumpBuffer();
                }

                pauseHandle = ControlLock.Acquire(ControlReason.Pause);

                previousLock = Cursor.lockState;
                previousCursorVisible = Cursor.visible;
            }

            panel.SetActive(true);
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            AudioListener.pause = true;
            SelectDefault();
        }

        /// <summary>
        /// Hides the card FIRST, then releases control, so the resumed gameplay never draws
        /// a frame with the panel still on screen.
        /// </summary>
        public void Close()
        {
            if (!isOpen) return;

            isOpen = false;
            if (panel != null) panel.SetActive(false);

            // Drop the focus with the card. An inactive button cannot be navigated, but a
            // lingering selection would keep routing UI submit events at a hidden menu.
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);

            ReleaseGameplay();

            AudioListener.pause = false;
            Cursor.lockState = previousLock;
            Cursor.visible = previousCursorVisible;
        }

        /// <summary>
        /// Restarts the demo through the flow controller (clean scene reload). Closes first
        /// so the panel cannot survive the load if the reload is deferred by a frame.
        /// </summary>
        public void Restart()
        {
            Close();

            GameFlowController flow = GameFlowController.Instance;
            if (flow != null)
            {
                flow.RestartLevel();
                return;
            }

            // Fallback: same sequence as GameFlowController.RestartLevel.
            ControlLock.Reset();
            AudioListener.pause = false;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        private void ReleaseGameplay()
        {
            if (motor != null && bodyToken != null)
            {
                motor.Body.Release(bodyToken);
                motor.FlushJumpBuffer();
            }

            bodyToken = null;
            motor = null;

            ControlLock.Release(pauseHandle);
            pauseHandle = null;
        }

        private void SetRestartOnly(bool value)
        {
            restartOnly = value;

            if (resumeButton != null) resumeButton.gameObject.SetActive(!value);

            if (title != null)
            {
                title.text = value ? "DEMO COMPLETE" : "PAUSED";
            }
        }

        /// <summary>
        /// Focuses the button the card starts on, so D-pad/stick (Navigate) and A/Submit work
        /// the moment it opens. A canvas with an input module but NO selected object silently
        /// swallows both, which is why the menu used to be mouse-only.
        /// The selection is cleared first on purpose: re-selecting the object that is already
        /// selected is a no-op in the EventSystem, so without the <c>null</c> step the
        /// highlight would not re-apply after the card was hidden and shown again.
        /// </summary>
        private void SelectDefault()
        {
            if (!isOpen || EventSystem.current == null) return;

            Button preferred = restartOnly ? restartButton : resumeButton;
            Button fallback = restartOnly ? resumeButton : restartButton;

            Button target = IsActive(preferred) ? preferred : fallback;
            if (!IsActive(target)) return;

            EventSystem.current.SetSelectedGameObject(null);
            EventSystem.current.SetSelectedGameObject(target.gameObject);
        }

        /// <summary>
        /// Self-heals a lost focus (a click on the dim layer, or the module clearing the
        /// selection when the focused object disappears). Without it the card would silently
        /// fall back to mouse-only input after the first stray click.
        /// </summary>
        private void EnsureSelection()
        {
            if (!isOpen || EventSystem.current == null) return;
            if (EventSystem.current.currentSelectedGameObject != null) return;

            SelectDefault();
        }

        private static bool IsActive(Button button)
        {
            return button != null && button.gameObject.activeInHierarchy;
        }

        // --- Runtime UI ------------------------------------------------------

        private void BuildUi()
        {
            var canvasObject = new GameObject("PauseCanvas", typeof(RectTransform));
            canvasObject.transform.SetParent(transform, false);

            Canvas canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = CanvasSortingOrder;

            CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            canvasObject.AddComponent<GraphicRaycaster>();

            EnsureEventSystem();

            panel = new GameObject("PausePanel", typeof(RectTransform));
            panel.transform.SetParent(canvasObject.transform, false);
            Stretch((RectTransform)panel.transform);

            Image dim = CreateImage(panel.transform, "Dim", DimColor);
            Stretch((RectTransform)dim.transform);

            title = CreateText(panel.transform, "Title", "PAUSED", 64, new Vector2(0f, 180f), new Vector2(900f, 140f));

            resumeButton = CreateButton(panel.transform, "Resume", "RESUME", new Vector2(0f, 0f));
            resumeButton.onClick.AddListener(Close);

            Button restart = CreateButton(panel.transform, "Restart", "RESTART", new Vector2(0f, -120f));
            restart.onClick.AddListener(Restart);
            restartButton = restart;
        }

        /// <summary>
        /// The scene has no EventSystem, and a canvas without one cannot click. Reuse an
        /// existing one when present (the HUD may own it) so two modules never fight over
        /// pointer routing.
        /// </summary>
        private static void EnsureEventSystem()
        {
            if (FindFirstObjectByType<EventSystem>(FindObjectsInactive.Include) != null) return;

            var eventSystemObject = new GameObject("PauseEventSystem", typeof(EventSystem));
            var module = eventSystemObject.AddComponent<InputSystemUIInputModule>();
            module.AssignDefaultActions();
        }

        private static Image CreateImage(Transform parent, string name, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);

            Image image = go.AddComponent<Image>();
            image.color = color;
            return image;
        }

        private static Text CreateText(
            Transform parent, string name, string value, int fontSize, Vector2 position, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);

            Text text = go.AddComponent<Text>();
            text.font = ResolveFont();
            text.text = value;
            text.fontSize = fontSize;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.raycastTarget = false;

            RectTransform rect = (RectTransform)go.transform;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            return text;
        }

        private static Button CreateButton(Transform parent, string name, string label, Vector2 position)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);

            Image background = go.AddComponent<Image>();
            background.color = ButtonColor;

            Button button = go.AddComponent<Button>();
            button.targetGraphic = background;

            // The card must be readable as "this is focused" from the couch, so the selected
            // state is a bright accent rather than the default barely-visible tint. Automatic
            // navigation is what turns D-pad/stick into movement between the two buttons.
            ColorBlock colors = button.colors;
            colors.normalColor = ButtonColor;
            colors.highlightedColor = ButtonHighlighted;
            colors.selectedColor = ButtonSelected;
            colors.pressedColor = ButtonPressed;
            colors.colorMultiplier = 1f;
            colors.fadeDuration = 0.05f;
            button.colors = colors;

            Navigation navigation = button.navigation;
            navigation.mode = Navigation.Mode.Automatic;
            button.navigation = navigation;

            RectTransform rect = (RectTransform)go.transform;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(360f, 84f);
            rect.anchoredPosition = position;

            Text text = CreateText(go.transform, "Label", label, 34, Vector2.zero, new Vector2(360f, 84f));
            text.raycastTarget = false;

            return button;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.pivot = new Vector2(0.5f, 0.5f);
        }

        /// <summary>
        /// Unity 6 ships one built-in legacy font; "Arial.ttf" throws now, so the runtime
        /// text must use the legacy runtime face (verified in the editor).
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
