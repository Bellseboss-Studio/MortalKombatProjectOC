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
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(150)]
    public sealed class CameraDirector : MonoBehaviour
    {
        /// <summary>Priority the base gameplay vcam always holds; rooms beat it by default.</summary>
        private const int BasePriority = 10;

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

        private readonly List<CameraZone> zones = new List<CameraZone>();
        private readonly List<CameraZone> active = new List<CameraZone>();
        private bool warnedRigOnZone;

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
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
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
