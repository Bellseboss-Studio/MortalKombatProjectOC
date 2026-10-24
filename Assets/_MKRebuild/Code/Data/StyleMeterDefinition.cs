using UnityEngine;

namespace MK.Rebuild
{
    /// <summary>
    /// M1 style-meter data (slice style-meter): EVERY v0.1 number lives here, tuned
    /// in the inspector — no hardcoded style constants in <see cref="StyleMeter"/>.
    /// Source: E DEMO 03-sistemas.md §"Reglas del estilo v0.1" + slice note
    /// (2026-10-14: variety/kill/decay/thresholds + rate-per-point charges).
    /// Move-key format (novelty): <c>comboAssetName#stepIndex</c> (e.g.
    /// <c>Combo_1#2</c>, opener <c>Combo_DashAttack#0</c>, grey-box fallback
    /// <c>Attack_Light#0</c>). Renaming an asset changes its key — acceptable in
    /// prototype; stable move IDs come post-M1.
    /// </summary>
    [CreateAssetMenu(fileName = "StyleMeter", menuName = "MK/Rebuild/StyleMeterDefinition")]
    public sealed class StyleMeterDefinition : ScriptableObject
    {
        [Tooltip("Points per connected hit (+8 v0.1).")]
        public int pointsPerHit = 8;

        [Tooltip("Novelty multiplier when the move was NOT used inside the window (x2 v0.1).")]
        public float noveltyMultiplier = 2f;

        [Tooltip("Anti-spam window in UNSCALED seconds (5 v0.1).")]
        public float noveltyWindowSeconds = 5f;

        [Tooltip("Bonus points per kill (+25 v0.1).")]
        public int killPoints = 25;

        [Tooltip("Decay rate in points per second (-15/s v0.1).")]
        public float decayPerSecond = 15f;

        [Tooltip("Idle UNSCALED seconds without a hit before decay starts (2 v0.1).")]
        public float decayDelaySeconds = 2f;

        [Tooltip("Points to reach rank C (100 v0.1). D starts at 0.")]
        public int thresholdC = 100;

        [Tooltip("Points to reach rank B (220 v0.1).")]
        public int thresholdB = 220;

        [Tooltip("Points to reach rank A (360 v0.1).")]
        public int thresholdA = 360;

        [Tooltip("Points to reach rank S (520 v0.1).")]
        public int thresholdS = 520;

        [Tooltip("Base charge-fill rate (progress 0..1 per second at 0 points). MORE points fill FASTER via chargeFillPerPoint.")]
        public float chargeFillRateBase = 0.05f;

        [Tooltip("Extra charge-fill rate per style point (progress per second per point).")]
        public float chargeFillPerPoint = 0.0005f;

        [Tooltip("Max banked Fatality charges (3 v0.1).")]
        public int maxCharges = 3;
    }
}
