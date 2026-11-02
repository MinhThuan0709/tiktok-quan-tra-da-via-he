using UnityEngine;

namespace TikTokFactionWar
{
    /// <summary>
    /// Procedural PCM synthesized audio floor with 5ms fade-in/out envelopes and static gain layer (MusicVol / SFXVol).
    /// Ensures zero external audio asset dependency and immediate tactile feedback (<100ms).
    /// </summary>
    public class GameAudio : MonoBehaviour
    {
        public static GameAudio Instance { get; private set; }

        [Range(0f, 1f)] public float musicVol = 0.55f;
        [Range(0f, 1f)] public float sfxVol = 0.85f;

        private AudioSource _ambientSource;
        private AudioSource _sfxSource;
        private AudioSource _milestoneSource;

        private AudioClip _spawnBlipClip;
        private AudioClip _impactClip;
        private AudioClip _giftStingClip;
        private AudioClip _nukeSirenClip;
        private AudioClip _uiClickClip;
        private AudioClip _ambientBedClip;

        private float _musicDuckMultiplier = 1f;

        public float MusicVol
        {
            get => musicVol;
            set
            {
                musicVol = Mathf.Clamp01(value);
                ApplyGainLayer();
            }
        }

        public float SFXVol
        {
            get => sfxVol;
            set
            {
                sfxVol = Mathf.Clamp01(value);
                ApplyGainLayer();
            }
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                if (Application.isPlaying) Destroy(gameObject);
                return;
            }
            EnsureInitialized();
        }

        public void EnsureInitialized()
        {
            Instance = this;
            if (_spawnBlipClip != null) return;

            SynthesizeAllClips();
            SetupSources();
            ApplyGainLayer();
        }

        private void SynthesizeAllClips()
        {
            const int sampleRate = 44100;

            // 1. Spawn / Pickup Blip: 880 -> 1320 Hz over 90ms with fast decay
            _spawnBlipClip = CreateSynthClip("SpawnBlip", 0.09f, sampleRate, (t, dur) =>
            {
                float norm = t / dur;
                float freq = Mathf.Lerp(880f, 1320f, norm);
                float env = (1f - norm) * (1f - norm);
                return Mathf.Sin(2f * Mathf.PI * freq * t) * env * 0.35f;
            });

            // 2. Impact Hit: low-frequency punch 180 -> 55 Hz over 80ms
            _impactClip = CreateSynthClip("ImpactHit", 0.08f, sampleRate, (t, dur) =>
            {
                float norm = t / dur;
                float freq = Mathf.Lerp(180f, 55f, norm);
                float env = 1f - norm;
                float tone = Mathf.Sin(2f * Mathf.PI * freq * t);
                return tone * env * 0.42f;
            });

            // 3. Milestone / Gift Sting: 3 ascending triad notes (523.25 / 659.25 / 783.99 Hz), 120ms each
            _giftStingClip = CreateSynthClip("GiftSting", 0.40f, sampleRate, (t, dur) =>
            {
                float sample = 0f;
                float[] notes = { 523.25f, 659.25f, 783.99f };
                for (int i = 0; i < notes.Length; i++)
                {
                    float start = i * 0.11f;
                    float localT = t - start;
                    if (localT >= 0f && localT <= 0.16f)
                    {
                        float env = 1f - (localT / 0.16f);
                        float wave = Mathf.Sin(2f * Mathf.PI * notes[i] * localT) +
                                     0.25f * Mathf.Sin(4f * Mathf.PI * notes[i] * localT);
                        sample += wave * env * 0.3f;
                    }
                }
                return Mathf.Clamp(sample, -0.9f, 0.9f);
            });

            // 4. Nuke Siren + Boom: 0.75s dramatic sweep
            _nukeSirenClip = CreateSynthClip("NukeSiren", 0.75f, sampleRate, (t, dur) =>
            {
                float norm = t / dur;
                float sirenFreq = Mathf.Lerp(440f, 880f, Mathf.PingPong(norm * 4f, 1f));
                float subBoom = Mathf.Sin(2f * Mathf.PI * Mathf.Lerp(120f, 38f, norm) * t) * (1f - norm * 0.5f);
                float siren = Mathf.Sin(2f * Mathf.PI * sirenFreq * t) * 0.35f;
                return Mathf.Clamp((siren + subBoom * 0.55f), -0.95f, 0.95f);
            });

            // 5. UI Click: 20ms filtered burst
            _uiClickClip = CreateSynthClip("UiClick", 0.025f, sampleRate, (t, dur) =>
            {
                float norm = t / dur;
                return Mathf.Sin(2f * Mathf.PI * 1600f * t) * (1f - norm) * 0.35f;
            });

            // 6. Ambient Bed: 4s loop of warm low-passed brown noise + gentle drone chord
            _ambientBedClip = CreateAmbientBedClip(4.0f, sampleRate);
        }

        private static AudioClip CreateSynthClip(string clipName, float duration, int sampleRate, System.Func<float, float, float> generator)
        {
            int samples = Mathf.Max(64, Mathf.RoundToInt(duration * sampleRate));
            float[] data = new float[samples];
            int fadeSamples = Mathf.Min(samples / 4, Mathf.RoundToInt(0.005f * sampleRate)); // 5ms fade

            for (int i = 0; i < samples; i++)
            {
                float t = i / (float)sampleRate;
                float val = generator(t, duration);

                // 5ms fade-in/out envelope to prevent clicks
                float fade = 1f;
                if (i < fadeSamples) fade = i / (float)fadeSamples;
                else if (i > samples - fadeSamples) fade = (samples - i) / (float)fadeSamples;

                data[i] = Mathf.Clamp(val * fade, -1f, 1f);
            }

            AudioClip clip = AudioClip.Create(clipName, samples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static AudioClip CreateAmbientBedClip(float duration, int sampleRate)
        {
            int samples = Mathf.RoundToInt(duration * sampleRate);
            float[] data = new float[samples];
            int fadeSamples = Mathf.RoundToInt(0.02f * sampleRate);
            float brown = 0f;
            System.Random rng = new System.Random(1337);

            for (int i = 0; i < samples; i++)
            {
                float t = i / (float)sampleRate;
                float white = (float)(rng.NextDouble() * 2.0 - 1.0);
                brown = Mathf.Clamp(brown + white * 0.02f, -1f, 1f) * 0.995f;

                float swell = 0.5f + 0.5f * Mathf.Sin(2f * Mathf.PI * t / duration);
                float pad = (Mathf.Sin(2f * Mathf.PI * 110f * t) + 0.5f * Mathf.Sin(2f * Mathf.PI * 164.81f * t)) * 0.06f * swell;
                float val = brown * 0.08f + pad;

                float fade = 1f;
                if (i < fadeSamples) fade = i / (float)fadeSamples;
                else if (i > samples - fadeSamples) fade = (samples - i) / (float)fadeSamples;

                data[i] = val * fade;
            }

            AudioClip clip = AudioClip.Create("AmbientBed", samples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private void SetupSources()
        {
            _ambientSource = gameObject.AddComponent<AudioSource>();
            _ambientSource.clip = _ambientBedClip;
            _ambientSource.loop = true;
            _ambientSource.spatialBlend = 0f;
            _ambientSource.playOnAwake = true;
            _ambientSource.Play();

            _sfxSource = gameObject.AddComponent<AudioSource>();
            _sfxSource.loop = false;
            _sfxSource.spatialBlend = 0f;
            _sfxSource.playOnAwake = false;

            _milestoneSource = gameObject.AddComponent<AudioSource>();
            _milestoneSource.loop = false;
            _milestoneSource.spatialBlend = 0f;
            _milestoneSource.playOnAwake = false;
        }

        public void SetPausedDuck(bool paused)
        {
            _musicDuckMultiplier = paused ? 0.5f : 1f; // -6dB ducking when paused
            ApplyGainLayer();
        }

        public void ApplyGainLayer()
        {
            if (_ambientSource != null)
                _ambientSource.volume = musicVol * _musicDuckMultiplier * 0.45f;
            if (_sfxSource != null)
                _sfxSource.volume = sfxVol;
            if (_milestoneSource != null)
                _milestoneSource.volume = sfxVol;
        }

        public void PlaySpawnBlip()
        {
            if (_sfxSource == null || _spawnBlipClip == null) return;
            _sfxSource.pitch = Random.Range(0.92f, 1.08f);
            _sfxSource.PlayOneShot(_spawnBlipClip, sfxVol * 0.65f);
        }

        public void PlayImpactHit()
        {
            if (_sfxSource == null || _impactClip == null) return;
            _sfxSource.pitch = Random.Range(0.90f, 1.10f);
            _sfxSource.PlayOneShot(_impactClip, sfxVol * 0.55f);
        }

        public void PlayGiftSting()
        {
            if (_milestoneSource == null || _giftStingClip == null) return;
            _milestoneSource.pitch = 1f;
            _milestoneSource.PlayOneShot(_giftStingClip, sfxVol);
        }

        public void PlayNukeSiren()
        {
            if (_milestoneSource == null || _nukeSirenClip == null) return;
            _milestoneSource.pitch = 1f;
            _milestoneSource.PlayOneShot(_nukeSirenClip, sfxVol);
        }

        public void PlayUiClick()
        {
            if (_sfxSource == null || _uiClickClip == null) return;
            _sfxSource.pitch = 1f;
            _sfxSource.PlayOneShot(_uiClickClip, sfxVol * 0.8f);
        }
    }
}
