using System.Collections.Generic;
using UnityEngine;

namespace MK.Rebuild
{
    /// <summary>
    /// Scene-level combat gate with independent reservations:
    /// 1. Melee attack tokens cap how many melee/charge enemies may press at once.
    /// 2. Ranged tokens cap how many shooters may hold a firing slot at once.
    /// 3. Ring slots give every non-attacker a distinct angle around the player
    ///    so a crowd circles instead of stacking on one point.
    ///
    /// Enemies ask for a token when they commit and release it when they recover
    /// or die; ring slots are acquired when a Melee/Charge enemy enters Chase and
    /// released only on death/disable (stagger keeps its slot). Ranged/backstab
    /// archetypes never take a ring slot.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CombatCoordinator : MonoBehaviour
    {
        [Tooltip("Maximum melee attackers allowed to hold an attack token at once.")]
        [SerializeField] private int maxMeleeAttackers = 2;

        [Tooltip("Maximum ranged attackers (Tirador) allowed to hold a ranged token at once.")]
        [SerializeField] private int maxRangedAttackers = 1;

        [Tooltip("Radius of the crowd ring the non-attackers occupy around the player.")]
        [SerializeField] private float ringRadius = 3f;

        [Tooltip("Number of evenly spaced ring slots (angles) around the player.")]
        [SerializeField] private int ringSlots = 6;

        private readonly HashSet<EnemyController> attackers = new();
        private readonly HashSet<EnemyController> rangedAttackers = new();
        private readonly Dictionary<EnemyController, int> slots = new();
        private bool[] slotUsed;

        /// <summary>Scene singleton (set in Awake, cleared in OnDestroy).</summary>
        public static CombatCoordinator Instance { get; private set; }

        /// <summary>How many enemies currently hold an attack token (debug seam).</summary>
        public int AttackerCount => attackers.Count;

        /// <summary>How many enemies currently hold a ranged token (debug seam).</summary>
        public int RangedAttackerCount => rangedAttackers.Count;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            slotUsed = new bool[Mathf.Max(1, ringSlots)];
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

        /// <summary>
        /// Grants a RANGED attack token to <paramref name="enemy"/>. Mirrors the
        /// melee rule: duplicates are granted, the cap rejects newcomers.
        /// </summary>
        public bool TryAcquireRanged(EnemyController enemy)
        {
            if (enemy == null) return false;
            if (rangedAttackers.Contains(enemy)) return true;
            if (rangedAttackers.Count >= Mathf.Max(0, maxRangedAttackers)) return false;
            rangedAttackers.Add(enemy);
            return true;
        }

        /// <summary>Releases a ranged token. Safe to call for an enemy that never held one.</summary>
        public void ReleaseRanged(EnemyController enemy)
        {
            if (enemy == null) return;
            rangedAttackers.Remove(enemy);
        }

        // --- Ring slots -------------------------------------------------------

        /// <summary>
        /// Reserves the FREE ring slot closest to the enemy's current world angle
        /// around the player. Already-assigned enemies keep their slot. When every
        /// slot is taken the closest one is reused (the grey-box crowd is at most
        /// <see cref="ringSlots"/>), mirroring the token cap's forgiving fallback.
        /// </summary>
        public void AcquireSlot(EnemyController enemy, Vector3 playerPosition)
        {
            if (enemy == null) return;

            PruneDestroyedSlots();
            if (slots.ContainsKey(enemy)) return;

            EnsureSlotArray();

            float slotCount = Mathf.Max(1, ringSlots);
            Vector3 delta = enemy.transform.position - playerPosition;
            float enemyAngle = Mathf.Atan2(delta.x, delta.z) * Mathf.Rad2Deg;

            int best = -1;
            float bestDelta = float.MaxValue;
            for (int i = 0; i < slotUsed.Length; i++)
            {
                if (slotUsed[i]) continue;
                float angle = i * 360f / slotCount;
                float distance = Mathf.Abs(Mathf.DeltaAngle(enemyAngle, angle));
                if (distance < bestDelta)
                {
                    bestDelta = distance;
                    best = i;
                }
            }

            // Crowd overflow: reuse the closest slot (duplicates are acceptable
            // at this scale, and the crowd never exceeds the slot count).
            if (best < 0)
            {
                bestDelta = float.MaxValue;
                for (int i = 0; i < slotUsed.Length; i++)
                {
                    float angle = i * 360f / slotCount;
                    float distance = Mathf.Abs(Mathf.DeltaAngle(enemyAngle, angle));
                    if (distance < bestDelta)
                    {
                        bestDelta = distance;
                        best = i;
                    }
                }
            }

            if (best < 0) best = 0;
            slots[enemy] = best;
            slotUsed[best] = true;
        }

        /// <summary>
        /// World position of the enemy's ring slot around the player. Acquires a
        /// slot on first use so callers never need a separate booking step.
        /// </summary>
        public Vector3 GetSlotPosition(EnemyController enemy, Vector3 playerPosition)
        {
            if (enemy == null) return playerPosition;

            if (!slots.ContainsKey(enemy)) AcquireSlot(enemy, playerPosition);
            if (!slots.TryGetValue(enemy, out int slot)) return playerPosition;

            float angle = slot * 360f / Mathf.Max(1, ringSlots);
            Vector3 offset = Quaternion.Euler(0f, angle, 0f) * Vector3.forward * ringRadius;
            return playerPosition + offset;
        }

        /// <summary>Frees the enemy's ring slot. Safe to call without one.</summary>
        public void ReleaseSlot(EnemyController enemy)
        {
            if (enemy == null) return;
            if (!slots.TryGetValue(enemy, out int slot)) return;

            slots.Remove(enemy);

            if (slot < 0 || slot >= slotUsed.Length) return;

            // A duplicated fallback slot stays reserved while another enemy uses it.
            slotUsed[slot] = false;
            foreach (var pair in slots)
            {
                if (pair.Value == slot)
                {
                    slotUsed[slot] = true;
                    break;
                }
            }
        }

        private void EnsureSlotArray()
        {
            if (slotUsed == null || slotUsed.Length == 0)
            {
                slotUsed = new bool[Mathf.Max(1, ringSlots)];
            }
        }

        /// <summary>
        /// Rebuilds the slot bookkeeping when a destroyed enemy never released
        /// (scene teardown, external Destroy). Destroyed Unity objects compare
        /// equal to null, so they are easy to drop before assigning new slots.
        /// </summary>
        private void PruneDestroyedSlots()
        {
            if (slots.Count == 0) return;

            bool hasDestroyed = false;
            foreach (var pair in slots)
            {
                if (pair.Key == null)
                {
                    hasDestroyed = true;
                    break;
                }
            }

            if (!hasDestroyed) return;

            var rebuilt = new Dictionary<EnemyController, int>(slots.Count);
            var used = new bool[Mathf.Max(1, ringSlots)];

            foreach (var pair in slots)
            {
                if (pair.Key == null) continue;
                rebuilt[pair.Key] = pair.Value;
                if (pair.Value >= 0 && pair.Value < used.Length) used[pair.Value] = true;
            }

            slots.Clear();
            foreach (var pair in rebuilt) slots[pair.Key] = pair.Value;
            slotUsed = used;
        }
    }
}
