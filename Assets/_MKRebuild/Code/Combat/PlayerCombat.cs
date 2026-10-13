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

        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
            visual = GetComponent<ICharacterVisual>();
            motor = GetComponent<PlayerMotor>();
            dash = GetComponent<PlayerDash>();
            input = new PlayerInput();
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
            // DASH-ATTACK OPENER (slice 3octies): an attack during the dash or within
            // dashAttackWindow after it uses the dedicated dashAttack FIRST. Its
            // continuation chains into the normal combo roots (TryAdvanceOnBuffer).
            if ((light || heavy) && TryStartDashAttack()) return true;

            // Combo entry (multi-root): pick the longest chain whose root matches
            // the pressed input; a lone press ends that chain's single step cleanly
            // while a continuation chains. No match -> grey-box standalone swing.
            if (light)
            {
                ComboDefinition c = SelectRoot(ComboInput.Ligero);
                if (c != null) { StartCombo(c); return true; }
            }
            if (heavy)
            {
                ComboDefinition c = SelectRoot(ComboInput.Pesado);
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
            if (LightPressedThisFrame()) lightBuffer = inputBufferSeconds;
            if (HeavyPressedThisFrame()) heavyBuffer = inputBufferSeconds;

            // Scaled time: hitstop (timeScale = 0) freezes the step mid-window.
            stepTime += Time.deltaTime;
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
        /// If the buffered input matches <c>siguiente</c> and the step is inside its
        /// cancel window, advance to the next step. The hit window is WIDER when the
        /// step connected (hit-cancel, decision d) than on whiff.
        /// </summary>
        private void TryAdvanceOnBuffer()
        {
            if (activeCombo == null) return;
            // A finisher (paso < 0) has no in-combo continuation — EXCEPT the dash-attack
            // opener, whose continuation chains into the normal roots instead of by index.
            if (!dashAttackActive && currentStep.siguiente.paso < 0) return;

            Vector2 window = connectedThisStep ? currentStep.cancelHitNorm : currentStep.cancelNorm;
            float duration = EffectiveDuration(currentStep);
            float c0 = Mathf.Clamp01(window.x) * duration;
            float c1 = Mathf.Clamp01(window.y) * duration;
            if (stepTime < c0 || stepTime > c1) return;

            bool wantsLight = currentStep.siguiente.input == ComboInput.Ligero;
            bool buffered = wantsLight ? lightBuffer > 0f : heavyBuffer > 0f;
            if (!buffered) return;

            lightBuffer = 0f; // consume the tap (both — only one can match)
            heavyBuffer = 0f;

            // OPENER -> NORMAL COMBO (slice 3octies): the dash-attack's continuation
            // resolves the NORMAL roots by the pressed input, so GolpeDeDash chains into
            // the standard combo (GolpeDeDash -> Golpe -> Golpe...).
            if (dashAttackActive)
            {
                dashAttackActive = false;
                ComboDefinition next = SelectRoot(currentStep.siguiente.input);
                if (next != null) { StartCombo(next); return; }
                EndCombo(); // no normal root for that input -> end the opener cleanly
                return;
            }

            AdvanceCombo(currentStep.siguiente.paso);
        }

        /// <summary>
        /// Root resolution (input-prefix rule, multi-combo): among
        /// <see cref="combos"/> whose first step input matches
        /// <paramref name="pressed"/>, pick the LONGEST chain (most steps) so a
        /// lone press still ends cleanly at step 0 while a continuation chains;
        /// ties keep the first in the array. Null when nothing matches.
        /// </summary>
        private ComboDefinition SelectRoot(ComboInput pressed)
        {
            ComboDefinition best = null;
            int bestLen = -1;
            if (combos == null) return null;
            for (int i = 0; i < combos.Length; i++)
            {
                ComboDefinition c = combos[i];
                if (c == null || !c.TryGetStep(0, out ComboStep s0)) continue;
                if (s0.input != pressed) continue;
                if (c.Length > bestLen) { bestLen = c.Length; best = c; }
            }
            return best;
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

            Vector3 facing = FacingVector();
            float advance = Mathf.Max(0f, step.lunge);
            float speed = Mathf.Max(0f, CurveMotion.Speed(step.lungeCurve, advance, window, stepTime));
            Vector3 v = rb.linearVelocity;
            v.x = facing.x * speed;
            v.z = facing.z * speed;
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
                // DASH-ATTACK opener: its OWN clip slot (dormant Attack_Dash state). Enter it
                // ONLY when the step has an assigned clip (ComboStep.clip) — with no clip the
                // EMPTY state would freeze the pose, so the visual is skipped and the opener
                // renders with whatever locomotion pose is live (no pose freeze).
                if (def.clip != null)
                    animatorVisual.PlayDashAttack(Mathf.Max(0f, crossFadeDuration));
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
            if (visual == null) return;

            // Shared selection (slice 3quinquies, Code/Combat/AimAssist.cs): the dash
            // direction uses the EXACT same cone + range + nearest-alive-target math.
            if (!AimAssist.TryFindAimDir(transform.position, rb, FacingVector(), aimAssist, out Vector3 aim))
                return;

            aimDir = aim;

            // SetFacing is incremental (~12°/frame at 720°/s): a single call cannot
            // align. Iterate over the swing opening — window sized to the worst-case
            // cone error at the configured turn speed.
            aimCorrectRemaining = Mathf.Clamp(aimAssist.coneHalfAngleDeg, 0f, 180f)
                / Mathf.Max(1f, aimAssist.turnSpeedDegPerSec);
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
        /// </summary>
        private bool ProbeAt(Vector3 origin, float radius, int damage, float knockback)
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

                receiver.TakeHit(damage, to * knockback);
                connected = true;
            }
            return connected;
        }

        private void ProbeHitboxStep()
        {
            if (ProbeAt(HitboxOrigin(currentStep.hitboxRadius, currentStep.hitboxOffset),
                    currentStep.hitboxRadius, currentStep.dano, currentStep.knockback))
            {
                connectedThisStep = true; // wider cancel window (hit-cancel)
                BeginHitstop(currentStep.hitstopMs);
            }
        }

        private void ProbeHitbox()
        {
            if (ProbeAt(HitboxOrigin(current.hitboxRadius, current.hitboxOffset),
                    current.hitboxRadius, current.damage, current.knockback))
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