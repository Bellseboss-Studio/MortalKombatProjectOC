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

        [Tooltip("Seconds between footsteps while grounded and moving.")]
        [SerializeField] private float footstepInterval = 0.4f;

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
        private float jumpLockoutTimer; // landing recovery: blocks a buffered re-fire on touchdown
        private float footstepTimer;    // counts down to the next footstep while grounded + moving
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
        private Vector3 ledgeInto;        // captured XZ INTO-edge direction of the current ledge hang (grab-time geometry)
        private bool wasLedgeHang; // previous frame's ledge hang: latches the hang while sliding (slide zeroes verticalVel, so the fall gate alone would flicker)
        private float wallCoyoteTimer;

        // Mantle: animation-driven timed warp (A2: direct root set, no kinematic switch).
        private bool mantling;
        private Vector3 warpStart;
        private Vector3 warpEnd;
        private Vector3 warpControl; // Bezier control: lip corner pushed up + outward (arcs over the lip)
        private Vector3 mantleDir; // captured at ApplyMantle: into/over the edge (XZ unit)
        private float warpT;

        // Last ground-probe result (cached for the read-only debug seam).
        // Updated every FixedUpdate by CheckGrounded().
        private bool grounded;
        // Previous step's probe: edge-detects touchdown (airborne -> grounded).
        private bool wasGrounded = true; // suppress the spawn "land" thud on the first grounded frame

        // 4 cardinal ground-probe origins (+X, -X, +Z, -Z around the base footprint).
        // Reused buffer — no per-frame allocation. Shared by CheckGrounded and the gizmo.
        private readonly Vector3[] groundRayOrigins = new Vector3[4];
        // Tiny upward nudge so each ray starts just above the feet surface level.
        private const float GroundRayStartEpsilon = 0.02f;

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
        /// Prioritized body-control arbiter (m2-hazards S2). Created in <see cref="Awake"/>;
        /// every lock writer acquires/releases its own <see cref="BodyOwnership.Token"/>.
        /// </summary>
        public BodyOwnership Body { get; private set; }

        /// <summary>
        /// Movement lock DERIVED from the arbiter: true while ANY system holds a body
        /// token (attack / dash / death / knockback). No setter — writers acquire and
        /// release their own token. While true the motor IGNORES the horizontal wish
        /// input and PRESERVES the current horizontal velocity, the facing stops
        /// following the input, and gravity / jump / wall state stay untouched.
        /// </summary>
        public bool InputLocked => Body.IsHeld;

        /// <summary>
        /// Current body owner (highest-priority held token), or null when free. Read-only
        /// seam for semantic checks that need to know WHICH system holds the body
        /// (e.g. PlayerCombat's dash-lock reads).
        /// </summary>
        public BodyPriority? BodyOwner => Body.Owner;

        /// <summary>
        /// True when the ground probe found a floor collider on the latest step.
        /// Read-only, no setter: exposed so the isolated debug module can observe
        /// the motor without any game→debug coupling.
        /// </summary>
        public bool IsGrounded => grounded;

        /// <summary>
        /// Current logical movement state (last committed in FixedUpdate). Single source
        /// for BOTH the visual and the action gate (<see cref="CanPerform"/>).
        /// </summary>
        public MoveState State { get; private set; }

        /// <summary>
        /// Central action gate (change action-gating): true when <paramref name="action"/>
        /// is legal in the current <see cref="State"/> + grounded state. Consumers
        /// (combat / dash / abilities / this motor's horizontal input) early-return when
        /// this is false. Deny by default — see <see cref="PlayerActionGate"/>.
        /// </summary>
        public bool CanPerform(PlayerAction action) => PlayerActionGate.Allows(State, grounded, action);

        /// <summary>World-space direction the character should face this step (XZ). Zero = keep.</summary>
        public Vector3 DesiredFacing => desiredFacing;

        /// <summary>
        /// Camera-relative INPUT direction on XZ (UNIT vector; zero when there is no
        /// move input). Read-only seam (slice 3octies): the DASH reuses the SAME input
        /// direction the walk uses, so a dash follows the stick, not the facing.
        /// </summary>
        public Vector3 WishDirection => wishDir;

        /// <summary>
        /// Hard-sets this actor's root pose (m2-respawn slice 1) and CLEARS every stale
        /// motion cache, so a leftover mantle warp, hang slide, wall-jump impulse or
        /// re-grab latch cannot carry across the move. This is the respawn teleport seam —
        /// the motor stays the movement owner (the checkpoint system does NOT poke the
        /// Rigidbody directly). Facing is kept.
        /// </summary>
        public void Teleport(Vector3 position)
        {
            Teleport(position, Vector3.zero);
        }

        /// <summary>
        /// Hard-sets the root to <paramref name="position"/> (and snaps the facing root to
        /// <paramref name="facing"/> when non-zero), then clears the stale motion caches.
        /// See <see cref="Teleport(Vector3)"/>.
        /// </summary>
        public void Teleport(Vector3 position, Vector3 facing)
        {
            // Kill the mantle warp outright; warpT is only meaningful while mantling.
            mantling = false;
            warpT = 0f;
            // Velocity + movement smoothing: nothing from the previous life carries over.
            verticalVel = 0f;
            smoothVel = Vector3.zero;
            lockHoriz = Vector3.zero;
            // Wall/ledge/lock timers and contact latches: a fresh pose grabs/falls fresh.
            wallJumpLockTimer = 0f;
            wallReGrabTimer = 0f;
            ledgeReGrabTimer = 0f;
            wallCoyoteTimer = 0f;
            coyoteTimer = 0f;
            // A jump pressed before death/respawn must not fire on revive.
            bufferTimer = 0f;
            wallGrabHeld = false;
            wallContact = false;
            wallCollider = null;
            ledgeContact = false;
            ledgeCollider = null;
            wasLedgeHang = false;
            holdingWallJumpFacing = false;
            facingLocked = false;
            desiredFacing = Vector3.zero;

            if (rb != null)
            {
                rb.position = position;
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }

            if (visual != null)
            {
                if (facing.sqrMagnitude > 0.000001f)
                {
                    if (visual is AnimatorVisual animatorVisual) animatorVisual.SnapFacing(facing);
                    else visual.SetFacing(facing);
                }
                visual.SetMoveState(MoveState.Free);
                visual.SetSpeed(0f);
                visual.SetVerticalSpeed(0f);
                visual.SetGrounded(false);
                visual.SetRunning(false);
            }
        }

        /// <summary>Hit entry: cancels upward momentum and the jump buffer so a held jump cannot keep ascending.</summary>
        public void CancelVerticalMomentum()
        {
            bufferTimer = 0f;
            wallCoyoteTimer = 0f;
            if (grounded) return;
            verticalVel = 0f;
            if (rb != null)
            {
                Vector3 v = rb.linearVelocity;
                v.y = 0f;
                rb.linearVelocity = v;
            }
        }

        private void Awake()
        {
            // Body ownership first: every sibling component reads Body in its own
            // Update/FixedUpdate, but a same-GameObject Awake order is not guaranteed —
            // create the arbiter before anything can observe it.
            Body = new BodyOwnership();
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
            // A jump pressed during the dash must survive it as a buffer (dash holds
            // the body, so the buffered press cannot fire until the token releases).
            if (BodyOwner != BodyPriority.Dash) bufferTimer = Mathf.Max(0f, bufferTimer - dt);
            jumpLockoutTimer = Mathf.Max(0f, jumpLockoutTimer - dt);

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
                    // Snap exact — no overshoot. warpEnd already includes the step
                    // past the edge (folded in at mantle start), so landing here puts
                    // solid top under the character and the next frame reads grounded
                    // (Free/idle) instead of hanging. No post-warp step: the forward
                    // advance happened smoothly DURING the warp.
                    rb.position = warpEnd;
                    mantling = false;
                    verticalVel = 0f;
                    smoothVel = Vector3.zero;
                    lockHoriz = Vector3.zero;
                }
                else
                {
                    // Curved warp: a quadratic Bezier around the lip instead of a straight
                    // line. The control point sits at the lip corner pushed up + outward,
                    // so the body rises along the wall and ARCS OVER the corner instead of
                    // cutting through it. SmoothStep on t keeps the eased in/out feel.
                    float t = Mathf.SmoothStep(0f, 1f, warpT);
                    rb.position = QuadraticBezier(warpStart, warpControl, warpEnd, t);
                }
                ExposeMantleState();
                return;
            }

            grounded = CheckGrounded();
            // Landing recovery: on touchdown (airborne -> grounded) start a short
            // lockout so a still-buffered jump cannot re-launch on the FIRST grounded
            // frame. Without a grounded dwell the animator never settles (jump-spam
            // thrash: each state lives ~1 frame). The buffer itself is untouched —
            // once the lockout expires, normal buffered jump behavior resumes.
            if (grounded && !wasGrounded)
            {
                jumpLockoutTimer = ResolveJumpLockout();
                AudioDirector.Play(AudioCueId.Land, transform.position);
            }
            wasGrounded = grounded;
            // Touching the ground clears the re-grab ban (and the coyote source).
            if (grounded)
            {
                lastWallJumpedFrom = null;
                lastWallContacted = null;
                ledgeBanCollider = null; // landed: the drop-through ban is spent
                ledgeReGrabTimer = 0f;
                wasLedgeHang = false; // landed: a walk-off starts falling fresh, never latched
                ledgeInto = Vector3.zero; // landed: the ledge interaction is over, drop the captured into-edge dir
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
            else if (bufferTimer > 0f && (grounded || coyoteTimer > 0f) && jumpLockoutTimer <= 0f && !Body.IsHeld)
            {
                verticalVel = paramsAsset.jumpVelocity;
                bufferTimer = 0f;
                coyoteTimer = 0f;
                grounded = false;
                hanging = false;
                AudioDirector.Play(AudioCueId.JumpVoice, transform.position);
                AudioDirector.Play(AudioCueId.JumpWhoosh, transform.position);
            }
            else if (bufferTimer > 0f && !grounded && (hanging || wallCoyoteTimer > 0f) && !Body.IsHeld)
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
                    AudioDirector.Play(AudioCueId.JumpVoice, transform.position);
                    AudioDirector.Play(AudioCueId.JumpWhoosh, transform.position);
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

            // Movement state for the animator AND the action gate: most specific active
            // state wins. Computed ONCE here — after the release/jump/mantle branches have
            // settled `hanging` — so the gate below and the visual at the end of the step
            // read the SAME value (no per-frame divergence).
            if (holdingWallJumpFacing) State = MoveState.WallJump;
            else if (ledgeHang) State = MoveState.LedgeHang;
            else if (wallGrab) State = MoveState.WallGrab;
            else State = MoveState.Free;

            // Snap-to-grip: on ledge-grab start, settle the root to the authored
            // hang pose (same XZ, Y = lip top - gripDepth) so every hang reads
            // identical instead of the variable physics overlap position. Instant
            // (industry standard) — no visible slide. Skipped when a mantle or
            // release fired this frame (the warp/impulse owns the root there).
            if (ledgeHang && !wasLedgeHang && ledgeCollider != null)
            {
                // Capture the INTO-edge direction NOW, while the grab-frame geometry is
                // in reach. A stationary hang has no wall probe (CheckWall early-returns
                // with no input/velocity), so without this the hang/mantle facing falls
                // back to the stale visual.Facing and the character hangs/climbs sideways.
                CaptureLedgeInto();
                SnapToGrip();
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

            if (grounded && moving && !InputLocked)
            {
                footstepTimer -= dt;
                if (footstepTimer <= 0f)
                {
                    AudioDirector.Play(AudioCueId.Footstep, transform.position);
                    footstepTimer = Mathf.Max(0.1f, footstepInterval);
                }
            }
            else
            {
                footstepTimer = 0.15f;
            }

            Vector3 horizontal;
            if (wallJumpLockTimer > 0f)
            {
                // Control locked: keep the impulse, skip the wish.
                horizontal = lockHoriz;
                smoothVel = lockHoriz;
            }
            else if (State == MoveState.WallJump)
            {
                // Wall-jump flight: the gate denies *input* Move in WallJump (frozen
                // matrix), but that must not brake our OWN ballistic push-off. Preserve
                // the launch impulse (no mid-flight steering) so the arc completes.
                horizontal = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
                smoothVel = horizontal;
            }
            else if (!CanPerform(PlayerAction.Move))
            {
                // ACTION GATE: hang states deny Move — ignore the horizontal wish and
                // kill lateral drift, so the player cannot slide parallel to the lip.
                // Vertical (slide) and the Down-release (Drop, handled above) are the
                // motor's own and stay untouched.
                horizontal = Vector3.zero;
                smoothVel = Vector3.zero;
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

            if (visual != null)
            {
                if (faceDir.sqrMagnitude > 0.000001f) visual.SetFacing(faceDir);
                visual.SetGrounded(grounded);
                visual.SetMoveState(State);
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

        /// <summary>
        /// Landing recovery (seconds) for this touchdown: the DERIVED total =
        /// Landing clip effective duration + jumpLockoutExtra, computed by the
        /// presentation layer (which owns the Animator) and read here as a plain number —
        /// the motor owns no Animator. Falls back to
        /// <see cref="PlayerParams.jumpLockout"/> when the proxy visual is active or the
        /// Landing clip could not be resolved. Read lazily on touchdown (after every
        /// component's Awake), so the derivation is guaranteed to be ready.
        /// </summary>
        private float ResolveJumpLockout()
        {
            if (visual is AnimatorVisual animatorVisual && animatorVisual.LandingRecoverySeconds > 0f)
                return animatorVisual.LandingRecoverySeconds;
            return paramsAsset != null ? paramsAsset.jumpLockout : 0f;
        }

        /// <summary>
        /// Ground probe: 4 cardinal rays cast DOWN from the body's base footprint
        /// (±<c>groundRayOffset</c> in X and Z, at feet height) over <c>groundSnap</c>.
        /// ANY ray hitting a solid non-trigger collider that is not the player's own
        /// body => grounded. Replaces the old single center ray, which slipped over a
        /// gap when the player stood on an edge/tip (e.g. right after a mantle) and
        /// read as "falling". Fully data-driven (offset + distance).
        /// </summary>
        private bool CheckGrounded()
        {
            if (paramsAsset == null || body == null) return false;
            Vector3[] origins = groundRayOrigins;
            GetGroundRays(body, origins);
            for (int i = 0; i < origins.Length; i++)
                if (GroundRayHits(body, origins[i], out _)) return true;
            return false;
        }

        /// <summary>
        /// The 4 cardinal ground-probe origins: body base footprint at feet height,
        /// offset ±<c>groundRayOffset</c> around the center XZ — one per cardinal
        /// direction (+X, -X, +Z, -Z). All cast DOWN. Shared by
        /// <see cref="CheckGrounded"/> and the Scene-view gizmo so they never diverge.
        /// </summary>
        private void GetGroundRays(CapsuleCollider capsule, Vector3[] origins)
        {
            Vector3 center = capsule.bounds.center;
            float feetY = capsule.bounds.min.y + GroundRayStartEpsilon;
            float o = paramsAsset.groundRayOffset;
            origins[0] = new Vector3(center.x + o, feetY, center.z);
            origins[1] = new Vector3(center.x - o, feetY, center.z);
            origins[2] = new Vector3(center.x, feetY, center.z + o);
            origins[3] = new Vector3(center.x, feetY, center.z - o);
        }

        /// <summary>
        /// Casts one ground ray down <c>groundSnap</c> from <paramref name="origin"/>.
        /// True when it hits a solid (non-trigger) collider that is neither the given
        /// capsule nor the player's own rigidbody; <paramref name="endpoint"/> is the
        /// hit distance (or the full ray length on a miss) for the gizmo.
        /// </summary>
        private bool GroundRayHits(CapsuleCollider capsule, Vector3 origin, out Vector3 endpoint)
        {
            float maxDist = groundSnap;
            endpoint = origin + Vector3.down * maxDist;
            var hits = Physics.RaycastAll(origin, Vector3.down, maxDist,
                Physics.AllLayers, QueryTriggerInteraction.Ignore);
            float best = float.MaxValue;
            bool any = false;
            for (int i = 0; i < hits.Length; i++)
            {
                var c = hits[i].collider;
                if (c == null || c == capsule) continue;
                if (hits[i].rigidbody == rb) continue; // own Face / children
                if (hits[i].distance < best) { best = hits[i].distance; any = true; }
            }
            if (any) endpoint = origin + Vector3.down * best;
            return any;
        }

        /// <summary>
        /// Wall probe by VOLUME (consistent with the ledge hands band): an
        /// <see cref="Physics.OverlapBox"/> band of <c>wallBandSize</c> at
        /// <c>wallBandHeight</c> above the body center catches Grabbable sides
        /// across the whole body width, not just the exact center line — so a
        /// corner no longer fires on the wall face before the ledge can take the
        /// lip. Because the box yields WHICH collider but not the surface normal,
        /// a SHORT ray toward the wall recovers the normal (sticky grab + wall-jump
        /// direction). Accepts only near-vertical Grabbable faces from a different
        /// rigidbody (ignores the Player's own Face child and the last wall-jumped-from).
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
                // Facing fallback: with no input and no horizontal velocity — a flush
                // takeoff, or a collision-stopped arrival at a destination wall — still
                // probe into the wall the character faces. The directionality test
                // below (Dot(probe, normal) >= 0) keeps a back-to-wall from grabbing.
                if (probe.sqrMagnitude < 0.000001f)
                {
                    Vector3 f = visual != null ? visual.Facing : transform.forward;
                    f.y = 0f;
                    probe = f;
                }
                if (probe.sqrMagnitude < 0.000001f) return;
            }
            probe.y = 0f;
            probe.Normalize();

            GetWallBandVolume(body, out Vector3 bandCenter, out Vector3 half);
            // Collide (not Ignore): the Grabbable marker may be a TRIGGER collider
            // (Playground S2_Wall / Grabbable_S3B). The GetComponentInParent<Grabbable>
            // filter below keeps Ledge/other triggers out, so only Grabbable sides count.
            var cols = Physics.OverlapBox(bandCenter, half, Quaternion.identity,
                Physics.AllLayers, QueryTriggerInteraction.Collide);
            float best = float.MaxValue;
            for (int i = 0; i < cols.Length; i++)
            {
                var col = cols[i];
                if (col == null || col == body) continue;
                if (col.attachedRigidbody == rb) continue; // Face / own children
                // Re-grab ban: ignore the exact wall we just wall-jumped from until
                // grounded or a different wall/ledge is grabbed.
                if (col == lastWallJumpedFrom) continue;
                if (col.GetComponentInParent<Grabbable>() == null) continue; // only Grabbable sides
                // The box gives WHICH collider, not the surface normal: recover it
                // with a short ray into the wall, then keep the horizontal test.
                if (!TryGetWallNormal(bandCenter, probe, col, out Vector3 normal, out float dist)) continue;
                // Directionality preserved from the old ray: the collider only counts
                // while probing INTO it (probe points against the face normal). A box
                // has no intrinsic direction, so this explicit test keeps the
                // move-into-wall semantics the center ray had (band catches sideways
                // overlap, not backpedal-by-a-wall).
                if (Vector3.Dot(probe, normal) >= 0f) continue;
                if (Mathf.Abs(normal.y) >= paramsAsset.wallNormalThreshold) continue; // floor/ceiling
                if (dist >= best) continue;
                best = dist;
                wallContact = true;
                wallNormal = normal;
                wallCollider = col;
            }
        }

        /// <summary>
        /// Shared wall-band volume math (dev visualization + <see cref="CheckWall"/>):
        /// a body-width slice (<c>wallBandSize</c>) at <c>wallBandHeight</c> above the
        /// body center. Fully data-driven — mirrors the ledge hands band
        /// (<see cref="GetHandsBandVolume"/>).
        /// </summary>
        private void GetWallBandVolume(CapsuleCollider capsule, out Vector3 center, out Vector3 half)
        {
            center = capsule.bounds.center + Vector3.up * paramsAsset.wallBandHeight;
            half = paramsAsset.wallBandSize * 0.5f;
        }

        /// <summary>
        /// Recovers the surface NORMAL for a collider caught by the wall band. The
        /// box tells WHICH collider but not the face orientation, so cast a short ray
        /// toward the wall: first along the probe (move/sticky direction), then — as a
        /// robust fallback for corners, where the probe is tangential to the face the
        /// band overlaps — straight at the collider's closest point. <paramref name="distance"/>
        /// is the ray hit distance, used to pick the nearest Grabbable.
        /// </summary>
        private bool TryGetWallNormal(Vector3 origin, Vector3 probe, Collider col,
            out Vector3 normal, out float distance)
        {
            if (CastWallNormal(origin, probe, paramsAsset.wallCheckDistance, col, out normal, out distance))
                return true;
            Vector3 target = col.ClosestPoint(origin);
            Vector3 dir = target - origin;
            float dist = dir.magnitude;
            if (dist <= 0.0001f)
            {
                normal = Vector3.zero;
                distance = float.MaxValue;
                return false;
            }
            return CastWallNormal(origin, dir / dist, dist + 0.1f, col, out normal, out distance);
        }

        /// <summary>
        /// Short ray that returns the normal of a SPECIFIC collider's face, skipping the
        /// Player's own colliders. True only when it hits <paramref name="target"/>.
        /// </summary>
        private bool CastWallNormal(Vector3 origin, Vector3 dir, float maxDist, Collider target,
            out Vector3 normal, out float distance)
        {
            normal = Vector3.zero;
            distance = float.MaxValue;
            // Collide (not Ignore): the target may be a TRIGGER Grabbable. The
            // `hit.collider != target` guard below still pins the normal to the
            // SPECIFIC collider handed in, so extra trigger hits cannot leak through.
            var hits = Physics.RaycastAll(origin, dir, maxDist,
                Physics.AllLayers, QueryTriggerInteraction.Collide);
            for (int i = 0; i < hits.Length; i++)
            {
                var hit = hits[i];
                if (hit.collider == null || hit.collider == body) continue;
                if (hit.rigidbody == rb) continue; // own Face / children
                if (hit.collider != target) continue;
                normal = hit.normal;
                distance = hit.distance;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Dev visualization probe: true when the wall band (as queried by
        /// <see cref="CheckWall"/>) overlaps a Grabbable collider. Direction- and
        /// probe-independent (that is the forgiving part) — used only by the gizmo.
        /// </summary>
        private bool WallBandCatchesGrabbable(CapsuleCollider capsule)
        {
            GetWallBandVolume(capsule, out Vector3 center, out Vector3 half);
            var cols = Physics.OverlapBox(center, half, Quaternion.identity,
                Physics.AllLayers, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < cols.Length; i++)
            {
                var col = cols[i];
                if (col == null || col == capsule) continue;
                if (col.GetComponentInParent<Grabbable>() == null) continue;
                return true;
            }
            return false;
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
            // Capture the climb direction NOW: the warp frames skip the wall probe,
            // so HangIntoDir would be stale by landing time. Captured BEFORE the
            // destination so the step-inclusive end can use it.
            mantleDir = HangIntoDir();
            mantleDir.y = 0f;
            mantleDir = mantleDir.sqrMagnitude > 0.000001f ? mantleDir.normalized : Vector3.zero;
            // Fold the step INTO the warp: the destination is the FINAL pose past the
            // ledge (StepPastLedge probes HasTopBeneath at that final pose), so the
            // whole climb interpolates start -> final target as ONE smooth motion
            // with NO instantaneous snap at the end.
            warpEnd = StepPastLedge(ComputeWarpEnd());
            // Shape the warp path: a Bezier control point over the lip so the body arcs
            // AROUND the corner (straight lerp clipped the edge). Same start, same exact
            // end — only the mid-path bulges.
            warpControl = ComputeMantleControl();
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
            // Landing height comes from the EDGE, not the wall: the Ledge marker's
            // OWN top (its bounds max.y) IS the lip = the walkable surface, so the
            // stand pose is lip + half height. The Grabbable wall is only a fallback
            // (no Ledge trigger present). Both are marker-owned references, so moving
            // a marker moves the landing with it.
            float topY;
            if (ledgeCollider != null)
                topY = ledgeCollider.bounds.max.y;
            else if (wallCollider != null)
                topY = wallCollider.bounds.max.y;
            else
                return new Vector3(
                    warpStart.x + into.x * body.radius,
                    warpStart.y + body.height,
                    warpStart.z + into.z * body.radius);

            // Same-line climb: land at the player's own XZ (warpStart), only the
            // Y comes from the top. Using the slab center teleported the character
            // sideways on off-center hangs.
            return new Vector3(
                warpStart.x,
                topY + body.height * 0.5f,
                warpStart.z);
        }

        /// <summary>
        /// Bezier control point for the mantle warp: the lip corner (wall/ledge top on the
        /// player's own vertical line) pushed UP and OUTWARD (away from the wall) by the body
        /// radius times <c>mantleArcRadius</c>. This bulges the path over the lip so the
        /// capsule ARCS AROUND the corner instead of clipping through it. Lip top comes from
        /// the ledge trigger (present on every ledge hang), then the grabbable wall bounds,
        /// then a body-sized fallback. XZ stays on the player's own line (same-line climb).
        /// </summary>
        private Vector3 ComputeMantleControl()
        {
            float lipY;
            if (ledgeCollider != null)
                lipY = ledgeCollider.bounds.max.y;
            else if (wallCollider != null)
                lipY = wallCollider.bounds.max.y;
            else
                lipY = warpStart.y + body.height * 0.5f;

            Vector3 lip = new Vector3(warpStart.x, lipY, warpStart.z);
            // Outward = away from the wall (opposite the into/over direction), so the
            // curve swings clear of the wall face on its way up to the lip.
            Vector3 outward = mantleDir.sqrMagnitude > 0.000001f ? -mantleDir : Vector3.zero;
            float bulge = body.radius * Mathf.Max(0f, paramsAsset.mantleArcRadius);
            return lip + Vector3.up * bulge + outward * bulge;
        }

        /// <summary>Point on the quadratic Bezier (p0, p1, p2) at parameter t in [0, 1].</summary>
        private static Vector3 QuadraticBezier(Vector3 p0, Vector3 p1, Vector3 p2, float t)
        {
            float u = 1f - t;
            return u * u * p0 + 2f * u * t * p1 + t * t * p2;
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
            State = MoveState.Mantle; // gate: the warp is fully locked
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
            // Query the UNION of the body AABB and the HANDS band (the same volume
            // OverlapsHandsBand uses). The Ledge marker is a thin sliver buried in
            // the wall, so on arrival the exact body AABB can drift just outside it
            // while the hands band still reaches its exposed face. Keeping the body
            // AABB in the box preserves the established-hang latch (the grip snap
            // settles the root below the band, but the body still overlaps).
            GetHandsBandVolume(body, out Vector3 hands, out Vector3 bandHalf);
            Vector3 lo = Vector3.Min(body.bounds.min, hands - bandHalf);
            Vector3 hi = Vector3.Max(body.bounds.max, hands + bandHalf);
            Vector3 center = (lo + hi) * 0.5f;
            Vector3 half = (hi - lo) * 0.5f;
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
        /// Shared hands-band volume math (dev visualization + <see cref="OverlapsHandsBand"/>):
        /// a small explicit chest-sized box (<c>hangBandSize</c>) at
        /// <c>hangGripHeight</c> above the body center. Fully data-driven —
        /// ignores the body extents and the ledge trigger size.
        /// Behavior-identical flow, smaller volume.
        /// </summary>
        private void GetHandsBandVolume(CapsuleCollider capsule, out Vector3 hands, out Vector3 half)
        {
            hands = capsule.bounds.center + Vector3.up * paramsAsset.hangGripHeight;
            half = paramsAsset.hangBandSize * 0.5f;
        }

        /// <summary>
        /// True when the ledge trigger overlaps the hands band: a small explicit
        /// box at <c>hangGripHeight</c> above the body center. Feet/head-level
        /// passes miss the band and do not hang.
        /// </summary>
        private bool OverlapsHandsBand(Collider ledgeCol)
        {
            GetHandsBandVolume(body, out Vector3 hands, out Vector3 half);
            var cols = Physics.OverlapBox(hands, half, Quaternion.identity,
                Physics.AllLayers, QueryTriggerInteraction.Collide);
            for (int i = 0; i < cols.Length; i++)
                if (cols[i] == ledgeCol) return true;
            return false;
        }

        /// <summary>
        /// Dev-only visualization of the invisible hand/wall checks (Scene view only,
        /// zero build cost). Draws EXACTLY the two OverlapBox volumes used by the
        /// grab logic, same style: cyan/faint normally, green while the band overlaps
        /// its target (hang/grab armed). Visualization only — no behavior.
        /// - LEDGE hands band: <see cref="OverlapsHandsBand"/> (green on a Ledge trigger).
        /// - WALL grab band: <see cref="CheckWall"/> (green on a Grabbable).
        /// </summary>
        private void OnDrawGizmos()
        {
            CapsuleCollider capsule = body != null ? body : GetComponent<CapsuleCollider>();
            if (capsule == null || paramsAsset == null) return;
            GetHandsBandVolume(capsule, out Vector3 hands, out Vector3 half);
            Gizmos.color = ledgeContact ? Color.green : Color.cyan;
            Gizmos.DrawWireCube(hands, half * 2f);
            Color fill = Gizmos.color;
            fill.a = 0.15f;
            Gizmos.color = fill;
            Gizmos.DrawCube(hands, half * 2f);

            // Wall-grab band: exactly the OverlapBox queried by CheckWall (same
            // offset, same extents). Green while the band catches a Grabbable
            // (grab armed), faint orange otherwise.
            GetWallBandVolume(capsule, out Vector3 wallBand, out Vector3 wallHalf);
            Gizmos.color = WallBandCatchesGrabbable(capsule)
                ? Color.green
                : new Color(1f, 0.6f, 0.1f, 0.4f);
            Gizmos.DrawWireCube(wallBand, wallHalf * 2f);
            Color wallFill = Gizmos.color;
            wallFill.a = 0.15f;
            Gizmos.color = wallFill;
            Gizmos.DrawCube(wallBand, wallHalf * 2f);

            // Ground check: the 4 cardinal rays queried by CheckGrounded (same
            // origins, same length). GREEN = that ray HITS (feeds grounded),
            // faint red = miss — shows exactly why grounding triggers or not.
            Vector3[] origins = groundRayOrigins;
            GetGroundRays(capsule, origins);
            for (int i = 0; i < origins.Length; i++)
            {
                bool hit = GroundRayHits(capsule, origins[i], out Vector3 end);
                Gizmos.color = hit ? Color.green : new Color(1f, 0.3f, 0.3f, 0.4f);
                Gizmos.DrawLine(origins[i], end);
                Gizmos.DrawWireSphere(origins[i], 0.04f);
            }
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

        /// <summary>
        /// Captures the ledge "into-edge" direction (XZ unit) at the grab edge: the
        /// XZ direction from the body toward the ledge collider, refined by a short
        /// ray into the solid surface behind the lip (the ray skips the trigger ledge
        /// itself, so `QueryTriggerInteraction.Ignore` is correct). Cached in
        /// <see cref="ledgeInto"/>; cleared when the hang ends (grounded). Gives
        /// <see cref="HangIntoDir"/> a geometry-derived target during a stationary
        /// hang instead of the stale <c>visual.Facing</c>.
        /// </summary>
        private void CaptureLedgeInto()
        {
            ledgeInto = Vector3.zero;
            if (body == null || ledgeCollider == null) return;
            Vector3 origin = body.bounds.center;
            // Aim at the ledge's nearest point: a trigger marker is not hit by a ray,
            // so this direction ALSO points into the edge for a wall-less platform lip.
            Vector3 dir = ledgeCollider.ClosestPoint(origin) - origin;
            dir.y = 0f;
            if (dir.sqrMagnitude < 0.000001f) return;
            dir.Normalize();
            // Prefer the solid face normal behind the lip (into = -normal); the ray
            // reliably finds the wall where the ClosestPoint aim is only approximate.
            var hits = Physics.RaycastAll(origin, dir, paramsAsset.wallCheckDistance,
                Physics.AllLayers, QueryTriggerInteraction.Ignore);
            float best = float.MaxValue;
            Vector3 normal = Vector3.zero;
            for (int i = 0; i < hits.Length; i++)
            {
                var hit = hits[i];
                if (hit.collider == null || hit.collider == body) continue;
                if (hit.rigidbody == rb) continue; // own Face / children
                if (hit.distance >= best) continue;
                best = hit.distance;
                normal = hit.normal;
            }
            normal.y = 0f;
            ledgeInto = normal.sqrMagnitude > 0.000001f ? -normal.normalized : dir;
        }

        /// <summary>Direction from the hang point INTO the surface/edge (over the top).</summary>
        private Vector3 HangIntoDir()
        {
            // On a Grabbable wall the inward direction is opposite the wall normal.
            if (wallNormal.sqrMagnitude > 0.000001f) return -wallNormal;
            // Stationary ledge hang (no wall probe): use the direction captured at the
            // grab edge, so the facing/climb goes INTO the edge instead of sideways.
            if (ledgeInto.sqrMagnitude > 0.000001f) return ledgeInto;
            // Safety net (wall-jump -> ledge chain): the last known wall normal.
            if (lastWallNormal.sqrMagnitude > 0.000001f) return -lastWallNormal;
            // Ledge without any geometry: climb toward the player's facing (they jumped in).
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
