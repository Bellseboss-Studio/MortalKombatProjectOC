using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace MK.Rebuild.Debugging
{
    /// <summary>
    /// TEMPORARY, READ-ONLY diagnostic probe for the jump-spam "animator thrash"
    /// (change <c>fix-jump-spam</c>). DELETE THIS FILE once the data is read.
    ///
    /// It <b>observes</b> the player: it never writes gameplay, never touches the
    /// Animator/controller, and never calls into PlayerMotor. It lives in the
    /// isolated <c>MK.Rebuild.Debug</c> assembly (<c>autoReferenced:false</c>) so
    /// the runtime never sees it.
    ///
    /// What it logs (ONE line per CHANGE — keeps output bounded):
    ///   - Animator layer-0 current state: readable short name + full-path hash +
    ///     normalizedTime; plus the NEXT state + its normalizedTime while a
    ///     transition is in flight (<c>IsInTransition</c>) — transitions are the
    ///     gold signal for "the graph is thrashing".
    ///   - Animator params: Grounded, MoveState, Running, VerticalSpeed, Speed.
    ///   - PlayerMotor: grounded (public seam) + buffer/lockout timers read via
    ///     reflection (they are private; best-effort, falls back to -1 if absent).
    ///
    /// A line is emitted only when one of these DISCRETE signals changes: current
    /// state, transition state, next-state, Grounded, MoveState, Running, motor
    /// grounded, buffer active (&gt;0), lockout active (&gt;0). Continuous floats
    /// (VerticalSpeed/Speed) are printed on every line but never TRIGGER a line,
    /// so the log stays a clean transition trace.
    ///
    /// Every line is prefixed with <see cref="Tag"/> (<c>[THRASH]</c>) so the
    /// console can be filtered. A hard cap (<see cref="maxLines"/>, default 300)
    /// stops logging so a long session cannot flood the console.
    ///
    /// Boot: attach this component to the <c>Player</c> GameObject in
    /// <c>Playground.unity</c> and <c>Gym.unity</c> (manual component, same
    /// pattern as <see cref="DebugWindow"/>/<see cref="PlayerDamageTestDriver"/> —
    /// NO <c>[RuntimeInitializeOnLoadMethod]</c>). The Animator is auto-resolved
    /// via <c>GetComponentInChildren</c>, with a scene-wide fallback.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class AnimatorThrashProbe : MonoBehaviour
    {
        /// <summary>Stable console prefix — filter the Console by <c>[THRASH]</c>.</summary>
        public const string Tag = "[THRASH]";

        [Tooltip("Hard cap on [THRASH] lines logged. The probe goes silent after this. Default 300.")]
        [SerializeField] private int maxLines = 300;

        [Tooltip("Player motor to observe. Null = auto-resolved (own GO, then first in scene).")]
        [SerializeField] private PlayerMotor motor;

        private Animator animator;
        private Rigidbody body;
        private int logged;
        private bool capped;

        // --- Change-detection state (last emitted values) --------------------
        private bool first = true;
        private int lastStateHash = int.MinValue;
        private int lastNextHash = int.MinValue;
        private bool lastInTransition;
        private bool lastAnimGrounded;
        private int lastMoveState = int.MinValue;
        private bool lastRunning;
        private bool lastMotorGrounded;
        private bool lastBufferActive;
        private bool lastLockoutActive;

        // --- Animator param hashes -------------------------------------------
        private static readonly int GroundedHash = Animator.StringToHash("Grounded");
        private static readonly int MoveStateHash = Animator.StringToHash("MoveState");
        private static readonly int RunningHash = Animator.StringToHash("Running");
        private static readonly int VerticalSpeedHash = Animator.StringToHash("VerticalSpeed");
        private static readonly int SpeedHash = Animator.StringToHash("Speed");

        // --- Readable state names (short-name-hash -> name) ------------------
        // Authored from Leena.controller (Base Layer). A state missing from this
        // list still logs, as "?0xHASH" — never silently drops.
        private static readonly string[] KnownStates =
        {
            "Idle", "Walk", "Run", "RunStop", "JumpStart", "FlipMidAir", "Landing",
            "Mantle", "LedgeGrab", "LedgeHang", "Dash", "HitReaction", "Death",
            "Attack_P1", "Attack_P2", "Attack_P3", "Attack_Dash", "Attack_Launcher",
            "Attack_H1", "Attack_H2", "TraversalPlaceholder"
        };

        private static readonly Dictionary<int, string> StateNames = BuildStateNames();

        private static Dictionary<int, string> BuildStateNames()
        {
            var map = new Dictionary<int, string>(KnownStates.Length);
            for (int i = 0; i < KnownStates.Length; i++)
                map[Animator.StringToHash(KnownStates[i])] = KnownStates[i];
            return map;
        }

        // --- PlayerMotor reflection (private timers; read-only) --------------
        private FieldInfo bufferField;
        private FieldInfo lockoutField;

        private void Awake()
        {
            Resolve();
        }

        private void Resolve()
        {
            if (motor == null) motor = GetComponent<PlayerMotor>();
            if (motor == null) motor = FindFirstObjectByType<PlayerMotor>();
            if (motor == null) return;

            if (animator == null) animator = motor.GetComponentInChildren<Animator>(true);
            if (animator == null) animator = GetComponentInChildren<Animator>(true);
            if (body == null) body = motor.GetComponent<Rigidbody>();

            if (bufferField == null)
                bufferField = typeof(PlayerMotor).GetField(
                    "bufferTimer", BindingFlags.Instance | BindingFlags.NonPublic);
            if (lockoutField == null)
                lockoutField = typeof(PlayerMotor).GetField(
                    "jumpLockoutTimer", BindingFlags.Instance | BindingFlags.NonPublic);
        }

        private void Update()
        {
            if (capped) return;
            if (motor == null || animator == null)
            {
                Resolve();
                if (motor == null || animator == null) return;
            }

            var state = animator.GetCurrentAnimatorStateInfo(0);
            int stateHash = state.shortNameHash;
            float normalizedTime = state.normalizedTime;

            bool inTransition = animator.IsInTransition(0);
            int nextHash = int.MinValue;
            float nextNormalized = 0f;
            if (inTransition)
            {
                var next = animator.GetNextAnimatorStateInfo(0);
                nextHash = next.shortNameHash;
                nextNormalized = next.normalizedTime;
            }

            bool animGrounded = animator.GetBool(GroundedHash);
            int moveState = animator.GetInteger(MoveStateHash);
            bool running = animator.GetBool(RunningHash);
            float verticalSpeed = animator.GetFloat(VerticalSpeedHash);
            float speed = animator.GetFloat(SpeedHash);

            bool motorGrounded = motor.IsGrounded;

            float buffer = ReadFloat(bufferField);
            float lockout = ReadFloat(lockoutField);
            bool bufferActive = buffer > 0f;
            bool lockoutActive = lockout > 0f;

            bool changed = first
                || stateHash != lastStateHash
                || inTransition != lastInTransition
                || nextHash != lastNextHash
                || animGrounded != lastAnimGrounded
                || moveState != lastMoveState
                || running != lastRunning
                || motorGrounded != lastMotorGrounded
                || bufferActive != lastBufferActive
                || lockoutActive != lastLockoutActive;

            if (!changed) return;

            first = false;
            lastStateHash = stateHash;
            lastInTransition = inTransition;
            lastNextHash = nextHash;
            lastAnimGrounded = animGrounded;
            lastMoveState = moveState;
            lastRunning = running;
            lastMotorGrounded = motorGrounded;
            lastBufferActive = bufferActive;
            lastLockoutActive = lockoutActive;

            string transition = inTransition
                ? $" -> {Name(nextHash)} nt={nextNormalized:0.00}"
                : string.Empty;

            Debug.Log(
                $"{Tag} f={Time.frameCount} t={Time.unscaledTime:0.000}" +
                $" state={Name(stateHash)} (fp=0x{state.fullPathHash:X8}) nt={normalizedTime:0.00}{transition}" +
                $" | Grounded={animGrounded} MoveState={moveState} Running={running}" +
                $" VS={verticalSpeed:0.0} Speed={speed:0.0}" +
                $" | motor(grounded={motorGrounded} buffer={buffer:0.00} lockout={lockout:0.00})");

            logged++;
            if (logged >= maxLines)
            {
                capped = true;
                Debug.Log($"{Tag} cap reached ({maxLines} lines) — probe silent. " +
                          "Raise maxLines on the component to keep logging.");
            }
        }

        private static string Name(int shortNameHash)
        {
            return StateNames.TryGetValue(shortNameHash, out var name)
                ? name
                : "?0x" + shortNameHash.ToString("X8");
        }

        private float ReadFloat(FieldInfo field)
        {
            if (field == null || motor == null) return -1f;
            object value = field.GetValue(motor);
            return value is float f ? f : -1f;
        }
    }
}
