using UnityEngine;

namespace MK.Rebuild
{
    /// <summary>
    /// Boss encounter ENTRY ZONE. The player entering this trigger spawns the boss
    /// through <see cref="BossSpawner"/> — there is NO teleport. The arena lock is
    /// owned by <see cref="BossArena"/>/<see cref="BossController"/> once the boss
    /// binds, so this component only decides WHEN the fight starts.
    ///
    /// Fires once per scene load (<see cref="once"/>); a respawn inside the zone
    /// does not respawn a dead boss.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider))]
    public sealed class BossEncounterTrigger : MonoBehaviour
    {
        [Tooltip("Spawner that instantiates the boss. Falls back to BossSpawner.Instance.")]
        [SerializeField] private BossSpawner spawner;

        [Tooltip("Only fire once. Keep ON so re-entering the zone cannot respawn a dead boss.")]
        [SerializeField] private bool once = true;

        private bool fired;

        /// <summary>
        /// Raised after the boss spawns, only on the accepted fire (never on the
        /// <see cref="once"/> early-out). WU4's flow controller uses it for the encounter
        /// flow; camera consumers subscribe to <see cref="BossSpawner.Spawned"/> instead.
        /// </summary>
        public event System.Action Triggered;

        /// <summary>Editor convenience: an entry zone is always a trigger volume.</summary>
        private void Reset()
        {
            var col = GetComponent<Collider>();
            if (col != null) col.isTrigger = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (once && fired) return;
            if (other == null) return;
            if (other.GetComponentInParent<PlayerDamage>() == null) return;

            BossSpawner target = spawner != null ? spawner : BossSpawner.Instance;
            if (target == null) return;

            target.SpawnBoss();
            fired = true;
            Triggered?.Invoke();
        }
    }
}
