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
        private Vector3 wishDir; // cached camera-relative input direction (XZ unit, zero = no input)
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
        private float ledgeReGrabTimer; // drop-through ban after a Down-release from a ledge
        private Collider ledgeBanCollider; // THAT ledge only: ignored until the ban expires
        private Collider ledgeCollider;   // ledge trigger overlapped this frame (if any)
        private bool wasLedgeHang; // previous frame's ledge hang: latches the hang while sliding (slide zeroes verticalVel, so the fall gate alone would flicker)
        private float wallCoyoteTimer;

        // Mantle: animation-driven timed warp (A2: direct root set, no kinematic switch).
        private bool mantling;
        private Vector3 warpStart;
        private Vector3 warpEnd;
        private Vector3 mantleDir; // captured at ApplyMantle: into/over the edge (XZ unit)
        private float warpT;

        // Last ground-probe result (cached for the read-only debug seam).
        // Updated every FixedUpdate by CheckGrounded().
        private bool grounded;

        // State-driven facing contract (animation readiness): facing is LOCKED while
        // on a wall/ledge or during a scripted wall-jump turn, and FREE (input) otherwise.
        private bool facingLocked;
        private Vector3 desiredFacing;
        private bool holdingWallJumpFacing; // scripted turn held through the whole flight
        private Vector3 wallJumpFacing;     // destination-wall direction (+wallNormal at the jump)
        private Vector3 lastWallNormal;     // last wall normal, for coyote wall-jumps

        /// <summary>True while facing is state-driven (wall/ledge/flight) and input cannot steer it.</summary>
        public bool FacingLocked => facingLocked;

        /// <summary>
        /// Attack movement lock (combat seam, default false). While true the motor
        /// IGNORES the horizontal wish input and PRESERVES the current horizontal
        /// velocity, so <see cref="PlayerCombat"/> can drive the per-step lunge;
        /// the facing stops following the input. Gravity / jump / wall state are
        /// untouched — the motor stays the movement owner.
        /// </summary>
        public bool InputLocked { get; set; }

        /// <summary>
        /// True when the ground probe found a floor collider on the latest step.
        /// Read-only, no setter: exposed so the isolated debug module can observe
        /// the motor without any game→debug coupling.
        /// </summary>
        public bool IsGrounded => grounded;

        /// <summary>World-space direction the character should face this step (XZ). Zero = keep.</summary>
        public Vector3 DesiredFacing => desiredFacing;

        /// <summary>
        /// Camera-relative INPUT direction on XZ (UNIT vector; zero when there is no
        /// move input). Read-only seam (slice 3octies): the DASH reuses the SAME input
        /// direction the walk uses, so a dash follows the stick, not the facing.
        /// </summary>
        public Vector3 WishDirection => wishDir;

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
                    // Snap exact — no overshoot — then ONE step forward past the edge:
                    // the warp lands inside the ledge trigger's overhang, so without
                    // the step the next frame would hang again instead of going Free.
                    rb.position = StepPastLedge(warpEnd);
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

            grounded = CheckGrounded();
            // Touching the ground clears the re-grab ban (and the coyote source).
            if (grounded)
            {
                lastWallJumpedFrom = null;
                lastWallContacted = null;
                ledgeBanCollider = null; // landed: the drop-through ban is spent
                ledgeReGrabTimer = 0f;
                wasLedgeHang = false; // landed: a walk-off starts falling fresh, never latched
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

            // Wall timers. An expired ledge ban releases its collider.
            wallJumpLockTimer = Mathf.Max(0f, wallJumpLockTimer - dt);
            wallReGrabTimer = Mathf.Max(0f, wallReGrabTimer - dt);
            ledgeReGrabTimer = Mathf.Max(0f, ledgeReGrabTimer - dt);
            if (ledgeReGrabTimer <= 0f) ledgeBanCollider = null;

            // A wall is grabbable only while airborne and not in a jump/mantle
            // lockout. Gating with !grounded stops the ground jump from being
            // clamped by the hang slide later in this same FixedUpdate.
            // Facing-gated: no grabbing with the back to the wall (slides/falls).
            bool wallGrab = !grounded
                && wallContact
                && FacingTowardWall()
                && wallReGrabTimer <= 0f
                && wallJumpLockTimer <= 0f
                && !mantling;

            // Ledge hang: overlapping a Ledge trigger while FALLING onto the lip
            // (works with or without a Grabbable wall underneath). The fall-only
            // gate stops a rising jump from auto-hanging when it passes the lip;
            // the wasLedgeHang latch keeps an ESTABLISHED hang stable (the hang
            // slide zeroes verticalVel, so the fall gate alone would flicker).
            // The latch is ledge-only: a wall-jump rising past a lip does NOT hang.
            bool ledgeHang = !grounded
                && ledgeContact
                && (verticalVel < 0f || wasLedgeHang)
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
                if (ledgeHang && ledgeCollider != null)
                {
                    // Drop-through: ignore THAT ledge until the ban expires, so
                    // the player falls past without re-hanging. Other lips stay live.
                    ledgeBanCollider = ledgeCollider;
                    ledgeReGrabTimer = paramsAsset.ledgeReGrabDelay;
                }
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

            // Snap-to-grip: on ledge-grab start, settle the root to the authored
            // hang pose (same XZ, Y = lip top - gripDepth) so every hang reads
            // identical instead of the variable physics overlap position. Instant
            // (industry standard) — no visible slide. Skipped when a mantle or
            // release fired this frame (the warp/impulse owns the root there).
            if (ledgeHang && !wasLedgeHang && ledgeCollider != null)
                SnapToGrip();

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
            // Cached on a field: the read-only seam PlayerDash reads at dash start so the
            // dash follows the INPUT (slice 3octies), not the facing.
            wishDir = CameraRelative(moveInput);

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
            else if (InputLocked)
            {
                // Attack lock (combat seam): skip the wish and preserve the
                // horizontal velocity PlayerCombat is applying (the lunge).
                // Gravity / jump / wall state are untouched.
                horizontal = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
                smoothVel = horizontal;
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
            else if (InputLocked)
            {
                // Attack lock: hold the swing facing. faceDir stays zero so the
                // visual keeps its last facing — input cannot rotate mid-swing.
                locked = true;
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
            // Latch the ledge hang for next frame's gate. `&& hanging` (not just
            // ledgeHang) so a release/mantle/wall-jump this frame unlatches.
            wasLedgeHang = ledgeHang && hanging;
        }

        private Vector3 CameraRelative(Vector2 stick)
        {
            // Shared with the DASH (slice 3octies): one stick-to-world mapping.
            return CameraRelativeMotion.WishDirection(cameraTransform, stick);
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
        /// Facing gate for the wall grab: true when the character faces INTO the
        /// wall (dot(facing, -wallNormal) above threshold). Back-to-wall contact
        /// slides/falls instead of grabbing. An established grab persists (sticky):
        /// while held the facing is locked into the wall, so the gate stays true.
        /// The wall-jump chain still works: the scripted flight facing points at
        /// the destination wall, so the next grab passes the gate.
        /// </summary>
        private bool FacingTowardWall()
        {
            Vector3 facing = visual != null ? visual.Facing : transform.forward;
            facing.y = 0f;
            if (facing.sqrMagnitude < 0.000001f) return false;
            Vector3 into = -wallNormal;
            into.y = 0f;
            if (into.sqrMagnitude < 0.000001f) return false;
            return Vector3.Dot(facing.normalized, into.normalized) > paramsAsset.facingDotThreshold;
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
            // Capture the climb direction NOW: the warp frames skip the wall probe,
            // so HangIntoDir would be stale by landing time.
            mantleDir = HangIntoDir();
            mantleDir.y = 0f;
            mantleDir = mantleDir.sqrMagnitude > 0.000001f ? mantleDir.normalized : Vector3.zero;
            warpT = 0f;
            mantling = true;
            wasLedgeHang = false; // mantle owns the edge now; the early-return warp frames skip the latch update
            wallCoyoteTimer = 0f;
            wallContact = false;
        }

        /// <summary>
        /// Exact stand pose over a ledge: wall top + half height, XZ = the player's
        /// own XZ at mantle start (same-line climb, NOT the slab center).
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
                // Same-line climb: land at the player's own XZ (warpStart), only the
                // Y comes from the wall top. Using the slab center teleported the
                // character sideways on off-center hangs.
                return new Vector3(
                    warpStart.x,
                    wall.max.y + body.height * 0.5f,
                    warpStart.z);
            }

            return new Vector3(
                warpStart.x + into.x * body.radius,
                warpStart.y + body.height,
                warpStart.z + into.z * body.radius);
        }

        /// <summary>
        /// One step forward from the mantle landing, along the captured mantle
        /// direction (<c>mantleStepForward</c> meters). The warp lands at the same
        /// XZ, still inside the ledge trigger's overhang — the step puts solid top
        /// under the character so the next frame reads grounded (Free/idle) instead
        /// of hanging. Same height as the warp end: never drops or floats. When the
        /// full step would leave the top (thin geometry) it shrinks until solid top
        /// is beneath, down to standing exactly on the warp end.
        /// </summary>
        private Vector3 StepPastLedge(Vector3 land)
        {
            if (mantleDir.sqrMagnitude < 0.000001f) return land;
            float step = paramsAsset != null ? Mathf.Max(0f, paramsAsset.mantleStepForward) : 0f;
            float fraction = 1f;
            for (int i = 0; i < 4; i++)
            {
                Vector3 candidate = land + mantleDir * (step * fraction);
                candidate.y = land.y;
                if (HasTopBeneath(candidate)) return candidate;
                fraction *= 0.5f;
            }
            return land;
        }

        /// <summary>True when solid (non-trigger) geometry sits within ground-snap below the pose.</summary>
        private bool HasTopBeneath(Vector3 center)
        {
            float maxDist = body.height * 0.5f + groundSnap;
            var hits = Physics.RaycastAll(center, Vector3.down, maxDist,
                Physics.AllLayers, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < hits.Length; i++)
            {
                var hit = hits[i];
                if (hit.collider == null || hit.collider == body) continue;
                if (hit.rigidbody == rb) continue; // own Face / children
                return true;
            }
            return false;
        }

        /// <summary>
        /// Locks facing into/over the edge and reports <see cref="MoveState.Mantle"/> to
        /// the visual. Shared by the trigger frame and the warp frames.
        /// </summary>
        private void ExposeMantleState()
        {
            facingLocked = true;
            desiredFacing = HangIntoDir();
            grounded = false; // the warp is airborne; keep the debug readout honest
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
        /// player's HANDS band overlaps a Ledge zone while airborne — not just any
        /// part of the body. The wall we just jumped from is ignored (its ledge
        /// too) until grounded/other contact.
        /// NOTE: overlap alone does NOT hang — the hang gate additionally requires
        /// falling (verticalVel &lt; 0) plus the re-grab timers (see FixedUpdate).
        /// </summary>
        private bool CheckLedge()
        {
            ledgeContact = false;
            ledgeCollider = null;
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
                if (col == ledgeBanCollider) continue; // drop-through ban: ignore THAT ledge
                if (lastWallJumpedFrom != null
                    && ledge.transform.root == lastWallJumpedFrom.transform.root) continue;
                // Hand-context: the trigger must overlap the HANDS band (chest
                // height), not feet/head — passing the lip at other heights does
                // NOT hang. First body touch happens with the feet, so the hang
                // fires as the fall brings the hands down to the lip. Skipped for
                // an ESTABLISHED hang (latch): the snap settles the root below the
                // band, but the body still overlaps — the latch owns stability.
                if (!wasLedgeHang && !OverlapsHandsBand(col)) continue;
                ledgeContact = true;
                ledgeCollider = col;
                return true;
            }
            // No overlap: the latch dies with the contact (also clears stale state
            // across teleports — a fresh overlap always starts falling, never latched).
            wasLedgeHang = false;
            return false;
        }

        /// <summary>
        /// True when the ledge trigger overlaps the hands band: a body-wide slice
        /// at <c>hangGripHeight</c> above the body center, as thick as the trigger
        /// itself. Feet/head-level passes miss the band and do not hang.
        /// </summary>
        private bool OverlapsHandsBand(Collider ledgeCol)
        {
            Vector3 hands = body.bounds.center + Vector3.up * paramsAsset.hangGripHeight;
            Vector3 half = body.bounds.extents;
            half.y = ledgeCol.bounds.extents.y;
            var cols = Physics.OverlapBox(hands, half, Quaternion.identity,
                Physics.AllLayers, QueryTriggerInteraction.Collide);
            for (int i = 0; i < cols.Length; i++)
                if (cols[i] == ledgeCol) return true;
            return false;
        }

        /// <summary>
        /// Authored grip pose: hands at the lip (lip top edge), body hanging below
        /// at <c>gripDepth</c>, same line (XZ untouched — never yanks sideways),
        /// facing locked by the existing hang contract. Instant on grab.
        /// </summary>
        private void SnapToGrip()
        {
            float lipTop = ledgeCollider.bounds.max.y;
            rb.position = new Vector3(
                rb.position.x,
                lipTop - paramsAsset.gripDepth,
                rb.position.z);
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
