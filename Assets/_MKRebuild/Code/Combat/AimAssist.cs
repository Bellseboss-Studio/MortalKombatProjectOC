using UnityEngine;

namespace MK.Rebuild
{
    /// <summary>
    /// M1 slice-3quinquies shared trajectory-corrector SELECTION (2026-10-13).
    /// Extracted 1:1 from <see cref="PlayerCombat"/> so the SAME cone + range +
    /// nearest-alive-target math serves both the attack entry AND the dash
    /// direction — one source of truth, no new system.
    ///
    /// Pure query: given a query axis (the current facing / intended direction),
    /// it returns the normalized XZ direction toward the nearest alive
    /// <see cref="HitReceiver"/> inside the frontal cone and [minRange, maxRange]
    /// METERS. It does NOT rotate anything — the caller owns the presentation
    /// (SetFacing / correction window).
    /// </summary>
    public static class AimAssist
    {
        /// <summary>
        /// Finds the nearest alive target inside the frontal cone + range, as a
        /// normalized XZ direction. Returns false (and <paramref name="aimDir"/>
        /// = zero) when there is no definition, it is disabled, or no target
        /// qualifies — the caller then keeps its forward-only behavior.
        /// </summary>
        /// <param name="selfPosition">Query origin (usually <c>transform.position</c>).</param>
        /// <param name="selfRigidbody">Own body to skip (own capsule / Face trigger share it).</param>
        /// <param name="coneAxis">Cone center: the current facing / intended direction (XZ used).</param>
        /// <param name="definition">Tuning (null or <c>enabled=false</c> = no selection).</param>
        /// <param name="aimDir">Normalized XZ direction toward the chosen target.</param>
        public static bool TryFindAimDir(
            Vector3 selfPosition,
            Rigidbody selfRigidbody,
            Vector3 coneAxis,
            AimAssistDefinition definition,
            out Vector3 aimDir)
        {
            aimDir = Vector3.zero;
            if (definition == null || !definition.enabled) return false;

            Vector3 facing = coneAxis;
            facing.y = 0f;
            if (facing.sqrMagnitude < 0.0001f) facing = Vector3.forward;
            facing.Normalize();

            float range = Mathf.Max(0.01f, definition.maxRangeMeters);
            Collider[] cols = Physics.OverlapSphere(
                selfPosition, range, Physics.AllLayers, QueryTriggerInteraction.Ignore);

            float cosLimit = Mathf.Cos(Mathf.Clamp(definition.coneHalfAngleDeg, 0f, 180f) * Mathf.Deg2Rad);
            float minRange = Mathf.Max(0f, definition.minRangeMeters);
            float minSqr = minRange * minRange;

            HitReceiver best = null;
            float bestSqr = float.MaxValue;
            for (int i = 0; i < cols.Length; i++)
            {
                var col = cols[i];
                if (col == null) continue;
                if (col.attachedRigidbody == selfRigidbody) continue; // own capsule / Face trigger

                var receiver = col.GetComponentInParent<HitReceiver>();
                if (receiver == null || !receiver.IsAlive) continue;

                Vector3 to = receiver.transform.position - selfPosition;
                to.y = 0f;
                float sqr = to.sqrMagnitude;
                if (sqr < 0.0001f || sqr < minSqr) continue; // no direction / dead-zone
                if (sqr >= bestSqr) continue;                // not nearer than the best

                Vector3 dir = to / Mathf.Sqrt(sqr);
                if (Vector3.Dot(facing, dir) < cosLimit) continue; // outside the frontal cone

                bestSqr = sqr;
                best = receiver;
            }

            if (best == null) return false;

            Vector3 aim = best.transform.position - selfPosition;
            aim.y = 0f;
            if (aim.sqrMagnitude < 0.0001f) return false;
            aimDir = aim.normalized;
            return true;
        }
    }
}
