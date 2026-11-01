using System.Collections.Generic;
using UnityEngine;

namespace MK.Rebuild
{
    /// <summary>
    /// Tarkatana's ROAR on the <c>Block</c> button (LB). In Mantis that button raises the guard;
    /// in Tarkatana it was dead, so this component gives it the stance's own verb: HOLD to charge
    /// a ring (the charge meter AND the range preview), RELEASE at full charge to stagger every
    /// enemy inside the radius.
    ///
    /// WHY the ring: there is no HUD by design. The ring grows with the charge using
    /// <see cref="GreyBoxFx"/> (placeholder grey-box VFX), so the player reads both "how much is
    /// left" and "who will be caught" from the same object.
    ///
    /// GATING CONTRACT: without a <see cref="PlayerStance"/> component (the v1 player) this
    /// component does NOTHING — it does not even read the Block input. It only runs in Tarkatana;
    /// switching stance mid-charge cancels the charge without roaring.
    ///
    /// The stagger uses <see cref="EnemyController.Stagger(float)"/>, the same explicit-duration
    /// seam the disarm uses, so the roar window also holds against poise re-staggers.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PlayerRoar : MonoBehaviour
    {
        [Tooltip("Roar radius in meters: every enemy inside is staggered, and the ring previews it.")]
        [SerializeField] private float roarRadius = 4.5f;

        [Tooltip("Seconds Block must be HELD to reach a full charge. Releasing early is a whiff.")]
        [SerializeField] private float chargeSeconds = 0.6f;

        [Tooltip("Seconds enemies reached by a full roar stay open (explicit-duration stagger).")]
        [SerializeField] private float staggerDuration = 1.2f;

        [Tooltip("Seconds before the roar can be charged again.")]
        [SerializeField] private float cooldown = 3f;

        [Tooltip("Seconds the ring stays visible after a full roar before it is destroyed.")]
        [SerializeField] private float ringSeconds = 0.45f;

        [Tooltip("Scale passed to GreyBoxFx.SpawnBurst for the roar pop.")]
        [SerializeField] private float burstScale = 1.5f;

        private PlayerStance stance;
        private PlayerInput input;

        private float cooldownTimer;
        private bool charging;
        private float chargeTimer;
        private GameObject ring;
        private float ringLifeTimer;

        private readonly HashSet<EnemyController> staggeredEnemies = new HashSet<EnemyController>();

        private void Awake()
        {
            stance = GetComponent<PlayerStance>();
        }

        private void OnEnable()
        {
            if (input == null) input = new PlayerInput();
            input.Player.Enable();
        }

        private void OnDisable()
        {
            if (input != null) input.Player.Disable();

            // Never leave a half-grown ring in the world when the component is disabled.
            CancelCharge();
        }

        private void OnDestroy()
        {
            CancelCharge();
            if (input != null) input.Dispose();
        }

        private void Update()
        {
            if (ControlLock.IsPaused) return;

            TickRingLifetime();

            // GATING CONTRACT: no PlayerStance (v1 player) = no roar, no input read.
            if (stance == null) return;

            // Mantis owns Block for its guard. A stance switch mid-charge drops the charge.
            if (!stance.IsTarkatana)
            {
                CancelCharge();
                return;
            }

            if (input == null) return;

            if (cooldownTimer > 0f) cooldownTimer = Mathf.Max(0f, cooldownTimer - Time.deltaTime);

            if (!charging)
            {
                if (cooldownTimer <= 0f && input.Player.Block.WasPressedThisFrame()) BeginCharge();
                return;
            }

            chargeTimer += Time.deltaTime;
            float progress = ChargeProgress;

            // The ring is the charge meter AND the range preview.
            if (ring != null) GreyBoxFx.ApplyRingSize(ring.transform, Mathf.Max(0.01f, roarRadius * progress));

            if (!input.Player.Block.IsPressed()) ReleaseCharge(progress >= 1f);
        }

        /// <summary>Normalized charge (0..1). 1 = a full roar on release.</summary>
        private float ChargeProgress => chargeSeconds > 0f ? Mathf.Clamp01(chargeTimer / chargeSeconds) : 1f;

        private void BeginCharge()
        {
            charging = true;
            chargeTimer = 0f;
            ringLifeTimer = 0f;

            // Radius 0.01 is just a seed; ApplyRingSize grows it with the charge.
            ring = GreyBoxFx.CreateRing(transform, 0.01f);
        }

        /// <summary>
        /// Resolves the held button. A full charge roars; an early release is a whiff that still
        /// pays a short cooldown and removes the ring.
        /// </summary>
        private void ReleaseCharge(bool fullyCharged)
        {
            charging = false;
            chargeTimer = 0f;

            if (!fullyCharged)
            {
                DestroyRing();
                cooldownTimer = Mathf.Max(0f, cooldown) * 0.33f;
                Debug.Log($"[{nameof(PlayerRoar)}] Whiff — released before full charge.", this);
                return;
            }

            int count = StaggerEnemiesInRadius();
            GreyBoxFx.SpawnBurst(transform.position, burstScale);

            if (ring != null)
            {
                GreyBoxFx.ApplyRingSize(ring.transform, roarRadius);
                ringLifeTimer = Mathf.Max(0.01f, ringSeconds);
            }

            cooldownTimer = Mathf.Max(0f, cooldown);
            Debug.Log($"[{nameof(PlayerRoar)}] ROAR — {count} enemies staggered.", this);
        }

        /// <summary>
        /// Staggers every enemy inside <see cref="roarRadius"/> once. Dedupes by controller because
        /// one enemy contributes several colliders (body + hurt triggers).
        /// </summary>
        private int StaggerEnemiesInRadius()
        {
            staggeredEnemies.Clear();

            Collider[] colliders = Physics.OverlapSphere(
                transform.position, Mathf.Max(0.01f, roarRadius), Physics.AllLayers, QueryTriggerInteraction.Ignore);

            for (int i = 0; i < colliders.Length; i++)
            {
                Collider collider = colliders[i];
                if (collider == null) continue;

                EnemyController enemy = collider.GetComponentInParent<EnemyController>();
                if (enemy == null) continue;
                if (!staggeredEnemies.Add(enemy)) continue;

                enemy.Stagger(staggerDuration);
            }

            return staggeredEnemies.Count;
        }

        private void TickRingLifetime()
        {
            if (ringLifeTimer <= 0f) return;

            ringLifeTimer -= Time.deltaTime;
            if (ringLifeTimer > 0f) return;

            DestroyRing();
        }

        /// <summary>Drops the charge (stance switch / disable): no roar, ring removed.</summary>
        private void CancelCharge()
        {
            charging = false;
            chargeTimer = 0f;
            DestroyRing();
        }

        private void DestroyRing()
        {
            if (ring != null) Destroy(ring);
            ring = null;
            ringLifeTimer = 0f;
        }
    }
}
