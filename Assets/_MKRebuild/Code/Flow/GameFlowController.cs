using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MK.Rebuild
{
    /// <summary>
    /// Drives the unattended demo through nine states with an explicit transition table.
    ///
    /// WHY a table and not "play the beat then set the state": every cinematic can fail
    /// (definition missing, binding invalid, director already busy) and the demo must not
    /// stall on a missing asset. <see cref="Enter"/> accepts ONLY the authored arrows, so a
    /// stray trigger can never teleport the flow; a state whose beat cannot play advances
    /// to its own successor instead of waiting forever.
    ///
    /// Boss progress is event-driven, never polled: the encounter trigger starts
    /// <see cref="FlowState.BossIntro"/> and the spawned boss reports its phase change and
    /// death. Subscriptions are taken when the boss exists and dropped the moment the flow
    /// leaves the boss states, so a destroyed boss can never call into a stale flow.
    ///
    /// Restart is ONE scene reload (design §9). Resetting system by system would have to undo
    /// the encounter trigger, the arena exit, the boss phase and every camera zone; a reload
    /// is the only reset that cannot miss one.
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(-200)]
    public sealed class GameFlowController : MonoBehaviour
    {
        [Tooltip("Plays the cinematic states. Auto-resolved from the scene when empty.")]
        [SerializeField] private CinematicDirector cinematics;

        [Tooltip("Opening beat. Empty = the flow goes straight to Exploration.")]
        [SerializeField] private CinematicDefinition intro;

        [Tooltip("Optional mid-demo reveal fired by a FlowTrigger volume.")]
        [SerializeField] private CinematicDefinition beat;

        [Tooltip("Boss encounter entry beat.")]
        [SerializeField] private CinematicDefinition bossIntro;

        [Tooltip("Phase 1 to 2 beat.")]
        [SerializeField] private CinematicDefinition bossTransition;

        [Tooltip("Boss death beat. When it completes, the restart-only pause card opens.")]
        [SerializeField] private CinematicDefinition demoEnd;

        [Tooltip("Encounter zone whose Triggered event starts BossIntro. Serialized so the flow hears the very first spawn.")]
        [SerializeField] private BossEncounterTrigger bossTrigger;

        [Tooltip("Restart card. Auto-resolved from the scene when empty.")]
        [SerializeField] private PauseMenu pauseMenu;

        [Tooltip("Optional music owner. Boss states switch it to the boss bed.")]
        [SerializeField] private MusicStateController music;

        private FlowState state = FlowState.Boot;
        private bool started;
        private BossController boss;

        /// <summary>Scene singleton. The flow is the only owner of the demo state.</summary>
        public static GameFlowController Instance { get; private set; }

        /// <summary>Current state. Read-only: use <see cref="Enter"/> or <see cref="JumpTo"/>.</summary>
        public FlowState State => state;

        /// <summary>Raised after every accepted transition with (previous, next).</summary>
        public event Action<FlowState, FlowState> StateChanged;

        private void Awake()
        {
            Instance = this;

            // A reload must not inherit a frozen clock or muted audio from the session
            // that asked for the restart (design §9).
            ControlLock.Reset();
            AudioListener.pause = false;

            if (cinematics == null) cinematics = FindFirstObjectByType<CinematicDirector>();
            if (cinematics != null) cinematics.Completed += HandleCinematicCompleted;

            if (music == null) music = FindFirstObjectByType<MusicStateController>();

            // Subscribed BEFORE the boot chain: a trigger that fires during the first frame
            // must still start the boss intro instead of being missed.
            if (bossTrigger != null) bossTrigger.Triggered += HandleBossTriggered;

            Enter(FlowState.Boot);
        }

        private void OnDestroy()
        {
            if (bossTrigger != null) bossTrigger.Triggered -= HandleBossTriggered;
            if (cinematics != null) cinematics.Completed -= HandleCinematicCompleted;

            UnsubscribeBoss();

            if (Instance == this) Instance = null;
        }

        /// <summary>
        /// Requests <paramref name="next"/>. Anything the table does not allow (and any
        /// self-transition) is rejected with a single log and no state change.
        /// </summary>
        public void Enter(FlowState next)
        {
            if (!started)
            {
                if (next != FlowState.Boot)
                {
                    Debug.Log($"[{nameof(GameFlowController)}] Rejected {next}: the flow must boot first.", this);
                    return;
                }

                started = true;
                ApplyEnter(next);
                return;
            }

            if (!IsAllowed(state, next))
            {
                Debug.Log($"[{nameof(GameFlowController)}] Rejected {state} -> {next}: not in the transition table.", this);
                return;
            }

            ApplyEnter(next);
        }

        /// <summary>
        /// Debug escape hatch: stops any running beat (releasing its locks and AI freeze),
        /// then enters <paramref name="target"/> IGNORING the table. Scene wiring and the
        /// debug window use it; gameplay code must use <see cref="Enter"/>.
        /// </summary>
        public void JumpTo(FlowState target)
        {
            if (cinematics != null) cinematics.Stop();

            started = true;
            ApplyEnter(target);
        }

        /// <summary>
        /// Clean restart: stop the beat, clear every control handle, unmute audio and reload
        /// the active scene. The reload re-enters <see cref="FlowState.Boot"/>, which repeats
        /// the reset, so a restart from an odd state cannot boot frozen.
        /// </summary>
        public void RestartLevel()
        {
            if (cinematics != null) cinematics.Stop();

            ControlLock.Reset();
            AudioListener.pause = false;

            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        // --- State machinery -------------------------------------------------

        /// <summary>
        /// Applies an already-validated state. Boss subscriptions are dropped BEFORE the
        /// new state runs, so re-entering BossIntro re-subscribes exactly once.
        /// </summary>
        private void ApplyEnter(FlowState next)
        {
            if (!IsBossState(next)) UnsubscribeBoss();

            FlowState previous = state;
            state = next;

            ApplyMusic(next);
            StateChanged?.Invoke(previous, next);
            OnEnter(next);
        }

        private void OnEnter(FlowState next)
        {
            switch (next)
            {
                case FlowState.Boot:
                    Enter(FlowState.Intro);
                    break;

                case FlowState.Intro:
                case FlowState.Beat:
                case FlowState.BossIntro:
                case FlowState.BossTransition:
                case FlowState.DemoEnd:
                    if (next == FlowState.BossIntro) SubscribeBoss();
                    PlayOrAdvance(DefinitionFor(next));
                    break;
            }
        }

        /// <summary>
        /// Plays the state's beat, or moves on when it cannot play. This is the anti-stall
        /// path: a missing definition, a missing director or a rejected Play all fall through
        /// to the table's successor.
        /// </summary>
        private void PlayOrAdvance(CinematicDefinition definition)
        {
            if (definition != null && cinematics != null && cinematics.Play(definition)) return;

            if (definition != null && cinematics == null)
            {
                Debug.LogWarning(
                    $"[{nameof(GameFlowController)}] '{definition.name}' has no {nameof(CinematicDirector)}; skipping the beat.",
                    this);
            }

            FlowState? next = NextOf(state);
            if (next.HasValue)
            {
                Enter(next.Value);
                return;
            }

            OpenRestartCard();
        }

        private void HandleCinematicCompleted(CinematicDefinition definition)
        {
            if (!IsCurrentCinematic(definition)) return;

            if (state == FlowState.DemoEnd)
            {
                OpenRestartCard();
                return;
            }

            FlowState? next = NextOf(state);
            if (next.HasValue) Enter(next.Value);
        }

        private void HandleBossTriggered()
        {
            Enter(FlowState.BossIntro);
        }

        private void HandleBossPhaseChanged(int phase)
        {
            if (phase < 2) return;
            Enter(FlowState.BossTransition);
        }

        private void HandleBossDied()
        {
            Enter(FlowState.DemoEnd);
        }

        private void SubscribeBoss()
        {
            UnsubscribeBoss();

            BossSpawner spawner = BossSpawner.Instance != null
                ? BossSpawner.Instance
                : FindFirstObjectByType<BossSpawner>();

            boss = spawner != null ? spawner.Current : null;
            if (boss == null)
            {
                Debug.LogWarning(
                    $"[{nameof(GameFlowController)}] BossIntro entered but no live boss was found; phase/death signals will not arrive.",
                    this);
                return;
            }

            boss.PhaseChanged += HandleBossPhaseChanged;
            boss.Died += HandleBossDied;
        }

        private void UnsubscribeBoss()
        {
            if (boss == null) return;

            boss.PhaseChanged -= HandleBossPhaseChanged;
            boss.Died -= HandleBossDied;
            boss = null;
        }

        private void OpenRestartCard()
        {
            if (pauseMenu == null) pauseMenu = FindFirstObjectByType<PauseMenu>();
            if (pauseMenu == null)
            {
                Debug.LogWarning(
                    $"[{nameof(GameFlowController)}] DemoEnd reached without a {nameof(PauseMenu)}; restart manually.",
                    this);
                return;
            }

            pauseMenu.Open(restartOnly: true);
        }

        private CinematicDefinition DefinitionFor(FlowState next)
        {
            switch (next)
            {
                case FlowState.Intro: return intro;
                case FlowState.Beat: return beat;
                case FlowState.BossIntro: return bossIntro;
                case FlowState.BossTransition: return bossTransition;
                case FlowState.DemoEnd: return demoEnd;
                default: return null;
            }
        }

        private bool IsCurrentCinematic(CinematicDefinition definition)
        {
            if (definition == null) return false;
            return definition == DefinitionFor(state);
        }

        private void ApplyMusic(FlowState next)
        {
            // Only the boss block is asserted here. The exploration bed is authored on the
            // MusicStateController itself, so overwriting it from the flow would silently
            // discard the scene's choice.
            if (music == null || !IsBossState(next)) return;
            music.SetBoss();
        }

        private static bool IsBossState(FlowState next)
        {
            return next == FlowState.BossIntro
                || next == FlowState.BossFight
                || next == FlowState.BossTransition
                || next == FlowState.BossPhase2;
        }

        /// <summary>
        /// The forced successor used when a beat cannot play (and by <see cref="Enter"/>'s
        /// callers). Null means "no automatic successor": Exploration has two possible
        /// arrows and DemoEnd is the end of the script.
        /// </summary>
        private static FlowState? NextOf(FlowState from)
        {
            switch (from)
            {
                case FlowState.Boot: return FlowState.Intro;
                case FlowState.Intro: return FlowState.Exploration;
                case FlowState.Beat: return FlowState.Exploration;
                case FlowState.BossIntro: return FlowState.BossFight;
                case FlowState.BossTransition: return FlowState.BossPhase2;
                case FlowState.BossPhase2: return FlowState.DemoEnd;
                default: return null;
            }
        }

        /// <summary>The demo script, verbatim. An arrow that is not listed does not exist.</summary>
        private static bool IsAllowed(FlowState from, FlowState to)
        {
            switch (from)
            {
                case FlowState.Boot: return to == FlowState.Intro;
                case FlowState.Intro: return to == FlowState.Exploration;
                case FlowState.Exploration: return to == FlowState.Beat || to == FlowState.BossIntro;
                case FlowState.Beat: return to == FlowState.Exploration;
                case FlowState.BossIntro: return to == FlowState.BossFight;
                case FlowState.BossFight: return to == FlowState.BossTransition;
                case FlowState.BossTransition: return to == FlowState.BossPhase2;
                case FlowState.BossPhase2: return to == FlowState.DemoEnd;
                default: return false;
            }
        }
    }
}
