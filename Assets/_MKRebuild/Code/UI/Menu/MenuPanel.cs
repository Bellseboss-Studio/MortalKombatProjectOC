using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MK.Rebuild
{
    /// <summary>
    /// Runtime-built modal panel for the main menu (Options, Credits).
    ///
    /// WHY runtime uGUI and not scene objects: the menu scene is a cinematic fork; adding authored
    /// panels would mean more scene surgery and more legacy-prefab residue. Building the panel in
    /// code keeps the scene focused on the cinematic and lets the panel be gamepad-navigable with
    /// the same rules as the pause card.
    /// </summary>
    public sealed class MenuPanel : MonoBehaviour
    {
        private static readonly Color Dim = new Color(0f, 0f, 0f, 0.85f);
        private static readonly Color PanelColor = new Color(0.10f, 0.10f, 0.13f, 0.98f);
        private static readonly Color ButtonColor = new Color(0.12f, 0.12f, 0.15f, 0.95f);
        private static readonly Color Highlight = new Color(0.32f, 0.36f, 0.42f, 1f);
        private static readonly Color Selected = new Color(0.24f, 0.55f, 0.85f, 1f);
        private static readonly Color Pressed = new Color(0.07f, 0.07f, 0.09f, 1f);

        private static Font cachedFont;

        /// <summary>Vertical area where the caller adds sliders or text.</summary>
        public RectTransform Content { get; private set; }

        /// <summary>Back button, always the last focusable element.</summary>
        public Button Back { get; private set; }

        public static MenuPanel Create(string title, Transform parent)
        {
            var root = new GameObject("MenuPanel_" + title, typeof(RectTransform));
            root.transform.SetParent(parent, false);
            Stretch((RectTransform)root.transform);

            var canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 60;

            var scaler = root.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);

            root.AddComponent<GraphicRaycaster>();

            var dim = new GameObject("Dim", typeof(RectTransform)).AddComponent<Image>();
            dim.transform.SetParent(root.transform, false);
            Stretch(dim.rectTransform);
            dim.color = Dim;

            var panel = new GameObject("Panel", typeof(RectTransform)).AddComponent<Image>();
            panel.transform.SetParent(root.transform, false);
            var panelRect = panel.rectTransform;
            panelRect.anchorMin = panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.sizeDelta = new Vector2(1000f, 640f);
            panel.color = PanelColor;

            CreateText(panel.transform, "Title", title, 56, new Vector2(0f, 250f), new Vector2(900f, 90f));

            var content = new GameObject("Content", typeof(RectTransform));
            content.transform.SetParent(panel.transform, false);
            var contentRect = (RectTransform)content.transform;
            contentRect.anchorMin = contentRect.anchorMax = new Vector2(0.5f, 0.5f);
            contentRect.pivot = new Vector2(0.5f, 0.5f);
            contentRect.sizeDelta = new Vector2(860f, 360f);
            contentRect.anchoredPosition = new Vector2(0f, 20f);

            var back = CreateButton(panel.transform, "Back", "VOLVER", new Vector2(0f, -250f));

            var component = root.AddComponent<MenuPanel>();
            component.Content = contentRect;
            component.Back = back;
            root.SetActive(false);
            return component;
        }

        public void Show(Selectable first)
        {
            gameObject.SetActive(true);

            if (EventSystem.current == null || first == null) return;

            // Clearing first re-applies the highlight even when the same object was already focused.
            EventSystem.current.SetSelectedGameObject(null);
            EventSystem.current.SetSelectedGameObject(first.gameObject);
        }

        public void Hide() => gameObject.SetActive(false);

        /// <summary>Creates a labelled 0..1 slider and wires its value to <paramref name="onChanged"/>.</summary>
        public static Slider CreateSlider(
            Transform parent,
            string label,
            float value,
            Vector2 position,
            UnityEngine.Events.UnityAction<float> onChanged)
        {
            CreateText(
                parent,
                label + "Label",
                label,
                30,
                position + new Vector2(-300f, 0f),
                new Vector2(220f, 60f),
                TextAnchor.MiddleLeft);

            var go = new GameObject(label + "Slider", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(420f, 40f);
            rect.anchoredPosition = position + new Vector2(120f, 0f);

            var background = new GameObject("Background", typeof(RectTransform)).AddComponent<Image>();
            background.transform.SetParent(go.transform, false);
            Stretch(background.rectTransform);
            background.color = new Color(0.05f, 0.05f, 0.07f, 1f);

            var fill = new GameObject("Fill", typeof(RectTransform)).AddComponent<Image>();
            fill.transform.SetParent(go.transform, false);
            var fillRect = fill.rectTransform;
            fillRect.anchorMin = new Vector2(0f, 0f);
            fillRect.anchorMax = new Vector2(0f, 1f);
            fillRect.pivot = new Vector2(0f, 0.5f);
            fillRect.sizeDelta = Vector2.zero;
            fill.color = Selected;

            var handle = new GameObject("Handle", typeof(RectTransform)).AddComponent<Image>();
            handle.transform.SetParent(go.transform, false);
            var handleRect = handle.rectTransform;
            handleRect.anchorMin = new Vector2(0f, 0.5f);
            handleRect.anchorMax = new Vector2(0f, 0.5f);
            handleRect.pivot = new Vector2(0.5f, 0.5f);
            handleRect.sizeDelta = new Vector2(22f, 38f);
            handle.color = Color.white;

            var slider = go.AddComponent<Slider>();
            slider.targetGraphic = handle;
            slider.fillRect = fillRect;
            slider.handleRect = handleRect;
            slider.direction = Slider.Direction.LeftToRight;
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = Mathf.Clamp01(value);
            slider.onValueChanged.AddListener(onChanged);

            Navigation navigation = slider.navigation;
            navigation.mode = Navigation.Mode.Automatic;
            slider.navigation = navigation;

            return slider;
        }

        public static Text CreateBody(Transform parent, string value)
        {
            var text = CreateText(
                parent,
                "Body",
                value,
                26,
                new Vector2(0f, 20f),
                new Vector2(820f, 300f),
                TextAnchor.UpperCenter);
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            return text;
        }

        private static Text CreateText(
            Transform parent,
            string name,
            string value,
            int size,
            Vector2 position,
            Vector2 sizeDelta,
            TextAnchor anchor = TextAnchor.MiddleCenter)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);

            var text = go.AddComponent<Text>();
            text.font = ResolveFont();
            text.text = value;
            text.fontSize = size;
            text.color = Color.white;
            text.alignment = anchor;
            text.raycastTarget = false;

            var rect = (RectTransform)go.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = sizeDelta;
            rect.anchoredPosition = position;
            return text;
        }

        private static Button CreateButton(Transform parent, string name, string label, Vector2 position)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);

            var image = go.AddComponent<Image>();
            image.color = ButtonColor;

            var button = go.AddComponent<Button>();
            button.targetGraphic = image;

            ColorBlock colors = button.colors;
            colors.normalColor = ButtonColor;
            colors.highlightedColor = Highlight;
            colors.selectedColor = Selected;
            colors.pressedColor = Pressed;
            colors.fadeDuration = 0.05f;
            button.colors = colors;

            Navigation navigation = button.navigation;
            navigation.mode = Navigation.Mode.Automatic;
            button.navigation = navigation;

            var rect = (RectTransform)go.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(360f, 76f);
            rect.anchoredPosition = position;

            CreateText(go.transform, "Label", label, 32, Vector2.zero, new Vector2(360f, 76f));
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

        private static Font ResolveFont()
        {
            // Unity 6 removed Arial.ttf; the legacy runtime face is the supported built-in.
            if (cachedFont == null) cachedFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            return cachedFont;
        }
    }
}
