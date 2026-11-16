using UnityEngine;

namespace MK.Rebuild
{
    /// <summary>
    /// Dispara un <see cref="CinematicDefinition"/> en un momento del nivel.
    ///
    /// Existe para que el contenido de cada beat sea reemplazable sin tocar la lógica: el cue
    /// sólo decide CUÁNDO, y el director de fotografía cambia el Timeline de la definición
    /// cuando quiera. Ningún sistema de juego conoce este componente.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CinematicCue : MonoBehaviour
    {
        public enum Trigger
        {
            /// <summary>Al cargar la escena, después de <see cref="delay"/> segundos.</summary>
            OnStart,

            /// <summary>Cuando el jugador entra en el volumen de este objeto.</summary>
            OnVolume,

            /// <summary>Cuando el objetivo de la mazmorra se completa.</summary>
            OnObjective,

            /// <summary>Cuando el spawner indicado crea un boss.</summary>
            OnBossSpawn,

            /// <summary>Cuando el boss del spawner indicado muere.</summary>
            OnBossDeath,
        }

        [Tooltip("Qué momento dispara el beat.")]
        [SerializeField] private Trigger trigger = Trigger.OnVolume;

        [Tooltip("Beat a reproducir. Sin definición el cue no hace nada.")]
        [SerializeField] private CinematicDefinition definition;

        [Tooltip("Segundos de espera antes de reproducir (OnStart lo usa; los demás también).")]
        [SerializeField] private float delay;

        [Tooltip("Reproducir una sola vez por carga de escena.")]
        [SerializeField] private bool once = true;

        [Tooltip("Objetivo observado por el modo OnObjective. Null = el primero de la escena.")]
        [SerializeField] private DungeonObjective objective;

        [Tooltip("Spawner observado por OnBossSpawn / OnBossDeath. Null = el primero de la escena.")]
        [SerializeField] private BossSpawner spawner;

        private bool fired;
        private BossController watchedBoss;

        private void Start()
        {
            if (trigger == Trigger.OnStart) Arm();
        }

        private void OnEnable()
        {
            if (trigger == Trigger.OnObjective)
            {
                if (objective == null) objective = FindFirstObjectByType<DungeonObjective>();
                if (objective != null) objective.Completed += HandleObjectiveCompleted;
            }

            if (trigger == Trigger.OnBossSpawn || trigger == Trigger.OnBossDeath)
            {
                if (spawner == null) spawner = FindFirstObjectByType<BossSpawner>();
                if (spawner != null) spawner.Spawned += HandleBossSpawned;
            }
        }

        private void OnDisable()
        {
            if (objective != null) objective.Completed -= HandleObjectiveCompleted;
            if (spawner != null) spawner.Spawned -= HandleBossSpawned;
            if (watchedBoss != null) watchedBoss.Died -= HandleBossDied;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (trigger != Trigger.OnVolume) return;
            if (other == null || other.GetComponentInParent<PlayerDamage>() == null) return;
            Arm();
        }

        private void HandleObjectiveCompleted()
        {
            if (trigger != Trigger.OnObjective) return;
            Arm();
        }

        private void HandleBossSpawned(BossController boss)
        {
            if (trigger == Trigger.OnBossSpawn)
            {
                Arm();
                return;
            }

            if (trigger != Trigger.OnBossDeath || boss == null) return;
            if (watchedBoss != null) watchedBoss.Died -= HandleBossDied;
            watchedBoss = boss;
            watchedBoss.Died += HandleBossDied;
        }

        private void HandleBossDied()
        {
            if (watchedBoss != null)
            {
                watchedBoss.Died -= HandleBossDied;
                watchedBoss = null;
            }

            Arm();
        }

        /// <summary>Arma el disparo: respeta <see cref="once"/> y el delay.</summary>
        private void Arm()
        {
            if (once && fired) return;
            if (definition == null) return;

            fired = true;
            if (delay > 0f) Invoke(nameof(Play), delay);
            else Play();
        }

        private void Play()
        {
            CinematicDirector director = FindFirstObjectByType<CinematicDirector>();
            if (director == null)
            {
                Debug.LogWarning("[CinematicCue] No hay CinematicDirector en la escena.", this);
                return;
            }

            director.Play(definition);
        }
    }
}
