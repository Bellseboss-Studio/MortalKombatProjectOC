using UnityEngine;

namespace MK.Rebuild
{
    /// <summary>
    /// Scene-owned SFX player (m3-build-feedback). Not attached to the Player or the
    /// camera: 3D impact cues must keep their world position instead of following an
    /// actor. Voices are pooled AudioSources; callers only pick a cue and a position.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class AudioDirector : MonoBehaviour
    {
        public static AudioDirector Instance { get; private set; }

        [SerializeField] private AudioCatalog catalog;
        [SerializeField] private int voices = 8;
        [SerializeField] private float sfxVolume = 1f;

        private AudioSource[] sources;
        private int nextVoice;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;

            int count = Mathf.Max(1, voices);
            sources = new AudioSource[count];
            for (int i = 0; i < count; i++)
            {
                var voice = new GameObject($"Voice_{i:00}");
                voice.transform.SetParent(transform, false);

                var source = voice.AddComponent<AudioSource>();
                source.playOnAwake = false;
                source.spatialBlend = 1f;
                source.rolloffMode = AudioRolloffMode.Linear;
                source.minDistance = 2f;
                source.maxDistance = 30f;
                sources[i] = source;
            }
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        /// <summary>Plays a cue at a world position (3D if the cue is spatial).</summary>
        public static void Play(AudioCueId id, Vector3 position)
        {
            if (Instance != null) Instance.PlayInternal(id, position, false);
        }

        /// <summary>Plays a cue without spatial attenuation (UI, voice, whooshes).</summary>
        public static void Play2D(AudioCueId id)
        {
            if (Instance != null) Instance.PlayInternal(id, Instance.transform.position, true);
        }

        private void PlayInternal(AudioCueId id, Vector3 position, bool force2D)
        {
            if (catalog == null || sources == null || sources.Length == 0) return;

            AudioCatalog.Cue cue = catalog.Get(id);
            AudioClip clip = PickClip(cue.clips);
            if (clip == null) return;

            AudioSource source = sources[nextVoice];
            nextVoice = (nextVoice + 1) % sources.Length;
            if (source == null) return;

            source.transform.position = position;
            source.spatialBlend = cue.spatial && !force2D ? 1f : 0f;

            float minPitch = Mathf.Min(cue.minPitch, cue.maxPitch);
            float maxPitch = Mathf.Max(cue.minPitch, cue.maxPitch);
            source.pitch = Random.Range(minPitch, maxPitch);

            source.PlayOneShot(clip, Mathf.Clamp01(cue.volume * sfxVolume));
        }

        private static AudioClip PickClip(AudioClip[] clips)
        {
            if (clips == null || clips.Length == 0) return null;

            int start = Random.Range(0, clips.Length);
            for (int i = 0; i < clips.Length; i++)
            {
                AudioClip clip = clips[(start + i) % clips.Length];
                if (clip != null) return clip;
            }

            return null;
        }
    }
}
