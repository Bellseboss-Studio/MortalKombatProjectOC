using UnityEngine;

namespace MK.Rebuild
{
    /// <summary>
    /// Scene-level owner of the music state. <see cref="AudioDirector"/> only plays
    /// what this component decides, so gameplay callers never juggle tracks
    /// directly. The catalog stays the single place where clips are swapped.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class MusicStateController : MonoBehaviour
    {
        [Tooltip("Music bed used outside combat.")]
        [SerializeField] private MusicState exploration = MusicState.Parkour;

        [Tooltip("Seconds the combat bed lingers after the last combat signal.")]
        [SerializeField] private float combatCooldown = 8f;

        private MusicState current;
        private float combatUntil;
        private bool debugOverride;

        private void Start()
        {
            Apply(exploration);
        }

        private void Update()
        {
            if (debugOverride || current != MusicState.Combat) return;
            if (Time.unscaledTime >= combatUntil) Apply(exploration);
        }

        /// <summary>
        /// Changes the non-combat bed. While combat is still lingering the change
        /// is remembered and applies when the combat bed expires.
        /// </summary>
        public void SetExploration(MusicState state)
        {
            debugOverride = false;
            exploration = state;
            if (current != MusicState.Combat) Apply(state);
        }

        /// <summary>Starts (or refreshes) the combat bed.</summary>
        public void NotifyCombat()
        {
            debugOverride = false;
            combatUntil = Time.unscaledTime + combatCooldown;
            Apply(MusicState.Combat);
        }

        /// <summary>Lingers the combat bed without restarting it (last enemy died).</summary>
        public void NotifyCombatEnd()
        {
            combatUntil = Time.unscaledTime + combatCooldown;
        }

        public void SetBoss()
        {
            debugOverride = false;
            Apply(MusicState.Boss);
        }

        /// <summary>
        /// Debug audition state (F9). It takes ownership until a gameplay call
        /// resets the override; there is no automatic revert.
        /// </summary>
        public void SetDebugState(MusicState state)
        {
            debugOverride = true;
            Apply(state);
        }

        private void Apply(MusicState state)
        {
            current = state;
            AudioDirector.SetMusic(state);
        }
    }
}
