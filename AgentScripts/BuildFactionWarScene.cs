using System.IO;
using TMPro;
using Unity.Cinemachine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;
using TikTokFactionWar;

public static class BuildFactionWarScene
{
    public static string BuildAll()
    {
        // 1. Ensure TMP Essential Resources exist & configure Sprite Importers per 2d-games/SKILL.md
        if (!Directory.Exists("Assets/TextMesh Pro/Resources"))
        {
            TMP_PackageResourceImporter.ImportResources(true, false, false);
        }
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        ConfigureGeneratedSpriteImporters();

        // 2. Set highest quality tier on desktop & enable HDR on URP asset
        string[] qNames = QualitySettings.names;
        if (qNames != null && qNames.Length > 0)
        {
            QualitySettings.SetQualityLevel(qNames.Length - 1, true);
        }

        var urpAsset = UniversalRenderPipeline.asset;
        if (urpAsset != null)
        {
            urpAsset.supportsHDR = true;
            EditorUtility.SetDirty(urpAsset);
        }

        // 3. Clean up any previous generated root objects so BuildAll is idempotent
        DestroyIfExists("GameSystems");
        DestroyIfExists("WorldArenaRoot");
        DestroyIfExists("FactionWarCanvas");
        DestroyIfExists("ArenaCinemachineCamera");
        DestroyIfExists("Global Volume");
        DestroyIfExists("FactionWarGlobalVolume");

        // 4. Configure Main Camera + Cinemachine 3.x + Global Light 2D
        Camera mainCam = Camera.main;
        if (mainCam == null)
        {
            GameObject camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            mainCam = camGo.AddComponent<Camera>();
        }
        mainCam.orthographic = true;
        mainCam.orthographicSize = 7.2f;
        mainCam.backgroundColor = Palette.ShadowDark;
        mainCam.clearFlags = CameraClearFlags.SolidColor;
        mainCam.transform.position = new Vector3(0f, 0.35f, -10f);

        var camData = mainCam.GetComponent<UniversalAdditionalCameraData>();
        if (camData == null) camData = mainCam.gameObject.AddComponent<UniversalAdditionalCameraData>();
        camData.renderPostProcessing = true;

        if (mainCam.GetComponent<CinemachineBrain>() == null)
        {
            mainCam.gameObject.AddComponent<CinemachineBrain>();
        }
        if (mainCam.GetComponent<CinemachineImpulseListener>() == null)
        {
            mainCam.gameObject.AddComponent<CinemachineImpulseListener>();
        }

        // Create CinemachineCamera 3.x
        GameObject vcamGo = new GameObject("ArenaCinemachineCamera");
        vcamGo.transform.position = new Vector3(0f, 0.35f, -10f);
        var cmCam = vcamGo.AddComponent<CinemachineCamera>();
        var lens = cmCam.Lens;
        lens.OrthographicSize = 7.2f;
        lens.NearClipPlane = 0.1f;
        lens.FarClipPlane = 100f;
        cmCam.Lens = lens;

        // Ensure Global Light2D exists
        Light2D existingLight = Object.FindAnyObjectByType<Light2D>();
        if (existingLight == null)
        {
            GameObject lightGo = new GameObject("Global Light 2D");
            existingLight = lightGo.AddComponent<Light2D>();
            existingLight.lightType = Light2D.LightType.Global;
        }
        existingLight.color = Palette.PureTint;
        existingLight.intensity = 1.0f;

        // 5. Setup URP Global Volume (Neutral Tonemapping, Bloom, Vignette)
        SetupGlobalVolume();

        // 6. Create GameSystems (Audio, VFX, AvatarCache, LiveServices, FactionWarManager, TikTokLiveBridge)
        GameObject systemsGo = new GameObject("GameSystems");
        systemsGo.AddComponent<GameAudio>();
        systemsGo.AddComponent<VfxPool>();
        systemsGo.AddComponent<CombatFx>();
        systemsGo.AddComponent<AvatarCache>();
        systemsGo.AddComponent<LiveServicesBootstrap>();
        var impulseSource = systemsGo.AddComponent<CinemachineImpulseSource>();
        var warManager = systemsGo.AddComponent<FactionWarManager>();
        var liveBridge = systemsGo.AddComponent<TikTokLiveBridge>();
        warManager.impulseSource = impulseSource;

        // 7. Build 2D World Arena Hierarchy
        BuildWorldArena(warManager);

        // 8. Build 9:16 uGUI Canvas Hierarchy (ScreenSpaceCamera for reliable capture & Bloom interplay)
        BuildCanvasUI(mainCam, warManager, liveBridge);

        // 9. Ensure EventSystem exists
        if (Object.FindAnyObjectByType<EventSystem>() == null)
        {
            GameObject esGo = new GameObject("EventSystem");
            esGo.AddComponent<EventSystem>();
            esGo.AddComponent<InputSystemUIInputModule>();
        }

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();
        AssetDatabase.SaveAssets();

        return "BuildFactionWarScene completed successfully.";
    }

    private static void DestroyIfExists(string name)
    {
        GameObject go = GameObject.Find(name);
        while (go != null)
        {
            Object.DestroyImmediate(go);
            go = GameObject.Find(name);
        }
    }

    private static void SetupGlobalVolume()
    {
        if (!Directory.Exists("Assets/Settings"))
        {
            Directory.CreateDirectory("Assets/Settings");
        }

        const string profilePath = "Assets/Settings/FactionWarGlobalProfile.asset";
        VolumeProfile profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(profilePath);
        if (profile == null)
        {
            profile = ScriptableObject.CreateInstance<VolumeProfile>();
            AssetDatabase.CreateAsset(profile, profilePath);
        }

        if (!profile.TryGet(out Tonemapping tonemapping))
            tonemapping = profile.Add<Tonemapping>(true);
        tonemapping.mode.overrideState = true;
        tonemapping.mode.value = TonemappingMode.Neutral;

        if (!profile.TryGet(out Bloom bloom))
            bloom = profile.Add<Bloom>(true);
        bloom.threshold.overrideState = true;
        bloom.threshold.value = 0.85f;
        bloom.intensity.overrideState = true;
        bloom.intensity.value = 0.85f;
        bloom.scatter.overrideState = true;
        bloom.scatter.value = 0.65f;

        if (!profile.TryGet(out Vignette vignette))
            vignette = profile.Add<Vignette>(true);
        vignette.intensity.overrideState = true;
        vignette.intensity.value = 0.24f;
        vignette.smoothness.overrideState = true;
        vignette.smoothness.value = 0.35f;
        vignette.color.overrideState = true;
        vignette.color.value = Palette.ShadowDark;

        EditorUtility.SetDirty(profile);

        GameObject volGo = new GameObject("FactionWarGlobalVolume");
        volGo.layer = 0;
        Volume vol = volGo.AddComponent<Volume>();
        vol.isGlobal = true;
        vol.priority = 1f;
        vol.sharedProfile = profile;
    }

    private static void EnsureFxMaterial()
    {
        const string dir = "Assets/Resources/Materials";
        const string path = dir + "/FxAdditive.mat";
        if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

        Material m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m == null)
        {
            Shader sh = Shader.Find("Legacy Shaders/Particles/Additive");
            if (sh == null) sh = Shader.Find("Sprites/Default");
            m = new Material(sh) { name = "FxAdditive" };
            AssetDatabase.CreateAsset(m, path);
        }
        if (m.HasProperty("_TintColor")) m.SetColor("_TintColor", new Color(0.5f, 0.5f, 0.5f, 0.5f));
        EditorUtility.SetDirty(m);
    }

    private static void ConfigureGeneratedSpriteImporters()
    {
        const string spritesDir = "Assets/Resources/Sprites";
        if (!Directory.Exists(spritesDir)) return;

        EnsureFxMaterial();

        string[] pngFiles = Directory.GetFiles(spritesDir, "*.png");
        foreach (string file in pngFiles)
        {
            string assetPath = file.Replace('\\', '/');
            TextureImporter ti = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (ti == null) continue;

            bool needsReimport = false;
            if (Path.GetFileName(file).StartsWith("vfx_") && ti.textureCompression != TextureImporterCompression.Uncompressed)
            {
                ti.textureCompression = TextureImporterCompression.Uncompressed;
                needsReimport = true;
            }
            if (ti.textureType != TextureImporterType.Sprite)
            {
                ti.textureType = TextureImporterType.Sprite;
                needsReimport = true;
            }
            if (ti.spriteImportMode != SpriteImportMode.Single)
            {
                ti.spriteImportMode = SpriteImportMode.Single;
                needsReimport = true;
            }
            if (!ti.alphaIsTransparency)
            {
                ti.alphaIsTransparency = true;
                needsReimport = true;
            }
            if (ti.mipmapEnabled)
            {
                ti.mipmapEnabled = false;
                needsReimport = true;
            }
            if (ti.filterMode != FilterMode.Bilinear)
            {
                ti.filterMode = FilterMode.Bilinear;
                needsReimport = true;
            }
            if (Mathf.Abs(ti.spritePixelsPerUnit - 100f) > 0.1f)
            {
                ti.spritePixelsPerUnit = 100f;
                needsReimport = true;
            }

            if (needsReimport)
            {
                ti.SaveAndReimport();
            }
        }
    }

    private static void BuildWorldArena(FactionWarManager warManager)
    {
        Material spriteMat = Palette.GetSpriteMaterial();
        Sprite fortressSprite = Palette.GetFortressBaseSprite();

        GameObject arenaRoot = new GameObject("WorldArenaRoot");
        warManager.worldArenaRoot = arenaRoot.transform;

        // Layer 0 (Background): High-detail Neon Cyber-Tactical Arena Floor
        CreateSpriteChild(arenaRoot.transform, "ArenaGridFloor", Palette.GetArenaBattlefieldSprite(),
            new Vector3(0f, FactionWarManager.ArenaCenterY, 0f), new Vector3(1.36f, 0.82f, 1f), Palette.PureTint, 0, spriteMat);

        // Layer 1 (Terrain): Subtle Territory Tints & Side Conduit Walls
        CreateSpriteChild(arenaRoot.transform, "TopTerritoryZone", Palette.GetSquareSprite(),
            new Vector3(0f, 3.00f, 0f), new Vector3(7.5f, 2.8f, 1f), Palette.WithAlpha(Palette.FactionTopDark, 0.16f), 1, spriteMat);

        CreateSpriteChild(arenaRoot.transform, "BottomTerritoryZone", Palette.GetSquareSprite(),
            new Vector3(0f, 0.20f, 0f), new Vector3(7.5f, 2.8f, 1f), Palette.WithAlpha(Palette.FactionBottomDark, 0.16f), 1, spriteMat);

        CreateSpriteChild(arenaRoot.transform, "LeftArenaWall", Palette.GetRoundedRectSprite(64, 128, 12, 3),
            new Vector3(-3.85f, FactionWarManager.ArenaCenterY, 0f), new Vector3(0.42f, 5.2f, 1f), Palette.ArenaLaneLine, 2, spriteMat);
        CreateSpriteChild(arenaRoot.transform, "RightArenaWall", Palette.GetRoundedRectSprite(64, 128, 12, 3),
            new Vector3(3.85f, FactionWarManager.ArenaCenterY, 0f), new Vector3(0.42f, 5.2f, 1f), Palette.ArenaLaneLine, 2, spriteMat);

        // Layer 2 (Props): Corner Turret Pylons & Top/Bottom Command Fortresses
        CreateSpriteChild(arenaRoot.transform, "TopLeftPylon", fortressSprite,
            new Vector3(-3.25f, FactionWarManager.TopBaseY, 0f), new Vector3(0.44f, 0.44f, 1f), Color.Lerp(Palette.PureTint, Palette.FactionTopPrimary, 0.35f), 4, spriteMat);
        CreateSpriteChild(arenaRoot.transform, "TopRightPylon", fortressSprite,
            new Vector3(3.25f, FactionWarManager.TopBaseY, 0f), new Vector3(0.44f, 0.44f, 1f), Color.Lerp(Palette.PureTint, Palette.FactionTopPrimary, 0.35f), 4, spriteMat);

        SpriteRenderer blPylon = CreateSpriteChild(arenaRoot.transform, "BottomLeftPylon", fortressSprite,
            new Vector3(-3.25f, FactionWarManager.BottomBaseY, 0f), new Vector3(0.44f, 0.44f, 1f), Color.Lerp(Palette.PureTint, Palette.FactionBottomPrimary, 0.35f), 4, spriteMat);
        blPylon.flipY = true;
        SpriteRenderer brPylon = CreateSpriteChild(arenaRoot.transform, "BottomRightPylon", fortressSprite,
            new Vector3(3.25f, FactionWarManager.BottomBaseY, 0f), new Vector3(0.44f, 0.44f, 1f), Color.Lerp(Palette.PureTint, Palette.FactionBottomPrimary, 0.35f), 4, spriteMat);
        brPylon.flipY = true;

        // Top Base Fortress (Twin Plasma Cannons pointing DOWN toward battlefield)
        CreateSpriteChild(arenaRoot.transform, "TopBasePlatform", Palette.GetRingSprite(64, 7),
            new Vector3(0f, FactionWarManager.TopBaseY, 0f), new Vector3(2.45f, 2.45f, 1f), Palette.WithAlpha(Palette.FactionTopPrimary, 0.78f), 3, spriteMat);
        warManager.topBaseCoreRenderer = CreateSpriteChild(arenaRoot.transform, "TopBaseCore", fortressSprite,
            new Vector3(0f, FactionWarManager.TopBaseY, 0f), new Vector3(1.05f, 1.05f, 1f), Color.Lerp(Palette.PureTint, Palette.FactionTopSecondary, 0.25f), 5, spriteMat);
        BoxCollider2D topCol = warManager.topBaseCoreRenderer.gameObject.AddComponent<BoxCollider2D>();
        topCol.isTrigger = true;
        topCol.size = new Vector2(2.1f, 2.1f);
        warManager.topBaseEmblemRenderer = CreateSpriteChild(warManager.topBaseCoreRenderer.transform, "TopBaseEmblem", Palette.GetEmblemSprite("star"),
            new Vector3(0f, 0.08f, 0f), new Vector3(0.36f, 0.36f, 1f), Palette.FactionTopPrimary, 6, spriteMat);

        // Bottom Base Fortress (Twin Plasma Cannons pointing UP toward battlefield via flipY)
        CreateSpriteChild(arenaRoot.transform, "BottomBasePlatform", Palette.GetRingSprite(64, 7),
            new Vector3(0f, FactionWarManager.BottomBaseY, 0f), new Vector3(2.45f, 2.45f, 1f), Palette.WithAlpha(Palette.FactionBottomPrimary, 0.78f), 3, spriteMat);
        warManager.bottomBaseCoreRenderer = CreateSpriteChild(arenaRoot.transform, "BottomBaseCore", fortressSprite,
            new Vector3(0f, FactionWarManager.BottomBaseY, 0f), new Vector3(1.05f, 1.05f, 1f), Color.Lerp(Palette.PureTint, Palette.FactionBottomSecondary, 0.25f), 5, spriteMat);
        warManager.bottomBaseCoreRenderer.flipY = true;
        BoxCollider2D botCol = warManager.bottomBaseCoreRenderer.gameObject.AddComponent<BoxCollider2D>();
        botCol.isTrigger = true;
        botCol.size = new Vector2(2.1f, 2.1f);
        warManager.bottomBaseEmblemRenderer = CreateSpriteChild(warManager.bottomBaseCoreRenderer.transform, "BottomBaseEmblem", Palette.GetEmblemSprite("shield"),
            new Vector3(0f, -0.08f, 0f), new Vector3(0.36f, 0.36f, 1f), Palette.FactionBottomPrimary, 6, spriteMat);

        // Layer 4 (Foreground): Tug-of-War Frontline Divider Beam
        GameObject frontlineGo = new GameObject("FrontlineBar");
        frontlineGo.transform.SetParent(arenaRoot.transform, false);
        frontlineGo.transform.position = new Vector3(0f, FactionWarManager.ArenaCenterY, 0f);
        warManager.frontlineBarTransform = frontlineGo.transform;

        warManager.frontlineGlowRenderer = CreateSpriteChild(frontlineGo.transform, "FrontlineLaser", Palette.GetSquareSprite(),
            Vector3.zero, new Vector3(7.4f, 0.14f, 1f), Palette.AccentGold, 20, spriteMat);
        CreateSpriteChild(frontlineGo.transform, "FrontlineCenterNode", Palette.GetEmblemSprite("diamond"),
            Vector3.zero, new Vector3(0.62f, 0.62f, 1f), Palette.AccentGold, 21, spriteMat);
    }

    private static SpriteRenderer CreateSpriteChild(Transform parent, string name, Sprite sprite, Vector3 localPos, Vector3 localScale, Color color, int sortingOrder, Material mat)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        go.transform.localScale = localScale;
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sharedMaterial = mat;
        sr.sprite = sprite;
        sr.color = color;
        sr.sortingOrder = sortingOrder;
        return sr;
    }

    private static void BuildCanvasUI(Camera mainCam, FactionWarManager warManager, TikTokLiveBridge liveBridge)
    {
        GameObject canvasGo = new GameObject("FactionWarCanvas");
        Canvas canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceCamera;
        canvas.worldCamera = mainCam;
        canvas.planeDistance = 5f;
        canvas.sortingOrder = 50;

        CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080f, 1920f);
        scaler.matchWidthOrHeight = 0.5f;

        canvasGo.AddComponent<GraphicRaycaster>();

        Sprite panelSprite = Palette.GetRoundedRectSprite(128, 128, 20, 4);
        Sprite barSprite = Palette.GetSquareSprite();

        // =====================================================================
        // 1. GAMEPLAY HUD PANEL
        // =====================================================================
        GameObject hudPanel = CreateUiObject("GameplayHudPanel", canvasGo.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        warManager.gameplayHudPanel = hudPanel;

        // 1A. Top Score & Controls Strip (Anchor Y: 0.915 - 0.968)
        GameObject topHeaderCard = CreateImagePanel("TopScoreHeader", hudPanel.transform,
            new Vector2(0.03f, 0.915f), new Vector2(0.97f, 0.968f), panelSprite, Palette.PanelBackground);
        warManager.scoreHeaderText = CreateStyledText("ScoreHeaderText", topHeaderCard.transform,
            "TỈ SỐ: PHE ĐỎ [0]  —  [0] PHE XANH", 25f, Palette.AccentGold, TextAlignmentOptions.Center,
            new Vector2(0.02f, 0.05f), new Vector2(0.76f, 0.95f));

        CreateStyledButton("PauseBtn", topHeaderCard.transform, "[MENU]", 21f,
            Palette.ArenaLaneLine, Palette.NeutralLight, panelSprite,
            new Vector2(0.78f, 0.12f), new Vector2(0.98f, 0.88f),
            () => warManager.TogglePause());

        // 1B. Top Faction Health Bar (Anchor Y: 0.835 - 0.905)
        GameObject topBarContainer = CreateImagePanel("TopFactionBarCard", hudPanel.transform,
            new Vector2(0.03f, 0.835f), new Vector2(0.97f, 0.905f), panelSprite, Palette.PanelBackground);

        warManager.topFactionTitleText = CreateStyledText("TopFactionTitle", topBarContainer.transform,
            "PHE ĐỎ (GÕ 1)", 26f, Palette.FactionTopPrimary, TextAlignmentOptions.Left,
            new Vector2(0.03f, 0.52f), new Vector2(0.55f, 0.96f));

        warManager.topHpValueText = CreateStyledText("TopHpValue", topBarContainer.transform,
            "5000 / 5000 HP", 24f, Palette.NeutralLight, TextAlignmentOptions.Right,
            new Vector2(0.55f, 0.52f), new Vector2(0.97f, 0.96f));

        GameObject topHpTrack = CreateImagePanel("TopHpTrack", topBarContainer.transform,
            new Vector2(0.03f, 0.12f), new Vector2(0.97f, 0.46f), barSprite, Palette.ShadowDark);
        GameObject topHpFillGo = CreateImagePanel("TopHpFill", topHpTrack.transform,
            Vector2.zero, Vector2.one, barSprite, Palette.FactionTopPrimary);
        Image topHpFill = topHpFillGo.GetComponent<Image>();
        topHpFill.type = Image.Type.Filled;
        topHpFill.fillMethod = Image.FillMethod.Horizontal;
        topHpFill.fillOrigin = (int)Image.OriginHorizontal.Left;
        topHpFill.fillAmount = 1f;
        warManager.topHpFillImage = topHpFill;

        // 1C. Left Gift Legend Card (Anchor X: 0.02 - 0.30, Y: 0.50 - 0.77)
        GameObject giftCard = CreateImagePanel("GiftLegendCard", hudPanel.transform,
            new Vector2(0.02f, 0.50f), new Vector2(0.30f, 0.77f), panelSprite, Palette.WithAlpha(Palette.ShadowDark, 0.86f));
        CreateStyledText("GiftLegendText", giftCard.transform,
            "[BẢNG QUY ĐỔI QUÀ]\n" +
            ">> Gõ 1/2: +1 Lính\n" +
            ">> 1 Xu: Tiểu Đội x5\n" +
            ">> 10 Xu: Xe Tăng\n" +
            ">> 100 Xu: Siêu Mecha\n" +
            ">> 500 Xu: Rồng Lửa\n" +
            ">> 2000 Xu: Hạt Nhân",
            19f, Palette.NeutralLight, TextAlignmentOptions.TopLeft,
            new Vector2(0.07f, 0.05f), new Vector2(0.95f, 0.95f));

        // 1D. Bottom Faction Health Bar (Anchor Y: 0.315 - 0.385)
        GameObject botBarContainer = CreateImagePanel("BottomFactionBarCard", hudPanel.transform,
            new Vector2(0.03f, 0.315f), new Vector2(0.97f, 0.385f), panelSprite, Palette.PanelBackground);

        warManager.bottomFactionTitleText = CreateStyledText("BottomFactionTitle", botBarContainer.transform,
            "PHE XANH (GÕ 2)", 26f, Palette.FactionBottomPrimary, TextAlignmentOptions.Left,
            new Vector2(0.03f, 0.52f), new Vector2(0.55f, 0.96f));

        warManager.bottomHpValueText = CreateStyledText("BottomHpValue", botBarContainer.transform,
            "5000 / 5000 HP", 24f, Palette.NeutralLight, TextAlignmentOptions.Right,
            new Vector2(0.55f, 0.52f), new Vector2(0.97f, 0.96f));

        GameObject botHpTrack = CreateImagePanel("BottomHpTrack", botBarContainer.transform,
            new Vector2(0.03f, 0.12f), new Vector2(0.97f, 0.46f), barSprite, Palette.ShadowDark);
        GameObject botHpFillGo = CreateImagePanel("BottomHpFill", botHpTrack.transform,
            Vector2.zero, Vector2.one, barSprite, Palette.FactionBottomPrimary);
        Image botHpFill = botHpFillGo.GetComponent<Image>();
        botHpFill.type = Image.Type.Filled;
        botHpFill.fillMethod = Image.FillMethod.Horizontal;
        botHpFill.fillOrigin = (int)Image.OriginHorizontal.Left;
        botHpFill.fillAmount = 1f;
        warManager.bottomHpFillImage = botHpFill;

        // 1E. MVP Live Leaderboard Strip (Anchor Y: 0.255 - 0.308)
        GameObject mvpCard = CreateImagePanel("MvpLeaderboardStrip", hudPanel.transform,
            new Vector2(0.03f, 0.255f), new Vector2(0.97f, 0.308f), panelSprite, Palette.PanelBackground);
        warManager.mvpBoardText = CreateStyledText("MvpBoardText", mvpCard.transform,
            "[TOP MVP] Comment '1' hoặc '2' để xuất quân lên bảng vàng!", 21f, Palette.AccentGold, TextAlignmentOptions.Center,
            new Vector2(0.02f, 0.05f), new Vector2(0.98f, 0.95f));

        // 1F. Center Milestone Event Banner (Anchor Y: 0.56 - 0.64)
        GameObject bannerCard = CreateImagePanel("MilestoneEventBanner", hudPanel.transform,
            new Vector2(0.06f, 0.56f), new Vector2(0.94f, 0.64f), panelSprite, Palette.WithAlpha(Palette.ShadowDark, 0.94f));
        warManager.eventBannerRect = bannerCard.GetComponent<RectTransform>();
        warManager.eventBannerText = CreateStyledText("EventBannerText", bannerCard.transform,
            ">> ĐẠI CHIẾN BẮT ĐẦU!", 30f, Palette.AccentGold, TextAlignmentOptions.Center,
            new Vector2(0.03f, 0.08f), new Vector2(0.97f, 0.92f));
        bannerCard.SetActive(false);

        // 1G. Bottom Streamer Sim & Control Dock (Anchor Y: 0.02 - 0.242)
        GameObject controlDock = CreateImagePanel("StreamerControlDock", hudPanel.transform,
            new Vector2(0.03f, 0.02f), new Vector2(0.97f, 0.242f), panelSprite, Palette.PanelBackground);

        // Row 1: Top Faction Quick Spawn Buttons
        CreateStyledButton("BtnRedInfantry", controlDock.transform, "+Lính Đỏ (1)", 19f,
            Palette.FactionTopDark, Palette.FactionTopSecondary, panelSprite,
            new Vector2(0.02f, 0.68f), new Vector2(0.25f, 0.95f),
            () => warManager.SpawnViewerUnit("top_hero", "ChiếnBinhĐỏ", "", 1, UnitTier.Infantry));

        CreateStyledButton("BtnRedTank", controlDock.transform, "+Tank Đỏ (W)", 19f,
            Palette.FactionTopDark, Palette.FactionTopSecondary, panelSprite,
            new Vector2(0.265f, 0.68f), new Vector2(0.495f, 0.95f),
            () => warManager.SpawnViewerUnit("top_tank", "TankĐỏ_VIP", "", 1, UnitTier.Tank));

        CreateStyledButton("BtnRedDragon", controlDock.transform, "+Rồng Đỏ (R)", 19f,
            Palette.FactionTopPrimary, Palette.NeutralLight, panelSprite,
            new Vector2(0.51f, 0.68f), new Vector2(0.74f, 0.95f),
            () => warManager.SpawnViewerUnit("top_dragon", "RồngĐỏ_Boss", "", 1, UnitTier.Dragon));

        CreateStyledButton("BtnRedNuke", controlDock.transform, "[NUKE] Đỏ (T)", 19f,
            Palette.AccentOrange, Palette.ShadowDark, panelSprite,
            new Vector2(0.755f, 0.68f), new Vector2(0.98f, 0.95f),
            () => warManager.TriggerTacticalNuke("top_whale", "ĐạiGia_PheĐỏ", 1));

        // Row 2: Bottom Faction Quick Spawn Buttons
        CreateStyledButton("BtnBlueInfantry", controlDock.transform, "+Lính Xanh (2)", 19f,
            Palette.FactionBottomDark, Palette.FactionBottomSecondary, panelSprite,
            new Vector2(0.02f, 0.37f), new Vector2(0.25f, 0.64f),
            () => warManager.SpawnViewerUnit("bot_hero", "ChiếnBinhXanh", "", 2, UnitTier.Infantry));

        CreateStyledButton("BtnBlueTank", controlDock.transform, "+Tank Xanh (I)", 19f,
            Palette.FactionBottomDark, Palette.FactionBottomSecondary, panelSprite,
            new Vector2(0.265f, 0.37f), new Vector2(0.495f, 0.64f),
            () => warManager.SpawnViewerUnit("bot_tank", "TankXanh_VIP", "", 2, UnitTier.Tank));

        CreateStyledButton("BtnBlueDragon", controlDock.transform, "+Rồng Xanh (P)", 19f,
            Palette.FactionBottomPrimary, Palette.ShadowDark, panelSprite,
            new Vector2(0.51f, 0.37f), new Vector2(0.74f, 0.64f),
            () => warManager.SpawnViewerUnit("bot_dragon", "RồngXanh_Boss", "", 2, UnitTier.Dragon));

        CreateStyledButton("BtnBlueNuke", controlDock.transform, "[NUKE] Xanh (L)", 19f,
            Palette.AccentGold, Palette.ShadowDark, panelSprite,
            new Vector2(0.755f, 0.37f), new Vector2(0.98f, 0.64f),
            () => warManager.TriggerTacticalNuke("bot_whale", "ĐạiGia_PheXanh", 2));

        // Row 3: Bot Swarm Toggle + Theme Preset Switcher + Status
        CreateStyledButton("BtnToggleBots", controlDock.transform, "[BOT] Bật/Tắt (B)", 18.5f,
            Palette.ArenaLaneLine, Palette.AccentGreen, panelSprite,
            new Vector2(0.02f, 0.05f), new Vector2(0.32f, 0.33f),
            () => liveBridge.ToggleBotSwarm());

        CreateStyledButton("BtnCycleTheme", controlDock.transform, "[CHỦ ĐỀ] Đổi Cờ (F1-F4)", 18.5f,
            Palette.ArenaLaneLine, Palette.AccentGold, panelSprite,
            new Vector2(0.335f, 0.05f), new Vector2(0.64f, 0.33f),
            () => warManager.CycleThemePreset());

        liveBridge.connectionStatusText = CreateStyledText("ConnectionStatusText", controlDock.transform,
            "Chế độ Giả Lập Live (Bot Swarm: ON)", 16.5f, Palette.NeutralMuted, TextAlignmentOptions.Center,
            new Vector2(0.65f, 0.05f), new Vector2(0.98f, 0.33f));

        // =====================================================================
        // 2. TITLE SCREEN PANEL (game-shell.md worked treatment)
        // =====================================================================
        GameObject titlePanel = CreateImagePanel("TitleScreenPanel", canvasGo.transform,
            Vector2.zero, Vector2.one, barSprite, Palette.WithAlpha(Palette.ShadowDark, 0.72f));
        warManager.titlePanel = titlePanel;

        CreateStyledText("TitleMainHeading", titlePanel.transform,
            "ĐẠI CHIẾN 2 PHE\nTIKTOK LIVE", 68f, Palette.AccentGold, TextAlignmentOptions.Center,
            new Vector2(0.05f, 0.52f), new Vector2(0.95f, 0.74f));

        CreateStyledText("TitleSubHeading", titlePanel.transform,
            "INTERACTIVE TUG-OF-WAR STREAM ARENA", 26f, Palette.FactionBottomSecondary, TextAlignmentOptions.Center,
            new Vector2(0.08f, 0.44f), new Vector2(0.92f, 0.51f));

        GameObject startBtnGo = CreateStyledButton("StartMatchButton", titlePanel.transform,
            "NHẤN SPACE HOẶC BẤM ĐỂ BẮT ĐẦU LIVE", 28f,
            Palette.PanelBackground, Palette.NeutralLight, panelSprite,
            new Vector2(0.10f, 0.22f), new Vector2(0.90f, 0.31f),
            () => warManager.StartMatchFromTitle());
        warManager.titlePromptText = startBtnGo.GetComponentInChildren<TextMeshProUGUI>();
        titlePanel.SetActive(false);

        // =====================================================================
        // 3. PAUSE & SETTINGS PANEL (game-shell.md + game-audio.md sliders)
        // =====================================================================
        GameObject pausePanel = CreateImagePanel("PauseSettingsPanel", canvasGo.transform,
            Vector2.zero, Vector2.one, barSprite, Palette.ScrimOverlay);
        warManager.pausePanel = pausePanel;

        GameObject pauseCard = CreateImagePanel("PauseCard", pausePanel.transform,
            new Vector2(0.10f, 0.26f), new Vector2(0.90f, 0.74f), panelSprite, Palette.PanelBackground);

        CreateStyledText("PauseHeader", pauseCard.transform,
            "TẠM DỪNG & CÀI ĐẶT ÂM THANH", 36f, Palette.AccentGold, TextAlignmentOptions.Center,
            new Vector2(0.05f, 0.80f), new Vector2(0.95f, 0.94f));

        CreateStyledText("MusicLabel", pauseCard.transform,
            "Âm lượng Nhạc nền (MusicVol)", 24f, Palette.NeutralLight, TextAlignmentOptions.Left,
            new Vector2(0.08f, 0.66f), new Vector2(0.92f, 0.76f));
        warManager.musicSlider = CreateStyledSlider("MusicVolSlider", pauseCard.transform,
            new Vector2(0.08f, 0.58f), new Vector2(0.92f, 0.65f), 0.55f, Palette.FactionBottomPrimary, barSprite);

        CreateStyledText("SfxLabel", pauseCard.transform,
            "Âm lượng Hiệu ứng (SFXVol)", 24f, Palette.NeutralLight, TextAlignmentOptions.Left,
            new Vector2(0.08f, 0.44f), new Vector2(0.92f, 0.54f));
        warManager.sfxSlider = CreateStyledSlider("SfxVolSlider", pauseCard.transform,
            new Vector2(0.08f, 0.36f), new Vector2(0.92f, 0.43f), 0.85f, Palette.FactionTopPrimary, barSprite);

        CreateStyledButton("ResumeBtn", pauseCard.transform, "TIẾP TỤC TRẬN ĐẤU", 25f,
            Palette.FactionBottomDark, Palette.NeutralLight, panelSprite,
            new Vector2(0.08f, 0.20f), new Vector2(0.48f, 0.31f),
            () => warManager.TogglePause());

        CreateStyledButton("RestartFromPauseBtn", pauseCard.transform, "VÁN MỚI", 25f,
            Palette.FactionTopDark, Palette.NeutralLight, panelSprite,
            new Vector2(0.52f, 0.20f), new Vector2(0.92f, 0.31f),
            () => warManager.RestartCleanRun());

        CreateStyledButton("TitleFromPauseBtn", pauseCard.transform, "VỀ MÀN HÌNH CHÍNH", 24f,
            Palette.ArenaLaneLine, Palette.AccentGold, panelSprite,
            new Vector2(0.20f, 0.05f), new Vector2(0.80f, 0.16f),
            () => warManager.ReturnToTitle());

        pausePanel.SetActive(false);

        // =====================================================================
        // 4. ENDED / VICTORY PANEL
        // =====================================================================
        GameObject endedPanel = CreateImagePanel("EndedVictoryPanel", canvasGo.transform,
            Vector2.zero, Vector2.one, barSprite, Palette.ScrimOverlay);
        warManager.endedPanel = endedPanel;

        GameObject endedCard = CreateImagePanel("EndedCard", endedPanel.transform,
            new Vector2(0.08f, 0.25f), new Vector2(0.92f, 0.75f), panelSprite, Palette.PanelBackground);

        warManager.endedWinnerText = CreateStyledText("EndedWinnerText", endedCard.transform,
            "[CHIẾN THẮNG] PHE ĐỎ!", 44f, Palette.AccentGold, TextAlignmentOptions.Center,
            new Vector2(0.05f, 0.76f), new Vector2(0.95f, 0.94f));

        warManager.endedStatsText = CreateStyledText("EndedStatsText", endedCard.transform,
            ">> MVP Toàn Trận: ---\n>> Tỉ số chuỗi thắng: 1 - 0", 26f, Palette.NeutralLight, TextAlignmentOptions.Center,
            new Vector2(0.06f, 0.32f), new Vector2(0.94f, 0.74f));

        CreateStyledButton("PlayAgainBtn", endedCard.transform, "CHƠI VÁN TIẾP THEO (SPACE)", 26f,
            Palette.FactionTopPrimary, Palette.NeutralLight, panelSprite,
            new Vector2(0.08f, 0.16f), new Vector2(0.92f, 0.28f),
            () => warManager.RestartCleanRun());

        CreateStyledButton("EndedToTitleBtn", endedCard.transform, "MÀN HÌNH CHÍNH", 23f,
            Palette.ArenaLaneLine, Palette.AccentGold, panelSprite,
            new Vector2(0.22f, 0.03f), new Vector2(0.78f, 0.13f),
            () => warManager.ReturnToTitle());

        endedPanel.SetActive(false);

        // =====================================================================
        // 5. FULLSCREEN FLASH & TRANSITION OVERLAYS
        // =====================================================================
        GameObject flashGo = CreateImagePanel("ScreenFlashOverlay", canvasGo.transform,
            Vector2.zero, Vector2.one, barSprite, Palette.WithAlpha(Palette.AccentGold, 0f));
        warManager.screenFlashOverlay = flashGo.GetComponent<Image>();
        warManager.screenFlashOverlay.raycastTarget = false;
        flashGo.SetActive(false);

        GameObject fadeGo = CreateImagePanel("TransitionFadeOverlay", canvasGo.transform,
            Vector2.zero, Vector2.one, barSprite, Palette.WithAlpha(Palette.ShadowDark, 0f));
        warManager.transitionFadeOverlay = fadeGo.GetComponent<Image>();
        warManager.transitionFadeOverlay.raycastTarget = false;
        fadeGo.SetActive(false);
    }

    private static GameObject CreateUiObject(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = offsetMin;
        rt.offsetMax = offsetMax;
        rt.localScale = Vector3.one;
        return go;
    }

    private static GameObject CreateImagePanel(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Sprite sprite, Color color)
    {
        GameObject go = CreateUiObject(name, parent, anchorMin, anchorMax, Vector2.zero, Vector2.zero);
        Image img = go.AddComponent<Image>();
        img.sprite = sprite;
        if (sprite != null && sprite.border != Vector4.zero)
        {
            img.type = Image.Type.Sliced;
            img.pixelsPerUnitMultiplier = 1f;
        }
        img.color = color;
        img.raycastTarget = false;
        return go;
    }

    private static TextMeshProUGUI CreateStyledText(string name, Transform parent, string content, float fontSize, Color color, TextAlignmentOptions align, Vector2 anchorMin, Vector2 anchorMax)
    {
        GameObject go = CreateUiObject(name, parent, anchorMin, anchorMax, Vector2.zero, Vector2.zero);
        TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
        if (TMP_Settings.defaultFontAsset != null)
        {
            tmp.font = TMP_Settings.defaultFontAsset;
            tmp.fontSharedMaterial = TMP_Settings.defaultFontAsset.material;
        }
        tmp.text = content;
        tmp.fontSize = fontSize;
        tmp.fontStyle = FontStyles.Bold;
        tmp.characterSpacing = 2.5f;
        tmp.color = color;
        tmp.alignment = align;
        tmp.raycastTarget = false;
        return tmp;
    }

    private static GameObject CreateStyledButton(string name, Transform parent, string label, float fontSize, Color bgColor, Color textColor, Sprite bgSprite, Vector2 anchorMin, Vector2 anchorMax, UnityEngine.Events.UnityAction onClick)
    {
        GameObject go = CreateUiObject(name, parent, anchorMin, anchorMax, Vector2.zero, Vector2.zero);
        Image img = go.AddComponent<Image>();
        img.sprite = bgSprite;
        if (bgSprite != null && bgSprite.border != Vector4.zero)
        {
            img.type = Image.Type.Sliced;
            img.pixelsPerUnitMultiplier = 1f;
        }
        img.color = bgColor;
        img.raycastTarget = true;

        Button btn = go.AddComponent<Button>();
        btn.targetGraphic = img;
        if (onClick != null)
        {
            btn.onClick.AddListener(onClick);
        }

        CreateStyledText("Label", go.transform, label, fontSize, textColor, TextAlignmentOptions.Center,
            new Vector2(0.04f, 0.06f), new Vector2(0.96f, 0.94f));

        return go;
    }

    private static Slider CreateStyledSlider(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, float initialVal, Color fillColor, Sprite barSprite)
    {
        GameObject sliderGo = CreateUiObject(name, parent, anchorMin, anchorMax, Vector2.zero, Vector2.zero);
        Image bg = sliderGo.AddComponent<Image>();
        bg.sprite = barSprite;
        bg.color = Palette.ShadowDark;
        bg.raycastTarget = true;

        GameObject fillArea = CreateUiObject("FillArea", sliderGo.transform, Vector2.zero, Vector2.one, new Vector2(6f, 6f), new Vector2(-6f, -6f));
        GameObject fillGo = CreateImagePanel("Fill", fillArea.transform, Vector2.zero, Vector2.one, barSprite, fillColor);

        Slider slider = sliderGo.AddComponent<Slider>();
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.value = initialVal;
        slider.fillRect = fillGo.GetComponent<RectTransform>();
        slider.targetGraphic = bg;
        return slider;
    }
}
