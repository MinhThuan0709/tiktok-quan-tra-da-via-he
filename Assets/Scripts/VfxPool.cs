using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Pool;

namespace TikTokFactionWar
{
    /// <summary>
    /// Pooled VFX and floating world-space text manager following craft-game-floors vfx.md & game-feel.md rules.
    /// Uses UnityEngine.Pool.ObjectPool so zero ParticleSystems or TMP labels are Destroyed at runtime.
    /// </summary>
    public class VfxPool : MonoBehaviour
    {
        public static VfxPool Instance { get; private set; }

        private ObjectPool<ParticleSystem> _burstPool;
        private ObjectPool<TextMeshPro> _floatingTextPool;
        private ParticleSystem _ambientMotes;
        private Material _particleMaterial;

        public int ActiveBurstCount { get; private set; }

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
            if (_burstPool != null) return;

            Shader shader = Shader.Find("Sprites/Default");
            if (shader == null) shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
            _particleMaterial = new Material(shader)
            {
                name = "Palette_ParticleMat",
                mainTexture = Palette.GetCircleSprite(32, 0).texture
            };

            _burstPool = new ObjectPool<ParticleSystem>(
                createFunc: CreateBurstSystem,
                actionOnGet: ps =>
                {
                    ps.gameObject.SetActive(true);
                    ActiveBurstCount++;
                },
                actionOnRelease: ps =>
                {
                    ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                    ps.gameObject.SetActive(false);
                    ActiveBurstCount = Mathf.Max(0, ActiveBurstCount - 1);
                },
                actionOnDestroy: ps =>
                {
                    if (ps != null) Destroy(ps.gameObject);
                },
                collectionCheck: false,
                defaultCapacity: 16,
                maxSize: 48
            );

            _floatingTextPool = new ObjectPool<TextMeshPro>(
                createFunc: CreateFloatingText,
                actionOnGet: tmp => tmp.gameObject.SetActive(true),
                actionOnRelease: tmp => tmp.gameObject.SetActive(false),
                actionOnDestroy: tmp =>
                {
                    if (tmp != null) Destroy(tmp.gameObject);
                },
                collectionCheck: false,
                defaultCapacity: 20,
                maxSize: 60
            );

            CreateAmbientMotes();
        }

        private ParticleSystem CreateBurstSystem()
        {
            GameObject go = new GameObject("PooledBurstVFX");
            go.transform.SetParent(transform, false);

            ParticleSystem ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = ps.main;
            main.playOnAwake = false;
            main.loop = false;
            main.duration = 0.55f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.32f, 0.55f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(1.8f, 3.8f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.14f, 0.28f);
            main.gravityModifier = 0.25f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startColor = Palette.AccentGold;

            var emission = ps.emission;
            emission.enabled = true;
            emission.rateOverTime = 0f;

            var shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 0.15f;

            var sizeOverLifetime = ps.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            AnimationCurve shrink = new AnimationCurve(
                new Keyframe(0f, 1f),
                new Keyframe(0.7f, 0.65f),
                new Keyframe(1f, 0f));
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, shrink);

            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = _particleMaterial;
            renderer.sortingOrder = 25;

            return ps;
        }

        private void CreateAmbientMotes()
        {
            GameObject go = new GameObject("AmbientBattlefieldMotes");
            go.transform.SetParent(transform, false);
            go.transform.position = new Vector3(0f, 1.2f, 0f);

            _ambientMotes = go.AddComponent<ParticleSystem>();
            _ambientMotes.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = _ambientMotes.main;
            main.loop = true;
            main.playOnAwake = true;
            main.duration = 6f;
            main.startLifetime = 4.5f;
            main.startSpeed = 0.25f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.15f);
            main.maxParticles = 24;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startColor = Palette.WithAlpha(Palette.AccentGold, 0.35f);

            var emission = _ambientMotes.emission;
            emission.enabled = true;
            emission.rateOverTime = 5f;

            var shape = _ambientMotes.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(7.5f, 9.0f, 0.1f);

            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = _particleMaterial;
            renderer.sortingOrder = 2;

            _ambientMotes.Play();
        }

        private TextMeshPro CreateFloatingText()
        {
            GameObject go = new GameObject("PooledFloatingText");
            go.transform.SetParent(transform, false);

            TextMeshPro tmp = go.AddComponent<TextMeshPro>();
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.fontSize = 3.2f;
            tmp.fontStyle = FontStyles.Bold;
            tmp.characterSpacing = 3f;
            tmp.color = Palette.AccentGold;
            if (Application.isPlaying && tmp.fontSharedMaterial != null)
            {
                tmp.outlineWidth = 0.18f;
                tmp.outlineColor = Palette.ShadowDark;
            }
            tmp.sortingOrder = 35;
            return tmp;
        }

        public void PlayBurst(Vector3 position, Color primaryColor, int count = 12, float scaleMultiplier = 1f)
        {
            EnsureInitialized();
            ParticleSystem ps = _burstPool.Get();
            ps.transform.position = position;

            var main = ps.main;
            main.startColor = primaryColor;
            main.startSize = new ParticleSystem.MinMaxCurve(0.06f * scaleMultiplier, 0.13f * scaleMultiplier);
            main.startSpeed = new ParticleSystem.MinMaxCurve(1.4f * scaleMultiplier, 3.0f * scaleMultiplier);

            ps.Clear();
            ps.Play();
            ps.Emit(count);
            if (!Application.isPlaying)
            {
                ps.Simulate(0.18f, true, false);
            }
            else
            {
                StartCoroutine(ReleaseBurstAfter(ps, 0.6f));
            }
        }

        public void PlayConfetti(Vector3 position)
        {
            PlayBurst(position + new Vector3(-0.8f, 0f, 0f), Palette.FactionTopPrimary, 18, 1.3f);
            PlayBurst(position, Palette.AccentGold, 22, 1.4f);
            PlayBurst(position + new Vector3(0.8f, 0f, 0f), Palette.FactionBottomPrimary, 18, 1.3f);
        }

        public void SpawnFloatingText(Vector3 position, string message, Color color, float size = 3.2f)
        {
            EnsureInitialized();
            TextMeshPro tmp = _floatingTextPool.Get();
            tmp.transform.position = position;
            tmp.transform.localScale = Vector3.one;
            tmp.text = message;
            tmp.fontSize = size;
            tmp.color = color;

            if (Application.isPlaying)
            {
                StartCoroutine(AnimateFloatingText(tmp, position));
            }
        }

        public void ClearTransientVisuals()
        {
            foreach (Transform child in transform)
            {
                if (child.name == "AmbientBattlefieldMotes") continue;
                var ps = child.GetComponent<ParticleSystem>();
                if (ps != null && child.gameObject.activeSelf)
                {
                    _burstPool.Release(ps);
                    continue;
                }
                var tmp = child.GetComponent<TextMeshPro>();
                if (tmp != null && child.gameObject.activeSelf)
                {
                    _floatingTextPool.Release(tmp);
                }
            }
        }

        private IEnumerator ReleaseBurstAfter(ParticleSystem ps, float delay)
        {
            yield return new WaitForSecondsRealtime(delay);
            if (ps != null && ps.gameObject.activeSelf)
            {
                _burstPool.Release(ps);
            }
        }

        private IEnumerator AnimateFloatingText(TextMeshPro tmp, Vector3 startPos)
        {
            Color baseColor = tmp.color;
            yield return Ease.Tween(0.65f, t =>
            {
                if (tmp == null) return;
                tmp.transform.position = startPos + new Vector3(0f, t * 0.85f, 0f);
                float pop = t < 0.25f ? Mathf.Lerp(0.6f, 1.15f, t / 0.25f) : Mathf.Lerp(1.15f, 0.9f, (t - 0.25f) / 0.75f);
                tmp.transform.localScale = Vector3.one * pop;
                tmp.color = Palette.WithAlpha(baseColor, 1f - t * t);
            }, Ease.OutCubic, unscaled: true);

            if (tmp != null && tmp.gameObject.activeSelf)
            {
                _floatingTextPool.Release(tmp);
            }
        }
    }
}
