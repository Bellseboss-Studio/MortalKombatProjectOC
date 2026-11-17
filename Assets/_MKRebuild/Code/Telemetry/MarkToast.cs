using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace MK.Rebuild.Telemetry
{
    /// <summary>
    /// Confirmación mínima de la marca F12 (MRK-2): uGUI en código, sin TextMeshPro, sin input y
    /// sin pausar nada. Se muestra SÓLO cuando <see cref="PlaytestLog.Mark"/> devolvió true, así
    /// que con <c>-nolog</c> o con el canal apagado por un fallo de IO no hay confirmación falsa.
    ///
    /// El toast vive en <c>DontDestroyOnLoad</c> y se reusa: una instancia, un canvas de
    /// <c>sortingOrder</c> 300 (encima del pausa 100 y del panel 200, porque la marca se hace
    /// jugando) y una corrutina de <c>WaitForSecondsRealtime</c> que apaga el texto a los 0,6 s.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class MarkToast : MonoBehaviour
    {
        /// <summary>Encima del pausa (100) y del panel de entrega (200).</summary>
        private const int CanvasSortingOrder = 300;

        /// <summary>Vida del aviso en segundos (MRK-2: ~0,6 s).</summary>
        private const float VisibleSeconds = 0.6f;

        private static readonly Color BackColor = new Color(0f, 0f, 0f, 0.62f);
        private static readonly Color TextColor = new Color(1f, 0.93f, 0.6f, 1f);

        private static MarkToast instance;

        private Text label;
        private Image background;
        private Coroutine hideRoutine;

        /// <summary>Muestra el aviso; crea el host la primera vez. No-op con mensaje vacío.</summary>
        public static void Show(string message)
        {
            if (string.IsNullOrEmpty(message)) return;

            // Unity convierte en null un objeto destruido, así que un static viejo (domain reload
            // desactivado) se recrea solo en vez de lanzar.
            if (instance == null)
            {
                var host = new GameObject("MarkToast");
                DontDestroyOnLoad(host);
                instance = host.AddComponent<MarkToast>();
            }

            instance.Display(message);
        }

        private void Awake()
        {
            Build();
            SetVisible(false);
        }

        /// <summary>Reinicia el temporizador: dos marcas seguidas no apagan el aviso de la segunda.</summary>
        private void Display(string message)
        {
            if (label == null) return;

            label.text = message;
            SetVisible(true);

            if (hideRoutine != null) StopCoroutine(hideRoutine);
            hideRoutine = StartCoroutine(HideAfterDelay());
        }

        private IEnumerator HideAfterDelay()
        {
            yield return new WaitForSecondsRealtime(VisibleSeconds);
            hideRoutine = null;
            SetVisible(false);
        }

        private void SetVisible(bool visible)
        {
            if (label != null) label.enabled = visible;
            if (background != null) background.enabled = visible;
        }

        /// <summary>
        /// Toast en la franja inferior central. Canvas propio para no depender de que la escena
        /// tenga HUD, y escaler 1920x1080 como el resto de la UI hecha en código.
        /// </summary>
        private void Build()
        {
            var canvasObject = new GameObject("MarkToastCanvas", typeof(RectTransform));
            canvasObject.transform.SetParent(transform, false);

            Canvas canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = CanvasSortingOrder;

            CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            var plate = new GameObject("Plate", typeof(RectTransform));
            plate.transform.SetParent(canvasObject.transform, false);

            background = plate.AddComponent<Image>();
            background.color = BackColor;
            background.raycastTarget = false;

            RectTransform plateRect = (RectTransform)plate.transform;
            plateRect.anchorMin = new Vector2(0.5f, 0f);
            plateRect.anchorMax = new Vector2(0.5f, 0f);
            plateRect.pivot = new Vector2(0.5f, 0f);
            plateRect.anchoredPosition = new Vector2(0f, 140f);
            plateRect.sizeDelta = new Vector2(520f, 64f);

            var textObject = new GameObject("Label", typeof(RectTransform));
            textObject.transform.SetParent(plate.transform, false);

            label = textObject.AddComponent<Text>();
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.text = string.Empty;
            label.fontSize = 32;
            label.alignment = TextAnchor.MiddleCenter;
            label.color = TextColor;
            label.raycastTarget = false;

            RectTransform textRect = (RectTransform)textObject.transform;
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            textRect.pivot = new Vector2(0.5f, 0.5f);
        }
    }
}
