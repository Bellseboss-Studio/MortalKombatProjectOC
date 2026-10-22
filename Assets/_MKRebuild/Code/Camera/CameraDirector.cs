using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;

namespace MK.Rebuild
{
    /// <summary>
    /// Single arbiter for gameplay cameras. Zones never toggle their vcam: they report
    /// entry/exit and this director writes <c>Priority</c>, letting the Cinemachine brain
    /// keep ownership of the blend. One writer also means a teleport can shift every
    /// camera by the same delta — the only reliable fix for a camera dragged across the level.
    ///
    /// Priority contract (design §3): base = 10, room = 20, boss = 30 (WU2). Never
    /// <c>enabled</c>: disabling a vcam makes the brain cut away from it and can leave a
    /// mid-shot blend frozen.
    ///
    /// Boss mode reacts to <see cref="BossSpawner.Spawned"/>, not to the encounter trigger:
    /// whatever starts the fight (trigger volume, debug key, future flow) gets the boss
    /// camera, and the camera never needs to know how the fight began.
    ///
    /// Engagement is bound to the arena FOOTPRINT, not to proximity: the player is on the
    /// boss camera while they stand inside <c>bossArenaVolume</c> and drops back to the
    /// zone/base camera the moment they leave it. Distance cannot express that — the S7 exit
    /// and the S10 checkpoint are both close enough to the boss spawn to stay "near", which
    /// is exactly how the camera used to get stuck in boss mode.
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(150)]
    public sealed class CameraDirector : MonoBehaviour
    {
        /// <summary>Priority the base gameplay vcam always holds; rooms beat it by default.</summary>
        private const int BasePriority = 10;

        /// <summary>Boss mode outranks every room zone (20) for the whole fight.</summary>
        private const int BossPriority = 30;

        /// <summary>Bound radius given to each boss-group member (design §3).</summary>
        private const float BossMemberRadius = 0.5f;

        [Tooltip("Scene brain. Null = auto-resolved from Camera.main, then the first in scene.")]
        [SerializeField] private CinemachineBrain brain;

        [Tooltip("Base gameplay vcam (the one that owns CameraRig). Null = auto-resolved from CameraRig, then first in scene.")]
        [SerializeField] private CinemachineCamera baseCamera;

        [Tooltip("Player root used for presence and Follow wiring. Null = auto-resolved from PlayerMotor.")]
        [SerializeField] private Transform player;

        [Tooltip("Blend (seconds) used when a room releases the camera back to the base vcam.")]
        [SerializeField] private float defaultGameplayBlend = 0.4f;

        [Tooltip("Reserved for WU2: boss-mode blend. Declared now so the camera policy has one home.")]
        [SerializeField] private float bossBlend = 0.6f;

        [Tooltip("Boss-mode vcam. Holds priority 30 while boss mode is active, 0 otherwise.")]
        [SerializeField] private CinemachineCamera bossCamera;

        [Tooltip("Target group the boss vcam follows. Author it empty: the director adds the player and the boss.")]
        [SerializeField] private CinemachineTargetGroup bossGroup;

        [Tooltip("Boss spawner. Boss mode starts on its Spawned event, so it reacts to a trigger, the debug key or any future flow.")]
        [SerializeField] private BossSpawner bossSpawner;

        [Tooltip("Arena trigger whose XZ footprint decides boss-camera engagement: inside = boss camera, outside = back to the zone/base camera. Y is ignored on purpose so a jump inside the arena cannot flicker the camera off.")]
        [SerializeField] private Collider bossArenaVolume;

        [Tooltip("Fallback only, used when no arena volume is assigned: max player↔boss distance (m) for the boss camera. It releases on a far respawn/teleport, so it is still better than never exiting.")]
        [SerializeField] private float bossActivationRange = 40f;

        private readonly List<CameraZone> zones = new List<CameraZone>();
        private readonly List<CameraZone> active = new List<CameraZone>();
        private bool warnedRigOnZone;
        private bool warnedBossWiring;
        private bool warnedArenaVolume;

        private Transform bossMember;
        private BossController bossController;
        private bool bossModeActive;

        /// <summary>Scene singleton; null in scenes without a director (WU1b wires it).</summary>
        public static CameraDirector Instance { get; private set; }

        /// <summary>Base gameplay vcam (the input basis owner).</summary>
        public CinemachineCamera BaseCamera => baseCamera;

        /// <summary>Scene brain the director writes blends into.</summary>
        public CinemachineBrain Brain => brain;

        /// <summary>Player root that Follow-mode zones wire their targets to.</summary>
        public Transform Player => player;

        /// <summary>Room→base blend (seconds).</summary>
        public float DefaultGameplayBlend => defaultGameplayBlend;

        /// <summary>Boss-mode blend (seconds); consumed by WU2.</summary>
        public float BossBlend => bossBlend;

        /// <summary>True between a successful <see cref="EnterBossMode"/> and <see cref="ExitBossMode"/>.</summary>
        public bool BossModeActive => bossModeActive;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else if (Instance != this)
            {
                Debug.LogWarning($"[{nameof(CameraDirector)}] Duplicate director on '{name}' ignored; '{Instance.name}' owns the camera.", this);
            }

            ResolveReferences();
            if (baseCamera != null) ApplyPriority(baseCamera, BasePriority);
            if (bossCamera != null) ApplyPriority(bossCamera, 0);
        }

        /// <summary>
        /// Subscribes to the boss spawn signal. Boss mode is driven by the SPAWN, not by the
        /// encounter trigger, so every path (trigger volume, debug key, future flow) enters
        /// the boss camera exactly once.
        /// </summary>
        private void OnEnable()
        {
            if (bossSpawner == null)
            {
                WarnBossWiringOnce("no BossSpawner assigned");
                return;
            }

            bossSpawner.Spawned += HandleBossSpawned;

            // The boss may already be live (a scene reload, or a spawn that happened before
            // this director enabled). Seed from the spawner so presence-based boss mode has
            // a target without waiting for the next spawn signal.
            if (bossSpawner.Current != null) AttachBoss(bossSpawner.Current);
        }

        /// <summary>Drops the spawn subscription and any live boss subscription.</summary>
        private void OnDisable()
        {
            if (bossSpawner != null) bossSpawner.Spawned -= HandleBossSpawned;
            DetachBoss();
        }

        private void OnDestroy()
        {
            if (bossSpawner != null) bossSpawner.Spawned -= HandleBossSpawned;
            DetachBoss();
            if (Instance == this) Instance = null;
        }

        /// <summary>
        /// Arena-footprint boss mode. A one-shot enter is wrong: the player can die and
        /// respawn at the checkpoint, be teleported, walk out into S7 and back in, and none
        /// of those paths re-fires the spawn signal. The arena trigger's XZ footprint covers
        /// every path without a new volume, and costs a few compares per frame: no
        /// allocations, no Reevaluate.
        /// </summary>
        private void Update()
        {
            if (player == null || bossSpawner == null) return;

            if (bossController == null || bossController.IsDead)
            {
                if (bossModeActive) ExitBossMode();
                return;
            }

            bool insideArena = IsPlayerInsideBossArena(bossController.transform.position);

            if (insideArena)
            {
                if (!bossModeActive) EnterBossMode(bossController.transform);
            }
            else if (bossModeActive)
            {
                ExitBossMode();
            }
        }

        /// <summary>
        /// True when the player stands inside the arena's XZ footprint. Y is deliberately
        /// ignored: the arena is 4 m tall and the player jumps, so a 3D test would drop boss
        /// mode mid-jump and flicker the camera. Without an authored volume this falls back
        /// to the boss distance so the camera still releases on a far respawn or teleport.
        /// </summary>
        private bool IsPlayerInsideBossArena(Vector3 bossPosition)
        {
            if (bossArenaVolume == null)
            {
                WarnArenaVolumeOnce();
                return Vector3.Distance(player.position, bossPosition) <= bossActivationRange;
            }

            Vector3 p = player.position;
            Bounds b = bossArenaVolume.bounds;
            return p.x >= b.min.x && p.x <= b.max.x && p.z >= b.min.z && p.z <= b.max.z;
        }

        private void ResolveReferences()
        {
            if (brain == null)
            {
                Camera main = Camera.main;
                if (main != null) brain = main.GetComponent<CinemachineBrain>();
                if (brain == null) brain = FindFirstObjectByType<CinemachineBrain>();
            }

            if (baseCamera == null)
            {
                CameraRig rig = FindFirstObjectByType<CameraRig>();
                if (rig != null) baseCamera = rig.GetComponent<CinemachineCamera>();
                if (baseCamera == null) baseCamera = FindFirstObjectByType<CinemachineCamera>();
            }

            if (player == null)
            {
                PlayerMotor motor = FindFirstObjectByType<PlayerMotor>();
                if (motor != null) player = motor.transform;
            }

            if (bossSpawner == null) bossSpawner = BossSpawner.Instance;
        }

        /// <summary>Adds a zone to the arbitration set. Presence starts on its first accepted enter.</summary>
        public void Register(CameraZone zone)
        {
            if (zone == null || zones.Contains(zone)) return;
            zones.Add(zone);

            // Belt-and-braces: the input basis lives on the base vcam. A rig on a room
            // vcam would rotate the movement frame whenever that room wins the camera.
            if (!warnedRigOnZone && zone.Vcam != null && zone.Vcam.GetComponent<CameraRig>() != null)
            {
                warnedRigOnZone = true;
                Debug.LogWarning($"[{nameof(CameraDirector)}] Zone '{zone.name}' owns a {nameof(CameraRig)}; the input basis must stay on the base vcam.", zone);
            }
        }

        /// <summary>Drops a zone (destroy/unload) and re-resolves the remaining priorities.</summary>
        public void Unregister(CameraZone zone)
        {
            if (zone == null) return;
            active.Remove(zone);
            if (zones.Remove(zone)) Reevaluate();
        }

        /// <summary>Zone entered: it joins the active set and arbitration runs immediately.</summary>
        public void NotifyEnter(CameraZone zone)
        {
            if (zone == null || !zones.Contains(zone)) return;
            if (!active.Contains(zone)) active.Add(zone);
            Reevaluate();
        }

        /// <summary>Zone exited: it leaves the active set and arbitration runs immediately.</summary>
        public void NotifyExit(CameraZone zone)
        {
            if (zone == null) return;
            active.Remove(zone);
            Reevaluate();
        }

        /// <summary>
        /// Applies the priority contract: base = 10, the winning zone = its priority, every
        /// other zone = 0. Ties resolve by the most recent entry sequence, so the result is
        /// deterministic even when two volumes overlap.
        /// </summary>
        public void Reevaluate()
        {
            for (int i = 0; i < zones.Count; i++)
            {
                CameraZone zone = zones[i];
                if (zone != null && zone.Vcam != null) ApplyPriority(zone.Vcam, 0);
            }

            if (baseCamera != null) ApplyPriority(baseCamera, BasePriority);

            // Boss mode owns the camera for the whole fight: it must outrank every room,
            // and it must survive a room entering/exiting while the fight is running.
            if (bossCamera != null) ApplyPriority(bossCamera, bossModeActive ? BossPriority : 0);

            CameraZone winner = null;
            int bestPriority = int.MinValue;
            uint bestSequence = 0;

            for (int i = 0; i < active.Count; i++)
            {
                CameraZone zone = active[i];
                if (zone == null)
                {
                    active.RemoveAt(i--);
                    continue;
                }

                if (zone.Vcam == null) continue;

                if (zone.Priority > bestPriority ||
                    (zone.Priority == bestPriority && zone.EnterSequence > bestSequence))
                {
                    winner = zone;
                    bestPriority = zone.Priority;
                    bestSequence = zone.EnterSequence;
                }
            }

            if (TryFindDuplicatePriority(out int duplicatePriority))
            {
                Debug.LogWarning($"[{nameof(CameraDirector)}] Two active {nameof(CameraZone)}s share priority {duplicatePriority}; resolving by most recent entry.", this);
            }

            if (winner != null) ApplyPriority(winner.Vcam, winner.Priority);
        }

        /// <summary>
        /// Shifts the base and every registered zone camera by <paramref name="delta"/>, then
        /// rebuilds presence from collider bounds and re-resolves. Callers must pass the delta
        /// captured BEFORE the teleport: after the move it is zero and changes nothing.
        /// </summary>
        public void WarpBy(Vector3 delta)
        {
            WarpCamera(baseCamera, delta);

            for (int i = 0; i < zones.Count; i++)
            {
                CameraZone zone = zones[i];
                if (zone != null) WarpCamera(zone.Vcam, delta);
            }

            SyncPresence();
            Reevaluate();
        }

        /// <summary>Writes the brain's default blend; CustomBlends stays null so this always wins.</summary>
        public void SetBlend(CinemachineBlendDefinition blend)
        {
            if (brain == null) return;
            brain.DefaultBlend = blend;
        }

        /// <summary>
        /// Picks the blend for the incoming camera: a room keeps its authored blend, while
        /// releasing back to the base uses the default gameplay blend.
        /// </summary>
        public void SetBlendBeforeSwap(CameraZone destination)
        {
            if (destination != null)
            {
                SetBlend(destination.Blend);
                return;
            }

            CinemachineBlendDefinition blend = default;
            blend.Style = CinemachineBlendDefinition.Styles.EaseInOut;
            blend.Time = defaultGameplayBlend;
            SetBlend(blend);
        }

        /// <summary>
        /// Enters boss mode: frames the player and <paramref name="boss"/> through the boss
        /// target group, raises the boss vcam to priority 30 and blends with
        /// <see cref="bossBlend"/>. Idempotent — a second call neither duplicates group
        /// members nor restarts the blend. A null <paramref name="boss"/> is allowed: the
        /// editor toggle uses it to eyeball the framing with the player alone.
        /// </summary>
        public void EnterBossMode(Transform boss)
        {
            if (bossGroup == null || bossCamera == null)
            {
                WarnBossWiringOnce("no CinemachineTargetGroup or boss vcam assigned");
                return;
            }

            if (player != null) AddMemberOnce(player);
            if (boss != null) AddMemberOnce(boss);

            // CinemachineTargetGroup is [ExecuteAlways] and rewrites its own transform from
            // the member bounding sphere in LateUpdate. With zero members that is the world
            // origin, so update it NOW: otherwise the first live frame evaluates the camera
            // at origin + offset and the follow damping turns that into a swoop across the map.
            bossGroup.DoUpdate();

            bossMember = boss;
            bossModeActive = true;

            // The destination owns the transition, so the blend is written before the swap.
            SetBlend(MakeBlend(bossBlend));
            Reevaluate();
        }

        /// <summary>
        /// Leaves boss mode: removes the group members, drops the boss vcam to 0 and blends
        /// back to the base camera. Safe to call when boss mode was never active.
        ///
        /// The cached boss (<see cref="bossController"/>) and its <c>Died</c> subscription
        /// are deliberately NOT dropped here: boss mode is arena-driven, so leaving the
        /// footprint must be reversible. Clearing the cache on exit would strand the director
        /// with no target and it could never re-enter when the player walks back in.
        /// </summary>
        public void ExitBossMode()
        {
            RemoveMemberOnce(player);
            RemoveMemberOnce(bossMember);
            bossMember = null;

            if (!bossModeActive) return;

            bossModeActive = false;
            SetBlend(MakeBlend(defaultGameplayBlend));
            Reevaluate();
        }

        /// <summary>
        /// Editor-only framing test (WU2 acceptance): toggles boss mode using the live boss
        /// when known, otherwise the player alone. Never throws with an unassigned group or
        /// vcam — the guard logs one warning and returns.
        /// </summary>
        [ContextMenu("Debug/Toggle Boss Camera")]
        private void DebugToggleBossCamera()
        {
            if (bossModeActive)
            {
                ExitBossMode();
                return;
            }

            Transform focus = bossMember != null ? bossMember : player;
            EnterBossMode(focus);
        }

        /// <summary>
        /// Boss spawned: <see cref="BossSpawner.Spawned"/> already guarantees a live
        /// instance, so this only attaches the death listener and enters boss mode.
        /// </summary>
        private void HandleBossSpawned(BossController boss)
        {
            if (boss == null)
            {
                WarnBossWiringOnce("BossSpawner reported a null boss");
                return;
            }

            AttachBoss(boss);
            EnterBossMode(boss.transform);
        }

        private void HandleBossDied()
        {
            ExitBossMode();
        }

        private void AttachBoss(BossController boss)
        {
            if (bossController == boss) return;
            DetachBoss();
            bossController = boss;
            if (bossController != null) bossController.Died += HandleBossDied;
        }

        private void DetachBoss()
        {
            if (bossController == null) return;
            bossController.Died -= HandleBossDied;
            bossController = null;
        }

        private void AddMemberOnce(Transform member)
        {
            if (bossGroup == null || member == null) return;
            if (bossGroup.FindMember(member) >= 0) return;
            bossGroup.AddMember(member, 1f, BossMemberRadius);
        }

        private void RemoveMemberOnce(Transform member)
        {
            if (bossGroup == null || member == null) return;
            if (bossGroup.FindMember(member) < 0) return;
            bossGroup.RemoveMember(member);
        }

        private static CinemachineBlendDefinition MakeBlend(float seconds)
        {
            CinemachineBlendDefinition blend = default;
            blend.Style = CinemachineBlendDefinition.Styles.EaseInOut;
            blend.Time = seconds;
            return blend;
        }

        private void WarnBossWiringOnce(string reason)
        {
            if (warnedBossWiring) return;
            warnedBossWiring = true;
            Debug.LogWarning($"[{nameof(CameraDirector)}] Boss mode unavailable: {reason}.", this);
        }

        /// <summary>
        /// One warning, not one per frame, when the arena volume is missing. The distance
        /// fallback is intentionally still active: silently holding boss mode forever is the
        /// worse failure. The message names the scene object so the wiring is obvious.
        /// </summary>
        private void WarnArenaVolumeOnce()
        {
            if (warnedArenaVolume) return;
            warnedArenaVolume = true;
            Debug.LogWarning(
                $"[{nameof(CameraDirector)}] No boss arena volume assigned; falling back to a {bossActivationRange} m player↔boss distance check. Assign the BossArena trigger collider to bind boss mode to the arena footprint.",
                this);
        }

        /// <summary>
        /// Rebuilds the active set from collider bounds. Teleports move the player without
        /// guaranteeing trigger callbacks in the same frame; this keeps arbitration honest.
        /// </summary>
        private void SyncPresence()
        {
            if (player == null) return;
            Vector3 position = player.position;

            for (int i = 0; i < zones.Count; i++)
            {
                CameraZone zone = zones[i];
                if (zone == null) continue;

                bool inside = zone.Contains(position);
                if (inside && !zone.IsActive)
                {
                    if (!zone.TryActivate()) continue;
                    zone.SetActive(true);
                    if (!active.Contains(zone)) active.Add(zone);
                }
                else if (!inside && zone.IsActive)
                {
                    zone.SetActive(false);
                    active.Remove(zone);
                }
            }
        }

        private bool TryFindDuplicatePriority(out int priority)
        {
            for (int i = 0; i < active.Count; i++)
            {
                CameraZone a = active[i];
                if (a == null || a.Vcam == null) continue;

                for (int j = i + 1; j < active.Count; j++)
                {
                    CameraZone b = active[j];
                    if (b == null || b.Vcam == null) continue;
                    if (a.Priority != b.Priority) continue;

                    priority = a.Priority;
                    return true;
                }
            }

            priority = 0;
            return false;
        }

        private static void WarpCamera(CinemachineCamera camera, Vector3 delta)
        {
            if (camera == null) return;

            // Only target-tracking cameras have state to shift. A Fixed zone is an authored
            // shot: moving it by the player delta would break its framing.
            Transform follow = camera.Follow;
            if (follow != null) camera.OnTargetObjectWarped(follow, delta);

            camera.PreviousStateIsValid = false;
        }

        private static void ApplyPriority(CinemachineCamera camera, int value)
        {
            if (camera == null) return;
            camera.Priority.Value = value;
            camera.Priority.Enabled = true;
        }
    }
}
