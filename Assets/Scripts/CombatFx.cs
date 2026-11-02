using System;
using System.Collections.Generic;
using UnityEngine;

namespace TikTokFactionWar
{
    /// <summary>
    /// Pooled sprite-animation combat VFX: "chuong luc" energy blasts, slashes, lightning, flame breath, impacts,
    /// shockwaves, summon portals, footstep / hover / wing-gust motion FX. Every effect is a pooled SpriteRenderer pair
    /// (tinted glow + white-hot core) animated by a manual Tick(dt) so it is deterministic and can be stepped in Edit Mode.
    /// Visual-only: gameplay damage stays in FactionUnit / FactionWarManager.
    /// </summary>
    public class CombatFx : MonoBehaviour
    {
        public static CombatFx Instance { get; private set; }

        private const int MaxActive = 240;
        private const int BusyThreshold = 110;
        private const float BlastHeadWorld = 1.28f; // head diameter of vfx_energy_blast at scale 1 (256x512 @100ppu)

        private enum Mode { Pop, Projectile, Beam }
        private enum ArriveKind { None, Hit, Slash, Slam }

        private sealed class Fx
        {
            public GameObject go;
            public Transform tf;
            public SpriteRenderer main;
            public SpriteRenderer core;
            public Transform coreTf;
            public Mode mode;
            public float t, dur;
            public Vector3 from, to, vel;
            public Color color;
            public float s0, s1, refSize;
            public float rot, spin;
            public float coreScale, coreAlpha, alphaMul, fadePower, flicker;
            public float spriteW, spriteH;
            public float trailTimer;
            public bool trail;
            public ArriveKind arrive;
            public float arriveSize;
            public Color arriveColor;
            public float flipTimer;
        }

        private struct Sched
        {
            public float t;
            public Action a;
        }

        private readonly List<Fx> _active = new List<Fx>(160);
        private readonly Stack<Fx> _free = new Stack<Fx>(160);
        private readonly List<Sched> _sched = new List<Sched>(32);
        private Material _fxMat;
        private bool _initialized;

        public bool Busy => _active.Count > BusyThreshold;
        public int ActiveCount => _active.Count;

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
            if (_initialized) return;
            _initialized = true;
            _fxMat = Palette.GetFxMaterial();
        }

        private void Update()
        {
            Tick(Time.deltaTime);
        }

        public void StepForTest(int steps, float dt)
        {
            for (int i = 0; i < steps; i++) Tick(dt);
        }

        public void ClearAll()
        {
            for (int i = _active.Count - 1; i >= 0; i--) Release(_active[i], false);
            _active.Clear();
            _sched.Clear();
        }

        // ------------------------------------------------------------------ core loop

        public void Tick(float dt)
        {
            if (!_initialized) return;

            for (int i = _sched.Count - 1; i >= 0; i--)
            {
                if (i >= _sched.Count) continue;
                Sched s = _sched[i];
                s.t -= dt;
                if (s.t <= 0f)
                {
                    _sched.RemoveAt(i);
                    s.a?.Invoke();
                }
                else
                {
                    _sched[i] = s;
                }
            }

            for (int i = _active.Count - 1; i >= 0; i--)
            {
                Fx f = _active[i];
                f.t += dt;
                float n = Mathf.Clamp01(f.t / f.dur);
                switch (f.mode)
                {
                    case Mode.Pop: TickPop(f, n, dt); break;
                    case Mode.Projectile: TickProjectile(f, n, dt); break;
                    case Mode.Beam: TickBeam(f, n, dt); break;
                }

                if (f.t >= f.dur)
                {
                    ArriveKind kind = f.arrive;
                    Vector3 at = f.to;
                    float asz = f.arriveSize;
                    Color ac = f.arriveColor;
                    bool wasProjectile = f.mode == Mode.Projectile;
                    _active.RemoveAt(i);
                    Release(f, false);
                    if (wasProjectile) OnArrive(kind, at, ac, asz);
                }
            }
        }

        private void TickPop(Fx f, float n, float dt)
        {
            f.tf.position += f.vel * dt;
            float size = Mathf.Lerp(f.s0, f.s1, Ease.OutCubic(n));
            float fadeIn = Mathf.Clamp01(n / 0.10f);
            float alpha = fadeIn * Mathf.Pow(1f - n, f.fadePower) * f.alphaMul;
            f.tf.localRotation = Quaternion.Euler(0f, 0f, f.rot + f.spin * f.t);
            float sc = size / f.refSize;
            f.tf.localScale = new Vector3(sc, sc, 1f);
            ApplyColor(f, alpha);
        }

        private void TickProjectile(Fx f, float n, float dt)
        {
            float e = Mathf.Lerp(n, n * n, 0.45f);
            Vector3 pos = Vector3.Lerp(f.from, f.to, e);
            f.tf.position = pos;
            float pulse = 1f + 0.12f * Mathf.Sin(f.t * 46f);
            float size = Mathf.Lerp(f.s0, f.s1, n) * pulse;
            float sc = size / f.refSize;
            f.tf.localRotation = Quaternion.Euler(0f, 0f, f.rot);
            f.tf.localScale = new Vector3(sc, sc, 1f);
            float alpha = Mathf.Clamp01(n / 0.08f) * (n > 0.88f ? Mathf.Clamp01((1f - n) / 0.12f) : 1f);
            ApplyColor(f, alpha);

            if (f.trail)
            {
                f.trailTimer -= dt;
                if (f.trailTimer <= 0f && !Busy)
                {
                    f.trailTimer = 0.045f;
                    Fx g = Pop("vfx_energy_orb", pos - (f.to - f.from).normalized * 0.12f, f.color, size * 0.55f, size * 0.15f, 0.20f, 0f, 0f, 25, 0.35f);
                    if (g != null) g.alphaMul = 0.55f;
                }
            }
        }

        private void TickBeam(Fx f, float n, float dt)
        {
            Vector3 d = f.to - f.from;
            float length = d.magnitude;
            Vector3 mid = (f.from + f.to) * 0.5f;
            f.tf.position = mid;
            float ang = Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg - 90f;
            f.tf.localRotation = Quaternion.Euler(0f, 0f, ang);

            f.flipTimer -= dt;
            float jitter = 1f + (UnityEngine.Random.value - 0.5f) * 2f * f.flicker;
            float width = Mathf.Lerp(f.s0, f.s1, n) * jitter;
            float sx = width / f.spriteW * (f.main.flipX ? 1f : 1f);
            float sy = length / f.spriteH;
            f.tf.localScale = new Vector3(sx, sy, 1f);
            if (f.flipTimer <= 0f)
            {
                f.flipTimer = 0.045f;
                f.main.flipX = !f.main.flipX;
                f.core.flipX = f.main.flipX;
            }

            float env = n < 0.12f ? n / 0.12f : (n > 0.65f ? (1f - n) / 0.35f : 1f);
            ApplyColor(f, Mathf.Clamp01(env) * f.alphaMul * (0.8f + 0.2f * jitter));
        }

        private static void ApplyColor(Fx f, float alpha)
        {
            f.main.color = new Color(f.color.r, f.color.g, f.color.b, Mathf.Clamp01(alpha * f.color.a * 1.15f));
            if (f.coreAlpha > 0f)
            {
                f.core.color = new Color(1f, 0.98f, 0.92f, Mathf.Clamp01(alpha * f.coreAlpha * 0.72f));
                f.coreTf.localScale = new Vector3(f.coreScale * 0.65f, f.coreScale * 0.65f, 1f);
            }
        }

        private void OnArrive(ArriveKind kind, Vector3 at, Color c, float size)
        {
            switch (kind)
            {
                case ArriveKind.Hit:
                    PlayImpact(at, c, size, true);
                    Snd(SfxId.ChuongHit, at.x, Mathf.Clamp(0.45f + size * 0.25f, 0.4f, 1f));
                    break;
                case ArriveKind.Slash:
                    PlayMeleeSlash(at, c, size);
                    PlayImpact(at, c, size * 0.55f, false);
                    break;
                case ArriveKind.Slam:
                    PlayImpact(at, c, size, true);
                    PlayShockwave(at, c, size * 0.3f, size * 1.9f, 0.40f);
                    Snd(SfxId.Shockwave, at.x, 0.55f);
                    break;
            }
        }

        // ------------------------------------------------------------------ pooling

        private Sprite GetSprite(string name)
        {
            if (name == "circle") return Palette.GetCircleSprite(64, 0);
            Sprite s = Palette.GetGeneratedSprite(name, 100f);
            return s != null ? s : Palette.GetCircleSprite(64, 0);
        }

        private Fx Acquire(string spriteName, Color c, int order, float coreAlpha)
        {
            if (_active.Count >= MaxActive) return null;
            EnsureInitialized();

            Fx f = _free.Count > 0 ? _free.Pop() : CreateFx();
            Sprite spr = GetSprite(spriteName);
            f.main.sprite = spr;
            f.core.sprite = spr;
            f.main.sortingOrder = order;
            f.core.sortingOrder = order + 1;
            f.main.flipX = false;
            f.core.flipX = false;
            f.spriteW = Mathf.Max(0.01f, spr.bounds.size.x);
            f.spriteH = Mathf.Max(0.01f, spr.bounds.size.y);
            f.refSize = f.spriteW;
            f.color = c;
            f.t = 0f;
            f.vel = Vector3.zero;
            f.rot = 0f;
            f.spin = 0f;
            f.alphaMul = 1f;
            f.fadePower = 1.6f;
            f.flicker = 0f;
            f.flipTimer = 0.045f;
            f.trail = false;
            f.arrive = ArriveKind.None;
            f.coreAlpha = coreAlpha;
            f.coreScale = 0.55f;
            f.core.gameObject.SetActive(coreAlpha > 0f);
            f.go.SetActive(true);
            _active.Add(f);
            return f;
        }

        private Fx CreateFx()
        {
            Fx f = new Fx();
            f.go = new GameObject("PooledCombatFx");
            f.go.transform.SetParent(transform, false);
            f.tf = f.go.transform;
            f.main = f.go.AddComponent<SpriteRenderer>();
            f.main.sharedMaterial = _fxMat;

            GameObject coreGo = new GameObject("Core");
            coreGo.transform.SetParent(f.tf, false);
            f.coreTf = coreGo.transform;
            f.core = coreGo.AddComponent<SpriteRenderer>();
            f.core.sharedMaterial = _fxMat;
            return f;
        }

        private void Release(Fx f, bool removeFromActive = true)
        {
            if (removeFromActive) _active.Remove(f);
            f.go.SetActive(false);
            _free.Push(f);
        }

        private void After(float delay, Action a)
        {
            if (delay <= 0f)
            {
                a();
                return;
            }
            _sched.Add(new Sched { t = delay, a = a });
        }

        private static void Snd(SfxId id, float worldX, float vol = 1f)
        {
            if (GameAudio.Instance != null) GameAudio.Instance.PlaySfx(id, worldX, vol);
        }

        // ------------------------------------------------------------------ primitive spawners

        private Fx Pop(string spr, Vector3 pos, Color c, float s0, float s1, float dur, float rot, float spin, int order, float coreAlpha)
        {
            Fx f = Acquire(spr, c, order, coreAlpha);
            if (f == null) return null;
            f.mode = Mode.Pop;
            f.tf.position = pos;
            f.s0 = s0;
            f.s1 = s1;
            f.dur = Mathf.Max(0.04f, dur);
            f.rot = rot;
            f.spin = spin;
            TickPop(f, 0f, 0f);
            return f;
        }

        private Fx Beam(string spr, Vector3 from, Vector3 to, Color c, float width, float dur, int order, float coreAlpha, float flicker)
        {
            Fx f = Acquire(spr, c, order, coreAlpha);
            if (f == null) return null;
            f.mode = Mode.Beam;
            f.from = from;
            f.to = to;
            f.s0 = width;
            f.s1 = width * 0.7f;
            f.dur = Mathf.Max(0.05f, dur);
            f.flicker = flicker;
            f.coreScale = 0.5f;
            TickBeam(f, 0.2f, 0f);
            return f;
        }

        private Fx Projectile(Vector3 from, Vector3 to, Color c, float size, float speed, ArriveKind arrive, float arriveSize, bool trail)
        {
            Fx f = Acquire("vfx_energy_blast", c, 26, 0.9f);
            if (f == null)
            {
                OnArrive(arrive, to, c, arriveSize);
                return null;
            }
            Vector3 d = to - from;
            float dist = d.magnitude;
            f.mode = Mode.Projectile;
            f.from = from;
            f.to = to;
            f.dur = Mathf.Clamp(dist / Mathf.Max(0.1f, speed), 0.07f, 0.55f);
            f.s0 = size * 0.7f;
            f.s1 = size;
            f.refSize = BlastHeadWorld;
            f.rot = Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg - 90f;
            f.coreScale = 0.5f;
            f.trail = trail;
            f.trailTimer = 0f;
            f.arrive = arrive;
            f.arriveSize = arriveSize;
            f.arriveColor = c;
            TickProjectile(f, 0f, 0f);
            return f;
        }

        // ------------------------------------------------------------------ public effect recipes

        public void FireChuong(Vector3 from, Vector3 to, Color c, float size, float speed, bool slashOnArrive, float arriveSize, bool trail)
        {
            Projectile(from, to, c, size, speed, slashOnArrive ? ArriveKind.Slash : ArriveKind.Hit, arriveSize, trail);
        }

        public void PlayImpact(Vector3 pos, Color c, float size, bool ring)
        {
            EnsureInitialized();
            Pop("vfx_impact_burst", pos, c, size * 0.35f, size, 0.26f, UnityEngine.Random.Range(0f, 360f), UnityEngine.Random.Range(-120f, 120f), 27, 0.7f);
            if (ring && !Busy) Pop("vfx_shockwave_ring", pos, c, size * 0.3f, size * 1.6f, 0.34f, 0f, 0f, 26, 0f);
            if (!Busy && VfxPool.Instance != null)
                VfxPool.Instance.PlayBurst(pos, c, Mathf.RoundToInt(5f + size * 4f), 0.55f + size * 0.35f);
        }

        public void PlayHitSpark(Vector3 pos, Color c)
        {
            if (Busy) return;
            Pop("vfx_impact_burst", pos, c, 0.08f, 0.55f, 0.15f, UnityEngine.Random.Range(0f, 360f), 0f, 27, 0.5f);
        }

        public void PlayShockwave(Vector3 pos, Color c, float startSize, float endSize, float dur)
        {
            EnsureInitialized();
            Pop("vfx_shockwave_ring", pos, c, startSize, endSize, dur, 0f, 0f, 24, 0f);
        }

        public void PlayMeleeSlash(Vector3 at, Color c, float size)
        {
            float baseRot = UnityEngine.Random.Range(-35f, 35f) + (UnityEngine.Random.value < 0.5f ? 0f : 180f);
            Fx f = Pop("vfx_slash_arc", at, c, size * 0.8f, size * 1.35f, 0.22f, baseRot - 55f, 300f, 28, 0.8f);
            if (f != null && UnityEngine.Random.value < 0.5f) f.main.flipX = true;
            Snd(SfxId.Slash, at.x, 0.7f);
        }

        public void PlayLightning(Vector3 from, Vector3 to, Color c, float width, float dur)
        {
            EnsureInitialized();
            Beam("vfx_lightning_bolt", from, to, c, width, dur, 25, 0.8f, 0.30f);
        }

        public void PlayFlameBreath(Vector3 from, Vector3 to, Color c, float width, float dur)
        {
            EnsureInitialized();
            Beam("vfx_flame_breath", from, to, c, width, dur, 25, 0.55f, 0.18f);
        }

        public void PlayOrb(Vector3 pos, Color c, float s0, float s1, float dur)
        {
            EnsureInitialized();
            Pop("vfx_energy_orb", pos, c, s0, s1, dur, 0f, 140f, 27, 0.8f);
        }

        public void PlayAttack(FactionUnit u, Vector3 target, bool isBase)
        {
            if (u == null) return;
            EnsureInitialized();

            Vector3 o = u.transform.position;
            Vector3 d = target - o;
            d.z = 0f;
            float dist = d.magnitude;
            Vector3 dir = dist > 0.01f ? d / dist : new Vector3(0f, u.FactionId == 1 ? -1f : 1f, 0f);
            Vector3 perp = new Vector3(-dir.y, dir.x, 0f);
            Color p = Palette.GetFactionPrimary(u.FactionId);
            Color s = Palette.GetFactionSecondary(u.FactionId);
            Color hot = Color.Lerp(s, Color.white, 0.4f);
            bool busy = Busy;

            switch (u.Tier)
            {
                case UnitTier.Infantry:
                case UnitTier.Squad:
                {
                    Vector3 muzzle = o + dir * 0.30f;
                    if (!busy) Pop("vfx_muzzle_flash", muzzle, Palette.AccentGold, 0.25f, 0.75f, 0.15f, UnityEngine.Random.Range(0f, 90f), 0f, 27, 0.8f);
                    FireChuong(muzzle, target, p, 0.85f, 6.5f, true, 1.25f, !busy);
                    PlayMeleeSlash(o + dir * 0.45f, p, 0.95f);
                    Snd(SfxId.ChuongLaunchSmall, o.x, 0.7f);
                    break;
                }
                case UnitTier.Tank:
                {
                    Vector3 muzzle = o + dir * 0.58f;
                    Pop("vfx_muzzle_flash", muzzle, Palette.AccentGold, 0.5f, 1.35f, 0.28f, UnityEngine.Random.Range(0f, 90f), 0f, 27, 0.9f);
                    if (!busy) PlayDust(o - dir * 0.28f, Palette.NeutralMuted, 0.55f);
                    FireChuong(muzzle, target, p, 1.35f, 5.5f, false, 2.0f, !busy);
                    Snd(SfxId.CannonBoom, o.x, 0.85f);
                    break;
                }
                case UnitTier.Mecha:
                {
                    Vector3 shoulderL = o + dir * 0.40f + perp * 0.40f;
                    Vector3 shoulderR = o + dir * 0.40f - perp * 0.40f;
                    PlayLightning(shoulderL, target, p, 1.45f, 0.38f);
                    PlayLightning(shoulderR, target + perp * 0.25f, Palette.AccentGold, 1.25f, 0.35f);
                    Pop("vfx_muzzle_flash", shoulderL, Palette.AccentGold, 0.25f, 0.95f, 0.16f, 20f, 0f, 27, 0.9f);
                    Pop("vfx_muzzle_flash", shoulderR, Palette.AccentGold, 0.25f, 0.95f, 0.16f, 60f, 0f, 27, 0.9f);
                    FireChuong(o + dir * 0.45f, target, p, 1.25f, 6.0f, false, 2.4f, !busy);
                    After(0.10f, () =>
                    {
                        PlayShockwave(target, p, 0.4f, 2.4f, 0.42f);
                        PlayImpact(target, Palette.AccentGold, 1.4f, false);
                    });
                    Snd(SfxId.Zap, o.x, 0.8f);
                    Snd(SfxId.ChuongLaunchBig, o.x, 0.65f);
                    break;
                }
                case UnitTier.Dragon:
                {
                    Vector3 end = dist < 1.5f ? o + dir * 1.5f : target;
                    Vector3 head = o + dir * 0.55f;
                    PlayFlameBreath(head, end, Palette.AccentOrange, 2.25f, 0.52f);
                    PlayFlameBreath(head, end + perp * 0.45f, p, 1.65f, 0.48f);
                    Pop("vfx_muzzle_flash", head, Palette.AccentGold, 0.4f, 1.6f, 0.24f, 0f, 90f, 27, 0.9f);
                    for (int i = 0; i < 3; i++)
                    {
                        float k = 0.45f + i * 0.28f;
                        Vector3 ip = Vector3.Lerp(head, end, k) + perp * UnityEngine.Random.Range(-0.3f, 0.3f);
                        After(0.06f + i * 0.07f, () => PlayImpact(ip, Palette.AccentOrange, 1.1f + k * 0.5f, false));
                    }
                    Snd(SfxId.FlameBreath, o.x, 0.8f);
                    if (UnityEngine.Random.value < 0.3f) Snd(SfxId.DragonRoar, o.x, 0.45f);
                    break;
                }
            }
        }

        public void PlaySpawnSummon(Vector3 pos, int factionId, UnitTier tier)
        {
            EnsureInitialized();
            Color p = Palette.GetFactionPrimary(factionId);
            Color s = Palette.GetFactionSecondary(factionId);
            float big = tier >= UnitTier.Dragon ? 3.4f : tier >= UnitTier.Mecha ? 2.6f : tier >= UnitTier.Tank ? 1.8f : 1.0f;

            PlayShockwave(pos, p, big * 0.25f, big, 0.45f);
            PlayOrb(pos, s, big * 0.2f, big * 0.8f, 0.35f);
            if (tier >= UnitTier.Tank)
            {
                PlayLightning(pos + new Vector3(0f, 3.4f, 0f), pos, Color.Lerp(s, Color.white, 0.3f), 0.9f + big * 0.15f, 0.30f);
                After(0.10f, () => PlayImpact(pos, p, big * 0.8f, false));
            }
            if (tier >= UnitTier.Mecha)
            {
                After(0.12f, () => PlayShockwave(pos, Palette.AccentGold, big * 0.2f, big * 1.4f, 0.5f));
            }

            Snd(SfxId.SummonCharge, pos.x, tier >= UnitTier.Tank ? 0.9f : 0.5f);
            if (tier == UnitTier.Dragon) Snd(SfxId.DragonRoar, pos.x, 0.9f);
            else if (tier == UnitTier.Mecha) Snd(SfxId.MechaStomp, pos.x, 0.9f);
        }

        public void PlayDeath(Vector3 pos, int factionId, UnitTier tier)
        {
            EnsureInitialized();
            Color p = Palette.GetFactionPrimary(factionId);
            Color boom = Color.Lerp(p, Palette.AccentOrange, 0.45f);
            float big = tier >= UnitTier.Dragon ? 4.0f : tier >= UnitTier.Mecha ? 2.8f : tier >= UnitTier.Tank ? 1.9f : 0.95f;

            PlayImpact(pos, boom, big, true);
            if (tier >= UnitTier.Tank)
            {
                PlayOrb(pos, Palette.AccentGold, big * 0.3f, big * 0.9f, 0.3f);
                After(0.10f, () => PlayShockwave(pos, Palette.AccentGold, big * 0.2f, big * 1.7f, 0.5f));
                Snd(SfxId.Explosion, pos.x, tier >= UnitTier.Mecha ? 1f : 0.7f);
            }
            else
            {
                Snd(SfxId.UnitDeath, pos.x, 0.6f);
            }
            if (tier == UnitTier.Dragon)
            {
                for (int i = 0; i < 3; i++)
                {
                    Vector3 ip = pos + new Vector3(UnityEngine.Random.Range(-1.0f, 1.0f), UnityEngine.Random.Range(-0.8f, 0.8f), 0f);
                    After(0.08f + i * 0.09f, () => PlayImpact(ip, boom, 1.5f, true));
                }
            }
        }

        public void PlayBaseHit(Vector3 pos, int attackerFactionId, float damage)
        {
            EnsureInitialized();
            Color p = Palette.GetFactionPrimary(attackerFactionId);
            float size = Mathf.Clamp(1.2f + damage / 150f, 1.2f, 3.4f);
            Vector3 at = pos + new Vector3(UnityEngine.Random.Range(-0.7f, 0.7f), UnityEngine.Random.Range(-0.25f, 0.25f), 0f);
            PlayImpact(at, p, size, true);
            Pop("vfx_impact_burst", at, Color.white, size * 0.3f, size * 0.9f, 0.16f, 0f, 60f, 28, 0f);
            if (damage >= 120f)
                PlayLightning(at + new Vector3(0f, 2.6f, 0f), at, Palette.AccentGold, 0.9f, 0.22f);
            Snd(SfxId.BaseHit, at.x, Mathf.Clamp(0.5f + damage / 400f, 0.5f, 1f));
        }

        public void PlayShieldUp(Vector3 pos)
        {
            EnsureInitialized();
            PlayShockwave(pos, Palette.AccentGold, 0.4f, 2.0f, 0.4f);
            if (!Busy) PlayOrb(pos, Palette.AccentGold, 0.3f, 1.1f, 0.25f);
            Snd(SfxId.ShieldUp, pos.x, 0.5f);
        }

        public void PlayDust(Vector3 pos, Color c, float size)
        {
            Fx f = Pop("circle", pos, new Color(c.r, c.g, c.b, 0.55f), size * 0.4f, size, 0.34f, 0f, 0f, 8, 0f);
            if (f != null)
            {
                f.vel = new Vector3(UnityEngine.Random.Range(-0.25f, 0.25f), UnityEngine.Random.Range(0.1f, 0.35f), 0f);
                f.fadePower = 1.2f;
            }
        }

        /// <summary>Per-stride motion FX: infantry dust, tank hover thrust, mecha stomp, dragon wing gust.</summary>
        public void PlayFootstep(FactionUnit u, float moveDirY)
        {
            if (u == null) return;
            EnsureInitialized();
            Vector3 pos = u.transform.position;
            Color p = Palette.GetFactionPrimary(u.FactionId);
            Color s = Palette.GetFactionSecondary(u.FactionId);
            Vector3 behind = pos - new Vector3(0f, moveDirY * 0.28f, 0f);
            bool busy = Busy;

            switch (u.Tier)
            {
                case UnitTier.Infantry:
                case UnitTier.Squad:
                    if (!busy) PlayDust(behind + new Vector3(UnityEngine.Random.Range(-0.12f, 0.12f), 0f, 0f), Palette.NeutralMuted, 0.26f);
                    Snd(SfxId.FootstepSoft, pos.x, 0.35f);
                    break;
                case UnitTier.Tank:
                    if (!busy)
                    {
                        Pop("vfx_energy_orb", behind - new Vector3(0f, moveDirY * 0.18f, 0f), s, 0.55f, 0.12f, 0.30f, 0f, 200f, 8, 0.5f);
                        Pop("vfx_energy_orb", behind + new Vector3(-0.28f, 0f, 0f), p, 0.3f, 0.08f, 0.26f, 0f, -160f, 8, 0.4f);
                        Pop("vfx_energy_orb", behind + new Vector3(0.28f, 0f, 0f), p, 0.3f, 0.08f, 0.26f, 0f, 160f, 8, 0.4f);
                    }
                    Snd(SfxId.HoverHum, pos.x, 0.35f);
                    break;
                case UnitTier.Mecha:
                    PlayShockwave(behind, Palette.NeutralMuted, 0.3f, 1.35f, 0.36f);
                    if (!busy)
                    {
                        PlayDust(behind + new Vector3(-0.4f, 0f, 0f), Palette.NeutralMuted, 0.5f);
                        PlayDust(behind + new Vector3(0.4f, 0f, 0f), Palette.NeutralMuted, 0.5f);
                        Pop("vfx_impact_burst", behind, Palette.AccentGold, 0.1f, 0.6f, 0.14f, 0f, 0f, 27, 0.4f);
                    }
                    Snd(SfxId.MechaStomp, pos.x, 0.75f);
                    break;
                case UnitTier.Dragon:
                    PlayShockwave(pos + new Vector3(-0.7f, 0.1f, 0f), s, 0.3f, 1.3f, 0.35f);
                    PlayShockwave(pos + new Vector3(0.7f, 0.1f, 0f), s, 0.3f, 1.3f, 0.35f);
                    if (!busy) Pop("vfx_energy_orb", behind, Palette.AccentGold, 0.7f, 0.15f, 0.32f, 0f, 120f, 8, 0.5f);
                    Snd(SfxId.WingFlap, pos.x, 0.6f);
                    break;
            }
        }

        public void PlayNuke(Vector3 center, Color attacker, int targetFactionId)
        {
            EnsureInitialized();
            Color gold = Palette.AccentGold;
            Pop("vfx_energy_orb", center, Color.Lerp(attacker, Color.white, 0.2f), 0.6f, 8.5f, 0.55f, 0f, 120f, 28, 1f);
            Pop("vfx_impact_burst", center, gold, 1.0f, 10.5f, 0.5f, 0f, 40f, 29, 0.9f);
            for (int i = 0; i < 3; i++)
            {
                float endSize = 11f + i * 3.5f;
                Color ringColor = i % 2 == 0 ? gold : attacker;
                After(0.05f + i * 0.13f, () => PlayShockwave(center, ringColor, 1.0f, endSize, 0.75f));
            }

            for (int i = 0; i < 9; i++)
            {
                float x = UnityEngine.Random.Range(-3.2f, 3.2f);
                float y = targetFactionId == 1 ? UnityEngine.Random.Range(2.2f, 4.6f) : UnityEngine.Random.Range(-1.4f, 1.2f);
                Vector3 ground = new Vector3(x, y, 0f);
                After(0.05f + i * 0.065f, () =>
                {
                    PlayLightning(ground + new Vector3(UnityEngine.Random.Range(-0.5f, 0.5f), 5.5f, 0f), ground, Color.Lerp(gold, Color.white, 0.3f), 1.15f, 0.32f);
                    PlayImpact(ground, gold, 1.7f, true);
                });
            }
            Snd(SfxId.NukeBlast, center.x, 1f);
        }
    }
}
