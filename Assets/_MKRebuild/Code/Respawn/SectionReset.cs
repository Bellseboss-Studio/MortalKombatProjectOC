using System.Collections.Generic;
using UnityEngine;

namespace MK.Rebuild
{
    /// <summary>
    /// m2-respawn slice 2: the RUNTIME section reset. Restores the current section to its
    /// initial state on death (or on any explicit caller): scene-placed <see cref="HitReceiver"/>
    /// dummies are re-posed at full HP, destroyed ones are re-spawned, debug-spawned extras are
    /// removed, every cached <see cref="IResettable"/> is restored, and each cached
    /// <see cref="EnemySpawner"/> resets its encounter (M3).
    ///
    /// This is a straight PROMOTION of the Debug-only logic that used to live in
    /// <c>MK.Rebuild.Debugging.FinishResetZone</c> (snapshot/restore moved, not changed) — it now
    /// lives in the Runtime assembly so the respawn path never references Debug. The Debug
    /// <c>FinishResetZone</c> keeps only its trigger + teleport and delegates here.
    ///
    /// Snapshot: at <c>Awake</c> every scene-placed <see cref="HitReceiver"/> is cloned as an
    /// INACTIVE template parented under this object (no runtime spawns exist at scene load, so
    /// the snapshot IS the scene dummy set). Restore: alive dummies are re-posed + toggled
    /// off/on (<c>OnEnable</c> restores full HP); destroyed ones (a kill <c>Destroy</c>s the
    /// object) are re-instantiated from their template; extras with no template are removed.
    ///
    /// No teleport here — that belongs to <see cref="CheckpointSystem"/> (death respawn) or the
    /// Debug <c>FinishResetZone</c> (track finish).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SectionReset : MonoBehaviour
    {
        private readonly List<GameObject> templates = new();
        private readonly List<HitReceiver> sceneSet = new();
        private readonly List<IResettable> resettables = new();
        private readonly List<EnemySpawner> enemySpawners = new();

        private void Awake()
        {
            SnapshotDummies();
            CacheResettables();
            CacheEnemySpawners();
        }

        /// <summary>
        /// Caches the scene encounter spawners once at Awake. Their enemies spawn in
        /// Start, so this is the authored set; <see cref="ResetEnemies"/> restores
        /// each encounter through its own owner.
        /// </summary>
        private void CacheEnemySpawners()
        {
            enemySpawners.Clear();
            var found = FindObjectsByType<EnemySpawner>(FindObjectsSortMode.None);
            for (int i = 0; i < found.Length; i++)
            {
                if (found[i] != null) enemySpawners.Add(found[i]);
            }
        }

        /// <summary>
        /// Clones every scene-placed <see cref="HitReceiver"/> as an inactive template under
        /// this object. Runs once at scene load, before any runtime spawn exists, so the set
        /// captured here is the authored (S7) dummy set.
        /// </summary>
        private void SnapshotDummies()
        {
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

        /// <summary>
        /// Caches every scene <see cref="IResettable"/> once at Awake (inactive templates are
        /// skipped by the default find). Future runtime-spawned participants should be restored
        /// by their own owner — this seam is for authored scene state.
        /// </summary>
        private void CacheResettables()
        {
            resettables.Clear();
            var behaviours = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);
            for (int i = 0; i < behaviours.Length; i++)
            {
                if (behaviours[i] is IResettable resettable) resettables.Add(resettable);
            }
        }

        /// <summary>
        /// Restores the section: dummies first, then cached <see cref="IResettable"/>s, then the
        /// (still-empty) enemy hook. Safe to call repeatedly. Does NOT move the player.
        /// </summary>
        public void ResetSection()
        {
            ResetDummies();
            ResetResettables();
            ResetEnemies();
        }

        private void ResetResettables()
        {
            for (int i = 0; i < resettables.Count; i++)
            {
                var resettable = resettables[i];
                if (resettable != null) resettable.ResetToInitial();
            }
        }

        /// <summary>
        /// M3: restores every cached encounter through its <see cref="EnemySpawner"/>
        /// (destroy the spawned wave + spawn the initial one). Runs after the dummy
        /// reset so a respawned wave is not mistaken for a debug extra. Safe when the
        /// encounter list is empty.
        /// </summary>
        public void ResetEnemies()
        {
            for (int i = 0; i < enemySpawners.Count; i++)
            {
                var spawner = enemySpawners[i];
                if (spawner != null) spawner.ResetEncounter();
            }
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
                // Enemies are owned by their spawner (ResetEnemies), not by the dummy cleanup.
                if (receiver.GetComponent<EnemyController>() != null) continue;
                if (!receiver.gameObject.activeInHierarchy) continue; // our inactive templates
                Destroy(receiver.gameObject);
            }
        }
    }
}
