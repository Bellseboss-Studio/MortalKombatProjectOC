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

        [SerializeField] private Cue[] cues = new Cue[16];

        public Cue Get(AudioCueId id)
        {
            int i = (int)id;
            if (cues == null || i < 0 || i >= cues.Length) return default;
            return cues[i];
        }
    }
}
