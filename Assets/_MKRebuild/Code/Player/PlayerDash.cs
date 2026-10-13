using UnityEngine;

namespace MK.Rebuild
{
    /// <summary>
    /// M1 slice-3 grey-box DASH (2026-10-13, decision f — user-approved scope).
    /// Defense/mobility: an impulse along the facing (~0.35 s) with a short invuln
    /// window (~0.25 s) and a cooldown (~1.2 s), numbers from doc 05.
    ///
    /// The MOTOR stays the movement owner: during the dash this component sets
    /// <see cref="PlayerMotor.InputLocked"/> = true (the motor preserves the horizontal
    /// velocity the dash writes — no wish input, no input rotation; gravity/jump/wall
    /// untouched) and Rigorously releases the lock at dash end AND on OnDisable.
    /// Facing is locked along the dash direction (<see cref="ICharacterVisual.SetFacing"/>).
    ///
    /// Dash = INTERRUPTOR, not a combo step (doc: "dash cancela TODO salvo hitstop
    /// propio"). On press while off-cooldown it CANCELS the active combo via
    /// <see cref="PlayerCombat.CancelCombo"/> and dashes; it must NOT fire during the
    /// hitstop freeze (<see cref="PlayerCombat.IsHitstopped"/> guard). Abilities are
    /// out of scope (PREPARE-only) — PlayerDash only reads the Dash input through
    /// <see cref="PlayerCombat.ConsumeDashTap"/>.
    ///
    /// ADDITIVE mode (slice 3ter, <see cref="additive"/>) — the dash is a BURST ON TOP
    /// of the player's current speed: at dash start the horizontal velocity is captured
    /// (<see cref="baseVelocity"/>, direction locked), and during the dash the velocity
    /// = base velocity + the curve impulse along the locked facing. Run + dash ends back
    /// at ~run speed (the run CONTINUES, no dip to 0); stand + dash is a burst from ~0
    /// (the pre-additive feel). Legacy pure-burst replay is kept via <see cref="additive"/>
    /// = false (A/B toggle, data-driven).
    ///
    /// AIM-ASSIST on the dash direction (slice 3sexies): at dash start its OWN
    /// <see cref="AimAssistDefinition"/> (<see cref="dashAimAssist"/>, tuned independently
    /// from the attack's — e.g. a more generous range) runs the shared selection
    /// (<see cref="AimAssist"/>, cone + range, nearest alive <see cref="HitReceiver"/>)
    /// against the intended dash direction. A target inside the frontal cone SNAPS the dash
    /// toward it (OFFENSIVE gap-close); dashing AWAY (target behind/outside the cone) finds
    /// nothing and the dash stays straight (DEFENSIVE/mobility, unaffected). The additive
    /// model, curve, cooldown, iframes, hitstop gate and combo-cancel are untouched — only
    /// the direction is corrected.
    ///
    /// ATTACK CANCEL (slice 3septies): an attack pressed during the dash CANCELS it early
    /// (dash-cancel into attack) — but only once the dash has run <see cref="minCancelTime"/>
    /// unscaled seconds, so the dash stays readable and cannot be cut at t=0.
    /// <see cref="TryCancelByAttack"/> ends the dash and releases
    /// <see cref="PlayerMotor.InputLocked"/>; <see cref="PlayerCombat"/> starts the attack the
    /// same frame. Before the window (or with no attack) the fallback is the 3sexies buffer:
    /// the press is held and fires when the dash ends on its own.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody))]
    public sealed class PlayerDash : MonoBehaviour
    {
        [Tooltip("Dash impulse duration in SECONDS (doc 05: 0.35).")]
        [SerializeField] private float dashDuration = 0.35f;

        [Tooltip("MINIMUM unscaled seconds the dash must run before an ATTACK can cancel it early (slice 3septies: dash-cancel into attack). Keeps the dash readable — it cannot be cut at t=0. 0 = cancel immediately. Movement/iframes/cooldown stay untouched: the cooldown is armed at dash START, so a cancelled dash still pays it.")]
        [SerializeField] private float minCancelTime = 0.05f;

        [Tooltip("Invulnerability window in SECONDS after the dash starts (doc 05: 0.25). Grey-box flag — no damage system consumes it yet.")]
        [SerializeField] private float invulnDuration = 0.25f;

        [Tooltip("Cooldown in SECONDS before the dash can be used again (doc 05: 1.2). Blocks dash spam.")]
        [SerializeField] private float cooldown = 1.2f;

        [Tooltip("Horizontal dash DISTANCE in meters (grey-box: ~3.5 m over 0.35 s — tune to feel). The speed shape comes from dashCurve.")]
        [SerializeField] private float dashDistance = 3.5f;

        [Tooltip("Normalized speed SHAPE over the dash (1 -> 0): strong impulse at the start, decaying to 0. Empty = default ease-out. The code integrates it so the total distance is exactly dashDistance.")]
        [SerializeField] private AnimationCurve dashCurve;

        [Tooltip("ADDITIVE dash (default true, slice 3ter): the velocity during the dash = the BASE horizontal velocity captured at dash start (direction locked) + the curve impulse. Run + dash = burst ON TOP and the run continues afterwards at ~base speed; stand + dash = burst from ~0. false = legacy pure-burst: the curve REPLACES the velocity (run stops then resumes). A/B toggle — data-driven, everything else stays in data.")]
        [SerializeField] private bool additive = true;

        [Tooltip("Aim-assist config for the DASH direction — its OWN definition, tunable independently from PlayerCombat's attack corrector (cone / range / turn). E.g. a more generous range so the dash gap-closes farther than an attack. Null / disabled = the dash stays straight.")]
        [SerializeField] private AimAssistDefinition dashAimAssist;

        private Rigidbody rb;
        private PlayerMotor motor;
        private PlayerCombat combat;
        private ICharacterVisual visual;

        private bool isDashing;
        private float dashTimer;      // unscaled seconds left of the impulse
        private float dashElapsed;    // unscaled seconds into the impulse (curve time)
        private float invulnTimer;    // unscaled seconds left of IsInvulnerable
        private float cooldownTimer;  // unscaled seconds left until the dash is ready
        private Vector3 dashDir;      // locked facing/direction of the impulse
        private Vector3 baseVelocity; // horizontal velocity captured at dash start (additive mode, direction locked)

        /// <summary>True while the dash impulse is running (read-only seam).</summary>
        public bool IsDashing => isDashing;

        /// <summary>True during the invuln window right after the dash starts (~0.25 s).</summary>
        public bool IsInvulnerable => invulnTimer > 0f;

        /// <summary>Unscaled seconds left until the dash is ready again (0 = ready).</summary>
        public float CooldownRemaining => cooldownTimer;

        /// <summary>Minimum unscaled seconds the dash must run before an attack can cancel it (read-only debug seam).</summary>
        public float MinCancelTime => minCancelTime;

        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
            motor = GetComponent<PlayerMotor>();
            combat = GetComponent<PlayerCombat>();
            visual = GetComponent<ICharacterVisual>();
        }

        private void Update()
        {
            // Unscaled timers: cooldown/invuln survive hitstop timeScale = 0.
            if (cooldownTimer > 0f) cooldownTimer = Mathf.Max(0f, cooldownTimer - Time.unscaledDeltaTime);
            if (invulnTimer > 0f) invulnTimer = Mathf.Max(0f, invulnTimer - Time.unscaledDeltaTime);

            if (isDashing)
            {
                dashElapsed += Time.unscaledDeltaTime;
                dashTimer -= Time.unscaledDeltaTime;
                // Keep facing on the (possibly aim-corrected) dash direction: SetFacing
                // is incremental (~12°/frame), so a corrected direction converges over
                // the dash opening instead of lagging a single step. No-op when the dash
                // is uncorrected (dashDir already equals the current facing).
                if (visual != null) visual.SetFacing(dashDir);
                if (dashTimer <= 0f) EndDash();
                return; // no new dash while one runs
            }

            // Fire gate (decision f): off-cooldown + NOT during the hitstop freeze.
            bool pressed = combat != null && combat.ConsumeDashTap();
            if (!pressed || cooldownTimer > 0f || (combat != null && combat.IsHitstopped)) return;

            // Dash as INTERRUPTOR: cancel the active combo/swing, then dash.
            if (combat != null && combat.Attacking) combat.CancelCombo();
            StartDash();
        }

        /// <summary>Curve impulse while the dash runs: keep the vertical velocity
        /// (gravity/jump untouched). ADDITIVE: velocity = the base velocity captured at
        /// dash start (direction locked) + the curve impulse along the locked facing, so
        /// a run+dash is a burst ON TOP and the run continues at dash end. Legacy mode
        /// (additive = false): the curve REPLACES the whole horizontal velocity.</summary>
        private void FixedUpdate()
        {
            if (!isDashing || rb == null) return;
            float window = Mathf.Max(0.001f, dashDuration);
            float speed = Mathf.Max(0f, CurveMotion.Speed(dashCurve, dashDistance, window, dashElapsed));
            Vector3 v = rb.linearVelocity;
            if (additive)
            {
                v.x = baseVelocity.x + dashDir.x * speed;
                v.z = baseVelocity.z + dashDir.z * speed;
            }
            else
            {
                v.x = dashDir.x * speed;
                v.z = dashDir.z * speed;
            }
            rb.linearVelocity = v;
        }

        private void StartDash()
        {
            isDashing = true;
            dashTimer = Mathf.Max(0.001f, dashDuration);
            dashElapsed = 0f;
            if (!CurveMotion.IsUsable(dashCurve)) dashCurve = CurveMotion.DefaultEaseOut();
            invulnTimer = Mathf.Max(0f, invulnDuration);
            cooldownTimer = Mathf.Max(0f, cooldown);

            // Facing locked along the dash direction (motor.InputLocked keeps it:
            // input cannot rotate mid-dash). Fallback to transform.forward for
            // proxy/visual-less setups — same pattern as PlayerCombat.
            dashDir = FacingNow();

            // Aim-assist on dash START (slice 3sexies): the dash's OWN definition. If a
            // target sits inside the frontal cone + range, SNAP the dash direction toward it
            // (OFFENSIVE gap-close). Dashing AWAY — target behind or outside the cone —
            // finds nothing, so dashDir stays forward (DEFENSIVE).
            if (AimAssist.TryFindAimDir(transform.position, rb, dashDir, dashAimAssist, out Vector3 aim))
                dashDir = aim;

            if (visual != null) visual.SetFacing(dashDir);

            // ADDITIVE seam: capture the horizontal velocity at the dash MOMENT as a
            // full vector (direction locked too — it is NOT re-aimed at the facing).
            // Run + dash keeps this base through the whole dash and hands back control
            // at ~base speed at dash end; stand + dash captures ~0 (burst from zero).
            if (rb != null)
                baseVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
            else
                baseVelocity = Vector3.zero;

            if (motor != null) motor.InputLocked = true;
        }

        /// <summary>
        /// Attack-cancel seam (slice 3septies): an attack pressed during the dash cuts it
        /// SHORT — but only once the dash has run at least <see cref="minCancelTime"/>
        /// (unscaled), so the dash stays readable and cannot be cancelled at t=0. Returns
        /// true when the dash was actually cut (the caller starts the attack this same
        /// frame); false when no dash is running or the min window has not elapsed.
        /// Movement/iframes/cooldown are untouched: the cooldown was armed at dash START, so
        /// a cancelled dash still pays it. The released <see cref="PlayerMotor.InputLocked"/>
        /// lets PlayerCombat's attack entry take over immediately.
        /// </summary>
        public bool TryCancelByAttack()
        {
            if (!isDashing) return false;
            if (dashElapsed < minCancelTime) return false;
            EndDash();
            return true;
        }

        private void EndDash()
        {
            isDashing = false;
            dashTimer = 0f;
            // Release the lock -> PlayerCombat's attack buffer (fed/held during the dash)
            // fires this frame or next and starts the combo: dash -> attack -> combo.
            if (motor != null) motor.InputLocked = false; // rigorous release: control returns
        }

        private void OnDisable()
        {
            // Rigorous release even if the dash is killed mid-flight (disable/stop).
            isDashing = false;
            dashTimer = 0f;
            if (motor != null) motor.InputLocked = false;
        }

        /// <summary>Editor defaulting: an empty dashCurve shows as the shared ease-out.</summary>
        private void OnValidate()
        {
            if (!CurveMotion.IsUsable(dashCurve)) dashCurve = CurveMotion.DefaultEaseOut();
        }

        private Vector3 FacingNow()
        {
            Vector3 facing = visual != null ? visual.Facing : transform.forward;
            facing.y = 0f;
            return facing.sqrMagnitude > 0.0001f ? facing.normalized : Vector3.forward;
        }
    }
}