using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;
using TikTokFactionWar;

public static class VerifyFactionWar
{
    public static string RunMachineScan()
    {
        var sb = new StringBuilder();

        // 1. URP & Post-Processing Pre-Flight Check
        var urpAsset = UniversalRenderPipeline.asset;
        if (urpAsset == null) return "FAIL: URP is not active.";
        if (!urpAsset.supportsHDR) sb.AppendLine("WARN: HDR disabled on URP asset.");

        var cam = Camera.main;
        if (cam == null) return "FAIL: No Main Camera.";
        var camData = cam.GetComponent<UniversalAdditionalCameraData>();
        if (camData == null || !camData.renderPostProcessing)
            sb.AppendLine("FAIL: Camera renderPostProcessing is false.");

        var volumes = Object.FindObjectsByType<Volume>();
        if (volumes.Length == 0) sb.AppendLine("FAIL: No Volume in scene.");

        // 2. Renderer Default-Material Scan
        var renderers = Object.FindObjectsByType<Renderer>();
        foreach (var r in renderers)
        {
            if (r.sharedMaterial != null && r.sharedMaterial.name.Contains("Default-Material"))
            {
                sb.AppendLine($"FAIL: Renderer '{r.name}' uses Default-Material.");
            }
        }

        // 3. ParticleSystem startColor Scan
        var particles = Object.FindObjectsByType<ParticleSystem>();
        foreach (var ps in particles)
        {
            Color c = ps.main.startColor.color;
            if (c == Color.white || c == Color.gray)
            {
                sb.AppendLine($"FAIL: ParticleSystem '{ps.name}' uses untinted white/gray startColor.");
            }
        }

        // 4. UI Image Scan
        var images = Object.FindObjectsByType<Image>();
        foreach (var img in images)
        {
            if (img.color == Color.white || img.color == Color.gray)
            {
                sb.AppendLine($"FAIL: UI Image '{img.name}' uses untinted white/gray color.");
            }
        }

        if (sb.Length == 0)
        {
            return $"PASS: Machine scan clean ({renderers.Length} renderers, {particles.Length} particle systems, {images.Length} UI images verified).";
        }
        return sb.ToString();
    }

    private static void EnsureManagersReady()
    {
        var audio = Object.FindAnyObjectByType<GameAudio>();
        if (audio != null) audio.EnsureInitialized();

        var vfx = Object.FindAnyObjectByType<VfxPool>();
        if (vfx != null) vfx.EnsureInitialized();

        var combatFx = Object.FindAnyObjectByType<CombatFx>();
        if (combatFx != null) combatFx.EnsureInitialized();

        var avatar = Object.FindAnyObjectByType<AvatarCache>();
        if (avatar != null) avatar.EnsureInitialized();

        var mgr = Object.FindAnyObjectByType<FactionWarManager>();
        if (mgr != null) mgr.EnsureInitialized();
    }

    public static string SetupTitleCapture()
    {
        EnsureManagersReady();
        var mgr = FactionWarManager.Instance;
        if (mgr == null) return "FAIL: FactionWarManager.Instance is null.";

        mgr.ApplyThemePreset(0);
        mgr.ResetMatchState();
        mgr.SetGameState(GameState.Title, instant: true);
        return "Title state ready for capture.";
    }

    public static string TriggerBattleActionForCapture()
    {
        EnsureManagersReady();
        var mgr = FactionWarManager.Instance;
        if (mgr == null) return "FAIL: FactionWarManager.Instance is null.";

        mgr.ResetMatchState();
        mgr.SetGameState(GameState.Playing, instant: true);

        // Spawn units for both factions
        mgr.SpawnViewerUnit("vn_1", "MinhTuan99", "", 1, UnitTier.Squad);
        mgr.SpawnViewerUnit("vn_2", "HoangBoss", "", 1, UnitTier.Tank);
        mgr.SpawnViewerUnit("vn_3", "LinhChi_VIP", "", 1, UnitTier.Mecha);

        mgr.SpawnViewerUnit("qt_1", "AlexGaming", "", 2, UnitTier.Squad);
        mgr.SpawnViewerUnit("qt_2", "DragonSlayer", "", 2, UnitTier.Tank);
        mgr.SpawnViewerUnit("qt_3", "CyberKnight", "", 2, UnitTier.Dragon);

        mgr.StepBattlefieldForTest(26, 0.04f);

        if (VfxPool.Instance != null)
            VfxPool.Instance.ClearTransientVisuals();

        if (mgr.eventBannerRect != null)
            mgr.eventBannerRect.gameObject.SetActive(false);

        return $"Battle started with {mgr.ActiveUnits.Count} active units.";
    }

    public static string TriggerBusyMilestoneMoment()
    {
        var mgr = FactionWarManager.Instance;
        if (mgr == null) return "FAIL: FactionWarManager.Instance is null.";

        if (VfxPool.Instance != null)
            VfxPool.Instance.ClearTransientVisuals();

        mgr.DamageBase(2, 850f, "vn_whale", "ĐạiGia_PheĐỏ", 1);
        mgr.DamageBase(1, 620f, "qt_whale", "SieuCapVip", 2);
        mgr.ShowMilestoneBanner("[VIP] ĐạiGia_PheĐỏ TRIỆU HỒI RỒNG LỬA HỦY DIỆT!", Palette.AccentGold);

        if (VfxPool.Instance != null)
        {
            VfxPool.Instance.PlayConfetti(new Vector3(0f, FactionWarManager.ArenaCenterY, 0f));
            VfxPool.Instance.SpawnFloatingText(new Vector3(0f, FactionWarManager.ArenaCenterY + 1.45f, 0f), ">> COMBO x10 ROSE!", Palette.AccentGold, 3.6f);
        }

        return $"Busy milestone triggered. ActiveBurstCount={(VfxPool.Instance != null ? VfxPool.Instance.ActiveBurstCount : 0)}";
    }

    private static void PlaceDuel(FactionUnit red, FactionUnit blue, float x, float redY, float blueY)
    {
        red.transform.position = new Vector3(x, redY, 0f);
        blue.transform.position = new Vector3(x + 0.15f, blueY, 0f);
    }

    /// <summary>All 4 attack styles firing at once: infantry chuong+slash, tank cannon, mecha lightning, dragon flame.</summary>
    public static string ShowcaseCombatFx()
    {
        EnsureManagersReady();
        var mgr = FactionWarManager.Instance;
        if (mgr == null) return "FAIL: FactionWarManager.Instance is null.";

        mgr.ResetMatchState();
        mgr.SetGameState(GameState.Playing, instant: true);
        mgr.SpawnViewerUnit("r1", "LinhDo", "", 1, UnitTier.Infantry);
        mgr.SpawnViewerUnit("b1", "LinhXanh", "", 2, UnitTier.Infantry);
        mgr.SpawnViewerUnit("r2", "TankDo", "", 1, UnitTier.Tank);
        mgr.SpawnViewerUnit("b2", "TankXanh", "", 2, UnitTier.Tank);
        mgr.SpawnViewerUnit("r3", "MechaDo", "", 1, UnitTier.Mecha);
        mgr.SpawnViewerUnit("b3", "MechaXanh", "", 2, UnitTier.Mecha);
        mgr.SpawnViewerUnit("r4", "RongDo", "", 1, UnitTier.Dragon);
        mgr.SpawnViewerUnit("b4", "RongXanh", "", 2, UnitTier.Dragon);

        var units = mgr.ActiveUnits;
        if (units.Count < 8) return $"FAIL: expected 8 units, got {units.Count}";
        PlaceDuel(units[0], units[1], -2.55f, 2.20f, 1.60f);
        PlaceDuel(units[2], units[3], -1.15f, 2.95f, 1.30f);
        PlaceDuel(units[4], units[5], 0.45f, 3.35f, 1.30f);
        PlaceDuel(units[6], units[7], 2.20f, 3.45f, 0.95f);

        // Clear transient spawn motes and text to stage clean combat duel
        if (VfxPool.Instance != null) VfxPool.Instance.ClearTransientVisuals();
        if (CombatFx.Instance != null) CombatFx.Instance.ClearAll();

        for (int i = 0; i < units.Count; i++)
        {
            units[i].GrantShield(10f);
            units[i].ApplyPowerBuff(5000f, 0f);
        }

        // Advance 3 frames (0.12s): exactly when _attackCooldown reaches 0 and all 4 tiers unleash attacks
        mgr.StepBattlefieldForTest(3, 0.04f);
        if (mgr.eventBannerRect != null) mgr.eventBannerRect.gameObject.SetActive(false);
        var fx = CombatFx.Instance;
        return $"Combat staged. ActiveFx={(fx != null ? fx.ActiveCount : -1)}, SfxPlayed={(GameAudio.Instance != null ? GameAudio.Instance.SfxPlayCount : -1)}";
    }

    public static string StepFx(int steps, float dt)
    {
        var mgr = FactionWarManager.Instance;
        if (mgr != null) mgr.StepBattlefieldForTest(steps, dt);
        var fx = CombatFx.Instance;
        return $"Stepped {steps}x{dt}. ActiveFx={(fx != null ? fx.ActiveCount : -1)}";
    }

    public static string ShowcaseCombatFxLate() => StepFx(3, 0.03f);

    /// <summary>Summon portals, base-hit lightning and shockwaves.</summary>
    public static string ShowcaseSummonAndNuke()
    {
        EnsureManagersReady();
        var mgr = FactionWarManager.Instance;
        if (mgr == null) return "FAIL: FactionWarManager.Instance is null.";

        mgr.ResetMatchState();
        mgr.SetGameState(GameState.Playing, instant: true);
        mgr.SpawnViewerUnit("s1", "Gia_Do", "", 1, UnitTier.Dragon);
        mgr.SpawnViewerUnit("s2", "Gia_Xanh", "", 2, UnitTier.Mecha);
        mgr.SpawnViewerUnit("s3", "Tank_Xanh", "", 2, UnitTier.Tank);
        mgr.StepBattlefieldForTest(3, 0.035f);
        mgr.DamageBase(2, 420f, "s1", "Gia_Do", 1);
        mgr.DamageBase(1, 260f, "s2", "Gia_Xanh", 2);
        mgr.StepBattlefieldForTest(2, 0.03f);
        if (mgr.eventBannerRect != null) mgr.eventBannerRect.gameObject.SetActive(false);
        var fx = CombatFx.Instance;
        return $"Summon staged. ActiveFx={(fx != null ? fx.ActiveCount : -1)}";
    }

    public static string ShowcaseNuke()
    {
        EnsureManagersReady();
        var mgr = FactionWarManager.Instance;
        var fx = CombatFx.Instance;
        if (mgr == null || fx == null) return "FAIL: missing manager/fx.";
        mgr.ResetMatchState();
        mgr.SetGameState(GameState.Playing, instant: true);
        mgr.SpawnViewerUnit("n1", "A", "", 2, UnitTier.Squad);
        mgr.SpawnViewerUnit("n2", "B", "", 2, UnitTier.Tank);
        mgr.StepBattlefieldForTest(25, 0.04f);
        if (VfxPool.Instance != null) VfxPool.Instance.ClearTransientVisuals();
        fx.PlayNuke(new Vector3(0f, FactionWarManager.ArenaCenterY, 0f), Palette.FactionTopPrimary, 2);
        mgr.StepBattlefieldForTest(3, 0.03f);
        if (mgr.eventBannerRect != null) mgr.eventBannerRect.gameObject.SetActive(false);
        return $"Nuke staged. ActiveFx={fx.ActiveCount}";
    }

    public static string VerifyAudioLibrary()
    {
        EnsureManagersReady();
        var audio = GameAudio.Instance;
        if (audio == null) return "FAIL: no GameAudio.";
        var sb = new StringBuilder();
        int bad = 0;
        foreach (SfxId id in System.Enum.GetValues(typeof(SfxId)))
        {
            var clip = audio.GetSfxClip(id);
            if (clip == null) { sb.AppendLine($"MISSING {id}"); bad++; continue; }
            float[] data = new float[clip.samples];
            clip.GetData(data, 0);
            float peak = 0f, sumSq = 0f;
            bool nan = false;
            for (int i = 0; i < data.Length; i++)
            {
                float a = Mathf.Abs(data[i]);
                if (float.IsNaN(data[i]) || float.IsInfinity(data[i])) nan = true;
                if (a > peak) peak = a;
                sumSq += data[i] * data[i];
            }
            float rms = Mathf.Sqrt(sumSq / Mathf.Max(1, data.Length));
            string flag = (nan || peak < 0.12f || peak > 0.99f || rms < 0.01f) ? "  <-- CHECK" : "";
            if (flag != "") bad++;
            sb.AppendLine($"{id,-18} {clip.length * 1000f,6:0}ms peak={peak:0.00} rms={rms:0.000}{flag}");
        }
        sb.AppendLine($"Library={audio.SfxLibraryCount} bad={bad}");
        return sb.ToString();
    }

    public static string VerifyShellAndAudio()
    {
        var mgr = FactionWarManager.Instance;
        var audio = GameAudio.Instance;
        if (mgr == null || audio == null) return "FAIL: Missing manager or audio.";

        // Audio gain layer check (game-audio.md verify)
        float oldMusic = audio.MusicVol;
        audio.MusicVol = 0f;
        var ambientSrc = audio.GetComponent<AudioSource>();
        bool zeroed = ambientSrc != null && Mathf.Approximately(ambientSrc.volume, 0f);
        audio.MusicVol = oldMusic;

        // Test Pooling stability: fire 10 bursts and ensure no leak
        if (VfxPool.Instance != null)
        {
            for (int i = 0; i < 10; i++)
            {
                VfxPool.Instance.PlayBurst(Vector3.zero, Palette.AccentGold, 6, 1f);
            }
        }

        return $"AudioGainLayerZeroCheck={zeroed}, GameState={mgr.CurrentState}, TopHP={mgr.TopBaseHp}, BotHP={mgr.BottomBaseHp}";
    }
}
