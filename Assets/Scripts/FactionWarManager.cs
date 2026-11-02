using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.UI;

namespace TikTokFactionWar
{
    public enum GameState
    {
        Title = 0,
        Playing = 1,
        Paused = 2,
        Ended = 3
    }

    [Serializable]
    public class FactionThemePreset
    {
        public string presetTitle;
        public string topFactionName;
        public string topEmblemType;
        public string bottomFactionName;
        public string bottomEmblemType;
    }

    public class ViewerStats
    {
        public string userId;
        public string nickname;
        public int factionId;
        public int score;
    }

    /// <summary>
    /// Core Tug-of-War Faction Battle Manager, UI Shell State Machine (Title, Playing, Paused, Ended),
    /// Theme Preset Switcher, ObjectPool Unit Spawner, and MVP Leaderboard tracker.
    /// </summary>
    public class FactionWarManager : MonoBehaviour
    {
        public static FactionWarManager Instance { get; private set; }

        // Vertical 9:16 Safe-Zone Coordinates (Unobstructed between Top & Bottom HUD bars)
        public const float TopBaseY = 4.25f;
        public const float BottomBaseY = -1.05f;
        public const float ArenaCenterY = (TopBaseY + BottomBaseY) * 0.5f; // +1.60f
        public const int MaxActiveUnitsPerSide = 80;

        [Header("Tunables (Craft Feel Floor)")]
        public float baseMaxHp = 5000f;
        public float popScaleMultiplier = 1.25f;
        public float barTweenDuration = 0.30f;
        public float shakeForceDefault = 0.35f;

        [Header("State & Scores")]
        public GameState CurrentState { get; private set; } = GameState.Title;
        public float TopBaseHp { get; private set; } = 5000f;
        public float BottomBaseHp { get; private set; } = 5000f;
        public int TopWinStreak { get; private set; }
        public int BottomWinStreak { get; private set; }
        public float TugOfWarRatio { get; private set; } = 0.5f; // 0 = Bottom pushed to Top, 1 = Top pushed to Bottom
        public int CurrentThemeIndex { get; private set; }

        [Header("Scene References")]
        public Transform worldArenaRoot;
        public SpriteRenderer topBaseCoreRenderer;
        public SpriteRenderer topBaseEmblemRenderer;
        public SpriteRenderer bottomBaseCoreRenderer;
        public SpriteRenderer bottomBaseEmblemRenderer;
        public Transform frontlineBarTransform;
        public SpriteRenderer frontlineGlowRenderer;
        public CinemachineImpulseSource impulseSource;

        [Header("HUD References")]
        public GameObject gameplayHudPanel;
        public Image topHpFillImage;
        public Image bottomHpFillImage;
        public TextMeshProUGUI topFactionTitleText;
        public TextMeshProUGUI bottomFactionTitleText;
        public TextMeshProUGUI topHpValueText;
        public TextMeshProUGUI bottomHpValueText;
        public TextMeshProUGUI scoreHeaderText;
        public TextMeshProUGUI mvpBoardText;
        public TextMeshProUGUI eventBannerText;
        public RectTransform eventBannerRect;
        public Image screenFlashOverlay;
        public Image transitionFadeOverlay;

        [Header("Shell Panels")]
        public GameObject titlePanel;
        public TextMeshProUGUI titlePromptText;
        public GameObject pausePanel;
        public Slider musicSlider;
        public Slider sfxSlider;
        public GameObject endedPanel;
        public TextMeshProUGUI endedWinnerText;
        public TextMeshProUGUI endedStatsText;

        private readonly FactionThemePreset[] _presets =
        {
            new FactionThemePreset
            {
                presetTitle = "ĐỎ vs XANH",
                topFactionName = "PHE ĐỎ",
                topEmblemType = "star",
                bottomFactionName = "PHE XANH",
                bottomEmblemType = "shield"
            },
            new FactionThemePreset
            {
                presetTitle = "VIỆT NAM vs QUỐC TẾ",
                topFactionName = "VIỆT NAM",
                topEmblemType = "star",
                bottomFactionName = "QUỐC TẾ",
                bottomEmblemType = "crown"
            },
            new FactionThemePreset
            {
                presetTitle = "TEAM MÈO vs TEAM CHÓ",
                topFactionName = "TEAM MÈO",
                topEmblemType = "cat",
                bottomFactionName = "TEAM CHÓ",
                bottomEmblemType = "shield"
            },
            new FactionThemePreset
            {
                presetTitle = "REAL vs BARCA",
                topFactionName = "REAL MADRID",
                topEmblemType = "crown",
                bottomFactionName = "BARCELONA",
                bottomEmblemType = "star"
            }
        };

        private ObjectPool<FactionUnit> _unitPool;
        private readonly List<FactionUnit> _activeUnits = new List<FactionUnit>(180);
        private readonly Dictionary<string, int> _viewerFactionMap = new Dictionary<string, int>();
        private readonly Dictionary<string, ViewerStats> _viewerStatsMap = new Dictionary<string, ViewerStats>();

        private bool _topLastStandTriggered;
        private bool _bottomLastStandTriggered;
        private Coroutine _topHpTween;
        private Coroutine _bottomHpTween;
        private Coroutine _bannerCoroutine;
        private float _backdropPanPhase;

        public IReadOnlyList<FactionUnit> ActiveUnits => _activeUnits;

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
            if (_unitPool != null) return;

            _unitPool = new ObjectPool<FactionUnit>(
                createFunc: CreatePooledUnit,
                actionOnGet: u => u.gameObject.SetActive(true),
                actionOnRelease: u => u.gameObject.SetActive(false),
                actionOnDestroy: u =>
                {
                    if (u != null) Destroy(u.gameObject);
                },
                collectionCheck: false,
                defaultCapacity: 64,
                maxSize: 200
            );
        }

        private void Start()
        {
            BindSettingsSliders();
            ApplyThemePreset(0);
            ResetMatchState();
            SetGameState(GameState.Title, instant: true);
            Debug.Log("[FactionWarManager] Initialized in Title state. Ready for Play or Live events.");
        }

        private void Update()
        {
            // Backdrop parallax motion on Title screen (2D delta from game-shell.md)
            if (CurrentState == GameState.Title)
            {
                _backdropPanPhase += Time.unscaledDeltaTime * 0.55f;
                if (worldArenaRoot != null)
                {
                    worldArenaRoot.position = new Vector3(Mathf.Sin(_backdropPanPhase) * 0.25f, 0f, 0f);
                }
                if (titlePromptText != null)
                {
                    float alpha = Mathf.Lerp(0.5f, 1.0f, 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 3.8f));
                    titlePromptText.color = Palette.WithAlpha(Palette.NeutralLight, alpha);
                }
            }
            else if (worldArenaRoot != null)
            {
                worldArenaRoot.position = Vector3.zero;
            }

            // Gentle idle breathing on base cores so nothing is statuary
            float basePulse = 1f + Mathf.Sin(Time.time * 2.8f) * 0.035f;
            if (topBaseCoreRenderer != null)
                topBaseCoreRenderer.transform.localScale = new Vector3(1.55f * basePulse, 0.85f / basePulse, 1f);
            if (bottomBaseCoreRenderer != null)
                bottomBaseCoreRenderer.transform.localScale = new Vector3(1.55f * basePulse, 0.85f / basePulse, 1f);

            if (CurrentState == GameState.Playing)
            {
                TickBattlefield(Time.deltaTime);
            }
        }

        private FactionUnit CreatePooledUnit()
        {
            GameObject go = new GameObject("PooledFactionUnit");
            go.transform.SetParent(worldArenaRoot != null ? worldArenaRoot : transform, false);
            FactionUnit unit = go.AddComponent<FactionUnit>();
            unit.BuildVisualHierarchyIfNeeded();
            return unit;
        }

        private void BindSettingsSliders()
        {
            if (musicSlider != null)
            {
                musicSlider.SetValueWithoutNotify(GameAudio.Instance != null ? GameAudio.Instance.MusicVol : 0.55f);
                musicSlider.onValueChanged.RemoveAllListeners();
                musicSlider.onValueChanged.AddListener(val =>
                {
                    if (GameAudio.Instance != null) GameAudio.Instance.MusicVol = val;
                });
            }

            if (sfxSlider != null)
            {
                sfxSlider.SetValueWithoutNotify(GameAudio.Instance != null ? GameAudio.Instance.SFXVol : 0.85f);
                sfxSlider.onValueChanged.RemoveAllListeners();
                sfxSlider.onValueChanged.AddListener(val =>
                {
                    if (GameAudio.Instance != null) GameAudio.Instance.SFXVol = val;
                });
            }
        }

        public void SetGameState(GameState nextState, bool instant = false)
        {
            if (!Application.isPlaying || instant)
            {
                CurrentState = nextState;
                Time.timeScale = (nextState == GameState.Paused) ? 0f : 1f;
                if (titlePanel != null) titlePanel.SetActive(nextState == GameState.Title);
                if (gameplayHudPanel != null) gameplayHudPanel.SetActive(nextState != GameState.Title);
                if (pausePanel != null) pausePanel.SetActive(nextState == GameState.Paused);
                if (endedPanel != null) endedPanel.SetActive(nextState == GameState.Ended);
                return;
            }
            StartCoroutine(TransitionStateCoroutine(nextState, instant));
        }

        private IEnumerator TransitionStateCoroutine(GameState nextState, bool instant)
        {
            if (!instant && transitionFadeOverlay != null)
            {
                transitionFadeOverlay.gameObject.SetActive(true);
                yield return Ease.Tween(0.15f, t =>
                {
                    transitionFadeOverlay.color = Palette.WithAlpha(Palette.ShadowDark, t * 0.85f);
                }, Ease.OutCubic, unscaled: true);
            }

            CurrentState = nextState;
            Time.timeScale = (nextState == GameState.Paused) ? 0f : 1f;

            if (GameAudio.Instance != null)
            {
                GameAudio.Instance.SetPausedDuck(nextState == GameState.Paused);
                if (!instant) GameAudio.Instance.PlayUiClick();
            }

            if (titlePanel != null) titlePanel.SetActive(nextState == GameState.Title);
            if (gameplayHudPanel != null) gameplayHudPanel.SetActive(nextState != GameState.Title);
            if (pausePanel != null) pausePanel.SetActive(nextState == GameState.Paused);
            if (endedPanel != null) endedPanel.SetActive(nextState == GameState.Ended);

            Debug.Log($"[FactionWarManager] GameState changed to {CurrentState} (timeScale={Time.timeScale})");

            if (!instant && transitionFadeOverlay != null)
            {
                yield return Ease.Tween(0.15f, t =>
                {
                    transitionFadeOverlay.color = Palette.WithAlpha(Palette.ShadowDark, (1f - t) * 0.85f);
                }, Ease.OutCubic, unscaled: true);
                transitionFadeOverlay.gameObject.SetActive(false);
            }
        }

        public void StartMatchFromTitle()
        {
            ResetMatchState();
            SetGameState(GameState.Playing);
            ShowMilestoneBanner($">> TRẬN ĐẤU BẮT ĐẦU: {_presets[CurrentThemeIndex].presetTitle}!", Palette.AccentGold);
        }

        public void TogglePause()
        {
            if (CurrentState == GameState.Playing)
                SetGameState(GameState.Paused);
            else if (CurrentState == GameState.Paused)
                SetGameState(GameState.Playing);
        }

        public void RestartCleanRun()
        {
            ResetMatchState();
            SetGameState(GameState.Playing);
            Debug.Log("[FactionWarManager] Clean run restarted.");
        }

        public void ReturnToTitle()
        {
            ResetMatchState();
            SetGameState(GameState.Title);
        }

        public void ResetMatchState()
        {
            for (int i = _activeUnits.Count - 1; i >= 0; i--)
            {
                if (_activeUnits[i] != null)
                    _unitPool.Release(_activeUnits[i]);
            }
            _activeUnits.Clear();
            _viewerFactionMap.Clear();
            _viewerStatsMap.Clear();

            TopBaseHp = baseMaxHp;
            BottomBaseHp = baseMaxHp;
            TugOfWarRatio = 0.5f;
            _topLastStandTriggered = false;
            _bottomLastStandTriggered = false;

            if (topHpFillImage != null) topHpFillImage.fillAmount = 1f;
            if (bottomHpFillImage != null) bottomHpFillImage.fillAmount = 1f;
            UpdateHudTexts();
            UpdateFrontlineVisual(0.5f);
        }

        public void ApplyThemePreset(int presetIndex)
        {
            CurrentThemeIndex = Mathf.Abs(presetIndex) % _presets.Length;
            FactionThemePreset p = _presets[CurrentThemeIndex];

            if (topFactionTitleText != null)
                topFactionTitleText.text = $"{p.topFactionName} (GÕ 1)";
            if (bottomFactionTitleText != null)
                bottomFactionTitleText.text = $"{p.bottomFactionName} (GÕ 2)";

            if (topBaseEmblemRenderer != null)
                topBaseEmblemRenderer.sprite = Palette.GetEmblemSprite(p.topEmblemType);
            if (bottomBaseEmblemRenderer != null)
                bottomBaseEmblemRenderer.sprite = Palette.GetEmblemSprite(p.bottomEmblemType);

            UpdateHudTexts();
            Debug.Log($"[FactionWarManager] Theme preset set to: {p.presetTitle}");
        }

        public void CycleThemePreset()
        {
            ApplyThemePreset(CurrentThemeIndex + 1);
            ShowMilestoneBanner($"ĐỔI CHỦ ĐỀ: {_presets[CurrentThemeIndex].presetTitle}", Palette.AccentGold);
        }

        public int GetOrAssignViewerFaction(string userId, int preferredFaction = 0)
        {
            if (string.IsNullOrEmpty(userId)) userId = "viewer";

            if (preferredFaction == 1 || preferredFaction == 2)
            {
                _viewerFactionMap[userId] = preferredFaction;
                return preferredFaction;
            }

            if (_viewerFactionMap.TryGetValue(userId, out int existing))
            {
                return existing;
            }

            // Auto-balance to faction with fewer active units or higher HP deficit
            int topCount = 0;
            int bottomCount = 0;
            for (int i = 0; i < _activeUnits.Count; i++)
            {
                if (_activeUnits[i].FactionId == 1) topCount++;
                else bottomCount++;
            }

            int assigned = topCount <= bottomCount ? 1 : 2;
            _viewerFactionMap[userId] = assigned;
            return assigned;
        }

        public void SpawnViewerUnit(string userId, string nickname, string avatarUrl, int factionId, UnitTier tier)
        {
            if (CurrentState == GameState.Title)
            {
                SetGameState(GameState.Playing, instant: true);
            }
            if (CurrentState != GameState.Playing) return;

            factionId = (factionId == 1) ? 1 : 2;
            _viewerFactionMap[userId] = factionId;

            int countToSpawn = (tier == UnitTier.Squad) ? 5 : 1;
            int points = tier switch
            {
                UnitTier.Infantry => 10,
                UnitTier.Squad => 50,
                UnitTier.Tank => 150,
                UnitTier.Mecha => 450,
                UnitTier.Dragon => 1200,
                _ => 10
            };

            RecordContribution(userId, nickname, factionId, points);

            int sideCount = 0;
            for (int i = 0; i < _activeUnits.Count; i++)
            {
                if (_activeUnits[i].FactionId == factionId) sideCount++;
            }

            // If side is at cap, merge new infantry into power buffs on existing units to preserve 60 FPS
            if (sideCount >= MaxActiveUnitsPerSide && tier <= UnitTier.Squad)
            {
                for (int i = 0; i < _activeUnits.Count; i++)
                {
                    if (_activeUnits[i].FactionId == factionId)
                    {
                        _activeUnits[i].ApplyPowerBuff(15f, 4f);
                    }
                }
                if (VfxPool.Instance != null)
                {
                    Vector3 basePos = new Vector3(0f, factionId == 1 ? TopBaseY - 0.8f : BottomBaseY + 0.8f, 0f);
                    VfxPool.Instance.SpawnFloatingText(basePos, $"[BUFF TOÀN ĐỘI] {nickname}", Palette.AccentGold, 2.8f);
                }
                return;
            }

            float spawnBaseY = factionId == 1 ? (TopBaseY - 0.75f) : (BottomBaseY + 0.75f);

            for (int n = 0; n < countToSpawn; n++)
            {
                FactionUnit unit = _unitPool.Get();
                float offsetX = (countToSpawn == 1)
                    ? UnityEngine.Random.Range(-2.6f, 2.6f)
                    : Mathf.Lerp(-2.2f, 2.2f, n / (float)(countToSpawn - 1)) + UnityEngine.Random.Range(-0.15f, 0.15f);
                float offsetY = UnityEngine.Random.Range(-0.22f, 0.22f);

                unit.transform.position = new Vector3(offsetX, spawnBaseY + offsetY, 0f);

                Sprite initialAvatar = AvatarCache.Instance != null
                    ? AvatarCache.Instance.GetOrRequestAvatar(userId, avatarUrl, factionId, loaded => unit.SetAvatarSprite(loaded))
                    : Palette.GetCircleSprite(64, 2);

                unit.Initialize(factionId, tier, userId, nickname, initialAvatar);
                _activeUnits.Add(unit);

                if (VfxPool.Instance != null && n == 0)
                {
                    VfxPool.Instance.PlayBurst(unit.transform.position, Palette.GetFactionPrimary(factionId), tier >= UnitTier.Tank ? 16 : 8, tier >= UnitTier.Tank ? 1.3f : 0.85f);
                }
            }

            // Scale-pop the spawning base core (game-feel.md pickup/spawn recipe)
            Transform baseCore = factionId == 1
                ? (topBaseCoreRenderer != null ? topBaseCoreRenderer.transform : null)
                : (bottomBaseCoreRenderer != null ? bottomBaseCoreRenderer.transform : null);

            if (baseCore != null && VfxPool.Instance != null)
            {
                VfxPool.Instance.SpawnFloatingText(
                    baseCore.position + new Vector3(0f, factionId == 1 ? -0.55f : 0.55f, 0f),
                    tier >= UnitTier.Tank ? $">> {nickname} GỌI {tier.ToString().ToUpper()}!" : $"+1 {nickname}",
                    tier >= UnitTier.Tank ? Palette.AccentGold : Palette.GetFactionSecondary(factionId),
                    tier >= UnitTier.Tank ? 3.2f : 2.4f);
            }

            if (GameAudio.Instance != null)
            {
                if (tier >= UnitTier.Tank) GameAudio.Instance.PlayGiftSting();
                else GameAudio.Instance.PlaySpawnBlip();
            }

            if (tier >= UnitTier.Mecha)
            {
                TriggerCameraShake(tier == UnitTier.Dragon ? shakeForceDefault * 1.8f : shakeForceDefault);
                string unitTitle = tier == UnitTier.Dragon ? "RỒNG LỬA HỦY DIỆT" : "SIÊU ROBOT MECHA";
                ShowMilestoneBanner($"[VIP] {nickname} TRIỆU HỒI {unitTitle}!", Palette.GetFactionPrimary(factionId));

                if (tier == UnitTier.Dragon)
                {
                    // Grant 5s Golden Shield to all allied units
                    for (int i = 0; i < _activeUnits.Count; i++)
                    {
                        if (_activeUnits[i].FactionId == factionId)
                            _activeUnits[i].GrantShield(5f);
                    }
                }
            }
        }

        public void TriggerTacticalNuke(string userId, string nickname, int factionId)
        {
            if (CurrentState == GameState.Title)
            {
                SetGameState(GameState.Playing, instant: true);
            }
            if (CurrentState != GameState.Playing) return;

            RecordContribution(userId, nickname, factionId, 3000);
            StartCoroutine(TacticalNukeSequenceCoroutine(userId, nickname, factionId));
        }

        private IEnumerator TacticalNukeSequenceCoroutine(string userId, string nickname, int factionId)
        {
            if (GameAudio.Instance != null) GameAudio.Instance.PlayNukeSiren();
            ShowMilestoneBanner($"[NUKE] CẢNH BÁO HẠT NHÂN TỪ [{nickname}]! 3... 2... 1...", Palette.AccentGold);
            TriggerCameraShake(shakeForceDefault * 2.5f);

            // Flash screen overlay (milestone recipe from game-feel.md)
            if (screenFlashOverlay != null)
            {
                screenFlashOverlay.gameObject.SetActive(true);
                yield return Ease.Tween(0.18f, t =>
                {
                    screenFlashOverlay.color = Palette.WithAlpha(Palette.AccentGold, (1f - t) * 0.72f);
                }, Ease.OutCubic, unscaled: true);
                screenFlashOverlay.gameObject.SetActive(false);
            }

            // Wipe enemy units
            int targetEnemyFaction = factionId == 1 ? 2 : 1;
            for (int i = _activeUnits.Count - 1; i >= 0; i--)
            {
                FactionUnit u = _activeUnits[i];
                if (u.FactionId == targetEnemyFaction)
                {
                    if (VfxPool.Instance != null)
                        VfxPool.Instance.PlayBurst(u.transform.position, Palette.AccentOrange, 12, 1.25f);
                    _unitPool.Release(u);
                    _activeUnits.RemoveAt(i);
                }
            }

            if (VfxPool.Instance != null)
            {
                VfxPool.Instance.PlayConfetti(new Vector3(0f, ArenaCenterY, 0f));
            }

            // Deal 25% Max HP to enemy base
            DamageBase(targetEnemyFaction, baseMaxHp * 0.25f, userId, nickname, factionId);
        }

        public void StepBattlefieldForTest(int steps, float dt)
        {
            for (int i = 0; i < steps; i++)
            {
                TickBattlefield(dt);
            }
        }

        private void TickBattlefield(float dt)
        {
            float sumY = 0f;

            for (int i = _activeUnits.Count - 1; i >= 0; i--)
            {
                if (i >= _activeUnits.Count) continue;
                FactionUnit u = _activeUnits[i];
                if (u == null || !u.IsAlive)
                {
                    if (u != null) _unitPool.Release(u);
                    _activeUnits.RemoveAt(i);
                    continue;
                }
                u.TickUnit(dt, this);
                if (u != null && u.IsAlive)
                {
                    sumY += u.transform.position.y;
                }
            }

            for (int i = _activeUnits.Count - 1; i >= 0; i--)
            {
                FactionUnit u = _activeUnits[i];
                if (u == null || !u.IsAlive)
                {
                    if (u != null) _unitPool.Release(u);
                    _activeUnits.RemoveAt(i);
                }
            }

            // Compute Tug-of-War frontline position from base HP ratio + unit momentum
            float hpBalance = TopBaseHp / Mathf.Max(1f, TopBaseHp + BottomBaseHp);
            float targetRatio = hpBalance;
            if (_activeUnits.Count > 0)
            {
                float avgY = sumY / _activeUnits.Count;
                float normY = Mathf.InverseLerp(BottomBaseY, TopBaseY, avgY);
                targetRatio = Mathf.Lerp(hpBalance, 1f - normY, 0.35f);
            }

            TugOfWarRatio = Mathf.Lerp(TugOfWarRatio, Mathf.Clamp(targetRatio, 0.08f, 0.92f), dt * 4f);
            UpdateFrontlineVisual(TugOfWarRatio);
        }

        private void UpdateFrontlineVisual(float ratio)
        {
            if (frontlineBarTransform == null) return;
            float yPos = Mathf.Lerp(TopBaseY - 0.9f, BottomBaseY + 0.9f, ratio);
            frontlineBarTransform.position = new Vector3(0f, yPos, 0f);

            if (frontlineGlowRenderer != null)
            {
                frontlineGlowRenderer.color = ratio >= 0.5f ? Palette.FactionTopPrimary : Palette.FactionBottomPrimary;
            }
        }

        public FactionUnit FindNearestEnemy(FactionUnit seeker, float maxRange)
        {
            FactionUnit best = null;
            float bestDistSq = maxRange * maxRange;
            Vector3 seekerPos = seeker.transform.position;

            for (int i = 0; i < _activeUnits.Count; i++)
            {
                FactionUnit candidate = _activeUnits[i];
                if (candidate == null || !candidate.IsAlive || candidate.FactionId == seeker.FactionId)
                    continue;

                float dSq = (candidate.transform.position - seekerPos).sqrMagnitude;
                if (dSq <= bestDistSq)
                {
                    bestDistSq = dSq;
                    best = candidate;
                }
            }
            return best;
        }

        public void DamageEnemiesInRadius(Vector3 center, int attackerFactionId, float damage, float radius, string attackerUserId, string attackerName)
        {
            float rSq = radius * radius;
            int totalDealt = 0;

            for (int i = _activeUnits.Count - 1; i >= 0; i--)
            {
                FactionUnit u = _activeUnits[i];
                if (u == null || !u.IsAlive || u.FactionId == attackerFactionId) continue;

                if ((u.transform.position - center).sqrMagnitude <= rSq)
                {
                    bool died = u.TakeDamage(damage);
                    totalDealt += Mathf.RoundToInt(damage);
                    if (died)
                    {
                        if (VfxPool.Instance != null)
                            VfxPool.Instance.PlayBurst(u.transform.position, Palette.GetFactionPrimary(attackerFactionId), 10, 1.0f);
                    }
                }
            }

            if (totalDealt > 0)
            {
                RecordContribution(attackerUserId, attackerName, attackerFactionId, totalDealt / 4);
            }
        }

        public void DamageBase(int targetFactionId, float damage, string attackerUserId, string attackerName, int attackerFactionId)
        {
            if (CurrentState != GameState.Playing) return;

            RecordContribution(attackerUserId, attackerName, attackerFactionId, Mathf.RoundToInt(damage));

            if (targetFactionId == 1)
            {
                float oldRatio = TopBaseHp / baseMaxHp;
                TopBaseHp = Mathf.Max(0f, TopBaseHp - damage);
                float newRatio = TopBaseHp / baseMaxHp;
                AnimateHpBar(topHpFillImage, ref _topHpTween, oldRatio, newRatio);

                if (VfxPool.Instance != null && topBaseCoreRenderer != null)
                {
                    VfxPool.Instance.PlayBurst(topBaseCoreRenderer.transform.position, Palette.FactionBottomPrimary, 8, 0.9f);
                    VfxPool.Instance.SpawnFloatingText(topBaseCoreRenderer.transform.position, $"-{Mathf.RoundToInt(damage)}", Palette.FactionBottomSecondary, 2.8f);
                }

                // Comeback Last Stand at 20% HP
                if (!_topLastStandTriggered && TopBaseHp > 0f && TopBaseHp <= baseMaxHp * 0.20f)
                {
                    _topLastStandTriggered = true;
                    TriggerLastStand(1);
                }

                if (TopBaseHp <= 0f)
                {
                    ConcludeMatch(winningFaction: 2, lastHitViewer: attackerName);
                }
            }
            else
            {
                float oldRatio = BottomBaseHp / baseMaxHp;
                BottomBaseHp = Mathf.Max(0f, BottomBaseHp - damage);
                float newRatio = BottomBaseHp / baseMaxHp;
                AnimateHpBar(bottomHpFillImage, ref _bottomHpTween, oldRatio, newRatio);

                if (VfxPool.Instance != null && bottomBaseCoreRenderer != null)
                {
                    VfxPool.Instance.PlayBurst(bottomBaseCoreRenderer.transform.position, Palette.FactionTopPrimary, 8, 0.9f);
                    VfxPool.Instance.SpawnFloatingText(bottomBaseCoreRenderer.transform.position, $"-{Mathf.RoundToInt(damage)}", Palette.FactionTopSecondary, 2.8f);
                }

                if (!_bottomLastStandTriggered && BottomBaseHp > 0f && BottomBaseHp <= baseMaxHp * 0.20f)
                {
                    _bottomLastStandTriggered = true;
                    TriggerLastStand(2);
                }

                if (BottomBaseHp <= 0f)
                {
                    ConcludeMatch(winningFaction: 1, lastHitViewer: attackerName);
                }
            }

            UpdateHudTexts();
        }

        private void TriggerLastStand(int factionId)
        {
            string fName = factionId == 1 ? _presets[CurrentThemeIndex].topFactionName : _presets[CurrentThemeIndex].bottomFactionName;
            ShowMilestoneBanner($"[BÁO ĐỘNG ĐỎ] {fName} KÍCH HOẠT CƠN THỊNH NỘ!", Palette.AccentOrange);
            TriggerCameraShake(shakeForceDefault * 1.4f);

            // Spawn 3 free emergency tanks for the defending faction
            for (int i = 0; i < 3; i++)
            {
                SpawnViewerUnit($"guard_{i}", "Vệ Binh", "", factionId, UnitTier.Tank);
            }
        }

        private void AnimateHpBar(Image barImage, ref Coroutine runningTween, float fromVal, float toVal)
        {
            if (barImage == null) return;
            if (!Application.isPlaying)
            {
                barImage.fillAmount = toVal;
                return;
            }
            if (runningTween != null) StopCoroutine(runningTween);
            runningTween = StartCoroutine(Ease.Tween(barTweenDuration, t =>
            {
                barImage.fillAmount = Mathf.Lerp(fromVal, toVal, t);
            }, Ease.OutCubic, unscaled: true));
        }

        public void RecordContribution(string userId, string nickname, int factionId, int deltaScore)
        {
            if (string.IsNullOrEmpty(userId)) userId = "viewer";
            if (!_viewerStatsMap.TryGetValue(userId, out ViewerStats stats))
            {
                stats = new ViewerStats
                {
                    userId = userId,
                    nickname = string.IsNullOrEmpty(nickname) ? userId : nickname,
                    factionId = factionId,
                    score = 0
                };
                _viewerStatsMap[userId] = stats;
            }
            stats.nickname = string.IsNullOrEmpty(nickname) ? stats.nickname : nickname;
            stats.factionId = factionId;
            stats.score += Mathf.Max(1, deltaScore);

            UpdateMvpBoardText();
        }

        private void UpdateHudTexts()
        {
            FactionThemePreset p = _presets[CurrentThemeIndex];

            if (topHpValueText != null)
                topHpValueText.text = $"{Mathf.CeilToInt(TopBaseHp)} / {Mathf.CeilToInt(baseMaxHp)} HP";
            if (bottomHpValueText != null)
                bottomHpValueText.text = $"{Mathf.CeilToInt(BottomBaseHp)} / {Mathf.CeilToInt(baseMaxHp)} HP";

            if (scoreHeaderText != null)
                scoreHeaderText.text = $"TỈ SỐ: {p.topFactionName} [{TopWinStreak}]  —  [{BottomWinStreak}] {p.bottomFactionName}";

            UpdateMvpBoardText();
        }

        private void UpdateMvpBoardText()
        {
            if (mvpBoardText == null) return;

            var topList = _viewerStatsMap.Values
                .OrderByDescending(v => v.score)
                .Take(3)
                .ToList();

            if (topList.Count == 0)
            {
                mvpBoardText.text = "[TOP MVP] Comment '1' hoặc '2' để xuất quân lên bảng vàng!";
                return;
            }

            System.Text.StringBuilder sb = new System.Text.StringBuilder("[TOP MVP]  ");
            for (int i = 0; i < topList.Count; i++)
            {
                string tag = topList[i].factionId == 1 ? "[ĐỎ]" : "[XANH]";
                sb.Append($"#{i + 1} {topList[i].nickname} {tag} ({topList[i].score}đ)   ");
            }
            mvpBoardText.text = sb.ToString();
        }

        public void ShowMilestoneBanner(string message, Color accentColor)
        {
            if (eventBannerText == null || eventBannerRect == null) return;
            eventBannerRect.gameObject.SetActive(true);
            eventBannerText.text = message;
            eventBannerText.color = accentColor;
            eventBannerRect.localScale = Vector3.one;

            if (!Application.isPlaying) return;
            if (_bannerCoroutine != null) StopCoroutine(_bannerCoroutine);
            _bannerCoroutine = StartCoroutine(MilestoneBannerSequence(message, accentColor));
        }

        private IEnumerator MilestoneBannerSequence(string message, Color accentColor)
        {
            eventBannerRect.gameObject.SetActive(true);
            eventBannerText.text = message;
            eventBannerText.color = accentColor;

            if (GameAudio.Instance != null) GameAudio.Instance.PlayGiftSting();

            // 50ms -> 350ms OutBack pop in
            yield return Ease.Tween(0.32f, t =>
            {
                eventBannerRect.localScale = Vector3.one * Mathf.Max(0.01f, t);
            }, Ease.OutBack, unscaled: true);

            yield return new WaitForSecondsRealtime(1.25f);

            // 200ms OutCubic scale out
            yield return Ease.Tween(0.20f, t =>
            {
                eventBannerRect.localScale = Vector3.one * (1f - t);
            }, Ease.OutCubic, unscaled: true);

            eventBannerRect.gameObject.SetActive(false);
        }

        public void TriggerCameraShake(float force)
        {
            if (impulseSource != null)
            {
                impulseSource.GenerateImpulseWithForce(force);
            }
        }

        private void ConcludeMatch(int winningFaction, string lastHitViewer)
        {
            if (winningFaction == 1) TopWinStreak++;
            else BottomWinStreak++;

            FactionThemePreset p = _presets[CurrentThemeIndex];
            string winnerFactionName = winningFaction == 1 ? p.topFactionName : p.bottomFactionName;

            var mvp = _viewerStatsMap.Values.OrderByDescending(v => v.score).FirstOrDefault();
            string mvpName = mvp != null ? mvp.nickname : lastHitViewer;
            int mvpScore = mvp != null ? mvp.score : 100;

            if (VfxPool.Instance != null)
            {
                VfxPool.Instance.PlayConfetti(new Vector3(0f, ArenaCenterY, 0f));
            }

            if (endedWinnerText != null)
            {
                endedWinnerText.text = $"[CHIẾN THẮNG] {winnerFactionName}!";
                endedWinnerText.color = Palette.GetFactionPrimary(winningFaction);
            }

            string rankLine = LiveServicesBootstrap.Instance != null
                ? LiveServicesBootstrap.Instance.LastLeaderboardRank
                : "Rank: local";

            if (endedStatsText != null)
            {
                endedStatsText.text =
                    $">> Đòn kết liễu (Last Hit): {lastHitViewer}\n" +
                    $">> MVP Toàn Trận: {mvpName} ({mvpScore} điểm)\n" +
                    $">> Tỉ số chuỗi thắng: {TopWinStreak} - {BottomWinStreak}\n" +
                    $">> Bảng xếp hạng Cloud: {rankLine}";
            }

            if (LiveServicesBootstrap.Instance != null)
            {
                _ = LiveServicesBootstrap.Instance.RecordMatchOutcomeAsync(winningFaction, mvpName, mvpScore);
            }

            SetGameState(GameState.Ended);
            Debug.Log($"[FactionWarManager] Match ended! Winner={winnerFactionName}, MVP={mvpName} ({mvpScore})");
        }
    }
}
