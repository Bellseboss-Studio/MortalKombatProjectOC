using System;
using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace MK.Rebuild
{
    /// <summary>
    /// Plays one authored <see cref="CinematicDefinition"/> through its pre-bound
    /// <see cref="PlayableDirector"/> and owns everything a beat borrows from gameplay:
    /// the body token, the cinematic control reason, enemy AI, the HUD and a short
    /// invulnerability window.
    ///
    /// It is a BORROWER, never an owner. Every lock is released on the same path that
    /// stops playback (natural stop, <see cref="Stop"/>, or disable), so a failed or
    /// interrupted beat can never leave the player soft-locked with no input.
    ///
    /// One director per definition on purpose (design §0): swapping
    /// <c>playableAsset</c> at runtime loses the exposed shot-camera references a
    /// Timeline needs, so the bindings table maps each definition to its own director.
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(160)]
    public sealed class CinematicDirector : MonoBehaviour
    {
        /// <summary>Definition → pre-bound director pair, serialized so the shot references stay authored.</summary>
        [Serializable]
        public struct CinematicBinding
        {
            [Tooltip("Definition this entry can play.")]
            public CinematicDefinition definition;

            [Tooltip("Pre-bound director whose playable asset is this definition's timeline. One director per definition.")]
            public PlayableDirector director;
        }

        [Tooltip("Definition → director pairs. One director per definition keeps its authored shot references intact.")]
        [SerializeField] private CinematicBinding[] bindings = Array.Empty<CinematicBinding>();

        [Tooltip("Root of the gameplay HUD. Hidden only while a definition with Hide Hud plays, and restored afterwards.")]
        [SerializeField] private GameObject hudRoot;

        [Tooltip("Camera arbiter used to re-resolve priorities after a beat. Null = CameraDirector.Instance.")]
        [SerializeField] private CameraDirector cameras;

        [Tooltip("Player motor that owns the body token. Null = first PlayerMotor in the scene.")]
        [SerializeField] private PlayerMotor motor;

        [Tooltip("Player-side raw-clip player for cinematic poses. Null = first CinematicActor under the motor's GameObject.")]
        [SerializeField] private CinematicActor playerActor;

        [Tooltip("Player damage used for the invulnerability window. Null = first PlayerDamage in the scene.")]
        [SerializeField] private PlayerDamage playerDamage;

        private readonly List<EnemyController> frozenEnemies = new List<EnemyController>();
        private readonly List<bool> frozenStates = new List<bool>();
        private readonly List<BossController> frozenBosses = new List<BossController>();
        private readonly List<bool> frozenBossStates = new List<bool>();

        private CinematicDefinition currentDefinition;
        private PlayableDirector currentDirector;
        private CinemachineBrain activeBrain;
        private CinemachineBlendDefinition savedBlend;
        private BodyOwnership.Token bodyToken;
        private ControlLock.Handle controlHandle;
        private bool hudCaptured;
        private bool hudWasActive;
        private bool isPlaying;
        private bool[] fired = Array.Empty<bool>();
        private CinematicContext context;

        /// <summary>True between a successful <see cref="Play"/> and the beat's end.</summary>
        public bool IsPlaying => isPlaying;

        /// <summary>
        /// Raised ONCE when a beat ends naturally. Not raised by <see cref="Stop"/>:
        /// a debug jump must not drive the flow forward.
        /// </summary>
        public event Action<CinematicDefinition> Completed;

        private void Awake()
        {
            ResolveReferences();
        }

        private void OnDisable()
        {
            // A disabled director cannot finish its beat; release without completing it.
            if (!isPlaying) return;
            if (currentDirector != null) currentDirector.stopped -= HandleStopped;
            ReleaseEverything();
        }

        private void Update()
        {
            if (!isPlaying || currentDirector == null || currentDefinition == null) return;

            CinematicEvent[] events = currentDefinition.Events;
            if (events == null) return;

            // The director clock is the single timer: GameTime advances only while the
            // beat plays, so pausing the beat freezes events for free (no wall clock,
            // no coroutines). Array order is the authoring order for same-time events.
            float t = (float)currentDirector.time;
            for (int i = 0; i < events.Length && i < fired.Length; i++)
            {
                if (fired[i] || events[i].time > t) continue;
                fired[i] = true;

                ICinematicAction action = events[i].action as ICinematicAction;
                if (action == null)
                {
                    Debug.LogWarning(
                        $"[{nameof(CinematicDirector)}] Event {i} on '{currentDefinition.name}' has an action that does not implement {nameof(ICinematicAction)}; skipping.",
                        this);
                    continue;
                }

                action.Execute(context);
            }
        }

        /// <summary>
        /// Plays <paramref name="definition"/> if the director is idle. Returns false —
        /// and leaves the player in control — when already playing, when the definition
        /// or its timeline is missing, or when the matching binding/asset is invalid.
        /// </summary>
        public bool Play(CinematicDefinition definition)
        {
            if (isPlaying) return false;

            if (definition == null || definition.Timeline == null)
            {
                Debug.LogError($"[{nameof(CinematicDirector)}] Play rejected: definition or timeline is null.", this);
                return false;
            }

            if (!TryResolveDirector(definition, out PlayableDirector director)) return false;

            ResolveReferences();

            CinemachineBrain brain = ResolveBrain();
            if (brain == null)
            {
                Debug.LogError($"[{nameof(CinematicDirector)}] Play rejected: no {nameof(CinemachineBrain)} in the scene.", this);
                return false;
            }

            // A track with no binding would just leave the gameplay camera live during the
            // beat; re-binding is harmless when the asset was authored correctly.
            BindCameraTracks(director, brain);

            currentDefinition = definition;
            currentDirector = director;
            activeBrain = brain;
            savedBlend = brain.DefaultBlend;

            // Per-play state: every event re-arms, and the context is built ONCE so all
            // actions of this beat see the same references.
            CinematicEvent[] events = definition.Events;
            fired = events != null ? new bool[events.Length] : Array.Empty<bool>();
            context = new CinematicContext(definition, GameFlowController.Instance, motor, cameras);

            // Authored shots are cuts, not camera moves.
            brain.DefaultBlend = new CinemachineBlendDefinition
            {
                Style = CinemachineBlendDefinition.Styles.Cut,
                Time = 0f,
            };

            director.time = 0d;
            director.timeUpdateMode = DirectorUpdateMode.GameTime;

            // Without this the director HOLDS the last frame forever and `stopped` never
            // fires, so the beat would never hand control back.
            director.extrapolationMode = DirectorWrapMode.None;

            if ((definition.Locks & CinematicLockFlags.Control) != 0)
            {
                if (motor != null)
                {
                    bodyToken = motor.Body.Acquire(BodyPriority.Cinematic);
                    motor.FlushJumpBuffer();
                }

                controlHandle = ControlLock.Acquire(ControlReason.Cinematic);
            }

            // The beat owns the presentation from here: stop the motor from pushing the
            // held move input into the Animator, then give the pose to the raw-clip actor.
            // MotionFrozen also zeroes the preserved velocity, so the body cannot keep
            // sliding while the pose is frozen.
            if (motor != null)
            {
                motor.VisualSuppressed = true;
                motor.MotionFrozen = true;
            }
            if (playerActor != null) playerActor.Play(definition.ActorClip);

            if ((definition.Locks & CinematicLockFlags.Ai) != 0)
            {
                FreezeEnemies();
            }

            if (definition.HideHud && hudRoot != null)
            {
                hudWasActive = hudRoot.activeSelf;
                hudCaptured = true;
                hudRoot.SetActive(false);
            }

            if ((definition.Locks & CinematicLockFlags.Invulnerable) != 0 && playerDamage != null)
            {
                playerDamage.GrantInvulnerability((float)definition.Timeline.duration + 0.25f);
            }

            director.stopped -= HandleStopped; // never double-subscribe
            director.stopped += HandleStopped;
            isPlaying = true;
            director.Play();
            return true;
        }

        /// <summary>
        /// Stops the current beat and releases every lock WITHOUT firing <see cref="Completed"/>.
        /// Idempotent: calling it when nothing plays does nothing.
        /// </summary>
        public void Stop()
        {
            if (!isPlaying) return;

            PlayableDirector director = currentDirector;
            if (director != null)
            {
                director.stopped -= HandleStopped; // our own stop must not look natural
                if (director.playableAsset != null) director.Stop();
            }

            ReleaseEverything();
        }

        private void ResolveReferences()
        {
            if (cameras == null) cameras = CameraDirector.Instance;
            if (motor == null) motor = FindFirstObjectByType<PlayerMotor>();
            if (playerActor == null && motor != null) playerActor = motor.GetComponentInChildren<CinematicActor>(true);
            if (playerDamage == null) playerDamage = FindFirstObjectByType<PlayerDamage>();
        }

        private CinemachineBrain ResolveBrain()
        {
            if (cameras != null)
            {
                CinemachineBrain brain = cameras.Brain;
                if (brain != null) return brain;
            }

            return FindFirstObjectByType<CinemachineBrain>();
        }

        private bool TryResolveDirector(CinematicDefinition definition, out PlayableDirector director)
        {
            director = null;

            if (bindings != null)
            {
                for (int i = 0; i < bindings.Length; i++)
                {
                    if (bindings[i].definition != definition) continue;
                    if (bindings[i].director == null) continue;

                    if (bindings[i].director.playableAsset != definition.Timeline)
                    {
                        Debug.LogError(
                            $"[{nameof(CinematicDirector)}] Binding for '{definition.name}' has a mismatched playable asset; expected its timeline.",
                            this);
                        return false;
                    }

                    director = bindings[i].director;
                    return true;
                }
            }

            Debug.LogError($"[{nameof(CinematicDirector)}] No bound PlayableDirector for '{definition.name}'.", this);
            return false;
        }

        private static void BindCameraTracks(PlayableDirector director, CinemachineBrain brain)
        {
            PlayableAsset asset = director.playableAsset;
            if (asset == null) return;

            foreach (PlayableBinding output in asset.outputs)
            {
                if (output.sourceObject is CinemachineTrack)
                {
                    director.SetGenericBinding(output.sourceObject, brain);
                }
            }
        }

        private void FreezeEnemies()
        {
            frozenEnemies.Clear();
            frozenStates.Clear();

            EnemyController[] enemies = FindObjectsByType<EnemyController>(
                FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            for (int i = 0; i < enemies.Length; i++)
            {
                frozenEnemies.Add(enemies[i]);
                frozenStates.Add(enemies[i].Frozen);
                enemies[i].Frozen = true;
            }

            // The boss is not an EnemyController, so it needs its own pass. Without it a
            // boss beat would freeze only the minions while the brute kept charging.
            frozenBosses.Clear();
            frozenBossStates.Clear();

            BossController[] bosses = FindObjectsByType<BossController>(
                FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            for (int i = 0; i < bosses.Length; i++)
            {
                frozenBosses.Add(bosses[i]);
                frozenBossStates.Add(bosses[i].Frozen);
                bosses[i].Frozen = true;
            }
        }

        private void HandleStopped(PlayableDirector director)
        {
            if (!isPlaying || director != currentDirector) return;

            CinematicDefinition completed = currentDefinition;
            ReleaseEverything();
            Completed?.Invoke(completed);
        }

        /// <summary>
        /// Single release path. Safe to call twice: every step is null-guarded and the
        /// lock APIs accept already-released handles.
        /// </summary>
        private void ReleaseEverything()
        {
            if (currentDirector != null)
            {
                currentDirector.stopped -= HandleStopped;
            }

            if (activeBrain != null)
            {
                activeBrain.DefaultBlend = savedBlend;
                activeBrain = null;
            }

            for (int i = 0; i < frozenEnemies.Count; i++)
            {
                if (frozenEnemies[i] != null) frozenEnemies[i].Frozen = frozenStates[i];
            }
            frozenEnemies.Clear();
            frozenStates.Clear();

            for (int i = 0; i < frozenBosses.Count; i++)
            {
                if (frozenBosses[i] != null) frozenBosses[i].Frozen = frozenBossStates[i];
            }
            frozenBosses.Clear();
            frozenBossStates.Clear();

            if (hudCaptured && hudRoot != null) hudRoot.SetActive(hudWasActive);
            hudCaptured = false;

            // Return the presentation before the body so the first unlocked frame pushes
            // locomotion again (and an interrupted beat can never freeze the visual).
            if (playerActor != null) playerActor.Stop();
            if (motor != null)
            {
                motor.VisualSuppressed = false;
                motor.MotionFrozen = false;
            }

            if (motor != null && bodyToken != null)
            {
                motor.Body.Release(bodyToken);
                motor.FlushJumpBuffer();
            }
            bodyToken = null;

            ControlLock.Release(controlHandle);
            controlHandle = null;

            if (cameras != null) cameras.Reevaluate();

            currentDefinition = null;
            currentDirector = null;
            isPlaying = false;
            fired = Array.Empty<bool>();
        }
    }
}
