using UnityEngine;

namespace MK.Rebuild
{
    /// <summary>
    /// H1 grey-box projectile (m1-abilities, 2026-10-15). A primitive sphere
    /// (no Rigidbody — transform-driven, same marker approach the combat probe
    /// uses) that travels STRAIGHT along its spawn facing at a constant speed,
    /// damages the FIRST <see cref="HitReceiver"/> it overlaps ONCE (with the
    /// shared hitstop), then destroys itself on hit or when its lifetime expires.
    /// Spawned by <see cref="PlayerAbilities"/> from the <see cref="AbilityDefinition"/>
    /// data (speed / lifetime / radius / muzzle / dano). Frozen by hitstop
    /// (scaled time).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class Projectile : MonoBehaviour
    {
        private Vector3 direction;
        private float speed;
        private float lifetime;
        private float radius;
        private int damage;
        private float knockback;
        private int hitstopMs;
        private Rigidbody ownerBody;
        private PlayerCombat ownerCombat;
        private bool consumed;
        private float age;

        /// <summary>
        /// Configures the projectile. <paramref name="dir"/> is the world facing,
        /// <paramref name="owner"/> the shooter's Rigidbody (excluded from the overlap
        /// so the projectile never hits its own owner).
        /// </summary>
        public void Launch(Vector3 dir, float speed, float lifetime, float radius,
                           int damage, float knockback, int hitstopMs,
                           Rigidbody owner, PlayerCombat combat)
        {
            this.direction = dir.sqrMagnitude > 0.0001f ? dir.normalized : Vector3.forward;
            this.speed = Mathf.Max(0f, speed);
            this.lifetime = Mathf.Max(0.01f, lifetime);
            this.radius = Mathf.Max(0.02f, radius);
            this.damage = damage;
            this.knockback = Mathf.Max(0f, knockback);
            this.hitstopMs = hitstopMs;
            ownerBody = owner;
            ownerCombat = combat;
            age = 0f;
            consumed = false;
        }

        private void Update()
        {
            if (consumed) return;

            float dt = Time.deltaTime; // scaled: hitstop (timeScale = 0) freezes it
            age += dt;
            if (age >= lifetime) { Destroy(gameObject); return; }

            transform.position += direction * speed * dt;

            Collider[] cols = Physics.OverlapSphere(
                transform.position, radius, Physics.AllLayers, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < cols.Length; i++)
            {
                var col = cols[i];
                if (col == null) continue;
                if (ownerBody != null && col.attachedRigidbody == ownerBody) continue; // never hit the shooter

                var receiver = col.GetComponentInParent<HitReceiver>();
                if (receiver == null) continue;

                Vector3 to = receiver.transform.position - transform.position;
                to.y = 0f;
                if (to.sqrMagnitude < 0.0001f) to = direction;
                to.Normalize();

                receiver.TakeHit(damage, to * knockback, 0f); // FIRST target only (single hit)
                if (ownerCombat != null) ownerCombat.RequestHitstop(hitstopMs);
                GreyBoxFx.SpawnBurst(transform.position, 1f);

                consumed = true;
                Destroy(gameObject);
                return;
            }
        }
    }
}
