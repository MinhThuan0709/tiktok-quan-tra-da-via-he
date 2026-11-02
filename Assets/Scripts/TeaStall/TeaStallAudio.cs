using System.Collections.Generic;
using UnityEngine;

namespace TikTokTeaStall
{
    /// <summary>
    /// Thư viện âm thanh tổng hợp Procedural (44100 Hz) thuần C# sống động, 
    /// không phụ thuộc vào bất kỳ file wav/mp3 bên ngoài nào.
    /// </summary>
    public class TeaStallAudio : MonoBehaviour
    {
        private static TeaStallAudio _instance;
        public static TeaStallAudio Instance
        {
            get
            {
                if (_instance == null) _instance = UnityEngine.Object.FindAnyObjectByType<TeaStallAudio>();
                return _instance;
            }
            set => _instance = value;
        }

        private const int SampleRate = 44100;
        private AudioSource _sfxSource;
        private AudioSource _bgmSource;
        private AudioSource _keoKeoSource;

        private readonly Dictionary<string, AudioClip> _clipCache = new Dictionary<string, AudioClip>();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                if (Application.isPlaying) Destroy(gameObject);
                return;
            }
            Instance = this;
            InitializeAudioSources();
        }

        public void EnsureInitialized()
        {
            Instance = this;
            InitializeAudioSources();
        }

        private void InitializeAudioSources()
        {
            if (_sfxSource == null)
            {
                _sfxSource = gameObject.AddComponent<AudioSource>();
                _sfxSource.playOnAwake = false;
                _sfxSource.spatialBlend = 0f;
            }
            if (_bgmSource == null)
            {
                _bgmSource = gameObject.AddComponent<AudioSource>();
                _bgmSource.playOnAwake = false;
                _bgmSource.loop = true;
                _bgmSource.volume = 0.22f;
                _bgmSource.spatialBlend = 0f;
            }
            if (_keoKeoSource == null)
            {
                _keoKeoSource = gameObject.AddComponent<AudioSource>();
                _keoKeoSource.playOnAwake = false;
                _keoKeoSource.loop = true;
                _keoKeoSource.volume = 0.45f;
                _keoKeoSource.spatialBlend = 0f;
            }
        }

        private void Start()
        {
            StartAmbientStreetBgm();
        }

        // ==================== CÁC HÀM PHÁT ÂM THANH SỰ KIỆN ====================

        public void PlayTeaPour()
        {
            PlayClip("TeaPour", CreateTeaPourClip(), 0.55f);
        }

        public void PlaySunflowerCrack()
        {
            PlayClip("SunflowerCrack", CreateSunflowerCrackClip(), 0.65f);
        }

        public void PlayDieuCayRiiit()
        {
            PlayClip("DieuCayRiiit", CreateDieuCayRiiitClip(), 0.85f);
        }

        public void PlayChatPop()
        {
            PlayClip("ChatPop", CreateChatPopClip(), 0.40f);
        }

        public void PlayTitleUpgradeFanfare()
        {
            PlayClip("TitleFanfare", CreateTitleFanfareClip(), 0.90f);
        }

        public void PlayGrabTingTing()
        {
            PlayClip("GrabTingTing", CreateGrabTingTingClip(), 0.80f);
        }

        public void PlayBanhMiCry()
        {
            PlayClip("BanhMiCry", CreateBanhMiClip(), 0.75f);
        }

        public void PlayPoliceWhistle()
        {
            PlayClip("PoliceWhistle", CreatePoliceWhistleClip(), 0.85f);
        }

        public void PlayBaoQuanJackpot()
        {
            PlayClip("BaoQuanJackpot", CreateBaoQuanJackpotClip(), 1.0f);
        }

        public void StartKeoKeoDisco()
        {
            if (_keoKeoSource == null) return;
            if (!_clipCache.TryGetValue("DiscoBeat", out AudioClip discoClip))
            {
                discoClip = CreateDiscoBeatClip();
                _clipCache["DiscoBeat"] = discoClip;
            }
            _keoKeoSource.clip = discoClip;
            _keoKeoSource.Play();
        }

        public void StopKeoKeoDisco()
        {
            if (_keoKeoSource != null && _keoKeoSource.isPlaying)
            {
                _keoKeoSource.Stop();
            }
        }

        public void StartAmbientStreetBgm()
        {
            if (_bgmSource == null) return;
            if (!_clipCache.TryGetValue("StreetBgm", out AudioClip bgmClip))
            {
                bgmClip = CreateStreetLoFiBgmClip();
                _clipCache["StreetBgm"] = bgmClip;
            }
            _bgmSource.clip = bgmClip;
            _bgmSource.Play();
        }

        private void PlayClip(string key, AudioClip clip, float volume)
        {
            if (_sfxSource == null) InitializeAudioSources();
            _sfxSource.PlayOneShot(clip, volume);
        }

        // ==================== TỔNG HỢP PROCEDURAL SYNTHESIZER ====================

        private AudioClip CreateTeaPourClip()
        {
            // Âm thanh nước rót róc rách + cục đá va vào cốc thủy tinh
            float duration = 1.2f;
            int samples = (int)(SampleRate * duration);
            float[] data = new float[samples];

            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / SampleRate;
                // Tiếng nước chảy róc rách (FM noise lọc thông dải)
                float bubble = Mathf.Sin(2f * Mathf.PI * (350f + Mathf.Sin(t * 35f) * 120f) * t) * (Mathf.PerlinNoise(t * 20f, 0f) - 0.5f);
                // 3 tiếng lách cách đá lạnh rơi vào thành cốc thủy tinh
                float clink1 = (t > 0.15f && t < 0.25f) ? Mathf.Sin(2f * Mathf.PI * 2400f * (t - 0.15f)) * Mathf.Exp(-(t - 0.15f) * 40f) : 0f;
                float clink2 = (t > 0.45f && t < 0.55f) ? Mathf.Sin(2f * Mathf.PI * 2900f * (t - 0.45f)) * Mathf.Exp(-(t - 0.45f) * 45f) : 0f;
                float clink3 = (t > 0.80f && t < 0.90f) ? Mathf.Sin(2f * Mathf.PI * 3300f * (t - 0.80f)) * Mathf.Exp(-(t - 0.80f) * 50f) : 0f;

                float env = Mathf.Sin(t / duration * Mathf.PI);
                data[i] = (bubble * 0.4f + clink1 * 0.35f + clink2 * 0.35f + clink3 * 0.3f) * env;
            }

            AudioClip clip = AudioClip.Create("TeaPour", samples, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private AudioClip CreateSunflowerCrackClip()
        {
            // Tiếng cắn vỡ hạt hướng dương "rốp... tách!"
            float duration = 0.25f;
            int samples = (int)(SampleRate * duration);
            float[] data = new float[samples];

            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / SampleRate;
                float crack1 = (t < 0.08f) ? (UnityEngine.Random.value * 2f - 1f) * Mathf.Exp(-t * 80f) : 0f;
                float pop = (t > 0.04f && t < 0.18f) ? Mathf.Sin(2f * Mathf.PI * (1600f - (t - 0.04f) * 5000f) * (t - 0.04f)) * Mathf.Exp(-(t - 0.04f) * 40f) : 0f;
                data[i] = crack1 * 0.5f + pop * 0.5f;
            }

            AudioClip clip = AudioClip.Create("SunflowerCrack", samples, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private AudioClip CreateDieuCayRiiitClip()
        {
            // Tiếng rít điếu cày huyền thoại: Riiiiiiiiiit kéo dài... tiếng sục nước lọc sọc... rồi tiếng thở phào phà khói!
            float duration = 2.4f;
            int samples = (int)(SampleRate * duration);
            float[] data = new float[samples];

            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / SampleRate;
                float s = 0f;

                // Giai đoạn 1: Rít hơi thuốc qua nỏ điếu (0.0s -> 1.5s)
                if (t < 1.5f)
                {
                    float rFreq = 750f + t * 450f + Mathf.Sin(t * 120f) * 80f;
                    float whistle = Mathf.Sin(2f * Mathf.PI * rFreq * t);
                    // Nước lọc sọc sôi ùng ục trong ống tre
                    float waterGargle = Mathf.Sin(2f * Mathf.PI * 180f * t) * (Mathf.Sin(t * 45f) > 0.2f ? 1f : -0.5f);
                    float noise = (UnityEngine.Random.value * 2f - 1f) * 0.4f;
                    float env = Mathf.Clamp01(t * 3f) * (1f - (t / 1.5f) * 0.2f);
                    s = (whistle * 0.45f + waterGargle * 0.35f + noise * 0.2f) * env;
                }
                // Giai đoạn 2: Phà khói trắng phào ra thư thái (1.6s -> 2.4s)
                else if (t > 1.6f)
                {
                    float ex = t - 1.6f;
                    float breath = (UnityEngine.Random.value * 2f - 1f) * Mathf.Exp(-ex * 4f) * 0.35f;
                    s = breath;
                }

                data[i] = s;
            }

            AudioClip clip = AudioClip.Create("DieuCayRiiit", samples, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private AudioClip CreateChatPopClip()
        {
            // Tiếng Pop bong bóng dễ thương
            float duration = 0.15f;
            int samples = (int)(SampleRate * duration);
            float[] data = new float[samples];

            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / SampleRate;
                float freq = 450f + (1f - t / duration) * 600f;
                float env = Mathf.Exp(-t * 28f);
                data[i] = Mathf.Sin(2f * Mathf.PI * freq * t) * env;
            }

            AudioClip clip = AudioClip.Create("ChatPop", samples, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private AudioClip CreateTitleFanfareClip()
        {
            // Tiếng kèn & chuông ngân chúc mừng thăng cấp VIP ZingMe
            float duration = 1.8f;
            int samples = (int)(SampleRate * duration);
            float[] data = new float[samples];

            // 4 nốt arpeggio Đô - Mi - Sol - Đô cao
            float[] notes = new float[] { 523.25f, 659.25f, 783.99f, 1046.50f, 1318.51f };
            float noteDur = 0.22f;

            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / SampleRate;
                int noteIdx = Mathf.Min((int)(t / noteDur), notes.Length - 1);
                float f = notes[noteIdx];
                float noteT = t - noteIdx * noteDur;

                float bell = Mathf.Sin(2f * Mathf.PI * f * noteT) + 0.35f * Mathf.Sin(2f * Mathf.PI * f * 2f * noteT);
                float shimmer = Mathf.Sin(2f * Mathf.PI * 2500f * t) * 0.15f;
                float env = Mathf.Exp(-noteT * 6f);
                data[i] = (bell + shimmer) * env * 0.5f;
            }

            AudioClip clip = AudioClip.Create("TitleFanfare", samples, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private AudioClip CreateGrabTingTingClip()
        {
            // Chuông nổ cuốc xe Grab "Ting... Ting!" 2 tiếng trong trẻo
            float duration = 0.8f;
            int samples = (int)(SampleRate * duration);
            float[] data = new float[samples];

            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / SampleRate;
                float ding1 = (t < 0.35f) ? Mathf.Sin(2f * Mathf.PI * 1760f * t) * Mathf.Exp(-t * 12f) : 0f;
                float ding2 = (t >= 0.25f) ? Mathf.Sin(2f * Mathf.PI * 2217f * (t - 0.25f)) * Mathf.Exp(-(t - 0.25f) * 12f) : 0f;
                data[i] = (ding1 + ding2) * 0.6f;
            }

            AudioClip clip = AudioClip.Create("GrabTingTing", samples, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private AudioClip CreateBanhMiClip()
        {
            // Tiếng còi xe bánh mì loa phát thanh tò tí te tò tí te
            float duration = 1.2f;
            int samples = (int)(SampleRate * duration);
            float[] data = new float[samples];

            float[] melody = new float[] { 440f, 554f, 659f, 554f, 440f, 659f };
            float seg = duration / melody.Length;

            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / SampleRate;
                int idx = Mathf.Min((int)(t / seg), melody.Length - 1);
                float f = melody[idx];
                // Horn waveform
                float horn = Mathf.Sign(Mathf.Sin(2f * Mathf.PI * f * t)) * 0.3f;
                data[i] = horn * Mathf.Sin((t / duration) * Mathf.PI);
            }

            AudioClip clip = AudioClip.Create("BanhMiCry", samples, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private AudioClip CreatePoliceWhistleClip()
        {
            // Tiếng còi tuýt tuýt gấp gáp của Trật Tự Phường
            float duration = 1.0f;
            int samples = (int)(SampleRate * duration);
            float[] data = new float[samples];

            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / SampleRate;
                // Còi có 2 đợt tuýt: 0.05-0.45s và 0.55-0.95s
                bool blow1 = (t >= 0.05f && t <= 0.45f);
                bool blow2 = (t >= 0.55f && t <= 0.95f);

                if (blow1 || blow2)
                {
                    float wFreq = 2600f + Mathf.Sin(t * 180f) * 350f;
                    float whistle = Mathf.Sin(2f * Mathf.PI * wFreq * t);
                    float hiss = (UnityEngine.Random.value * 2f - 1f) * 0.25f;
                    data[i] = (whistle * 0.7f + hiss) * 0.6f;
                }
                else
                {
                    data[i] = 0f;
                }
            }

            AudioClip clip = AudioClip.Create("PoliceWhistle", samples, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private AudioClip CreateDiscoBeatClip()
        {
            // Beat loa kẹo kéo xập xình quẩy vỉa hè (4 nhịp Kick + HiHat + Bass lặp 2 giây)
            float duration = 2.0f;
            int samples = (int)(SampleRate * duration);
            float[] data = new float[samples];

            float bpm = 120f;
            float beatDur = 60f / bpm; // 0.5s mỗi phách

            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / SampleRate;
                float beatPhase = (t % beatDur) / beatDur;
                float beatTime = t % beatDur;

                // Kick 4-on-the-floor
                float kickFreq = 140f * Mathf.Exp(-beatTime * 35f) + 45f;
                float kick = Mathf.Sin(2f * Mathf.PI * kickFreq * beatTime) * Mathf.Exp(-beatTime * 14f);

                // Hi-hat ở off-beat
                float offBeat = (beatPhase > 0.45f && beatPhase < 0.65f) ? (UnityEngine.Random.value * 2f - 1f) * Mathf.Exp(-(beatPhase - 0.45f) * 40f) * 0.3f : 0f;

                // Disco Bassline
                float bassFreq = (t < 1.0f) ? 110f : 130.8f;
                float bass = Mathf.Sin(2f * Mathf.PI * bassFreq * t) * 0.25f;

                data[i] = Mathf.Clamp(kick * 0.6f + offBeat + bass, -1f, 1f);
            }

            AudioClip clip = AudioClip.Create("DiscoBeat", samples, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private AudioClip CreateBaoQuanJackpotClip()
        {
            // Đại gia bao cả quán: Pháo hoa nổ đùng đoàng + chuông vàng reo
            float duration = 2.5f;
            int samples = (int)(SampleRate * duration);
            float[] data = new float[samples];

            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / SampleRate;
                // Chuông reo jackpot
                float chime = Mathf.Sin(2f * Mathf.PI * (1200f + Mathf.Sin(t * 30f) * 400f) * t) * 0.35f;
                // Tiếng pháo hoa nổ ầm ĩ
                float boom = (t < 0.8f) ? (UnityEngine.Random.value * 2f - 1f) * Mathf.Exp(-t * 5f) * 0.5f : 0f;
                // Kèn đồng chiến thắng
                float horn = Mathf.Sin(2f * Mathf.PI * 659.25f * t) * 0.25f;

                data[i] = (chime + boom + horn) * Mathf.Clamp01(1f - t / duration);
            }

            AudioClip clip = AudioClip.Create("BaoQuanJackpot", samples, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private AudioClip CreateStreetLoFiBgmClip()
        {
            // Nhạc nền lo-fi acoustic phố cổ Hà Nội êm dịu kéo dài 6 giây (loop liền mạch)
            float duration = 6.0f;
            int samples = (int)(SampleRate * duration);
            float[] data = new float[samples];

            // Vòng hợp âm C - Am - F - G
            float[] rootChords = new float[] { 261.63f, 220.00f, 174.61f, 196.00f };
            float chordDur = duration / 4f;

            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / SampleRate;
                int cIdx = (int)(t / chordDur) % 4;
                float chordRoot = rootChords[cIdx];

                // Guitar acoustic mộc
                float guitar = Mathf.Sin(2f * Mathf.PI * chordRoot * t) * 0.15f
                             + Mathf.Sin(2f * Mathf.PI * (chordRoot * 1.25f) * t) * 0.10f
                             + Mathf.Sin(2f * Mathf.PI * (chordRoot * 1.50f) * t) * 0.10f;

                // Gió thổi xào xạc vỉa hè
                float breeze = (Mathf.PerlinNoise(t * 1.5f, 0f) - 0.5f) * 0.08f;

                data[i] = guitar + breeze;
            }

            AudioClip clip = AudioClip.Create("StreetLoFiBgm", samples, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
