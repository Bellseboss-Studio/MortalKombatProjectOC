using UnityEngine;

namespace MK.Rebuild
{
    /// <summary>
    /// M3.3 grey-box enemy projectile: transform-driven (NO Rigidbody, NO
    /// collider on the root), flies straight, and resolves its hit by an
    /// <see cref="Physics.OverlapSphere"/> each frame. Enemy colliders are
    /// ignored so a projectile never hits its own crowd.
    ///
    /// <see cref="ActiveCount"/> is a global in-flight counter so ranged
    /// archetypes can respect their projectile cap without owning a pool.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class EnemyProjectile : MonoBehaviour
    {
        /// <summary>Live projectile instances across the whole scene (read-only seam).</summary>
        public static int ActiveCount { get; private set; }

        private Vector3 direction = Vector3.forward;
        private float speed;
        private float lifetime;
        private float radius;
        private int damage;
        private float age;
        private bool counted;

        private void OnEnable()
        {
            if (counted) return;
            counted = true;
            ActiveCount++;
        }

        private void OnDisable()
        {
            DecrementCount();
        }

        private void OnDestroy()
        {
            DecrementCount();
        }

        /// <summary>
        /// Arms the projectile. <paramref name="dir"/> is flattened; the shot
        /// keeps its launch height and travels in a straight horizontal line.
        /// </summary>
        public void Launch(Vector3 dir, float newSpeed, float newLifetime, float newRadius, int newDamage)
        {
            direction = dir;
            direction.y = 0f;
            if (direction.sqrMagnitude < 0.000001f) direction = transform.forward;
            direction.Normalize();

            speed = Mathf.Max(0f, newSpeed);
            lifetime = Mathf.Max(0f, newLifetime);
            radius = Mathf.Max(0.01f, newRadius);
            damage = Mathf.Max(0, newDamage);
            age = 0f;
        }

        private void Update()
        {
            age += Time.deltaTime;
            if (age >= lifetime)
            {
                Destroy(gameObject);
                return;
            }

            transform.position += direction * (speed * Time.deltaTime);

            Collider[] hits = Physics.OverlapSphere(transform.position, radius);
            for (int i = 0; i < hits.Length; i++)
            {
                Collider hit = hits[i];
                if (hit == null) continue;

                // Never collide with the enemy crowd (including the shooter).
                if (hit.GetComponentInParent<EnemyController>() != null) continue;

                PlayerDamage target = hit.GetComponentInParent<PlayerDamage>();
                if (target == null || !target.IsAlive) continue;

                target.TakeHit(damage, direction);
                Destroy(gameObject);
                return;
            }
        }

        private void DecrementCount()
        {
            if (!counted) return;
            counted = false;
            ActiveCount = Mathf.Max(0, ActiveCount - 1);
        }
    }
}
