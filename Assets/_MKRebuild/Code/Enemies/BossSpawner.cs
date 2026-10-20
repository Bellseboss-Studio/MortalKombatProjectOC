using UnityEngine;

namespace MK.Rebuild
{
    /// <summary>
    /// On-demand boss spawner (M3.4). Holds one <see cref="BossController"/> at a
    /// scene-authored point and exposes the two operations the reset/debug seams
    /// need: <see cref="SpawnBoss"/> and <see cref="ResetBoss"/>. Scene authored
    /// with <c>spawnOnStart = false</c> so the fight is triggered by the B debug
    /// key or by a future arena trigger.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class BossSpawner : MonoBehaviour
    {
        [Tooltip("Boss prefab to instantiate. Spawn is a no-op when null.")]
        [SerializeField] private BossController prefab;

        [Tooltip("Where the boss appears. Falls back to this transform.")]
        [SerializeField] private Transform spawnPoint;

        [Tooltip("Spawn the boss in Start. Keep FALSE for an on-demand encounter.")]
        [SerializeField] private bool spawnOnStart = false;

        /// <summary>Live boss instance (null until spawned / after death).</summary>
        public BossController Current { get; private set; }

        /// <summary>First scene spawner (convenience for the debug window).</summary>
        public static BossSpawner Instance { get; private set; }

        private void Awake()
        {
            if (Instance == null) Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void Start()
        {
            if (spawnOnStart) SpawnBoss();
        }

        /// <summary>Spawns one boss if none is live. Safe to call repeatedly.</summary>
        public void SpawnBoss()
        {
            if (Current != null) return;
            if (prefab == null) return;

            Transform point = spawnPoint != null ? spawnPoint : transform;
            Current = Instantiate(prefab, point.position, point.rotation, transform);
        }

        /// <summary>
        /// Restores the live boss to the spawn point and its current-phase HP.
        /// A dead boss stays dead; an unspawned encounter is a no-op.
        /// </summary>
        public void ResetBoss()
        {
            if (Current == null) return;

            Vector3 position = spawnPoint != null ? spawnPoint.position : transform.position;
            Current.ResetForRespawn(position);
        }
    }
}
