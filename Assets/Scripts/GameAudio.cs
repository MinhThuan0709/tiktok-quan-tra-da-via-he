using System.Collections.Generic;
using UnityEngine;

namespace TikTokFactionWar
{
    /// <summary>Combat / motion sound effect library keys (all procedurally synthesized).</summary>
    public enum SfxId
    {
        ChuongLaunchSmall,
        ChuongLaunchBig,
        ChuongHit,
        CannonBoom,
        Slash,
        Zap,
        FlameBreath,
        Explosion,
        Shockwave,
        FootstepSoft,
        HoverHum,
        MechaStomp,
        WingFlap,
        SummonCharge,
        DragonRoar,
        ShieldUp,
        UnitDeath,
        BaseHit,
        NukeBlast,
        CritPing
    }

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

        private const int VoiceCount = 14;
        private AudioSource[] _voices;
        private int _voiceCursor;
        private readonly Dictionary<SfxId, AudioClip> _sfxLibrary = new Dictionary<SfxId, AudioClip>();
        private readonly float[] _sfxLastPlayed = new float[32];
        public int SfxPlayCount { get; private set; }

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

            // 7. Combat & motion SFX library
            SynthesizeSfxLibrary(sampleRate);
        }

        /// <summary>Tiny DSP toolbox handed to clip generators: noise, filters, phase-accurate oscillators.</summary>
        private sealed class Syn
        {
            public float Sr;
            private uint _seed = 2463534242u;
            private float _lp1, _lp2, _hpState, _phase, _phase2;

            public float Noise()
            {
                _seed = _seed * 1664525u + 1013904223u;
                return ((_seed >> 8) & 0xFFFF) / 32768f - 1f;
            }

            public float Lp(float x, float a)
            {
                _lp1 += a * (x - _lp1);
                return _lp1;
            }

            public float Lp2(float x, float a)
            {
                _lp1 += a * (x - _lp1);
                _lp2 += a * (_lp1 - _lp2);
                return _lp2;
            }

            public float Hp(float x, float a)
            {
                _hpState += a * (x - _hpState);
                return x - _hpState;
            }

            public float Sin(float freq)
            {
                _phase += freq / Sr;
                _phase -= Mathf.Floor(_phase);
                return Mathf.Sin(2f * Mathf.PI * _phase);
            }

            public float Sin2(float freq)
            {
                _phase2 += freq / Sr;
                _phase2 -= Mathf.Floor(_phase2);
                return Mathf.Sin(2f * Mathf.PI * _phase2);
            }

            public float Saw(float freq)
            {
                _phase += freq / Sr;
                _phase -= Mathf.Floor(_phase);
                return 2f * _phase - 1f;
            }

            public float Square(float freq)
            {
                _phase += freq / Sr;
                _phase -= Mathf.Floor(_phase);
                return _phase < 0.5f ? 1f : -1f;
            }
        }

        private static float Exp(float n, float k) => Mathf.Exp(-n * k);
        private static float Soft(float x) => (float)System.Math.Tanh(x * 1.25f) * 0.92f;

        private AudioClip Sfx(string name, float duration, int sampleRate, System.Func<float, float, Syn, float> gen)
        {
            Syn syn = new Syn { Sr = sampleRate };
            return CreateSynthClip(name, duration, sampleRate, (t, dur) => Soft(gen(t, t / dur, syn)));
        }

        private void SynthesizeSfxLibrary(int sr)
        {
            // Chuong luc (energy palm) launch - small: bright airy whoosh rising in pitch
            _sfxLibrary[SfxId.ChuongLaunchSmall] = Sfx("ChuongLaunchSmall", 0.22f, sr, (t, n, s) =>
            {
                float sweep = s.Sin(Mathf.Lerp(520f, 1500f, n * n));
                float air = s.Hp(s.Noise(), 0.25f) * 0.7f;
                float env = Mathf.Sin(Mathf.PI * Mathf.Pow(n, 0.6f)) * Exp(n, 1.5f);
                return (sweep * 0.32f + air * 0.35f) * env;
            });

            // Chuong luc launch - big: deep charged roar with rising harmonic sweep
            _sfxLibrary[SfxId.ChuongLaunchBig] = Sfx("ChuongLaunchBig", 0.45f, sr, (t, n, s) =>
            {
                float f = Mathf.Lerp(120f, 640f, n * n);
                float body = s.Saw(f) * 0.28f + s.Sin2(f * 2.01f) * 0.18f;
                float air = s.Lp(s.Noise(), 0.12f + 0.3f * n) * 0.6f;
                float env = Mathf.Sin(Mathf.PI * Mathf.Pow(n, 0.55f)) * Exp(n, 1.1f);
                return (body + air * 0.5f) * env * 1.3f;
            });

            // Chuong luc hit: crack + sub thump
            _sfxLibrary[SfxId.ChuongHit] = Sfx("ChuongHit", 0.32f, sr, (t, n, s) =>
            {
                float crack = s.Noise() * Exp(n, 11f) * 0.75f;
                float sub = s.Sin(Mathf.Lerp(120f, 42f, Mathf.Sqrt(n))) * Exp(n, 5.5f) * 0.9f;
                float click = t < 0.003f ? 0.8f : 0f;
                float zing = s.Sin2(1900f - n * 900f) * Exp(n, 16f) * 0.18f;
                return crack + sub + click + zing;
            });

            // Tank cannon: low boom, mid punch, noise tail
            _sfxLibrary[SfxId.CannonBoom] = Sfx("CannonBoom", 0.6f, sr, (t, n, s) =>
            {
                float boom = s.Sin(Mathf.Lerp(98f, 30f, Mathf.Sqrt(n))) * Exp(n, 3.2f) * 1.0f;
                float punch = s.Lp(s.Noise(), 0.35f) * Exp(n, 9f) * 0.8f;
                float tail = s.Lp2(s.Noise(), 0.04f) * Exp(n, 3.5f) * 1.3f;
                return boom + punch + tail * 0.5f;
            });

            // Slash: filtered noise swish + metallic ping
            _sfxLibrary[SfxId.Slash] = Sfx("Slash", 0.22f, sr, (t, n, s) =>
            {
                float swish = s.Hp(s.Noise(), 0.12f + 0.4f * n) * Mathf.Sin(Mathf.PI * Mathf.Pow(n, 0.7f)) * 0.8f;
                float ping = s.Sin(Mathf.Lerp(2600f, 1900f, n)) * Exp(n, 9f) * 0.18f;
                return swish * 0.7f + ping;
            });

            // Lightning zap: gated crackle + buzzing square
            _sfxLibrary[SfxId.Zap] = Sfx("Zap", 0.38f, sr, (t, n, s) =>
            {
                float gate = Mathf.Sin(t * 260f + s.Noise() * 3f) > -0.2f ? 1f : 0.25f;
                float crackle = s.Noise() * gate * Exp(n, 3.2f) * 0.7f;
                float buzz = s.Square(150f + 70f * Mathf.Sin(t * 80f)) * Exp(n, 4.5f) * 0.22f;
                float sizzle = s.Hp(s.Noise(), 0.4f) * Exp(n, 2f) * 0.25f;
                return crackle + buzz + sizzle;
            });

            // Dragon flame breath: roaring noise swell with crackle pops
            _sfxLibrary[SfxId.FlameBreath] = Sfx("FlameBreath", 0.75f, sr, (t, n, s) =>
            {
                float swell = Mathf.Pow(Mathf.Max(0f, Mathf.Sin(Mathf.PI * Mathf.Clamp01(n * 1.05f))), 0.7f);
                float roar = s.Lp2(s.Noise(), 0.16f + 0.12f * Mathf.Sin(t * 25f)) * 2.2f;
                float pops = (s.Noise() > 0.985f ? 1f : 0f) * 0.8f;
                float low = s.Sin(70f + 20f * Mathf.Sin(t * 14f)) * 0.25f;
                return (roar * 0.75f + low) * swell * 0.9f + pops * Exp(n, 2f) * 0.3f;
            });

            // Explosion: broadband burst darkening over time + sub boom
            _sfxLibrary[SfxId.Explosion] = Sfx("Explosion", 0.95f, sr, (t, n, s) =>
            {
                float a = Mathf.Lerp(0.55f, 0.025f, Mathf.Sqrt(n));
                float rumble = s.Lp2(s.Noise(), a) * Exp(n, 3.4f) * 2.4f;
                float sub = s.Sin(Mathf.Lerp(78f, 26f, Mathf.Sqrt(n))) * Exp(n, 3f) * 0.9f;
                float crack = t < 0.004f ? 0.9f : 0f;
                return rumble * 0.8f + sub + crack;
            });

            // Shockwave: sub drop + whooshing ring
            _sfxLibrary[SfxId.Shockwave] = Sfx("Shockwave", 0.65f, sr, (t, n, s) =>
            {
                float sub = s.Sin(Mathf.Lerp(86f, 28f, Mathf.Sqrt(n))) * Exp(n, 3f) * 0.95f;
                float whoosh = s.Lp2(s.Noise(), 0.03f + 0.18f * n) * Mathf.Sin(Mathf.PI * n) * 1.6f;
                return sub + whoosh * 0.55f;
            });

            // Footstep (infantry): soft thud
            _sfxLibrary[SfxId.FootstepSoft] = Sfx("FootstepSoft", 0.075f, sr, (t, n, s) =>
            {
                float thud = s.Lp(s.Noise(), 0.17f) * Exp(n, 8f) * 1.1f;
                float body = s.Sin(Mathf.Lerp(150f, 90f, n)) * Exp(n, 10f) * 0.5f;
                return thud * 0.6f + body;
            });

            // Hover hum (tank): vibrato whirr
            _sfxLibrary[SfxId.HoverHum] = Sfx("HoverHum", 0.22f, sr, (t, n, s) =>
            {
                float vib = 1f + 0.06f * Mathf.Sin(t * 60f);
                float tone = s.Saw(135f * vib) * 0.14f + s.Sin2(270f * vib) * 0.12f;
                float air = s.Lp(s.Noise(), 0.2f) * 0.2f;
                return (tone + air) * Mathf.Sin(Mathf.PI * n);
            });

            // Mecha stomp: heavy thud + metallic clank
            _sfxLibrary[SfxId.MechaStomp] = Sfx("MechaStomp", 0.38f, sr, (t, n, s) =>
            {
                float thud = s.Sin(Mathf.Lerp(66f, 30f, Mathf.Sqrt(n))) * Exp(n, 6.5f) * 1.05f;
                float clank = (Mathf.Sin(2f * Mathf.PI * 690f * t) + 0.7f * Mathf.Sin(2f * Mathf.PI * 1130f * t) + 0.5f * Mathf.Sin(2f * Mathf.PI * 1710f * t)) * Exp(n, 15f) * 0.17f;
                float dirt = s.Lp(s.Noise(), 0.22f) * Exp(n, 9f) * 0.7f;
                return thud + clank + dirt;
            });

            // Dragon wing flap: windy swell with low thump
            _sfxLibrary[SfxId.WingFlap] = Sfx("WingFlap", 0.26f, sr, (t, n, s) =>
            {
                float wind = s.Lp(s.Noise(), 0.28f) * Mathf.Pow(Mathf.Max(0f, Mathf.Sin(Mathf.PI * n)), 1.6f) * 0.9f;
                float thump = s.Sin(Mathf.Lerp(88f, 55f, n)) * Exp(n, 6f) * 0.55f;
                return wind * 0.7f + thump;
            });

            // Summon charge: rising shimmer ending in a pop
            _sfxLibrary[SfxId.SummonCharge] = Sfx("SummonCharge", 0.5f, sr, (t, n, s) =>
            {
                float f = Mathf.Lerp(180f, 1300f, n * n);
                float tone = s.Sin(f) * 0.3f + s.Sin2(f * 1.5f) * 0.16f;
                float shimmer = s.Hp(s.Noise(), 0.3f) * 0.18f * n;
                float pop = n > 0.88f ? Exp((n - 0.88f) * 8f, 6f) * s.Noise() * 0.6f : 0f;
                float env = Mathf.Pow(n, 1.2f) * (n > 0.9f ? Mathf.Clamp01((1f - n) / 0.1f) : 1f);
                return (tone + shimmer) * env * 1.2f + pop;
            });

            // Dragon roar: growling sawtooth with vibrato, noise breath
            _sfxLibrary[SfxId.DragonRoar] = Sfx("DragonRoar", 1.05f, sr, (t, n, s) =>
            {
                float f = 85f + 25f * Mathf.Sin(t * 44f) + 60f * Mathf.Sin(Mathf.PI * n) * 0.6f - 20f * n;
                float growl = s.Saw(f) * (0.55f + 0.45f * Mathf.Sin(t * 70f));
                float breath = s.Lp(s.Noise(), 0.25f) * 0.7f;
                float env = Mathf.Min(1f, n / 0.08f) * Exp(Mathf.Max(0f, n - 0.35f), 2.4f);
                return (growl * 0.45f + breath * 0.4f) * env * 1.2f;
            });

            // Shield up: shimmering high arpeggio
            _sfxLibrary[SfxId.ShieldUp] = Sfx("ShieldUp", 0.45f, sr, (t, n, s) =>
            {
                float[] notes = { 880f, 1318.5f, 1760f };
                float sum = 0f;
                for (int i = 0; i < notes.Length; i++)
                {
                    float lt = t - i * 0.07f;
                    if (lt < 0f) continue;
                    sum += (Mathf.Sin(2f * Mathf.PI * notes[i] * lt) + 0.3f * Mathf.Sin(4f * Mathf.PI * notes[i] * lt)) * Mathf.Exp(-lt * 9f) * 0.24f;
                }
                return sum + s.Hp(s.Noise(), 0.4f) * Exp(n, 8f) * 0.08f;
            });

            // Unit death: descending blip + noise pop
            _sfxLibrary[SfxId.UnitDeath] = Sfx("UnitDeath", 0.36f, sr, (t, n, s) =>
            {
                float blip = s.Square(Mathf.Lerp(620f, 90f, Mathf.Sqrt(n))) * Exp(n, 5f) * 0.22f;
                float pop = s.Lp(s.Noise(), 0.4f) * Exp(n, 12f) * 0.7f;
                float sub = s.Sin2(Mathf.Lerp(130f, 50f, n)) * Exp(n, 6f) * 0.55f;
                return blip + pop + sub;
            });

            // Base hit: inharmonic metal clang + boom
            _sfxLibrary[SfxId.BaseHit] = Sfx("BaseHit", 0.55f, sr, (t, n, s) =>
            {
                float clang = (Mathf.Sin(2f * Mathf.PI * 220f * t) + 0.8f * Mathf.Sin(2f * Mathf.PI * 331f * t) + 0.6f * Mathf.Sin(2f * Mathf.PI * 476f * t) + 0.4f * Mathf.Sin(2f * Mathf.PI * 811f * t)) * Exp(n, 5.5f) * 0.14f;
                float boom = s.Sin(Mathf.Lerp(74f, 34f, Mathf.Sqrt(n))) * Exp(n, 5f) * 0.9f;
                float crack = s.Noise() * Exp(n, 13f) * 0.6f;
                return clang + boom + crack;
            });

            // Nuke blast: massive rumbling explosion
            _sfxLibrary[SfxId.NukeBlast] = Sfx("NukeBlast", 1.7f, sr, (t, n, s) =>
            {
                float a = Mathf.Lerp(0.45f, 0.012f, Mathf.Sqrt(n));
                float rumble = s.Lp2(s.Noise(), a) * Exp(n, 1.9f) * 3.0f;
                float sub = s.Sin(Mathf.Lerp(62f, 20f, Mathf.Sqrt(n))) * Exp(n, 1.4f) * 1.1f;
                float shimmer = s.Hp(s.Noise(), 0.3f) * Exp(n, 6f) * 0.5f;
                float crack = t < 0.006f ? 1f : 0f;
                return rumble * 0.75f + sub + shimmer + crack;
            });

            // Crit ping: bright two-tone
            _sfxLibrary[SfxId.CritPing] = Sfx("CritPing", 0.26f, sr, (t, n, s) =>
            {
                return (Mathf.Sin(2f * Mathf.PI * 1760f * t) + 0.8f * Mathf.Sin(2f * Mathf.PI * 2349f * t)) * Exp(n, 7f) * 0.3f;
            });
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

            // Round-robin voice pool for combat SFX: per-voice pitch + stereo pan so overlapping hits stay distinct
            _voices = new AudioSource[VoiceCount];
            for (int i = 0; i < VoiceCount; i++)
            {
                AudioSource v = gameObject.AddComponent<AudioSource>();
                v.loop = false;
                v.spatialBlend = 0f;
                v.playOnAwake = false;
                _voices[i] = v;
            }
        }

        private static float MinInterval(SfxId id)
        {
            switch (id)
            {
                case SfxId.FootstepSoft: return 0.085f;
                case SfxId.HoverHum: return 0.30f;
                case SfxId.WingFlap: return 0.22f;
                case SfxId.MechaStomp: return 0.16f;
                case SfxId.ChuongLaunchSmall: return 0.055f;
                case SfxId.ChuongHit: return 0.045f;
                case SfxId.Slash: return 0.06f;
                case SfxId.ShieldUp: return 0.25f;
                case SfxId.UnitDeath: return 0.05f;
                case SfxId.Zap: return 0.09f;
                case SfxId.DragonRoar: return 0.9f;
                default: return 0.04f;
            }
        }

        /// <summary>
        /// Plays a synthesized combat/motion SFX on a pooled voice with random pitch jitter and stereo pan from world X.
        /// Per-sound cooldowns stop crowd battles from turning into noise.
        /// </summary>
        public void PlaySfx(SfxId id, float worldX = 0f, float volume = 1f, float pitchJitter = 0.09f)
        {
            if (_voices == null || !_sfxLibrary.TryGetValue(id, out AudioClip clip) || clip == null) return;

            float now = Time.realtimeSinceStartup;
            int idx = (int)id;
            if (now - _sfxLastPlayed[idx] < MinInterval(id)) return;
            _sfxLastPlayed[idx] = now;

            AudioSource v = _voices[_voiceCursor];
            _voiceCursor = (_voiceCursor + 1) % _voices.Length;
            v.Stop();
            v.clip = clip;
            v.panStereo = Mathf.Clamp(worldX / 3.8f, -0.85f, 0.85f);
            v.pitch = 1f + Random.Range(-pitchJitter, pitchJitter);
            v.volume = Mathf.Clamp01(sfxVol * volume);
            v.Play();
            SfxPlayCount++;
        }

        public bool HasSfx(SfxId id) => _sfxLibrary.ContainsKey(id) && _sfxLibrary[id] != null;
        public AudioClip GetSfxClip(SfxId id) => _sfxLibrary.TryGetValue(id, out AudioClip c) ? c : null;
        public int SfxLibraryCount => _sfxLibrary.Count;

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
