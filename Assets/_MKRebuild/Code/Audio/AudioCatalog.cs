using System;
using UnityEngine;

namespace MK.Rebuild
{
    public enum AudioCueId
    {
        JumpVoice = 0, JumpWhoosh = 1, Land = 2, Dash = 3, Footstep = 4,
        LightSwing = 5, LightVoice = 6, LightHit = 7,
        HeavySwing = 8, HeavyVoice = 9, HeavyHit = 10,
        Hurt = 11, Death = 12, ProjectileFire = 13, ProjectileImpact = 14, AreaCast = 15,
    }

    /// <summary>
    /// Ambient bed selector. Data-driven: the actual loops/one-shots live in the
    /// catalog asset, so re-skinning a place means editing the asset, not code.
    /// </summary>
    public enum AmbienceState
    {
        None = 0,
        Parkour = 1,
        Combat = 2,
        City = 3,
        Menu = 4,
    }

    /// <summary>
    /// Music bed selector. The tracks are placeholders: swapping a loop means
    /// editing the catalog asset, never the code.
    /// </summary>
    public enum MusicState
    {
        None = 0,
        Parkour = 1,
        Combat = 2,
        City = 3,
        Menu = 4,
        Boss = 5,
    }

    [CreateAssetMenu(fileName = "AudioCatalog", menuName = "MK/Rebuild/AudioCatalog")]
    public sealed class AudioCatalog : ScriptableObject
    {
        [Serializable]
        public struct Cue
        {
            public AudioClip[] clips;
            [Range(0f, 1f)] public float volume;
            public float minPitch;
            public float maxPitch;
            public bool spatial;
        }

        /// <summary>
        /// One music bed: an optional one-shot intro followed by the main loop.
        /// A cue without a loop is intentional silence for that state.
        /// </summary>
        [Serializable]
        public struct MusicCue
        {
            public AudioClip intro;
            public AudioClip loop;
            [Range(0f, 1f)] public float volume;
            public float fadeSeconds;
        }

        /// <summary>
        /// One ambience bed: a looping base plus optional random one-shots
        /// (thunder, birds, ...). Placeholder clips live only in the asset.
        /// </summary>
        [Serializable]
        public struct AmbienceCue
        {
            public AudioClip loop;
            [Range(0f, 1f)] public float volume;
            public float fadeSeconds;
            public AudioClip[] oneShots;
            [Range(0f, 1f)] public float oneShotVolume;
            public float oneShotMinInterval;
            public float oneShotMaxInterval;
        }

        [SerializeField] private Cue[] cues = new Cue[16];
        [SerializeField] private AmbienceCue[] ambience = new AmbienceCue[5];
        [SerializeField] private MusicCue[] music = new MusicCue[6];

        public Cue Get(AudioCueId id)
        {
            int i = (int)id;
            if (cues == null || i < 0 || i >= cues.Length) return default;
            return cues[i];
        }

        public AmbienceCue GetAmbience(AmbienceState state)
        {
            int i = (int)state;
            if (ambience == null || i < 0 || i >= ambience.Length) return default;
            return ambience[i];
        }

        public MusicCue GetMusic(MusicState state)
        {
            int i = (int)state;
            if (music == null || i < 0 || i >= music.Length) return default;
            return music[i];
        }
    }
}
