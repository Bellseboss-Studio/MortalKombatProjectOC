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
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody))]
    public sealed class PlayerCombat : MonoBehaviour
    {
        [Tooltip("Light attack definition (gamepad X / mouse left). Fallback when no combo or the combo does not start on light.")]
        [SerializeField] private AttackDefinition lightAttack;

        [Tooltip("Heavy attack definition (gamepad Y / mouse right). Fallback when no combo or the combo does not start on heavy.")]
        [SerializeField] private AttackDefinition heavyAttack;

        [Tooltip("Optional combo chain. When assigned, the first step REPLACES the grey-box swing for its input (doc: L→L→P).")]
        [SerializeField] private ComboDefinition combo;

        [Tooltip("CrossFade duration (seconds) used to enter every attack state (decision b, ~0.1).")]
        [SerializeField] private float crossFadeDuration = 0.1f;

        [Tooltip("Input buffer in SECONDS (decision d, 0.15). UNSCALED: survives hitstop timeScale = 0.")]
        [SerializeField] private float inputBufferSeconds = 0.15f;

        private PlayerInput input;
        private Rigidbody rb;
        private ICharacterVisual visual;
        private PlayerMotor motor;

        // One-shot taps submitted out-of-band (editor smoke driver / future
        // menu or demo hooks). Consumed by the next Update as if pressed this frame.
        private bool queuedLightTap;
        private bool queuedHeavyTap;

        // Grey-box fallback swing (slice 1, one at a time).
        private AttackDefinition current;
        private float swingTime;                 // scaled seconds into the swing
        private readonly HashSet<HitReceiver> hitThisSwing = new HashSet<HitReceiver>();

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

        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
            visual = GetComponent<ICharacterVisual>();
            motor = GetComponent<PlayerMotor>();
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
            if (lightBuffer > 0f) lightBuffer = Mathf.Max(0f, lightBuffer - Time.unscaledDeltaTime);
            if (heavyBuffer > 0f) heavyBuffer = Mathf.Max(0f, heavyBuffer - Time.unscaledDeltaTime);

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

            bool light = LightPressedThisFrame();
            bool heavy = HeavyPressedThisFrame();

            // Combo entry replaces the grey-box swing for the first step's input.
            if (combo != null && combo.TryGetStep(0, out ComboStep first))
            {
                bool startsOnLight = first.input == ComboInput.Ligero && light;
                bool startsOnHeavy = first.input == ComboInput.Pesado && heavy;
                if (startsOnLight || startsOnHeavy)
                {
                    StartCombo();
                    return;
                }
            }

            if (light && lightAttack != null) StartSwing(lightAttack);
            else if (heavy && heavyAttack != null) StartSwing(heavyAttack);
            else if ((light || heavy) && !warnedMissingDefinition)
            {
                warnedMissingDefinition = true;
                Debug.LogWarning(
                    $"{name}: attack pressed but no AttackDefinition assigned in the inspector.", this);
            }
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
            if (combo == null || currentStep.siguiente.paso < 0) return;

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
            AdvanceCombo(currentStep.siguiente.paso);
        }

        private void StartCombo()
        {
            if (combo == null || !combo.TryGetStep(0, out ComboStep first)) return;
            current = null;
            comboActive = true;
            stepIndex = 0;
            currentStep = first;
            stepTime = 0f;
            connectedThisStep = false;
            hitThisStep.Clear();
            hitThisSwing.Clear();
            ApplyStepLock(first);
            PlayAttackVisual(1, first);
        }

        private void AdvanceCombo(int index)
        {
            if (combo == null || !combo.TryGetStep(index, out ComboStep next))
            {
                EndCombo();
                return;
            }
            stepIndex = index;
            currentStep = next;
            stepTime = 0f;
            connectedThisStep = false;
            hitThisStep.Clear();
            ApplyStepLock(next);
            PlayAttackVisual(index + 1, next);
        }

        private void EndCombo()
        {
            comboActive = false;
            stepIndex = -1;
            currentStep = default;
            stepTime = 0f;
            connectedThisStep = false;
            hitThisStep.Clear();
            if (motor != null) motor.InputLocked = false; // release the attack lock -> wish control returns
            if (visual is AnimatorVisual animatorVisual) animatorVisual.ReleaseAttack();
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
        /// locked step: forward speed = <c>lunge / effective window</c>, so the
        /// total advance over the step ≈ <c>lunge</c> meters (H&S step-into-the-
        /// strike). The motor's <see cref="PlayerMotor.InputLocked"/> preserves this
        /// velocity. A locked step with lunge = 0 is a full stop (no drift).
        /// </summary>
        private void ApplyLunge(in ComboStep step)
        {
            if (!step.moveLock) return;
            float window = EffectiveDuration(step);
            if (window <= 0f) return;

            Vector3 facing = FacingVector();
            float speed = Mathf.Max(0f, step.lunge) / window;
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
            if (visual is AnimatorVisual animatorVisual)
            {
                animatorVisual.PlayAttack(step, Mathf.Max(0f, crossFadeDuration));
                animatorVisual.SetAttackSpeed(step, def.speedMultiplier);
            }
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
        }

        private void EndSwing()
        {
            current = null;
            swingTime = 0f;
            hitThisSwing.Clear();
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