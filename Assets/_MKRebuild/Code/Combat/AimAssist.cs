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

            float unusedDistance;
            return TryFindAimTarget(
                selfPosition, selfRigidbody, coneAxis, definition,
                definition.maxRangeMeters, out aimDir, out unusedDistance);
        }

        /// <summary>
        /// Same selection with an EXPLICIT search range, plus the distance to the chosen target.
        ///
        /// WHY this overload exists: the lunge must reach an enemy that is inside the cone no
        /// matter how far away it is, so it queries with a much larger range than the definition's
        /// and then stretches the dash to cover the measured distance. The cone math stays here —
        /// there is still one source of truth for "nearest target in the cone".
        /// </summary>
        /// <param name="rangeMeters">Search radius override (the definition's range is ignored).</param>
        /// <param name="distance">XZ distance from the origin to the chosen target's transform.</param>
        public static bool TryFindAimTarget(
            Vector3 selfPosition,
            Rigidbody selfRigidbody,
            Vector3 coneAxis,
            AimAssistDefinition definition,
            float rangeMeters,
            out Vector3 aimDir,
            out float distance)
        {
            return TryFindTarget(
                selfPosition,
                selfRigidbody,
                coneAxis,
                definition,
                rangeMeters,
                ResolveAliveReceiver,
                out aimDir,
                out distance);
        }

        /// <summary>
        /// GENERIC variant of the same selection (cone + range + nearest), where the aim
        /// transform for a collider is supplied by <paramref name="resolve"/>. This is what
        /// lets the SAME math serve damage targeting (<see cref="HitReceiver"/>) and throw
        /// targeting (enemies + puzzle props through <see cref="ResolveThrowable"/>) without
        /// duplicating the cone.
        ///
        /// CONTRACT: <paramref name="resolve"/> must not return a transform that belongs to
        /// <paramref name="selfRigidbody"/>; own colliders are already skipped, but a proxy
        /// transform under the self body would otherwise be selectable.
        /// </summary>
        /// <param name="resolve">Collider-to-aim-transform resolver (null = no selection).</param>
        public static bool TryFindTarget(
            Vector3 selfPosition,
            Rigidbody selfRigidbody,
            Vector3 coneAxis,
            AimAssistDefinition definition,
            float rangeMeters,
            System.Func<Collider, Transform> resolve,
            out Vector3 aimDir,
            out float distance)
        {
            aimDir = Vector3.zero;
            distance = 0f;
            if (resolve == null) return false;
            if (definition == null || !definition.enabled) return false;

            Vector3 facing = coneAxis;
            facing.y = 0f;
            if (facing.sqrMagnitude < 0.0001f) facing = Vector3.forward;
            facing.Normalize();

            float range = Mathf.Max(0.01f, rangeMeters);
            Collider[] cols = Physics.OverlapSphere(
                selfPosition, range, Physics.AllLayers, QueryTriggerInteraction.Ignore);

            float cosLimit = Mathf.Cos(Mathf.Clamp(definition.coneHalfAngleDeg, 0f, 180f) * Mathf.Deg2Rad);
            float minRange = Mathf.Max(0f, definition.minRangeMeters);
            float minSqr = minRange * minRange;

            Transform best = null;
            float bestSqr = float.MaxValue;
            for (int i = 0; i < cols.Length; i++)
            {
                var col = cols[i];
                if (col == null) continue;
                if (col.attachedRigidbody == selfRigidbody) continue; // own capsule / Face trigger

                Transform candidate = resolve(col);
                if (candidate == null) continue; // not a valid target for this caller

                Vector3 to = candidate.position - selfPosition;
                to.y = 0f;
                float sqr = to.sqrMagnitude;
                if (sqr < 0.0001f || sqr < minSqr) continue; // no direction / dead-zone
                if (sqr >= bestSqr) continue;                // not nearer than the best

                Vector3 dir = to / Mathf.Sqrt(sqr);
                if (Vector3.Dot(facing, dir) < cosLimit) continue; // outside the frontal cone

                bestSqr = sqr;
                best = candidate;
            }

            if (best == null) return false;

            Vector3 aim = best.position - selfPosition;
            aim.y = 0f;
            if (aim.sqrMagnitude < 0.0001f) return false;

            distance = aim.magnitude;
            aimDir = aim / distance;
            return true;
        }

        /// <summary>
        /// Collider -> throw target. An ACTIVE <see cref="IThrowTarget"/> (puzzle props) wins;
        /// otherwise a living <see cref="HitReceiver"/> (enemies/dummies). Null when the
        /// collider is not throwable. Safe to hand directly to <see cref="TryFindTarget"/>.
        /// </summary>
        public static Transform ResolveThrowable(Collider col)
        {
            if (col == null) return null;

            IThrowTarget throwTarget = col.GetComponentInParent<IThrowTarget>();
            if (throwTarget != null && throwTarget.IsThrowTargetActive)
            {
                Transform point = throwTarget.ThrowTargetTransform;
                if (point != null) return point;
            }

            HitReceiver receiver = col.GetComponentInParent<HitReceiver>();
            if (receiver != null && receiver.IsAlive) return receiver.transform;

            return null;
        }

        /// <summary>Damage-target resolver: the living receiver (never a dead/invulnerable husk).</summary>
        private static Transform ResolveAliveReceiver(Collider col)
        {
            if (col == null) return null;

            HitReceiver receiver = col.GetComponentInParent<HitReceiver>();
            return receiver != null && receiver.IsAlive ? receiver.transform : null;
        }
    }
}
