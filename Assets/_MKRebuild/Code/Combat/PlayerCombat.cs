using System;
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
    /// <c>cancelNorm</c> (narrower) on whiff. Hitstop (decision h): acquires and
    /// releases a <see cref="ControlLock"/> Hitstop handle, so the scale restored on
    /// expiry is whatever the stack says — never a stale saved value (debug F6 base).
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
    ///
    /// FILE SPLIT (combo-grammar T0.1): this is a <c>partial</c> class spread over three
    /// files that compile together and share ALL state — this file (core: serialized
    /// fields, references, lifecycle, input seams, hitstop, audio and the live tuning
    /// layer), <c>PlayerCombat.Combo.cs</c> (chain flow: entry, advance, cancel windows,
    /// start/advance/end, the body-ownership token, step playback and the combo grammar)
    /// and <c>PlayerCombat.Hitbox.cs</c> (aim/facing, targeting, probes and gizmos). The
    /// split is an organization boundary only: no behavior depends on which file a member
    /// lives in.
    ///
    /// COMBO-GRAMMAR GATE: <c>GrammarEnabled</c> is TRUE only when the player has a
    /// <see cref="PlayerStance"/> whose <c>allowComboGrammar</c> is ON. With the gate CLOSED
    /// (the v1 player, or the flag off) the chain runs the legacy V1 rules in
    /// <see cref="ComboResolver"/> and every grammar seam returns the old value, so the
    /// behavior is exactly the pre-change one. With the gate OPEN but no matrix authored for
    /// the current stance, the player is ROOT-ONLY (only the opener plays) — it never falls
    /// back to legacy chaining.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody))]
    public sealed partial class PlayerCombat : MonoBehaviour
    {
        [Tooltip("Light attack definition (gamepad X / mouse left). Fallback when no combo or the combo does not start on light.")]
        [SerializeField] private AttackDefinition lightAttack;

        [Tooltip("Heavy attack definition (gamepad Y / mouse right). Fallback when no combo or the combo does not start on heavy.")]
        [SerializeField] private AttackDefinition heavyAttack;

        [Tooltip("Combo chains (data-driven, multi-root). Each combo's root is its step 0 input; when several combos match a press, the LONGEST chain wins (input-prefix rule): L→Combo_1 (L→L→P), P→Combo_H, etc. Any combo can be tested alone by assigning only it.")]
        [SerializeField] private ComboDefinition[] combos = new ComboDefinition[0];

        [Tooltip("COMBO GRAMMAR: one transitions/windows/cadence matrix per stance (the asset decides which stance it describes). The gate is the STANCE, not this array: with the grammar ON and no matrix assigned the player is ROOT-ONLY (only the opener plays, no transition is invented). Wiring lands in T3.1.")]
        [SerializeField] private StanceComboMatrix[] stanceMatrices = new StanceComboMatrix[0];

        [Tooltip("DASH-ATTACK OPENER (slice 3octies): a dedicated combo whose FIRST step is the attack that comes out of a dash (its OWN clip slot + hit data + lunge). When an attack starts during a dash or within dashAttackWindow after it, this runs FIRST; its 'siguiente' then chains into the NORMAL combo roots (GolpeDeDash -> Golpe -> Golpe...). Null = no opener (attacks always use the normal roots).")]
        [SerializeField] private ComboDefinition dashAttack;

        [Tooltip("Unscaled SECONDS after a dash during which the first attack counts as the dash-attack opener. 0 = only while the dash runs. Default 0.35 ~ the dash duration.")]
        [SerializeField] private float dashAttackWindow = 0.35f;

        [Tooltip("Soft trajectory corrector (aim-assist): at attack entry, turn the facing toward the nearest alive target inside the frontal cone + range. Drags lunge + hitbox (single point: visual.Facing). Null/disabled = forward-only (current behavior).")]
        [SerializeField] private AimAssistDefinition aimAssist;

        [Tooltip("LIVE COMBAT TUNING source (DebugWindow F1 sliders): attack speed / reach / hitstop / cancel-start scales and the victim-cap override. Null = fall back to the PlayerMotor's own params asset, so the sliders work even when this slot is empty.")]
        [SerializeField] private PlayerParams paramsAsset;

        [Tooltip("CrossFade duration (seconds) used to enter every attack state (decision b, ~0.1).")]
        [SerializeField] private float crossFadeDuration = 0.1f;

        [Tooltip("Input buffer in SECONDS (decision d, 0.15). UNSCALED: survives hitstop timeScale = 0.")]
        [SerializeField] private float inputBufferSeconds = 0.15f;

        private PlayerInput input;
        private Rigidbody rb;
        private ICharacterVisual visual;
        private PlayerMotor motor;
        private PlayerDash dash;
        private PlayerStance stance;       // combo-grammar gate + style cadence source
        private PlayerDamage playerDamage; // hurt signals: a hit taken resets the chain memory
        private Transform cameraTransform; // reference frame for the facing-relative stick

        // One-shot taps submitted out-of-band (editor smoke driver / future
        // menu or demo hooks). Consumed by the next Update as if pressed this frame.
        private bool queuedLightTap;
        private bool queuedHeavyTap;
        private bool queuedDashTap;
        // Ability input seam (m1-abilities): one-shot Skill taps queued out-of-band
        // (editor smoke driver) + the sibling ability runner. ConsumeSkillTap is the
        // SINGLE consumer, so PlayerAbilities stays the ability owner.
        private bool queuedSkill1Tap;
        private bool queuedSkill2Tap;
        private PlayerAbilities abilities;

        // Hitstop state.
        private float hitstopRemaining;                    // unscaled seconds left frozen
        private ControlLock.Handle hitstopHandle;          // lock token owned by this freeze

        private bool warnedMissingDefinition;
        private AudioCueId hitCue = AudioCueId.LightHit;

        /// <summary>True while a swing or a combo step is active (read-only debug seam).</summary>
        public bool Attacking => current != null || comboActive;

        /// <summary>Definition of the active grey-box swing, null when idle (read-only debug seam).</summary>
        public AttackDefinition CurrentAttack => current;

        /// <summary>Index of the active combo step, -1 when not in a combo (read-only debug seam).</summary>
        public int CurrentStepIndex => chain.stepIndex;

        /// <summary>True while the active combo is the DASH-ATTACK opener (read-only debug seam).</summary>
        public bool DashAttackActive => dashAttackActive;

        /// <summary>
        /// True while the hitstop freeze is active (timeScale = 0). Seam for the dash:
        /// "dash cancela TODO salvo hitstop propio" — the dash must NOT fire during the
        /// freeze (decision f guard).
        /// </summary>
        public bool IsHitstopped => hitstopRemaining > 0f;

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
            stance = GetComponent<PlayerStance>();       // null on Player_v1 (the gate stays closed)
            playerDamage = GetComponent<PlayerDamage>();
            abilities = GetComponent<PlayerAbilities>(); // sibling ability runner (m1-abilities)
            input = new PlayerInput();
            RefreshCamera();
        }

        private void RefreshCamera()
        {
            // Prefer the owned rig's basis (the authored vcam) over the tagged camera:
            // the rig is the deterministic contract, Camera.main is only a safety net.
            var rig = FindFirstObjectByType<CameraRig>();
            if (rig != null && rig.Basis != null)
            {
                cameraTransform = rig.Basis;
                return;
            }

            var cam = Camera.main;
            cameraTransform = cam != null ? cam.transform : null;
        }

        private void OnEnable()
        {
            if (input == null) input = new PlayerInput();
            input.Player.Enable();

            // COMBO GRAMMAR WIRING: only a player that HAS the grammar installs the
            // stance predicate and the reset signals, so a stance-less player (Player_v1)
            // and a grammar-off player keep the exact v0.2.0 behavior. The condition is
            // evaluated at (re)enable time, which is the only sensible moment for a gate.
            if (stance == null) stance = GetComponent<PlayerStance>();
            if (stance != null && stance.AllowsComboGrammar) SubscribeGrammarSignals();
        }

        private void OnDisable()
        {
            if (input != null) input.Player.Disable();
            UnsubscribeGrammarSignals(); // never leave a stale predicate/signal behind
            ReleaseAttackToken(); // never leave a stale attack token
            ReleaseHitstop();     // a disabled combat component must not pin the time lock
        }

        private void OnDestroy()
        {
            UnsubscribeGrammarSignals(); // idempotent: static/event signals must not leak
            ReleaseHitstop();     // domain/scene teardown must not leave Time.timeScale at 0
            if (input != null) input.Dispose();
        }

        private void Update()
        {
            // Hitstop owns the frame: consume unscaled time, release the time lock on
            // expiry, and ignore new input while frozen. The input buffers do NOT decay
            // here either — they survive the freeze (decision d).
            if (hitstopRemaining > 0f)
            {
                hitstopRemaining -= Time.unscaledDeltaTime;
                if (hitstopRemaining <= 0f)
                {
                    hitstopRemaining = 0f;
                    ControlLock.Release(hitstopHandle);
                    hitstopHandle = null;
                }

                // COMBO GRAMMAR (T2.7): the freeze must not EAT the press. The step clock and
                // the press lifetime stay frozen (chain.Tick does not run), but a button read on
                // this frame is queued for the grammar so it can resolve the instant the hitstop
                // ends. Gated to the grammar: V1 returns exactly where it always did.
                LatchGrammarPressDuringHitstop();
                return;
            }

            // Unscaled buffer decay (jumpBuffer pattern): a tap survives ~0.15 s.
            // EXCEPT while the DASH owns the body (idle + dash token + no combo):
            // the attack pressed mid-dash is HELD (no decay) so it fires the instant the dash
            // ends — dash -> attack -> combo. Combo steps are UNAFFECTED: their own 0.15 s
            // step buffer keeps decaying (comboActive skips this hold).
            bool dashHoldsInput = motor != null && motor.BodyOwner == BodyPriority.Dash && !comboActive;
            if (!dashHoldsInput)
            {
                if (chain.lightBuffer > 0f) chain.lightBuffer = Mathf.Max(0f, chain.lightBuffer - Time.unscaledDeltaTime);
                if (chain.heavyBuffer > 0f) chain.heavyBuffer = Mathf.Max(0f, chain.heavyBuffer - Time.unscaledDeltaTime);
            }

            // CHAIN CLOCKS (combo-grammar T2.3/T2.4): the step clock advances only while a
            // step runs (same frame ordering as the old inline increment) and the neutral
            // clock only while it does NOT — a running step can never be "neutral".
            chain.Tick(
                comboActive ? Time.deltaTime : 0f,
                dashHoldsInput ? 0f : Time.unscaledDeltaTime,
                comboActive ? 0f : ChainResetSeconds);

            UpdateAimCorrection();
            UpdateDirectionHold();

            if (comboActive) UpdateCombo();
            else UpdateIdle();
        }

        private void FixedUpdate()
        {
            if (hitstopRemaining > 0f) return;
            // BODY-OWNERSHIP GUARD (m2-hazards S2): a higher-priority system (death /
            // knockback) owns the body — do NOT write a lunge velocity or probe hits onto
            // the corpse. Mirrors PlayerDash's yield check. The combo itself keeps running
            // (UpdateCombo) and ends normally, releasing ONLY its own attack token.
            if (motor != null && motor.Body != null
                && motor.Body.Owner != null && motor.Body.Owner != BodyPriority.Attack)
                return;
            if (comboActive)
            {
                ApplyLunge(currentStep);
                // Ability steps deal their own damage (PlayerAbilities); the generic
                // hitbox probe is skipped so H1/H2 never double-hits (m1-abilities).
                if (IsInsideStepWindow() && currentStep.stepType != ComboStepType.Ability) ProbeHitboxStep();
                return;
            }
            if (current == null) return;
            if (IsInsideActiveWindow()) ProbeHitbox();
        }

        /// <summary>
        /// Plays the swing + effort voice for an attack that has JUST started. Called
        /// from the single point where each attack path is committed (combo root,
        /// standalone swing, dash-attack opener) so an attack never plays it twice.
        /// </summary>
        private void PlayAttackAudio(bool heavy)
        {
            AudioDirector.Play(heavy ? AudioCueId.HeavySwing : AudioCueId.LightSwing, transform.position);
            AudioDirector.Play(heavy ? AudioCueId.HeavyVoice : AudioCueId.LightVoice, transform.position);
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
            // m1-abilities: route the seam to the same pipeline the real skill reads.
            if (ability == ComboInput.H1) queuedSkill1Tap = true;
            else if (ability == ComboInput.H2) queuedSkill2Tap = true;
        }

        /// <summary>Public tap seam for Skill1 (H1 projectile). Mirrors <see cref="SubmitLightAttack"/>.</summary>
        public void SubmitSkill1() => queuedSkill1Tap = true;

        /// <summary>Public tap seam for Skill2 (H2 area).</summary>
        public void SubmitSkill2() => queuedSkill2Tap = true;

        /// <summary>
        /// Skill press reader shared with <see cref="PlayerAbilities"/> (m1-abilities):
        /// the real Skill1/Skill2 actions OR the queued SubmitSkill1/2 taps. Returns true
        /// once per press; <paramref name="ability"/> is H1 for Skill1, H2 for Skill2.
        /// </summary>
        public bool ConsumeSkillTap(out ComboInput ability)
        {
            if (queuedSkill1Tap) { queuedSkill1Tap = false; ability = ComboInput.H1; return true; }
            if (queuedSkill2Tap) { queuedSkill2Tap = false; ability = ComboInput.H2; return true; }
            if (input != null && input.Player.Skill1.WasPressedThisFrame()) { ability = ComboInput.H1; return true; }
            if (input != null && input.Player.Skill2.WasPressedThisFrame()) { ability = ComboInput.H2; return true; }
            ability = ComboInput.H1;
            return false;
        }

        /// <summary>
        /// Hitstop seam for abilities (m1-abilities): <see cref="PlayerAbilities"/> owns the
        /// ability hit but the freeze lives here (same save/restore contract as the combo probes).
        /// </summary>
        public void RequestHitstop(int milliseconds) => BeginHitstop(milliseconds);

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
            ReleaseAttackToken(); // belt-and-braces: never leave a stale attack token
            chain.ClearBuffer();
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
        /// T2.7 — HITSTOP INPUT LATCH. <see cref="Update"/> returns early while frozen, so a
        /// press that lands in those 70-90 ms used to be lost. With the grammar ON and a step
        /// running, the press is read and ENQUEUED (never advanced): <c>stepTime</c> stays
        /// frozen because <see cref="ComboChainState.Tick"/> does not run, and the queued press
        /// resolves as soon as the hitstop ends.
        ///
        /// The seam is single-consumer (<see cref="LightPressedThisFrame"/> clears the queued
        /// tap and the Input System reports <c>WasPressedThisFrame</c> exactly once), so the
        /// press cannot be double-counted. V1 (no grammar / no step) never reaches the latch.
        /// </summary>
        private void LatchGrammarPressDuringHitstop()
        {
            if (!GrammarEnabled || !comboActive) return;

            if (LightPressedThisFrame())
            {
                chain.bufferedDirection = ReadAttackDirection();
                BufferGrammarPress(ComboInput.Ligero, chain.bufferedDirection);
            }

            if (HeavyPressedThisFrame())
            {
                chain.bufferedDirection = ReadAttackDirection();
                BufferGrammarPress(ComboInput.Pesado, chain.bufferedDirection);
            }
        }

        // ------------------------------------------------------------------
        // LIVE combat tuning (WU6): global scales that MULTIPLY the per-step data.
        // The data stays AUTHORITATIVE (speedMultiplier, hitboxRadius/Offset,
        // hitstopMs, cancel windows, maxTargets); scale 1 + override 0 reproduce it
        // exactly, so this is a pure multiplier layer. The values live on
        // PlayerParams and the DebugWindow edits them during Play Mode (no recompile).
        // ------------------------------------------------------------------

        /// <summary>
        /// Tuning source: this component's own asset, else the motor's params asset.
        /// Null (no asset anywhere) = every scale is 1 and the override is 0.
        /// </summary>
        private PlayerParams Params => paramsAsset != null ? paramsAsset : (motor != null ? motor.Params : null);

        /// <summary>Global attack-cadence multiplier (1 = data).</summary>
        private float SpeedScale => Params != null ? Params.attackSpeedScale : 1f;

        /// <summary>Global reach multiplier on BOTH the radius and the offset (1 = data).</summary>
        private float ReachScale => Params != null ? Params.attackReachScale : 1f;

        /// <summary>Global hitstop multiplier (1 = data, 0 = no freeze).</summary>
        private float HitstopScale => Params != null ? Params.hitstopScale : 1f;

        /// <summary>Cancel-START divisor (&gt; 1 opens the cancel window EARLIER; 1 = data).</summary>
        private float CancelStartScale => Params != null ? Params.cancelStartScale : 1f;

        /// <summary>Victim-cap override; 0 = use each step's own maxTargets (data).</summary>
        private int TargetsOverride => Params != null ? Params.maxTargetsOverride : 0;

        // ------------------------------------------------------------------
        // COMBO GRAMMAR gate (combo-grammar T2.4/T2.5).
        //
        // The gate is the STANCE, not the matrix (design decision 5): a player with no
        // PlayerStance (Player_v1) or with allowComboGrammar OFF runs the exact v0.2.0
        // path. GrammarEnabled is consulted in EXACTLY four behavior points:
        //   TryAdvanceOnBuffer   (matrix lookup vs legacy continuation)
        //   CancelWindowBounds   (absolute asymmetric windows)
        //   ProbeHitboxStep      (AppliedDamage)
        //   AttackSpeed          (style cadence, via StyleSpeedScale)
        // and nowhere else.
        // ------------------------------------------------------------------

        /// <summary>True when the new combo grammar owns the chain for this player.</summary>
        private bool GrammarEnabled => stance != null && stance.AllowsComboGrammar;

        /// <summary>Matrix of the CURRENT stance; null when none is assigned (root-only).</summary>
        private StanceComboMatrix ActiveMatrix
        {
            get
            {
                if (stance == null || stanceMatrices == null) return null;

                PlayerStance.Stance current = stance.CurrentStance;
                for (int i = 0; i < stanceMatrices.Length; i++)
                {
                    StanceComboMatrix matrix = stanceMatrices[i];
                    if (matrix != null && matrix.Stance == current) return matrix;
                }

                return null;
            }
        }

        /// <summary>
        /// Per-style cadence. EXACTLY 1 when the grammar is off or there is no matrix, so
        /// <see cref="AttackSpeed"/> (and therefore the step duration, the hit window AND the
        /// clip retime) reproduces v0.2.0 byte for byte.
        /// </summary>
        private float StyleSpeedScale =>
            GrammarEnabled && ActiveMatrix != null ? ActiveMatrix.StyleSpeedScale : 1f;

        /// <summary>Chain budget of the active matrix (unbounded without one).</summary>
        private int ChainSlots
        {
            get
            {
                StanceComboMatrix matrix = ActiveMatrix;
                return matrix != null ? matrix.ChainSlots : int.MaxValue;
            }
        }

        /// <summary>
        /// Neutral seconds before the chain memory clears. +infinity without a matrix so the
        /// reset can never fire for a player with no grammar data.
        /// </summary>
        private float ChainResetSeconds
        {
            get
            {
                StanceComboMatrix matrix = ActiveMatrix;
                return matrix != null ? matrix.ChainResetSeconds : float.PositiveInfinity;
            }
        }

        /// <summary>
        /// The step's REAL clip retime: the data's speedMultiplier x the live scale x the
        /// STYLE cadence (1 without grammar). This single function feeds both the step
        /// duration and the Animator retime, so animation and windows cannot desync.
        /// </summary>
        private float AttackSpeed(in ComboStep step) =>
            Mathf.Max(0.01f, step.speedMultiplier * SpeedScale * StyleSpeedScale);


        /// <summary>
        /// Installs the grammar signals (T2.4): the stance window predicate, the stance-change
        /// reset, the "took a hit" reset and the "target died" reset. Called ONLY when the player
        /// actually has the grammar (see OnEnable), so V1 never sees any of these events.
        /// </summary>
        private void SubscribeGrammarSignals()
        {
            if (stance != null)
            {
                // The controller owns WHEN a switch is legal; PlayerStance never learns what a
                // chain is. Mid-combo the switch survives only inside the cancel window.
                stance.OnStanceChanged += HandleStanceChanged;
                stance.CanSwitchStanceNow = CanSwitchStanceNow;
            }

            if (playerDamage != null)
            {
                playerDamage.OnDamaged += HandlePlayerDamaged;
                playerDamage.OnDied += HandlePlayerDamaged;
            }

            HitReceiver.AnyDied += HandleTargetDied;
        }

        /// <summary>
        /// Drops every grammar signal (idempotent). Safe on a component that never subscribed
        /// and safe to call twice (OnDestroy after OnDisable).
        /// </summary>
        private void UnsubscribeGrammarSignals()
        {
            if (stance != null)
            {
                stance.OnStanceChanged -= HandleStanceChanged;
                stance.CanSwitchStanceNow = null;
            }

            if (playerDamage != null)
            {
                playerDamage.OnDamaged -= HandlePlayerDamaged;
                playerDamage.OnDied -= HandlePlayerDamaged;
            }

            HitReceiver.AnyDied -= HandleTargetDied;
        }

        private void BeginHitstop(float hitstopMs)
        {
            if (hitstopRemaining > 0f) return; // one freeze per swing/step
            // The lock keeps the debug base scale (F6) intact; releasing it restores
            // that base instead of the value that happened to be live before the swing.
            hitstopHandle = ControlLock.Acquire(ControlReason.Hitstop);
            hitstopRemaining = Mathf.Max(0.001f, hitstopMs / 1000f);
        }

        /// <summary>
        /// Drops the hitstop freeze and its lock token. Safe when idle; used by the
        /// expiry path, <see cref="OnDisable"/> and <see cref="OnDestroy"/> so a
        /// disabled/swapped combat component can never leave time at 0.
        /// </summary>
        private void ReleaseHitstop()
        {
            hitstopRemaining = 0f;
            if (hitstopHandle == null) return;
            ControlLock.Release(hitstopHandle);
            hitstopHandle = null;
        }
    }
}
