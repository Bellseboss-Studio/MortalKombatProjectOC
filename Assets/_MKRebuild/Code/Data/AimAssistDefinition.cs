using UnityEngine;

namespace MK.Rebuild
{
    /// <summary>
    /// M1 slice-3quater aim-assist data (2026-10-13): a SOFT trajectory corrector.
    /// Today an attack demands pixel precision — the lunge and the hitbox sphere
    /// both come from <c>visual.Facing</c>, so a slightly off-axis enemy whiffs.
    /// This definition parametrizes a corrector that adjusts ONE point
    /// (<c>visual.Facing</c>) at attack ENTRY, dragging lunge + hitbox with it
    /// (no motor changes, no continuous homing). All numbers live in data.
    ///
    /// Units are METERS (the 3D rebuild), NOT the doc's pre-3D pixels. The ±60°
    /// frontal cone follows the Fatality-selection precedent ("el más cercano en
    /// tu cono de facing"). A/B: <see cref="enabled"/> = false disables it.
    /// </summary>
    [CreateAssetMenu(fileName = "AimAssist", menuName = "MK/Rebuild/AimAssistDefinition")]
    public sealed class AimAssistDefinition : ScriptableObject
    {
        [Tooltip("Master switch. false = no correction at all (attack behaves forward-only, current behavior).")]
        public bool enabled = true;

        [Tooltip("Half-angle in DEGREES of the frontal cone measured from the current facing (60 — Fatality-selection precedent). Targets outside are ignored.")]
        public float coneHalfAngleDeg = 60f;

        [Tooltip("Max target distance in METERS (OverlapSphere radius). ~3 m ≈ reach + lunge (the 3D rebuild uses meters, NOT the doc's px).")]
        public float maxRangeMeters = 3f;

        [Tooltip("Inner dead-zone radius in METERS: targets CLOSER than this are ignored (0 = none). Avoids snapping onto an overlapping body with no real direction.")]
        public float minRangeMeters = 0f;

        [Tooltip("Correction turn speed in DEGREES/SECOND (720). Used to size the entry correction window (worst-case cone error / turnSpeed); the actual per-frame rotation is owned by the visual adapter.")]
        public float turnSpeedDegPerSec = 720f;
    }
}
