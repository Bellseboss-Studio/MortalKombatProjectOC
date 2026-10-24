using System;
using UnityEngine;
using UnityEngine.Timeline;

namespace MK.Rebuild
{
    /// <summary>
    /// What a cinematic borrows from gameplay. Flags so a beat can opt out of any part
    /// (a short HUD-only beat keeps control, a gameplay reveal keeps the AI awake).
    /// </summary>
    [Flags]
    public enum CinematicLockFlags
    {
        /// <summary>Nothing is taken; the definition only plays its Timeline.</summary>
        None = 0,

        /// <summary>Player body token at <see cref="BodyPriority.Cinematic"/> + cinematic control reason.</summary>
        Control = 1,

        /// <summary>Every <see cref="EnemyController.Frozen"/> is raised for the beat and restored after.</summary>
        Ai = 2,

        /// <summary>Short grace window so a stray hit cannot interrupt the beat.</summary>
        Invulnerable = 4,
    }

    /// <summary>Who owns the outcome after the Timeline ends.</summary>
    public enum CinematicOnComplete
    {
        /// <summary>The beat returns to whatever the flow was doing before it (self-contained beat).</summary>
        ReturnToPrevious,

        /// <summary>The flow controller advances through its transition table.</summary>
        AdvanceFlow,
    }

    /// <summary>
    /// Data for one scripted beat: the authored Timeline plus the locks it needs.
    /// Plain data only — the Timeline is played by a pre-bound <see cref="UnityEngine.Playables.PlayableDirector"/>
    /// owned by <see cref="CinematicDirector"/>, because swapping <c>playableAsset</c> at
    /// runtime loses the exposed shot-camera references the Timeline needs.
    /// </summary>
    [CreateAssetMenu(menuName = "MK/Cinematic Definition", fileName = "Cinematic_New")]
    public sealed class CinematicDefinition : ScriptableObject
    {
        [Tooltip("Authored Timeline for this beat. Required: Play rejects a definition without one.")]
        [SerializeField] private TimelineAsset timeline;

        [Tooltip("Gameplay systems this beat borrows. Control/Ai/Invulnerable; every lock is released on stop.")]
        [SerializeField] private CinematicLockFlags locks = CinematicLockFlags.Control | CinematicLockFlags.Ai | CinematicLockFlags.Invulnerable;

        [Tooltip("Hide the whole HUD root for the beat and restore its previous active state afterwards.")]
        [SerializeField] private bool hideHud = true;

        [Tooltip("ReturnToPrevious = self-contained beat; AdvanceFlow = the flow controller takes over.")]
        [SerializeField] private CinematicOnComplete onComplete = CinematicOnComplete.AdvanceFlow;

        [Tooltip("Optional raw clip played on the player outside her Animator Controller while this beat runs. Null = freeze the current pose.")]
        [SerializeField] private AnimationClip actorClip;

        [Tooltip("Timed actions fired from the beat's own clock. Time 0 = as soon as the beat begins.")]
        [SerializeField] private CinematicEvent[] events = Array.Empty<CinematicEvent>();

        [Tooltip("Human-readable name used by debug overlays and validation logs.")]
        [SerializeField] private string debugLabel = string.Empty;

        /// <summary>Timeline this definition plays; also supplies the invulnerability duration.</summary>
        public TimelineAsset Timeline => timeline;

        /// <summary>
        /// Raw clip for <see cref="CinematicActor"/> to play outside the Animator Controller.
        /// Null means the actor freezes on its current pose instead.
        /// </summary>
        public AnimationClip ActorClip => actorClip;

        /// <summary>
        /// Timed actions for this beat, in array order. Fired against the director's
        /// playback clock, so a pause stops them automatically.
        /// </summary>
        public CinematicEvent[] Events => events;

        /// <summary>Gameplay systems borrowed for the duration of the beat.</summary>
        public CinematicLockFlags Locks => locks;

        /// <summary>True when the HUD root is hidden while the beat plays.</summary>
        public bool HideHud => hideHud;

        /// <summary>Who owns the outcome after the beat.</summary>
        public CinematicOnComplete OnComplete => onComplete;

        /// <summary>Short human-readable label for debug and validation.</summary>
        public string DebugLabel => debugLabel;
    }
}
