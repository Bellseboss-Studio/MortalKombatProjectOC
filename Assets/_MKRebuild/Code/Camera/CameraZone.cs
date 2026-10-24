using Unity.Cinemachine;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

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

        // Authoring warnings latch until the offending condition clears, so OnValidate can
        // run on every inspector keystroke without flooding the console.
        private bool warnedYawDeviation;
        private bool warnedPriorityOverlap;

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

        // --- Authoring validation (WU5) --------------------------------------

        /// <summary>
        /// Flat (XZ) angle in degrees between this zone's vcam forward and the base vcam
        /// forward. Returns -1 when either camera is missing (nothing to compare). The Y
        /// component is ignored on purpose: pitch never rotates the movement basis, only
        /// yaw does.
        /// </summary>
        public float ComputeYawDeviationDegrees()
        {
            if (vcam == null) return -1f;

            CinemachineCamera baseCamera = ResolveBaseCamera();
            if (baseCamera == null) return -1f;

            return Vector3.Angle(
                FlatForward(vcam.transform.forward),
                FlatForward(baseCamera.transform.forward));
        }

        /// <summary>
        /// Authoring guard. Two mistakes are cheap to make and expensive to debug at play
        /// time, so the inspector flags them instead:
        /// 1. a room yaw outside <see cref="maxYawDeviation"/> — the input basis stays on the
        ///    base vcam, so a wider angle makes the view and the controls disagree;
        /// 2. an overlapping neighbour with the same <see cref="priority"/> — equal priorities
        ///    resolve by most recent entry, so the winner depends on walk order.
        /// Runtime behaviour is untouched: this only logs.
        /// </summary>
        private void OnValidate()
        {
            // Prefab-asset edits have no scene peers (and no base camera) to compare against.
            if (!gameObject.scene.IsValid()) return;

            ValidateYawDeviation();
            ValidatePriorityOverlap();
        }

        private void ValidateYawDeviation()
        {
            if (vcam == null) return;

            float deviation = ComputeYawDeviationDegrees();
            if (deviation > maxYawDeviation)
            {
                if (warnedYawDeviation) return;
                warnedYawDeviation = true;
                Debug.LogWarning(
                    $"[{nameof(CameraZone)}] '{name}' sits {deviation:0.#}° off the base camera " +
                    $"(authoring band ±{maxYawDeviation:0}°). The input basis stays on the base vcam, " +
                    "so a wider angle makes the view and the controls disagree.",
                    this);
                return;
            }

            warnedYawDeviation = false; // condition cleared: a later break warns again
        }

        private void ValidatePriorityOverlap()
        {
            Collider own = volume != null ? volume : GetComponent<Collider>();
            if (own == null) return;

            CameraZone[] zones = FindObjectsByType<CameraZone>(FindObjectsSortMode.None);
            for (int i = 0; i < zones.Length; i++)
            {
                CameraZone other = zones[i];
                if (other == null || other == this) continue;
                if (other.priority != priority) continue;

                Collider otherVolume = other.volume != null ? other.volume : other.GetComponent<Collider>();
                if (otherVolume == null) continue;
                if (!own.bounds.Intersects(otherVolume.bounds)) continue;

                if (warnedPriorityOverlap) return;
                warnedPriorityOverlap = true;
                Debug.LogWarning(
                    $"[{nameof(CameraZone)}] '{name}' shares priority {priority} with overlapping zone " +
                    $"'{other.name}'. Equal priorities resolve by most recent entry, so the winner " +
                    "depends on walk order — give them distinct priorities or separate volumes.",
                    this);
                return;
            }

            warnedPriorityOverlap = false;
        }

        /// <summary>
        /// Scene-view authoring aid: the trigger volume plus a flat-forward comparison between
        /// this zone's vcam and the base vcam. Green = inside the ±band, red = out of band,
        /// amber = can't compare (a camera is unassigned). Runtime behaviour is untouched.
        /// </summary>
        private void OnDrawGizmosSelected()
        {
            Collider collider = volume != null ? volume : GetComponent<Collider>();
            if (collider == null) return;

            Bounds bounds = collider.bounds;
            CinemachineCamera zoneCamera = vcam;
            CinemachineCamera baseCamera = ResolveBaseCamera();

            float deviation = ComputeYawDeviationDegrees();
            bool comparable = zoneCamera != null && baseCamera != null && deviation >= 0f;
            bool outOfBand = comparable && deviation > maxYawDeviation;

            Color color = !comparable
                ? new Color(0.95f, 0.75f, 0.2f) // amber: missing camera
                : outOfBand ? Color.red : Color.green;

            Gizmos.color = color;
            Gizmos.DrawWireCube(bounds.center, bounds.size);

            if (!comparable) return;

            // Draw both directions from the volume centre; the grey ray is the base vcam's
            // (the controls' frame) and the coloured ray is this zone's authored view.
            Vector3 zoneForward = FlatForward(zoneCamera.transform.forward);
            Vector3 baseForward = FlatForward(baseCamera.transform.forward);
            float rayLength = Mathf.Clamp(bounds.extents.magnitude, 1f, 4f);
            Vector3 anchor = bounds.center;

            Gizmos.color = new Color(0.45f, 0.45f, 0.45f);
            Gizmos.DrawLine(anchor, anchor + baseForward * rayLength);

            Gizmos.color = color;
            Gizmos.DrawLine(anchor, anchor + zoneForward * rayLength);
            Gizmos.DrawSphere(anchor + zoneForward * rayLength, 0.12f);

#if UNITY_EDITOR
            string label = $"{deviation:0.#}° / max {maxYawDeviation:0}°";
            Handles.Label(anchor + Vector3.up * (bounds.extents.y + 0.4f), label);
#endif
        }

        private static CinemachineCamera ResolveBaseCamera()
        {
            CameraDirector director = CameraDirector.Instance;
            if (director == null) director = FindFirstObjectByType<CameraDirector>();
            return director != null ? director.BaseCamera : null;
        }

        private static Vector3 FlatForward(Vector3 forward)
        {
            forward.y = 0f;
            return forward.sqrMagnitude > 0.0001f ? forward.normalized : Vector3.forward;
        }
    }
}
