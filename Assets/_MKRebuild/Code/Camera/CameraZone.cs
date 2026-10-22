using Unity.Cinemachine;
using UnityEngine;

namespace MK.Rebuild
{
    /// <summary>How a zone frames the player while it owns the camera.</summary>
    public enum CameraZoneMode
    {
        /// <summary>Track the player with the zone vcam's authored body/aim.</summary>
        Follow,

        /// <summary>Keep an authored static shot; no target is assigned.</summary>
        Fixed
    }

    /// <summary>Whether walking back into the volume can claim the camera again.</summary>
    public enum CameraZoneReentry
    {
        /// <summary>Every entry can claim the camera (backtracking friendly).</summary>
        Reenter,

        /// <summary>First entry only; reloading the scene resets the latch.</summary>
        OncePerLoad
    }

    /// <summary>
    /// Trigger volume that asks <see cref="CameraDirector"/> to own the camera while the
    /// player is inside. The zone never toggles its vcam: the director arbitrates by
    /// writing <c>Priority</c>, so Cinemachine's own blending stays in charge of the cut.
    ///
    /// The zone deliberately owns no input basis. <see cref="CameraRig.Basis"/> must stay
    /// on the base vcam: moving it to a room camera would rotate the movement/attack frame
    /// out from under the player (design §0, ±25° authoring band).
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider))]
    public sealed class CameraZone : MonoBehaviour
    {
        [Tooltip("Follow = track the player with this vcam's body/aim. Fixed = authored static shot.")]
        [SerializeField] private CameraZoneMode mode = CameraZoneMode.Follow;

        [Tooltip("Virtual camera this zone hands to the director. Required for the zone to win.")]
        [SerializeField] private CinemachineCamera vcam;

        [Tooltip("Arbitration weight while inside. Base vcam is 10; boss is 30 (WU2).")]
        [SerializeField] private int priority = 20;

        [Tooltip("Blend used when the camera enters this zone (the destination owns the transition).")]
        [SerializeField] private CinemachineBlendDefinition blend = new CinemachineBlendDefinition
        {
            Style = CinemachineBlendDefinition.Styles.EaseInOut,
            Time = 0.35f
        };

        [Tooltip("Reenter = every entry wins again. OncePerLoad = first entry only, reset by scene reload.")]
        [SerializeField] private CameraZoneReentry reentry = CameraZoneReentry.Reenter;

        [Tooltip("Authoring guard: a room yaw beyond ±this band makes the view and the controls disagree.")]
        [SerializeField] private float maxYawDeviation = 25f;

        // Global monotonic counter: equal priorities resolve by the most recent entry,
        // which keeps overlapping zones deterministic regardless of physics callback order.
        private static uint nextEnterSequence;

        private Collider volume;
        private bool occupied;       // the trigger currently contains a player
        private bool hasActivated;   // OncePerLoad latch
        private bool warnedNoDirector;
        private bool warnedNoVcam;

        /// <summary>Arbitration priority while this zone is active.</summary>
        public int Priority => priority;

        /// <summary>Virtual camera the director raises/lowers.</summary>
        public CinemachineCamera Vcam => vcam;

        /// <summary>Transition blend owned by this destination zone.</summary>
        public CinemachineBlendDefinition Blend => blend;

        /// <summary>Authoring band (degrees) used by the WU5 yaw gizmo/validation.</summary>
        public float MaxYawDeviation => maxYawDeviation;

        /// <summary>Monotonic entry stamp; the director uses it as the deterministic tie-break.</summary>
        public uint EnterSequence { get; private set; }

        /// <summary>True while the director counts this zone in its active set.</summary>
        public bool IsActive { get; private set; }

        private void Awake()
        {
            volume = GetComponent<Collider>();
        }

        private void Start()
        {
            CameraDirector director = CameraDirector.Instance;
            if (director == null)
            {
                if (!warnedNoDirector)
                {
                    warnedNoDirector = true;
                    Debug.LogWarning($"[{nameof(CameraZone)}] '{name}' has no {nameof(CameraDirector)} in the scene; it stays inert.", this);
                }
                return;
            }

            if (vcam == null && !warnedNoVcam)
            {
                warnedNoVcam = true;
                Debug.LogWarning($"[{nameof(CameraZone)}] '{name}' has no vcam; it can never win arbitration.", this);
            }

            // Registration owns the zero baseline: a zone never starts above the base vcam.
            ApplyPriority(0);
            director.Register(this);

            if (mode == CameraZoneMode.Follow && vcam != null && director.Player != null)
            {
                vcam.Follow = director.Player;
                vcam.LookAt = director.Player;
            }

            // The player may already be inside when the zone enables (scene start or a
            // teleport). Seed presence from bounds so the first frame is correct without
            // waiting for a physics callback.
            if (director.Player != null && Contains(director.Player.position) && TryActivate())
            {
                occupied = true;
                IsActive = true;
                director.NotifyEnter(this);
            }
        }

        private void OnDestroy()
        {
            CameraDirector director = CameraDirector.Instance;
            if (director != null) director.Unregister(this);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (occupied) return;
            if (other == null || other.GetComponentInParent<PlayerDamage>() == null) return;

            occupied = true;
            CameraDirector director = CameraDirector.Instance;
            if (director == null) return;
            if (!TryActivate()) return; // OncePerLoad already spent

            IsActive = true;
            director.NotifyEnter(this);
        }

        private void OnTriggerExit(Collider other)
        {
            if (!occupied) return;
            if (other == null || other.GetComponentInParent<PlayerDamage>() == null) return;

            occupied = false;
            if (!IsActive) return;

            IsActive = false;
            CameraDirector director = CameraDirector.Instance;
            if (director != null) director.NotifyExit(this);
        }

        /// <summary>
        /// Bounds test against the attached collider, so the director can rebuild presence
        /// after a teleport without waiting for a physics step.
        /// </summary>
        public bool Contains(Vector3 worldPoint)
        {
            return volume != null && volume.bounds.Contains(worldPoint);
        }

        /// <summary>
        /// Claims an entry slot. Returns false once a <see cref="CameraZoneReentry.OncePerLoad"/>
        /// zone has already revealed itself; every accepted entry stamps a fresh sequence.
        /// </summary>
        public bool TryActivate()
        {
            if (reentry == CameraZoneReentry.OncePerLoad && hasActivated) return false;

            hasActivated = true;
            EnterSequence = ++nextEnterSequence;
            return true;
        }

        /// <summary>Director-only: keeps <see cref="IsActive"/> honest after a bounds re-resolve.</summary>
        internal void SetActive(bool active)
        {
            IsActive = active;
        }

        private void ApplyPriority(int value)
        {
            if (vcam == null) return;
            vcam.Priority.Value = value;
            vcam.Priority.Enabled = true;
        }
    }
}
