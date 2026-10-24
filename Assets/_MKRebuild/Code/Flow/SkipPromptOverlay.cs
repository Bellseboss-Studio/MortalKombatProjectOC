using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace MK.Rebuild
{
    /// <summary>
    /// Code-built "you can skip" hint shared by the cinematic boots.
    ///
    /// WHY runtime UI: the intro scenes stay free of authored HUD prefabs and EventSystems, so
    /// nothing in the scene YAML can pin a legacy canvas or input module. The overlay is
    /// unparented, so it ships with the scene and disappears when the scene unloads.
    ///
    /// WHY no GraphicRaycaster and no EventSystem: this overlay must never consume an event.
    /// It only paints text; the skip itself comes from the boot's <c>UI/Skip</c> action.
    ///
    /// The visual spec mirrors the Intro00 prompt (white label, dark outline, bottom-centre,
    /// fade in after a delay, then breathe). Keep them in sync if the style changes.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SkipPromptOverlay : MonoBehaviour
    {
        private const string PromptText = "PRESIONÁ ESPACIO / A PARA SALTAR";
        private const int CanvasSortingOrder = 50;
        private const float FadeSeconds = 0.3f;

        private static Font cachedFont;

        private CanvasGroup group;
        private Coroutine reveal;

        /// <summary>
        /// Builds the overlay and starts its reveal after <paramref name="delaySeconds"/>. Returns
        /// the component so the caller can <see cref="Hide"/> it on finish or on disable.
        /// </summary>
        public static SkipPromptOverlay Create(float delaySeconds)
        {
            var root = new GameObject("IntroSkipPrompt", typeof(RectTransform));
            SkipPromptOverlay overlay = root.AddComponent<SkipPromptOverlay>();
            overlay.Build();
            overlay.reveal = overlay.StartCoroutine(overlay.Reveal(delaySeconds));
            return overlay;
        }

        /// <summary>Stops the reveal and removes the overlay. Safe to call more than once.</summary>
        public void Hide()
        {
            if (reveal != null)
            {
                StopCoroutine(reveal);
                reveal = null;
            }

            Destroy(gameObject);
        }

        private void Build()
        {
            Canvas canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = CanvasSortingOrder;

            CanvasScaler scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            group = gameObject.AddComponent<CanvasGroup>();
            group.alpha = 0f;
            group.interactable = false;
            group.blocksRaycasts = false;

            var textObject = new GameObject("Label", typeof(RectTransform));
            textObject.transform.SetParent(transform, false);

            Text label = textObject.AddComponent<Text>();
            label.font = ResolveFont();
            label.text = PromptText;
            label.fontSize = 30;
            label.alignment = TextAnchor.MiddleCenter;
            label.color = Color.white;
            label.raycastTarget = false;

            // Dark outline keeps the white label readable over a bright frame.
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
        /// Holds the prompt hidden, fades it in, then lets it breathe. Realtime deltas are used
        /// because the cinematic must not depend on the game clock.
        /// </summary>
        private IEnumerator Reveal(float delaySeconds)
        {
            float elapsed = 0f;
            while (elapsed < delaySeconds)
            {
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            if (group == null) yield break;

            elapsed = 0f;
            while (elapsed < FadeSeconds)
            {
                elapsed += Time.unscaledDeltaTime;
                if (group != null) group.alpha = Mathf.Clamp01(elapsed / FadeSeconds);
                yield return null;
            }

            if (group == null) yield break;
            group.alpha = 1f;

            float pulse = 0f;
            while (group != null)
            {
                pulse += Time.unscaledDeltaTime;
                group.alpha = Mathf.Lerp(0.7f, 1f, (Mathf.Sin(pulse * Mathf.PI * 1.25f) + 1f) * 0.5f);
                yield return null;
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
