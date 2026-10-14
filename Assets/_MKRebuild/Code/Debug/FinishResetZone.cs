using System.Collections.Generic;
using UnityEngine;

namespace MK.Rebuild.Debugging
{
    /// <summary>
    /// M0 track-only finish reset for <c>Playground.unity</c>: a
    /// <see cref="PlayerMotor"/> body touching this trigger (the S8 arch) is
    /// teleported back to the S0 track start AND the scene-placed dummies
    /// (S7 set) are restored — full HP at their initial poses — so the track
    /// is replayable. FULL reset: no checkpoints, no menus, no fades.
    ///
    /// Track-only: lives in the isolated <c>MK.Rebuild.Debug</c> assembly and
    /// never touches game assemblies' behavior. It only writes the player's
    /// Rigidbody pose/velocity (same seam as <c>SafetyResetZone</c>) and
    /// re-poses / re-spawns scene dummies.
    ///
    /// Dummy restore: at <c>Awake</c> every scene-placed <c>HitReceiver</c> is
    /// snapshotted as an INACTIVE template clone parented under this zone (no
    /// runtime spawns exist yet at scene load, so the snapshot IS the S7 set).
    /// On reset, alive dummies are re-posed + toggled off/on (<c>OnEnable</c>
    /// restores full HP); destroyed ones (kills <c>Destroy</c> the object) are
    /// re-instantiated from their template. Debug-spawned extras
    /// (<c>DebugDummy_*</c>) have no initial pose and are REMOVED.
    ///
    /// Wiring: one trigger box at the S8 arch (x ~78, covering the passage).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class FinishResetZone : MonoBehaviour
    {
        [Tooltip("S0 track-start pose (Rigidbody center, feet on the slab top).")]
        [SerializeField] private Vector3 respawnPosition = new Vector3(0f, 1f, 0f);

        private readonly List<GameObject> templates = new();
        private readonly List<HitReceiver> sceneSet = new();

        private void Awake()
        {
            // Scene load: no runtime spawns exist yet, so every HitReceiver
            // found IS a scene-placed (S7) dummy.
            var receivers = FindObjectsByType<HitReceiver>(FindObjectsSortMode.None);
            foreach (var receiver in receivers)
            {
                if (receiver == null) continue;
                var template = Instantiate(
                    receiver.gameObject,
                    receiver.transform.position,
                    receiver.transform.rotation);
                template.name = "Template_" + receiver.gameObject.name;
                template.SetActive(false);
                template.transform.SetParent(transform, worldPositionStays: true);
                templates.Add(template);
                sceneSet.Add(receiver);
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other == null) return;
            var motor = other.GetComponentInParent<PlayerMotor>();
            if (motor == null) return;
            var rb = motor.GetComponent<Rigidbody>();
            if (rb == null) return;

            rb.position = respawnPosition;
            rb.linearVelocity = Vector3.zero;

            ResetDummies();
        }

        private void ResetDummies()
        {
            for (int i = 0; i < templates.Count; i++)
            {
                var template = templates[i];
                if (template == null) continue;
                var current = i < sceneSet.Count ? sceneSet[i] : null;
                // Dead = destroyed OR with 0 HP (a kill's Destroy is deferred to
                // end-of-frame, so a same-frame reset can catch it still pending).
                bool dead = current == null || current.HP <= 0;
                if (dead)
                {
                    // Killed — re-spawn from the template at the initial pose.
                    if (current != null) Destroy(current.gameObject);
                    var fresh = Instantiate(
                        template, template.transform.position, template.transform.rotation);
                    fresh.transform.SetParent(null, worldPositionStays: true);
                    fresh.name = template.name.Substring("Template_".Length);
                    fresh.SetActive(true);
                    var receiver = fresh.GetComponent<HitReceiver>();
                    if (i < sceneSet.Count) sceneSet[i] = receiver;
                    else sceneSet.Add(receiver);
                }
                else
                {
                    // Alive (maybe knocked away / damaged) — re-pose + toggle
                    // off/on so OnEnable restores full HP.
                    var go = current.gameObject;
                    go.SetActive(false);
                    current.transform.SetPositionAndRotation(
                        template.transform.position, template.transform.rotation);
                    go.SetActive(true);
                }
            }

            // FULL reset: remove debug-spawned extras (no initial pose to restore).
            var all = FindObjectsByType<HitReceiver>(FindObjectsSortMode.None);
            for (int i = 0; i < all.Length; i++)
            {
                var receiver = all[i];
                if (receiver == null) continue;
                if (sceneSet.Contains(receiver)) continue;
                if (!receiver.gameObject.activeInHierarchy) continue; // our inactive templates
                Destroy(receiver.gameObject);
            }
        }
    }
}
