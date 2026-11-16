using System.Collections;
using UnityEngine;

namespace MK.Rebuild
{
    /// <summary>
    /// Wakes a boss that <see cref="BossSpawner"/> left dormant.
    ///
    /// The spawner freezes every instance on purpose: the boss starts in its Approach state,
    /// so an unfrozen boss would walk to the player during the intro beat. In the dungeon
    /// there is no flow controller to unfreeze it later, so this component does it after a
    /// short beat. When the cinematic layer lands, the beat's duration replaces the delay.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class BossEncounterStarter : MonoBehaviour
    {
        [Tooltip("Spawner whose boss this component wakes.")]
        [SerializeField] private BossSpawner spawner;

        [Tooltip("Seconds the boss stays dormant after spawning (intro beat length).")]
        [SerializeField] private float dormantSeconds = 0.8f;

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
            CameraDirector director = CameraDirector.Instance;
            if (director != null) director.BindBossSpawner(spawner);

            StartCoroutine(Wake(boss));
        }

        private IEnumerator Wake(BossController boss)
        {
            yield return new WaitForSecondsRealtime(Mathf.Max(0f, dormantSeconds));
            if (boss != null) boss.Frozen = false;
        }
    }
}
