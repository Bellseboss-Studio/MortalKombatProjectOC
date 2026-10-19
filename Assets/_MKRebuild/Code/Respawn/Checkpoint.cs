using System.Collections;
using UnityEngine;

namespace MK.Rebuild
{
    /// <summary>
    /// m2-respawn slice 2: a CHECKPOINT trigger. The instant the player walks into this
    /// volume the checkpoint is recorded — no menu, button, or prompt (friction-zero, per
    /// the spec). Recording sets the respawn target to the player's CURRENT position and
    /// facing, tagged with this checkpoint's <c>sectionId</c>, so a later death respawns
    /// exactly here.
    ///
    /// Grey-box feedback: a short color FLASH on this object's renderer (UNSCALED so it
    /// reads even during hitstop) — the same minimal-feedback language as
    /// <see cref="HitReceiver"/>.
    ///
    /// Runtime-only; the heavy lifting lives in <see cref="CheckpointSystem"/> (the respawn
    /// owner, which is on the Player) and <see cref="SectionReset"/> (the restore owner).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class Checkpoint : MonoBehaviour
    {
        [Tooltip("Section id recorded with this checkpoint and used as the respawn section (0 = start).")]
        [SerializeField] private int sectionId;

        [Tooltip("Grey-box flash tint applied to this object's renderer while hit (UNSCALED).")]
        [SerializeField] private Color flashTint = new Color(0.4f, 1f, 0.55f);

        [Tooltip("Flash duration in seconds (UNSCALED).")]
        [SerializeField] private float flashDuration = 0.25f;

        private Renderer cachedRenderer;
        private Color originalColor;
        private Coroutine flashRoutine;

        /// <summary>Section id this checkpoint records (read-only debug seam).</summary>
        public int SectionId => sectionId;

        private void Awake()
        {
            ResolveRenderer();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other == null) return;
            var system = other.GetComponentInParent<CheckpointSystem>();
            if (system == null) return; // only the player carries the respawn owner

            // Record the PLAYER's live pose (position + facing), tagged with this section.
            // Facing comes from the visual's facing root, not the player root — the root
            // never rotates (the rig turns ArtRoot), so root.forward would be stale.
            Vector3 facing = system.transform.forward;
            var visual = system.GetComponent<AnimatorVisual>();
            if (visual != null) facing = visual.Facing;

            system.RecordCheckpoint(system.transform.position, facing, sectionId);
            Flash();
        }

        private void ResolveRenderer()
        {
            if (cachedRenderer != null) return;
            cachedRenderer = GetComponentInChildren<Renderer>();
            if (cachedRenderer != null) originalColor = cachedRenderer.material.color;
        }

        private void Flash()
        {
            ResolveRenderer();
            if (cachedRenderer == null) return;
            if (flashRoutine != null) StopCoroutine(flashRoutine);
            flashRoutine = StartCoroutine(FlashRoutine());
        }

        private IEnumerator FlashRoutine()
        {
            var material = cachedRenderer.material; // instance — never clobber shared mats
            material.color = flashTint;
            yield return new WaitForSecondsRealtime(Mathf.Max(0f, flashDuration));
            material.color = originalColor;
            flashRoutine = null;
        }
    }
}
