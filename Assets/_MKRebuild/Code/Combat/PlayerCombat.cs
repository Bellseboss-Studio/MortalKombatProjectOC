using System;
using System.Collections.Generic;
using UnityEngine;

namespace MK.Rebuild
{
    /// <summary>
    /// M1 slice-2 combat controller: data-driven COMBO chaining on top of the
    /// slice-1 grey-box engine. Reads the generated <see cref="PlayerInput"/>
    /// actions and either runs the single grey-box attack (<see cref="AttackDefinition"/>,
    /// kept as the standalone fallback) or a <see cref="ComboDefinition"/> chain:
    /// one combo at a time, steps advanced by input within the step's cancel window.
    ///
    /// Combo flow (decisions b/d/f): a 0.15 s UNSCALED input buffer (jumpBuffer
    /// pattern) keyed by button; the controller drives the REAL attack clips via
    /// <c>Animator.CrossFade</c> to the <c>Attack_P{n}</c> states pushed through the
    /// dedicated <c>AttackStep</c> param (NEVER the MoveState enum — the AnyState
    /// <c>MoveState != 0</c> would hijack new values). Hits are timed by normalized
    /// windows (<c>hitNorm × duracion / speedMultiplier</c>), NEVER by Animation Events. The step ends
    /// on whiff (no buffered input in whiff window) or on the finisher
    /// (<c>siguiente.paso == -1</c>): both release <c>AttackStep = 0</c> and let the
    /// controller's landing-mirror exits return to locomotion.
    ///
    /// Hit-cancel: <c>cancelHitNorm</c> (wider) applies when the step connected;
    /// <c>cancelNorm</c> (narrower) on whiff. Hitstop (decision h): saves and
    /// restores the CURRENT <c>Time.timeScale</c>, never hardcodes 1 (debug F4).
    /// Swing timing is SCALED (hitstop freezes the swing); the hitstop countdown,
    /// the input buffer and the victim feedback run UNSCALED.
    ///
    /// Dash-cancel (slice 3septies): an attack pressed while the DASH holds the movement
    /// lock does NOT wait for the dash to end. Past <c>PlayerDash.minCancelTime</c> it calls
    /// <see cref="PlayerDash.TryCancelByAttack"/>, cuts the dash short and starts the attack
    /// the SAME frame (dash-cancel into attack). Before that window the press is buffered
    /// (held while the lock) and fires at the dash-end handoff — the 3sexies path is the fallback.
    ///
    /// Dash-attack opener (slice 3octies): an attack DURING a dash or within
    /// <c>dashAttackWindow</c> after it runs the dedicated <c>dashAttack</c> combo FIRST
    /// (its OWN clip slot + hit data + lunge); its <c>siguiente</c> then chains into the
    /// NORMAL combo roots (GolpeDeDash -> Golpe -> Golpe...). Outside the window the attack
    /// is normal. The opener's visual is a DORMANT slot (Attack_Dash state), entered only
    /// once a clip is assigned to the step — with no clip it never freezes the pose.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody))]
    public sealed class PlayerCombat : MonoBehaviour
    {
        [Tooltip("Light attack definition (gamepad X / mouse left). Fallback when no combo or the combo does not start on light.")]
        [SerializeField] private AttackDefinition lightAttack;

        [Tooltip("Heavy attack definition (gamepad Y / mouse right). Fallback when no combo or the combo does not start on heavy.")]
        [SerializeField] private AttackDefinition heavyAttack;

        [Tooltip("Combo chains (data-driven, multi-root). Each combo's root is its step 0 input; when several combos match a press, the LONGEST chain wins (input-prefix rule): L→Combo_1 (L→L→P), P→Combo_H, etc. Any combo can be tested alone by assigning only it.")]
        [SerializeField] private ComboDefinition[] combos = new ComboDefinition[0];

        [Tooltip("DASH-ATTACK OPENER (slice 3octies): a dedicated combo whose FIRST step is the attack that comes out of a dash (its OWN clip slot + hit data + lunge). When an attack starts during a dash or within dashAttackWindow after it, this runs FIRST; its 'siguiente' then chains into the NORMAL combo roots (GolpeDeDash -> Golpe -> Golpe...). Null = no opener (attacks always use the normal roots).")]
        [SerializeField] private ComboDefinition dashAttack;

        [Tooltip("Unscaled SECONDS after a dash during which the first attack counts as the dash-attack opener. 0 = only while the dash runs. Default 0.35 ~ the dash duration.")]
        [SerializeField] private float dashAttackWindow = 0.35f;

        [Tooltip("Soft trajectory corrector (aim-assist): at attack entry, turn the facing toward the nearest alive target inside the frontal cone + range. Drags lunge + hitbox (single point: visual.Facing). Null/disabled = forward-only (current behavior).")]
        [SerializeField] private AimAssistDefinition aimAssist;

        // Active combo (the one currently running), resolved by SelectRoot at entry.
        private ComboDefinition activeCombo;

        // Dash-attack opener state (slice 3octies): true while the active combo IS the
        // opener. PlayAttackVisual routes to the dormant Attack_Dash clip slot, and
        // TryAdvanceOnBuffer chains into the NORMAL combo roots instead of by index.
        private bool dashAttackActive;

        [Tooltip("CrossFade duration (seconds) used to enter every attack state (decision b, ~0.1).")]
        [SerializeField] private float crossFadeDuration = 0.1f;

        [Tooltip("Input buffer in SECONDS (decision d, 0.15). UNSCALED: survives hitstop timeScale = 0.")]
        [SerializeField] private float inputBufferSeconds = 0.15f;

        private PlayerInput input;
        private Rigidbody rb;
        private ICharacterVisual visual;
        private PlayerMotor motor;
        private PlayerDash dash;
        private Transform cameraTransform; // reference frame for the facing-relative stick

        // One-shot taps submitted out-of-band (editor smoke driver / future
        // menu or demo hooks). Consumed by the next Update as if pressed this frame.
        private bool queuedLightTap;
        private bool queuedHeavyTap;
        private bool queuedDashTap;

        // Grey-box fallback swing (slice 1, one at a time).
        private AttackDefinition current;
        private float swingTime;                 // scaled seconds into the swing
        private readonly HashSet<HitReceiver> hitThisSwing = new HashSet<HitReceiver>();

        // Aim-assist runtime (m1-aim-assist): the XZ direction to turn toward at
        // attack entry + the seconds left of the entry correction window. Zero
        // direction = no active correction. SetFacing is incremental, so the
        // corrector ITERATES over the swing opening instead of one snap.
        private Vector3 aimDir;
        private float aimCorrectRemaining;

        // Within-step lunge COMMIT (target-switch iteration): the world XZ direction the
        // active step's lunge was committed to at step entry (the soft-locked target, or
        // the facing when there was none). ApplyLunge uses THIS instead of the live facing
        // so the body can re-orient between steps without the lunge drifting mid-step.
        private Vector3 lungeDir;

        // Combo state (decision b: one combo at a time).
        private bool comboActive;
        private int stepIndex = -1;             // index into combo.steps
        private ComboStep currentStep;          // struct copy of the active step
        private float stepTime;                 // scaled seconds into the step
        private bool connectedThisStep;         // hit landed this step -> wider cancel
        private readonly HashSet<HitReceiver> hitThisStep = new HashSet<HitReceiver>();

        // Input taps (unscaled timers, jumpBuffer pattern keyed by button).
        private float lightBuffer;
        private float heavyBuffer;

        // Direction (relative to the facing) classified at the PRESS frame and carried
        // with the buffer, so a mid-combo branch matches the direction the player pressed
        // even if the stick is released before the cancel window opens (follow-up slice).
        private AttackDirection bufferedDirection;

        // Hitstop state.
        private float hitstopRemaining;          // unscaled seconds left frozen
        private float savedTimeScale;            // the EXACT previous value to restore

        private bool warnedMissingDefinition;

        /// <summary>True while a swing or a combo step is active (read-only debug seam).</summary>
        public bool Attacking => current != null || comboActive;

        /// <summary>Definition of the active grey-box swing, null when idle (read-only debug seam).</summary>
        public AttackDefinition CurrentAttack => current;

        /// <summary>Index of the active combo step, -1 when not in a combo (read-only debug seam).</summary>
        public int CurrentStepIndex => stepIndex;

        /// <summary>True while the active combo is the DASH-ATTACK opener (read-only debug seam).</summary>
        public bool DashAttackActive => dashAttackActive;

        /// <summary>
        /// True while the hitstop freeze is active (timeScale = 0). Seam for the dash:
        /// "dash cancela TODO salvo hitstop propio" — the dash must NOT fire during the
        /// freeze (decision f guard).
        /// </summary>
        public bool IsHitstopped => hitstopRemaining > 0f;

        /// <summary>
        /// Aim-assist tuning used by the ATTACK entry (read-only debug seam). The dash
        /// has its OWN definition now (<c>PlayerDash.dashAimAssist</c>), so the attack and
        /// the dash tune their range/cone independently. Null / disabled = forward-only.
        /// </summary>
        public AimAssistDefinition AimAssistDef => aimAssist;

        /// <summary>
        /// Style-meter seam (slice style-meter): fired ONCE per newly connected
        /// receiver with the move-key <c>comboAssetName#stepIndex</c> (e.g.
        /// <c>Combo_1#2</c>, opener <c>Combo_DashAttack#0</c>, grey-box fallback
        /// <c>Attack_Light#0</c>). Additive — no behavior change; nobody listening
        /// means nobody notices.
        /// </summary>
        public event Action<string> OnStepConnected;

        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
            visual = GetComponent<ICharacterVisual>();
            motor = GetComponent<PlayerMotor>();
            dash = GetComponent<PlayerDash>();
            input = new PlayerInput();
            RefreshCamera();
        }

        private void RefreshCamera()
        {
            var cam = Camera.main;
            cameraTransform = cam != null ? cam.transform : null;
        }

        private void OnEnable()
        {
            if (input == null) input = new PlayerInput();
            input.Player.Enable();
        }

        private void OnDisable()
        {
            if (input != null) input.Player.Disable();
            if (motor != null) motor.InputLocked = false; // never leave a stale attack lock
        }

        private void OnDestroy()
        {
            if (input != null) input.Dispose();
        }

        private void Update()
        {
            // Hitstop owns the frame: consume unscaled time, restore the PREVIOUS
            // timeScale on expiry, and ignore new input while frozen. The input
            // buffers do NOT decay here either — they survive the freeze (decision d).
            if (hitstopRemaining > 0f)
            {
                hitstopRemaining -= Time.unscaledDeltaTime;
                if (hitstopRemaining <= 0f)
                {
                    Time.timeScale = savedTimeScale;
                    hitstopRemaining = 0f;
                }
                return;
            }

            // Unscaled buffer decay (jumpBuffer pattern): a tap survives ~0.15 s.
            // EXCEPT while the DASH holds the movement lock (idle + InputLocked + no combo):
            // the attack pressed mid-dash is HELD (no decay) so it fires the instant the dash
            // ends — dash -> attack -> combo. Combo steps are UNAFFECTED: their own 0.15 s
            // step buffer keeps decaying (comboActive skips this hold).
            bool dashHoldsInput = motor != null && motor.InputLocked && !comboActive;
            if (!dashHoldsInput)
            {
                if (lightBuffer > 0f) lightBuffer = Mathf.Max(0f, lightBuffer - Time.unscaledDeltaTime);
                if (heavyBuffer > 0f) heavyBuffer = Mathf.Max(0f, heavyBuffer - Time.unscaledDeltaTime);
            }

            UpdateAimCorrection();

            if (comboActive) UpdateCombo();
            else UpdateIdle();
        }

        private void FixedUpdate()
        {
            if (hitstopRemaining > 0f) return;
            if (comboActive)
            {
                ApplyLunge(currentStep);
                if (IsInsideStepWindow()) ProbeHitboxStep();
                return;
            }
            if (current == null) return;
            if (IsInsideActiveWindow()) ProbeHitbox();
        }

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

            // While the dash owns the movement lock, combat cannot normally START an attack
            // (its lunge would fight the dash velocity). Two paths (slice 3septies):
            //   1. EARLY CANCEL: an attack pressed past the dash's min cancel window CUTS the
            //      dash short (PlayerDash.TryCancelByAttack) and starts the attack this SAME
            //      frame — no waiting for the dash to end (dash-cancel into attack).
            //   2. FALLBACK: before that window (or with no dash) the press is BUFFERED (held,
            //      not decayed) and fires when PlayerDash releases the lock at dash end.
            if (motor != null && motor.InputLocked)
            {
                bool lightPressed = LightPressedThisFrame();
                bool heavyPressed = HeavyPressedThisFrame();

                if ((lightPressed || heavyPressed) && dash != null && dash.TryCancelByAttack())
                {
                    if (TryStartAttack(lightPressed, heavyPressed)) return;
                }

                if (lightPressed) lightBuffer = inputBufferSeconds;
                if (heavyPressed) heavyBuffer = inputBufferSeconds;
                return;
            }

            // Consume either the direct press THIS frame OR a tap buffered during the dash.
            bool light = LightPressedThisFrame() || lightBuffer > 0f;
            bool heavy = HeavyPressedThisFrame() || heavyBuffer > 0f;
            if (light) lightBuffer = 0f;
            if (heavy) heavyBuffer = 0f;

            TryStartAttack(light, heavy);
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
            // DIRECTIONAL VARIANT (slice 3nonies, decision a/f): read the RAW stick in
            // the FRAME OF THE PRESS and classify it. Done HERE, before StartCombo ->
            // ApplyAimAssist: the direction picks WHICH attack (root), the aim-assist
            // then corrects WHICH WAY the body faces (different axes, no conflict).
            AttackDirection direction = ReadAttackDirection();

            // DASH-ATTACK OPENER (slice 3octies): an attack during the dash or within
            // dashAttackWindow after it uses the dedicated dashAttack FIRST. Its
            // continuation chains into the normal combo roots (TryAdvanceOnBuffer).
            if ((light || heavy) && TryStartDashAttack()) return true;

            // Combo entry (multi-root): pick the longest chain whose root matches
            // the pressed input AND the stick direction (exact > Any wildcard); a
            // lone press ends that chain's single step cleanly while a continuation
            // chains. No match -> grey-box standalone swing.
            if (light)
            {
                ComboDefinition c = SelectRoot(ComboInput.Ligero, direction);
                if (c != null) { StartCombo(c); return true; }
            }
            if (heavy)
            {
                ComboDefinition c = SelectRoot(ComboInput.Pesado, direction);
                if (c != null) { StartCombo(c); return true; }
            }

            if (light && lightAttack != null) { StartSwing(lightAttack); return true; }
            if (heavy && heavyAttack != null) { StartSwing(heavyAttack); return true; }

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
                lightBuffer = inputBufferSeconds;
                bufferedDirection = ReadAttackDirection();
            }
            if (HeavyPressedThisFrame())
            {
                heavyBuffer = inputBufferSeconds;
                bufferedDirection = ReadAttackDirection();
            }

            // Scaled time: hitstop (timeScale = 0) freezes the step mid-window.
            stepTime += Time.deltaTime;
            UpdateMidComboFacing();
            TryAdvanceOnBuffer();
            if (stepTime >= EffectiveDuration(currentStep)) EndCombo(); // whiff end / finisher end
        }

        // ------------------------------------------------------------------
        // Input seam: same path for the real actions and out-of-band taps.
        // ------------------------------------------------------------------

        /// <summary>
        /// Public tap seam (editor smoke driver / menu / demo hooks): the next
        /// Update treats this exactly like a LightAttack press. Real input is
        /// untouched — the action read is OR-ed with the queued tap.
        /// </summary>
        public void SubmitLightAttack() => queuedLightTap = true;

        /// <summary>Public tap seam for HeavyAttack (gamepad Y / mouse right).</summary>
        public void SubmitHeavyAttack() => queuedHeavyTap = true;

        /// <summary>
        /// Public tap seam for Dash (decision e): the next <see cref="ConsumeDashTap"/>
        /// read treats it exactly like a Dash press. <see cref="PlayerDash"/> owns the
        /// dash logic and reads through this pipeline so editor/menu/demo taps and the
        /// real action share one path.
        /// </summary>
        public void SubmitDash() => queuedDashTap = true;

        /// <summary>
        /// Dash press reader shared with <see cref="PlayerDash"/>: real Dash action OR
        /// the queued <see cref="SubmitDash"/> tap. The queued tap is consumed exactly
        /// once (one consumer — the dash component).
        /// </summary>
        public bool ConsumeDashTap()
        {
            bool tap = queuedDashTap;
            queuedDashTap = false;
            return tap || (input != null && input.Player.Dash.WasPressedThisFrame());
        }

        /// <summary>
        /// Ability input seam (decision b/e): entry point for H1/H2/Launcher as combo
        /// step inputs. PREPARE-only in slice 3 (abilities deferred): no projectile,
        /// no spin, no multi-hit — the seam exists so the future AbilitySystem plugs
        /// in without touching the input layer.
        /// </summary>
        public void SubmitAbility(ComboInput ability)
        {
            // Deliberately empty in this slice: ability behavior is deferred by scope.
        }

        /// <summary>
        /// Aborts the active combo/swing and releases the attack lock (dash-cancel
        /// seam, decision a: "dash cancela TODO salvo hitstop propio"). No-op when
        /// idle. Does NOT touch hitstop state — the caller decides whether the dash
        /// may fire (<see cref="PlayerDash"/> gates on <see cref="IsHitstopped"/>).
        /// </summary>
        public void CancelCombo()
        {
            if (comboActive) EndCombo();
            if (current != null) EndSwing();
            if (motor != null) motor.InputLocked = false; // belt-and-braces: never leave a stale lock
            lightBuffer = 0f;
            heavyBuffer = 0f;
        }

        private bool LightPressedThisFrame()
        {
            bool tap = queuedLightTap;
            queuedLightTap = false;
            return tap || (input != null && input.Player.LightAttack.WasPressedThisFrame());
        }

        private bool HeavyPressedThisFrame()
        {
            bool tap = queuedHeavyTap;
            queuedHeavyTap = false;
            return tap || (input != null && input.Player.HeavyAttack.WasPressedThisFrame());
        }

        /// <summary>
        /// If the buffered input matches a continuation (<c>siguiente</c> + <c>branches</c>)
        /// keyed by (input, direction) and the step is inside its cancel window, advance to
        /// the matching step. The window is WIDER when the step connected (hit-cancel).
        /// </summary>
        private void TryAdvanceOnBuffer()
        {
            if (activeCombo == null) return;

            CancelWindowBounds(out float c0, out float c1);
            if (stepTime < c0 || stepTime > c1) return;

            // Continuation keyed by (input, direction): the scalar 'siguiente' plus any
            // extra 'branches'. A finisher (paso < 0) is not a candidate, so a step with
            // no live matching link simply ends as before.
            if (!TryResolveContinuation(currentStep, out ComboLink link)) return;

            lightBuffer = 0f; // consume the tap (both — only one can match)
            heavyBuffer = 0f;

            // OPENER -> NORMAL COMBO (slice 3octies): the dash-attack's continuation
            // resolves the NORMAL roots by the pressed input, so GolpeDeDash chains into
            // the standard combo (GolpeDeDash -> Golpe -> Golpe...). The root is resolved
            // with Neutral (input-only), which keeps the legacy Any-wildcard roots eligible.
            if (dashAttackActive)
            {
                dashAttackActive = false;
                ComboDefinition next = SelectRoot(link.input, AttackDirection.Neutral);
                if (next != null) { StartCombo(next); return; }
                EndCombo(); // no normal root for that input -> end the opener cleanly
                return;
            }

            AdvanceCombo(link.paso);
        }

        /// <summary>
        /// Resolves the continuation of <paramref name="step"/> matching a LIVE buffered
        /// input AND the buffered (facing-relative) direction: among the scalar
        /// <c>siguiente</c> and the extra <c>branches</c>, picks the link whose input has
        /// a buffer and whose direction is EXACT or <see cref="AttackDirection.Any"/>
        /// (wildcard). Exact beats wildcard; ties keep the first (siguiente, then the
        /// branches in list order). Links with <c>paso &lt; 0</c> (finisher) are skipped,
        /// as is a non-attack input (no press edge in this slice). False when nothing
        /// matches.
        /// </summary>
        private bool TryResolveContinuation(ComboStep step, out ComboLink best)
        {
            ComboLink result = default;
            bool found = false;
            bool bestExact = false;

            Consider(step.siguiente);
            if (step.branches != null)
            {
                for (int i = 0; i < step.branches.Count; i++) Consider(step.branches[i]);
            }

            best = result;
            return found;

            void Consider(ComboLink link)
            {
                if (link.paso < 0) return; // finisher: not an in-combo continuation
                bool live = link.input == ComboInput.Ligero ? lightBuffer > 0f
                          : link.input == ComboInput.Pesado ? heavyBuffer > 0f
                          : false; // H1/H2/Launcher/Dash: no press edge in this slice
                if (!live) return;

                bool exact = link.direction != AttackDirection.Any && link.direction == bufferedDirection;
                bool wildcard = link.direction == AttackDirection.Any;
                if (!exact && !wildcard) return;

                if (!found || (exact && !bestExact)) // exact beats wildcard; else first wins
                {
                    result = link;
                    found = true;
                    bestExact = exact;
                }
            }
        }

        /// <summary>
        /// Root resolution (input-prefix + directional variant rule, multi-combo):
        /// among <see cref="combos"/> whose first step input matches
        /// <paramref name="pressed"/> AND whose step-0 direction is
        /// <paramref name="direction"/> (exact) OR <see cref="AttackDirection.Any"/>
        /// (wildcard), pick the LONGEST chain so a lone press still ends cleanly at
        /// step 0 while a continuation chains. An EXACT direction always beats a
        /// wildcard; ties (same exactness, same length) keep the first in the array.
        /// Null when nothing matches.
        /// </summary>
        private ComboDefinition SelectRoot(ComboInput pressed, AttackDirection direction)
        {
            ComboDefinition best = null;
            int bestLen = -1;
            bool bestExact = false;
            if (combos == null) return null;
            for (int i = 0; i < combos.Length; i++)
            {
                ComboDefinition c = combos[i];
                if (c == null || !c.TryGetStep(0, out ComboStep s0)) continue;
                if (s0.input != pressed) continue;

                bool exact = s0.direction == direction;
                bool wildcard = s0.direction == AttackDirection.Any;
                if (!exact && !wildcard) continue;

                bool better = best == null
                    || (exact && !bestExact)                       // exact beats wildcard
                    || (exact == bestExact && c.Length > bestLen); // then longest chain
                if (better) { best = c; bestLen = c.Length; bestExact = exact; }
            }
            return best;
        }

        /// <summary>
        /// Reads the RAW Move stick THIS frame and classifies it into an
        /// <see cref="AttackDirection"/> RELATIVE TO THE FACING (follow-up slice): the
        /// stick is turned into a world direction (camera-relative) and projected onto
        /// the facing, so Back = away from the front. Zero / no input -> Neutral.
        /// </summary>
        private AttackDirection ReadAttackDirection()
        {
            if (input == null) return AttackDirection.Neutral;
            if (cameraTransform == null) RefreshCamera();
            return AttackDirectionClassifier.FromStick(
                input.Player.Move.ReadValue<Vector2>(), FacingVector(), cameraTransform);
        }

        private void StartCombo(ComboDefinition definition, bool dashOpener = false)
        {
            if (definition == null || !definition.TryGetStep(0, out ComboStep first)) return;
            activeCombo = definition;
            current = null;
            comboActive = true;
            dashAttackActive = dashOpener;
            stepIndex = 0;
            currentStep = first;
            stepTime = 0f;
            connectedThisStep = false;
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
            stepIndex = index;
            currentStep = next;
            stepTime = 0f;
            connectedThisStep = false;
            hitThisStep.Clear();
            ApplyAimAssist();
            ApplyStepLock(next);
            PlayAttackVisual(index + 1, next);
        }

        private void EndCombo()
        {
            comboActive = false;
            dashAttackActive = false;
            stepIndex = -1;
            activeCombo = null;
            currentStep = default;
            stepTime = 0f;
            connectedThisStep = false;
            hitThisStep.Clear();
            ClearAimAssist();
            if (motor != null) motor.InputLocked = false; // release the attack lock -> wish control returns
            if (visual is AnimatorVisual animatorVisual)
            {
                animatorVisual.ReleaseAttack();
                animatorVisual.ReleaseDashAttack(); // dormant opener slot never stays gated
                animatorVisual.ReleaseLauncher();   // dormant launcher slot never stays gated
            }
        }

        /// <summary>
        /// Per-step movement lock (decision: no drift during the swing). A locked
        /// step tells the motor to ignore the horizontal wish; the motor holds the
        /// facing. moveLock = false leaves full control (and skips the lunge).
        /// </summary>
        private void ApplyStepLock(in ComboStep step)
        {
            if (motor != null) motor.InputLocked = step.moveLock;
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
            float speed = Mathf.Max(0f, CurveMotion.Speed(step.lungeCurve, advance, window, stepTime));
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

            if (dashAttackActive)
            {
                // DASH-ATTACK opener: its OWN clip slot (Attack_Dash state). Enter it
                // ONLY when the step has an assigned clip (ComboStep.clip) — with no clip the
                // state would freeze the pose, so the visual is skipped and the opener
                // renders with whatever locomotion pose is live (no pose freeze).
                // The step's speedMultiplier rides in as the state's retime (slice 2).
                if (def.clip != null)
                    animatorVisual.PlayDashAttack(Mathf.Max(0f, crossFadeDuration), def.speedMultiplier);
                return;
            }

            // LAUNCHER variant (slice 3nonies): its OWN clip slot (Attack_Launcher),
            // entered ONLY when the step has an assigned clip. With no clip it falls through to
            // the grey-box Attack_P mapping below (attackStateIndex) — never enters a
            // clip-less state (no pose freeze). speedMultiplier rides in as the retime (slice 2).
            if (def.launch && def.clip != null)
            {
                animatorVisual.PlayLauncher(Mathf.Max(0f, crossFadeDuration), def.speedMultiplier);
                return;
            }

            // attackStateIndex > 0 overrides the auto index+1 so a 1-step
            // combo can play a deeper clip (Combo_H -> Attack_P3).
            int state = def.attackStateIndex > 0 ? def.attackStateIndex : step;
            animatorVisual.PlayAttack(state, Mathf.Max(0f, crossFadeDuration));
            animatorVisual.SetAttackSpeed(state, def.speedMultiplier);
        }

        // ------------------------------------------------------------------
        // Aim-assist (m1-aim-assist, decisions a/b/e): soft trajectory corrector.
        // At attack entry, pick the nearest alive HitReceiver inside the frontal
        // cone + range and turn visual.Facing toward it over the swing opening.
        // Facing is the SINGLE point: ApplyLunge (reads FacingVector) and
        // HitboxOrigin both follow -> lunge + hitbox redirect with NO motor change.
        // ------------------------------------------------------------------

        /// <summary>
        /// Computes the aim direction ONCE at attack entry (NOT continuous homing):
        /// nearest alive target inside the ±coneHalfAngle frontal cone and within
        /// [minRange, maxRange] METERS. On a hit, stores the direction and starts the
        /// entry correction window (worst-case cone error / turnSpeed). No target or
        /// disabled -> no-op (forward-only behavior intact). Applies in air/wall too.
        /// </summary>
        private void ApplyAimAssist()
        {
            ClearAimAssist();
            // Default lunge commit: the facing this step starts on. Overridden below with
            // the soft-locked target direction when one is found.
            lungeDir = FacingVector();
            if (visual == null) return;

            // Shared selection (slice 3quinquies, Code/Combat/AimAssist.cs): the dash
            // direction uses the EXACT same cone + range + nearest-alive-target math.
            // CONE AXIS = the player's AIM (target-switch iteration): the cone is centered
            // where the player POINTS (camera-relative stick), so pointing at another enemy
            // selects it (the nearest enemy inside THAT cone). Neutral stick = the current
            // facing, i.e. the pre-existing behavior.
            if (!AimAssist.TryFindAimDir(transform.position, rb, AimAxis(), aimAssist, out Vector3 aim))
                return;

            aimDir = aim;

            // SetFacing is incremental (~12°/frame at 720°/s): a single call cannot
            // align. Iterate over the swing opening — window sized to the worst-case
            // cone error at the configured turn speed.
            aimCorrectRemaining = Mathf.Clamp(aimAssist.coneHalfAngleDeg, 0f, 180f)
                / Mathf.Max(1f, aimAssist.turnSpeedDegPerSec);
            // The lunge is COMMITTED to the chosen target for the whole step, so the
            // between-steps re-orientation below never drags it mid-step.
            lungeDir = aim;
            visual.SetFacing(aimDir);
        }

        /// <summary>
        /// Iterates <c>visual.SetFacing</c> toward the stored aim direction during the
        /// swing OPENING (a smooth turn), then stops — NOT continuous homing. Scaled
        /// time: hitstop freezes the correction with the rest of the swing.
        /// </summary>
        private void UpdateAimCorrection()
        {
            if (aimCorrectRemaining <= 0f) return;
            aimCorrectRemaining -= Time.deltaTime;
            if (visual != null) visual.SetFacing(aimDir);
        }

        private void ClearAimAssist()
        {
            aimDir = Vector3.zero;
            aimCorrectRemaining = 0f;
        }

        /// <summary>
        /// MID-COMBO facing re-orientation (target-switch, DMC/Bayonetta feel): while the
        /// active step is PAST its strike (hit window closed) and inside its cancel window,
        /// keep turning the facing toward the player's AIM + soft-lock — the nearest alive
        /// enemy inside the cone centered on the stick. This is what lets the player swing
        /// the body toward ANOTHER enemy mid-combo so the NEXT step's hit goes there. The
        /// lunge stays committed (<see cref="ApplyLunge"/> uses <see cref="lungeDir"/>), so
        /// the body pivots without drifting. It only turns when an enemy is soft-locked:
        /// free-facing the raw stick would rotate the facing under the player's own "Back"
        /// input and break the facing-relative launcher. SetFacing is incremental, so
        /// calling it every frame converges over the window.
        /// </summary>
        private void UpdateMidComboFacing()
        {
            if (visual == null) return;
            if (aimCorrectRemaining > 0f) return; // the entry correction owns the opening
            if (IsInsideStepWindow()) return;     // committed through the strike
            if (!IsInCancelWindow()) return;      // only between steps / in the chain tail

            if (!AimAssist.TryFindAimDir(transform.position, rb, AimAxis(), aimAssist, out Vector3 lockDir))
                return; // no enemy in the aim cone -> keep the committed facing

            visual.SetFacing(lockDir);
        }

        // ------------------------------------------------------------------
        // Shared hitbox probe (slice-1 seam extended 1:1 to combo steps)
        // ------------------------------------------------------------------

        private bool IsInsideStepWindow()
        {
            float duration = EffectiveDuration(currentStep);
            float t0 = Mathf.Clamp01(currentStep.hitNorm.x) * duration;
            float t1 = Mathf.Clamp01(currentStep.hitNorm.y) * duration;
            return stepTime >= t0 && stepTime <= t1;
        }

        /// <summary>
        /// The active step's cancel window in SCALED seconds (wider when the step
        /// connected). Shared by the buffered advance and the mid-combo facing re-orient.
        /// </summary>
        private void CancelWindowBounds(out float c0, out float c1)
        {
            Vector2 window = connectedThisStep ? currentStep.cancelHitNorm : currentStep.cancelNorm;
            float duration = EffectiveDuration(currentStep);
            c0 = Mathf.Clamp01(window.x) * duration;
            c1 = Mathf.Clamp01(window.y) * duration;
        }

        /// <summary>True while the active step is inside its cancel window (scaled time).</summary>
        private bool IsInCancelWindow()
        {
            CancelWindowBounds(out float c0, out float c1);
            return stepTime >= c0 && stepTime <= c1;
        }

        /// <summary>
        /// Effective step duration in SCALED seconds. The clip plays at
        /// <c>speedMultiplier</c> (Animator state speed, decision c), so the hit
        /// window, the cancel windows and the step end must be measured against
        /// <c>duracion / speedMultiplier</c> — NOT the raw clip length — or the
        /// logic would outlive the accelerated clip by a full speed factor.
        /// </summary>
        private static float EffectiveDuration(in ComboStep step)
        {
            return step.duracion / Mathf.Max(0.01f, step.speedMultiplier);
        }

        private bool IsInsideActiveWindow()
        {
            float t0 = Mathf.Clamp01(current.hitNorm.x) * current.duration;
            float t1 = Mathf.Clamp01(current.hitNorm.y) * current.duration;
            return swingTime >= t0 && swingTime <= t1;
        }

        /// <summary>
        /// Sphere probe against <see cref="HitReceiver"/> targets, excluding the
        /// player's own Rigidbody (Face child collider shares it). Forward-only:
        /// the origin is offset along the facing, so a target behind never counts.
        /// Returns true when at least one target was hit.
        /// <paramref name="moveKey"/> (style-meter seam): when non-null, every NEWLY
        /// hit receiver fires <see cref="OnStepConnected"/> once with it.
        /// </summary>
        private bool ProbeAt(Vector3 origin, float radius, int damage, float knockback, float launchImpulse, string moveKey = null)
        {
            Collider[] cols = Physics.OverlapSphere(
                origin, radius, Physics.AllLayers, QueryTriggerInteraction.Ignore);

            bool connected = false;
            for (int i = 0; i < cols.Length; i++)
            {
                var col = cols[i];
                if (col == null) continue;
                if (col.attachedRigidbody == rb) continue; // own capsule / Face trigger

                var receiver = col.GetComponentInParent<HitReceiver>();
                if (receiver == null) continue;
                if (!hitThisStep.Add(receiver)) continue; // one hit per target per step/swing

                Vector3 to = receiver.transform.position - transform.position;
                to.y = 0f;
                if (to.sqrMagnitude < 0.0001f) to = FacingVector();
                to.Normalize();

                // LAUNCHER (slice 3nonies): the vertical impulse is passed SEPARATELY
                // (not baked into the XZ push) so HitReceiver can drive the juggle-lite
                // arc (rise -> fall -> land) with its own gravity. 0 = normal hit.
                receiver.TakeHit(damage, to * knockback, launchImpulse);
                if (moveKey != null) OnStepConnected?.Invoke(moveKey);
                connected = true;
            }
            return connected;
        }

        private void ProbeHitboxStep()
        {
            float launch = currentStep.launch ? Mathf.Max(0f, currentStep.launchImpulse) : 0f;
            string moveKey = activeCombo != null ? activeCombo.name + "#" + stepIndex : null;
            if (ProbeAt(HitboxOrigin(currentStep.hitboxRadius, currentStep.hitboxOffset),
                    currentStep.hitboxRadius, currentStep.dano, currentStep.knockback, launch, moveKey))
            {
                connectedThisStep = true; // wider cancel window (hit-cancel)
                BeginHitstop(currentStep.hitstopMs);
            }
        }

        private void ProbeHitbox()
        {
            string moveKey = current != null ? current.name + "#0" : null;
            if (ProbeAt(HitboxOrigin(current.hitboxRadius, current.hitboxOffset),
                    current.hitboxRadius, current.damage, current.knockback, 0f, moveKey))
            {
                BeginHitstop(current.hitstopMs);
            }
        }

        private void StartSwing(AttackDefinition def)
        {
            current = def;
            swingTime = 0f;
            hitThisSwing.Clear();
            ApplyAimAssist();
        }

        private void EndSwing()
        {
            current = null;
            swingTime = 0f;
            hitThisSwing.Clear();
            ClearAimAssist();
        }

        private void BeginHitstop(float hitstopMs)
        {
            if (hitstopRemaining > 0f) return; // one freeze per swing/step
            savedTimeScale = Time.timeScale;   // may be 0.25 / 0.5 / 2 — restore THIS
            Time.timeScale = 0f;
            hitstopRemaining = Mathf.Max(0.001f, hitstopMs / 1000f);
        }

        // ------------------------------------------------------------------
        // Dev visualization (decision: make the grey-box hitbox legible).
        // The origin math below is a DELIBERATE copy of the probe so what you
        // see is exactly what the sphere query tests.
        // ------------------------------------------------------------------

        /// <summary>World-space hitbox center for radius/offset — mirrors the probe.</summary>
        private Vector3 HitboxOrigin(float radius, Vector3 offset)
        {
            Vector3 facing = FacingForGizmos();
            return transform.position
                   + Vector3.up * offset.y
                   + facing * offset.z;
        }

        private Vector3 FacingVector()
        {
            Vector3 facing = visual != null ? visual.Facing : transform.forward;
            facing.y = 0f;
            if (facing.sqrMagnitude < 0.0001f) facing = Vector3.forward;
            return facing.normalized;
        }

        /// <summary>
        /// World-space AIM direction on XZ (unit) used as the soft-lock CONE AXIS: the
        /// camera-relative MOVE stick direction when the stick is past its deadzone (the
        /// player is pointing somewhere), else the current facing (keep the cone where the
        /// body already looks). Uses the SAME stick-to-world mapping the walk/dash use
        /// (<see cref="CameraRelativeMotion"/>) and the SAME deadzone as the facing-relative
        /// classifier (<see cref="AttackDirectionClassifier"/>), so the aim is consistent
        /// across the combat model.
        /// </summary>
        private Vector3 AimAxis()
        {
            Vector3 facing = FacingVector();
            if (input == null) return facing;
            if (cameraTransform == null) RefreshCamera();
            Vector2 stick = input.Player.Move.ReadValue<Vector2>();
            if (stick.magnitude < AttackDirectionClassifier.DefaultDeadzone) return facing;
            Vector3 dir = CameraRelativeMotion.WishDirection(cameraTransform, stick);
            return dir.sqrMagnitude > 0.000001f ? dir : facing;
        }

        /// <summary>
        /// Facing for gizmos: uses the cached visual, else resolves it lazily
        /// (Awake does not run in edit mode), else falls back to transform.forward.
        /// </summary>
        private Vector3 FacingForGizmos()
        {
            var v = visual != null ? visual : GetComponent<ICharacterVisual>();
            return FacingVector();
        }

        private void OnDrawGizmos()
        {
            // Live hitbox of the active combo step OR grey-box swing. Red while the
            // active hit window is open, amber otherwise, so the connect frame is obvious.
            if (comboActive)
            {
                Gizmos.color = IsInsideStepWindow()
                    ? Color.red
                    : new Color(1f, 0.6f, 0.1f, 0.6f);
                Gizmos.DrawWireSphere(HitboxOrigin(currentStep.hitboxRadius, currentStep.hitboxOffset),
                    currentStep.hitboxRadius);
                return;
            }
            if (current == null) return;
            Gizmos.color = IsInsideActiveWindow()
                ? Color.red
                : new Color(1f, 0.6f, 0.1f, 0.6f);
            Gizmos.DrawWireSphere(HitboxOrigin(current.hitboxRadius, current.hitboxOffset),
                current.hitboxRadius);
        }

        private void OnDrawGizmosSelected()
        {
            // Selected + idle: show BOTH reaches so light-vs-heavy is comparable.
            DrawDefinitionGizmo(lightAttack, new Color(0.2f, 0.8f, 1f, 0.7f));
            DrawDefinitionGizmo(heavyAttack, new Color(1f, 0.4f, 0f, 0.7f));
        }

        private void DrawDefinitionGizmo(AttackDefinition def, Color color)
        {
            if (def == null) return;
            Gizmos.color = color;
            Gizmos.DrawWireSphere(HitboxOrigin(def.hitboxRadius, def.hitboxOffset), def.hitboxRadius);
        }
    }
}