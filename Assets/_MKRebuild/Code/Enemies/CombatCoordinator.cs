using System.Collections.Generic;
using UnityEngine;

namespace MK.Rebuild
{
    /// <summary>
    /// Scene-level combat token gate: caps how many melee enemies may be in the
    /// ATTACK state at the same time. Enemies ask for a token when they commit
    /// and release it when they recover or die — without this, a crowd would
    /// stack attacks on the same frame and the fight would feel unfair.
    ///
    /// Ring slots (spatial reservations around the player) are M3.2 and are NOT
    /// implemented here.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CombatCoordinator : MonoBehaviour
    {
        [Tooltip("Maximum melee attackers allowed to hold an attack token at once.")]
        [SerializeField] private int maxMeleeAttackers = 2;

        private readonly HashSet<EnemyController> attackers = new();

        /// <summary>Scene singleton (set in Awake, cleared in OnDestroy).</summary>
        public static CombatCoordinator Instance { get; private set; }

        /// <summary>How many enemies currently hold an attack token (debug seam).</summary>
        public int AttackerCount => attackers.Count;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        /// <summary>
        /// Grants an attack token to <paramref name="enemy"/>. Returns false when
        /// the cap is reached; a duplicate request from the same enemy is granted
        /// (it already owns a token).
        /// </summary>
        public bool TryAcquire(EnemyController enemy)
        {
            if (enemy == null) return false;
            if (attackers.Contains(enemy)) return true;
            if (attackers.Count >= Mathf.Max(0, maxMeleeAttackers)) return false;
            attackers.Add(enemy);
            return true;
        }

        /// <summary>Releases a token. Safe to call for an enemy that never held one.</summary>
        public void Release(EnemyController enemy)
        {
            if (enemy == null) return;
            attackers.Remove(enemy);
        }
    }
}
