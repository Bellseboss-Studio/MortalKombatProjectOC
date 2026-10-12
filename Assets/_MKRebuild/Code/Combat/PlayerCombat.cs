using System.Collections.Generic;
using UnityEngine;

namespace MK.Rebuild
{
    /// <summary>
    /// M1 slice-1 combat controller — ISOLATED from the movement motor (decision i:
    /// PlayerMotor stays movement-only). Reads the generated <see cref="PlayerInput"/>
    /// actions, runs ONE grey-box attack at a time (no chaining, forward-only —
    /// decisions a/b), probes a sphere hitbox during the data-driven active window
    /// (<see cref="AttackDefinition.hitNorm"/>), and applies hitstop on connect.
    ///
    /// Hitstop (decision h): saves the CURRENT <c>Time.timeScale</c> and restores
    /// THAT value after the freeze — NEVER hardcodes 1, because the debug window
    /// (F4) cycles timeScale and restoring to 1 would break its slow-mo.
    ///
    /// Timing runs on SCALED time so hitstop (timeScale = 0) freezes the swing;
    /// the hitstop countdown itself runs on UNSCALED time so the freeze ends in
    /// real milliseconds. Hit feedback on the victim is unscaled too
    /// (see <see cref="HitReceiver"/>).
    /// No layers touched: hit detection filters by <see cref="HitReceiver"/>
    /// component (Grabbable pattern) and excludes the player's own Rigidbody
    /// (Face child-collider gotcha).
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody))]
    public sealed class PlayerCombat : MonoBehaviour
    {
        [Tooltip("Light attack definition (gamepad X / mouse left). Required.")]
        [SerializeField] private AttackDefinition lightAttack;

        [Tooltip("Heavy attack definition (gamepad Y / mouse right). Required.")]
        [SerializeField] private AttackDefinition heavyAttack;

        private PlayerInput input;
        private Rigidbody rb;
        private ICharacterVisual visual;

        // Active swing (one at a time — no chaining in slice 1).
        private AttackDefinition current;
        private float swingTime;                 // scaled seconds into the swing
        private readonly HashSet<HitReceiver> hitThisSwing = new HashSet<HitReceiver>();

        // Hitstop state.
        private float hitstopRemaining;          // unscaled seconds left frozen
        private float savedTimeScale;            // the EXACT previous value to restore

        private bool warnedMissingDefinition;

        /// <summary>True while a swing is active (read-only debug seam).</summary>
        public bool Attacking => current != null;

        /// <summary>Definition of the active swing, null when idle (read-only debug seam).</summary>
        public AttackDefinition CurrentAttack => current;

        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
            visual = GetComponent<ICharacterVisual>();
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
        }

        private void OnDestroy()
        {
            if (input != null) input.Dispose();
        }

        private void Update()
        {
            // Hitstop owns the frame: consume unscaled time, restore the PREVIOUS
            // timeScale on expiry, and ignore new input while frozen.
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

            if (current == null)
            {
                if (lightAttack != null && input.Player.LightAttack.WasPressedThisFrame())
                    StartSwing(lightAttack);
                else if (heavyAttack != null && input.Player.HeavyAttack.WasPressedThisFrame())
                    StartSwing(heavyAttack);
                else if ((input.Player.LightAttack.WasPressedThisFrame()
                          || input.Player.HeavyAttack.WasPressedThisFrame())
                         && !warnedMissingDefinition)
                {
                    warnedMissingDefinition = true;
                    Debug.LogWarning(
                        $"{name}: attack pressed but no AttackDefinition assigned in the inspector.", this);
                }
            }
            else
            {
                // Scaled time: hitstop (timeScale = 0) freezes the swing mid-window.
                swingTime += Time.deltaTime;
                if (swingTime >= current.duration) EndSwing();
            }
        }

        private void FixedUpdate()
        {
            if (current == null || hitstopRemaining > 0f) return;
            if (!IsInsideActiveWindow()) return;
            ProbeHitbox();
        }

        private bool IsInsideActiveWindow()
        {
            float t0 = Mathf.Clamp01(current.hitNorm.x) * current.duration;
            float t1 = Mathf.Clamp01(current.hitNorm.y) * current.duration;
            return swingTime >= t0 && swingTime <= t1;
        }

        /// <summary>
        /// Forward-only probe (decision a): the sphere is offset along the facing,
        /// so a target behind the player is NEVER in range. Filters by
        /// <see cref="HitReceiver"/> component, excludes the player's own
        /// Rigidbody (Face child collider shares it).
        /// </summary>
        private void ProbeHitbox()
        {
            Vector3 facing = visual != null ? visual.Facing : transform.forward;
            facing.y = 0f;
            if (facing.sqrMagnitude < 0.0001f) facing = Vector3.forward;
            facing.Normalize();

            // Local-space offset: x = sideways, y = height, z = forward (data-driven).
            Vector3 origin = transform.position
                             + Vector3.up * current.hitboxOffset.y
                             + facing * current.hitboxOffset.z;

            Collider[] cols = Physics.OverlapSphere(
                origin, current.hitboxRadius, Physics.AllLayers, QueryTriggerInteraction.Ignore);

            bool connected = false;
            for (int i = 0; i < cols.Length; i++)
            {
                var col = cols[i];
                if (col == null) continue;
                if (col.attachedRigidbody == rb) continue; // own capsule / Face trigger

                var receiver = col.GetComponentInParent<HitReceiver>();
                if (receiver == null) continue;
                if (!hitThisSwing.Add(receiver)) continue; // one hit per target per swing

                Vector3 to = receiver.transform.position - transform.position;
                to.y = 0f;
                if (to.sqrMagnitude < 0.0001f) to = facing;
                to.Normalize();

                receiver.TakeHit(current.damage, to * current.knockback);
                connected = true;
            }

            if (connected) BeginHitstop();
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

        private void BeginHitstop()
        {
            if (hitstopRemaining > 0f) return; // one freeze per swing
            savedTimeScale = Time.timeScale;   // may be 0.25 / 0.5 / 2 — restore THIS
            Time.timeScale = 0f;
            hitstopRemaining = Mathf.Max(0.001f, current.hitstopMs / 1000f);
        }

        // ------------------------------------------------------------------
        // Dev visualization (decision: make the grey-box hitbox legible).
        // Gizmos render ONLY in the editor / Scene view — zero cost in builds.
        // The origin math below is a DELIBERATE copy of ProbeHitbox so what you
        // see is exactly what the sphere query tests (offset.x is unused there).
        // ------------------------------------------------------------------

        /// <summary>World-space hitbox center for <paramref name="def"/> — mirrors ProbeHitbox.</summary>
        private Vector3 HitboxOrigin(AttackDefinition def)
        {
            Vector3 facing = FacingForGizmos();
            return transform.position
                   + Vector3.up * def.hitboxOffset.y
                   + facing * def.hitboxOffset.z;
        }

        /// <summary>
        /// Facing for gizmos: uses the cached visual, else resolves it lazily
        /// (Awake does not run in edit mode), else falls back to transform.forward.
        /// </summary>
        private Vector3 FacingForGizmos()
        {
            var v = visual != null ? visual : GetComponent<ICharacterVisual>();
            Vector3 facing = v != null ? v.Facing : transform.forward;
            facing.y = 0f;
            if (facing.sqrMagnitude < 0.0001f) facing = Vector3.forward;
            return facing.normalized;
        }

        private void OnDrawGizmos()
        {
            // During a swing: draw the LIVE hitbox. Red while the active hit
            // window is open, amber otherwise, so the connect frame is obvious.
            if (current == null) return;
            Gizmos.color = IsInsideActiveWindow()
                ? Color.red
                : new Color(1f, 0.6f, 0.1f, 0.6f);
            Gizmos.DrawWireSphere(HitboxOrigin(current), current.hitboxRadius);
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
            Gizmos.DrawWireSphere(HitboxOrigin(def), def.hitboxRadius);
        }
    }
}