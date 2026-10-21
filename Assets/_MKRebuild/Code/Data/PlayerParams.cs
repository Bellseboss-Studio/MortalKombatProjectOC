using UnityEngine;

namespace MK.Rebuild
{
    [CreateAssetMenu(fileName = "PlayerParams", menuName = "MK/Rebuild/PlayerParams")]
    public sealed class PlayerParams : ScriptableObject
    {
        [Header("Locomotion")]
        [Tooltip("Walk speed in m/s (FIXED across the walk input zone — not analog-scaled).")]
        public float walkSpeed = 6.0f;

        [Tooltip("Run speed in m/s (FIXED across the run input zone).")]
        public float runSpeed = 9.0f;

        [Tooltip("Takeoff velocity in m/s (NOT height). apex = v^2 / 2g.")]
        public float jumpVelocity = 8.0f;

        [Tooltip("Gravity applied when airborne (negative Y, m/s^2).")]
        public float gravity = -20f;

        [Header("Move input zones (fixed discrete speeds)")]
        [Tooltip("Stick magnitude below this = deadzone/idle.")]
        [Range(0f, 1f)]
        public float walkInputThreshold = 0.2f;

        [Tooltip("Stick magnitude up to this = walk; above = run.")]
        [Range(0f, 1f)]
        public float runInputThreshold = 0.4f;

        [Header("Timing")]
        [Tooltip("Seconds a jump press stays buffered while airborne.")]
        public float jumpBuffer = 0.15f;

        [Tooltip("ACTIVE since M0 feel-calibration (Mario jump): grace time to jump after leaving ground.")]
        public float coyoteTime = 0.12f;

        [Tooltip("Landing recovery FALLBACK total (seconds) — used only when the Landing clip " +
                  "cannot be resolved at runtime. The LIVE value is DERIVED: Landing clip " +
                  "effective duration + jumpLockoutExtra (0.467 / 2 + 0.1 = ~0.333 s). During " +
                  "this window a still-buffered ground/coyote jump may NOT re-fire, so the " +
                  "animator gets a real grounded dwell instead of re-launching on the first " +
                  "grounded frame (jump-spam thrash).")]
        public float jumpLockout = 0.33f;

        [Tooltip("Animator state speed multiplier for the Landing state. MUST match the Landing " +
                  "state's m_Speed in Leena.controller (currently 2). The jump lockout derives the " +
                  "Landing clip's EFFECTIVE duration as clipLength / landingClipSpeed. Same retiming " +
                  "home as dashClipSpeed / mantleClipSpeed / deathClipSpeed.")]
        public float landingClipSpeed = 2.0f;

        [Tooltip("Extra settle time (seconds) added on top of the Landing clip's effective " +
                  "duration to form the total jump lockout. Higher = the landing reads fully " +
                  "before a buffered re-jump may fire (more settle / less responsive bunny-hop).")]
        public float jumpLockoutExtra = 0.1f;

        [Header("Mario jump (tunables)")]
        [Tooltip("Descent gravity multiplier: gravity x this while falling (snap down).")]
        public float fallGravityMultiplier = 2.2f;

        [Tooltip("Releasing Jump while ascending scales upward velocity by this (short hop).")]
        [Range(0f, 1f)]
        public float jumpCutMultiplier = 0.45f;

        [Tooltip("Fastest allowed fall speed (negative Y, m/s).")]
        public float maxFallSpeed = -25f;

        [Header("Jump clip retiming (TEMPORARY — set to 1 once clips are retimed to x1)")]
        [Tooltip("Animator state speed multiplier for JumpStart (~ clipLength / launchWindow; " +
                 "launch = ascent half, ~0.20 s). Somersault-on-ascent iteration.")]
        public float jumpStartSpeed = 8.2f;

        [Tooltip("Animator state speed multiplier for FlipMidAir (~ clipLength / flipWindow; " +
                 "flip = mid-ascent to touchdown, ~0.49 s). Somersault-on-ascent iteration.")]
        public float flipMidAirSpeed = 3.5f;

        [Header("Dash clip retiming (TEMPORARY — set to 1 once clips are retimed to x1)")]
        [Tooltip("Animator state speed multiplier for the Dash state (~ clipLength / dashWindow; " +
                 "anim_dash2_north 0.80 s over the 0.35 s dash ≈ 2.29x). Same retiming home as " +
                 "jumpStartSpeed; drives the Dash state's DashSpeed parameter.")]
        public float dashClipSpeed = 2.29f;

        [Header("Death clip retiming")]
        [Tooltip("Animator state speed multiplier for the Death state. The Death state plays the " +
                 "trimmed collapse clip anim_dizzy_death_collapse (2.5 s: 7.0→9.5 s of the source, " +
                 "the readable fall; the 12.667 s source was dizzy → collapse → motionless corpse). " +
                 "3.5 speeds the collapse to ~0.71 s. The respawn delay is DERIVED from it: " +
                 "AnimatorVisual.DeathDuration = clip.length / this value, so the teleport happens " +
                 "when the collapse completes (same contract as hitReactionClipSpeed).")]
        public float deathClipSpeed = 3.5f;

        [Header("Hit reaction clip retiming")]
        [Tooltip("Animator state speed multiplier for the HitReaction state. The body lock is " +
                 "DERIVED from the trimmed impact clip: AnimatorVisual.HitReactionDuration = " +
                 "clip.length / this value, so the lock and the animation can never drift apart. " +
                 "1 = natural playback of the 0.40 s impact clip (peak lean + 2-frame hold).")]
        public float hitReactionClipSpeed = 1.0f;

        [Header("Combat tuning (live)")]
        [Tooltip("LIVE attack cadence multiplier (DebugWindow F1 sliders). Multiplies every combo step's " +
                  "speedMultiplier, so the effective step duration becomes duracion / (speed * this). " +
                  "1 = data as authored; 2 = double speed (half duration).")]
        [Range(0.5f, 2f)]
        public float attackSpeedScale = 1f;

        [Tooltip("LIVE attack reach multiplier (DebugWindow F1 sliders). Multiplies BOTH the hitbox " +
                  "radius and its forward offset, so the sphere edge (radius + offset.z) grows with it. " +
                  "1 = data as authored; 1.5 = 50% more reach.")]
        [Range(0.5f, 1.5f)]
        public float attackReachScale = 1f;

        [Tooltip("LIVE hitstop multiplier (DebugWindow F1 sliders). Multiplies every step's hitstopMs " +
                  "freeze on connect. 1 = data as authored; 0 = no freeze at all.")]
        [Range(0f, 2f)]
        public float hitstopScale = 1f;

        [Tooltip("LIVE cancel-window start multiplier (DebugWindow F1 sliders). The cancel START is " +
                  "divided by this (window.x / scale), so > 1 opens the cancel EARLIER. The cancel END " +
                  "is untouched. 1 = data as authored.")]
        [Range(0.5f, 1.5f)]
        public float cancelStartScale = 1f;

        [Tooltip("LIVE victim cap override (DebugWindow F1 sliders). 0 = use each step's own " +
                  "maxTargets (data as authored); N > 0 = every step may damage up to N victims.")]
        [Range(0, 6)]
        public int maxTargetsOverride = 0;

        [Tooltip("Seconds a stick direction must be held longer than this to stop counting as a " +
                  "deliberate attack variant. A flick (direction entered within this window) selects " +
                  "Forward/Side/Back roots; walking with the stick held uses the neutral combo. " +
                  "Fixes 'holding toward the enemy interrupts the combo'.")]
        [Range(0.05f, 0.5f)]
        public float directionFlickWindow = 0.22f;

        [Header("Feel (tunables)")]
        [Tooltip("Seconds to reach full speed (ground).")]
        public float accelTime = 0.08f;

        [Tooltip("Seconds to stop from full speed (ground).")]
        public float decelTime = 0.10f;

        [Tooltip("Ground accel multiplier while airborne (0-1).")]
        [Range(0f, 1f)]
        public float airControlFactor = 0.66f;

        [Tooltip("Visual facing rotation speed (deg/s).")]
        public float turnSpeed = 720f;

        [Header("Grounding")]
        [Tooltip("Radius (m) of the 4 cardinal ground rays cast DOWN from the body's base " +
                  "footprint (±offset in X and ±offset in Z around the center). Near the " +
                  "capsule radius; tune if the rays sit too far from or too close to the center.")]
        public float groundRayOffset = 0.4f;

        [Tooltip("Minimum surface normal Y for a ground ray hit to count as ground. " +
                  "Walls, shoulders and capsule sides are not ground; flat floors and " +
                  "slopes up to 45 degrees still are (0.7).")]
        [Range(0f, 1f)]
        public float groundNormalMinDot = 0.7f;

        [Header("Wall motion (M0)")]
        [Tooltip("Forward probe distance for wall contact (m). Used as the SHORT ray " +
                  "length toward the wall to recover the surface normal.")]
        public float wallCheckDistance = 0.7f;

        [Tooltip("Wall-grab band volume full size in meters (XYZ). The wall check " +
                  "queries exactly this box at wallBandHeight above the body center — a " +
                  "body-width slice, NOT a single center ray. Should span at least " +
                  "wallCheckDistance so the band reaches the wall.")]
        public Vector3 wallBandSize = new Vector3(1.2f, 0.6f, 1.2f);

        [Tooltip("Height above the body center of the wall-grab band. Mirrors " +
                  "hangGripHeight so the wall and ledge checks sit at the same chest " +
                  "height — consistent reads at a corner.")]
        public float wallBandHeight = 0.4f;

        [Tooltip("Max |normal.y| that still counts as a wall (floor/ceiling rejected).")]
        [Range(0f, 1f)]
        public float wallNormalThreshold = 0.5f;

        [Tooltip("Hang-grab vertical speed while in wall contact (0 = full stop; negative = slide).")]
        public float wallSlideSpeed = 0.0f;

        [Tooltip("Wall-jump takeoff velocity (m/s).")]
        public float wallJumpVelocity = 8.0f;

        [Tooltip("Wall-jump horizontal push-off away from the wall (m/s).")]
        public float wallJumpPushOff = 6.0f;

        [Tooltip("Seconds of horizontal control lockout after a wall-jump.")]
        public float wallJumpLockTime = 0.15f;

        [Tooltip("Seconds after a wall-jump before the same wall can be re-grabbed.")]
        public float wallReGrabDelay = 0.20f;

        [Tooltip("Seconds after a Down-release from a ledge before THAT ledge can " +
                 "hang again (drop-through: the player falls past without re-hanging). " +
                 "Per-ledge, so other lips stay live. Sized above the longest track " +
                 "fall past a lip (~0.5 s) with margin.")]
        public float ledgeReGrabDelay = 0.6f;

        [Tooltip("Wall grab only fires when facing the wall: dot(facing, -wallNormal) " +
                  "must exceed this. Back-to-wall contact slides/falls instead of grabbing.")]
        [Range(0f, 1f)]
        public float facingDotThreshold = 0.5f;

        [Tooltip("Grace time to chain a wall-jump after losing wall contact.")]
        public float wallCoyoteTime = 0.12f;

        [Tooltip("Small up+away hop applied when releasing a wall with Down (m/s).")]
        public float wallReleaseHop = 3.0f;

        [Tooltip("Move.y at or below this while hanging releases the wall (Down).")]
        public float wallReleaseThreshold = -0.5f;

        [Header("Ledge (mantle)")]
        [Tooltip("Snap-to-grip: how far below the lip top the root hangs on grab " +
                  "(root Y = lipTop - gripDepth, same XZ). Hands read at the lip.")]
        public float gripDepth = 1.1f;

        [Tooltip("Hand-context: height above the body center of the hands/chest band. " +
                  "The ledge trigger must overlap THIS band (not just any part of the " +
                  "body) to hang — feet/head-level passes do not grab.")]
        public float hangGripHeight = 0.5f;

        [Tooltip("Hands-band volume full size in meters (XYZ). The ledge hang check " +
                  "queries exactly this box at hangGripHeight above the body center — " +
                  "small and chest-sized, NOT the body extents.")]
        public Vector3 hangBandSize = new Vector3(0.6f, 0.3f, 0.6f);

        [Tooltip("True = auto-mantle the top on contact; false = hang + Jump to mantle (default).")]
        public bool autoMantle = false;

        [Tooltip("Seconds the animation-driven mantle warp lasts (SmoothStep from hang to stand).")]
        public float mantleDuration = 0.3f;

        [Tooltip("Animator state speed multiplier for Mantle (~ clipLength / mantleDuration; " +
                 "climbFlip 0.83 s over the 0.3 s warp ≈ 2.78x). TEMPORARY retiming — set to 1 " +
                 "once the clip is retimed to x1. >3x reads frantic.")]
        public float mantleClipSpeed = 2.78f;

        [Tooltip("Forward step in meters taken after the mantle warp lands, along the mantle " +
                 "direction (over the edge). Clears the ledge trigger overlap so the next frame " +
                 "reads grounded Free (idle/walk) instead of hanging. Keeps the landing height.")]
        public float mantleStepForward = 0.8f;

        [Tooltip("Mantle arc bulge as a MULTIPLIER of the body radius (capsule radius). The warp " +
                  "path is a quadratic Bezier whose control point sits at the lip corner pushed UP " +
                  "+ OUTWARD (away from the wall) by body.radius x this. Higher = the curve bulges " +
                  "more over the lip (clears the corner with more margin); 0 = straight line. " +
                  "Default ~1.5 x body radius (0.5 m) clears the lip.")]
        public float mantleArcRadius = 1.5f;
    }
}
