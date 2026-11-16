using UnityEngine;

namespace MK.Rebuild
{
    /// <summary>
    /// Closes a physical gate when the boss spawns and opens it when the boss dies.
    ///
    /// It exists because <see cref="BossArena"/> is a scene singleton and the dungeon has two
    /// arenas. This component is deliberately per-arena: it only knows its own spawner and its
    /// own slab, so two encounters can coexist without fighting over a static instance.
    ///
    /// The gate is a real collider, not a trigger: while closed it blocks the passage.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ArenaGate : MonoBehaviour
    {
        [Tooltip("Spawner whose boss locks this gate.")]
        [SerializeField] private BossSpawner spawner;

        [Tooltip("Slab that becomes active while the fight is live. Inactive = passage open.")]
        [SerializeField] private GameObject slab;

        [Tooltip("Seconds after the spawn before the gate closes, so the player can step in.")]
        [SerializeField] private float closeDelaySeconds = 1f;

        private BossController boss;
        private float closeTimer;
        private bool closing;

        private void Awake()
        {
            if (slab != null) slab.SetActive(false);
        }

        private void OnEnable()
        {
            if (spawner != null) spawner.Spawned += HandleSpawned;
        }

        private void OnDisable()
        {
            if (spawner != null) spawner.Spawned -= HandleSpawned;
        }

        private void HandleSpawned(BossController spawned)
        {
            boss = spawned;
            closing = true;
            closeTimer = Mathf.Max(0f, closeDelaySeconds);

            if (boss != null) boss.Died += HandleDied;
        }

        private void HandleDied()
        {
            closing = false;
            if (boss != null) boss.Died -= HandleDied;
            if (slab != null) slab.SetActive(false);
        }

        private void Update()
        {
            if (!closing) return;

            closeTimer -= Time.deltaTime;
            if (closeTimer > 0f) return;

            closing = false;
            if (slab != null) slab.SetActive(true);
        }
    }
}
