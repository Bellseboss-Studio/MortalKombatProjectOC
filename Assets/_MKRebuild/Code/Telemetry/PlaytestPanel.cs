using System;
using System.Globalization;
using System.IO;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace MK.Rebuild.Telemetry
{
    /// <summary>
    /// Panel de entrega del playtest (HND-2): uGUI armado en código, sin TextMeshPro y sin
    /// prefabs. Lo crea <see cref="PlaytestLog"/> en el rig <c>Telemetry</c>, escucha el aviso
    /// <see cref="PauseMenu.SessionEndRequested"/> y se muestra encima del pausa
    /// (<c>sortingOrder</c> 200 sobre 100).
    ///
    /// Qué garantiza:
    /// - Abrirlo NO escribe <c>session.end</c>, no cierra el archivo y no detiene el registro: el
    ///   único cierre sigue siendo <see cref="PlaytestLog"/> en <c>OnApplicationQuit</c> (HND-1).
    /// - Muestra duración, muertes, distancia, marcas con reparto por sala, la tabla
    ///   <c>Sala | tiempo | presupuesto | Δ</c> de las 10 salas (con las no visitadas en 0) y la
    ///   carpeta activa con su archivo: la informa el servicio y, si es el fallback de
    ///   <c>persistentDataPath</c>, lo dice en vez de mostrar una ruta asumida (TEL-1).
    /// - <c>ABRIR CARPETA</c>, <c>COPIAR RUTA</c> y <c>EXPORTAR AL ESCRITORIO</c> son acciones
    ///   explícitas del jugador y operan sobre esa carpeta activa: un fallo se cuenta en pantalla
    ///   y deja el original intacto.
    /// - Sin telemetría (o con un canal caído) no ofrece una ruta falsa: dice el estado y
    ///   deshabilita los botones (TEL-5).
    ///
    /// El canvas vive en un hijo y se prende/apaga; el GameObject del rig NUNCA se apaga, porque
    /// ahí vive el servicio.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PlaytestPanel : MonoBehaviour
    {
        /// <summary>Debajo del toast de marca (300) y encima del pausa (100).</summary>
        private const int CanvasSortingOrder = 200;

        /// <summary>Doc 02: una sala se marca fuera de tolerancia al ±20 % de su presupuesto.</summary>
        private const float Tolerance = 0.2f;

        private static readonly Color DimColor = new Color(0.02f, 0.02f, 0.03f, 0.94f);
        private static readonly Color RowColor = new Color(0.86f, 0.88f, 0.92f, 1f);
        private static readonly Color OutOfBudgetColor = new Color(1f, 0.55f, 0.42f, 1f);
        private static readonly Color MutedColor = new Color(0.62f, 0.66f, 0.72f, 1f);
        private static readonly Color ButtonColor = new Color(0.12f, 0.12f, 0.15f, 0.95f);
        private static readonly Color ButtonHighlighted = new Color(0.32f, 0.36f, 0.42f, 1f);
        private static readonly Color ButtonSelected = new Color(0.24f, 0.55f, 0.85f, 1f);
        private static readonly Color ButtonPressed = new Color(0.07f, 0.07f, 0.09f, 1f);

        private static Font cachedFont;

        private GameObject canvasRoot;
        private Text summaryText;
        private Text statusText;
        private Text pathText;
        private Text messageText;
        private Text[] roomRows;
        private Button openFolderButton;
        private Button copyPathButton;
        private Button exportButton;

        private bool built;
        private bool failed;
        private bool isOpen;

        /// <summary>True mientras el panel tapa el pausa (lo consulta el propio pausa).</summary>
        public bool IsOpen => isOpen;

        // ------------------------------------------------------------------ ciclo de vida

        private void Awake()
        {
            try
            {
                BuildUi();
                built = true;
            }
            catch (Exception error)
            {
                failed = true;
                Debug.LogWarning(
                    "[Telemetry] Panel de entrega no construible (" + error.GetType().Name + "). " +
                    "La ruta queda en consola: " + PathForLog());
            }
        }

        /// <summary>
        /// Alta del evento <c>static</c> de <see cref="PauseMenu"/>: TEL-6 exige que TODA
        /// suscripción —también las estáticas— se dé de baja en <see cref="OnDisable"/>, así que
        /// el alta vive acá y no en <c>Awake</c>. El <c>-=</c> previo evita duplicados.
        /// </summary>
        private void OnEnable()
        {
            PauseMenu.SessionEndRequested -= HandleSessionEndRequested;
            PauseMenu.SessionEndRequested += HandleSessionEndRequested;
        }

        private void OnDisable()
        {
            PauseMenu.SessionEndRequested -= HandleSessionEndRequested;
        }

        private void OnDestroy()
        {
            PauseMenu.SessionEndRequested -= HandleSessionEndRequested;

            if (isOpen)
            {
                isOpen = false;
                PauseMenu.OverlayActive = false;
            }
        }

        private void Update()
        {
            if (failed || !built || !isOpen) return;

            // El pausa sale temprano con OverlayActive: el Esc de este panel es el único Esc vivo.
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame) Close();
        }

        /// <summary>El botón TERMINAR SESIÓN sólo avisa; el panel decide (y puede reabrirse).</summary>
        private void HandleSessionEndRequested()
        {
            Open();
        }

        // ------------------------------------------------------------------ abrir / cerrar

        /// <summary>
        /// Muestra el panel con datos frescos. No escribe nada: <see cref="PlaytestLog.Snapshot"/>
        /// es de sólo lectura (HND-1/HND-2).
        /// </summary>
        public void Open()
        {
            if (failed || !built)
            {
                // Sin panel, la ruta tiene que quedar igual en consola y en Player.log.
                Debug.Log("[Telemetry] Panel no disponible. Ruta de la sesión: " + PathForLog());
                return;
            }

            if (isOpen)
            {
                Refresh();
                return;
            }

            // Pausar PRIMERO, refrescar después: SetPaused(true) cierra la visita abierta y
            // acumula su dwell, así el Refresh siguiente ya muestra la sala en la que está parado
            // el jugador (antes aparecía en 0 s justo cuando abre la entrega).
            PlaytestLog log = PlaytestLog.Instance;
            if (log != null) log.SetPaused(true);

            Refresh();

            canvasRoot.SetActive(true);
            isOpen = true;
            PauseMenu.OverlayActive = true;

            SelectDefault();
        }

        /// <summary>
        /// Cierra el overlay y devuelve el Esc al pausa. No toca el archivo: el registro sigue y
        /// el panel se puede volver a abrir.
        /// </summary>
        public void Close()
        {
            if (!isOpen) return;

            isOpen = false;
            if (canvasRoot != null) canvasRoot.SetActive(false);
            PauseMenu.OverlayActive = false;

            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);

            PlaytestLog log = PlaytestLog.Instance;
            if (log != null) log.SetPaused(false);
        }

        // ------------------------------------------------------------------ acciones

        private void OpenFolder()
        {
            SessionSnapshot snapshot = Snapshot();
            if (string.IsNullOrEmpty(snapshot.Path))
            {
                ShowMessage("FALLÓ: no hay carpeta (telemetría desactivada)");
                return;
            }

            try
            {
                // Carpeta activa del servicio, no una constante ni la del archivo asumida.
                string folder = !string.IsNullOrEmpty(snapshot.Root)
                    ? snapshot.Root
                    : Path.GetDirectoryName(snapshot.Path);

                if (string.IsNullOrEmpty(folder) || !Directory.Exists(folder))
                {
                    ShowMessage("FALLÓ: la carpeta no existe en disco");
                    return;
                }

                // Uri escapa espacios y separadores; OpenURL abre el Explorador en Windows.
                Application.OpenURL(new Uri(folder).AbsoluteUri);
                ShowMessage("carpeta abierta: " + folder);
            }
            catch (Exception error)
            {
                ShowMessage("FALLÓ: no se pudo abrir la carpeta (" + error.GetType().Name + ")");
            }
        }

        private void CopyPath()
        {
            SessionSnapshot snapshot = Snapshot();
            if (string.IsNullOrEmpty(snapshot.Path))
            {
                ShowMessage("FALLÓ: no hay ruta que copiar");
                return;
            }

            try
            {
                GUIUtility.systemCopyBuffer = snapshot.Path;
                ShowMessage("ruta copiada al portapapeles");
            }
            catch (Exception error)
            {
                ShowMessage("FALLÓ: no se pudo copiar (" + error.GetType().Name + ")");
            }
        }

        private void ExportToDesktop()
        {
            SessionSnapshot snapshot = Snapshot();
            if (string.IsNullOrEmpty(snapshot.Path))
            {
                ShowMessage("FALLÓ: la grabación está desactivada");
                return;
            }

            string folder = ResolveExportFolder();
            if (string.IsNullOrEmpty(folder))
            {
                ShowMessage("FALLÓ: no hay Escritorio ni Documentos accesibles");
                return;
            }

            try
            {
                string copied = null;
                if (!string.IsNullOrEmpty(snapshot.LevelPath)) copied = CopyTo(folder, snapshot.LevelPath);
                if (!string.IsNullOrEmpty(snapshot.CombatPath)) CopyTo(folder, snapshot.CombatPath);

                ShowMessage(copied != null
                    ? "EXPORTADO a " + folder + " (" + Path.GetFileName(copied) + ")"
                    : "EXPORTADO a " + folder);
            }
            catch (UnauthorizedAccessException)
            {
                ShowMessage("FALLÓ: sin permiso de escritura en " + folder);
            }
            catch (IOException error)
            {
                ShowMessage("FALLÓ: " + error.GetType().Name + " en " + folder + " (el original está intacto)");
            }
            catch (Exception error)
            {
                ShowMessage("FALLÓ: " + error.GetType().Name + " (el original está intacto)");
            }
        }

        /// <summary>Escritorio, si no Documentos, si no el perfil del usuario.</summary>
        private static string ResolveExportFolder()
        {
            string desktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            if (!string.IsNullOrEmpty(desktop) && Directory.Exists(desktop)) return desktop;

            string documents = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            if (!string.IsNullOrEmpty(documents) && Directory.Exists(documents)) return documents;

            string profile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            return !string.IsNullOrEmpty(profile) && Directory.Exists(profile) ? profile : null;
        }

        /// <summary>
        /// Copia sin sobrescribir: si el nombre existe, agrega <c>-2</c>, <c>-3</c>… antes de la
        /// extensión. Lanza si no puede; el llamador lo cuenta en pantalla.
        /// </summary>
        private static string CopyTo(string folder, string source)
        {
            string fileName = Path.GetFileName(source);
            string stem = Path.GetFileNameWithoutExtension(fileName);
            string extension = Path.GetExtension(fileName);

            string target = Path.Combine(folder, fileName);
            for (int suffix = 2; File.Exists(target) && suffix < 100; suffix++)
            {
                target = Path.Combine(
                    folder,
                    stem + "-" + suffix.ToString(CultureInfo.InvariantCulture) + extension);
            }

            File.Copy(source, target, false);
            return target;
        }

        // ------------------------------------------------------------------ datos

        private static SessionSnapshot Snapshot()
        {
            PlaytestLog log = PlaytestLog.Instance;
            return log != null ? log.Snapshot() : default;
        }

        private static string PathForLog()
        {
            PlaytestLog log = PlaytestLog.Instance;
            string path = log != null ? log.SessionPath : null;
            return string.IsNullOrEmpty(path) ? "(sin archivo: telemetría desactivada)" : path;
        }

        private void Refresh()
        {
            PlaytestLog log = PlaytestLog.Instance;
            SessionSnapshot snapshot = Snapshot();

            summaryText.text = BuildSummary(log, snapshot);
            statusText.text = BuildStatus(snapshot);
            pathText.text = BuildPathText(snapshot);

            for (int i = 0; i < PlaytestRooms.All.Length; i++)
            {
                PlaytestRooms.Room room = PlaytestRooms.All[i];
                float dwell = log != null ? log.DwellOf(room.Id) : 0f;
                float delta = dwell - room.Budget;

                // "!" sólo para salas VISITADAS fuera de ±20 %: una sala con dwell 0 tiene
                // Δ = -presupuesto y marcaría las 10 filas desde el segundo cero (deviation 27).
                bool outOfBudget = dwell > 0f && Mathf.Abs(delta) > room.Budget * Tolerance;

                roomRows[i].text = (outOfBudget ? "! " : "  ") + BuildRow(room.Id, dwell, room.Budget, delta);
                roomRows[i].color = outOfBudget ? OutOfBudgetColor : RowColor;
            }

            ShowMessage(string.Empty);

            bool canAct = !string.IsNullOrEmpty(snapshot.Path);
            openFolderButton.interactable = canAct;
            copyPathButton.interactable = canAct;
            exportButton.interactable = canAct;
        }

        private static string BuildSummary(PlaytestLog log, in SessionSnapshot snapshot)
        {
            string marks = "marcas " + snapshot.Marks.ToString(CultureInfo.InvariantCulture);
            string distribution = BuildMarkDistribution(log);
            if (distribution.Length > 0) marks += " (" + distribution + ")";

            return "DURACIÓN " + Clock(snapshot.Duration) +
                   "   MUERTES " + snapshot.Deaths.ToString(CultureInfo.InvariantCulture) +
                   "   DISTANCIA " + Meters(snapshot.Distance) + "\n" +
                   marks +
                   (snapshot.ObjectiveComplete ? "   OBJETIVO COMPLETO" : string.Empty);
        }

        /// <summary>Reparto de marcas por sala; sin marcas no hay paréntesis vacío.</summary>
        private static string BuildMarkDistribution(PlaytestLog log)
        {
            if (log == null) return string.Empty;

            var builder = new System.Text.StringBuilder(64);
            for (int i = 0; i < PlaytestRooms.All.Length; i++)
            {
                string id = PlaytestRooms.All[i].Id;
                int count = log.MarksInRoom(id);
                if (count <= 0) continue;

                if (builder.Length > 0) builder.Append(", ");
                builder.Append(id);
                builder.Append(' ');
                builder.Append(count.ToString(CultureInfo.InvariantCulture));
            }

            int corridor = log.MarksInRoom(PlaytestRooms.NoRoom);
            if (corridor > 0)
            {
                if (builder.Length > 0) builder.Append(", ");
                builder.Append(PlaytestRooms.NoRoom);
                builder.Append(' ');
                builder.Append(corridor.ToString(CultureInfo.InvariantCulture));
            }

            return builder.ToString();
        }

        private static string BuildStatus(in SessionSnapshot snapshot)
        {
            if (snapshot.LogOff) return "ESTADO: telemetría desactivada (-nolog): no se escribe nada";
            if (snapshot.AnyFailed) return "ESTADO: grabación incompleta (un canal se apagó por un fallo de IO)";

            if (snapshot.LevelEnabled && snapshot.CombatEnabled) return "ESTADO: grabando level + combat";
            if (snapshot.LevelEnabled) return "ESTADO: grabando level";
            if (snapshot.CombatEnabled) return "ESTADO: grabando combat";
            return "ESTADO: telemetría desactivada";
        }

        /// <summary>
        /// Ruta REAL en uso (TEL-1), no una asumida: la carpeta sale del servicio
        /// (<see cref="PlaytestLog.ActiveRoot"/>) y, si se grabó en el fallback de
        /// <c>persistentDataPath</c>, se dice explícitamente.
        /// </summary>
        private static string BuildPathText(in SessionSnapshot snapshot)
        {
            if (string.IsNullOrEmpty(snapshot.Path)) return "CARPETA: (sin ruta)";

            string root = !string.IsNullOrEmpty(snapshot.Root)
                ? snapshot.Root
                : Path.GetDirectoryName(snapshot.Path);

            var builder = new System.Text.StringBuilder(256);
            builder.Append("CARPETA: ").Append(root);
            builder.Append('\n').Append("ARCHIVO: ").Append(Path.GetFileName(snapshot.Path));

            if (snapshot.UsesFallback)
                builder.Append("\n(la carpeta preferida no era escribible: respaldo en persistentDataPath)");

            return builder.ToString();
        }

        private static string BuildRow(string id, float dwell, float budget, float delta)
        {
            string sign = delta >= 0f ? "+" : "-";
            return id.PadRight(16) +
                   Seconds(dwell).PadLeft(9) +
                   Seconds(budget).PadLeft(13) +
                   "   " + sign + Seconds(Mathf.Abs(delta)).PadLeft(8);
        }

        private static string Seconds(float value)
        {
            return value.ToString("0.0", CultureInfo.InvariantCulture) + " s";
        }

        private static string Meters(float value)
        {
            return value.ToString("0.0", CultureInfo.InvariantCulture) + " m";
        }

        private static string Clock(float seconds)
        {
            int total = Mathf.Max(0, Mathf.FloorToInt(seconds));
            int minutes = total / 60;
            int rest = total % 60;
            return minutes.ToString("00", CultureInfo.InvariantCulture) + ":" +
                   rest.ToString("00", CultureInfo.InvariantCulture);
        }

        private void ShowMessage(string message)
        {
            if (messageText != null) messageText.text = message;
        }

        // ------------------------------------------------------------------ UI

        private void SelectDefault()
        {
            if (EventSystem.current == null || openFolderButton == null) return;

            Button target = openFolderButton.interactable ? openFolderButton : null;
            if (target == null) return;

            EventSystem.current.SetSelectedGameObject(null);
            EventSystem.current.SetSelectedGameObject(target.gameObject);
        }

        /// <summary>
        /// Construye una sola vez. Molde de <see cref="PauseMenu"/>: canvas overlay, escaler
        /// 1920x1080, <c>LegacyRuntime.ttf</c> y un EventSystem reusado si la escena ya tiene uno.
        /// </summary>
        private void BuildUi()
        {
            canvasRoot = new GameObject("PlaytestPanelCanvas", typeof(RectTransform));
            canvasRoot.transform.SetParent(transform, false);

            Canvas canvas = canvasRoot.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = CanvasSortingOrder;

            CanvasScaler scaler = canvasRoot.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            canvasRoot.AddComponent<GraphicRaycaster>();

            EnsureEventSystem();

            var panel = new GameObject("PlaytestPanel", typeof(RectTransform));
            panel.transform.SetParent(canvasRoot.transform, false);
            Stretch((RectTransform)panel.transform);

            Image dim = CreateImage(panel.transform, "Dim", DimColor);
            Stretch((RectTransform)dim.transform);

            CreateLabel(panel.transform, "Title", "ENTREGA DEL PLAYTEST",
                46, new Vector2(0f, 470f), new Vector2(1700f, 70f), TextAnchor.MiddleCenter, Color.white);

            summaryText = CreateLabel(panel.transform, "Summary", string.Empty,
                26, new Vector2(0f, 370f), new Vector2(1700f, 110f), TextAnchor.MiddleLeft, RowColor);

            statusText = CreateLabel(panel.transform, "Status", string.Empty,
                22, new Vector2(0f, 292f), new Vector2(1700f, 36f), TextAnchor.MiddleLeft, MutedColor);

            CreateLabel(panel.transform, "TableHeader",
                "   " + "SALA".PadRight(16) + "TIEMPO".PadLeft(9) + "PRESUPUESTO".PadLeft(13) + "   " + "Δ".PadLeft(9),
                22, new Vector2(0f, 248f), new Vector2(1700f, 32f), TextAnchor.MiddleLeft, MutedColor);

            roomRows = new Text[PlaytestRooms.All.Length];
            for (int i = 0; i < roomRows.Length; i++)
            {
                roomRows[i] = CreateLabel(panel.transform, "Row_" + PlaytestRooms.All[i].Id, string.Empty,
                    22, new Vector2(0f, 210f - i * 34f), new Vector2(1700f, 32f), TextAnchor.MiddleLeft, RowColor);
            }

            // Tres líneas posibles: CARPETA, ARCHIVO y el aviso de fallback (TEL-1).
            pathText = CreateLabel(panel.transform, "Path", string.Empty,
                20, new Vector2(0f, -186f), new Vector2(1700f, 92f), TextAnchor.MiddleLeft, RowColor);

            messageText = CreateLabel(panel.transform, "Message", string.Empty,
                22, new Vector2(0f, -276f), new Vector2(1700f, 32f), TextAnchor.MiddleCenter, OutOfBudgetColor);

            openFolderButton = CreateButton(panel.transform, "OpenFolder", "ABRIR CARPETA", new Vector2(-360f, -330f));
            openFolderButton.onClick.AddListener(OpenFolder);

            copyPathButton = CreateButton(panel.transform, "CopyPath", "COPIAR RUTA", new Vector2(0f, -330f));
            copyPathButton.onClick.AddListener(CopyPath);

            exportButton = CreateButton(panel.transform, "Export", "EXPORTAR AL ESCRITORIO", new Vector2(360f, -330f));
            exportButton.onClick.AddListener(ExportToDesktop);

            Button closeButton = CreateButton(panel.transform, "Close", "CERRAR", new Vector2(0f, -430f));
            closeButton.onClick.AddListener(Close);

            // Arranca apagado: el rig queda vivo, el canvas no tapa nada.
            canvasRoot.SetActive(false);
        }

        /// <summary>
        /// El pausa ya crea uno en <c>Awake</c>; si no existe (otra escena), el panel lo crea para
        /// que los botones se puedan clickear. Nunca dos módulos: se reusa el que haya.
        /// </summary>
        private static void EnsureEventSystem()
        {
            if (FindFirstObjectByType<EventSystem>(FindObjectsInactive.Include) != null) return;

            var eventSystemObject = new GameObject("TelemetryEventSystem", typeof(EventSystem));
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

        private static Text CreateLabel(
            Transform parent,
            string name,
            string value,
            int fontSize,
            Vector2 position,
            Vector2 size,
            TextAnchor alignment,
            Color color)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);

            Text text = go.AddComponent<Text>();
            text.font = ResolveFont();
            text.text = value;
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.color = color;
            text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;

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
            rect.sizeDelta = new Vector2(320f, 70f);
            rect.anchoredPosition = position;

            CreateLabel(go.transform, "Label", label, 26, Vector2.zero, new Vector2(320f, 70f),
                TextAnchor.MiddleCenter, Color.white);

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

        /// <summary>Unity 6: la fuente legacy de runtime; "Arial.ttf" ya no existe.</summary>
        private static Font ResolveFont()
        {
            if (cachedFont == null) cachedFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            return cachedFont;
        }
    }
}
