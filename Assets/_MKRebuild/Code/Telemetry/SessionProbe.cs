using UnityEngine;
using UnityEngine.InputSystem;

namespace MK.Rebuild.Telemetry
{
    /// <summary>
    /// Probe del jugador: no escribe nada, sólo le pasa hechos al <see cref="PlaytestLog"/>.
    ///
    /// Lo que mide (TEL-3):
    /// - <c>p</c> a 4 Hz con acumulador de intervalo fijo (sin deriva) y una sola muestra tras un
    ///   hitch, nunca una ráfaga.
    /// - <c>reset</c> cuando dos muestras consecutivas se separan más que el umbral de teleport,
    ///   que depende de la velocidad reciente: <c>max(5 m, sp_max × dt × 1,5)</c> (foso, respawn,
    ///   checkpoint). Así un dash —que en la sesión real cubre 5–6,5 m entre muestras a 25–33 m/s—
    ///   no se lee como teleport. El salto no suma a <c>dist</c> y no se emite en la primera
    ///   muestra tras <c>run.start</c>. Si el salto cae dentro de 0,5 s de un golpe recibido se
    ///   descarta: es el empujón del knockback, no un teleport (la base de posición avanza igual).
    /// - <c>idle</c> al cerrar una quietud de más de 3 s.
    /// - <c>verb</c> por FLANCO EN CADA FRAME, no por la muestra: dash y jump duran menos que el
    ///   intervalo de 0,25 s y por muestra se perderían.
    /// - <c>mark</c> en el instante de la pulsación de F12 (sala y posición de ese frame, no de la
    ///   última muestra).
    ///
    /// Todo el tiempo sale de <see cref="Time.unscaledTime"/>: el hitstop y el pausa no pueden
    /// inflar ni acortar un dwell.
    ///
    /// El componente lo agrega <see cref="PlaytestLog"/> en <c>sceneLoaded</c> sobre el GameObject
    /// con <see cref="PlayerMotor"/>; el builder no cablea nada del jugador. Todas las referencias
    /// se resuelven en <see cref="Awake"/>/<see cref="OnEnable"/> y toleran null: la señal que falte
    /// simplemente no se emite.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SessionProbe : MonoBehaviour
    {
        /// <summary>Intervalo de muestreo: 4 Hz (TEL-3).</summary>
        private const float SampleInterval = 0.25f;

        /// <summary>Un hitch más largo que esto reinicia el acumulador (una muestra, sin ráfaga).</summary>
        private const float HitchThreshold = 1f;

        /// <summary>
        /// Piso del umbral de teleport (TEL-3): por debajo de esta distancia entre muestras nunca
        /// hay <c>reset</c>, por lento que haya ido el jugador.
        /// </summary>
        private const float TeleportDistance = 5f;

        /// <summary>
        /// Muestras de velocidad que forman la ventana de <c>sp_max</c>: 5 muestras = 1,25 s.
        /// Un dash dura 0,35 s, así que 5 muestras (1,25 s) lo siguen conteniendo con margen,
        /// pero el pico deja de "justificar" un teleport más de un segundo después. Con 10
        /// muestras (2,5 s) un dash de 33,5 m/s elevaba el umbral a 12,57 m durante 2,5 s y podía
        /// tapar el teleport de 12 m del foso.
        /// </summary>
        private const int SpeedWindow = 5;

        /// <summary>
        /// Velocidad de referencia (m/s) cuando todavía no hay histórico: 5 m / 0,25 s, el mismo
        /// borde que asumía el umbral fijo anterior. Con <c>dt = 0,25 s</c> da un umbral de 7,5 m,
        /// por debajo del salto de 12 m del foso, así que un teleport temprano se sigue viendo.
        /// </summary>
        private const float DefaultSpeedCap = 20f;

        /// <summary>
        /// Margen sobre la distancia que la velocidad observada permite recorrer en el intervalo:
        /// absorbe el orden de lectura (la velocidad se mide al final del intervalo) y el jitter
        /// de ±20 ms del muestreo. Con 1,5 un dash de 33 m/s cubre ~12 m de umbral.
        /// </summary>
        private const float TeleportSpeedMargin = 1.5f;

        /// <summary>
        /// Ventana tras un golpe recibido dentro de la cual un salto por encima del umbral es el
        /// empujón del knockback y NO un teleport. 0,5 s cubre el shove del jefe (que se percibe en
        /// la muestra siguiente, ≤ 0,25 s después) sin tapar una caída al vacío posterior.
        /// </summary>
        private const float KnockbackWindow = 0.5f;

        /// <summary>Movimiento por muestra que todavía cuenta como quietud (TEL-3).</summary>
        private const float IdleMoveEpsilon = 0.05f;

        /// <summary>Quietud mínima para emitir <c>idle</c> (TEL-3).</summary>
        private const float IdleThreshold = 3f;

        private PlayerMotor motor;
        private PlayerDamage damage;
        private PlayerDash dash;
        private PlayerGrab grab;
        private CheckpointSystem checkpoints;
        private Rigidbody body;

        // --- muestreo ---
        private float lastSample;
        private bool havePosition;
        private Vector3 lastPosition;

        /// <summary>Instante unscaled de la última muestra: da el <c>dt</c> real del umbral.</summary>
        private float lastSampleTime;

        /// <summary>Velocidades de las últimas <see cref="SpeedWindow"/> muestras (ring buffer).</summary>
        private readonly float[] recentSpeeds = new float[SpeedWindow];
        private int speedCount;
        private int speedIndex;

        // --- quietud ---
        private bool idleOpen;
        private float idleSince;
        private Vector3 idlePosition;

        // --- flancos por frame ---
        private bool wasDashing;
        private bool wasGrabbing;
        private bool wasMantling;
        private bool wasGrounded;
        private bool wasRespawning;
        private int lastSectionId;
        private bool resolved;

        /// <summary>
        /// Re-arma la línea base de la corrida (nueva carga de escena): la primera muestra no
        /// compara contra la posición de la vida anterior, así que no inventa un <c>reset</c>.
        /// </summary>
        public void PrepareRun()
        {
            Resolve();

            float now = Time.unscaledTime;
            lastSample = now;
            lastSampleTime = now;
            havePosition = false;
            idleOpen = false;
            idleSince = now;
            speedCount = 0;
            speedIndex = 0;

            wasDashing = dash != null && dash.IsDashing;
            wasGrabbing = grab != null && grab.IsGrabbing;
            wasMantling = motor != null && motor.State == MoveState.Mantle;
            wasGrounded = motor != null && motor.IsGrounded;
            wasRespawning = checkpoints != null && checkpoints.IsRespawning;
            lastSectionId = checkpoints != null ? checkpoints.CheckpointSectionId : 0;
        }

        private void Awake()
        {
            Resolve();
        }

        private void OnEnable()
        {
            Resolve();
        }

        private void Update()
        {
            if (!resolved) Resolve();

            PlaytestLog log = PlaytestLog.Instance;
            float now = Time.unscaledTime;

            // MARCA MANUAL (MRK-1): se atiende ANTES del guard de pausa/canal porque el momento
            // natural de marcar es justamente "me trabé, abrí el menú y lo marqué". La pausa NO
            // invalida la marca: la posición y la sala siguen siendo las del jugador. `Mark()`
            // re-comprueba canal y jugador, así que sin archivo (`-nolog`, IO caída) o sin
            // `PlayerMotor` devuelve false, no escribe y no hay toast.
            if (log != null) PollMark(log);

            // Canal sin archivo (o -nolog) o pausa abierta: no se muestrea, no se acumula idle y
            // el acumulador queda anclado a "ahora" para que al reanudar no salga una ráfaga.
            if (log == null || !log.IsChannelEnabled(Channel.Level) || log.IsPaused)
            {
                lastSample = now;
                lastSampleTime = now;
                CloseIdle(log, now);
                return;
            }

            TrackVerbs(log);
            TrackRespawnAndCheckpoint(log);

            // Con el jugador muerto no hay muestra ni distancia: la muerte la emite el binder.
            // La posición anterior se conserva a propósito: la primera muestra tras el respawn
            // ve el salto al checkpoint y lo reporta como reset (no como caminata).
            if (damage != null && !damage.IsAlive)
            {
                lastSample = now;
                lastSampleTime = now;
                CloseIdle(log, now);
                return;
            }

            // Hitch: sale UNA muestra y el acumulador se reancla, en vez de una ráfaga de 8.
            if (now - lastSample > HitchThreshold)
            {
                lastSample = now;
                Sample(log);
                return;
            }

            // Muestreo sin deriva: last avanza de a un intervalo exacto, no a "now".
            while (now - lastSample >= SampleInterval)
            {
                lastSample += SampleInterval;
                Sample(log);
            }
        }

        /// <summary>
        /// Marca manual F12 (MRK-1/MRK-2): polling directo del dispositivo, mismo patrón que
        /// <c>PauseMenu</c> y <c>DebugWindow</c> (sin action map, sin regenerar <c>PlayerInput.cs</c>).
        ///
        /// Corre antes del guard de pausa/canal: marcar con el pausa o el panel de entrega abiertos
        /// MUST funcionar (es el momento en que el jugador quiere señalar que algo lo trabó) y la
        /// posición del frame sigue siendo válida. El muestreo de 4 Hz, la quietud y los verbos sí
        /// siguen suspendidos durante la pausa: ahí el reloj de juego no avanza.
        ///
        /// <c>wasPressedThisFrame</c> da UNA marca por pulsación: mantener la tecla no repite.
        /// <see cref="PlaytestLog.Mark"/> comprueba canal y jugador y devuelve si escribió: el toast
        /// sólo aparece cuando hay línea, así que con <c>-nolog</c> no hay confirmación falsa.
        /// El toast usa tiempo real y un canvas por encima del pausa, así que también se ve pausado.
        ///
        /// F12 no abre ni cambia <c>DebugWindow</c>: ahí es un no-op a propósito.
        /// </summary>
        private void PollMark(PlaytestLog log)
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null || !keyboard.f12Key.wasPressedThisFrame) return;

            if (!log.Mark()) return;

            MarkToast.Show("MARCA REGISTRADA");
        }

        /// <summary>
        /// Una muestra: detecta teleport (reset, fuera de dist) o acumula distancia caminada, y
        /// cierra/abre la quietud. Emite <c>p</c> al final para que un <c>reset</c> quede ANTES de
        /// la muestra siguiente (TEL-3). Un salto por encima del umbral adaptativo recién pegado a
        /// un golpe recibido es knockback y no emite <c>reset</c>, aunque la base de posición avance
        /// igual.
        /// </summary>
        private void Sample(PlaytestLog log)
        {
            float now = Time.unscaledTime;
            Vector3 position = motor != null ? motor.transform.position : transform.position;
            Vector3 velocity = body != null ? body.linearVelocity : Vector3.zero;
            float speed = velocity.magnitude;
            float delta = havePosition ? Vector3.Distance(lastPosition, position) : 0f;

            RememberSpeed(speed);

            if (havePosition && delta > TeleportThreshold(now))
            {
                CloseIdle(log, now);

                // FALSO POSITIVO DEL KNOCKBACK: el empujón de un golpe recibido mueve al jugador
                // por encima del umbral, así que la muestra siguiente lo leería como teleport. Un
                // `reset` ahí mentiría dos veces (sala de origen y salto que no suma a dist). Se
                // descarta el evento, pero la posición base SÍ avanza: sin eso el frame siguiente
                // volvería a ver el mismo salto y encadenaría un reset falso por cada muestra.
                //
                // Orden con el foso: si el knockback tira al jugador al vacío, el VoidReset
                // (respawn al checkpoint) ocurre cuando ya cayó, mucho más de 0,5 s después del
                // golpe, así que esa ventana ya venció y el reset se registra igual.
                if (!log.HadRecentDamage(KnockbackWindow))
                {
                    log.Teleported(position, delta); // no suma a dist
                }
            }
            else
            {
                if (havePosition) log.AddDistance(delta);
                TrackIdle(log, position, delta);
            }

            log.Sample(position, speed);

            lastPosition = position;
            lastSampleTime = now;
            havePosition = true;
        }

        /// <summary>
        /// Umbral de teleport para esta muestra: <c>max(5 m, sp_max × dt × 1,5)</c>.
        ///
        /// <c>sp_max</c> es la mayor velocidad de las últimas <see cref="SpeedWindow"/> muestras,
        /// incluida la actual (se mide al final del intervalo y es la que mejor describe el tramo
        /// recorrido); mientras la ventana no está completa se usa <see cref="DefaultSpeedCap"/>.
        /// El objetivo es que la distancia que el jugador PUDO recorrer legalmente no cuente como
        /// teleport: en la sesión real un dash de 25–33 m/s cubría 5–6,5 m entre dos muestras, y el
        /// umbral fijo de 5 m lo marcaba como reset.
        ///
        /// <c>dt</c> es la separación real entre las dos muestras, pero acotada a un intervalo de
        /// muestreo: un hueco mayor significa que el muestreo estaba suspendido (muerte, pausa) o
        /// que hubo un hitch, y en esos casos el jugador no recorrió ese tiempo de juego. Escalar el
        /// umbral con el reloj de pared taparía justo el salto al checkpoint tras un respawn, que
        /// es un teleport real que MUST registrarse.
        /// </summary>
        private float TeleportThreshold(float now)
        {
            float dt = Mathf.Clamp(now - lastSampleTime, 0f, SampleInterval);

            // Con la ventana incompleta (arranque de la corrida) el máximo de 1–4 muestras no es
            // representativo: se usa la constante de seguridad, que es el borde que ya asumía el
            // umbral fijo (5 m / 0,25 s). Con la ventana llena manda lo que el jugador hizo.
            float speedCap = MaxRecentSpeed();
            if (speedCount < SpeedWindow) speedCap = Mathf.Max(DefaultSpeedCap, speedCap);

            return Mathf.Max(TeleportDistance, speedCap * dt * TeleportSpeedMargin);
        }

        private void RememberSpeed(float speed)
        {
            recentSpeeds[speedIndex] = speed;
            speedIndex = (speedIndex + 1) % SpeedWindow;
            if (speedCount < SpeedWindow) speedCount++;
        }

        private float MaxRecentSpeed()
        {
            float max = 0f;
            for (int i = 0; i < speedCount; i++)
            {
                if (recentSpeeds[i] > max) max = recentSpeeds[i];
            }

            return max;
        }

        /// <summary>
        /// Quietud por muestreo: mientras el desplazamiento entre muestras sea menor a 5 cm se
        /// mantiene abierta; al moverse se cierra con la duración real (unscaled).
        /// </summary>
        private void TrackIdle(PlaytestLog log, Vector3 position, float delta)
        {
            float now = Time.unscaledTime;

            if (delta < IdleMoveEpsilon)
            {
                if (idleOpen) return;
                idleOpen = true;
                idleSince = now - SampleInterval; // la quietud empezó en la muestra anterior
                idlePosition = position;
                return;
            }

            CloseIdle(log, now);
        }

        /// <summary>
        /// Cierra la quietud en curso. Un pausa, una muerte o un teleport la cierran igual: la
        /// quietud que dura menos de 3 s no se emite.
        /// </summary>
        private void CloseIdle(PlaytestLog log, float now)
        {
            if (!idleOpen) return;

            idleOpen = false;
            float duration = Mathf.Max(0f, now - idleSince);
            if (duration < IdleThreshold) return;

            log?.Idle(idlePosition, duration);
        }

        /// <summary>
        /// Verbos por flanco en CADA frame: un dash de 0,35 s y un salto se perderían con la
        /// muestra de 4 Hz. La propiedad pública es exacta y no se repite mientras el estado siga
        /// activo (TEL-2).
        /// </summary>
        private void TrackVerbs(PlaytestLog log)
        {
            if (dash != null)
            {
                bool dashing = dash.IsDashing;
                if (dashing && !wasDashing) log.Verb("dash");
                wasDashing = dashing;
            }

            if (grab != null)
            {
                bool grabbing = grab.IsGrabbing;
                if (grabbing && !wasGrabbing) log.Verb("grab");
                wasGrabbing = grabbing;
            }

            if (motor == null) return;

            bool mantling = motor.State == MoveState.Mantle;
            if (mantling && !wasMantling) log.Verb("mantle");
            wasMantling = mantling;

            // Jump: despegue real = dejó de estar grounded con velocidad vertical positiva. Se
            // lee por frame (IsGrounded se compromete en FixedUpdate), no por la muestra.
            bool grounded = motor.IsGrounded;
            float verticalSpeed = body != null ? body.linearVelocity.y : 0f;
            if (wasGrounded && !grounded && verticalSpeed > 0f) log.Verb("jump");
            wasGrounded = grounded;
        }

        /// <summary>
        /// Respawn y checkpoints por polling de <see cref="CheckpointSystem"/>: el sistema no
        /// expone eventos y no se toca gameplay para agregarlos (TEL-6).
        /// </summary>
        private void TrackRespawnAndCheckpoint(PlaytestLog log)
        {
            if (checkpoints == null) return;

            bool respawning = checkpoints.IsRespawning;
            if (wasRespawning && !respawning) log.Respawned();
            wasRespawning = respawning;

            int section = checkpoints.CheckpointSectionId;
            if (section == lastSectionId) return;

            lastSectionId = section;

            // El checkpoint implícito 0 (pose de spawn) no es una visita: no se emite.
            if (section != 0) log.Checkpointed(section);
        }

        /// <summary>
        /// Resolución tolerante a null: el probe puede vivir en un Player sin dash, sin grab o sin
        /// stance (v1) y cada señal ausente se salta sin ruido.
        /// </summary>
        private void Resolve()
        {
            resolved = true;

            if (motor == null) motor = GetComponent<PlayerMotor>();
            if (damage == null) damage = GetComponent<PlayerDamage>();
            if (dash == null) dash = GetComponent<PlayerDash>();
            if (grab == null) grab = GetComponent<PlayerGrab>();
            if (checkpoints == null) checkpoints = GetComponent<CheckpointSystem>();
            if (body == null) body = GetComponent<Rigidbody>();
        }
    }
}
