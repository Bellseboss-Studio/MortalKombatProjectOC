using System;
using UnityEngine;

namespace MK.Rebuild.Telemetry
{
    /// <summary>
    /// Probe de ENTRADA de combate: registra la CAUSA de lo que el canal <c>combat</c> ya mide
    /// como efecto. <c>p</c>/<c>ph</c> dicen cuántos pasos salieron y tocaron; este probe dice
    /// CUÁNDO se apretó y si el juego lo encadenó, así la ventana de cancel se mide en vez de
    /// asumirse.
    ///
    /// Emite dos eventos, ambos al canal <c>level</c> (TEL-2):
    /// - <c>in</c>: UNA línea por pulsación de ataque, con <c>k</c> (<c>light</c>, <c>heavy</c> o
    ///   <c>both</c>), el <c>step</c> vigente y los <c>ms</c> desde que empezó ese paso.
    /// - <c>step</c>: cada transición del cursor <c>PlayerCombat.CurrentStepIndex</c>, incluido el
    ///   arranque 0 y el cierre a -1, con el nombre del combo cuando se conoce.
    ///
    /// Observa con una instancia PROPIA del asset generado <c>MK.Rebuild.PlayerInput</c>
    /// (<c>Assets/_MKRebuild/Input/Player.inputactions</c>): leer una action no consume el input
    /// ni cambia lo que hace <c>PlayerCombat</c>, que sigue siendo el único dueño de la acción.
    /// No se inyecta ningún input y no se llama a ningún <c>Submit*Attack()</c>.
    ///
    /// Lo que NO se toca (TEL-6): <c>PlayerCombat</c> no se modifica. El estado se lee por su
    /// seam público de sólo lectura (<c>CurrentStepIndex</c>), el mismo que ya usa
    /// <see cref="GameplayBindings"/>.
    ///
    /// Ciclo de vida: la instancia se crea una sola vez, se <c>Enable()</c> en <c>OnEnable</c> y
    /// se <c>Disable()</c>/<c>Dispose()</c> en <c>OnDisable</c>/<c>OnDestroy</c>: nunca queda viva
    /// entre escenas. Si el asset no se puede instanciar o el <c>Enable()</c> falla, el probe se
    /// apaga solo, deja UN aviso por consola y el resto de la telemetría sigue igual.
    ///
    /// El componente lo agrega <see cref="PlaytestLog"/> en <c>sceneLoaded</c> sobre el GameObject
    /// del jugador, al lado de <see cref="SessionProbe"/>.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class InputProbe : MonoBehaviour
    {
        /// <summary>Evita repetir el aviso de fallo en cada recarga de escena.</summary>
        private static bool warned;

        /// <summary>
        /// Asset observador. Plain class (no MonoBehaviour): <c>new PlayerInput()</c> construye
        /// el asset desde el JSON embebido del generador, sin prefab ni asset asignado — igual
        /// que hace <c>PlayerCombat.Awake</c> con su propia instancia.
        /// </summary>
        private MK.Rebuild.PlayerInput input;

        /// <summary>True tras un fallo de creación/Enable: el probe queda apagado hasta morir.</summary>
        private bool inputFailed;

        /// <summary>Combate del jugador: fuente del cursor de paso (sólo lectura).</summary>
        private PlayerCombat combat;

        /// <summary>
        /// Quien ya resuelve el nombre del combo activo (la cadena de
        /// <see cref="GameplayBindings"/>). Se reusa su resolución en vez de inventar otra: el
        /// nombre del asset sólo existe en el string <c>Combo_1#2</c> del impacto.
        /// </summary>
        private GameplayBindings comboSource;

        /// <summary>Último cursor leído: -1 = sin combo, que también es el estado inicial.</summary>
        private int lastStep = -1;

        /// <summary>
        /// Último nombre de combo visto en la cadena ABIERTA. Sirve para el cierre: cuando el
        /// cursor cae a -1 el binder ya cerró la cadena en su <c>LateUpdate</c> y
        /// <see cref="GameplayBindings.ActiveComboName"/> vuelve a <c>null</c>, así que sin esta
        /// memoria la línea del cierre diría <c>?</c> y borraría la evidencia de qué combo se
        /// jugaba. Se limpia al arrancar una cadena nueva: el opener no hereda el nombre ajeno.
        /// </summary>
        private string chainCombo;

        /// <summary>Instante unscaled en que empezó el paso vigente; -1 si no hay paso.</summary>
        private float stepStartedAt = -1f;

        // ------------------------------------------------------------------ ciclo de vida

        private void OnEnable()
        {
            if (inputFailed) return;

            try
            {
                if (input == null) input = new MK.Rebuild.PlayerInput();
                input.Enable();
            }
            catch (Exception error)
            {
                // Un probe de entrada que no puede observar no puede mentir: se apaga y el resto
                // de la telemetría (muestras, salas, combos) sigue funcionando.
                input = null;
                inputFailed = true;
                WarnOnce(error);
            }
        }

        private void OnDisable()
        {
            DisableInput();
        }

        private void OnDestroy()
        {
            DisableInput();

            if (input == null) return;

            try
            {
                input.Dispose();
            }
            catch (Exception)
            {
                // Teardown: un asset que no se pudo destruir no puede tirar el cierre de escena.
            }

            input = null;
        }

        /// <summary>
        /// Re-apunta el probe a la escena nueva. Re-arma el cursor: la primera lectura de la vida
        /// nueva no debe emitir una transición desde el paso de la anterior.
        /// </summary>
        public void Bind(PlayerCombat playerCombat, GameplayBindings bindings)
        {
            combat = playerCombat;
            comboSource = bindings;

            lastStep = -1;
            stepStartedAt = -1f;
            chainCombo = null;
        }

        /// <summary>Suelta las referencias de la escena vieja (el asset observador sigue vivo).</summary>
        public void Unbind()
        {
            combat = null;
            comboSource = null;
            lastStep = -1;
            stepStartedAt = -1f;
            chainCombo = null;
        }

        // ------------------------------------------------------------------ frame

        /// <summary>
        /// Orden deliberado dentro del frame: PRIMERO la pulsación, DESPUÉS la transición de paso.
        ///
        /// - El <c>in</c> se escribe con el último cursor polleado (≤ 1 frame de antigüedad), así
        ///   que cuando el paso avanza en el MISMO frame que la pulsación, el archivo queda
        ///   <c>in</c> (step = N) y acto seguido <c>step</c> (n = N+1): el analista ve siempre el
        ///   press ANTES del paso que provocó, sin importar si este script se ejecutó antes o
        ///   después de <c>PlayerCombat.Update</c> (el orden de scripts no está definido).
        /// - Durante la pausa no se registra nada: el clic del menú está bindeado a
        ///   <c>LightAttack</c> y contaría como pulsación caída. El cursor tampoco cambia porque
        ///   el juego está congelado.
        /// - Sin <c>PlayerCombat</c> el probe calla: <c>step</c> y <c>ms</c> no tendrían fuente y
        ///   una pulsación con <c>step = -1</c> inventaría una "apertura" que no existió.
        /// </summary>
        private void Update()
        {
            PlaytestLog log = PlaytestLog.Instance;
            if (log == null || combat == null || input == null) return;
            if (log.IsPaused) return;

            float now = Time.unscaledTime;

            PollPresses(log, now);
            PollStep(log, now);
        }

        /// <summary>
        /// Una línea <c>in</c> por pulsación (una sola con <c>k = "both"</c> si en el mismo frame
        /// se apretaron las dos). <c>wasPressedThisFrame</c> da exactamente una por pulsación:
        /// mantener la tecla no repite.
        /// </summary>
        private void PollPresses(PlaytestLog log, float now)
        {
            bool light;
            bool heavy;

            try
            {
                light = input.Player.LightAttack.WasPressedThisFrame();
                heavy = input.Player.HeavyAttack.WasPressedThisFrame();
            }
            catch (Exception error)
            {
                // Un asset observador roto se apaga: el probe no puede detener el juego (TEL-6).
                DisableInput();
                input = null;
                inputFailed = true;
                WarnOnce(error);
                return;
            }

            if (!light && !heavy) return;

            string kind = light && heavy ? "both" : (light ? "light" : "heavy");
            log.InputPressed(kind, lastStep, MillisSinceStepStart(now));
        }

        /// <summary>
        /// Detecta el cambio de paso y lo emite. Incluye el arranque 0 y el cierre a -1: el cierre
        /// es la evidencia de que la cadena terminó sin encadenar.
        /// </summary>
        private void PollStep(PlaytestLog log, float now)
        {
            int step = combat.CurrentStepIndex;
            if (step == lastStep) return;

            bool chainStarted = step >= 0 && lastStep < 0;
            lastStep = step;
            stepStartedAt = step >= 0 ? now : -1f;

            string live = comboSource != null ? comboSource.ActiveComboName : null;
            if (chainStarted) chainCombo = null;         // una cadena nueva no hereda el nombre
            if (!string.IsNullOrEmpty(live)) chainCombo = live;

            string reported = !string.IsNullOrEmpty(live) ? live : (step < 0 ? chainCombo : null);
            log.StepChanged(step, reported);

            if (step < 0) chainCombo = null;
        }

        /// <summary>
        /// <c>ms</c> desde el arranque del paso vigente (-1 si no hay paso). Sale de
        /// <see cref="Time.unscaledTime"/>, como todo el archivo: el hitstop no detiene este reloj
        /// (y el brief lo pide explícitamente unscaled).
        /// </summary>
        private int MillisSinceStepStart(float now)
        {
            if (lastStep < 0 || stepStartedAt < 0f) return -1;

            return Mathf.Max(0, Mathf.RoundToInt((now - stepStartedAt) * 1000f));
        }

        private void DisableInput()
        {
            if (input == null) return;

            try
            {
                input.Disable();
            }
            catch (Exception)
            {
                // Igual que el Dispose: nunca propagar en el teardown.
            }
        }

        private static void WarnOnce(Exception error)
        {
            if (warned) return;
            warned = true;

            Debug.LogWarning(
                "[Telemetry] el probe de entrada no pudo activarse (" +
                error.GetType().Name + ": " + error.Message +
                "). Se apaga: los eventos 'in'/'step' no se graban; el resto de la telemetría sigue.");
        }
    }
}
