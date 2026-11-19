using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Security;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MK.Rebuild.Telemetry
{
    /// <summary>
    /// Servicio pasivo de telemetría de playtest: el ÚNICO emisor. Escribe un archivo JSONL por
    /// canal habilitado y por proceso (TEL-1, TEL-7), con un solo reloj (<c>Time.unscaledTime</c>),
    /// una sola atribución de sala y una sola sesión, aunque la escena se recargue diez veces.
    ///
    /// Reglas duras que este archivo implementa:
    /// - <c>-nolog</c> deja el servicio inerte desde <see cref="Awake"/>: sin directorios, sin
    ///   archivo, sin eventos (TEL-5).
    /// - Un fallo de IO en un canal apaga ESE canal, con un aviso; el otro sigue (TEL-5, TEL-7).
    /// - <c>session.end</c> se escribe exactamente una vez, en <see cref="OnApplicationQuit"/>
    ///   (HND-1). El panel y las recargas de escena no lo tocan.
    /// - Nada de gameplay depende de este tipo: los probes lo llaman y los handlers se dan de baja
    ///   en cada descarga (TEL-6).
    ///
    /// Los canales se eligen con <c>-log=level,combat</c> (default <c>level,combat</c>) y la
    /// identidad con <c>-player &lt;nombre&gt;</c>; si no viene, se recuerda la última y si no hay
    /// ninguna es <c>anon</c> sin subcarpeta (TEL-1).
    ///
    /// La carpeta sale de <c>-logdir &lt;ruta&gt;</c>, luego de la raíz del ejecutable / del
    /// proyecto y, si ninguna es escribible, de <c>persistentDataPath</c> (TEL-1). La elegida se
    /// expone en <see cref="ActiveRoot"/>: el panel muestra esa ruta, no una asumida.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PlaytestLog : MonoBehaviour
    {
        /// <summary>Clave de <see cref="PlayerPrefs"/> con la última identidad usada.</summary>
        private const string PlayerPrefsKey = "mk.playtest.player";

        /// <summary>Identidad sin datos: no se crea subcarpeta.</summary>
        private const string AnonymousPlayer = "anon";

        /// <summary>Gracia de <c>sin-sala</c>: menos que esto es rozar el vano, no un corredor.</summary>
        private const float EmptyRoomGrace = 0.5f;

        /// <summary>El buffer se vuelca cada segundo: un crash pierde como máximo el último (TEL-2).</summary>
        private const float FlushInterval = 1f;

        private static PlaytestLog instance;

        /// <summary>Instancia viva del servicio, o null si el rig no está en la escena.</summary>
        public static PlaytestLog Instance => instance;

        /// <summary>
        /// Red de seguridad para "Enter Play Mode Options" con domain reload desactivado: un
        /// static que sobrevive a la salida de Play mode apuntaría a un objeto destruido.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            instance = null;
        }

        private readonly JsonLineBuilder json = new JsonLineBuilder();
        private readonly Dictionary<string, float> dwellByRoom = new Dictionary<string, float>(PlaytestRooms.All.Length);
        private readonly Dictionary<string, int> marksByRoom = new Dictionary<string, int>(PlaytestRooms.All.Length + 1);

        private JsonlSink levelSink;
        private JsonlSink combatSink;

        private bool shutDown;      // -nolog
        private bool ended;         // session.end ya escrito (HND-1)
        private bool levelEnabled;  // canal level habilitado
        private bool combatEnabled; // canal combat habilitado

        private string playerId = AnonymousPlayer;
        private string sessionBase; // "sesion-YYYYMMDD-HHmmss" (sid y prefijo de archivo)
        private string startedIso;  // ISO local del instante de creación

        // --- carpeta activa (TEL-1) ---
        private string logDirOverride;    // -logdir <ruta>
        private string activeRoot;        // carpeta real de grabación (raíz + jugador)
        private bool usesFallbackRoot;    // la raíz preferida no era escribible

        private float t0;           // origen de TODO t/dur/dwell (unscaled)
        private int lastT;          // último t emitido: nunca retrocede
        private float flushTimer;

        // --- atribución de sala (la dueña es este servicio, nunca el probe) ---
        private RoomProbe[] probes;
        private string currentRoom = PlaytestRooms.NoRoom;
        private float visitStart;
        private bool visitOpen;
        private float emptyDeadline = -1f;

        /// <summary>
        /// Última sala REAL en la que estuvo el jugador (nunca <c>sin-sala</c>). La usa
        /// <see cref="Mark"/> para atribuir una marca hecha en un corredor sin inventar una sala:
        /// es la última que el jugador pisó, y la marca se declara en tránsito con <c>tr = 1</c>.
        /// Se limpia en cada <c>run.start</c>: una sala de la vida anterior no es "la última".
        /// </summary>
        private string lastRoom = PlaytestRooms.NoRoom;

        // --- pausa ---
        private bool pauseHeld;      // ControlLock.IsPaused
        private bool externalPaused; // panel de entrega (F6) o cualquier consumidor
        private bool pausedState;    // transición ya aplicada

        // --- escena / jugador ---
        private SessionProbe probe;
        private InputProbe inputProbe;
        private PlayerMotor motor;
        private PlayerDamage playerDamage;
        private GameplayBindings bindings;
        private int runIndex;
        private bool sceneHooked;
        private bool sceneReady;

        // --- acumulados de sesión completa (sobreviven a las recargas) ---
        private int deaths;
        private int markCount;
        private float distanceTravelled;
        private float lastResetAt = -1f;
        private float lastDamageAt = -1f;
        private bool objectiveComplete;

        /// <summary>True si al menos un canal tiene archivo abierto (TEL-5).</summary>
        public bool IsEnabled => IsChannelEnabled(Channel.Level) || IsChannelEnabled(Channel.Combat);

        /// <summary>Ruta completa del archivo (primer canal disponible) o null si está apagada.</summary>
        public string SessionPath
        {
            get
            {
                if (levelSink != null && levelSink.IsOpen) return levelSink.FilePath;
                if (combatSink != null && combatSink.IsOpen) return combatSink.FilePath;
                return null;
            }
        }

        /// <summary>Atribución vigente: la del último probe cruzado, o <c>sin-sala</c>.</summary>
        public string CurrentRoom => currentRoom;

        /// <summary>True con el pausa o el panel de entrega abiertos: no se muestrea ni se atribuye.</summary>
        public bool IsPaused => pauseHeld || externalPaused;

        /// <summary>True una vez que el objetivo contó todas las palancas (lo consume el panel, F6).</summary>
        public bool IsObjectiveComplete => objectiveComplete;

        /// <summary>True con <c>-nolog</c>: el servicio es inerte y el panel debe decirlo (TEL-5).</summary>
        public bool IsLogOff => shutDown;

        /// <summary>True si algún canal se apagó por un fallo de IO (panel: "grabación incompleta").</summary>
        public bool HasFailedChannel
        {
            get
            {
                if (levelSink != null && levelSink.Failed) return true;
                if (combatSink != null && combatSink.Failed) return true;
                return false;
            }
        }

        /// <summary>Ruta del canal <c>level</c>, o null si no está abierto.</summary>
        public string LevelPath => levelSink != null && levelSink.IsOpen ? levelSink.FilePath : null;

        /// <summary>Ruta del canal <c>combat</c>, o null si no está abierto.</summary>
        public string CombatPath => combatSink != null && combatSink.IsOpen ? combatSink.FilePath : null;

        /// <summary>
        /// Carpeta REAL en la que se está grabando (raíz resuelta + subcarpeta del jugador), o
        /// null con <c>-nolog</c> o sin ninguna carpeta escribible. Es la que muestran y abren el
        /// panel y los scripts: nunca una ruta asumida (TEL-1).
        /// </summary>
        public string ActiveRoot => activeRoot;

        /// <summary>
        /// True si la raíz preferida (override o carpeta del ejecutable) no era escribible y la
        /// sesión se grabó en el fallback de <c>persistentDataPath</c> (TEL-1). El panel lo dice.
        /// </summary>
        public bool UsesFallbackRoot => usesFallbackRoot;

        /// <summary>Muertes de la sesión completa (la muerte se duplica en ambos canales).</summary>
        public int Deaths => deaths;

        /// <summary>Marcas F12 registradas en la sesión.</summary>
        public int MarkCount => markCount;

        /// <summary>Duración unscaled de la sesión hasta este instante.</summary>
        public float Duration => shutDown ? 0f : Mathf.Max(0f, Time.unscaledTime - t0);

        /// <summary>Distancia caminada acumulada de la sesión completa.</summary>
        public float DistanceTravelled => distanceTravelled;

        /// <summary>Marcas F12 de una sala (lo usa el reparto del panel).</summary>
        public int MarksInRoom(string roomId)
        {
            if (string.IsNullOrEmpty(roomId)) return 0;
            return marksByRoom.TryGetValue(roomId, out int count) ? count : 0;
        }

        // ------------------------------------------------------------------ ciclo de vida

        private void Awake()
        {
            // Singleton resistente a la recarga: la escena nueva trae su propia copia del rig y
            // esta se descarta antes de tocar disco (un archivo por proceso, TEL-1).
            if (instance != null && instance != this)
            {
                shutDown = true; // copia de la escena nueva: no toca disco ni estado
                Destroy(gameObject);
                return;
            }

            instance = this;

            // DontDestroyOnLoad sólo actúa sobre objetos RAÍZ (el builder cuelga el rig de la
            // escena: root → Systems). Sin desprenderlo primero Unity IGNORA la llamada, el rig
            // muere en la primera recarga y la escena nueva abre un segundo archivo (TEL-1/TEL-6).
            // SetParent(null, true) preserva la transformación de mundo.
            if (transform.parent != null) transform.SetParent(null, true);
            DontDestroyOnLoad(gameObject);

            ParseCommandLine();

            // El panel se construye SIEMPRE (también con -nolog): TEL-5 pide que el panel diga
            // "telemetría desactivada" en vez de ofrecer una ruta que no existe.
            gameObject.AddComponent<PlaytestPanel>();

            if (shutDown) return; // -nolog: inerte, ni directorios ni archivo (TEL-5)

            t0 = Time.unscaledTime;
            DateTime now = DateTime.Now;
            startedIso = now.ToString("yyyy-MM-ddTHH:mm:sszzz", CultureInfo.InvariantCulture);
            sessionBase = "sesion-" + now.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture);

            string folder = ResolveFolder();
            string stem = string.IsNullOrEmpty(folder) ? null : Path.Combine(folder, sessionBase);

            if (stem != null && levelEnabled)
            {
                levelSink = new JsonlSink(Channel.Level, stem + JsonlSink.SuffixOf(Channel.Level) + ".jsonl");
                levelSink.Open();
            }

            if (stem != null && combatEnabled)
            {
                combatSink = new JsonlSink(Channel.Combat, stem + JsonlSink.SuffixOf(Channel.Combat) + ".jsonl");
                combatSink.Open();
            }

            ResetAttribution();
            WriteSessionStart();

            // El binder vive con el servicio: sobrevive las recargas y se re-enlaza por escena.
            bindings = gameObject.AddComponent<GameplayBindings>();

            HookScene();
        }

        private void OnEnable()
        {
            if (shutDown) return;

            HookScene();

            // Re-habilitación DENTRO de la misma escena (el rig se apagó y volvió): hay que
            // re-enlazar el jugador sin emitir un run.start que no corresponde.
            if (!sceneReady) return;

            CacheSceneObjects();
            BindScene();
        }

        private void OnDisable()
        {
            // Un servicio deshabilitado no puede seguir atribuyendo ni escuchando la escena.
            UnbindScene();
            UnhookScene();
        }

        /// <summary>
        /// Red de seguridad del PRIMER enlace: si por el orden de carga el rig no recibió
        /// <c>sceneLoaded</c> (p. ej. el rig se agregó después de despacharlo), la escena activa se
        /// enlaza acá. <see cref="sceneReady"/> evita el doble <c>run.start</c> cuando el evento sí
        /// llegó.
        /// </summary>
        private void Start()
        {
            if (shutDown || sceneReady) return;
            HandleSceneLoaded(SceneManager.GetActiveScene(), LoadSceneMode.Single);
        }

        private void Update()
        {
            if (shutDown) return;

            PollPause();
            UpdateRoomGrace();

            // Volcado por reloj unscaled: el pausa y el hitstop no retrasan el archivo.
            flushTimer += Time.unscaledDeltaTime;
            if (flushTimer < FlushInterval) return;

            flushTimer = 0f;
            FlushSinks();
        }

        private void OnApplicationQuit()
        {
            // ÚNICO lugar que escribe session.end (HND-1). Si el proceso muere antes, el archivo
            // sigue siendo válido y parseable, sólo sin el cierre.
            if (shutDown || ended) return;
            ended = true;

            // Cierra la visita abierta para que el último dwell sea calculable sin heurísticas.
            CloseVisit(PlaytestRooms.NoRoom);

            float duration = Mathf.Max(0f, Time.unscaledTime - t0);

            json.Begin("session.end");
            json.Int("t", T());
            json.Num("dur", duration);
            json.Int("deaths", deaths);
            json.Num("dist", distanceTravelled);
            LogBoth(json);

            FlushSinks();
            CloseSinks();
        }

        private void OnDestroy()
        {
            UnbindScene();
            UnhookScene();

            // Sin session.end: ese cierre es responsabilidad exclusiva de OnApplicationQuit.
            CloseSinks();

            if (instance == this) instance = null;
        }

        /// <summary>Suscripción idempotente a las cargas de escena (una por ciclo de vida).</summary>
        private void HookScene()
        {
            if (sceneHooked) return;
            sceneHooked = true;

            SceneManager.sceneLoaded += HandleSceneLoaded;
            SceneManager.sceneUnloaded += HandleSceneUnloaded;
        }

        private void UnhookScene()
        {
            if (!sceneHooked) return;
            sceneHooked = false;

            SceneManager.sceneLoaded -= HandleSceneLoaded;
            SceneManager.sceneUnloaded -= HandleSceneUnloaded;
        }

        // ------------------------------------------------------------------ escena

        private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (shutDown) return;

            // Orden: lo viejo se suelta, se re-abre la atribución, se emite run.start y recién
            // después se resuelve la sala (así el archivo dice "run.start" antes del primer
            // "room.enter").
            UnbindScene();
            CloseVisit(PlaytestRooms.NoRoom);
            ResetAttribution();

            CacheSceneObjects();

            runIndex++;
            json.Begin("run.start");
            json.Int("t", T());
            json.Str("sc", scene.name);
            json.Int("run", runIndex);
            Log(Channel.Level, json);

            BindScene();
            sceneReady = true;
        }

        private void HandleSceneUnloaded(Scene scene)
        {
            if (shutDown) return;

            // RestartLevel(): mismo archivo, mismo session.start, un run.start más. La visita
            // abierta se cierra antes para que el dwell no cruce la carga.
            UnbindScene();
            CloseVisit(PlaytestRooms.NoRoom);
            probes = null;
            sceneReady = false;
        }

        /// <summary>Resuelve el jugador y los probes de la escena nueva. Todo null-safe.</summary>
        private void CacheSceneObjects()
        {
            probes = FindObjectsByType<RoomProbe>(FindObjectsSortMode.None);

            motor = FindFirstObjectByType<PlayerMotor>();
            playerDamage = motor != null ? motor.GetComponent<PlayerDamage>() : null;
        }

        /// <summary>
        /// Agrega (si falta) el <see cref="SessionProbe"/> y el <see cref="InputProbe"/> al
        /// GameObject del jugador, re-arma sus líneas base y enlaza los eventos de gameplay de la
        /// escena. Un solo punto de autoría: el builder no cablea nada del jugador (F2.T5).
        /// </summary>
        private void BindScene()
        {
            // El combate puede no vivir en el mismo GO que el motor: se busca sólo si falta,
            // porque sin PlayerCombat no hay combos ni daño infligido (F9). El InputProbe usa la
            // misma referencia para leer el cursor de paso.
            PlayerCombat playerCombat = motor != null ? motor.GetComponent<PlayerCombat>() : null;
            if (playerCombat == null) playerCombat = FindFirstObjectByType<PlayerCombat>();

            if (motor != null)
            {
                probe = motor.GetComponent<SessionProbe>();
                if (probe == null) probe = motor.gameObject.AddComponent<SessionProbe>();
                probe.PrepareRun(); // primera muestra tras run.start no puede emitir reset

                // Probe de ENTRADA (causa del combo): la instancia observadora del input vive en
                // el jugador, así muere con la escena (Disable/Dispose en OnDisable/OnDestroy).
                inputProbe = motor.GetComponent<InputProbe>();
                if (inputProbe == null) inputProbe = motor.gameObject.AddComponent<InputProbe>();
                inputProbe.Bind(playerCombat, bindings);
            }

            if (bindings != null)
            {
                bindings.Bind(
                    playerDamage,
                    motor != null ? motor.GetComponent<PlayerStance>() : null,
                    playerCombat);
            }

            // Atribución inicial: el jugador suele aparecer dentro de una zona, y sin este paso
            // el tiempo hasta el primer cruce se perdería en sin-sala.
            string room = motor != null ? ResolveRoomAt(motor.transform.position) : null;
            if (room != null) EnterRoom(room);
        }

        /// <summary>Suelta todo lo que la escena vieja dejó enganchado (TEL-6).</summary>
        private void UnbindScene()
        {
            if (bindings != null) bindings.Unbind();
            if (inputProbe != null) inputProbe.Unbind(); // no se retiene la escena vieja
            probe = null;
            inputProbe = null;
            motor = null;
            playerDamage = null;
        }

        private void ResetAttribution()
        {
            currentRoom = PlaytestRooms.NoRoom;
            lastRoom = PlaytestRooms.NoRoom; // sala de otra vida: no se hereda
            visitStart = Time.unscaledTime;
            visitOpen = false;
            emptyDeadline = -1f;
        }

        // ------------------------------------------------------------------ línea de comandos

        /// <summary>
        /// <c>-nolog</c>, <c>-log=level,combat</c>, <c>-logdir &lt;ruta&gt;</c> y
        /// <c>-player &lt;nombre&gt;</c>. La identidad cae a la última recordada y, si no hay
        /// ninguna, a <c>anon</c> (TEL-1).
        ///
        /// El default es <c>level,combat</c> (TEL-7): el combate se graba solo si nadie pide otra
        /// cosa, y <c>-log=level</c> lo apaga explícitamente.
        /// </summary>
        private void ParseCommandLine()
        {
            levelEnabled = true;  // default: -log=level,combat
            combatEnabled = true;

            string[] args = Environment.GetCommandLineArgs();
            bool logFlagSeen = false;
            bool playerFromArgs = false;

            for (int i = 0; i < args.Length; i++)
            {
                string arg = args[i];
                if (string.IsNullOrEmpty(arg)) continue;

                if (string.Equals(arg, "-nolog", StringComparison.OrdinalIgnoreCase))
                {
                    shutDown = true;
                    return;
                }

                if (StartsWith(arg, "-player"))
                {
                    string value = ValueOf(arg, "-player");
                    if (string.IsNullOrEmpty(value) && i + 1 < args.Length) value = args[++i];
                    if (string.IsNullOrEmpty(value)) continue;

                    playerId = SanitizePlayer(value);
                    playerFromArgs = true;
                    continue;
                }

                if (StartsWith(arg, "-logdir"))
                {
                    // Override explícito de la carpeta (TEL-1). Se resuelve recién en Awake, después
                    // del guard de -nolog, y con prueba real de escritura.
                    string value = ValueOf(arg, "-logdir");
                    if (string.IsNullOrEmpty(value) && i + 1 < args.Length) value = args[++i];
                    logDirOverride = string.IsNullOrEmpty(value) ? null : value.Trim().Trim('"');
                    continue;
                }

                if (!StartsWith(arg, "-log")) continue;

                string list = ValueOf(arg, "-log");
                if (string.IsNullOrEmpty(list) && i + 1 < args.Length) list = args[++i];
                if (string.IsNullOrEmpty(list)) continue;

                logFlagSeen = true;
                levelEnabled = false;
                combatEnabled = false;

                string[] parts = list.Split(',');
                for (int p = 0; p < parts.Length; p++)
                {
                    string channel = parts[p].Trim().ToLowerInvariant();
                    if (channel == "level") levelEnabled = true;
                    else if (channel == "combat") combatEnabled = true;
                    else if (channel.Length > 0)
                        Debug.LogWarning($"[Telemetry] Canal desconocido en -log: '{channel}' (se ignora).");
                }
            }

            if (logFlagSeen && !levelEnabled && !combatEnabled)
                Debug.LogWarning("[Telemetry] -log no habilitó ningún canal: la grabación queda inerte.");

            if (!playerFromArgs)
            {
                string remembered = PlayerPrefs.GetString(PlayerPrefsKey, string.Empty);
                if (!string.IsNullOrEmpty(remembered)) playerId = remembered;
            }
            else
            {
                // La última identidad se recuerda para las próximas ejecuciones (TEL-1).
                PlayerPrefs.SetString(PlayerPrefsKey, playerId);
                PlayerPrefs.Save();
            }
        }

        private static bool StartsWith(string arg, string flag)
        {
            return arg.StartsWith(flag, StringComparison.OrdinalIgnoreCase)
                && (arg.Length == flag.Length || arg[flag.Length] == '=' || arg[flag.Length] == ':');
        }

        private static string ValueOf(string arg, string flag)
        {
            if (arg.Length <= flag.Length + 1) return string.Empty;
            return arg.Substring(flag.Length + 1).Trim();
        }

        /// <summary>
        /// Una identidad no puede romper la ruta del archivo: se reemplazan los caracteres
        /// inválidos de nombre de archivo. Sin nada válido queda <c>anon</c>.
        /// </summary>
        private static string SanitizePlayer(string raw)
        {
            string trimmed = raw.Trim();
            if (trimmed.Length == 0) return AnonymousPlayer;

            char[] invalid = Path.GetInvalidFileNameChars();
            var clean = new StringBuilder(trimmed.Length);
            for (int i = 0; i < trimmed.Length; i++)
            {
                char c = trimmed[i];
                bool bad = c == '/' || c == '\\' || Array.IndexOf(invalid, c) >= 0;
                clean.Append(bad ? '-' : c);
            }

            string result = clean.ToString().Trim('-', ' ', '.');
            return result.Length == 0 ? AnonymousPlayer : result;
        }

        /// <summary>
        /// Resuelve la carpeta de la sesión y deja la elegida en <see cref="ActiveRoot"/>.
        ///
        /// Orden (TEL-1): <c>-logdir &lt;ruta&gt;</c> explícito → raíz junto al ejecutable / al
        /// proyecto (<c>Path.GetDirectoryName(Application.dataPath) + "/playtest"</c>: en el
        /// Editor <c>&lt;proyecto&gt;/playtest</c>, en un build la carpeta del <c>.exe</c>) →
        /// fallback <c>persistentDataPath/playtest</c>.
        ///
        /// Cada candidato se prueba ESCRIBIENDO un archivo sonda: <c>Directory.Exists</c> no
        /// alcanza porque en <c>Program Files</c> el directorio existe y la escritura falla. Gana
        /// el primero que pasa; si no es la raíz preferida se avisa UNA vez con ambas rutas.
        /// </summary>
        private string ResolveFolder()
        {
            string overrideRoot = NormalizeRoot(logDirOverride);
            string exeRoot = NormalizeRoot(RootBesideExecutable());
            string fallbackRoot = NormalizeRoot(Path.Combine(Application.persistentDataPath, "playtest"));

            string preferred = !string.IsNullOrEmpty(overrideRoot) ? overrideRoot
                             : !string.IsNullOrEmpty(exeRoot) ? exeRoot
                             : fallbackRoot;

            // Orden literal de TEL-1: override, raíz del ejecutable, fallback.
            string[] roots = { overrideRoot, exeRoot, fallbackRoot };
            var tried = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            string firstFailedRoot = null;
            string firstFailure = null;

            for (int i = 0; i < roots.Length; i++)
            {
                string baseRoot = roots[i];
                if (string.IsNullOrEmpty(baseRoot) || !tried.Add(baseRoot)) continue;

                string candidate = WithPlayer(baseRoot);
                if (TryPrepareFolder(candidate, out string normalized, out string failure))
                {
                    bool isFallback = string.Equals(baseRoot, fallbackRoot, StringComparison.OrdinalIgnoreCase);

                    activeRoot = normalized;
                    usesFallbackRoot = isFallback;

                    if (firstFailure != null || isFallback)
                    {
                        Debug.LogWarning(
                            "[Telemetry] La carpeta preferida no es escribible: '" +
                            (firstFailedRoot ?? preferred) + "' (" + (firstFailure ?? "no disponible") + "). " +
                            "La sesión se graba en: " + normalized +
                            (isFallback ? " (fallback de persistentDataPath)." : "."));
                    }

                    return normalized;
                }

                if (firstFailure == null)
                {
                    firstFailure = failure;
                    firstFailedRoot = candidate;
                }
            }

            // Ningún candidato escribible: la sesión queda sin archivo y el juego sigue igual.
            activeRoot = null;
            usesFallbackRoot = true;
            Debug.LogWarning(
                "[Telemetry] Sin carpeta escribible para la telemetría. Preferida: '" + preferred +
                "' (" + (firstFailure ?? "no disponible") + "). Fallback: '" + fallbackRoot +
                "'. No se graba nada.");
            return null;
        }

        /// <summary>
        /// Raíz junto al ejecutable (build) o al proyecto (Editor), sin crear nada todavía.
        /// <c>Application.dataPath</c> es <c>&lt;proyecto&gt;/Assets</c> en el Editor y
        /// <c>&lt;carpeta del .exe&gt;/&lt;producto&gt;_Data</c> en un build de Windows/Linux;
        /// su directorio padre es la carpeta visible para el jugador en los dos casos.
        /// </summary>
        private static string RootBesideExecutable()
        {
            string dataPath = Application.dataPath;
            if (string.IsNullOrEmpty(dataPath)) return null;

            string baseDirectory = Path.GetDirectoryName(dataPath);
            return string.IsNullOrEmpty(baseDirectory) ? null : Path.Combine(baseDirectory, "playtest");
        }

        /// <summary>Ruta absoluta normalizada, o null si la ruta no es utilizable.</summary>
        private static string NormalizeRoot(string path)
        {
            if (string.IsNullOrEmpty(path)) return null;

            try
            {
                return Path.GetFullPath(path.Trim().Trim('"'));
            }
            catch (ArgumentException) { return null; }
            catch (NotSupportedException) { return null; }
            catch (PathTooLongException) { return null; }
            catch (SecurityException) { return null; }
        }

        /// <summary><c>anon</c> no crea subcarpeta: la raíz es la carpeta de la sesión.</summary>
        private string WithPlayer(string root)
        {
            return playerId == AnonymousPlayer ? root : Path.Combine(root, playerId);
        }

        /// <summary>
        /// Prueba REAL de escritura: crea el directorio y abre, escribe y borra un archivo sonda
        /// dentro de <c>try/catch</c>. Devuelve la ruta absoluta normalizada, o false con el
        /// motivo del fallo; nunca propaga.
        /// </summary>
        private static bool TryPrepareFolder(string folder, out string normalized, out string failure)
        {
            normalized = null;
            failure = null;
            string probe = null;

            try
            {
                normalized = Path.GetFullPath(folder);
                Directory.CreateDirectory(normalized);

                probe = Path.Combine(normalized, ".mk-playtest-probe-" + Guid.NewGuid().ToString("N") + ".tmp");
                using (var stream = new FileStream(probe, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    stream.WriteByte((byte)'\n');
                    stream.Flush();
                }

                return true;
            }
            catch (UnauthorizedAccessException error) { failure = error.GetType().Name; }
            catch (IOException error) { failure = error.GetType().Name + ": " + error.Message; }
            catch (SecurityException error) { failure = error.GetType().Name; }
            catch (ArgumentException error) { failure = error.GetType().Name; }
            catch (NotSupportedException error) { failure = error.GetType().Name; }
            finally
            {
                if (probe != null)
                {
                    try
                    {
                        File.Delete(probe); // la sonda es descartable: su borrado no puede tirar nada
                    }
                    catch
                    {
                        // Antivirus, FS de sólo lectura, etc. El archivo queda como basura inocua.
                    }
                }
            }

            return false;
        }

        // ------------------------------------------------------------------ sesión

        private void WriteSessionStart()
        {
            // Mismos sid y started en todos los canales: el cruce por tiempo no necesita
            // heurísticas (TEL-7).
            json.Begin("session.start");
            json.Int("t", T());
            json.Str("p", playerId);
            json.Str("bv", Application.version);
            json.Str("sc", SceneManager.GetActiveScene().name);
            json.Str("m", SystemInfo.deviceName);
            json.Str("started", startedIso);
            json.Str("sid", sessionBase);
            LogBoth(json);
        }

        // ------------------------------------------------------------------ entradas de los probes

        /// <summary>Muestra de posición/velocidad a 4 Hz (canal level).</summary>
        public void Sample(Vector3 position, float speed)
        {
            if (!IsChannelEnabled(Channel.Level)) return;

            json.Begin("p");
            json.Int("t", T());
            json.Str("r", currentRoom);
            json.Num("x", position.x);
            json.Num("y", position.y);
            json.Num("z", position.z);
            json.Num("sp", speed);
            Log(Channel.Level, json);
        }

        /// <summary>Cruce de un probe de sala: cierra la visita anterior y abre la nueva.</summary>
        public void RoomEntered(string roomId)
        {
            // La atribución es estado interno del servicio, NO un evento del canal level: corre
            // con cualquier canal habilitado (-log=combat también necesita saber la sala del evento).
            if (IsPaused || string.IsNullOrEmpty(roomId)) return;
            if (roomId == currentRoom) return; // dedupe: dos colliders del jugador, re-entrada

            EnterRoom(roomId);
        }

        /// <summary>Salida de un probe: arma la gracia de <c>sin-sala</c>; no cierra al instante.</summary>
        public void RoomLeft(string roomId)
        {
            // Igual que RoomEntered: la atribución no depende del canal level.
            if (IsPaused) return;
            if (roomId != currentRoom || !visitOpen) return;
            if (emptyDeadline >= 0f) return; // ya está armada

            emptyDeadline = Time.unscaledTime + EmptyRoomGrace;
        }

        /// <summary>
        /// Cierra una quietud &gt; 3 s con la duración real (unscaled). La sala la pone el servicio:
        /// el probe no conoce la atribución.
        /// </summary>
        public void Idle(Vector3 position, float duration)
        {
            if (!IsChannelEnabled(Channel.Level)) return;
            if (duration <= 0f) return;

            json.Begin("idle");
            json.Int("t", T());
            json.Str("r", currentRoom);
            json.Num("x", position.x);
            json.Num("y", position.y);
            json.Num("z", position.z);
            json.Num("dur", duration);
            Log(Channel.Level, json);
        }

        /// <summary>Verbo de movimiento detectado por flanco: <c>dash</c>, <c>jump</c>, <c>grab</c>, <c>mantle</c>.</summary>
        public void Verb(string kind)
        {
            if (!IsChannelEnabled(Channel.Level)) return;
            if (string.IsNullOrEmpty(kind)) return;

            json.Begin("verb");
            json.Int("t", T());
            json.Str("r", currentRoom);
            json.Str("k", kind);
            Log(Channel.Level, json);
        }

        /// <summary>
        /// Salto de posición que supera el umbral de teleport entre muestras: teleport (foso,
        /// respawn, checkpoint). Se emite ANTES de la muestra siguiente, con la sala de origen en
        /// <c>from</c>, y NO suma a <c>dist</c> (TEL-3). Después se re-atribuye sin esperar el paso
        /// de física.
        /// </summary>
        public void Teleported(Vector3 position, float distance)
        {
            if (!IsChannelEnabled(Channel.Level)) return;

            string from = currentRoom;
            lastResetAt = Time.unscaledTime;

            json.Begin("reset");
            json.Int("t", T());
            json.Str("r", from);
            json.Str("from", from);
            json.Num("x", position.x);
            json.Num("y", position.y);
            json.Num("z", position.z);
            json.Num("d", distance);
            Log(Channel.Level, json);

            string landing = ResolveRoomAt(position);
            if (landing != null)
            {
                if (landing != currentRoom) EnterRoom(landing);
            }
            else if (currentRoom != PlaytestRooms.NoRoom)
            {
                // Aterrizó en un corredor: la gracia decide si es sin-sala o un roce del vano.
                emptyDeadline = Time.unscaledTime + EmptyRoomGrace;
            }
        }

        /// <summary>
        /// Distancia caminada de sesión completa. La suma la hace el servicio para que sobreviva
        /// a las recargas de escena.
        /// </summary>
        public void AddDistance(float meters)
        {
            if (meters <= 0f) return;
            distanceTravelled += meters;
        }

        /// <summary>Golpe no letal (canal combat). <paramref name="damage"/> es prevHp - hp.</summary>
        public void Damaged(int damage, int hp)
        {
            // La marca de tiempo se anota SIEMPRE, antes del guard de canal: el probe del canal
            // level la usa para distinguir un teleport real del empujón del knockback (TEL-3).
            // Anotarla sólo con el canal combat abierto haría que -log=level perdiera el filtro.
            lastDamageAt = Time.unscaledTime;

            if (!IsChannelEnabled(Channel.Combat)) return;

            json.Begin("player.damaged");
            json.Int("t", T());
            json.Str("r", currentRoom);
            json.Int("dmg", damage);
            json.Int("hp", hp);
            Log(Channel.Combat, json);
        }

        /// <summary>Muerte: se DUPLICA en ambos canales (TEL-7). <c>hp</c> siempre 0.</summary>
        public void Died(string cause)
        {
            if (!IsEnabled) return;

            deaths++;

            json.Begin("player.died");
            json.Int("t", T());
            json.Str("r", currentRoom);
            json.Int("hp", 0);
            json.Str("cause", cause);
            LogBoth(json);
        }

        /// <summary>Revivir tras el respawn (canal level: pertenece al recorrido, no al combate).</summary>
        public void Respawned()
        {
            if (!IsChannelEnabled(Channel.Level)) return;

            json.Begin("player.respawn");
            json.Int("t", T());
            json.Str("r", currentRoom);
            Log(Channel.Level, json);
        }

        /// <summary>Checkpoint registrado (canal level).</summary>
        public void Checkpointed(int sectionId)
        {
            if (!IsChannelEnabled(Channel.Level)) return;

            json.Begin("checkpoint");
            json.Int("t", T());
            json.Str("r", currentRoom);
            json.Int("sec", sectionId);
            Log(Channel.Level, json);
        }

        /// <summary>Palanca accionada (canal level).</summary>
        public void ObjectiveProgress(int activated, int total)
        {
            if (!IsChannelEnabled(Channel.Level)) return;

            json.Begin("objective");
            json.Int("t", T());
            json.Str("r", currentRoom);
            json.Int("n", activated);
            json.Int("total", total);
            Log(Channel.Level, json);
        }

        /// <summary>
        /// El objetivo se completó. No agrega una línea: <c>Progress</c> ya emitió el último
        /// <c>objective</c> con <c>n == total</c>, y duplicarlo rompería el conteo.
        /// </summary>
        public void NotifyObjectiveComplete()
        {
            objectiveComplete = true;
        }

        /// <summary>Cambio de stance (canal level): <c>mantis</c> / <c>tarkatana</c>.</summary>
        public void StanceChanged(string stance)
        {
            if (!IsChannelEnabled(Channel.Level)) return;
            if (string.IsNullOrEmpty(stance)) return;

            json.Begin("stance");
            json.Int("t", T());
            json.Str("r", currentRoom);
            json.Str("s", stance);
            Log(Channel.Level, json);
        }

        /// <summary>Boss despertado (canal combat).</summary>
        public void Encounter(string boss)
        {
            if (!IsChannelEnabled(Channel.Combat)) return;

            json.Begin("encounter");
            json.Int("t", T());
            json.Str("r", currentRoom);
            json.Str("boss", boss);
            Log(Channel.Combat, json);
        }

        /// <summary>Cambio de fase del boss (canal combat).</summary>
        public void BossPhase(string boss, int phase)
        {
            if (!IsChannelEnabled(Channel.Combat)) return;

            json.Begin("boss.phase");
            json.Int("t", T());
            json.Str("r", currentRoom);
            json.Str("boss", boss);
            json.Int("ph", phase);
            Log(Channel.Combat, json);
        }

        /// <summary>Muerte del boss (canal combat).</summary>
        public void BossDied(string boss)
        {
            if (!IsChannelEnabled(Channel.Combat)) return;

            json.Begin("boss.died");
            json.Int("t", T());
            json.Str("r", currentRoom);
            json.Str("boss", boss);
            Log(Channel.Combat, json);
        }

        /// <summary>
        /// <c>flow</c> queda implementado para cuando exista un <c>GameFlowController</c> en la
        /// escena; en <c>Dungeon</c> no hay emisor y nunca se llama (design §7).
        /// </summary>
        public void Flow(string from, string to)
        {
            if (!IsChannelEnabled(Channel.Level)) return;

            json.Begin("flow");
            json.Int("t", T());
            json.Str("from", from);
            json.Str("to", to);
            Log(Channel.Level, json);
        }

        // ------------------------------------------------------------------ marca y combate (F5, F9)

        /// <summary>
        /// Marca manual F12 (MRK-1): <c>e,t,r,x,y,z,tr</c> y NADA más. La sala y la posición se
        /// leen en el instante de la pulsación, no de la última muestra de 4 Hz.
        ///
        /// En tránsito (corredor, <c>sin-sala</c>) la marca NO cae en una sala vacía: se atribuye
        /// a la última sala real pisada y se declara con <c>tr = 1</c>, así el reporte sabe que la
        /// posición no estaba dentro de esa sala. Dentro de una sala, <c>tr = 0</c>. Si la
        /// atribución dice <c>sin-sala</c> pero el punto SÍ está dentro de un probe (el caso de la
        /// pausa, que cierra el tramo como <c>sin-sala</c>), gana la sala física con <c>tr = 0</c>.
        ///
        /// Con el pausa abierto la marca es VÁLIDA: el jugador marca justamente cuando algo lo
        /// trabó, y la pausa no mueve al jugador ni cambia la atribución del momento. Sólo se
        /// rechaza si no hay canal <c>level</c> (sin sink no hay línea ni toast) o si no hay
        /// <c>PlayerMotor</c> (sin jugador no hay posición honesta que anotar).
        ///
        /// Devuelve true SÓLO si la línea se escribió: el llamador usa ese valor para decidir si
        /// muestra la confirmación (MRK-2 prohíbe confirmar una marca que no existe).
        /// </summary>
        public bool Mark()
        {
            if (!IsChannelEnabled(Channel.Level)) return false;
            if (motor == null) return false; // sin jugador no hay posición honesta

            Vector3 position = motor.transform.position;

            // Atribución: la sala vigente si la hay; si no, la sala FÍSICA del punto y, si tampoco,
            // la última sala real + tránsito. Si nunca se pisó una sala (arranque en un corredor)
            // queda sin-sala con tr = 1: se dice "en tránsito" en vez de inventar una sala que el
            // jugador no visitó.
            //
            // La sala física importa desde que la marca funciona EN PAUSA: al pausar, el tiempo
            // pausado se cierra como sin-sala (TEL-4) aunque el jugador siga parado dentro de una
            // sala. Sin este paso, marcar pausado en `lanzas` saldría como "lanzas (tránsito)",
            // que es falso: el jugador está dentro. Fuera de la pausa la atribución vigente ya es
            // la correcta y esta lectura devuelve lo mismo.
            string room = currentRoom;
            int transit = 0;
            if (room == PlaytestRooms.NoRoom)
            {
                string physical = ResolveRoomAt(position);
                if (physical != null)
                {
                    room = physical; // dentro de una sala: tr = 0
                }
                else
                {
                    transit = 1;
                    if (lastRoom != PlaytestRooms.NoRoom) room = lastRoom;
                }
            }

            json.Begin("mark");
            json.Int("t", T());
            json.Str("r", room);
            json.Num("x", position.x);
            json.Num("y", position.y);
            json.Num("z", position.z);
            json.Int("tr", transit);

            if (!Log(Channel.Level, json)) return false;

            markCount++;
            marksByRoom.TryGetValue(room, out int count);
            marksByRoom[room] = count + 1;
            return true;
        }

        /// <summary>
        /// Pulsación de ataque (canal level): <c>e,t,r,k,step,ms</c>. Es la CAUSA que le faltaba
        /// al canal combat, que sólo mide el efecto (<c>p</c>/<c>ph</c>): con esta línea la
        /// ventana de cancel se mide —"press en ms 120 y el paso avanzó 40 ms después"— en vez de
        /// asumirse.
        ///
        /// <paramref name="kind"/> vale <c>light</c>, <c>heavy</c> o <c>both</c> (las dos teclas en
        /// el mismo frame: una sola línea, no dos). <paramref name="step"/> es el
        /// <c>PlayerCombat.CurrentStepIndex</c> vigente al apretar (-1 = sin combo activo) y
        /// <paramref name="millis"/> los milisegundos unscaled desde el arranque de ese paso (-1
        /// si no hay paso). Una línea por pulsación: mantener la tecla no repite.
        /// </summary>
        public void InputPressed(string kind, int step, int millis)
        {
            if (!IsChannelEnabled(Channel.Level)) return;
            if (string.IsNullOrEmpty(kind)) return;

            json.Begin("in");
            json.Int("t", T());
            json.Str("r", currentRoom);
            json.Str("k", kind);
            json.Int("step", step);
            json.Int("ms", millis);
            Log(Channel.Level, json);
        }

        /// <summary>
        /// Transición del cursor de paso (canal level): <c>e,t,r,n,c</c>. Se emite en cada cambio
        /// de <c>PlayerCombat.CurrentStepIndex</c> —el arranque <c>n = 0</c>, cada avance y el
        /// cierre <c>n = -1</c>—, así el analista puede cruzar un <c>in</c> con el <c>step</c> que
        /// provocó y ver los que no provocaron ninguno.
        ///
        /// <paramref name="combo"/> es el nombre que ya resuelve <see cref="GameplayBindings"/>
        /// (el asset sólo se conoce cuando un golpe conecta); sin nombre queda <c>?</c>, nunca se
        /// inventa.
        /// </summary>
        public void StepChanged(int step, string combo)
        {
            if (!IsChannelEnabled(Channel.Level)) return;

            json.Begin("step");
            json.Int("t", T());
            json.Str("r", currentRoom);
            json.Int("n", step);
            json.Str("c", string.IsNullOrEmpty(combo) ? "?" : combo);
            Log(Channel.Level, json);
        }

        /// <summary>
        /// Resultado de UNA cadena de combo, al cerrarse (canal combat): <c>c</c> = combo,
        /// <c>n</c> = paso máximo alcanzado CONECTANDO (0-based; -1 si no conectó ninguno),
        /// <c>h</c> = golpes distintos que conectaron (la profundidad REAL de lo que tocó: en un
        /// combo ramificado <c>n</c> salta), <c>p</c> = paso máximo JUGADO, <c>ph</c> = pasos
        /// distintos jugados (la profundidad de lo que salió, toque o no), <c>len</c> = largo
        /// esperado y <c>ok</c> = llegó al último paso. Una línea por intento, no una por golpe:
        /// el reporte cuenta cadenas.
        ///
        /// <c>ph &gt; h</c> es el whiff: el paso salió y no encontró nada. Una cadena que nunca
        /// conectó llega con <c>n = -1</c>, <c>h = 0</c> y <c>c = "?"</c> (el asset sólo se conoce
        /// al impactar): es un intento medido, no un error.
        /// </summary>
        public void ComboResult(
            string combo,
            int reachedStep,
            int hits,
            int playedMax,
            int playedCount,
            int length,
            bool completed)
        {
            if (!IsChannelEnabled(Channel.Combat)) return;
            if (string.IsNullOrEmpty(combo)) return;

            json.Begin("combo");
            json.Int("t", T());
            json.Str("r", currentRoom);
            json.Str("c", combo);
            json.Int("n", reachedStep);
            json.Int("h", hits);
            json.Int("p", playedMax);
            json.Int("ph", playedCount);
            json.Int("len", length);
            json.Int("ok", completed ? 1 : 0);
            Log(Channel.Combat, json);
        }

        /// <summary>
        /// Daño INFLIGIDO a un <c>HitReceiver</c> (canal combat). <c>hp</c> es el HP restante que
        /// reporta el receptor después del golpe; el golpe letal no pasa por acá (no dispara
        /// <c>Damaged</c>), así que la muerte del enemigo no se cuenta como daño.
        /// </summary>
        public void EnemyDamaged(int damage, int hp)
        {
            if (!IsChannelEnabled(Channel.Combat)) return;

            json.Begin("enemy.damaged");
            json.Int("t", T());
            json.Str("r", currentRoom);
            json.Int("dmg", damage);
            json.Int("hp", hp);
            Log(Channel.Combat, json);
        }

        /// <summary>
        /// Un remate CONECTADO (canal combat): <c>k</c> = clase del receptor (<c>minion</c> si
        /// tiene <see cref="EnemyController"/>, <c>boss</c> si tiene <see cref="BossController"/>,
        /// <c>other</c> si ninguno), <c>stag</c> = entró en <c>Stagger</c> dentro de los 100 ms
        /// posteriores al impacto (0 en boss/other: no se intenta), y <c>dx/dz/d</c> = desplazamiento
        /// horizontal medido desde el impacto hasta 0.4 s después (unscaled). Una línea por víctima
        /// del remate: un cleave de 3 escribe 3 líneas con su propio desplazamiento.
        /// </summary>
        public void Ender(string kind, bool staggered, float dx, float dz, float distance)
        {
            if (!IsChannelEnabled(Channel.Combat)) return;

            json.Begin("ender");
            json.Int("t", T());
            json.Str("r", currentRoom);
            json.Str("k", kind);
            json.Int("stag", staggered ? 1 : 0);
            json.Num("dx", dx);
            json.Num("dz", dz);
            json.Num("d", distance);
            Log(Channel.Combat, json);
        }

        // ------------------------------------------------------------------ pausa

        /// <summary>
        /// Pausa externa (el panel de entrega de F6). Se combina con la pausa del juego: con
        /// cualquiera de las dos abiertas no se muestrea ni se atribuye tiempo a una sala.
        /// </summary>
        public void SetPaused(bool value)
        {
            externalPaused = value;
            PollPause();
        }

        /// <summary>
        /// Lee la pausa real (<see cref="ControlLock.IsPaused"/>, el mismo que consulta el dash) y
        /// aplica la transición. Con el pausa abierto se cierra la visita abierta y el tiempo
        /// pausado cae en <c>sin-sala</c>, no dentro de una sala (TEL-4).
        /// </summary>
        private void PollPause()
        {
            pauseHeld = ControlLock.IsPaused;

            bool now = pauseHeld || externalPaused;
            if (now == pausedState) return;
            pausedState = now;

            if (now)
            {
                // Ya en sin-sala no hay visita real que cerrar: cerrar y reabrir "sin-sala" sólo
                // escribe un par exit/enter consecutivo con el mismo `r` (el duplicado que
                // apareció en la sesión del 17/11 al pausar en un corredor). El tramo de
                // sin-sala sigue abierto y su dwell no entra en ninguna fila del presupuesto.
                if (currentRoom != PlaytestRooms.NoRoom)
                {
                    CloseVisit(PlaytestRooms.NoRoom);
                    OpenVisit(PlaytestRooms.NoRoom);
                }

                return;
            }

            // Al reanudar se re-resuelve con Contains: el jugador pudo moverse mientras no se
            // miraba (o el pausa lo dejó en otro volumen).
            string room = motor != null ? ResolveRoomAt(motor.transform.position) : null;
            if (room != null)
            {
                if (room != currentRoom) EnterRoom(room);
            }
            else if (currentRoom != PlaytestRooms.NoRoom)
            {
                emptyDeadline = Time.unscaledTime + EmptyRoomGrace;
            }
        }

        // ------------------------------------------------------------------ atribución

        /// <summary>
        /// Resuelve la sala de un punto con los probes de la escena, en el ORDEN del catálogo para
        /// que un solapamiento sea determinista. Null = ninguna sala (corredor).
        /// </summary>
        private string ResolveRoomAt(Vector3 position)
        {
            if (probes == null) return null;

            for (int row = 0; row < PlaytestRooms.All.Length; row++)
            {
                string id = PlaytestRooms.All[row].Id;
                for (int i = 0; i < probes.Length; i++)
                {
                    RoomProbe probeCandidate = probes[i];
                    if (probeCandidate == null || probeCandidate.RoomId != id) continue;
                    if (probeCandidate.Contains(position)) return id;
                }
            }

            return null;
        }

        /// <summary>Entra a una sala: cierra la visita anterior con <c>to</c> y abre la nueva.</summary>
        private void EnterRoom(string roomId)
        {
            if (roomId == currentRoom) return;

            CloseVisit(roomId);
            OpenVisit(roomId);
        }

        /// <summary>
        /// Cierra la visita abierta. El dwell se acumula por sala (lo consume el embudo del panel)
        /// y el tiempo de <c>sin-sala</c> no entra en ninguna fila del presupuesto.
        /// </summary>
        private void CloseVisit(string to)
        {
            emptyDeadline = -1f;
            if (!visitOpen) return;

            float now = Time.unscaledTime;
            float dwell = Mathf.Max(0f, now - visitStart);

            if (currentRoom != PlaytestRooms.NoRoom)
            {
                dwellByRoom.TryGetValue(currentRoom, out float accumulated);
                dwellByRoom[currentRoom] = accumulated + dwell;
            }

            json.Begin("room.exit");
            json.Int("t", T());
            json.Str("r", currentRoom);
            json.Str("to", to);
            json.Num("dwell", dwell);
            Log(Channel.Level, json);

            visitOpen = false;
        }

        private void OpenVisit(string roomId)
        {
            string from = currentRoom;

            currentRoom = roomId;
            if (roomId != PlaytestRooms.NoRoom) lastRoom = roomId;
            visitStart = Time.unscaledTime;
            visitOpen = true;
            emptyDeadline = -1f;

            json.Begin("room.enter");
            json.Int("t", T());
            json.Str("r", roomId);
            json.Str("from", from);
            Log(Channel.Level, json);
        }

        /// <summary>
        /// Materializa <c>sin-sala</c> cuando pasó la gracia sin que el jugador entrara a otra
        /// sala: así un roce del vano no parte una visita en dos.
        /// </summary>
        private void UpdateRoomGrace()
        {
            if (emptyDeadline < 0f) return;
            if (Time.unscaledTime < emptyDeadline) return;

            CloseVisit(PlaytestRooms.NoRoom);
            OpenVisit(PlaytestRooms.NoRoom);
        }

        /// <summary>Dwell acumulado de una sala (segundos). Lo usa el embudo del panel (F6).</summary>
        public float DwellOf(string roomId)
        {
            return dwellByRoom.TryGetValue(roomId, out float dwell) ? dwell : 0f;
        }

        /// <summary>True si hubo un teleport hace menos de <paramref name="window"/> segundos.</summary>
        public bool HadRecentReset(float window)
        {
            return lastResetAt >= 0f && Time.unscaledTime - lastResetAt <= window;
        }

        /// <summary>
        /// Instante unscaled del último <c>player.damaged</c>, o -1 si todavía no hubo ninguno.
        /// El probe lo usa para no confundir el empujón del knockback con un teleport.
        /// </summary>
        public float LastDamageAt => lastDamageAt;

        /// <summary>
        /// True si el último golpe recibido fue hace menos de <paramref name="window"/> segundos
        /// (unscaled). Un salto de &gt; 5 m dentro de esa ventana es el empujón del golpe, no un
        /// teleport: el <c>reset</c> mentiría sobre la sala y sobre el recorrido (TEL-3).
        /// </summary>
        public bool HadRecentDamage(float window)
        {
            return lastDamageAt >= 0f && Time.unscaledTime - lastDamageAt <= window;
        }

        /// <summary>
        /// Retrato de la sesión para el panel de entrega (F6). Sólo lecturas: pedirlo no escribe,
        /// no cierra el archivo y no toca el reloj (HND-2).
        /// </summary>
        public SessionSnapshot Snapshot()
        {
            return new SessionSnapshot(
                duration: Duration,
                deaths: deaths,
                distance: distanceTravelled,
                marks: markCount,
                path: SessionPath,
                root: activeRoot,
                usesFallback: usesFallbackRoot,
                levelPath: LevelPath,
                combatPath: CombatPath,
                logOff: shutDown,
                anyFailed: HasFailedChannel,
                levelEnabled: IsChannelEnabled(Channel.Level),
                combatEnabled: IsChannelEnabled(Channel.Combat),
                objectiveComplete: objectiveComplete);
        }

        // ------------------------------------------------------------------ escritura

        /// <summary>
        /// Funnel único: un evento va a UN canal. Sin boxing y sin switch de strings (design §3).
        /// Devuelve true SÓLO si la línea entró al buffer del canal: la marca F12 depende de eso
        /// para no confirmar en falso (MRK-2).
        /// </summary>
        private bool Log(Channel channel, JsonLineBuilder line)
        {
            JsonlSink sink = SinkOf(channel);
            if (sink == null || !sink.IsOpen) return false;

            return sink.Write(line.End());
        }

        /// <summary>
        /// Los eventos compartidos (<c>session.start</c>, <c>session.end</c>, <c>player.died</c>)
        /// se emiten una vez POR canal: duplicar por consumidor es más barato que acoplar los
        /// archivos (TEL-7).
        /// </summary>
        private bool LogBoth(JsonLineBuilder line)
        {
            string built = line.End();
            bool wrote = false;

            if (levelSink != null && levelSink.IsOpen && levelSink.Write(built)) wrote = true;
            if (combatSink != null && combatSink.IsOpen && combatSink.Write(built)) wrote = true;

            return wrote;
        }

        private JsonlSink SinkOf(Channel channel)
        {
            return channel == Channel.Combat ? combatSink : levelSink;
        }

        /// <summary>True sólo con ese canal abierto y sin fallo de IO.</summary>
        public bool IsChannelEnabled(Channel channel)
        {
            JsonlSink sink = SinkOf(channel);
            return sink != null && sink.IsOpen;
        }

        private void FlushSinks()
        {
            if (levelSink != null) levelSink.Flush();
            if (combatSink != null) combatSink.Flush();
        }

        private void CloseSinks()
        {
            if (levelSink != null) levelSink.Close();
            if (combatSink != null) combatSink.Close();
        }

        /// <summary>
        /// Milisegundos enteros desde <c>session.start</c>, con clamp monótono: un <c>t</c> que
        /// retrocede rompe cualquier cruce por tiempo (TEL-3).
        /// </summary>
        private int T()
        {
            int milliseconds = Mathf.RoundToInt((Time.unscaledTime - t0) * 1000f);
            if (milliseconds < lastT) milliseconds = lastT;
            lastT = milliseconds;
            return milliseconds;
        }
    }

    /// <summary>
    /// Retrato inmutable de la sesión para el panel de entrega (HND-2). Se arma en <see
    /// cref="PlaytestLog.Snapshot"/> con lecturas puras: sin IO, sin cerrar el archivo y sin
    /// escribir ningún evento.
    /// </summary>
    public readonly struct SessionSnapshot
    {
        /// <summary>Duración unscaled desde <c>session.start</c>, en segundos.</summary>
        public readonly float Duration;

        /// <summary>Muertes de la sesión.</summary>
        public readonly int Deaths;

        /// <summary>Distancia caminada acumulada, en metros.</summary>
        public readonly float Distance;

        /// <summary>Marcas F12 registradas.</summary>
        public readonly int Marks;

        /// <summary>Ruta del archivo principal (primer canal abierto) o null si no hay.</summary>
        public readonly string Path;

        /// <summary>Carpeta activa de grabación (raíz + jugador) o null si no hay archivo.</summary>
        public readonly string Root;

        /// <summary>True si se grabó en el fallback de <c>persistentDataPath</c> (TEL-1).</summary>
        public readonly bool UsesFallback;

        /// <summary>Ruta del canal <c>level</c> o null.</summary>
        public readonly string LevelPath;

        /// <summary>Ruta del canal <c>combat</c> o null.</summary>
        public readonly string CombatPath;

        /// <summary>True con <c>-nolog</c>.</summary>
        public readonly bool LogOff;

        /// <summary>True si un canal se apagó por un fallo de IO.</summary>
        public readonly bool AnyFailed;

        /// <summary>True si el canal <c>level</c> está abierto.</summary>
        public readonly bool LevelEnabled;

        /// <summary>True si el canal <c>combat</c> está abierto.</summary>
        public readonly bool CombatEnabled;

        /// <summary>True si el objetivo contó todas las palancas.</summary>
        public readonly bool ObjectiveComplete;

        public SessionSnapshot(
            float duration,
            int deaths,
            float distance,
            int marks,
            string path,
            string root,
            bool usesFallback,
            string levelPath,
            string combatPath,
            bool logOff,
            bool anyFailed,
            bool levelEnabled,
            bool combatEnabled,
            bool objectiveComplete)
        {
            Duration = duration;
            Deaths = deaths;
            Distance = distance;
            Marks = marks;
            Path = path;
            Root = root;
            UsesFallback = usesFallback;
            LevelPath = levelPath;
            CombatPath = combatPath;
            LogOff = logOff;
            AnyFailed = anyFailed;
            LevelEnabled = levelEnabled;
            CombatEnabled = combatEnabled;
            ObjectiveComplete = objectiveComplete;
        }
    }
}
