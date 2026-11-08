using System.Collections.Generic;
using UnityEngine;

namespace MK.Rebuild
{
    /// <summary>
    /// COMBO CHAIN STATE — runtime-only state of the in-combo chain.
    ///
    /// T0.2 moved the step cursor, the scaled step clock, the per-step "connected"
    /// flag and the (input, facing-relative direction) buffer here VERBATIM out of
    /// <see cref="PlayerCombat"/> (same names, same initializers, zero behavior
    /// change). Plain sealed C# class — NOT a MonoBehaviour and NOT [Serializable]
    /// (decision 2 of the combo-grammar design): this state is neither authored nor
    /// saved, so it must not touch the inspector or the prefabs.
    ///
    /// T2.3 added the POLICY of the grammar on top of the moved fields: the move
    /// memory (no-repeat), the slot budget, the connected-hit counter, the hit
    /// outcome of the current step and the explicit buffer with its neutral-reset
    /// clock. The legacy V1 path keeps reading the public fields directly and never
    /// touches the new members, so nothing here can change the old behavior.
    ///
    /// TWO MEMORIES, deliberately separate:
    ///  * the LEGACY cursor (<see cref="stepIndex"/>, <see cref="stepTime"/>,
    ///    <see cref="connectedThisStep"/>, light/heavy buffers) — V1 reads it;
    ///  * the GRAMMAR memory (<see cref="Current"/>, <see cref="Used"/>,
    ///    <see cref="SlotsUsed"/>, <see cref="HitsLanded"/>, <see cref="Outcome"/>,
    ///    the buffered press) — the grammar path reads it.
    /// <see cref="Reset"/> clears the grammar memory ONLY: it must never yank the
    /// legacy cursor out from under a running step.
    /// </summary>
    public sealed class ComboChainState
    {
        // ------------------------------------------------------------------
        // LEGACY (V1) cursor — moved verbatim in T0.2. The V1 path reads/writes
        // these fields directly; do not rename or remove them.
        // ------------------------------------------------------------------
        public int stepIndex = -1;             // index into combo.steps
        public float stepTime;                 // scaled seconds into the step
        public bool connectedThisStep;         // hit landed this step -> wider cancel

        // Input taps (unscaled timers, jumpBuffer pattern keyed by button).
        public float lightBuffer;
        public float heavyBuffer;

        // Direction (relative to the facing) classified at the PRESS frame and carried
        // with the buffer, so a mid-combo branch matches the direction the player pressed
        // even if the stick is released before the cancel window opens (follow-up slice).
        public AttackDirection bufferedDirection;

        // ------------------------------------------------------------------
        // GRAMMAR memory (T2.3)
        // ------------------------------------------------------------------

        // Moves already played in THIS chain memory. Cleared only by Reset (neutral
        // timeout / hit taken / target death / stance switch) — NOT by EndCombo: the
        // spec keeps a move unavailable after the chain closes until the 0.45 s
        // neutral window elapses ("el mismo ataque vuelve a estar disponible").
        private readonly List<ComboMoveRef> used = new List<ComboMoveRef>(8);

        // Seconds spent outside a running step without an attack press (neutral clock).
        private float neutralTime;

        /// <summary>Move the chain is currently on (the matrix row to look up).</summary>
        public ComboMoveRef Current { get; private set; }

        /// <summary>True while a chain memory exists (from the first root until Reset).</summary>
        public bool IsActive { get; private set; }

        /// <summary>Normal advances consumed in the current chain.</summary>
        public int SlotsUsed { get; private set; }

        /// <summary>Slot budget of the current chain (from the matrix; MaxValue without one).</summary>
        public int SlotsMax { get; private set; }

        /// <summary>Hits that actually CONNECTED in the current chain (drives damage scaling).</summary>
        public int HitsLanded { get; private set; }

        /// <summary>Result of the CURRENT step's probe (drives the asymmetric cancel window).</summary>
        public HitOutcome Outcome { get; private set; }

        // ------------------------------------------------------------------
        // GRAMMAR press QUEUE (T2.6) — a FIFO of at most TWO presses.
        //
        // The old single slot was the root cause of "L→L→P de Mantis no sale": a second
        // press OVERWROTE the first one, so an input with no transition in the current
        // row (P from Combo_1#0) destroyed the valid press that was already waiting.
        // Now a press that does not resolve is NOT destroyed on the spot: it survives
        // ONE step transition and is retried in the next step; a second failure discards
        // it. The same button replaces its pending press (last press wins for that
        // button) and a full queue drops its OLDEST press. Each step consumes at most
        // ONE lookup, always on the FRONT of the queue.
        // ------------------------------------------------------------------

        /// <summary>Hard cap of the press queue (design T2.6: a FIFO of 2).</summary>
        public const int MaxPresses = 2;

        private struct BufferedPress
        {
            public ComboInput input;
            public AttackDirection variant;
            public float remaining;     // unscaled seconds until the window closes
            public int stepsSurvived;   // step transitions survived since the press
            public int failures;        // lookups that did not resolve
        }

        private readonly BufferedPress[] presses = new BufferedPress[MaxPresses];
        private int pressCount;

        /// <summary>Presses currently waiting in the queue (0..<see cref="MaxPresses"/>).</summary>
        public int PressCount => pressCount;

        /// <summary>True while at least one press is queued and still within its lifetime.</summary>
        public bool HasBufferedPress => pressCount > 0;

        // ------------------------------------------------------------------
        // API
        // ------------------------------------------------------------------

        /// <summary>True when this move already played in the current chain memory.</summary>
        public bool HasUsed(in ComboMoveRef move)
        {
            for (int i = 0; i < used.Count; i++)
            {
                if (used[i].Equals(in move)) return true;
            }

            return false;
        }

        /// <summary>
        /// Starts a chain at <paramref name="root"/>.
        ///
        /// The MOVE MEMORY survives a closed chain (the spec keeps a used move locked
        /// until the neutral reset), so Begin only clears it when no memory is live.
        /// The per-chain counters (slots, landed hits) always start fresh, and the
        /// step outcome starts at <see cref="HitOutcome.Ignored"/>.
        /// </summary>
        public void Begin(in ComboMoveRef root, int slots)
        {
            if (!IsActive) used.Clear();

            IsActive = true;
            SlotsMax = Mathf.Max(1, slots);
            SlotsUsed = 0;
            HitsLanded = 0;
            Outcome = HitOutcome.Ignored;
            Current = root;
            MarkUsed(in root); // the opener cannot repeat inside its own chain either
            ClearBuffer();
            neutralTime = 0f;
        }

        /// <summary>
        /// Spends ONE slot of the chain budget. This is the cost of a NORMAL advance
        /// only — a stance switch costs the whole chain (a reset), never a slot.
        /// False when the budget is exhausted (the press must not chain).
        /// </summary>
        public bool TryConsumeSlot()
        {
            if (SlotsUsed >= SlotsMax) return false;
            SlotsUsed++;
            return true;
        }

        /// <summary>
        /// Records the move the chain just advanced into (memory + current row). The
        /// SLOT is NOT spent here: the caller checks <see cref="TryConsumeSlot"/>
        /// first and pushes only when the advance is actually happening, so the budget
        /// is never double-charged. Resets the per-step outcome.
        /// </summary>
        public void Push(in ComboMoveRef next)
        {
            IsActive = true;
            Current = next;
            MarkUsed(in next);
            Outcome = HitOutcome.Ignored;
            neutralTime = 0f;
        }

        /// <summary>
        /// Feeds the result of the current step's probe. A landed <see cref="HitOutcome.Hit"/>
        /// is STICKY for the step (later probe frames that find no new victim must not
        /// downgrade it to a whiff and shrink the window) and increments
        /// <see cref="HitsLanded"/> exactly once per step. Blocked / Ignored are the
        /// latest-wins "short window" states.
        /// </summary>
        public void MarkOutcome(HitOutcome outcome)
        {
            if (outcome == HitOutcome.Hit)
            {
                if (Outcome != HitOutcome.Hit) HitsLanded++;
                Outcome = HitOutcome.Hit;
                return;
            }

            if (Outcome == HitOutcome.Hit) return; // sticky: a connected hit stays connected
            Outcome = outcome;
        }

        /// <summary>
        /// Queues a press with its UNSCALED lifetime (seconds): the grammar sizes it so a
        /// press inside the grace survives until the window CLOSES. The SAME button replaces
        /// its pending press (last press wins for that button); when the queue is full the
        /// OLDEST press is dropped.
        /// </summary>
        public void EnqueuePress(ComboInput input, AttackDirection variant, float seconds)
        {
            for (int i = 0; i < pressCount; i++)
            {
                if (presses[i].input != input) continue;
                RemovePressAt(i); // same button: the new press replaces the pending one
                break;
            }

            if (pressCount >= MaxPresses) RemovePressAt(0); // full: drop the OLDEST

            presses[pressCount] = new BufferedPress
            {
                input = input,
                variant = variant,
                remaining = Mathf.Max(0f, seconds),
                stepsSurvived = 0,
                failures = 0
            };
            pressCount++;
            neutralTime = 0f; // a press is activity: it restarts the neutral clock
        }

        /// <summary>Reads the FRONT press — the only one a step may look up.</summary>
        public bool TryPeekPress(out ComboInput input, out AttackDirection variant)
        {
            if (pressCount <= 0)
            {
                input = default;
                variant = default;
                return false;
            }

            input = presses[0].input;
            variant = presses[0].variant;
            return true;
        }

        /// <summary>The front press RESOLVED: it leaves the queue.</summary>
        public void ResolveFrontPress()
        {
            if (pressCount > 0) RemovePressAt(0);
        }

        /// <summary>
        /// The front press did NOT resolve in this step. The FIRST failure RETAINS it (it is
        /// retried in the following step); the SECOND failure discards it (spec: "se reintenta
        /// una sola vez en el paso siguiente y se descarta si falla").
        /// </summary>
        public void FailFrontPress()
        {
            if (pressCount <= 0) return;

            presses[0].failures++;
            if (presses[0].failures >= 2) RemovePressAt(0);
        }

        /// <summary>
        /// The chain advanced one step: every queued press survives exactly ONE transition.
        /// Presses that already outlived one are discarded, so a stale press can never come
        /// back later as a ghost attack.
        /// </summary>
        public void CarryPressesToNextStep()
        {
            for (int i = pressCount - 1; i >= 0; i--)
            {
                presses[i].stepsSurvived++;
                if (presses[i].stepsSurvived > 1) RemovePressAt(i);
            }
        }

        /// <summary>Re-anchors the carried presses to the NEW step's window lifetime.</summary>
        public void RefreshPressLifetime(float seconds)
        {
            float lifetime = Mathf.Max(0f, seconds);
            for (int i = 0; i < pressCount; i++) presses[i].remaining = lifetime;
        }

        /// <summary>Drops every buffered press (legacy buttons + grammar queue).</summary>
        public void ClearBuffer()
        {
            lightBuffer = 0f;
            heavyBuffer = 0f;
            pressCount = 0;
            for (int i = 0; i < presses.Length; i++) presses[i] = default;
        }

        /// <summary>
        /// Advances the chain clocks.
        ///
        /// <paramref name="scaledDt"/> drives the step clock (the caller skips the
        /// frozen hitstop frames, exactly like the old inline increment).
        /// <paramref name="unscaledDt"/> drives the buffered press and the neutral
        /// clock (jumpBuffer pattern: both survive hitstop).
        /// <paramref name="resetSeconds"/> is the neutral window before the chain
        /// memory clears. 0 or less means "a step is running", which can never be
        /// neutral — the timer is held at 0 instead of accumulating. +infinity means
        /// resets are disabled (no matrix / grammar off).
        ///
        /// Returns true when a NEUTRAL reset actually happened.
        /// </summary>
        public bool Tick(float scaledDt, float unscaledDt, float resetSeconds)
        {
            stepTime += scaledDt;

            // Unscaled lifetime of every queued press: expired presses are dropped so a
            // press that never reached its window can never fire later ("no se adivina").
            for (int i = pressCount - 1; i >= 0; i--)
            {
                presses[i].remaining = Mathf.Max(0f, presses[i].remaining - unscaledDt);
                if (presses[i].remaining <= 0f) RemovePressAt(i);
            }

            if (resetSeconds <= 0f) { neutralTime = 0f; return false; }      // mid-step: never neutral
            if (float.IsPositiveInfinity(resetSeconds)) return false;        // resets disabled

            neutralTime += unscaledDt;
            if (neutralTime < resetSeconds) return false;

            Reset();
            return true;
        }

        /// <summary>
        /// Clears the CHAIN MEMORY: used moves, slot budget, landed hits, step outcome
        /// and the buffered press. Does NOT touch the legacy cursor (stepIndex /
        /// stepTime / connectedThisStep) — the controller owns the running step, and a
        /// reset that yanked the cursor would desync `comboActive`.
        /// </summary>
        public void Reset()
        {
            IsActive = false;
            Current = default;
            SlotsUsed = 0;
            SlotsMax = 0;
            HitsLanded = 0;
            Outcome = HitOutcome.Ignored;
            used.Clear();
            bufferedDirection = default;
            ClearBuffer();
            neutralTime = 0f;
        }

        private void MarkUsed(in ComboMoveRef move)
        {
            if (!move.IsValid) return;
            if (HasUsed(in move)) return;
            used.Add(move);
        }

        /// <summary>Removes the press at <paramref name="index"/>, shifting the rest down.</summary>
        private void RemovePressAt(int index)
        {
            for (int i = index; i < pressCount - 1; i++) presses[i] = presses[i + 1];
            pressCount--;
            presses[pressCount] = default;
        }
    }
}
