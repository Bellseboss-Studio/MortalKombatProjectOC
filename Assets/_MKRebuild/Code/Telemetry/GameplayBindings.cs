using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace MK.Rebuild.Telemetry
{
    /// <summary>
    /// Enlaza los eventos REALES del gameplay al <see cref="PlaytestLog"/>. Un solo lugar, cero
    /// ediciones de gameplay (TEL-6): sólo se suscribe a lo que ya existe.
    ///
    /// Contrato de vida: <see cref="Bind"/> siempre empieza por <see cref="Unbind"/>, y
    /// <see cref="OnDisable"/> / <see cref="OnDestroy"/> dan de baja todo. El único evento
    /// <c>static</c> del proyecto que interesaría (<c>HitReceiver.AnyDied</c>) NO se usa: el canal
    /// <c>combat</c> no cuenta enemigos comunes y <c>Dungeon</c> no tiene <c>EnemySpawner</c>
    /// (TEL-2 los deja reservados).
    ///
    /// Lo que NO se puede saber sin tocar gameplay (documentado, no inventado):
    /// - <c>dmg</c> recibido se reconstruye con el HP del frame anterior, porque <c>OnDamaged</c> no
    ///   lleva payload y el golpe letal no lo dispara.
    /// - <c>cause</c> es heurística: <c>fall</c> (hubo <c>reset</c> hace &lt; 2 s), <c>boss</c>
    ///   (boss vivo a &lt; 12 m) o <c>unknown</c>.
    /// - El daño INFLIGIDO se lee del <c>Damaged</c> de cada <see cref="HitReceiver"/> enlazado: el
    ///   golpe letal no dispara ese evento, así que el último golpe de cada enemigo no se cuenta.
    /// - El largo de la cadena sale del <see cref="ComboDefinition"/> cargado: la cantidad de
    ///   pasos de su cadena principal SÓLO si esa cadena es lineal (índices consecutivos), porque
    ///   <c>n</c> es el índice del paso. Si el asset no se resuelve con certeza —incluidos los
    ///   combos ramificados como <c>Combo_1</c>—, <c>len = -1</c> y <c>ok = 0</c>.
    /// - La profundidad REAL de la cadena no es <c>n</c> sino <c>h</c>: en un combo ramificado
    ///   (0→6, 0→1→2→6…) el índice del paso salta, así que <c>n</c> mide la numeración del asset
    ///   y <c>h</c> cuenta cuántos golpes distintos conectaron. Es la métrica que responde
    ///   "¿encadenó y tocó?".
    /// - <c>h</c> sólo cuenta IMPACTOS, así que una cadena que sale y whifea dejaría <c>h = 1</c>
    ///   y se leería como "no encadenó". Por eso cada línea trae además <c>p</c> (paso máximo
    ///   jugado) y <c>ph</c> (pasos distintos jugados), polleados del cursor público
    ///   <see cref="PlayerCombat.CurrentStepIndex"/> cada frame: "¿encadenó?" es <c>ph</c>,
    ///   "¿tocó?" es <c>h</c>, y la diferencia es el whiff.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class GameplayBindings : MonoBehaviour
    {
        /// <summary>Ventana de la heurística <c>fall</c> (igual que la del servicio).</summary>
        private const float FallResetWindow = 2f;

        /// <summary>Distancia a la que un boss vivo explica una muerte.</summary>
        private const float BossKillRadius = 12f;

        /// <summary>Ventana (s, unscaled) en la que el receptor cuenta como aturdido por el remate.</summary>
        private const float EnderStaggerSeconds = 0.1f;

        /// <summary>Ventana (s, unscaled) de muestreo del desplazamiento horizontal del remate.</summary>
        private const float EnderSampleSeconds = 0.4f;

        /// <summary>
        /// Nombre de la cadena que nunca conectó un golpe. El nombre del asset sólo se conoce por
        /// <see cref="PlayerCombat.OnStepConnected"/>, así que una cadena que salió entera al aire
        /// no tiene nombre que reportar: se escribe el centinela <c>?</c> con <c>n = -1</c>,
        /// <c>h = 0</c>, <c>len = -1</c> y <c>ok = 0</c> (TEL-2). Es honesto: dice "hubo intento,
        /// no tocó nadie", en vez de inventar un asset o borrar el intento del archivo.
        /// </summary>
        private const string UnknownCombo = "?";

        private PlayerDamage damage;
        private PlayerStance stance;
        private PlayerCombat combat;
        private DungeonObjective objective;
        private GameFlowController flow;
        private BossSpawner[] spawners = Array.Empty<BossSpawner>();
        private BossEncounterTrigger[] triggers = Array.Empty<BossEncounterTrigger>();

        private readonly List<BossBinding> bossBindings = new List<BossBinding>(2);
        private readonly List<TriggerBinding> triggerBindings = new List<TriggerBinding>(2);
        private readonly List<ReceiverBinding> receiverBindings = new List<ReceiverBinding>(8);
        private readonly Dictionary<string, int> comboLengths = new Dictionary<string, int>(4);

        private bool subscribed;
        private int lastHp;
        private int lastEncounterFrame = -1;
        private float lastReceiverScan = -1f;

        /// <summary>Receptor del último impacto del paso en curso, para emparejarlo con el remate.</summary>
        private HitReceiver lastVictim;

        /// <summary>Frame del impacto: el par <c>Damaged</c>→<c>OnStepConnected</c> vive en UN frame.</summary>
        private int lastVictimFrame = -1;

        /// <summary>Matrices de stance cargadas (mismo truco que <see cref="ResolveComboLength"/>).</summary>
        private StanceComboMatrix[] matrices;

        /// <summary>Cadena de combo abierta (F9). Se cierra al terminar el ataque y emite UNA línea.</summary>
        private ChainCapture openChain;

        /// <summary>
        /// Nombre del combo de la cadena ABIERTA, o <c>null</c> si todavía no hay nombre (la
        /// cadena se abrió por <c>Attacking</c> y ningún golpe conectó) o no hay cadena.
        ///
        /// Se expone de sólo lectura para que <see cref="InputProbe"/> use ESTA resolución en sus
        /// eventos <c>step</c> en vez de inventar otra: el nombre del asset sólo existe en el
        /// move-key <c>Combo_1#2</c> que dispara <see cref="PlayerCombat.OnStepConnected"/>. Es un
        /// getter puro —no suscribe, no escribe, no cambia el ciclo de la cadena—.
        /// </summary>
        public string ActiveComboName => openChain != null ? openChain.Combo : null;

        /// <summary>Un boss enlazado: guarda el controller, su id y los delegados para poder dar de baja.</summary>
        private sealed class BossBinding
        {
            public BossController Controller;
            public string Id;
            public Action<int> OnPhase;
            public Action OnDied;
        }

        /// <summary>Un trigger de entrada enlazado (Triggered no lleva emisor, hay que recordarlo).</summary>
        private sealed class TriggerBinding
        {
            public BossEncounterTrigger Trigger;
            public string BossId;
            public Action Handler;
        }

        /// <summary>
        /// Un <see cref="HitReceiver"/> enlazado para el daño INFLIGIDO (F9). Se guarda el delegado
        /// para poder darse de baja aunque el receptor ya esté destruido.
        /// </summary>
        private sealed class ReceiverBinding
        {
            public HitReceiver Receiver;
            public Action<int, Vector3, float> Handler;
            public Action<HitReceiver> LethalHandler;
        }

        /// <summary>
        /// Cadena en curso. Se abre con el primer paso JUGADO —aunque whifee— y se cierra al caer
        /// <see cref="PlayerCombat.Attacking"/>, que es el único fin de cadena que el gameplay
        /// expone.
        ///
        /// Dos profundidades, dos preguntas:
        /// - <see cref="Reached"/>/<see cref="Hits"/> = pasos que CONECTARON (campos <c>n</c>/<c>h</c>).
        /// - <see cref="MaxPlayed"/>/<see cref="PlayedSteps"/> = pasos que SALIERON
        ///   (campos <c>p</c>/<c>ph</c>). Una cadena 0→1→2 que sólo conecta el primero deja
        ///   <c>p = 2, ph = 3, n = 0, h = 1</c>: eso es el whiff, visible.
        ///
        /// <see cref="Reached"/> es el MÁXIMO que anunció el evento (<c>Combo_1#2</c>), nunca el
        /// cursor de <see cref="PlayerCombat"/> al cerrar (que ya vale -1). Arranca en -1: una
        /// cadena abierta por <c>Attacking</c> que no tocó nada no tiene paso conectado, y <c>h</c>
        /// queda en 0 (el reporte lo lee como "0 conectados", no como error).
        ///
        /// <see cref="HitSteps"/> des-duplica por paso y no por evento: un paso con
        /// <c>maxTargets &gt; 1</c> (cleave) dispara <see cref="PlayerCombat.OnStepConnected"/> una
        /// vez por víctima, y contar eventos haría crecer la profundidad con la cantidad de
        /// enemigos. <see cref="PlayedSteps"/> dedupea igual: los ~20 frames de un mismo paso son
        /// UN paso. Ambas listas son cortas (≤ pasos del combo) y viven una vez por cadena.
        /// </summary>
        private sealed class ChainCapture
        {
            /// <summary>Nombre del combo; <c>null</c> hasta que un golpe conecta.</summary>
            public string Combo;

            /// <summary>Paso máximo CONECTADO (0-based, como <c>Combo_1#2</c>); -1 si ninguno.</summary>
            public int Reached = -1;

            /// <summary>Cantidad de pasos DISTINTOS que conectaron.</summary>
            public int Hits;

            /// <summary>Pasos que conectaron, sin repetir (cleave no infla).</summary>
            public readonly List<int> HitSteps = new List<int>(4);

            /// <summary>Paso máximo JUGADO (<c>p</c>); -1 mientras no se haya jugado ninguno.</summary>
            public int MaxPlayed = -1;

            /// <summary>Pasos DISTINTOS jugados (<c>ph</c>): la profundidad que salió, toque o no.</summary>
            public readonly List<int> PlayedSteps = new List<int>(4);

            /// <summary>Anota un paso JUGADO (lo llaman el polling por frame y el impacto).</summary>
            public void NotePlayed(int step)
            {
                if (step < 0) return;
                if (step > MaxPlayed) MaxPlayed = step;
                if (!PlayedSteps.Contains(step)) PlayedSteps.Add(step);
            }

            /// <summary>Anota un paso CONECTADO: suma a <c>n</c>/<c>h</c> y también a <c>p</c>/<c>ph</c>.</summary>
            public void NoteConnected(int step)
            {
                NotePlayed(step);                  // un golpe que conecta es, antes que nada, un paso jugado
                if (step > Reached) Reached = step;
                if (HitSteps.Contains(step)) return;
                HitSteps.Add(step);
                Hits++;
            }
        }

        // ------------------------------------------------------------------ ciclo de vida

        private void OnEnable()
        {
            Subscribe();
        }

        private void OnDisable()
        {
            Unbind();
        }

        private void OnDestroy()
        {
            Unbind();
        }

        /// <summary>
        /// Sincroniza el HP conocido cuando sube sin evento (Heal + Revive del respawn). Sin esto,
        /// el primer golpe después de revivir reportaría un <c>dmg</c> enorme.
        /// </summary>
        private void LateUpdate()
        {
            // Profundidad JUGADA (p/ph): se pollea ANTES del cierre para que el último paso de la
            // cadena quede contado, y en LateUpdate porque es después de que `PlayerCombat.Update`
            // movió el cursor. Ver TrackPlayedStep.
            TrackPlayedStep();

            // Cierre de la cadena de combo (F9): el controller avisa "conecté el paso N" pero no
            // avisa "la cadena terminó". Attacking pasa a false cuando cae el último paso, así que
            // ese flanco es el único lugar donde cabe UNA línea por intento.
            if (openChain != null && (combat == null || !combat.Attacking)) CloseChain();

            if (damage == null) return;

            int hp = damage.HP;
            if (hp > lastHp) lastHp = hp;
        }

        /// <summary>
        /// Profundidad JUGADA de la cadena (<c>p</c> = paso máximo jugado, <c>ph</c> = pasos
        /// distintos jugados). Se lee el seam público de sólo lectura
        /// <see cref="PlayerCombat.CurrentStepIndex"/> cada frame mientras la cadena está abierta
        /// (<see cref="PlayerCombat.Attacking"/>), sin tocar gameplay.
        ///
        /// Por qué el cursor y no el evento: <see cref="PlayerCombat.OnStepConnected"/> dispara
        /// por IMPACTO, así que una cadena 0→1→2 con los pasos 1 y 2 fuera de alcance (el
        /// knockback saca al enemigo) sólo dejaba <c>h = 1</c> y parecía "no encadenó". El cursor
        /// avanza cuando el jugador ENCADENA el paso, conecte o no: eso es lo que mide <c>ph</c>.
        ///
        /// Abre la cadena SIN impacto: si el paso 0 whifea no hay evento y, sin esto, el intento
        /// no existiría en el archivo. La cadena queda sin nombre hasta el primer golpe y se
        /// reporta con <c>c = "?"</c> si nunca conecta. <c>Attacking</c> también es true en el
        /// swing gris (sin combo, cursor -1): ese frame no cuenta pasos, así que no inventa
        /// cadenas ni profundidad.
        ///
        /// Limitación conocida (documentada, no corregida): el cursor es un índice POR asset. Si
        /// la cadena adopta otro combo (opener de dash → roots normales) y el nuevo arranca en 0,
        /// ese 0 puede repetir un índice ya anotado y <c>ph</c> cuenta un paso menos. Los índices
        /// no distinguen assets sin exponer el combo activo desde gameplay.
        /// </summary>
        private void TrackPlayedStep()
        {
            if (combat == null || !combat.Attacking) return;

            int step = combat.CurrentStepIndex;
            if (step < 0) return; // ataque sin cadena (swing gris): no es un paso de combo

            if (openChain == null) openChain = new ChainCapture();
            openChain.NotePlayed(step);
        }

        /// <summary>
        /// Enlaza la escena nueva. Busca el objetivo y los dos spawners/triggers de la escena;
        /// los bosses concretos llegan por <see cref="BossSpawner.Spawned"/>.
        /// </summary>
        public void Bind(PlayerDamage playerDamage, PlayerStance playerStance, PlayerCombat playerCombat)
        {
            Unbind();

            damage = playerDamage;
            stance = playerStance;
            combat = playerCombat;
            objective = FindFirstObjectByType<DungeonObjective>();

            // Defensivo: en Dungeon NO hay GameFlowController, así que `flow` nunca emite. La
            // suscripción queda lista para cualquier escena que sí lo tenga (design §7).
            flow = GameFlowController.Instance;

            spawners = FindObjectsByType<BossSpawner>(FindObjectsSortMode.None);
            triggers = FindObjectsByType<BossEncounterTrigger>(FindObjectsSortMode.None);
            lastHp = damage != null ? damage.HP : 0;

            Subscribe();

            // Los receptores de la escena (dummies, bosses ya presentes) se enlazan para el daño
            // infligido. Los que aparezcan después se suman al spawnear un boss (F9).
            BindReceivers(true);
        }

        /// <summary>Da de baja TODO y olvida las referencias de la escena vieja (TEL-6).</summary>
        public void Unbind()
        {
            if (subscribed)
            {
                if (damage != null)
                {
                    damage.OnDamaged -= HandleDamaged;
                    damage.OnDied -= HandleDied;
                }

                if (stance != null) stance.OnStanceChanged -= HandleStance;

                if (combat != null) combat.OnStepConnected -= HandleStepConnected;

                if (objective != null)
                {
                    objective.Progress -= HandleObjectiveProgress;
                    objective.Completed -= HandleObjectiveCompleted;
                }

                if (flow != null) flow.StateChanged -= HandleFlow;

                for (int i = 0; i < spawners.Length; i++)
                {
                    if (spawners[i] != null) spawners[i].Spawned -= HandleSpawned;
                }

                for (int i = 0; i < triggerBindings.Count; i++)
                {
                    TriggerBinding binding = triggerBindings[i];
                    if (binding.Trigger != null) binding.Trigger.Triggered -= binding.Handler;
                }
            }

            subscribed = false;
            triggerBindings.Clear();

            // La cadena abierta se reporta antes de soltar todo: es evidencia del intento.
            CloseChain();
            UnbindReceivers();

            for (int i = bossBindings.Count - 1; i >= 0; i--) UnbindBoss(bossBindings[i]);
            bossBindings.Clear();

            damage = null;
            stance = null;
            combat = null;
            objective = null;
            flow = null;
            spawners = Array.Empty<BossSpawner>();
            triggers = Array.Empty<BossEncounterTrigger>();
            lastHp = 0;

            // La víctima del impacto no sobrevive a la escena vieja.
            lastVictim = null;
        }

        /// <summary>
        /// Suscripción idempotente: <c>-=</c> antes de <c>+=</c> para que un segundo pase no
        /// acumule handlers.
        /// </summary>
        private void Subscribe()
        {
            if (subscribed) return;
            subscribed = true;

            if (damage != null)
            {
                damage.OnDamaged -= HandleDamaged;
                damage.OnDamaged += HandleDamaged;
                damage.OnDied -= HandleDied;
                damage.OnDied += HandleDied;
            }

            if (stance != null)
            {
                stance.OnStanceChanged -= HandleStance;
                stance.OnStanceChanged += HandleStance;
            }

            if (combat != null)
            {
                combat.OnStepConnected -= HandleStepConnected;
                combat.OnStepConnected += HandleStepConnected;
            }

            if (objective != null)
            {
                objective.Progress -= HandleObjectiveProgress;
                objective.Progress += HandleObjectiveProgress;
                objective.Completed -= HandleObjectiveCompleted;
                objective.Completed += HandleObjectiveCompleted;
            }

            if (flow != null)
            {
                flow.StateChanged -= HandleFlow;
                flow.StateChanged += HandleFlow;
            }

            for (int i = 0; i < spawners.Length; i++)
            {
                BossSpawner spawner = spawners[i];
                if (spawner == null) continue;
                spawner.Spawned -= HandleSpawned;
                spawner.Spawned += HandleSpawned;
            }

            for (int i = 0; i < triggers.Length; i++)
            {
                BossEncounterTrigger trigger = triggers[i];
                if (trigger == null) continue;

                // Triggered no lleva emisor: el id del boss sale del nombre del host
                // ("GuardianEncounter_Entry" -> guardian), y el delegado se guarda para la baja.
                var binding = new TriggerBinding
                {
                    Trigger = trigger,
                    BossId = ResolveBossIdFromName(trigger.transform.parent != null
                        ? trigger.transform.parent.name
                        : trigger.gameObject.name)
                };
                binding.Handler = () => HandleTriggered(binding.BossId);
                trigger.Triggered -= binding.Handler;
                trigger.Triggered += binding.Handler;
                triggerBindings.Add(binding);
            }
        }

        // ------------------------------------------------------------------ jugador

        private void HandleDamaged()
        {
            if (damage == null) return;

            int hp = damage.HP;
            int dealt = Mathf.Max(0, lastHp - hp);
            lastHp = hp;

            PlaytestLog.Instance?.Damaged(dealt, hp);
        }

        private void HandleDied()
        {
            lastHp = 0;

            PlaytestLog log = PlaytestLog.Instance;
            if (log == null) return;

            string cause = "unknown";
            if (log.HadRecentReset(FallResetWindow)) cause = "fall";
            else if (AnyLiveBossNear(BossKillRadius)) cause = "boss";

            log.Died(cause);
        }

        private void HandleStance(PlayerStance.Stance newStance)
        {
            PlaytestLog.Instance?.StanceChanged(newStance == PlayerStance.Stance.Tarkatana ? "tarkatana" : "mantis");
        }

        // ------------------------------------------------------------------ objetivos

        private void HandleObjectiveProgress(int activated, int total)
        {
            PlaytestLog.Instance?.ObjectiveProgress(activated, total);
        }

        private void HandleObjectiveCompleted()
        {
            // Sin línea propia: Progress ya emitió el último objective con n == total.
            PlaytestLog.Instance?.NotifyObjectiveComplete();
        }

        /// <summary>
        /// <c>flow</c> defensivo: en <c>Dungeon</c> no hay <see cref="GameFlowController"/>, así que
        /// este handler nunca corre hoy. Se deja listo y con la misma baja que el resto.
        /// </summary>
        private void HandleFlow(FlowState previous, FlowState next)
        {
            PlaytestLog.Instance?.Flow(
                previous.ToString().ToLowerInvariant(),
                next.ToString().ToLowerInvariant());
        }

        // ------------------------------------------------------------------ combate (F9)

        /// <summary>
        /// Un paso CONECTÓ: <c>"Combo_1#2"</c> (o <c>"Attack_Light#0"</c> en el fallback gris).
        /// El canal <c>combat</c> no escribe una línea por golpe: acumula la cadena y la reporta
        /// UNA vez al cerrarse (<see cref="CloseChain"/>), porque al reporte le interesa el intento.
        ///
        /// FUENTE DE VERDAD de <c>n</c>: el propio string del evento. <c>c</c> sale de lo que hay
        /// antes del <c>#</c> y <c>n</c> del entero que sigue. NO se lee
        /// <see cref="PlayerCombat.CurrentStepIndex"/> al cerrar: en el cierre el cursor ya volvió
        /// a -1/0 y la línea diría <c>n = 0</c> siempre. Se guarda el MÁXIMO por cadena.
        ///
        /// FUENTE DE VERDAD de <c>h</c>: la cantidad de pasos DISTINTOS que conectaron. El evento
        /// se dispara una vez por víctima nueva (<c>maxTargets</c> puede ser 3 en <c>Combo_1</c>),
        /// así que se deduplica por paso: un cleave no inventa profundidad.
        ///
        /// La cadena puede estar YA abierta y sin nombre: la abrió <see cref="TrackPlayedStep"/>
        /// por <c>Attacking</c> en un paso que todavía no había tocado. Ese primer golpe le pone
        /// nombre; NO es otra cadena (cerrarla acá tiraría los pasos jugados antes del impacto).
        ///
        /// Mismo <c>moveKey</c> repetido (cleave a varios objetivos) no inventa un paso nuevo.
        /// </summary>
        private void HandleStepConnected(string moveKey)
        {
            HitReceiver victim = lastVictimFrame == Time.frameCount ? lastVictim : null;
            lastVictim = null;

            if (!TryParseMoveKey(moveKey, out string combo, out int step)) return;

            if (openChain != null && openChain.Combo == null)
            {
                // Cadena abierta por un paso jugado que no tocó: el primer impacto le da nombre.
                openChain.Combo = combo;
                BindReceivers(); // mismo momento barato que el alta por impacto (throttle 1 s)
            }
            else if (openChain != null && openChain.Combo != combo)
            {
                // Otra cadena (el opener de dash encadena a los roots normales): se cierra la anterior.
                CloseChain();
            }

            if (openChain == null)
            {
                var chain = new ChainCapture { Combo = combo };
                openChain = chain;

                // Primer golpe de la cadena: momento barato para sumar receptores que aparecieron
                // con un spawn (el boss ya existe cuando la pelea empieza).
                BindReceivers();
            }

            openChain.NoteConnected(step);

            // Remate (R4): el paso conectado es un Closer y su víctima se guardó en ESE impacto.
            if (victim != null && IsCloserStep(combo, step)) StartEnder(victim);
        }

        /// <summary>True si el paso es el Closer de la matriz del stance vigente (indeciso = false).</summary>
        private bool IsCloserStep(string combo, int step)
        {
            if (stance == null || string.IsNullOrEmpty(combo)) return false;

            if (matrices == null || matrices.Length == 0)
                matrices = Resources.FindObjectsOfTypeAll<StanceComboMatrix>();
            ComboDefinition[] definitions = Resources.FindObjectsOfTypeAll<ComboDefinition>();

            for (int i = 0; i < matrices.Length; i++)
            {
                if (matrices[i] == null || matrices[i].Stance != stance.CurrentStance) continue;

                for (int j = 0; j < definitions.Length; j++)
                {
                    if (definitions[j] == null || definitions[j].name != combo) continue;
                    return matrices[i].RoleOf(new ComboMoveRef { combo = definitions[j], stepIndex = step })
                        == ComboRole.Closer;
                }
            }

            return false;
        }

        private void StartEnder(HitReceiver receiver) => StartCoroutine(MeasureEnder(receiver));

        /// <summary>
        /// Mide UN remate conectado (una corrutina por víctima): <c>stag</c> = entró en Stagger
        /// dentro de los 100 ms del impacto (sólo <c>minion</c>: boss/other no lo intentan) y
        /// <c>dx/dz/d</c> = desplazamiento horizontal desde el impacto hasta 0.4 s después,
        /// unscaled. Si el receptor se destruye antes, se emite con lo último medido (d parcial).
        /// </summary>
        private IEnumerator MeasureEnder(HitReceiver receiver)
        {
            EnemyController enemy = receiver.GetComponentInParent<EnemyController>();
            string kind = enemy != null ? "minion"
                : receiver.GetComponentInParent<BossController>() != null ? "boss" : "other";
            Vector3 origin = receiver.transform.position;
            Vector3 last = origin;
            float staggerDeadline = Time.unscaledTime + EnderStaggerSeconds;
            float deadline = Time.unscaledTime + EnderSampleSeconds;
            bool staggered = false;

            while (receiver != null && Time.unscaledTime < deadline)
            {
                if (enemy != null && !staggered && Time.unscaledTime <= staggerDeadline
                    && enemy.DebugState == "Stagger")
                    staggered = true;
                last = receiver.transform.position;
                yield return null;
            }

            Vector3 delta = last - origin;
            PlaytestLog.Instance?.Ender(kind, staggered, delta.x, delta.z,
                Mathf.Sqrt(delta.x * delta.x + delta.z * delta.z));
        }

        /// <summary>
        /// Separa <c>"Combo_1#2"</c> en nombre y paso. El <c>#</c> se busca desde el final porque
        /// es el separador que fija <see cref="PlayerCombat"/>; un string sin paso válido se
        /// descarta entero (mejor perder un intento que anotar un paso inventado).
        /// </summary>
        private static bool TryParseMoveKey(string moveKey, out string combo, out int step)
        {
            combo = null;
            step = 0;

            if (string.IsNullOrEmpty(moveKey)) return false;

            int separator = moveKey.LastIndexOf('#');
            if (separator <= 0 || separator >= moveKey.Length - 1) return false;

            if (!int.TryParse(
                    moveKey.Substring(separator + 1),
                    NumberStyles.Integer,
                    CultureInfo.InvariantCulture,
                    out step))
                return false;

            combo = moveKey.Substring(0, separator);
            return combo.Length > 0;
        }

        /// <summary>
        /// Cierra la cadena abierta y emite <c>combo{c,n,h,p,ph,len,ok}</c>.
        /// <c>n</c> es el paso máximo que el propio evento <c>"Combo_1#2"</c> anunció (ver
        /// <see cref="HandleStepConnected"/>); <c>h</c> es la cantidad de pasos distintos que
        /// conectaron; <c>p</c> es el paso máximo JUGADO y <c>ph</c> los pasos distintos jugados,
        /// con o sin impacto — juntos responden "¿encadenó?" (<c>ph</c>) separado de "¿tocó?"
        /// (<c>h</c>). <c>len</c> es el largo inequívoco del <see cref="ComboDefinition"/> real y
        /// queda en -1 cuando no se puede afirmar sin adivinar. <c>ok</c> exige igualdad exacta
        /// con el último paso: con <c>len</c> desconocido nadie puede declarar el combo completo.
        ///
        /// Una cadena que nunca conectó no tiene nombre de asset: se emite con
        /// <see cref="UnknownCombo"/> (<c>?</c>), <c>n = -1</c> y <c>h = 0</c>. Antes ese intento
        /// no dejaba línea y el reporte lo contaba como "no encadenó"; ahora se ve como
        /// "encadenó 2 pasos y no tocó ninguno".
        /// </summary>
        private void CloseChain()
        {
            ChainCapture chain = openChain;
            openChain = null;
            if (chain == null) return;

            string combo = string.IsNullOrEmpty(chain.Combo) ? UnknownCombo : chain.Combo;
            int length = ResolveComboLength(chain.Combo);
            bool completed = length > 0 && chain.Reached == length - 1;

            PlaytestLog.Instance?.ComboResult(
                combo,
                chain.Reached,
                chain.Hits,
                chain.MaxPlayed,
                chain.PlayedSteps.Count,
                length,
                completed);
        }

        /// <summary>
        /// Largo de un combo por nombre de asset EXACTO, y sólo cuando el largo es INEQUÍVOCO.
        /// <see cref="PlayerCombat"/> no expone su array de combos, pero los assets referenciados
        /// están cargados y <see cref="Resources.FindObjectsOfTypeAll{T}"/> los alcanza sin tocar
        /// gameplay.
        ///
        /// Devuelve -1 (el reporte lo muestra como <c>-</c>) en todos los casos dudosos: asset
        /// ausente, nombre distinto, asset sin pasos y —clave— cualquier combo cuya cadena
        /// principal no sea lineal. NUNCA se inventa un largo: un número falso cuesta más que un
        /// dato ausente.
        /// </summary>
        private int ResolveComboLength(string comboName)
        {
            if (string.IsNullOrEmpty(comboName)) return -1;
            if (comboLengths.TryGetValue(comboName, out int cached)) return cached;

            int length = -1;
            bool found = false;
            ComboDefinition[] definitions = Resources.FindObjectsOfTypeAll<ComboDefinition>();
            for (int i = 0; i < definitions.Length; i++)
            {
                ComboDefinition definition = definitions[i];
                // Nombre EXACTO: Resources devuelve todo lo cargado y un asset homónimo parcial
                // cae en -1 en vez de prestar un largo ajeno.
                if (definition == null || definition.name != comboName) continue;

                found = true;
                length = LinearLength(definition);
                break;
            }

            // Se cachea el RESULTADO cuando el asset existe (también el -1 de un combo ramificado:
            // no va a cambiar en la sesión). Un asset que no apareció NO se cachea: puede cargarse
            // más tarde y resolverse en la cadena siguiente.
            if (found) comboLengths[comboName] = length;
            return length;
        }

        /// <summary>
        /// Pasos del combo si su cadena principal es LINEAL (índices 0,1,2,… consecutivos, cada
        /// uno enlazado con <c>siguiente.paso</c> al siguiente, cerrando en un enlace negativo), o
        /// -1 si no lo es.
        ///
        /// El requisito de linealidad no es capricho: <c>n</c> es el ÍNDICE del paso en el asset,
        /// así que la prueba de combo completo (<c>n == len - 1</c>) sólo tiene sentido si el
        /// camino recorre 0..len-1 sin saltos. Con saltos, cualquier <c>len</c> produciría <c>ok</c>
        /// falsos en las dos direcciones.
        ///
        /// Casos concretos del proyecto:
        /// - <c>Combo_1</c> tiene 13 nodos y una cadena principal 0→1→2→6: NO es lineal ⇒ -1. El
        ///   árbol tampoco da un largo único (el camino L→L→P sale por el branch 2→3).
        /// - <c>Combo_DashAttack</c> declara <c>paso: 0</c> en su paso 0 (auto-referencia que el
        ///   gameplay no usa: el opener encadena a las raíces normales) ⇒ se cierra ahí ⇒ 1.
        /// - Los combos de un paso y las cadenas 0→1 valen 1 y 2.
        /// </summary>
        private static int LinearLength(ComboDefinition definition)
        {
            int steps = definition != null ? definition.Length : 0;
            if (steps <= 0) return -1;

            for (int index = 0; index < steps; index++)
            {
                if (!definition.TryGetStep(index, out ComboStep step)) return -1;

                int next = step.siguiente.paso;
                if (next < 0) return index + 1;  // enlace de cierre: la cadena termina acá
                if (next <= index) return index + 1; // no avanza (auto-referencia): cierra acá
                if (next != index + 1) return -1; // salto: la cadena no es lineal, largo incierto
            }

            return -1; // nunca cerró dentro del asset: ciclo o falta el enlace final
        }

        /// <summary>
        /// Daño INFLIGIDO: se suscribe al <see cref="HitReceiver.Damaged"/> de cada receptor
        /// presente. Sin tocar gameplay y sin inventar un evento global. Limitación honesta: el
        /// golpe letal no dispara <c>Damaged</c>, así que ese daño no aparece (documentado).
        ///
        /// El rescaneo está limitado a uno por segundo salvo que se fuerce (bind o spawn de boss),
        /// porque busca objetos y no puede correr en cada paso de combo.
        /// </summary>
        private void BindReceivers(bool force = false)
        {
            float now = Time.unscaledTime;
            if (!force && now - lastReceiverScan < 1f) return;
            lastReceiverScan = now;

            HitReceiver[] found = FindObjectsByType<HitReceiver>(FindObjectsSortMode.None);
            for (int i = 0; i < found.Length; i++)
            {
                HitReceiver receiver = found[i];
                if (receiver == null || IsReceiverBound(receiver)) continue;

                var binding = new ReceiverBinding { Receiver = receiver };
                binding.Handler = (dealt, direction, launch) => HandleEnemyDamaged(binding.Receiver, dealt);
                binding.LethalHandler = lethal =>
                {
                    lastVictim = lethal; // golpe letal: no dispara `Damaged`, pero sí conectó
                    lastVictimFrame = Time.frameCount;
                };

                receiver.Damaged -= binding.Handler;
                receiver.Damaged += binding.Handler;
                receiver.LethalHit -= binding.LethalHandler;
                receiver.LethalHit += binding.LethalHandler;
                receiverBindings.Add(binding);
            }
        }

        private void UnbindReceivers()
        {
            for (int i = 0; i < receiverBindings.Count; i++)
            {
                ReceiverBinding binding = receiverBindings[i];
                if (binding == null || binding.Receiver == null) continue;

                binding.Receiver.Damaged -= binding.Handler;
                binding.Receiver.LethalHit -= binding.LethalHandler;
            }

            receiverBindings.Clear();
        }

        private bool IsReceiverBound(HitReceiver receiver)
        {
            for (int i = 0; i < receiverBindings.Count; i++)
            {
                if (receiverBindings[i] != null && receiverBindings[i].Receiver == receiver) return true;
            }

            return false;
        }

        private void HandleEnemyDamaged(HitReceiver receiver, int dealt)
        {
            lastVictim = receiver; // `Damaged` corre dentro de `TakeHit`: es el instante del impacto
            lastVictimFrame = Time.frameCount;
            int hp = receiver != null ? receiver.HP : 0;
            PlaytestLog.Instance?.EnemyDamaged(dealt, hp);
        }

        // ------------------------------------------------------------------ bosses

        private void HandleSpawned(BossController boss)
        {
            if (boss == null) return;

            EmitEncounter(ResolveBossId(boss));
            BindBoss(boss);

            // El boss trae su HitReceiver recién creado: se suma al daño infligido (F9).
            BindReceivers(true);
        }

        /// <summary>
        /// Respaldo: <see cref="BossEncounterTrigger.Triggered"/> llega el MISMO frame que
        /// <see cref="BossSpawner.Spawned"/> (el trigger llama a SpawnBoss antes de avisar), así que
        /// la dedupe por frame lo descarta cuando el spawn salió bien. Si el spawn falló (prefab o
        /// spawner ausentes), el despertar queda registrado igual.
        /// </summary>
        private void HandleTriggered(string bossId)
        {
            EmitEncounter(bossId);
        }

        private void EmitEncounter(string bossId)
        {
            int frame = Time.frameCount;
            if (frame == lastEncounterFrame) return;
            lastEncounterFrame = frame;

            PlaytestLog.Instance?.Encounter(bossId);
        }

        /// <summary>
        /// Enlaza el boss vivo. Los handlers se dan de baja en <c>Died</c> y en <c>Unbind</c>: un
        /// boss muerto se destruye después y no puede quedar apuntado (TEL-6).
        /// </summary>
        private void BindBoss(BossController boss)
        {
            for (int i = 0; i < bossBindings.Count; i++)
            {
                if (bossBindings[i].Controller == boss) return;
            }

            var binding = new BossBinding
            {
                Controller = boss,
                Id = ResolveBossId(boss)
            };

            binding.OnPhase = phase => PlaytestLog.Instance?.BossPhase(binding.Id, phase);
            binding.OnDied = () =>
            {
                PlaytestLog.Instance?.BossDied(binding.Id);
                UnbindBoss(binding);
            };

            boss.PhaseChanged -= binding.OnPhase;
            boss.PhaseChanged += binding.OnPhase;
            boss.Died -= binding.OnDied;
            boss.Died += binding.OnDied;

            bossBindings.Add(binding);
        }

        private void UnbindBoss(BossBinding binding)
        {
            if (binding == null) return;

            if (binding.Controller != null)
            {
                binding.Controller.PhaseChanged -= binding.OnPhase;
                binding.Controller.Died -= binding.OnDied;
            }

            bossBindings.Remove(binding);
        }

        /// <summary>True si hay un boss vivo a menos de <paramref name="radius"/> metros del jugador.</summary>
        private bool AnyLiveBossNear(float radius)
        {
            if (damage == null) return false;

            Vector3 origin = damage.transform.position;
            for (int i = 0; i < bossBindings.Count; i++)
            {
                BossController boss = bossBindings[i].Controller;
                if (boss == null || boss.IsDead) continue;
                if (Vector3.Distance(origin, boss.transform.position) <= radius) return true;
            }

            return false;
        }

        /// <summary>
        /// Id del boss para el log. <see cref="BossController"/> no expone su
        /// <c>BossDefinition.id</c> (es privado/protected), así que se deriva del nombre del host
        /// del spawner — que el builder fija como <c>GuardianEncounter</c> / <c>OrochiEncounter</c>.
        /// Cualquier otro boss cae a <c>boss</c> (genérico, design §13).
        /// </summary>
        private static string ResolveBossId(BossController boss)
        {
            if (boss == null) return "boss";

            string source = boss.transform.parent != null
                ? boss.transform.parent.name
                : boss.gameObject.name;

            return ResolveBossIdFromName(source);
        }

        private static string ResolveBossIdFromName(string sourceName)
        {
            if (string.IsNullOrEmpty(sourceName)) return "boss";

            string lower = sourceName.ToLowerInvariant();
            if (lower.Contains("orochi")) return "orochi";
            if (lower.Contains("guardian")) return "guardian";
            if (lower.Contains("executioner")) return "executioner";
            return "boss";
        }
    }
}
