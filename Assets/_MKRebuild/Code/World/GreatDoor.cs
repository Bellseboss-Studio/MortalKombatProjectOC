using UnityEngine;

namespace MK.Rebuild
{
    /// <summary>
    /// The great door that seals the north passage until every lever is thrown. It rises into
    /// the lintel on <see cref="DungeonObjective.Completed"/> and stays open.
    ///
    /// The door is a moving collider, not a trigger: while closed it physically blocks the
    /// passage, so the objective cannot be skipped by walking around it.
    /// </summary>
    public sealed class GreatDoor : MonoBehaviour
    {
        [Tooltip("Door slab that rises. Null = this transform.")]
        [SerializeField] private Transform door;

        [Tooltip("Metres the slab travels upward when it opens.")]
        [SerializeField] private float openHeight = 6f;

        [Tooltip("Seconds the opening takes.")]
        [SerializeField] private float openSeconds = 2.6f;

        [Tooltip("Objective that unlocks this door.")]
        [SerializeField] private DungeonObjective objective;

        [Tooltip("Optional stone that rolls away as the door opens.")]
        [SerializeField] private Transform stone;

        [Tooltip("Metres the stone rolls to the side.")]
        [SerializeField] private float stoneTravel = 5f;

        private Vector3 closedPosition;
        private Vector3 stoneStart;
        private float progress = -1f;

        /// <summary>True once the door has been triggered.</summary>
        public bool IsOpening => progress >= 0f;

        /// <summary>True when the slab has finished travelling.</summary>
        public bool IsOpen => progress >= 1f;

        private void Awake()
        {
            if (door == null) door = transform;
            closedPosition = door.localPosition;
            if (stone != null) stoneStart = stone.localPosition;
        }

        private void OnEnable()
        {
            if (objective != null) objective.Completed += Open;
        }

        private void OnDisable()
        {
            if (objective != null) objective.Completed -= Open;
        }

        /// <summary>Starts the opening. Safe to call twice.</summary>
        public void Open()
        {
            if (IsOpening) return;
            progress = 0f;
        }

        private void Update()
        {
            if (progress < 0f || progress >= 1f) return;

            progress = Mathf.Clamp01(progress + Time.deltaTime / Mathf.Max(0.05f, openSeconds));
            float eased = Mathf.SmoothStep(0f, 1f, progress);

            door.localPosition = closedPosition + Vector3.up * (openHeight * eased);

            if (stone != null)
            {
                stone.localPosition = stoneStart + Vector3.right * (stoneTravel * eased);
                stone.Rotate(Vector3.forward, 90f * Time.deltaTime, Space.Self);
            }
        }
    }
}
