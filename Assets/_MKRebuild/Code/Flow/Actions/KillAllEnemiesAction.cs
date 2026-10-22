using UnityEngine;

namespace MK.Rebuild
{
    /// <summary>
    /// Example cinematic action: wipes every live enemy when the event fires.
    /// It routes each kill through the enemy's own <see cref="HitReceiver"/> so the
    /// normal death flow runs (controller death visual/destroy, style kill bonus,
    /// tokens released) instead of deleting objects behind the gameplay layer's back.
    /// Dead or missing receivers are skipped, so firing twice is harmless.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class KillAllEnemiesAction : MonoBehaviour, ICinematicAction
    {
        public void Execute(CinematicContext context)
        {
            EnemyController[] enemies = FindObjectsByType<EnemyController>(
                FindObjectsInactive.Exclude, FindObjectsSortMode.None);

            for (int i = 0; i < enemies.Length; i++)
            {
                EnemyController enemy = enemies[i];
                if (enemy == null) continue;

                HitReceiver receiver = enemy.GetComponent<HitReceiver>();
                if (receiver == null || !receiver.IsAlive) continue;

                receiver.TakeHit(999999, Vector3.zero);
            }
        }
    }
}
