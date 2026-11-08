using System;
using System.Collections.Generic;
using UnityEngine;

namespace MK.Rebuild
{
    /// <summary>
    /// What a move IS inside a stance's chain. The ender is a ROLE in the data, not
    /// "the last index of the combo": with a matrix the chain can close on any move.
    /// Append-only (serialized in the stance assets).
    /// </summary>
    public enum ComboRole
    {
        Opener = 0,
        Filler = 1,
        Closer = 2,
        GuardBreak = 3
    }

    /// <summary>
    /// Result of a step's hit probe, fed back into the chain state. <see cref="Ignored"/> =
    /// the probe did not connect (whiff); <see cref="Hit"/> = it landed; <see cref="Blocked"/>
    /// is the seam for a future enemy guard (unreachable today).
    /// </summary>
    public enum HitOutcome
    {
        Ignored = 0,
        Hit = 1,
        Blocked = 2
    }

    /// <summary>
    /// A pointer to ONE step of a <see cref="ComboDefinition"/>. The matrix never copies
    /// step data: it references the asset + index so live inspector edits keep flowing.
    /// </summary>
    [Serializable]
    public struct ComboMoveRef
    {
        [Tooltip("Combo asset that owns the step.")]
        public ComboDefinition combo;

        [Tooltip("0-based step index inside that combo.")]
        [Min(0)] public int stepIndex;

        /// <summary>True when a combo is assigned and the index is non-negative.</summary>
        public bool IsValid => combo != null && stepIndex >= 0;

        /// <summary>Identity comparison (same asset instance + same index) — no value copies.</summary>
        public bool Equals(in ComboMoveRef other)
        {
            return ReferenceEquals(combo, other.combo) && stepIndex == other.stepIndex;
        }
    }

    /// <summary>
    /// One transition out of a row: pressing <see cref="input"/> with stick
    /// <see cref="variant"/> leads to <see cref="next"/>.
    /// </summary>
    [Serializable]
    public struct MatrixEntry
    {
        [Tooltip("Input that triggers the transition (Ligero / Pesado / ...).")]
        public ComboInput input;

        [Tooltip("Stick variant that keys this transition, relative to the facing. Neutral is also the fallback when no exact variant exists.")]
        public AttackDirection variant;

        [Tooltip("Move this transition advances into.")]
        public ComboMoveRef next;
    }

    /// <summary>
    /// One move of the stance's chain: its identity, its role, and everything it can
    /// lead to. Rows are matched exactly (same combo + same step), first match wins.
    /// </summary>
    [Serializable]
    public struct MatrixRow
    {
        [Tooltip("The move this row describes (combo + step index).")]
        public ComboMoveRef current;

        [Tooltip("Role of this move: Opener / Filler / Closer (ender bonus) / GuardBreak.")]
        public ComboRole role;

        [Tooltip("Transitions out of this move. One press resolves AT MOST one entry here.")]
        public List<MatrixEntry> entries;
    }

    /// <summary>
    /// ONE stance's combo language, as data: which transitions exist, the chain windows
    /// (absolute seconds), the chain budget, the damage curve and the style cadence.
    ///
    /// WHY one asset per stance: each style tunes its own windows and its own cadence,
    /// and a diff of one language must never touch the other. Policy (slots, no-repeat,
    /// reset, buffering) does NOT live here — it lives in <c>ComboChainState</c>; this
    /// asset is pure data so it can be tuned in the inspector without a recompile.
    ///
    /// NOTE: windows are ABSOLUTE seconds, not fractions of a step. Raising
    /// <see cref="styleSpeedScale"/> shortens each step, so the windows and the cadence
    /// of a style MUST be tuned together (see design, "Interacción cadencia ↔ ventanas").
    /// </summary>
    [CreateAssetMenu(fileName = "Stance", menuName = "MK/Rebuild/StanceComboMatrix")]
    public sealed class StanceComboMatrix : ScriptableObject
    {
        [Header("Identidad")]
        [Tooltip("Stance this matrix describes. PlayerCombat picks the active matrix by matching this against PlayerStance.CurrentStance.")]
        [SerializeField] private PlayerStance.Stance stance = PlayerStance.Stance.Mantis;

        [Header("Ventanas (s)")]
        [Tooltip("Cancel window length in SECONDS when the hit CONNECTED (hit-cancel). The generous one.")]
        [SerializeField, Min(0f)] private float connectWindowSeconds = 0.20f;

        [Tooltip("Cancel window length in SECONDS when the step MISSED (whiff). The tight one.")]
        [SerializeField, Min(0f)] private float whiffWindowSeconds = 0.10f;

        [Tooltip("How early (SECONDS, before the window opens) a press is still buffered instead of dropped.")]
        [SerializeField, Min(0f)] private float bufferGraceSeconds = 0.10f;

        [Tooltip("Seconds of neutral with no press before the chain resets by itself.")]
        [SerializeField, Min(0f)] private float chainResetSeconds = 0.45f;

        [Tooltip("Maximum advances in one chain. Each normal advance consumes one slot; changing stance costs NO slot (it resets the chain).")]
        [SerializeField, Min(1)] private int chainSlots = 6;

        [Header("Cadencia")]
        [Tooltip("Per-style attack speed multiplier (Mantis 1.15 = faster, Tarkatana 0.80 = heavier). Multiplies AttackSpeed; 1 = unchanged.")]
        [SerializeField, Min(0.1f)] private float styleSpeedScale = 1f;

        [Header("Daño")]
        [Tooltip("Damage multiplier by CONNECTED hits so far (index 0 = first hit). Past the end the last value repeats.")]
        [SerializeField] private float[] damageScaleByHit = { 1f, 0.9f, 0.8f, 0.7f };

        [Tooltip("Damage can never drop below this fraction of the authored value.")]
        [SerializeField, Range(0f, 1.5f)] private float damageFloorPct = 0.5f;

        [Tooltip("Extra damage fraction when the move that lands is the Closer (0.15 = +15%).")]
        [SerializeField, Range(0f, 1f)] private float enderBonusPct = 0.15f;

        [Header("Filas")]
        [Tooltip("Every move this stance knows, with its transitions. Empty = the stance has no chains at all.")]
        [SerializeField] private List<MatrixRow> rows = new List<MatrixRow>();

        public PlayerStance.Stance Stance => stance;
        public float ConnectWindow => connectWindowSeconds;
        public float WhiffWindow => whiffWindowSeconds;
        public float BufferGrace => bufferGraceSeconds;
        public float ChainResetSeconds => chainResetSeconds;
        public int ChainSlots => chainSlots;

        /// <summary>Per-style attack cadence (1.15 Mantis / 0.80 Tarkatana).</summary>
        public float StyleSpeedScale => styleSpeedScale;

        /// <summary>
        /// Role of a move. Unknown moves are reported as <see cref="ComboRole.Opener"/>
        /// (the neutral role: only Closer changes anything downstream).
        /// </summary>
        public ComboRole RoleOf(in ComboMoveRef move)
        {
            int index = IndexOfRow(move);
            return index >= 0 ? rows[index].role : ComboRole.Opener;
        }

        /// <summary>
        /// Damage multiplier for the next landed hit. Scale by hit count FIRST, then the
        /// floor, then the ender bonus, and finally clamp to [0.5, 1.5] so a mis-authored
        /// asset can never nuke or inflate a combo.
        /// </summary>
        public float DamageScale(int hitsLanded, bool isEnder)
        {
            float pct = 1f;
            if (damageScaleByHit != null && damageScaleByHit.Length > 0)
            {
                int index = Mathf.Clamp(hitsLanded, 0, damageScaleByHit.Length - 1);
                pct = damageScaleByHit[index];
            }

            pct = Mathf.Max(pct, damageFloorPct);
            if (isEnder) pct *= 1f + enderBonusPct;
            return Mathf.Clamp(pct, 0.5f, 1.5f);
        }

        /// <summary>
        /// THE single lookup of the matrix. A press resolves exactly ONCE:
        /// exact (input + variant) first; if that entry does not exist, the Neutral
        /// entry of the SAME input; otherwise false (no attack, nothing is guessed).
        /// </summary>
        public bool TryResolve(in ComboMoveRef current, ComboInput input, AttackDirection variant, out ComboMoveRef next)
        {
            next = default;

            int rowIndex = IndexOfRow(current);
            if (rowIndex < 0) return false;

            List<MatrixEntry> entries = rows[rowIndex].entries;
            if (entries == null) return false;

            if (TryFindEntry(entries, input, variant, out next)) return true;
            if (variant != AttackDirection.Neutral && TryFindEntry(entries, input, AttackDirection.Neutral, out next)) return true;

            next = default;
            return false;
        }

        /// <summary>First row whose move matches exactly; -1 when absent.</summary>
        private int IndexOfRow(in ComboMoveRef move)
        {
            if (!move.IsValid || rows == null) return -1;

            for (int i = 0; i < rows.Count; i++)
            {
                if (rows[i].current.Equals(in move)) return i;
            }

            return -1;
        }

        /// <summary>First entry with this exact (input, variant) whose target is usable.</summary>
        private static bool TryFindEntry(List<MatrixEntry> entries, ComboInput input, AttackDirection variant, out ComboMoveRef next)
        {
            for (int i = 0; i < entries.Count; i++)
            {
                MatrixEntry entry = entries[i];
                if (entry.input != input || entry.variant != variant) continue;
                if (!entry.next.IsValid) continue;

                next = entry.next;
                return true;
            }

            next = default;
            return false;
        }
    }
}
