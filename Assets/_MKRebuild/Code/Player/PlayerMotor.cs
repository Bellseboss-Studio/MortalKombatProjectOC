using UnityEngine;

namespace MK.Rebuild
{
    /// <summary>
    /// M0 slice: camera-relative walk + Mario-style jump + wall grab/wall-jump
    /// + ledge auto-mantle on a Rigidbody.
    /// All numbers come from <see cref="PlayerParams"/> — NO hardcoded constants.
    /// NO combat.
    /// Jump: variable height (cut on release) + asymmetric gravity + coyote.
    /// Facing is state-driven: LOCKED into walls/ledges and during the scripted
    /// wall-jump turn to the destination wall; FREE (input) otherwise.
    /// Input in Update, physics in FixedUpdate, own ground/wall checks via RaycastAll.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(CapsuleCollider))]
    public sealed class PlayerMotor : MonoBehaviour
    {
        [Tooltip("Single source of gameplay numbers. Required.")]
        [SerializeField] private PlayerParams paramsAsset;

        [Tooltip("Stick snap distance below feet that still counts as grounded.")]
        [SerializeField] private float groundSnap = 0.15f;

        [Tooltip("Downward stick force while grounded (keeps contact on Floor).")]
        [SerializeField] private float stickForce = -1f;

        private Rigidbody rb;
        private CapsuleCollider body;
        private ICharacterVisual visual;
        private PlayerInput input;

        private Vector2 moveInput;
        private Vector3 smoothVel;
        private float verticalVel;
        private float bufferTimer;
        private float coyoteTimer;
        private Transform cameraTransform;

        // Wall motion (M0): minimal contact state, no state machine.
        private bool wallContact;
        private bool wallGrabHeld; // sticky grab: persist contact while airborne
        private Vector3 wallNormal;
        private Collider wallCollider;        // wall hit by the probe this frame
        private Collider lastWallContacted;   // last wall actually touched (coyote jumps)
        private Collider lastWallJumpedFrom;  // banned collider until grounded / other contact
        private bool ledgeContact;            // overlapping a Ledge trigger this frame
        private Vector3 lockHoriz;
        private float wallJumpLockTimer;
        private float wallReGrabTimer;
        private float wallCoyoteTimer;

        // Mantle: animation-driven timed warp (A2: direct root set, no kinematic switch).
        private bool mantling;
        private Vector3 warpStart;
        private Vector3 warpEnd;
        private float warpT;

        // State-driven facing contract (animation readiness): facing is LOCKED while
        // on a wall/ledge or during a scripted wall-jump turn, and FREE (input) otherwise.
        private bool facingLocked;
        private Vector3 desiredFacing;
        private bool holdingWallJumpFacing; // scripted turn held through the whole flight
        private Vector3 wallJumpFacing;     // destination-wall direction (+wallNormal at the jump)
        private Vector3 lastWallNormal;     // last wall normal, for coyote wall-jumps

        /// <summary>True while facing is state-driven (wall/ledge/flight) and input cannot steer it.</summary>
        public bool FacingLocked => facingLocked;

        /// <summary>World-space direction the character should face this step (XZ). Zero = keep.</summary>
        public Vector3 DesiredFacing => desiredFacing;

        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
            body = GetComponent<CapsuleCollider>();
            visual = GetComponent<ICharacterVisual>();
            // One-time params seam for the concrete driver (turnSpeed stays data-driven).
            if (visual is AnimatorVisual animatorVisual) animatorVisual.Bind(paramsAsset);

            // M0 physical contract (also set in scene): interpolated, no torque.
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.freezeRotation = true;
            rb.useGravity = false;

            input = new PlayerInput();
            RefreshCamera();
        }

        private void EnsureInput()
        {
            // OnEnable can run without Awake after a domain reload
            // (exiting play mode): non-serialized fields are null there.
            if (input == null) input = new PlayerInput();
        }

        private void OnEnable()
        {
            EnsureInput();
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

        private void RefreshCamera()
        {
            var cam = Camera.main;
            cameraTransform = cam != null ? cam.transform : null;
        }

        private void Update()
        {
            if (paramsAsset == null || input == null) return;
            moveInput = input.Player.Move.ReadValue<Vector2>();
            if (input.Player.Jump.WasPressedThisFrame())
                bufferTimer = paramsAsset.jumpBuffer;
            // Mario variable height: release while ascending cuts upward velocity.
            if (input.Player.Jump.WasReleasedThisFrame() && verticalVel > 0f)
                verticalVel *= paramsAsset.jumpCutMultiplier;
        }

        private void FixedUpdate()
        {
            if (paramsAsset == null) return;
            if (cameraTransform == null) RefreshCamera();

            float dt = Time.fixedDeltaTime;
            bufferTimer = Mathf.Max(0f, bufferTimer - dt);

            // Mantle: animation-driven timed warp. Owns the root while active; input is
            // ignored and control returns exactly on arrival (A2: no kinematic switch).
            // Handled before the geometry probes so facing stays stable through the warp.
            if (mantling)
            {
                float duration = Mathf.Max(0.0001f, paramsAsset.mantleDuration);
                warpT += dt / duration;
                rb.linearVelocity = Vector3.zero;
                if (warpT >= 1f)
                {
                    rb.position = warpEnd; // snap exact — no overshoot
                    mantling = false;
                    verticalVel = 0f;
                    smoothVel = Vector3.zero;
                    lockHoriz = Vector3.zero;
                }
                else
                {
                    rb.position = Vector3.Lerp(warpStart, warpEnd, Mathf.SmoothStep(0f, 1f, warpT));
                }
                ExposeMantleState();
                return;
            }

            bool grounded = CheckGrounded();
            // Touching the ground clears the re-grab ban (and the coyote source).
            if (grounded)
            {
                lastWallJumpedFrom = null;
                lastWallContacted = null;
                holdingWallJumpFacing = false; // land -> facing is free again
            }

            CheckWall();
            CheckLedge();
            if (wallContact)
            {
                lastWallContacted = wallCollider;
                lastWallNormal = wallNormal;
            }

            // Grabbing a DIFFERENT wall/ledge also clears the re-grab ban.
            if (wallContact && wallCollider != null && wallCollider != lastWallJumpedFrom)
                lastWallJumpedFrom = null;
            if (ledgeContact)
                lastWallJumpedFrom = null; // same-root ledges were already ignored

            // Wall timers.
            wallJumpLockTimer = Mathf.Max(0f, wallJumpLockTimer - dt);
            wallReGrabTimer = Mathf.Max(0f, wallReGrabTimer - dt);

            // A wall is grabbable only while airborne and not in a jump/mantle
            // lockout. Gating with !grounded stops the ground jump from being
            // clamped by the hang slide later in this same FixedUpdate.
            bool wallGrab = !grounded
                && wallContact
                && wallReGrabTimer <= 0f
                && wallJumpLockTimer <= 0f
                && !mantling;

            // Ledge hang: overlapping a Ledge trigger while airborne (works with
            // or without a Grabbable wall underneath).
            bool ledgeHang = !grounded
                && ledgeContact
                && wallReGrabTimer <= 0f
                && wallJumpLockTimer <= 0f
                && !mantling;

            bool hanging = wallGrab || ledgeHang;

            // Grabbing any surface ends the scripted wall-jump flight turn; from here
            // the hang/mantle facing rules take over.
            if (hanging) holdingWallJumpFacing = false;

            // Coyote: refresh grace window while grounded, decay while airborne.
            if (grounded)
                coyoteTimer = paramsAsset.coyoteTime;
            else
                coyoteTimer = Mathf.Max(0f, coyoteTimer - dt);

            // Wall coyote: grace to chain a wall-jump after leaving a wall.
            if (wallGrab)
                wallCoyoteTimer = paramsAsset.wallCoyoteTime;
            else
                wallCoyoteTimer = Mathf.Max(0f, wallCoyoteTimer - dt);

            // Down while hanging: detach with a small up+away hop. Checked first
            // so it never fights the jump/mantle branches.
            if (hanging && moveInput.y <= paramsAsset.wallReleaseThreshold)
            {
                verticalVel = paramsAsset.wallReleaseHop;
                lockHoriz = HangAwayDir() * paramsAsset.wallReleaseHop;
                wallJumpLockTimer = paramsAsset.wallJumpLockTime;
                wallReGrabTimer = paramsAsset.wallReGrabDelay;
                wallCoyoteTimer = 0f;
                wallContact = false;
                wallGrab = false;
                ledgeHang = false;
                hanging = false;
                holdingWallJumpFacing = false; // release -> facing is free again
            }
            // Jump: ground takes priority, then hang (mantle if at a Ledge).
            else if (bufferTimer > 0f && (grounded || coyoteTimer > 0f))
            {
                verticalVel = paramsAsset.jumpVelocity;
                bufferTimer = 0f;
                coyoteTimer = 0f;
                grounded = false;
                hanging = false;
            }
            else if (bufferTimer > 0f && !grounded && (hanging || wallCoyoteTimer > 0f))
            {
                bufferTimer = 0f;
                if (ledgeHang)
                {
                    // Manual mantle: hanging at a Ledge + Jump -> climb the top.
                    ApplyMantle();
                    wallGrab = false;
                    hanging = false;
                }
                else
                {
                    // Wall-jump: up + push-off away from the wall, then lock control
                    // and ban re-grabbing the SAME wall until grounded/other contact.
                    verticalVel = paramsAsset.wallJumpVelocity;
                    lockHoriz = wallNormal * paramsAsset.wallJumpPushOff;
                    wallJumpLockTimer = paramsAsset.wallJumpLockTime;
                    wallReGrabTimer = paramsAsset.wallReGrabDelay;
                    lastWallJumpedFrom = lastWallContacted != null ? lastWallContacted : wallCollider;
                    // Scripted turn: face the destination wall (+wallNormal, away from the
                    // wall you left) and hold it through the whole flight. Falls back to the
                    // last known normal on a coyote wall-jump with no fresh contact.
                    Vector3 destDir = wallNormal.sqrMagnitude > 0.0001f ? wallNormal : lastWallNormal;
                    if (destDir.sqrMagnitude > 0.0001f)
                    {
                        wallJumpFacing = destDir.normalized;
                        holdingWallJumpFacing = true;
                    }
                    wallCoyoteTimer = 0f;
                    wallContact = false;
                    wallGrab = false;
                    ledgeHang = false;
                    hanging = false;
                }
            }
            else if (hanging && paramsAsset.autoMantle && ledgeHang)
            {
                // Auto-mantle (opt-in): a Ledge is in range -> over the top.
                ApplyMantle();
                wallGrab = false;
                hanging = false;
            }

            // Mantle triggered THIS frame: skip physics/control now and expose the state
            // immediately (the warp proper runs on the next FixedUpdate).
            if (mantling)
            {
                ExposeMantleState();
                return;
            }

            if (grounded)
            {
                if (verticalVel < 0f) verticalVel = stickForce;
            }
            else if (hanging)
            {
                // Hang: hold vertical at the tunable slide speed (0 = full stop).
                verticalVel = paramsAsset.wallSlideSpeed;
            }
            else
            {
                // Mario asymmetric gravity: float up, snap down.
                float g = paramsAsset.gravity;
                if (verticalVel <= 0f) g *= paramsAsset.fallGravityMultiplier;
                verticalVel += g * dt;
                verticalVel = Mathf.Max(verticalVel, paramsAsset.maxFallSpeed);
            }

            // Camera-relative wish direction on XZ (UNIT vector: the stick magnitude
            // is handled separately by the fixed input zones below, never scaling speed).
            Vector3 wishDir = CameraRelative(moveInput);

            // Fixed DISCRETE target speed by input zone: deadzone / walk / run.
            // NOT analog-scaled by how far the stick is pushed.
            float inputMagnitude = moveInput.magnitude;
            float targetSpeed;
            bool running = false;
            if (inputMagnitude < paramsAsset.walkInputThreshold)
            {
                targetSpeed = 0f; // idle / deadzone
            }
            else if (inputMagnitude <= paramsAsset.runInputThreshold)
            {
                targetSpeed = paramsAsset.walkSpeed; // walk zone (fixed)
            }
            else
            {
                targetSpeed = paramsAsset.runSpeed; // run zone (fixed)
                running = true;
            }
            bool moving = targetSpeed > 0.0001f;

            Vector3 horizontal;
            if (wallJumpLockTimer > 0f)
            {
                // Control locked: keep the impulse, skip the wish.
                horizontal = lockHoriz;
                smoothVel = lockHoriz;
            }
            else
            {
                Vector3 target = wishDir * targetSpeed;
                float smoothTime = moving
                    ? paramsAsset.accelTime
                    : paramsAsset.decelTime;
                if (!grounded) smoothTime /= Mathf.Max(0.01f, paramsAsset.airControlFactor);

                horizontal = Vector3.SmoothDamp(
                    new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z),
                    target, ref smoothVel, Mathf.Max(0.0001f, smoothTime), Mathf.Infinity, dt);
            }

            rb.linearVelocity = new Vector3(horizontal.x, verticalVel, horizontal.z);

            // --- State-driven facing contract (animation readiness) -------------
            // Locked states face geometry (input cannot steer); Free follows input.
            Vector3 faceDir = Vector3.zero;
            bool locked = false;

            if (holdingWallJumpFacing)
            {
                // Wall-jump: scripted turn to the destination wall, held the whole flight.
                locked = true;
                faceDir = wallJumpFacing;
            }
            else if (hanging)
            {
                // Wall grab / ledge hang: LOCKED into the surface/edge. Input ignored.
                locked = true;
                faceDir = HangIntoDir();
            }
            else if (wishDir.sqrMagnitude > 0.000001f && moving)
            {
                // Free: input/movement direction (only once past the deadzone).
                faceDir = wishDir;
            }

            facingLocked = locked;
            desiredFacing = faceDir;

            // Movement state for the animator: most specific active state wins.
            MoveState moveState;
            if (mantling) moveState = MoveState.Mantle;
            else if (holdingWallJumpFacing) moveState = MoveState.WallJump;
            else if (ledgeHang) moveState = MoveState.LedgeHang;
            else if (wallGrab) moveState = MoveState.WallGrab;
            else moveState = MoveState.Free;

            if (visual != null)
            {
                if (faceDir.sqrMagnitude > 0.000001f) visual.SetFacing(faceDir);
                visual.SetGrounded(grounded);
                visual.SetMoveState(moveState);
                // Animation hooks: XZ speed picks Idle/Walk; vertical velocity picks
                // the jump sub-phases (JumpStart → FlipMidAir → Landing).
                visual.SetSpeed(new Vector2(rb.linearVelocity.x, rb.linearVelocity.z).magnitude);
                visual.SetVerticalSpeed(rb.linearVelocity.y);
                // Discrete input zone: true = run (fixed runSpeed), false = walk/idle.
                visual.SetRunning(running);
            }

            // Sticky grab: persist while airborne on a grabbable wall, so contact
            // no longer depends on holding a direction toward the wall.
            wallGrabHeld = wallGrab;
        }

        private Vector3 CameraRelative(Vector2 stick)
        {
            if (stick.sqrMagnitude < 0.000001f) return Vector3.zero;
            Vector3 forward = cameraTransform != null ? cameraTransform.forward : Vector3.forward;
            forward.y = 0f;
            if (forward.sqrMagnitude < 0.000001f) forward = Vector3.forward;
            forward.Normalize();
            Vector3 right = new Vector3(forward.z, 0f, -forward.x);
            Vector3 dir = forward * stick.y + right * stick.x;
            return dir.sqrMagnitude > 0.000001f ? dir.normalized : Vector3.zero;
        }

        private bool CheckGrounded()
        {
            Vector3 origin = body.bounds.center;
            float maxDist = body.height * 0.5f + groundSnap;
            var hits = Physics.RaycastAll(origin, Vector3.down, maxDist,
                Physics.AllLayers, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < hits.Length; i++)
            {
                if (hits[i].collider != null && hits[i].collider != body)
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Horizontal wall probe. Accepts only near-vertical faces belonging to a
        /// different rigidbody (ignores the Player's own Face child collider).
        /// </summary>
        private void CheckWall()
        {
            wallContact = false;
            wallCollider = null;

            // Keep the previous normal before clearing it: the sticky probe below
            // needs it to aim back into the wall being held.
            Vector3 prevNormal = wallNormal;
            wallNormal = Vector3.zero;

            Vector3 probe;
            if (wallGrabHeld && prevNormal.sqrMagnitude > 0.000001f)
            {
                // Sticky grab: keep probing into the wall being held so contact
                // persists with no movement input.
                probe = -prevNormal;
            }
            else
            {
                probe = CameraRelative(moveInput);
                if (probe.sqrMagnitude < 0.000001f)
                    probe = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
                if (probe.sqrMagnitude < 0.000001f) return;
            }
            probe.y = 0f;
            probe.Normalize();

            Vector3 origin = body.bounds.center;
            var hits = Physics.RaycastAll(origin, probe, paramsAsset.wallCheckDistance,
                Physics.AllLayers, QueryTriggerInteraction.Ignore);
            float best = float.MaxValue;
            for (int i = 0; i < hits.Length; i++)
            {
                var hit = hits[i];
                if (hit.collider == null || hit.collider == body) continue;
                if (hit.rigidbody == rb) continue; // Face / own children
                // Re-grab ban: ignore the exact wall we just wall-jumped from until
                // grounded or a different wall/ledge is grabbed.
                if (hit.collider == lastWallJumpedFrom) continue;
                if (hit.collider.GetComponentInParent<Grabbable>() == null) continue; // only Grabbable sides
                if (Mathf.Abs(hit.normal.y) >= paramsAsset.wallNormalThreshold) continue; // floor/ceiling
                if (hit.distance >= best) continue;
                best = hit.distance;
                wallContact = true;
                wallNormal = hit.normal;
                wallCollider = hit.collider;
            }
        }

        /// <summary>
        /// Starts the animation-driven mantle warp: capture the hang pose and the exact
        /// stand pose (wall top + half height), then let FixedUpdate interpolate. Shared
        /// by the opt-in auto path (<c>autoMantle</c>) and the manual Jump path.
        /// </summary>
        private void ApplyMantle()
        {
            warpStart = rb.position;
            warpEnd = ComputeWarpEnd();
            warpT = 0f;
            mantling = true;
            wallCoyoteTimer = 0f;
            wallContact = false;
        }

        /// <summary>
        /// Exact stand pose over a ledge: wall top + half height, XZ = slab center +
        /// inward offset (offset = body radius; tune if the capsule clips the lip).
        /// With no Grabbable wall the top is not derivable — Ledge-based derivation is
        /// deferred, so fall back to a body-sized climb that always releases control
        /// instead of crashing.
        /// </summary>
        private Vector3 ComputeWarpEnd()
        {
            Vector3 into = HangIntoDir();
            if (wallCollider != null)
            {
                Bounds wall = wallCollider.bounds;
                // Inward offset from the slab center to clear the lip. Tuned to 0:
                // with this 0.5-wide slab, center + body.radius (0.5) landed past the
                // top footprint (x=2.0 > 1.75) and the capsule fell off the far side.
                // The slab center is the stand, so the offset stays 0 here.
                float offset = 0f;
                return new Vector3(
                    wall.center.x + into.x * offset,
                    wall.max.y + body.height * 0.5f,
                    wall.center.z + into.z * offset);
            }

            return new Vector3(
                warpStart.x + into.x * body.radius,
                warpStart.y + body.height,
                warpStart.z + into.z * body.radius);
        }

        /// <summary>
        /// Locks facing into/over the edge and reports <see cref="MoveState.Mantle"/> to
        /// the visual. Shared by the trigger frame and the warp frames.
        /// </summary>
        private void ExposeMantleState()
        {
            facingLocked = true;
            desiredFacing = HangIntoDir();
            if (visual != null)
            {
                if (desiredFacing.sqrMagnitude > 0.000001f) visual.SetFacing(desiredFacing);
                visual.SetGrounded(false);
                visual.SetMoveState(MoveState.Mantle);
                // Warp velocity is zeroed — mirrors the controller out of locomotion.
                visual.SetSpeed(0f);
                visual.SetVerticalSpeed(0f);
                visual.SetRunning(false);
            }
            wallGrabHeld = false;
            wallContact = false;
        }

        /// <summary>
        /// Ledge detection via the <see cref="Ledge"/> marker trigger: true when the
        /// player's body overlaps a Ledge zone while airborne. The wall we just
        /// jumped from is ignored (its ledge too) until grounded/other contact.
        /// </summary>
        private bool CheckLedge()
        {
            ledgeContact = false;
            Vector3 center = body.bounds.center;
            Vector3 half = body.bounds.extents;
            var cols = Physics.OverlapBox(center, half, Quaternion.identity,
                Physics.AllLayers, QueryTriggerInteraction.Collide);
            for (int i = 0; i < cols.Length; i++)
            {
                var col = cols[i];
                if (col == null) continue;
                if (col.attachedRigidbody == rb) continue; // own Face trigger / capsule
                var ledge = col.GetComponentInParent<Ledge>();
                if (ledge == null) continue;
                if (lastWallJumpedFrom != null
                    && ledge.transform.root == lastWallJumpedFrom.transform.root) continue;
                ledgeContact = true;
                return true;
            }
            return false;
        }

        /// <summary>Direction from the hang point INTO the surface/edge (over the top).</summary>
        private Vector3 HangIntoDir()
        {
            // On a Grabbable wall the inward direction is opposite the wall normal.
            if (wallNormal.sqrMagnitude > 0.000001f) return -wallNormal;
            // Ledge without a wall: climb toward the player's facing (they jumped in).
            Vector3 facing = visual != null ? visual.Facing : transform.forward;
            facing.y = 0f;
            return facing.sqrMagnitude > 0.000001f ? facing.normalized : Vector3.zero;
        }

        /// <summary>Direction from the hang point AWAY from the surface/edge.</summary>
        private Vector3 HangAwayDir()
        {
            if (wallNormal.sqrMagnitude > 0.000001f) return wallNormal;
            return -HangIntoDir();
        }
    }
}
