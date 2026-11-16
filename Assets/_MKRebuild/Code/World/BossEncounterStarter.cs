using System.Collections;
using UnityEngine;

namespace MK.Rebuild
{
    /// <summary>
    /// Despierta a un boss que <see cref="BossSpawner"/> dejó dormido, y lo hace SIN pisar la
    /// cinemática de presentación.
    ///
    /// Dos cosas importan acá:
    ///
    /// 1. POSE DE PRESENTACIÓN. Mientras el boss está congelado, el state machine no avanza
    ///    (<c>BossController.FixedUpdate</c> sale temprano), así que se le puede pedir un clip
    ///    suelto por <see cref="CharacterVisual.CrossFadeSafe"/>. Eso es una animación FUERA del
    ///    state machine: se ve el rugido sin que el boss empiece a atacar.
    ///
    /// 2. ORDEN DE DESCONGELADO. <see cref="CinematicDirector"/> guarda el <c>Frozen</c> del boss
    ///    al empezar el beat y lo restaura al terminar. Si el beat arranca mientras el boss sigue
    ///    dormido, lo restaura CONGELADO y el boss nunca despierta (se queda en Idle para
    ///    siempre). Por eso acá se espera a que la cinemática termine antes de soltarlo.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class BossEncounterStarter : MonoBehaviour
    {
        [Tooltip("Spawner whose boss this component wakes.")]
        [SerializeField] private BossSpawner spawner;

        [Tooltip("Segundos mínimos dormido después de spawnear.")]
        [SerializeField] private float dormantSeconds = 0.8f;

        [Tooltip("Clip que el boss muestra mientras está congelado (fuera del state machine). Vacío = deja el Idle.")]
        [SerializeField] private string introState = string.Empty;

        [Tooltip("Seguridad: si una cinemática nunca termina, el boss se despierta igual pasado este tiempo.")]
        [SerializeField] private float maxWaitSeconds = 20f;

        private void OnEnable()
        {
            if (spawner != null) spawner.Spawned += HandleSpawned;
        }

        private void OnDisable()
        {
            if (spawner != null) spawner.Spawned -= HandleSpawned;
        }

        private void HandleSpawned(BossController boss)
        {
            // El director tiene un solo bossSpawner: hay que apuntarlo al de esta arena o el
            // segundo boss nunca recibe la cámara de pelea.
            CameraDirector cameraDirector = CameraDirector.Instance;
            if (cameraDirector != null) cameraDirector.BindBossSpawner(spawner);

            StartCoroutine(Wake(boss));
        }

        /// <summary>Pide el clip de presentación sin tocar el state machine del boss.</summary>
        private void PlayIntroPose(BossController boss)
        {
            if (boss == null || string.IsNullOrEmpty(introState)) return;

            CharacterVisual visual = boss.GetComponentInChildren<CharacterVisual>(true);
            if (visual != null) visual.CrossFadeSafe(introState, 0.15f);
        }

        private IEnumerator Wake(BossController boss)
        {
            // Un frame: BossController.Start() llama PlaySpawn() y pisaría la pose de
            // presentación si la aplicáramos antes.
            yield return null;
            PlayIntroPose(boss);

            yield return new WaitForSecondsRealtime(Mathf.Max(0f, dormantSeconds));

            // No soltar al boss dentro del plano: el director lo volvería a congelar y al
            // restaurar su estado previo lo dejaría dormido para siempre.
            float waited = 0f;
            CinematicDirector cinematics = FindFirstObjectByType<CinematicDirector>();
            while (cinematics != null && cinematics.IsPlaying && waited < maxWaitSeconds)
            {
                waited += Time.unscaledDeltaTime;
                yield return null;
            }

            if (boss != null) boss.Frozen = false;
        }
    }
}
