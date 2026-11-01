using System.Collections;
using UnityEngine;

namespace MK.Rebuild
{
    /// <summary>
    /// The <c>Grab</c> button, STYLE-SPLIT: Tarkatana grabs and throws a body; Mantis
    /// disarms (a long single-target lock, no throw, no movement, no damage). One input,
    /// two verbs — the stance decides which, so no extra button is introduced.
    ///
    /// WHY this component exists: <see cref="PlayerStance"/> states that Tarkatana cannot
    /// block — its defensive answer is the grab. Mantis keeps block/parry and gains the
    /// disarm, which is NOT a stagger clone: it lasts much longer and is the window for a
    /// precise hit (low risk, high opportunity). The throw is not a new damage system: the
    /// held victim is pushed with a normal Rigidbody velocity and then handed to
    /// <see cref="ThrownBody"/>, which reuses <see cref="HitReceiver"/> as the only hurt seam.
    ///
    /// TARGETING reuses the shared <see cref="AimAssist"/> cone (nearest alive
    /// <see cref="HitReceiver"/> inside the frontal cone within <see cref="grabRange"/>).
    /// <see cref="AimAssist.TryFindAimTarget"/> returns a direction + distance but NOT the
    /// receiver, so the direction is resolved back to a receiver with a second, small
    /// OverlapSphere around the expected point (see <see cref="ResolveVictim"/>). No cone
    /// math is duplicated here.
    ///
    /// GATING CONTRACT: without a <see cref="PlayerStance"/> component (the v1 player) this
    /// component does NOTHING — it does not even read the Grab input. Both verbs therefore
    /// only exist on the stance-aware player.
    ///
    /// ATTACH POINT: with a <see cref="grabAnchor"/> the victim is parented under a bone and
    /// its Rigidbody frozen (kinematic), so the throw animation carries the body for free.
    /// Without an anchor the victim is held in front of the player by FixedUpdate. Release
    /// always detaches and restores the body, and <see cref="CancelGrab"/> does the same so
    /// a disabled component or a scene stop never leaves an enemy welded to the bone.
    ///
    /// THROW AIM: the throw can solve a ballistic arc at the nearest throwable inside a cone
    /// (the shared <see cref="AimAssist.TryFindTarget"/> selection, with
    /// <see cref="AimAssist.ResolveThrowable"/> so a puzzle prop and an enemy are equally
    /// valid). The arc is evaluated with the PROJECT's real gravity (Physics.gravity.y = -100),
    /// and any failure (no target, impossible angle) falls back to the authored throw.
    ///
    /// PLANTING: while holding, the grab acquires its own <see cref="BodyPriority.Attack"/>
    /// token on <see cref="PlayerMotor.Body"/> so the motor preserves the zeroed horizontal
    /// velocity instead of steering with the wished direction. FixedUpdate also zeroes the
    /// horizontal velocity explicitly. The token is released by <see cref="ReleaseThrow"/>
    /// and by OnDisable, so a disabled component can never leave the player locked.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody))]
    public sealed class PlayerGrab : MonoBehaviour
    {
        [Header("Grab")]
        [Tooltip("Grab reach in meters. Only targets inside this radius AND inside the aim-assist cone qualify.")]
        [SerializeField] private float grabRange = 1.8f;

        [Tooltip("Seconds the victim is held before being thrown.")]
        [SerializeField] private float grabDuration = 0.45f;

        [Tooltip("Seconds the grab stays blocked after a MISS (no target inside the cone).")]
        [SerializeField] private float missRecovery = 0.5f;

        [Tooltip("Seconds before the grab can be used again (armed at grab start, so a throw also pays it).")]
        [SerializeField] private float cooldown = 1.2f;

        [Header("Throw")]
        [Tooltip("Horizontal speed in m/s applied to the thrown victim.")]
        [SerializeField] private float throwSpeed = 12f;

        [Tooltip("Vertical speed in m/s added to the throw, so the body pops off the ground.")]
        [SerializeField] private float throwLift = 3f;

        [Tooltip("Damage the grab itself deals to the victim on release (the throw also becomes a projectile).")]
        [SerializeField] private int throwDamage = 8;

        [Tooltip("Impact radius in meters of the flying body (handed to ThrownBody).")]
        [SerializeField] private float throwRadius = 0.9f;

        [Tooltip("Seconds the thrown body keeps damaging everything it touches (handed to ThrownBody).")]
        [SerializeField] private float flightDuration = 0.5f;

        [Header("Aim assist")]
        [Tooltip("Cone used to pick the victim. Null = the grab never finds a target. Reuses the shared AimAssistDefinition type.")]
        [SerializeField] private AimAssistDefinition grabAimAssist;

        [Header("Throw aim (ballistic)")]
        [Tooltip("Cone used to aim the throw. Reuses the grab aim definition; only the range is overridden.")]
        [SerializeField] private AimAssistDefinition throwAimAssist;

        [Tooltip("Search range in meters for the throw target. Overrides the definition's max range.")]
        [SerializeField] private float throwAimRange = 12f;

        [Tooltip("Launch angle above the horizontal used to solve the arc.")]
        [SerializeField, Range(15f, 75f)] private float throwAngleDeg = 45f;

        [Tooltip("Hard cap on the solved launch speed, so a far target does not create a missile.")]
        [SerializeField] private float maxThrowSpeed = 25f;

        [Tooltip("When on, the throw solves a ballistic arc at the nearest throwable inside the throw cone. Off = the authored forward throw.")]
        [SerializeField] private bool aimThrow = true;

        [Header("Attach point")]
        [Tooltip("Anchor the victim is parented to while held. Put an empty under the hand/chest bone so the grabbed enemy follows the throw animation automatically. Null = hold in front of the player (current behaviour).")]
        [SerializeField] private Transform grabAnchor;

        [Tooltip("Extra local offset applied after parenting, for fine alignment against the anchor.")]
        [SerializeField] private Vector3 grabAnchorLocalOffset = Vector3.zero;

        [Tooltip("Which axis of the player/anchor the throw follows. true = the anchor's forward (animation-driven); false = the player's forward (predictable).")]
        [SerializeField] private bool throwAlongAnchor = false;

        [Header("Disarm (Mantis)")]
        [Tooltip("Seconds the disarmed enemy stays open. Deliberately LONGER than a poise break: this is the precise-hit window, not a stagger.")]
        [SerializeField] private float disarmStaggerDuration = 1.2f;

        [Tooltip("Seconds before the disarm can be used again. Shorter than the Tarkatana grab cooldown (low risk, high opportunity).")]
        [SerializeField] private float disarmCooldown = 0.8f;

        [Tooltip("Seconds the disarm stays blocked after a MISS (no enemy inside the cone).")]
        [SerializeField] private float disarmRecovery = 0.25f;

        private const float MinHoldDistance = 0.6f;
        private const float VictimMatchRadius = 0.75f;
        private const float TemporaryBodySafetyTimeout = 4f;
        private const float TemporaryBodyRestSpeedSqr = 0.5f;
        private const float TemporaryBodyGroundProbe = 2.5f;

        private Rigidbody rb;
        private CapsuleCollider playerCapsule;
        private PlayerMotor motor;
        private ICharacterVisual visual;
        private PlayerStance stance;
        private PlayerInput input;

        private bool isGrabbing;
        private float grabTimer;
        private float cooldownTimer;
        private float recoveryTimer;

        private HitReceiver victim;
        private Rigidbody victimBody;
        private Vector3 throwDir = Vector3.forward;
        private float holdDistance = MinHoldDistance;
        private bool victimParented;
        private Rigidbody temporaryBody; // Rigidbody the grab added to a body-less victim
        private BodyOwnership.Token holdToken;

        /// <summary>True while a victim is held (read-only seam for debug/UI).</summary>
        public bool IsGrabbing => isGrabbing;

        /// <summary>Seconds left before the grab is available again (0 = ready).</summary>
        public float CooldownRemaining => Mathf.Max(cooldownTimer, recoveryTimer);

        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
            playerCapsule = GetComponent<CapsuleCollider>();
            motor = GetComponent<PlayerMotor>();
            visual = GetComponent<ICharacterVisual>();
            stance = GetComponent<PlayerStance>();
        }

        private void OnEnable()
        {
            if (input == null) input = new PlayerInput();
            input.Player.Enable();
        }

        private void OnDisable()
        {
            if (input != null) input.Player.Disable();

            // A disabled grab must not keep the victim reference, the timer state or the
            // body token alive: release everything so control returns immediately.
            CancelGrab();
        }

        private void OnDestroy()
        {
            CancelGrab();
            if (input != null) input.Dispose();
        }

        private void Update()
        {
            // PAUSE GATE: timers are scaled, but the game being paused must not let the
            // grab resolve or start. Same seam PlayerDash uses.
            if (ControlLock.IsPaused) return;

            // GATING CONTRACT: no PlayerStance (v1 player) = grab disabled, no input read.
            if (stance == null) return;
            if (input == null) return;

            if (cooldownTimer > 0f) cooldownTimer = Mathf.Max(0f, cooldownTimer - Time.deltaTime);
            if (recoveryTimer > 0f) recoveryTimer = Mathf.Max(0f, recoveryTimer - Time.deltaTime);

            if (isGrabbing)
            {
                // The victim may die (or be destroyed) while held: drop the hold without a
                // throw instead of writing to a dead body.
                if (victim == null || !victim.IsAlive)
                {
                    CancelGrab();
                    return;
                }

                grabTimer -= Time.deltaTime;
                if (grabTimer <= 0f) ReleaseThrow();
                return;
            }

            if (cooldownTimer > 0f || recoveryTimer > 0f) return;

            if (!input.Player.Grab.WasPressedThisFrame()) return;

            // Grab is a STYLE-DEPENDENT verb on the SAME button: Tarkatana grabs and throws,
            // Mantis disarms (long single-target lock, no throw, no damage). No extra input.
            if (stance.IsTarkatana) TryGrab();
            else TryDisarm();
        }

        private void FixedUpdate()
        {
            // Same gate as Update: a v1 player (no stance) must never be touched, and a
            // stale flag after a domain reload must not plant the body.
            if (stance == null || !isGrabbing) return;
            if (ControlLock.IsPaused) return;
            if (victim == null || !victim.IsAlive) return;

            // Hold aim is LIVE: the player is planted by the token, so the stick is the only
            // intent. Recomputed here (right where HoldVictim consumes it) so the victim is
            // always held and thrown on the side the stick points at.
            UpdateHoldAim();

            PlantPlayer();

            // With an anchor the parent hierarchy moves the victim with the animation, so
            // only the body-less "hold in front" path needs a per-frame reposition.
            if (!victimParented) HoldVictim();
        }

        /// <summary>
        /// Attempts the grab: finds the nearest alive target inside the aim-assist cone and
        /// either holds it or pays the miss recovery. Called only from a Tarkatana Update.
        /// </summary>
        private void TryGrab()
        {
            if (rb == null) return;

            Vector3 facing = ForwardNow();
            bool found = AimAssist.TryFindAimTarget(
                transform.position,
                rb,
                facing,
                grabAimAssist,
                Mathf.Max(0.01f, grabRange),
                out Vector3 aimDir,
                out float distance);

            HitReceiver target = found ? ResolveVictim(aimDir, distance) : null;
            if (target == null)
            {
                recoveryTimer = Mathf.Max(0f, missRecovery);
                cooldownTimer = Mathf.Max(0f, cooldown);
                Debug.Log($"[{nameof(PlayerGrab)}] Miss — no target inside the grab cone.", this);
                return;
            }

            victim = target;
            victimBody = ResolveBody(target);
            throwDir = aimDir;
            holdDistance = Mathf.Clamp(distance, MinHoldDistance, Mathf.Max(MinHoldDistance, grabRange));

            isGrabbing = true;
            grabTimer = Mathf.Max(0f, grabDuration);
            cooldownTimer = Mathf.Max(0f, cooldown);

            // Anchor mode: parent to the hand/chest bone and freeze the body so the
            // animation carries it. No anchor: the victim is held in front (FixedUpdate).
            victimParented = AttachVictim(target);

            // Face the victim so the throw goes where the grab was aimed (SetFacing is
            // incremental, so the visual converges while the hold plays).
            if (visual != null) visual.SetFacing(throwDir);
            else transform.rotation = Quaternion.LookRotation(throwDir, Vector3.up);

            AcquireHoldToken();
            PlantPlayer();

            // PLACEHOLDER audio: no grab cue exists yet, Dash is the closest whoosh.
            AudioDirector.Play(AudioCueId.Dash, transform.position);

            Debug.Log(
                $"[{nameof(PlayerGrab)}] GRAB ({distance:0.##} m, hold {grabTimer:0.##} s)",
                this);
        }

        /// <summary>
        /// Mantis's Grab verb: leaves ONE enemy open for a long single-target window WITHOUT
        /// throwing, moving or damaging it. Reuses the grab cone/range so the input feels the
        /// same across stances, but the reward is the precise-hit opening, not a projectile.
        /// </summary>
        private void TryDisarm()
        {
            if (rb == null) return;

            Vector3 facing = ForwardNow();
            bool found = AimAssist.TryFindAimTarget(
                transform.position,
                rb,
                facing,
                grabAimAssist,
                Mathf.Max(0.01f, grabRange),
                out Vector3 aimDir,
                out float distance);

            HitReceiver target = found ? ResolveVictim(aimDir, distance) : null;
            EnemyController enemy = target != null ? target.GetComponentInParent<EnemyController>() : null;
            if (enemy == null)
            {
                recoveryTimer = Mathf.Max(0f, disarmRecovery);
                cooldownTimer = Mathf.Max(0f, disarmCooldown);
                Debug.Log($"[{nameof(PlayerGrab)}] Disarm miss — no enemy inside the cone.", this);
                return;
            }

            // The enemy owns its state machine: the disarm only asks for a LONGER stagger
            // window than a poise break. No parenting, no throw, no damage on this path.
            enemy.Stagger(disarmStaggerDuration);
            cooldownTimer = Mathf.Max(0f, disarmCooldown);

            if (visual != null) visual.SetFacing(aimDir);
            else transform.rotation = Quaternion.LookRotation(aimDir, Vector3.up);

            Debug.Log(
                $"[{nameof(PlayerGrab)}] DISARM ({distance:0.##} m, open {disarmStaggerDuration:0.##} s)",
                this);
        }

        /// <summary>
        /// Ends the hold and turns the victim into a projectile. With <see cref="aimThrow"/>
        /// on, the launch velocity is SOLVED as a ballistic arc at the nearest throwable
        /// inside the throw cone (enemies and puzzle props share the selection); otherwise
        /// the authored forward throw is used. <see cref="ThrownBody"/> owns the impact.
        /// </summary>
        private void ReleaseThrow()
        {
            HitReceiver target = victim;
            Rigidbody body = victimBody;
            bool wasParented = victimParented;
            Vector3 dir = ThrowForward();

            isGrabbing = false;
            grabTimer = 0f;
            victim = null;
            victimBody = null;
            victimParented = false;
            ReleaseHoldToken();

            // Detach and re-enable physics BEFORE the early return: a victim that died while
            // held must still be released from the bone and must not stay kinematic.
            if (wasParented && target != null)
            {
                target.transform.SetParent(null, worldPositionStays: true);
            }
            if (body != null) body.isKinematic = false;

            if (target == null || !target.IsAlive)
            {
                Debug.Log($"[{nameof(PlayerGrab)}] Hold ended — victim was already down.", this);
                return;
            }

            // The throw is regular physics, not a new movement system: the victim's Rigidbody
            // carries the body while ThrownBody only owns the impact query. Debug dummies are a
            // primitive + HitReceiver with NO body, so a temporary Rigidbody is added for the
            // flight and removed afterwards (the dummy is left exactly as it was authored).
            if (body == null) body = AddTemporaryBody(target);

            // AIM: solve a ballistic arc at the nearest throwable inside the throw cone
            // (enemies AND puzzle props share the same selection). No target or an
            // impossible arc keeps the authored forward throw. The velocity is CACHED here:
            // TakeHit below may kill (and destroy) the victim, so it must survive that call.
            Vector3 velocity = dir * throwSpeed + Vector3.up * throwLift;
            if (aimThrow) TrySolveThrowArc(target.transform.position, body, dir, out velocity);

            if (!target.TryGetComponent(out ThrownBody thrown))
            {
                thrown = target.gameObject.AddComponent<ThrownBody>();
            }

            thrown.Initialize(gameObject, throwDamage, throwRadius, flightDuration);

            // Base grab damage (the victim also takes the symmetric impact once, in ThrownBody).
            target.TakeHit(throwDamage, dir);

            // LAST: TakeHit runs the hit feedback, and enemies with rigidbodyKnockback
            // OVERWRITE the horizontal velocity there (only Y survives). Writing the throw
            // velocity before TakeHit made the body rise and fall in place — so it is applied
            // after the damage/feedback, and only if the victim is still alive afterwards.
            if (target != null && target.IsAlive && body != null)
            {
                // PLACE THE BODY ON THE RELEASE SIDE: throwing backwards used to start the
                // body on the old side, fly it THROUGH the player and read as a short throw.
                float playerRadius = playerCapsule != null ? playerCapsule.radius : 0.5f;
                float releaseDistance = Mathf.Max(holdDistance, playerRadius + 0.6f);
                Vector3 releasePosition = transform.position + dir * releaseDistance;
                releasePosition.y = target.transform.position.y;

                if (!body.isKinematic)
                {
                    body.MovePosition(releasePosition);
                }
                else
                {
                    target.transform.position = releasePosition;
                }

                // EXACT facing for the throw: the incremental SetFacing would leave the
                // character mid-turn when the body leaves the hand.
                if (visual is AnimatorVisual animatorVisual) animatorVisual.SnapFacing(dir);
                else if (visual != null) visual.SetFacing(dir);

                body.isKinematic = false;
                body.linearVelocity = velocity;

                // DIAGNOSTIC (temporary): what was written vs what the physics step keeps.
                // Remove once the throw is verified.
                Debug.Log(
                    $"[{nameof(PlayerGrab)}] THROW VELOCITY {velocity} | dir {dir} | " +
                    $"kinematic {body.isKinematic} | body {body.name}",
                    this);
                StartCoroutine(LogVelocityAfterStep(body));
            }

            Debug.Log(
                $"[{nameof(PlayerGrab)}] THROW ({throwDamage} dmg, radius {throwRadius:0.##} m, " +
                $"{flightDuration:0.##} s)",
                this);
        }

        /// <summary>
        /// Plants the player: zero horizontal velocity, keep gravity/vertical motion. The
        /// motor's InputLocked branch preserves this while the hold token is held.
        /// </summary>
        private void PlantPlayer()
        {
            if (rb == null) return;

            Vector3 velocity = rb.linearVelocity;
            velocity.x = 0f;
            velocity.z = 0f;
            rb.linearVelocity = velocity;
        }

        /// <summary>
        /// Keeps the held victim in front of the player. A Rigidbody victim is moved with
        /// MovePosition and has its velocity cleared (it is suspended, not simulated); a
        /// body-less victim is positioned directly. Its own height is preserved so the hold
        /// never drags a body into the floor.
        /// </summary>
        private void HoldVictim()
        {
            Vector3 target = transform.position + throwDir * holdDistance;
            target.y = victim.transform.position.y;

            if (victimBody != null && !victimBody.isKinematic)
            {
                victimBody.linearVelocity = Vector3.zero;
                victimBody.MovePosition(target);
                return;
            }

            if (victimBody != null)
            {
                victimBody.MovePosition(target);
                return;
            }

            victim.transform.position = target;
        }

        /// <summary>
        /// Resolves the receiver that <see cref="AimAssist"/> selected. The selection only
        /// returns a direction and a distance, so this looks for the closest alive receiver
        /// around the expected point (<c>position + direction * distance</c>) — the point
        /// AimAssist measured from. Receivers outside a small tolerance are ignored, which
        /// keeps the choice consistent with the cone result.
        /// </summary>
        private HitReceiver ResolveVictim(Vector3 aimDir, float distance)
        {
            Vector3 expected = transform.position + aimDir * distance;
            Collider[] colliders = Physics.OverlapSphere(
                expected, VictimMatchRadius, Physics.AllLayers, QueryTriggerInteraction.Ignore);

            HitReceiver best = null;
            float bestSqr = float.MaxValue;
            for (int i = 0; i < colliders.Length; i++)
            {
                Collider collider = colliders[i];
                if (collider == null) continue;

                HitReceiver receiver = collider.GetComponentInParent<HitReceiver>();
                if (receiver == null || !receiver.IsAlive) continue;

                Vector3 to = receiver.transform.position - transform.position;
                to.y = 0f;
                if (to.sqrMagnitude < 0.0001f) continue;
                if (Vector3.Dot(aimDir, to.normalized) <= 0f) continue; // behind the player

                Vector3 delta = receiver.transform.position - expected;
                delta.y = 0f;
                float sqr = delta.sqrMagnitude;
                if (sqr >= bestSqr) continue;

                bestSqr = sqr;
                best = receiver;
            }

            return best;
        }

        /// <summary>
        /// Resolves the victim's body: the same GameObject first, then its parents (the
        /// receiver may live on a child hurtbox while the Rigidbody sits on the root).
        /// </summary>
        private Rigidbody ResolveBody(HitReceiver receiver)
        {
            Rigidbody body = receiver.GetComponent<Rigidbody>();
            if (body == null) body = receiver.GetComponentInParent<Rigidbody>();
            return body;
        }

        /// <summary>
        /// Takes the body token that makes the motor preserve the planted velocity. If a
        /// higher-priority lock already owns the body (hit recovery, death) the grab still
        /// runs, but FixedUpdate's explicit zeroing is the only planting force.
        /// </summary>
        private void AcquireHoldToken()
        {
            if (holdToken != null) return;
            if (motor == null || motor.Body == null) return;
            if (!motor.Body.CanAcquire(BodyPriority.Attack)) return;

            holdToken = motor.Body.Acquire(BodyPriority.Attack);
        }

        /// <summary>
        /// Parents the victim to <see cref="grabAnchor"/> so the hold animation drives it.
        /// Returns true when the victim is anchored (FixedUpdate then skips holding it in
        /// front). The body is frozen (kinematic, velocity zero) so physics cannot drag it
        /// out of the hand while it is a child of the bone.
        /// </summary>
        private bool AttachVictim(HitReceiver target)
        {
            if (grabAnchor == null) return false;

            target.transform.SetParent(grabAnchor, worldPositionStays: false);
            target.transform.localPosition = grabAnchorLocalOffset;
            target.transform.localRotation = Quaternion.identity;

            if (victimBody != null)
            {
                victimBody.linearVelocity = Vector3.zero;
                victimBody.isKinematic = true;
            }

            return true;
        }

        /// <summary>
        /// Recomputes the hold/throw direction every physics step from the LIVE stick. The
        /// player is planted while holding, so the visual facing never changes; keeping the
        /// previous direction on neutral input is deliberate — falling back to the facing here
        /// would snap the victim (and the throw) back to where the grab started.
        /// </summary>
        private void UpdateHoldAim()
        {
            Vector3 dir;
            if (throwAlongAnchor && grabAnchor != null)
            {
                dir = grabAnchor.forward;
            }
            else if (motor != null && motor.WishDirection.sqrMagnitude > 0.0001f)
            {
                dir = motor.WishDirection;
            }
            else
            {
                dir = throwDir; // no stick: keep the last aim (never reset mid-hold)
            }

            dir.y = 0f;
            if (dir.sqrMagnitude < 0.0001f) dir = throwDir;
            dir.y = 0f;
            if (dir.sqrMagnitude > 0.0001f) throwDir = dir.normalized;

            // Incremental on purpose: over the ~0.45 s hold the character turns toward the
            // stick instead of snapping. The exact snap happens on release.
            if (visual != null) visual.SetFacing(throwDir);
        }

        /// <summary>
        /// Current throw direction. During the hold the player is PLANTED by the body token,
        /// so the visual facing is frozen and the camera-relative STICK is the only live
        /// intent — using the facing made every throw go where the player was looking when
        /// the grab started. Order: anchor (when enabled for the throw) -> motor wish
        /// direction (stick) -> visual facing -> root forward -> world forward. Always
        /// flattened on Y and normalized.
        /// </summary>
        private Vector3 ThrowForward()
        {
            Vector3 dir;
            if (throwAlongAnchor && grabAnchor != null)
            {
                dir = grabAnchor.forward;
            }
            else if (motor != null && motor.WishDirection.sqrMagnitude > 0.0001f)
            {
                dir = motor.WishDirection;
            }
            else
            {
                dir = ForwardNow();
            }

            dir.y = 0f;
            if (dir.sqrMagnitude < 0.0001f) dir = transform.forward;
            dir.y = 0f;
            return dir.sqrMagnitude > 0.0001f ? dir.normalized : Vector3.forward;
        }

        /// <summary>
        /// Solves the launch velocity that lands the victim on the nearest throwable inside
        /// the throw cone. Writes the authored forward throw into <paramref name="velocity"/>
        /// first, so returning false (no target, unresolvable point, impossible angle) leaves
        /// the old behaviour intact.
        ///
        /// WHY the angle formula: Physics.gravity.y is -100 in this project, so the standard
        /// v² = g·d² / (2·cos²θ·(d·tanθ − h)) is evaluated with the REAL gravity magnitude.
        /// The horizontal direction is the aim-assist direction, not the raw forward.
        /// </summary>
        private bool TrySolveThrowArc(
            Vector3 origin,
            Rigidbody throwBody,
            Vector3 forward,
            out Vector3 velocity)
        {
            velocity = forward * throwSpeed + Vector3.up * throwLift;

            AimAssistDefinition definition = throwAimAssist != null ? throwAimAssist : grabAimAssist;
            bool found = AimAssist.TryFindTarget(
                origin,
                throwBody,
                forward,
                definition,
                Mathf.Max(0.01f, throwAimRange),
                AimAssist.ResolveThrowable,
                out Vector3 aimDir,
                out float distance);
            if (!found) return false;

            // AimAssist returns only direction + distance; the ballistic solve also needs the
            // target HEIGHT, so resolve the selected transform back around the expected point.
            Transform aimTarget = ResolveThrowAimTarget(origin, aimDir, distance);
            if (aimTarget == null) return false;

            Vector3 targetPosition = aimTarget.position;
            Vector3 flat = targetPosition - origin;
            flat.y = 0f;
            float d = flat.magnitude;
            if (d < 0.01f) return false;

            float h = targetPosition.y - origin.y;
            float g = Mathf.Abs(Physics.gravity.y);
            if (g <= 0.0001f) return false;

            float theta = Mathf.Clamp(throwAngleDeg, 15f, 75f) * Mathf.Deg2Rad;
            float cos = Mathf.Cos(theta);
            float denominator = 2f * cos * cos * (d * Mathf.Tan(theta) - h);
            if (denominator <= 0.0001f) return false; // target out of reach at this angle

            float vSquared = g * d * d / denominator;
            if (vSquared <= 0f) return false;

            float v = Mathf.Min(Mathf.Sqrt(vSquared), Mathf.Max(0.01f, maxThrowSpeed));
            velocity = aimDir * (v * cos) + Vector3.up * (v * Mathf.Sin(theta));
            return true;
        }

        /// <summary>
        /// Resolves the transform that <see cref="AimAssist.TryFindTarget"/> selected: the
        /// nearest throwable (active <see cref="IThrowTarget"/> or living
        /// <see cref="HitReceiver"/>) around <c>origin + direction * distance</c>.
        /// </summary>
        private static Transform ResolveThrowAimTarget(Vector3 origin, Vector3 aimDir, float distance)
        {
            Vector3 expected = origin + aimDir * distance;
            Collider[] colliders = Physics.OverlapSphere(
                expected, VictimMatchRadius, Physics.AllLayers, QueryTriggerInteraction.Ignore);

            Transform best = null;
            float bestSqr = float.MaxValue;
            for (int i = 0; i < colliders.Length; i++)
            {
                Transform candidate = AimAssist.ResolveThrowable(colliders[i]);
                if (candidate == null) continue;

                Vector3 delta = candidate.position - expected;
                delta.y = 0f;
                float sqr = delta.sqrMagnitude;
                if (sqr >= bestSqr) continue;

                bestSqr = sqr;
                best = candidate;
            }

            return best;
        }

        /// <summary>
        /// Adds the throw-time Rigidbody a body-less victim (debug dummy: primitive +
        /// <see cref="HitReceiver"/>) needs to fly. It is configured as a throwable body and
        /// scheduled for removal at the end of the flight window, so the dummy returns to its
        /// authored state (no Rigidbody) instead of permanently gaining physics.
        /// </summary>
        private Rigidbody AddTemporaryBody(HitReceiver target)
        {
            Rigidbody body = target.gameObject.AddComponent<Rigidbody>();
            body.mass = 1f;
            body.useGravity = true;
            body.freezeRotation = true;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            body.isKinematic = false;

            temporaryBody = body;
            StartCoroutine(DestroyTemporaryBodyAfterFlight(body));
            return body;
        }

        /// <summary>
        /// Removes the temporary Rigidbody once the body has actually come to rest, not after
        /// a nominal delay. A long ballistic arc (12 m at 45° with the speed clamp) outlives
        /// any fixed window, so the old timer froze the dummy mid-air; here the wait watches
        /// the real body state and gives up after a 4 s safety timeout. <see cref="ThrownBody"/>
        /// keeps its own <c>flightDuration</c>: only the Rigidbody component is destroyed.
        /// </summary>
        private IEnumerator DestroyTemporaryBodyAfterFlight(Rigidbody body)
        {
            // One frame so ReleaseThrow can apply the launch velocity before state is read.
            yield return null;

            float elapsed = 0f;
            while (body != null && elapsed < TemporaryBodySafetyTimeout)
            {
                if (body.isKinematic == false
                    && body.linearVelocity.sqrMagnitude < TemporaryBodyRestSpeedSqr
                    && HasLanded(body))
                {
                    break;
                }

                elapsed += Time.deltaTime; // scaled: a paused game never times the body out
                yield return null;
            }

            // The victim GameObject may be gone (dead dummy): nothing to clean up.
            if (body == null) yield break;
            if (temporaryBody == body) temporaryBody = null;
            Destroy(body);
        }

        /// <summary>
        /// True when the throw body is resting: asleep, gravity-less, or a short downward probe
        /// from its center of mass finds ground. The probe skips the body's own collider
        /// because a raycast starting inside a collider does not report it.
        /// </summary>
        /// <summary>
        /// DIAGNOSTIC (temporary): logs the body velocity after one physics step, to tell apart
        /// "the launch velocity was never written" from "something overwrote it".
        /// </summary>
        private System.Collections.IEnumerator LogVelocityAfterStep(Rigidbody body)
        {
            yield return new WaitForFixedUpdate();

            if (body == null)
            {
                Debug.Log($"[{nameof(PlayerGrab)}] VELOCITY AFTER STEP: body destroyed", this);
                yield break;
            }

            Debug.Log(
                $"[{nameof(PlayerGrab)}] VELOCITY AFTER STEP {body.linearVelocity} | " +
                $"kinematic {body.isKinematic} | pos {body.position}",
                this);
        }

        private static bool HasLanded(Rigidbody body)
        {
            if (body.IsSleeping()) return true;
            if (!body.useGravity) return true;

            return Physics.Raycast(
                body.worldCenterOfMass,
                Vector3.down,
                TemporaryBodyGroundProbe,
                Physics.AllLayers,
                QueryTriggerInteraction.Ignore);
        }

        /// <summary>Removes a temporary body if the component is disabled mid-flight.</summary>
        private void ClearTemporaryBody()
        {
            if (temporaryBody == null) return;

            Destroy(temporaryBody);
            temporaryBody = null;
        }

        /// <summary>Releases the hold token (idempotent; safe on teardown).</summary>
        private void ReleaseHoldToken()
        {
            if (holdToken == null) return;
            if (motor != null && motor.Body != null) motor.Body.Release(holdToken);
            holdToken = null;
        }

        /// <summary>
        /// Drops any hold without throwing (victim died, component disabled). An anchored
        /// victim is detached and its body made dynamic again, so a cut scene/disable never
        /// leaves an enemy welded to the bone.
        /// </summary>
        private void CancelGrab()
        {
            // Null-check first: the victim GameObject may already be destroyed (dead
            // dummy), in which case there is nothing to detach.
            if (victimParented && victim != null)
            {
                victim.transform.SetParent(null, worldPositionStays: true);
            }
            if (victimBody != null) victimBody.isKinematic = false;

            victimParented = false;
            isGrabbing = false;
            grabTimer = 0f;
            victim = null;
            victimBody = null;
            ClearTemporaryBody();
            ReleaseHoldToken();
        }

        /// <summary>Horizontal facing right now (visual when present, transform fallback).</summary>
        private Vector3 ForwardNow()
        {
            Vector3 facing = visual != null ? visual.Facing : transform.forward;
            facing.y = 0f;
            return facing.sqrMagnitude > 0.0001f ? facing.normalized : Vector3.forward;
        }
    }
}
