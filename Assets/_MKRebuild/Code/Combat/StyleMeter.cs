using System;
using System.Collections.Generic;
using UnityEngine;

namespace MK.Rebuild
{
    /// <summary>
    /// M1 style meter (slice style-meter): an OBSERVER — it never drives gameplay,
    /// it only listens to two additive events and keeps score. Same asmdef
    /// (<c>MK.Rebuild.Runtime</c>).
    ///
    /// Scoring (all numbers from <see cref="StyleMeterDefinition"/>):
    /// variety (+pts/hit, x2 novelty inside the unscaled window) + kills (+pts) −
    /// decay (−pts/s after the idle delay, UNSCALED so it survives hitstop).
    /// Ranks D-C-B-A-S by thresholds. One line readout in
    /// <c>MK.Rebuild.Debugging.DebugWindow</c> (F3 block): NO real HUD.
    ///
    /// Fatality charges (user-designed): a 0..1 progress fills at a rate
    /// proportional to the points (<c>base + points*perPoint</c>/s) — MORE points
    /// fill FASTER. Completing banks one charge (cap <c>maxCharges</c>). The future
    /// M4 Fatality banks/spends through <see cref="AddCharge"/> /
    /// <see cref="SpendCharge"/> — no finisher, no QTE here.
    ///
    /// Limpieza (damage-taken rank-down): <see cref="NotifyDamageTaken"/> is an
    /// effect-free stub. It now HAS a real caller — <see cref="PlayerDamage"/> invokes
    /// it on every non-lethal hit and on death — but the rank-down effect itself is
    /// deferred to M3 (a future design decision), so its body stays empty. Rank→D on
    /// RESPAWN is instead the explicit <see cref="ResetRank"/> that
    /// <see cref="CheckpointSystem"/> drives (m2-respawn slice 3).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class StyleMeter : MonoBehaviour
    {
        [Tooltip("All v0.1 numbers (points, novelty, kill, decay, thresholds, charge rate, cap). Null = meter stays at zero.")]
        [SerializeField] private StyleMeterDefinition definition;

        [Tooltip("Combat source for hit events. Null = auto-resolved (first PlayerCombat in the scene).")]
        [SerializeField] private PlayerCombat combat;

        // Novelty map: move-key ("asset#index") -> last hit time (UNSCALED seconds).
        private readonly Dictionary<string, float> lastUsed = new Dictionary<string, float>();

        private float points;
        private float lastHitTime = float.NegativeInfinity;
        private float chargeProgress; // 0..1 toward the next banked charge
        private int charges;

        /// <summary>Current style points (float; the readout floors it).</summary>
        public float Points => points;

        /// <summary>Live rank letter from the definition thresholds (D-C-B-A-S).</summary>
        public char Rank => RankFor(points);

        /// <summary>Banked Fatality charges (0..maxCharges).</summary>
        public int Charges => charges;

        /// <summary>Max banked charges from the definition (3 v0.1).</summary>
        public int MaxCharges => definition != null ? definition.maxCharges : 0;

        /// <summary>Progress 0..1 toward the next banked charge (read-only debug seam).</summary>
        public float ChargeProgress => chargeProgress;

        private void Awake()
        {
            ResolveCombat();
        }

        private void OnEnable()
        {
            ResolveCombat();
            if (combat != null) combat.OnStepConnected += NotifyHit;
            HitReceiver.AnyDied += NotifyKill;
        }

        private void OnDisable()
        {
            if (combat != null) combat.OnStepConnected -= NotifyHit;
            HitReceiver.AnyDied -= NotifyKill;
        }

        private void ResolveCombat()
        {
            if (combat == null) combat = FindFirstObjectByType<PlayerCombat>();
        }

        private void Update()
        {
            if (definition == null) return;
            float now = Time.unscaledTime;

            // Decay after the idle delay (UNSCALED — survives hitstop timeScale = 0).
            if (now - lastHitTime >= definition.decayDelaySeconds && points > 0f)
                points = Mathf.Max(0f, points - definition.decayPerSecond * Time.unscaledDeltaTime);

            // Charge progress fills at a rate proportional to the points:
            // MORE points = fills FASTER. Capped stock holds (no overfill).
            if (charges < definition.maxCharges)
            {
                float rate = definition.chargeFillRateBase + points * definition.chargeFillPerPoint;
                chargeProgress += Mathf.Max(0f, rate) * Time.unscaledDeltaTime;
                while (chargeProgress >= 1f && charges < definition.maxCharges)
                {
                    chargeProgress -= 1f;
                    charges++;
                }
                if (charges >= definition.maxCharges) chargeProgress = 0f;
            }
            else
            {
                chargeProgress = 0f;
            }
        }

        /// <summary>
        /// Scores one connected hit. First use of a move-key inside the novelty
        /// window pays x2 (variety); repeats pay base. Called by
        /// <see cref="PlayerCombat"/> with the <c>asset#index</c> key.
        /// </summary>
        public void NotifyHit(string moveKey)
        {
            if (definition == null || string.IsNullOrEmpty(moveKey)) return;
            float now = Time.unscaledTime;
            float award = definition.pointsPerHit;
            if (!lastUsed.TryGetValue(moveKey, out float last) || now - last >= definition.noveltyWindowSeconds)
                award *= definition.noveltyMultiplier;
            lastUsed[moveKey] = now;
            lastHitTime = now;
            points += award;
        }

        /// <summary>Scores one kill (+killPoints). Wired to <see cref="HitReceiver.AnyDied"/>.</summary>
        public void NotifyKill()
        {
            if (definition == null) return;
            lastHitTime = Time.unscaledTime;
            points += definition.killPoints;
        }

        /// <summary>
        /// Limpieza hook (damage-taken → rank-down): DORMANT STUB for M3. Leena is
        /// not damageable and no enemy hits back, so this has NO callers and NO
        /// effect today. Kept so the future hurt path plugs in without a new seam.
        /// </summary>
        public void NotifyDamageTaken()
        {
            // M3: rank-down effect goes here. Deliberately empty in this slice.
        }

        /// <summary>
        /// Drops the rank to D by zeroing style points. Called by
        /// <see cref="CheckpointSystem"/> on respawn (m2-respawn slice 3). Banked
        /// Fatality <see cref="Charges"/> are deliberately UNTOUCHED — death resets the
        /// rank, not the banked stock. Charge progress is left alone (it only ever fills).
        /// </summary>
        public void ResetRank()
        {
            points = 0f;
        }

        /// <summary>
        /// Banks one Fatality charge now (future M4 API): caps at maxCharges.
        /// </summary>
        public void AddCharge()
        {
            if (definition == null) return;
            if (charges < definition.maxCharges) charges++;
        }

        /// <summary>
        /// Spends one banked charge (future M4 Fatality API): true when a charge
        /// was available. No finisher, no QTE — just the stock operation.
        /// </summary>
        public bool SpendCharge()
        {
            if (charges <= 0) return false;
            charges--;
            return true;
        }

        private char RankFor(float pts)
        {
            if (definition == null) return 'D';
            if (pts >= definition.thresholdS) return 'S';
            if (pts >= definition.thresholdA) return 'A';
            if (pts >= definition.thresholdB) return 'B';
            if (pts >= definition.thresholdC) return 'C';
            return 'D';
        }
    }
}
