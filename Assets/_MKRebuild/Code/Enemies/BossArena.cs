using UnityEngine;

namespace MK.Rebuild
{
    /// <summary>
    /// M3.5a grey-box boss arena lock for S7. A trigger volume on the arena floor
    /// closes the authored N/S/W barriers (and the east exit wall) while the boss
    /// fight is live, so the player cannot leave the section. The arena starts
    /// OPEN; <see cref="BossController.Start"/> calls <see cref="Bind"/>, which
    /// closes immediately when the player is already inside, otherwise the trigger
    /// closes it on entry.
    ///
    /// The boss owns the lifetime: phase transition breaks only the exit wall's
    /// VISUAL (<see cref="BreakExitWall"/>), while its collider stays until the boss
    /// dies or the player-death reset opens the arena again. All operations are
    /// idempotent.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class BossArena : MonoBehaviour
    {
        [Tooltip("Arena volume used to detect the player entering/being inside.")]
        [SerializeField] private Collider trigger;

        [Tooltip("North / South / West wall objects closed during the fight.")]
        [SerializeField] private GameObject[] barriers;

        [Tooltip("East exit wall (renderer + collider). Closed during the fight.")]
        [SerializeField] private GameObject exitWall;

        public static BossArena Instance { get; private set; }

        private BossController boss;
        private bool closed;

        private void Awake()
        {
            Instance = this;
            Open();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        /// <summary>
        /// Binds the live boss (no serialized reference) and closes the arena when
        /// the player is already inside the trigger volume on spawn.
        /// </summary>
        public void Bind(BossController b)
        {
            boss = b;
            CloseIfPlayerInside();
        }

        /// <summary>
        /// Re-locks the arena when the player respawns inside the trigger after a
        /// player-death reset: OnTriggerEnter cannot re-fire for a body that never
        /// left the volume, so the open state is corrected here.
        /// </summary>
        private void Update()
        {
            if (boss == null || boss.IsDead || closed) return;
            CloseIfPlayerInside();
        }

        /// <summary>Closes the arena if the bound boss is alive and the player is inside.</summary>
        public void CloseIfPlayerInside()
        {
            if (boss == null || boss.IsDead || trigger == null) return;

            var player = FindFirstObjectByType<PlayerDamage>();
            if (player == null) return;
            if (trigger.bounds.Contains(player.transform.position)) Close();
        }

        /// <summary>Activates every barrier + the exit wall. Idempotent.</summary>
        public void Close()
        {
            if (closed) return;
            closed = true;
            SetBarriersActive(true);
            if (exitWall != null)
            {
                exitWall.SetActive(true);

                // Phase 2 keeps the broken-exit visual after a reset; phase 1 restores it.
                var r = exitWall.GetComponentInChildren<Renderer>();
                if (r != null) r.enabled = boss == null || boss.Phase < 2;
            }
        }

        /// <summary>Deactivates every barrier + the exit wall. Idempotent.</summary>
        public void Open()
        {
            closed = false;
            SetBarriersActive(false);
            if (exitWall != null) exitWall.SetActive(false);
        }

        /// <summary>
        /// Breaks the exit wall VISUAL only (renderer off) while keeping its collider,
        /// so the way out is revealed at the 50% transition but the arena stays locked.
        /// </summary>
        public void BreakExitWall()
        {
            if (exitWall == null) return;
            var wallRenderer = exitWall.GetComponent<MeshRenderer>();
            if (wallRenderer != null) wallRenderer.enabled = false;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (boss == null || boss.IsDead || other == null) return;
            if (other.GetComponentInParent<PlayerDamage>() != null) Close();
        }

        private void SetBarriersActive(bool active)
        {
            if (barriers == null) return;
            for (int i = 0; i < barriers.Length; i++)
            {
                if (barriers[i] != null) barriers[i].SetActive(active);
            }
        }
    }
}
