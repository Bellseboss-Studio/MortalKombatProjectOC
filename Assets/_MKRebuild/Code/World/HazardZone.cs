using System.Collections.Generic;
using UnityEngine;

namespace MK.Rebuild
{
    /// <summary>
    /// m2-hazards: the scene-side hazard. ONE component reads ONE
    /// <see cref="HazardDefinition"/> and enforces its consequence — there are no
    /// per-consequence hazard classes.
    ///
    /// Consequences:
    /// <list type="bullet">
    /// <item><see cref="HazardConsequence.Damage"/> (wall, S3): on contact it deals the
    /// authored damage with the CONTACT NORMAL as the push direction (away from the wall),
    /// then opens the authored i-frame window through the existing
    /// <see cref="PlayerDamage.GrantInvulnerability"/> knob. The zone's own cooldown gate
    /// mirrors that window so continuous contact (sliding along the wall) cannot re-damage
    /// inside it, even if another consumer ignores invulnerability.</item>
    /// <item><see cref="HazardConsequence.ReturnToEdge"/> (soft pit, S4): ON ENTER it teleports
    /// the player to the authored <c>safePoint</c> with NO damage, clearing any in-flight
    /// knockback first. The safe point must sit OUTSIDE the trigger volume (authoring), so the
    /// return cannot re-trigger in a loop; a small cooldown is kept as belt-and-braces.</item>
    /// <item><see cref="HazardConsequence.KillAndRespawn"/> (lethal pit, S4): a TALL trigger
    /// volume tracks overlap and a per-<see cref="FixedUpdate"/> Y test kills when an
    /// overlapping, living player's root reaches <c>definition.killY</c>. The Y THRESHOLD is
    /// the gate — NEVER a thin bottom trigger collider (a frictionless capsule at
    /// <c>maxFallSpeed</c> would tunnel it). <see cref="PlayerDamage.Kill"/> bypasses
    /// invulnerability, so wall/respawn i-frames can never block a lethal pit.</item>
    /// </list>
    ///
    /// <see cref="IResettable"/>: <see cref="SectionReset"/> caches every scene resettable, so
    /// a fired/cooldown hazard returns to its initial state when the section resets.
    ///
    /// The trigger collider must live on THIS GameObject (that is where Unity delivers
    /// OnTrigger/Exit and where the kill volume is read); the player's Rigidbody provides the
    /// other side of the pair.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class HazardZone : MonoBehaviour, IResettable
    {
        [Tooltip("The data this zone enforces. Null = inert (no consequence).")]
        [SerializeField] private HazardDefinition definition;

        [Tooltip("ReturnToEdge ONLY: scene point the player is returned to (must sit OUTSIDE this trigger volume). Unused by Damage/KillAndRespawn.")]
        [SerializeField] private Transform safePoint;

        /// <summary>Belt-and-braces re-entry gate for ReturnToEdge (seconds).</summary>
        private const float ReturnCooldown = 0.25f;

        private float nextDamageTime; // zone-side cooldown, mirrors definition.iframeSeconds
        private float nextReturnTime; // ReturnToEdge re-entry gate

        // KillAndRespawn overlap tracking. The player has TWO colliders (root capsule + Face
        // trigger), so the count is kept per player and the player only drops out when EVERY
        // overlapping collider has exited the volume.
        private readonly List<PlayerDamage> pitOccupants = new();
        private readonly List<int> pitOverlapCounts = new();

        /// <summary>The definition this zone enforces (read-only debug seam).</summary>
        public HazardDefinition Definition => definition;

        /// <summary>True while the zone's damage cooldown gate is closed (read-only debug seam).</summary>
        public bool IsCoolingDown => Time.time < nextDamageTime;

        /// <summary>Number of distinct players currently tracked inside a kill volume (debug seam).</summary>
        public int PitOccupantCount => pitOccupants.Count;

        private void Awake()
        {
            WarnIfSafePointInsideVolume();
        }

        private void OnTriggerEnter(Collider other)
        {
            TryApply(other, isEnter: true);
        }

        private void OnTriggerStay(Collider other)
        {
            // Stay (not only Enter) so a player who is pushed back INTO a wall is handled, and
            // so a soft/kill pit keeps a stable occupancy while a fast fall passes through.
            TryApply(other, isEnter: false);
        }

        private void OnTriggerExit(Collider other)
        {
            if (definition == null || other == null) return;
            if (definition.consequence != HazardConsequence.KillAndRespawn) return;

            PlayerDamage player = other.GetComponentInParent<PlayerDamage>();
            if (player != null) RemovePitOccupant(player);
        }

        private void FixedUpdate()
        {
            // The kill plane is evaluated on the physics step: it reads the simulated root Y,
            // so a fast fall cannot skip the threshold the way a thin bottom trigger can.
            if (definition == null || definition.consequence != HazardConsequence.KillAndRespawn)
                return;

            for (int i = pitOccupants.Count - 1; i >= 0; i--)
            {
                PlayerDamage player = pitOccupants[i];
                if (player == null)
                {
                    RemovePitOccupantAt(i);
                    continue;
                }
                TryKill(player);
            }
        }

        private void TryApply(Collider other, bool isEnter)
        {
            if (definition == null || other == null) return;
            PlayerDamage player = other.GetComponentInParent<PlayerDamage>();
            if (player == null) return;

            switch (definition.consequence)
            {
                case HazardConsequence.Damage:
                    ApplyDamage(player);
                    break;

                case HazardConsequence.ReturnToEdge:
                    // Enter only: a teleport into the safe point must never be re-applied by
                    // Stay while the physics pair settles.
                    if (isEnter) ReturnToEdge(player);
                    break;

                case HazardConsequence.KillAndRespawn:
                    if (isEnter) AddPitOccupant(player);
                    else EnsurePitOccupant(player);
                    TryKill(player);
                    break;
            }
        }

        /// <summary>
        /// Soft pit: return the player to the authored edge point, NO damage, NO death.
        /// Clears any in-flight shove first so a knockback cannot carry the player back into
        /// the zone after the teleport. <see cref="PlayerMotor.Teleport"/> clears the rest of
        /// the motion state (velocity, wall/ledge latches, mantle warp).
        /// </summary>
        private void ReturnToEdge(PlayerDamage player)
        {
            if (player == null || !player.IsAlive) return;
            if (Time.time < nextReturnTime) return;

            if (safePoint == null)
            {
                Debug.LogWarning(
                    $"[HazardZone] '{definition.id}' is ReturnToEdge but has no safePoint; ignored.",
                    this);
                return;
            }

            PlayerMotor motor = player.GetComponent<PlayerMotor>();
            if (motor == null) return;

            PlayerKnockback knockback = player.GetComponent<PlayerKnockback>();
            if (knockback != null) knockback.Cancel();

            nextReturnTime = Time.time + ReturnCooldown;
            motor.Teleport(safePoint.position, safePoint.forward);
        }

        /// <summary>
        /// Kill plane: the Y threshold is the gate (no damage, no i-frame checks).
        /// <see cref="PlayerDamage.Kill"/> is deliberately ungated by invulnerability.
        /// </summary>
        private void TryKill(PlayerDamage player)
        {
            if (player == null || !player.IsAlive) return;
            if (player.transform.position.y > definition.killY) return;
            player.Kill();
        }

        private void AddPitOccupant(PlayerDamage player)
        {
            int i = pitOccupants.IndexOf(player);
            if (i < 0)
            {
                pitOccupants.Add(player);
                pitOverlapCounts.Add(1);
            }
            else
            {
                pitOverlapCounts[i]++;
            }
        }

        /// <summary>Stay path: keep an existing entrant (no double count), recover a missed Enter.</summary>
        private void EnsurePitOccupant(PlayerDamage player)
        {
            if (!pitOccupants.Contains(player))
            {
                pitOccupants.Add(player);
                pitOverlapCounts.Add(1);
            }
        }

        private void RemovePitOccupant(PlayerDamage player)
        {
            int i = pitOccupants.IndexOf(player);
            if (i < 0) return;
            pitOverlapCounts[i]--;
            if (pitOverlapCounts[i] <= 0) RemovePitOccupantAt(i);
        }

        private void RemovePitOccupantAt(int index)
        {
            pitOccupants.RemoveAt(index);
            pitOverlapCounts.RemoveAt(index);
        }

        /// <summary>
        /// Wall consequence: authored damage + shove away from the wall + one shared i-frame
        /// window. The body lock itself is derived downstream by PlayerKnockback from the
        /// hit-reaction animation, so nothing about the lock is authored here.
        /// </summary>
        private void ApplyDamage(PlayerDamage player)
        {
            if (!player.IsAlive || player.IsInvulnerable) return;
            if (Time.time < nextDamageTime) return;

            Vector3 pushDir = ContactNormal(player.transform.position);
            if (definition.cancelVerticalOnHit) player.CancelAirMomentum();
            player.TakeHit(
                definition.damage,
                pushDir,
                definition.knockbackDistance,
                definition.knockbackDuration);

            // ONE invulnerability knob (design open question #2): a wall graze also buys
            // immunity to other Damage sources for the same window. Kill() stays ungated.
            player.GrantInvulnerability(definition.iframeSeconds);
            nextDamageTime = Time.time + Mathf.Max(0f, definition.iframeSeconds);
        }

        /// <summary>
        /// Contact normal = outward from the wall surface toward the player. Uses the zone's
        /// own collider (<see cref="Collider.ClosestPoint"/>) so a wide wall pushes along the
        /// face the player actually touched, not along a center-to-player diagonal. Y is
        /// flattened: the shove is horizontal.
        /// </summary>
        private Vector3 ContactNormal(Vector3 playerPosition)
        {
            Collider self = GetComponent<Collider>();
            Vector3 surface = self != null ? self.ClosestPoint(playerPosition) : transform.position;
            Vector3 dir = playerPosition - surface;
            dir.y = 0f;
            if (dir.sqrMagnitude < 0.000001f)
            {
                dir = playerPosition - transform.position;
                dir.y = 0f;
            }
            if (dir.sqrMagnitude < 0.000001f) dir = transform.forward;
            return dir.normalized;
        }

        /// <summary>
        /// Authoring guard: a ReturnToEdge safe point inside the zone's own volume is the one
        /// way to get a re-trigger loop, so surface it once at load (the fix is moving the
        /// point, not changing code).
        /// </summary>
        private void WarnIfSafePointInsideVolume()
        {
            if (definition == null || safePoint == null) return;
            if (definition.consequence != HazardConsequence.ReturnToEdge) return;

            Collider self = GetComponent<Collider>();
            if (self != null && self.bounds.Contains(safePoint.position))
            {
                Debug.LogWarning(
                    $"[HazardZone] '{definition.id}' safePoint sits INSIDE its trigger volume; " +
                    "the player will be returned into the zone.", this);
            }
        }

        /// <summary>Clears this zone's fired/cooldown/occupancy state (section reset).</summary>
        public void ResetToInitial()
        {
            nextDamageTime = 0f;
            nextReturnTime = 0f;
            pitOccupants.Clear();
            pitOverlapCounts.Clear();
        }
    }
}
