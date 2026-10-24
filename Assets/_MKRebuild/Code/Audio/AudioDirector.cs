using System.Collections;
using UnityEngine;

namespace MK.Rebuild
{
    /// <summary>
    /// Scene-owned audio entry point (m3-build-feedback). Not attached to the Player
    /// or the camera: 3D impact cues must keep their world position instead of
    /// following an actor. Voices are pooled AudioSources; callers only pick a cue
    /// and a position. Ambience and music are separate, data-driven 2D layers.
    /// This is also the central volume API (master + per-layer), so every caller
    /// goes through one place.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class AudioDirector : MonoBehaviour
    {
        public static AudioDirector Instance { get; private set; }

        [SerializeField] private AudioCatalog catalog;
        [SerializeField] private int voices = 8;
        [Range(0f, 1f)] [SerializeField] private float masterVolume = 1f;
        [Range(0f, 1f)] [SerializeField] private float sfxVolume = 1f;
        [Range(0f, 1f)] [SerializeField] private float ambienceVolume = 1f;
        [Range(0f, 1f)] [SerializeField] private float musicVolume = 1f;
        [SerializeField] private AmbienceState initialAmbience = AmbienceState.Parkour;
        [SerializeField] private MusicState initialMusic = MusicState.Parkour;

        private AudioSource[] sources;
        private int nextVoice;

        private AudioSource ambienceSource;
        private AudioSource ambienceOneShotSource;
        private Coroutine ambienceRoutine;
        private Coroutine ambienceOneShotRoutine;
        private AmbienceState currentAmbience;
        private float currentAmbienceBaseVolume;

        private AudioSource musicSource;
        private Coroutine musicRoutine;
        private MusicState currentMusic;
        private float currentMusicBaseVolume;

        public static AmbienceState CurrentAmbience =>
            Instance != null ? Instance.currentAmbience : AmbienceState.None;

        public static MusicState CurrentMusic =>
            Instance != null ? Instance.currentMusic : MusicState.None;

        /// <summary>Overall gain applied to every layer (0..1).</summary>
        public static float MasterVolume
        {
            get => Instance != null ? Instance.masterVolume : 1f;
            set
            {
                if (Instance == null) return;
                Instance.masterVolume = Mathf.Clamp01(value);
                Instance.ApplyActiveVolumes();
            }
        }

        public static float SfxVolume
        {
            get => Instance != null ? Instance.sfxVolume : 1f;
            set
            {
                if (Instance == null) return;
                Instance.sfxVolume = Mathf.Clamp01(value);
                Instance.ApplyActiveVolumes();
            }
        }

        public static float AmbienceVolume
        {
            get => Instance != null ? Instance.ambienceVolume : 1f;
            set
            {
                if (Instance == null) return;
                Instance.ambienceVolume = Mathf.Clamp01(value);
                Instance.ApplyActiveVolumes();
            }
        }

        public static float MusicVolume
        {
            get => Instance != null ? Instance.musicVolume : 1f;
            set
            {
                if (Instance == null) return;
                Instance.musicVolume = Mathf.Clamp01(value);
                Instance.ApplyActiveVolumes();
            }
        }

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

            ambienceSource = Create2DSource("Ambience");
            ambienceSource.loop = true;

            ambienceOneShotSource = Create2DSource("AmbienceOneShot");

            musicSource = Create2DSource("Music");
            musicSource.loop = true;
        }

        private void Start()
        {
            if (Instance != this) return;
            SetAmbience(initialAmbience);
            SetMusic(initialMusic);
        }

        private void OnDestroy()
        {
            StopAllCoroutines();
            ambienceRoutine = null;
            ambienceOneShotRoutine = null;
            musicRoutine = null;

            if (Instance == this) Instance = null;
        }

        private AudioSource Create2DSource(string sourceName)
        {
            var child = new GameObject(sourceName);
            child.transform.SetParent(transform, false);

            var source = child.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 0f; // beds and UI are never positional
            return source;
        }

        /// <summary>
        /// Re-applies the currently playing bed/music with their stored base
        /// volumes after a volume slider change. One-shot SFX already in flight
        /// keep the volume they were played with.
        /// </summary>
        private void ApplyActiveVolumes()
        {
            if (ambienceSource != null)
            {
                ambienceSource.volume =
                    Mathf.Clamp01(currentAmbienceBaseVolume * ambienceVolume * masterVolume);
            }

            if (musicSource != null)
            {
                musicSource.volume =
                    Mathf.Clamp01(currentMusicBaseVolume * musicVolume * masterVolume);
            }
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

        /// <summary>Switches the ambience bed (crossfade, unscaled time).</summary>
        public static void SetAmbience(AmbienceState state)
        {
            if (Instance != null) Instance.SetAmbienceInternal(state);
        }

        /// <summary>Switches the music bed (crossfade, unscaled time).</summary>
        public static void SetMusic(MusicState state)
        {
            if (Instance != null) Instance.SetMusicInternal(state);
        }

        private void SetAmbienceInternal(AmbienceState state)
        {
            if (state == currentAmbience &&
                ambienceSource != null && ambienceSource.isPlaying)
            {
                return;
            }

            if (ambienceOneShotRoutine != null)
            {
                StopCoroutine(ambienceOneShotRoutine);
                ambienceOneShotRoutine = null;
            }

            if (ambienceRoutine != null)
            {
                StopCoroutine(ambienceRoutine);
                ambienceRoutine = null;
            }

            ambienceRoutine = StartCoroutine(CrossfadeAmbience(state));
        }

        /// <summary>
        /// Fades the current bed out, then the new one in. Unscaled time so hitstop
        /// (timeScale dips) never freezes the ambience.
        /// </summary>
        private IEnumerator CrossfadeAmbience(AmbienceState state)
        {
            float fadeSeconds = catalog != null ? catalog.GetAmbience(state).fadeSeconds : 0f;

            if (ambienceSource != null)
            {
                float from = ambienceSource.volume;

                if (fadeSeconds <= 0.01f)
                {
                    ambienceSource.volume = 0f;
                }
                else
                {
                    float t = 0f;
                    while (t < fadeSeconds)
                    {
                        t += Time.unscaledDeltaTime;
                        if (ambienceSource == null) break;
                        ambienceSource.volume = Mathf.Lerp(from, 0f, t / fadeSeconds);
                        yield return null;
                    }
                    if (ambienceSource != null) ambienceSource.volume = 0f;
                }

                ambienceSource.Stop();
            }

            if (catalog == null)
            {
                currentAmbience = state;
                currentAmbienceBaseVolume = 0f;
                ambienceRoutine = null;
                yield break;
            }

            AudioCatalog.AmbienceCue cue = catalog.GetAmbience(state);

            // A cue with no loop is intentional silence for that state.
            if (cue.loop == null || ambienceSource == null)
            {
                currentAmbience = state;
                currentAmbienceBaseVolume = 0f;
                ambienceRoutine = null;
                yield break;
            }

            currentAmbienceBaseVolume = cue.volume;
            ambienceSource.clip = cue.loop;
            ambienceSource.loop = true;
            ambienceSource.volume = 0f;
            ambienceSource.Play();

            float target = Mathf.Clamp01(cue.volume * ambienceVolume * masterVolume);
            if (cue.fadeSeconds <= 0.01f)
            {
                ambienceSource.volume = target;
            }
            else
            {
                float t = 0f;
                while (t < cue.fadeSeconds)
                {
                    t += Time.unscaledDeltaTime;
                    if (ambienceSource == null) break;
                    ambienceSource.volume = Mathf.Lerp(0f, target, t / cue.fadeSeconds);
                    yield return null;
                }
                if (ambienceSource != null) ambienceSource.volume = target;
            }

            currentAmbience = state;

            if (cue.oneShots != null && cue.oneShots.Length > 0 &&
                cue.oneShotMaxInterval > 0f && ambienceOneShotSource != null)
            {
                ambienceOneShotRoutine = StartCoroutine(OneShotLoop(state, cue));
            }

            ambienceRoutine = null;
        }

        private IEnumerator OneShotLoop(AmbienceState state, AudioCatalog.AmbienceCue cue)
        {
            float min = Mathf.Max(0f, cue.oneShotMinInterval);
            float max = Mathf.Max(0f, cue.oneShotMaxInterval);
            if (max < min)
            {
                float swap = min;
                min = max;
                max = swap;
            }

            while (true)
            {
                yield return new WaitForSecondsRealtime(Random.Range(min, max));

                if (currentAmbience != state) yield break;
                if (ambienceOneShotSource == null) yield break;

                AudioClip clip = PickClip(cue.oneShots);
                if (clip != null)
                {
                    ambienceOneShotSource.PlayOneShot(
                        clip,
                        Mathf.Clamp01(cue.oneShotVolume * ambienceVolume * masterVolume));
                }
            }
        }

        private void SetMusicInternal(MusicState state)
        {
            if (state == currentMusic && musicSource != null && musicSource.isPlaying)
            {
                return;
            }

            if (musicRoutine != null)
            {
                StopCoroutine(musicRoutine);
                musicRoutine = null;
            }

            musicRoutine = StartCoroutine(CrossfadeMusic(state));
        }

        /// <summary>
        /// Fades the current track out, then plays an optional intro before the
        /// loop. Unscaled time so hitstop/pause never freezes the music.
        /// </summary>
        private IEnumerator CrossfadeMusic(MusicState state)
        {
            float fadeSeconds = catalog != null ? catalog.GetMusic(state).fadeSeconds : 0f;

            if (musicSource != null)
            {
                float from = musicSource.volume;

                if (fadeSeconds <= 0.01f)
                {
                    musicSource.volume = 0f;
                }
                else
                {
                    float t = 0f;
                    while (t < fadeSeconds)
                    {
                        t += Time.unscaledDeltaTime;
                        if (musicSource == null) break;
                        musicSource.volume = Mathf.Lerp(from, 0f, t / fadeSeconds);
                        yield return null;
                    }
                    if (musicSource != null) musicSource.volume = 0f;
                }

                musicSource.Stop();
            }

            if (catalog == null)
            {
                currentMusic = state;
                currentMusicBaseVolume = 0f;
                musicRoutine = null;
                yield break;
            }

            AudioCatalog.MusicCue cue = catalog.GetMusic(state);

            // A cue without a loop is intentional silence for that state.
            if (cue.loop == null || musicSource == null)
            {
                currentMusic = state;
                currentMusicBaseVolume = 0f;
                musicRoutine = null;
                yield break;
            }

            currentMusic = state;
            currentMusicBaseVolume = cue.volume;
            float target = Mathf.Clamp01(cue.volume * musicVolume * masterVolume);

            if (cue.intro != null)
            {
                musicSource.clip = cue.intro;
                musicSource.loop = false;
                musicSource.volume = target;
                musicSource.Play();

                yield return new WaitForSecondsRealtime(cue.intro.length);

                if (currentMusic != state || musicSource == null)
                {
                    musicRoutine = null;
                    yield break;
                }
            }

            musicSource.clip = cue.loop;
            musicSource.loop = true;
            musicSource.volume = 0f;
            musicSource.Play();

            if (cue.fadeSeconds <= 0.01f)
            {
                musicSource.volume = target;
            }
            else
            {
                float t = 0f;
                while (t < cue.fadeSeconds)
                {
                    t += Time.unscaledDeltaTime;
                    if (musicSource == null) break;
                    musicSource.volume = Mathf.Lerp(0f, target, t / cue.fadeSeconds);
                    yield return null;
                }
                if (musicSource != null) musicSource.volume = target;
            }

            currentMusic = state;
            musicRoutine = null;
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

            source.PlayOneShot(clip, Mathf.Clamp01(cue.volume * sfxVolume * masterVolume));
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
