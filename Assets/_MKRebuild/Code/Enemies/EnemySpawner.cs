using System.Collections.Generic;
using UnityEngine;

namespace MK.Rebuild
{
    /// <summary>
    /// Grey-box encounter spawner. Instantiates an <see cref="EnemyController"/>
    /// prefab at scene-authored points and owns the spawned set so
    /// <see cref="SectionReset"/> can restore the encounter on player death.
    /// No pooling: these are a handful of grey-box enemies.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class EnemySpawner : MonoBehaviour
    {
        [Tooltip("Enemy prefab to instantiate (Esbirro).")]
        [SerializeField] private EnemyController prefab;

        [Tooltip("Spawn points, cycled in order. Falls back to this transform when empty.")]
        [SerializeField] private Transform[] spawnPoints;

        [Tooltip("How many enemies the encounter starts (and resets) with.")]
        [SerializeField] private int initialCount = 2;

        [Tooltip("Spawn the initial wave in Start. Disable for manual/scripted encounters.")]
        [SerializeField] private bool spawnOnStart = true;

        private readonly List<EnemyController> spawned = new();
        private int nextPointIndex;

        /// <summary>Live spawned enemies (nulls are pruned on every read).</summary>
        public int AliveCount
        {
            get
            {
                PruneDead();
                return spawned.Count;
            }
        }

        private void Start()
        {
            if (spawnOnStart) SpawnInitial();
        }

        /// <summary>Instantiates <see cref="initialCount"/> enemies.</summary>
        public void SpawnInitial()
        {
            for (int i = 0; i < Mathf.Max(0, initialCount); i++) SpawnOne();
        }

        /// <summary>
        /// Instantiates one enemy at the next authored point (cycled), parented
        /// under this spawner. Falls back to this transform when no points exist.
        /// </summary>
        public void SpawnOne()
        {
            if (prefab == null) return;

            Vector3 position;
            Quaternion rotation;
            if (spawnPoints != null && spawnPoints.Length > 0)
            {
                var point = spawnPoints[nextPointIndex % spawnPoints.Length];
                nextPointIndex = (nextPointIndex + 1) % spawnPoints.Length;
                if (point != null)
                {
                    position = point.position;
                    rotation = point.rotation;
                }
                else
                {
                    position = transform.position;
                    rotation = transform.rotation;
                }
            }
            else
            {
                position = transform.position;
                rotation = transform.rotation;
            }

            var enemy = Instantiate(prefab, position, rotation, transform);
            if (enemy != null) spawned.Add(enemy);
        }

        /// <summary>
        /// Full encounter restore: destroys every spawned enemy (null-safe) and
        /// spawns the initial wave again.
        /// </summary>
        public void ResetEncounter()
        {
            for (int i = 0; i < spawned.Count; i++)
            {
                var enemy = spawned[i];
                if (enemy != null) Destroy(enemy.gameObject);
            }

            spawned.Clear();
            SpawnInitial();
        }

        private void PruneDead()
        {
            for (int i = spawned.Count - 1; i >= 0; i--)
            {
                if (spawned[i] == null) spawned.RemoveAt(i);
            }
        }
    }
}
