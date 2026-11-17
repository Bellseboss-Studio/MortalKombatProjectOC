using System.Collections.Generic;
using UnityEngine;

namespace MK.Rebuild
{
    /// <summary>
    /// PlayerCombat — COMBO partial (T0.1 split): chain entry, advance, cancel
    /// windows, start/advance/end and the body-ownership token. Same type as
    /// <see cref="PlayerCombat"/>; the code below is the pre-split code, moved
    /// verbatim (no behavior change).
    ///
    /// T0.2 moved the per-step cursor/timer/buffers to <see cref="ComboChainState"/>;
    /// T0.3 moved the legacy root/continuation resolution to <see cref="ComboResolver"/>.
    /// Both keep the exact same behavior (pure refactor).
    /// </summary>
    public sealed partial class PlayerCombat
    {
        // Active combo (the one currently running), resolved by SelectRoot at entry.
        private ComboDefinition activeCombo;

        // Dash-attack opener state (slice 3octies): true while the active combo IS the
        // opener. PlayAttackVisual routes to the dormant Attack_Dash clip slot, and
        // TryAdvanceOnBuffer chains into the NORMAL combo roots instead of by index.
        private bool dashAttackActive;

        // Grey-box fallback swing (slice 1, one at a time).
        private AttackDefinition current;
        private float swingTime;                 // scaled seconds into the swing
        private readonly HashSet<HitReceiver> hitThisSwing = new HashSet<HitReceiver>();

        // Combo state (decision b: one combo at a time).
        private bool comboActive;
        private readonly ComboChainState chain = new ComboChainState(); // T0.2: step cursor/timer/buffers (moved verbatim)
        private ComboStep currentStep;          // struct copy of the active step

        // T2.6: "pot a paso, una sola lookup". A grammar step may look the press queue up AT
        // MOST once; the flag is cleared by every step transition (start / advance / end).
        private bool grammarLookupSpent;

        // KILL SWITCH (combo-grammar-neutral, R8). Serializado para poder volver al
        // comportamiento previo en Play Mode, en caliente y sin recompilar ni editar assets:
        // ON = TODO press resuelve sólo por la matriz de stance. El default (OFF) deja la
        // precedencia nueva (asset primero para el neutro). Ningún asset lo serializa todavía,
        // así que el default de C# es el que corre.
        [Tooltip("KILL SWITCH: ON = todo press resuelve sólo por la matriz de stance (comportamiento previo). Default OFF.")]
        [SerializeField] private bool neutralPrefersLegacy;

        private BodyOwnership.Token attackToken; // body-ownership token for moveLock steps
        private readonly HashSet<HitReceiver> hitThisStep = new HashSet<HitReceiver>();

        // ROOT VARIANT "FLICK" GATE: a stick direction held longer than
        // PlayerParams.directionFlickWindow is ordinary movement, not a deliberate
        // variant input. Walking toward the enemy with the stick held must keep the
        // neutral combo; only a fresh flick selects Forward/Side/Back roots.
        private AttackDirection heldDirection = AttackDirection.Neutral;
        private float heldDirectionTime;

        /// <summary>
        /// Ability cancel-window seam (change ability-cancel-window): true while a real
        /// COMBO is active AND its current step is PAST its cancel point — the earliest
        /// frame the player may interrupt it (on a connected hit the window opens earlier
        /// via <c>cancelHitNorm</c>, so the ability can cancel right after the strike
        /// lands). <see cref="PlayerAbilities"/> reads this to cancel the step and play
        /// the STANDALONE ability, mirroring how the dash cancels a combo. False when idle
        /// and for the grey-box single swing (which has no cancel window).
        /// </summary>
        public bool CanCancelIntoAbility()
        {
            if (!comboActive) return false;
            CancelWindowBounds(out float cancelStart, out float _);
            return chain.stepTime >= cancelStart;
        }

        /// <summary>Identity of the running step as the matrix sees it (combo asset + index).</summary>
        private ComboMoveRef CurrentMove => new ComboMoveRef { combo = activeCombo, stepIndex = chain.stepIndex };

        /// <summary>
        /// Damage this step actually applies.
        ///
        /// COMBO GRAMMAR (gate point 3 of 4, called from <see cref="ProbeHitboxStep"/>): with the
        /// grammar ON the authored value scales by the number of CONNECTED hits so far
        /// (100/90/80/70 % from the asset), never below the asset floor, with the ender bonus
        /// applied LAST when the move's role is Closer. Off = the authored value, untouched.
        /// </summary>
        private int AppliedDamage(in ComboStep step)
        {
            if (!GrammarEnabled) return step.dano;

            StanceComboMatrix matrix = ActiveMatrix;
            if (matrix == null) return step.dano;

            bool ender = matrix.RoleOf(CurrentMove) == ComboRole.Closer;
            float pct = matrix.DamageScale(chain.HitsLanded, ender);
            return Mathf.Max(1, Mathf.RoundToInt(step.dano * pct));
        }

        // ------------------------------------------------------------------
        // COMBO GRAMMAR reset signals (T2.4). Installed ONLY when the player has the
        // grammar (see PlayerCombat.OnEnable), so V1 never observes them.
        // ------------------------------------------------------------------

        /// <summary>
        /// Window predicate installed on <see cref="PlayerStance.CanSwitchStanceNow"/>. Mid-combo
        /// a stance switch survives only inside the cancel window; outside it the switch is
        /// IGNORED and the chain keeps running untouched. Idle = always allowed.
        /// </summary>
        private bool CanSwitchStanceNow() => !comboActive || IsInCancelWindow();

        /// <summary>
        /// Stance switch actually happened (it passed <see cref="CanSwitchStanceNow"/>): the
        /// switch costs the WHOLE chain and NO slot (spec "Cambio de estilo mid-combo").
        /// <see cref="EndCombo"/> closes the running step, releases the body token and empties
        /// the buffers; <see cref="ComboChainState.Reset"/> then clears the move memory, so the
        /// next press opens a clean chain in the new style.
        /// </summary>
        private void HandleStanceChanged(PlayerStance.Stance next)
        {
            if (comboActive) EndCombo();
            chain.Reset();
        }

        /// <summary>Player took a hit (or died): the chain memory resets (spec "Reset por recibir golpe").</summary>
        private void HandlePlayerDamaged() => chain.Reset();

        /// <summary>
        /// A target died: the chain memory resets (spec "Reset por muerte del objetivo"). The
        /// running step is left alone — only the memory clears. Approximation: <see cref="HitReceiver.AnyDied"/>
        /// is the static death echo, so ANY death in the scene clears it (the player is single-target
        /// today, so this is the only reachable authority).
        /// </summary>
        private void HandleTargetDied() => chain.Reset();

        // ------------------------------------------------------------------
        // Combo flow
        // ------------------------------------------------------------------

        private void UpdateIdle()
        {
            if (current != null)
            {
                // Scaled time: hitstop (timeScale = 0) freezes the grey-box swing.
                swingTime += Time.deltaTime;
                if (swingTime >= current.duration) EndSwing();
                return;
            }

            // ACTION GATE (change action-gating): no ground combat while airborne,
            // hanging or in a scripted state. DENY BY DEFAULT — only Free-grounded
            // starts a NEW attack. An attack already in flight keeps playing (its own
            // branch above); only the ENTRY is gated.
            if (!CanStartAttack())
            {
                chain.lightBuffer = 0f;
                chain.heavyBuffer = 0f;
                return;
            }

            // While the DASH owns the body, combat cannot normally START an attack
            // (its lunge would fight the dash velocity). Two paths (slice 3septies):
            //   1. EARLY CANCEL: an attack pressed past the dash's min cancel window CUTS the
            //      dash short (PlayerDash.TryCancelByAttack) and starts the attack this SAME
            //      frame — no waiting for the dash to end (dash-cancel into attack).
            //   2. FALLBACK: before that window (or with no dash) the press is BUFFERED (held,
            //      not decayed) and fires when PlayerDash releases the token at dash end.
            if (motor != null && motor.BodyOwner == BodyPriority.Dash)
            {
                bool lightPressed = LightPressedThisFrame();
                bool heavyPressed = HeavyPressedThisFrame();

                if ((lightPressed || heavyPressed) && dash != null && dash.TryCancelByAttack())
                {
                    if (TryStartAttack(lightPressed, heavyPressed)) return;
                }

                if (lightPressed) chain.lightBuffer = inputBufferSeconds;
                if (heavyPressed) chain.heavyBuffer = inputBufferSeconds;
                return;
            }

            // Consume either the direct press THIS frame OR a tap buffered during the dash.
            bool light = LightPressedThisFrame() || chain.lightBuffer > 0f;
            bool heavy = HeavyPressedThisFrame() || chain.heavyBuffer > 0f;
            if (light) chain.lightBuffer = 0f;
            if (heavy) chain.heavyBuffer = 0f;

            TryStartAttack(light, heavy);
        }

        /// <summary>
        /// Action gate for STARTING an attack (change action-gating): the motor's
        /// central gate must allow the attack verb (Free-grounded only). Light and
        /// Heavy share the same rule. Falls back to allowed when there is no motor,
        /// so combat-only setups keep working.
        /// </summary>
        private bool CanStartAttack()
        {
            if (motor == null) return true;
            return motor.CanPerform(PlayerAction.Light);
        }

        /// <summary>
        /// Starts whatever the pressed input resolves to: the LONGEST matching combo root
        /// (L→Combo_1 (L→L→P), P→Combo_H…), else the standalone grey-box swing, else the
        /// missing-definition warning. Shared by the idle entry and the dash early-cancel
        /// (slice 3septies) so both start the attack identically. Returns true when an
        /// attack actually started.
        /// </summary>
        private bool TryStartAttack(bool light, bool heavy)
        {
            // BODY-OWNERSHIP ENTRY GATE (m2-hazards S2): a swing must not start while a
            // higher-priority system (death / knockback) owns the body. Checked HERE rather
            // than in CanStartAttack: the dash branch of UpdateIdle must stay reachable so an
            // attack pressed during the dash can CANCEL it — this gate is re-evaluated after
            // that cancel releases the dash token.
            if (motor != null && motor.Body != null && !motor.Body.CanAcquire(BodyPriority.Attack))
                return false;

            // DIRECTIONAL VARIANT (slice 3nonies, decision a/f): read the RAW stick in
            // the FRAME OF THE PRESS and classify it. Done HERE, before StartCombo ->
            // ApplyAimAssist: the direction picks WHICH attack (root), the aim-assist
            // then corrects WHICH WAY the body faces (different axes, no conflict).
            AttackDirection direction = ReadRootDirection();

            // Impact cue for this attack (a heavy press selects the heavy hit, even when
            // it falls through to a light/standalone path). Set BEFORE any path starts.
            hitCue = heavy ? AudioCueId.HeavyHit : AudioCueId.LightHit;

            // DASH-ATTACK OPENER (slice 3octies): an attack during the dash or within
            // dashAttackWindow after it uses the dedicated dashAttack FIRST. Its
            // continuation chains into the normal combo roots (TryAdvanceOnBuffer).
            if ((light || heavy) && TryStartDashAttack())
            {
                PlayAttackAudio(heavy);
                return true;
            }

            // Combo entry (multi-root): pick the longest chain whose root matches
            // the pressed input AND the stick direction (exact > Any wildcard); a
            // lone press ends that chain's single step cleanly while a continuation
            // chains. No match -> grey-box standalone swing.
            if (light)
            {
                ComboDefinition c = ComboResolver.SelectRoot(combos, ComboInput.Ligero, direction);
                if (c != null) { StartCombo(c); PlayAttackAudio(false); return true; }
            }
            if (heavy)
            {
                ComboDefinition c = ComboResolver.SelectRoot(combos, ComboInput.Pesado, direction);
                if (c != null) { StartCombo(c); PlayAttackAudio(true); return true; }
            }

            if (light && lightAttack != null) { StartSwing(lightAttack); PlayAttackAudio(false); return true; }
            if (heavy && heavyAttack != null) { StartSwing(heavyAttack); PlayAttackAudio(true); return true; }

            if ((light || heavy) && !warnedMissingDefinition)
            {
                warnedMissingDefinition = true;
                Debug.LogWarning(
                    $"{name}: attack pressed but no AttackDefinition assigned in the inspector.", this);
            }
            return false;
        }

        /// <summary>
        /// Starts the DASH-ATTACK OPENER when configured and the player is inside the
        /// opener window (during the dash or within <see cref="dashAttackWindow"/> unscaled
        /// seconds after it). Returns false when there is no opener / step 0 / dash, or the
        /// window has passed — the caller then falls through to the normal roots.
        /// </summary>
        private bool TryStartDashAttack()
        {
            if (dashAttack == null || dash == null) return false;
            if (dash.SecondsSinceDash > dashAttackWindow) return false;
            if (!dashAttack.TryGetStep(0, out ComboStep _)) return false;
            StartCombo(dashAttack, true);
            return true;
        }

        private void UpdateCombo()
        {
            // Feed the buffer from this step onward (the pending input for the NEXT step).
            // The stick direction is classified at the PRESS frame (relative to the facing,
            // which is locked toward the target mid-combo) and carried with the buffer, so
            // a directional branch matches even if the stick is released before the window.
            if (LightPressedThisFrame())
            {
                chain.lightBuffer = inputBufferSeconds;
                chain.heavyBuffer = 0f; // LAST PRESS WINS: a branching tree resolves the newest input
                chain.bufferedDirection = ReadAttackDirection();
                BufferGrammarPress(ComboInput.Ligero, chain.bufferedDirection);
            }
            if (HeavyPressedThisFrame())
            {
                chain.heavyBuffer = inputBufferSeconds;
                chain.lightBuffer = 0f; // LAST PRESS WINS (see above)
                chain.bufferedDirection = ReadAttackDirection();
                BufferGrammarPress(ComboInput.Pesado, chain.bufferedDirection);
            }

            // NOTE: the step clock is advanced by chain.Tick in Update (same ordering as the
            // old inline `stepTime += Time.deltaTime`), so the grammar sees the clock already
            // advanced when it evaluates the window this frame.
            UpdateMidComboFacing();
            TryAdvanceOnBuffer();
            if (chain.stepTime >= EffectiveDuration(currentStep)) EndCombo(); // whiff end / finisher end
        }

        /// <summary>
        /// Buffers a press for the GRAMMAR path with the matrix' grace (T2.4, queue in T2.6).
        ///
        /// A press earlier than <c>c0 - BufferGrace</c> cannot possibly chain and is DISCARDED
        /// (no guessing); a press past the window's END is discarded too. A valid early press
        /// lives until the window CLOSES, so it fires the moment the window opens.
        ///
        /// T2.6: the press goes into the FIFO QUEUE, never over the pending ones. An invalid
        /// press drops ONLY itself — it MUST NOT wipe the presses already waiting (that was
        /// exactly the L→L→P bug: the P wiped the L).
        ///
        /// No GrammarEnabled branch: these fields are only READ by the grammar path, so filling
        /// them in V1 changes nothing.
        /// </summary>
        private void BufferGrammarPress(ComboInput input, AttackDirection variant)
        {
            CancelWindowBounds(out float c0, out float c1);
            StanceComboMatrix matrix = ActiveMatrix;
            float grace = matrix != null ? matrix.BufferGrace : 0f;
            float now = chain.stepTime;

            if (now < c0 - grace || now > c1) return; // invalid press: drop it, keep the queue

            chain.EnqueuePress(input, variant, Mathf.Max(0f, c1 - now));
        }

        /// <summary>
        /// If the buffered input matches a continuation (<c>siguiente</c> + <c>branches</c>)
        /// keyed by (input, direction) and the step is inside its cancel window, advance to
        /// the matching step. The window is WIDER when the step connected (hit-cancel).
        ///
        /// COMBO GRAMMAR (gate point 1 of 4): with the grammar ON the press resolves through
        /// the stance matrix ONLY, and the legacy resolver below is never consulted.
        /// </summary>
        private void TryAdvanceOnBuffer()
        {
            if (activeCombo == null) return;

            if (GrammarEnabled)
            {
                TryAdvanceOnGrammar();
                return;
            }

            CancelWindowBounds(out float c0, out float c1);
            if (chain.stepTime < c0) return;

            // B1 — NO STALE BUFFER: once the step is PAST its cancel window, or the
            // buffered input does NOT match a live continuation, the press is DEAD
            // (unmatched button). Otherwise it would survive into idle and start a
            // fresh attack (e.g. X then Y = one jab, then a Heavy).
            if (chain.stepTime > c1
                || !ComboResolver.TryResolveContinuation(
                    currentStep, chain.lightBuffer, chain.heavyBuffer, chain.bufferedDirection,
                    out ComboLink link))
            {
                chain.lightBuffer = 0f;
                chain.heavyBuffer = 0f;
                return;
            }

            chain.lightBuffer = 0f; // consume the tap (both — only one can match)
            chain.heavyBuffer = 0f;

            // OPENER -> NORMAL COMBO (slice 3octies): the dash-attack's continuation
            // resolves the NORMAL roots by the pressed input, so GolpeDeDash chains into
            // the standard combo (GolpeDeDash -> Golpe -> Golpe...). The root is resolved
            // with Neutral (input-only), which keeps the legacy Any-wildcard roots eligible.
            if (dashAttackActive)
            {
                dashAttackActive = false;
                ComboDefinition next = ComboResolver.SelectRoot(combos, link.input, AttackDirection.Neutral);
                if (next != null) { StartCombo(next); return; }
                EndCombo(); // no normal root for that input -> end the opener cleanly
                return;
            }

            AdvanceCombo(link.paso);
        }

        /// <summary>
        /// GRAMMAR advance (T2.4, QUEUE in T2.6): the ONE place a press becomes a chain link.
        ///
        /// ONE press = AT MOST ONE LOOKUP PER STEP, on the FRONT of the queue, and the lookup
        /// covers BOTH sources: the press must be inside the step's window (with the asset grace
        /// applied when it was buffered), then
        /// * a NEUTRAL press resolves FIRST against the authored progression of the running step
        ///   (<c>siguiente</c>, then <c>branches</c>) and only falls back to the matrix when the
        ///   asset offers no live link;
        /// * a DIRECTIONAL variant resolves ONLY through the matrix (exact variant, else Neutral
        ///   of the same input, else nothing) — it never reads the asset.
        /// A move already used in the chain memory is rejected; the slot budget has the last word.
        ///
        /// FALLO ≠ BORRADO: when the front press does not resolve it is RETAINED by
        /// <see cref="ComboChainState.FailFrontPress"/> (one step of life, discarded on the second
        /// failure). On a successful advance <see cref="ComboChainState.CarryPressesToNextStep"/>
        /// keeps the presses that were waiting BEHIND it and discards the ones that outlived more
        /// than one transition, so nothing stale can fire later.
        ///
        /// With NO matrix the grammar is still ON but there are no transitions: the chain stays
        /// ROOT-ONLY (expected, reversible regression until the prefab wiring lands in T3.1).
        /// </summary>
        private void TryAdvanceOnGrammar()
        {
            CancelWindowBounds(out float c0, out float c1);
            if (chain.stepTime < c0) return; // window not open yet: the presses wait in the queue

            if (chain.stepTime > c1)
            {
                chain.ClearBuffer(); // past the window: no stale press survives the step
                return;
            }

            if (grammarLookupSpent) return; // ONE lookup per step, already spent

            if (!chain.TryPeekPress(out ComboInput input, out AttackDirection variant)) return;

            StanceComboMatrix matrix = ActiveMatrix;
            if (matrix == null)
            {
                chain.ClearBuffer(); // root-only: no invented transition
                return;
            }

            grammarLookupSpent = true; // the step's single lookup is spent from here on

            ComboMoveRef current = CurrentMove;
            ComboMoveRef next = default;
            bool have;

            // NEUTRO = 1, no 0 (AttackDirection.Neutral, ComboDefinition.cs:48). El 0 es
            // AttackDirection.Any, que en el ASSET es wildcard (ComboLink.direction) pero en la
            // MATRIZ es un valor MUERTO: TryFindEntry compara el variant por igualdad exacta y el
            // único reintento es con Neutral, así que una entrada serializada con 0 nunca matchea.
            // Por eso el test es `== Neutral` y no `!= Any`.
            bool neutralFirst = variant == AttackDirection.Neutral
                                && !neutralPrefersLegacy   // R8: el kill switch restaura el camino previo
                                && !dashAttackActive;      // R1: el opener sigue root-only

            // R1 — el press neutro profundiza primero la progresión AUTORADA de ESTE paso
            // (`siguiente` → `branches`): el paso n avanza al n+1 del mismo asset sin fila nueva,
            // y por eso reautorar un combo no reabre este change. Las variantes direccionales NO
            // pasan por acá (R2): su única autoridad sigue siendo la matriz.
            if (neutralFirst
                && ComboResolver.TryResolveContinuation(in currentStep, input, variant, out ComboLink link))
            {
                // Link intra-asset: el paso destino pertenece al combo que ya está corriendo.
                next = new ComboMoveRef { combo = activeCombo, stepIndex = link.paso };
                have = true;
                // Si un guard lo rechaza, el press MUERE: no se reintenta contra la matriz. Ese
                // reintento sería una segunda lookup del mismo paso (R5) y podría cobrar dos slots.
            }
            else
            {
                // Segunda y ÚLTIMA fuente: la precedencia de siempre (exacta → Neutral de la fila).
                // También es la única fuente cuando el asset no ofrece link vivo (hoja, `paso: -1`,
                // input distinto) y cuando el neutro es el de una hoja que sí tiene fila (R3).
                have = matrix.TryResolve(in current, input, variant, out next);
            }

            // R4 — UN guard para asset y matriz. El `&&` corto-circuita: un destino rechazado no
            // cobra slot, y TryConsumeSlot queda como última validación y ÚNICA mutación previa a
            // ResolveFrontPress (R5: a lo sumo una vez por intento). TryGetStep descarta links
            // hacia pasos que ya no existen sin excepción (`paso: -1` ya lo filtró el resolver).
            bool resolved = have
                            && next.IsValid
                            && next.combo != null
                            && !next.Equals(in current)                 // dead target / self-link
                            && next.combo.TryGetStep(next.stepIndex, out ComboStep _)
                            && !chain.HasUsed(in next)                  // NO-REPEAT inside the chain
                            && chain.TryConsumeSlot();                  // chain budget spent

            if (!resolved)
            {
                chain.FailFrontPress(); // retain one step; the second failure drops it
                return;
            }

            chain.ResolveFrontPress();

            // A transition may live in ANOTHER combo asset (the matrix references moves, not
            // indices): adopt it before advancing so the cursor and the visual agree. The
            // dash-attack opener is a single entry move — leaving it means leaving its clip.
            if (next.combo != activeCombo)
            {
                activeCombo = next.combo;
                dashAttackActive = false;
            }

            AdvanceCombo(next.stepIndex);

            if (!comboActive) return; // AdvanceCombo closed the chain: EndCombo emptied the queue

            // Presses that were waiting BEHIND the resolved one survive THIS transition only,
            // re-anchored to the new step's window so they get their chance there.
            chain.CarryPressesToNextStep();
            CancelWindowBounds(out float _, out float nextC1);
            chain.RefreshPressLifetime(nextC1);
        }

        /// <summary>
        /// Reads the RAW Move stick THIS frame and classifies it into an
        /// <see cref="AttackDirection"/> RELATIVE TO THE FACING (follow-up slice): the
        /// stick is turned into a world direction (camera-relative) and projected onto
        /// the facing, so Back = away from the front. Zero / no input -> Neutral.
        /// </summary>
        private AttackDirection ReadAttackDirection()
        {
            return ClassifyAttackDirection();
        }

        /// <summary>
        /// Raw stick classification at THIS frame (relative to the facing). Used for
        /// buffered continuations, where a directional branch must still match even if
        /// the stick is held.
        /// </summary>
        private AttackDirection ClassifyAttackDirection()
        {
            if (input == null) return AttackDirection.Neutral;
            RefreshCamera(); // el basis cambia por sala: releerlo siempre
            return AttackDirectionClassifier.FromStick(
                input.Player.Move.ReadValue<Vector2>(), FacingVector(), cameraTransform);
        }

        /// <summary>
        /// Tracks how long the current stick direction has been held. A direction that
        /// just changed (or was just entered) has <see cref="heldDirectionTime"/> ~ 0,
        /// which is what marks a deliberate "flick" for root selection.
        /// </summary>
        private void UpdateDirectionHold()
        {
            AttackDirection current = ClassifyAttackDirection();
            if (current != heldDirection)
            {
                heldDirection = current;
                heldDirectionTime = 0f;
            }
            else
            {
                heldDirectionTime += Time.unscaledDeltaTime;
            }
        }

        /// <summary>
        /// Direction used to pick a combo ROOT. Walking (stick held longer than
        /// <see cref="PlayerParams.directionFlickWindow"/>) reads as Neutral so the
        /// neutral combo keeps chaining; a flick selects the directional variant.
        /// </summary>
        private AttackDirection ReadRootDirection()
        {
            AttackDirection current = ClassifyAttackDirection();
            if (current == AttackDirection.Neutral) return current;
            float flick = Params != null ? Params.directionFlickWindow : 0.22f;
            return heldDirectionTime <= flick ? current : AttackDirection.Neutral;
        }

        private void StartCombo(ComboDefinition definition, bool dashOpener = false)
        {
            if (definition == null || !definition.TryGetStep(0, out ComboStep first)) return;

            if (abilities != null) abilities.Stop(); // leaving any standalone ability (m1-abilities)
            activeCombo = definition;
            current = null;
            comboActive = true;
            dashAttackActive = dashOpener;
            chain.stepIndex = 0;
            currentStep = first;
            chain.stepTime = 0f;
            chain.connectedThisStep = false;
            grammarLookupSpent = false; // T2.6: a new step = a new (single) lookup
            // GRAMMAR (T2.4): a root starts a new chain SESSION. The move MEMORY survives —
            // it is cleared by the neutral timer / hit taken / target death / stance switch —
            // so a move used moments ago cannot be replayed inside the new chain either.
            chain.Begin(new ComboMoveRef { combo = definition, stepIndex = 0 }, ChainSlots);
            hitThisStep.Clear();
            hitThisSwing.Clear();
            ApplyAimAssist();
            ApplyStepLock(first);
            PlayAttackVisual(1, first);
        }

        private void AdvanceCombo(int index)
        {
            if (activeCombo == null || !activeCombo.TryGetStep(index, out ComboStep next))
            {
                EndCombo();
                return;
            }
            if (abilities != null) abilities.Stop(); // release a previous ability step (m1-abilities)
            chain.stepIndex = index;
            currentStep = next;
            chain.stepTime = 0f;
            chain.connectedThisStep = false;
            grammarLookupSpent = false; // T2.6: the new step gets its own single lookup
            chain.Push(new ComboMoveRef { combo = activeCombo, stepIndex = index }); // grammar memory
            hitThisStep.Clear();
            ApplyAimAssist();
            ApplyStepLock(next);
            PlayAttackVisual(index + 1, next);
        }

        private void EndCombo()
        {
            comboActive = false;
            dashAttackActive = false;
            chain.stepIndex = -1;
            activeCombo = null;
            currentStep = default;
            chain.stepTime = 0f;
            chain.connectedThisStep = false;
            grammarLookupSpent = false;
            hitThisStep.Clear();
            ClearAimAssist();
            // B1 — a combo end must never leak a buffered press into idle (the root
            // cause of "X then Y starts a fresh Heavy after the jab"). Clears the legacy
            // buttons AND the grammar buffer, so no ghost attack survives the close.
            chain.ClearBuffer();
            ReleaseAttackToken(); // release the attack token -> wish control returns
            if (visual is AnimatorVisual animatorVisual)
            {
                animatorVisual.ReleaseAttack();
                animatorVisual.ReleaseDashAttack(); // dormant opener slot never stays gated
                animatorVisual.ReleaseLauncher();   // dormant launcher slot never stays gated
            }
            if (abilities != null) abilities.Stop(); // release the ability slot (m1-abilities)
        }

        /// <summary>
        /// Per-step movement lock (decision: no drift during the swing). A locked
        /// step tells the motor to ignore the horizontal wish; the motor holds the
        /// facing. moveLock = false leaves full control (and skips the lunge).
        /// </summary>
        private void ApplyStepLock(in ComboStep step)
        {
            if (motor == null || motor.Body == null) return;
            if (step.moveLock)
            {
                // ONE token per combo. Release removes a SPECIFIC token, so acquiring a
                // fresh token on every moveLock step would orphan the earlier ones and
                // leave the body locked forever after EndCombo (deadlock).
                if (attackToken == null) attackToken = motor.Body.Acquire(BodyPriority.Attack);
            }
            else
            {
                ReleaseAttackToken();
            }
        }

        /// <summary>Releases the combo's attack token (idempotent).</summary>
        private void ReleaseAttackToken()
        {
            if (motor != null && motor.Body != null) motor.Body.Release(attackToken);
            attackToken = null;
        }

        /// <summary>
        /// Advances the character FORWARD along the current swing facing during a
        /// locked step, using the CURVED motion model: speed(t) decays over the
        /// step per <c>lungeCurve</c> (normalized 1 -> 0), and the integral of the
        /// speed over the effective window equals the total advance (≈
        /// <c>lunge</c> meters; strong impulse at the start, decaying). An empty
        /// curve defaults to the shared ease-out (<see cref="CurveMotion"/>). The
        /// motor's <see cref="PlayerMotor.InputLocked"/> preserves this velocity.
        /// A locked step with lunge = 0 is a full stop (no drift).
        /// </summary>
        private void ApplyLunge(in ComboStep step)
        {
            if (!step.moveLock) return;
            float window = EffectiveDuration(step);
            if (window <= 0f) return;

            // Committed direction (captured at step entry): re-orienting the facing between
            // steps must NOT drag the lunge of the step already running.
            Vector3 dir = lungeDir.sqrMagnitude > 0.0001f ? lungeDir : FacingVector();
            float advance = Mathf.Max(0f, step.lunge);
            float speed = Mathf.Max(0f, CurveMotion.Speed(step.lungeCurve, advance, window, chain.stepTime));
            Vector3 v = rb.linearVelocity;
            v.x = dir.x * speed;
            v.z = dir.z * speed;
            rb.linearVelocity = v;
        }

        /// <summary>
        /// Code-driven entry into the attack states (decision b): CrossFade to
        /// <c>Attack_P{n}</c> and push the dedicated param + the step's clip speed.
        /// Presentation-only: the generic proxy visual simply does nothing here.
        /// </summary>
        private void PlayAttackVisual(int step, ComboStep def)
        {
            if (!(visual is AnimatorVisual animatorVisual)) return;

            // ABILITY STEP (m1-abilities): the step delegates to PlayerAbilities (its own
            // Attack_H1/H2 state + hitbox). No Attack_P entry — the ability owns the visual.
            if (def.stepType == ComboStepType.Ability)
            {
                if (abilities != null && def.abilityRef != null) abilities.PlayFromCombo(def.abilityRef);
                return;
            }

            // LIVE tuning: the clip retime must equal the value EffectiveDuration divides by,
            // so the animation and the hit/cancel windows stay locked together at any scale.
            float clipSpeed = AttackSpeed(def);

            if (dashAttackActive)
            {
                // DASH-ATTACK opener: its OWN clip slot (Attack_Dash state). Enter it
                // ONLY when the step has an assigned clip (ComboStep.clip) — with no clip the
                // state would freeze the pose, so the visual is skipped and the opener
                // renders with whatever locomotion pose is live (no pose freeze).
                // The step's speedMultiplier rides in as the state's retime (slice 2).
                if (def.clip != null)
                    animatorVisual.PlayDashAttack(Mathf.Max(0f, crossFadeDuration), clipSpeed);
                return;
            }

            // LAUNCHER variant (slice 3nonies): its OWN clip slot (Attack_Launcher),
            // entered ONLY when the step has an assigned clip. With no clip it falls through to
            // the grey-box Attack_P mapping below (attackStateIndex) — never enters a
            // clip-less state (no pose freeze). speedMultiplier rides in as the retime (slice 2).
            if (def.launch && def.clip != null)
            {
                animatorVisual.PlayLauncher(Mathf.Max(0f, crossFadeDuration), clipSpeed);
                return;
            }

            // attackStateIndex > 0 overrides the auto index+1 so a 1-step
            // combo can play a deeper clip (Combo_H -> Attack_P3).
            int state = def.attackStateIndex > 0 ? def.attackStateIndex : step;
            animatorVisual.PlayAttack(state, Mathf.Max(0f, crossFadeDuration));
            animatorVisual.SetAttackSpeed(state, clipSpeed);
        }

        /// <summary>
        /// Debug readout: cancel-window START of the ACTIVE step, normalized (0..1), or
        /// -1 when no combo step is running. Pre-scale value (the live divisor is applied
        /// by <see cref="CancelWindowBounds"/>), so the overlay can show both.
        /// </summary>
        public float ActiveCancelStartNormalized =>
            comboActive
                ? Mathf.Clamp01((chain.connectedThisStep ? currentStep.cancelHitNorm : currentStep.cancelNorm).x)
                : -1f;

        /// <summary>
        /// The active step's cancel window in SCALED seconds (wider when the step
        /// connected). Shared by the buffered advance and the mid-combo facing re-orient.
        ///
        /// COMBO GRAMMAR (gate point 2 of 4): with the grammar ON the window is ASYMMETRIC and
        /// ABSOLUTE — it opens at the real impact (<c>hitNorm.y × dur</c>) and lasts the asset's
        /// ConnectWindow (connected) or WhiffWindow (whiff/blocked). <c>cancelNorm</c>,
        /// <c>cancelHitNorm</c> and <c>cancelStartScale</c> are IGNORED there; they still rule V1.
        /// </summary>
        private void CancelWindowBounds(out float c0, out float c1)
        {
            if (GrammarEnabled)
            {
                CancelWindowBoundsGrammar(out c0, out c1);
                return;
            }

            Vector2 window = chain.connectedThisStep ? currentStep.cancelHitNorm : currentStep.cancelNorm;
            float duration = EffectiveDuration(currentStep);
            // LIVE tuning: only the START moves (window.x / cancelStartScale), so > 1
            // opens the cancel EARLIER; the END stays on the data. Never invert.
            c0 = Mathf.Clamp01(Mathf.Clamp01(window.x) / Mathf.Max(0.01f, CancelStartScale)) * duration;
            c1 = Mathf.Clamp01(window.y) * duration;
            c0 = Mathf.Min(c0, c1);
        }

        /// <summary>
        /// GRAMMAR window: <c>c0 = anchor</c> (the ACTUAL impact, <c>hitNorm.y × duration</c>),
        /// <c>c1 = min(duration, anchor + len)</c>, where len is the matrix' connect window when
        /// the step CONNECTED and its whiff window otherwise (Blocked behaves as whiff).
        /// The windows are absolute seconds, so raising the style cadence shortens the step and
        /// can clamp <c>c1</c> against its end — never producing an inverted window.
        /// </summary>
        private void CancelWindowBoundsGrammar(out float c0, out float c1)
        {
            float duration = EffectiveDuration(currentStep);
            float anchor = Mathf.Clamp01(currentStep.hitNorm.y) * duration;

            StanceComboMatrix matrix = ActiveMatrix;
            float length = matrix != null
                ? (chain.Outcome == HitOutcome.Hit ? matrix.ConnectWindow : matrix.WhiffWindow)
                : 0f;

            c0 = anchor;
            c1 = Mathf.Min(duration, anchor + Mathf.Max(0f, length));
            c0 = Mathf.Min(c0, c1);
        }

        /// <summary>True while the active step is inside its cancel window (scaled time).</summary>
        private bool IsInCancelWindow()
        {
            CancelWindowBounds(out float c0, out float c1);
            return chain.stepTime >= c0 && chain.stepTime <= c1;
        }

        /// <summary>
        /// Effective step duration in SCALED seconds. The clip plays at
        /// <c>speedMultiplier</c> (Animator state speed, decision c) x the LIVE
        /// <c>attackSpeedScale</c>, so the hit window, the cancel windows and the
        /// step end must be measured against
        /// <c>duracion / (speedMultiplier x scale)</c> — NOT the raw clip length —
        /// or the logic would outlive the accelerated clip by a full speed factor.
        /// Instance method (not static): the duration now depends on the live tuning.
        /// </summary>
        private float EffectiveDuration(in ComboStep step)
        {
            return step.duracion / AttackSpeed(step);
        }

        private void StartSwing(AttackDefinition def)
        {
            current = def;
            swingTime = 0f;
            hitThisSwing.Clear();
            hitThisStep.Clear(); // standalone swings must not inherit the previous swing's victims
            ApplyAimAssist();
        }

        private void EndSwing()
        {
            current = null;
            swingTime = 0f;
            hitThisSwing.Clear();
            hitThisStep.Clear(); // next swing starts with a clean victim set
            ClearAimAssist();
        }
    }
}
