using UnityEngine;

namespace MK.Rebuild
{
    /// <summary>
    /// A dungeon lever. The player plants on <see cref="Interactable.Anchor"/>, pulls, and the
    /// handle swings to its thrown angle while the objective counter advances.
    ///
    /// It is deliberately one-shot: a lever that can be pulled twice would let the counter
    /// exceed the objective and open the door early.
    /// </summary>
    public sealed class LeverInteractable : Interactable
    {
        [Tooltip("Handle that swings when the lever is pulled. Null = no visible motion.")]
        [SerializeField] private Transform handle;

        [Tooltip("Degrees the handle tilts around its local X when thrown.")]
        [SerializeField] private float throwAngle = 55f;

        [Tooltip("Objective this lever reports to.")]
        [SerializeField] private DungeonObjective objective;

        private Quaternion handleRest;
        private float swing = -1f;
        private bool used;

        public override bool CanInteract => !used;

        private void Awake()
        {
            if (handle != null) handleRest = handle.localRotation;
        }

        protected override void OnInteract()
        {
            if (used) return;

            used = true;
            swing = 0f;

            if (objective != null) objective.NotifyActivated(this);
            else Debug.LogWarning("[Lever] Sin DungeonObjective asignado: la palanca no cuenta.", this);
        }

        private void Update()
        {
            if (swing < 0f) return;

            swing += Time.deltaTime / Mathf.Max(0.05f, Duration);
            float t = Mathf.Clamp01(swing);

            if (handle != null)
            {
                handle.localRotation = handleRest * Quaternion.Euler(throwAngle * Mathf.SmoothStep(0f, 1f, t), 0f, 0f);
            }

            if (swing >= 1f) swing = -1f;
        }
    }
}
