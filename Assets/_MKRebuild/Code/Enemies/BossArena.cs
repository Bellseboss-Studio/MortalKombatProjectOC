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

        [Tooltip("Out portal root (BossPortal_Out). Inactive until the boss dies.")]
        [SerializeField] private GameObject exitPortal;

        [Tooltip("Seconds the arena waits after the player enters before closing the walls. " +
                  "Gives time to step inside (or walk back in after a respawn) before the lock.")]
        [SerializeField] private float closeDelaySeconds = 1f;

        public static BossArena Instance { get; private set; }

        private BossController boss;
        private PlayerDamage player;
        private bool closed;
        private bool unlocked;
        private bool closePending;
        private float closeTimer;

        private void Awake()
        {
            Instance = this;
            unlocked = false;
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

            if (!IsPlayerInside())
            {
                // Left the zone before the lock: cancel the pending close.
                closePending = false;
                closeTimer = 0f;
                return;
            }

            if (!closePending)
            {
                BeginClose();
                return;
            }

            closeTimer -= Time.deltaTime;
            if (closeTimer <= 0f) Close();
        }

        /// <summary>Closes the arena if the bound boss is alive and the player is inside.</summary>
        public void CloseIfPlayerInside()
        {
            if (boss == null || boss.IsDead || trigger == null) return;
            if (!IsPlayerInside()) return;
            BeginClose();
        }

        private bool IsPlayerInside()
        {
            if (trigger == null) return false;
            if (player == null) player = FindFirstObjectByType<PlayerDamage>();
            if (player == null) return false;
            return trigger.bounds.Contains(player.transform.position);
        }

        /// <summary>
        /// Starts the close countdown. The walls only activate after
        /// <see cref="closeDelaySeconds"/>, so entering the trigger (or walking back
        /// in after a respawn) is never cut off by an instant lock.
        /// </summary>
        private void BeginClose()
        {
            if (closed || closePending) return;
            closePending = true;
            closeTimer = Mathf.Max(0f, closeDelaySeconds);
            if (closeTimer <= 0f) Close();
        }

        /// <summary>Activates every barrier + the exit wall, hides the out portal. Idempotent.</summary>
        public void Close()
        {
            if (closed) return;
            closed = true;
            closePending = false;
            closeTimer = 0f;
            unlocked = false;
            SetBarriersActive(true);
            SetExitPortalActive(false);
            if (exitWall != null)
            {
                exitWall.SetActive(true);

                // Phase 2 keeps the broken-exit visual after a reset; phase 1 restores it.
                var r = exitWall.GetComponentInChildren<Renderer>();
                if (r != null) r.enabled = boss == null || boss.Phase < 2;
            }
        }

        /// <summary>
        /// Deactivates every barrier + the exit wall. The out portal stays INACTIVE
        /// until <see cref="UnlockExit"/> (an explicit kill unlock), so opening the arena
        /// before/during the fight does not give the player an early way out. Idempotent.
        /// </summary>
        public void Open()
        {
            closed = false;
            closePending = false;
            closeTimer = 0f;
            SetBarriersActive(false);
            if (exitWall != null) exitWall.SetActive(false);
            if (!unlocked) SetExitPortalActive(false);
        }

        /// <summary>
        /// Boss-death unlock: opens the arena AND activates the out portal. The unlock is
        /// sticky across later <see cref="Open"/> calls (e.g. the boss object being
        /// destroyed by its death routine), so the way out cannot disappear after the kill.
        /// Idempotent.
        /// </summary>
        public void UnlockExit()
        {
            closed = false;
            unlocked = true;
            SetBarriersActive(false);
            if (exitWall != null) exitWall.SetActive(false);
            SetExitPortalActive(true);
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
            if (other.GetComponentInParent<PlayerDamage>() != null) BeginClose();
        }

        private void SetBarriersActive(bool active)
        {
            if (barriers == null) return;
            for (int i = 0; i < barriers.Length; i++)
            {
                if (barriers[i] != null) barriers[i].SetActive(active);
            }
        }

        private void SetExitPortalActive(bool active)
        {
            if (exitPortal != null) exitPortal.SetActive(active);
        }
    }
}
