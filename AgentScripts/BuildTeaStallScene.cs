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
using TikTokFactionWar;
using TikTokTeaStall;

public static class BuildTeaStallScene
{
    public static string BuildAll()
    {
        // 1. Kiểm tra TextMesh Pro Resources & Cấu hình Sprite Importers cho TeaStall
        if (!Directory.Exists("Assets/TextMesh Pro/Resources"))
        {
            TMP_PackageResourceImporter.ImportResources(true, false, false);
        }
        ConfigureTeaStallSpriteImporters();
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

        // 2. Cấu hình chất lượng cao nhất & URP HDR
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

        // 3. Dọn dẹp các GameObject cũ để đảm bảo Idempotent
        DestroyIfExists("TeaStallSystems");
        DestroyIfExists("TeaStallEnvironment");
        DestroyIfExists("TeaStallHudRoot");
        DestroyIfExists("TeaStallGlobalVolume");
        DestroyIfExists("TeaStallCinemachineCamera");
        DestroyIfExists("GameSystems");
        DestroyIfExists("WorldArenaRoot");
        DestroyIfExists("FactionWarCanvas");

        // 4. Thiết lập Main Camera 9:16 cho TikTok Live dọc
        Camera mainCam = Camera.main;
        if (mainCam == null)
        {
            GameObject camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            mainCam = camGo.AddComponent<Camera>();
        }
        mainCam.orthographic = true;
        mainCam.orthographicSize = 7.5f; // Khung nhìn dọc 9:16 rộng rãi, trọn vẹn vỉa hè
        mainCam.backgroundColor = new Color(0.12f, 0.10f, 0.12f);
        mainCam.clearFlags = CameraClearFlags.SolidColor;
        mainCam.transform.position = new Vector3(0f, 1.8f, -10f);

        var camData = mainCam.GetComponent<UniversalAdditionalCameraData>();
        if (camData == null) camData = mainCam.gameObject.AddComponent<UniversalAdditionalCameraData>();
        camData.renderPostProcessing = true;

        if (mainCam.GetComponent<CinemachineBrain>() == null)
        {
            mainCam.gameObject.AddComponent<CinemachineBrain>();
        }

        // Tạo CinemachineCamera 3.x
        GameObject vcamGo = new GameObject("TeaStallCinemachineCamera");
        vcamGo.transform.position = new Vector3(0f, 1.8f, -10f);
        var cmCam = vcamGo.AddComponent<CinemachineCamera>();
        var lens = cmCam.Lens;
        lens.OrthographicSize = 7.5f;
        lens.NearClipPlane = 0.1f;
        lens.FarClipPlane = 100f;
        cmCam.Lens = lens;

        // 5. Ánh sáng 2D ấm cúng phố cổ Hà Nội
        Light2D globalLight = Object.FindAnyObjectByType<Light2D>();
        if (globalLight == null)
        {
            GameObject lightGo = new GameObject("Global Light 2D");
            globalLight = lightGo.AddComponent<Light2D>();
            globalLight.lightType = Light2D.LightType.Global;
        }
        globalLight.color = new Color(1.0f, 0.94f, 0.86f);
        globalLight.intensity = 1.05f;

        // 6. Setup URP Global Volume (Bloom để viền ZingMe phát sáng + Vignette hoài cổ)
        SetupTeaStallVolume();

        // 7. Tạo Hệ thống Quản lý GameSystems
        GameObject systemsGo = new GameObject("TeaStallSystems");
        var audioAgent = systemsGo.AddComponent<TeaStallAudio>();
        var avatarCache = systemsGo.AddComponent<AvatarCache>();
        var liveBridge = systemsGo.AddComponent<TikTokLiveBridge>();
        var stallManager = systemsGo.AddComponent<TeaStallManager>();

        // 8. Dựng Môi trường Vỉa hè Phố Cổ Hà Nội 2.5D
        BuildHanoiStreetEnvironment(stallManager);

        // 9. Ensure EventSystem
        if (Object.FindAnyObjectByType<EventSystem>() == null)
        {
            GameObject esGo = new GameObject("EventSystem");
            esGo.AddComponent<EventSystem>();
            esGo.AddComponent<InputSystemUIInputModule>();
        }

        // 10. Lưu Scene vào cả 2 đường dẫn
        if (!Directory.Exists("Assets/Scenes")) Directory.CreateDirectory("Assets/Scenes");

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(), "Assets/Scenes/TeaStallScene.unity");
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(), "Assets/Scenes/SampleScene.unity");
        AssetDatabase.SaveAssets();

        return "BuildTeaStallScene completed successfully. Saved to TeaStallScene.unity and SampleScene.unity.";
    }

    private static void ConfigureTeaStallSpriteImporters()
    {
        const string spritesDir = "Assets/Resources/TeaStall/Sprites";
        if (!Directory.Exists(spritesDir)) return;

        string[] pngFiles = Directory.GetFiles(spritesDir, "*.png");
        bool anyChanged = false;

        foreach (string file in pngFiles)
        {
            string assetPath = file.Replace('\\', '/');
            TextureImporter ti = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (ti == null) continue;

            bool needsReimport = false;
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

            if (needsReimport)
            {
                ti.SaveAndReimport();
                anyChanged = true;
            }
        }

        if (anyChanged)
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        }
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

    private static void SetupTeaStallVolume()
    {
        if (!Directory.Exists("Assets/Settings")) Directory.CreateDirectory("Assets/Settings");

        const string profilePath = "Assets/Settings/TeaStallGlobalProfile.asset";
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
        bloom.intensity.value = 0.95f;
        bloom.scatter.overrideState = true;
        bloom.scatter.value = 0.65f;

        if (!profile.TryGet(out Vignette vignette))
            vignette = profile.Add<Vignette>(true);
        vignette.intensity.overrideState = true;
        vignette.intensity.value = 0.22f;
        vignette.smoothness.overrideState = true;
        vignette.smoothness.value = 0.35f;
        vignette.color.overrideState = true;
        vignette.color.value = new Color(0.15f, 0.08f, 0.05f);

        EditorUtility.SetDirty(profile);

        GameObject volGo = new GameObject("TeaStallGlobalVolume");
        var volume = volGo.AddComponent<Volume>();
        volume.isGlobal = true;
        volume.profile = profile;
    }

    private static void BuildHanoiStreetEnvironment(TeaStallManager stallManager)
    {
        GameObject envRoot = new GameObject("TeaStallEnvironment");
        stallManager.EnvironmentParent = envRoot.transform;

        // Nhóm Khách Hàng
        GameObject custRoot = new GameObject("CustomersRoot");
        stallManager.CustomerParent = custRoot.transform;

        // ==================== 1. ẢNH NỀN PHỐ CỔ (NẾU CÓ) HOẶC TƯỜNG PROCEDURAL ====================
        Sprite streetBgSprite = Resources.Load<Sprite>("TeaStall/Sprites/street_tea_bg");
        if (streetBgSprite != null)
        {
            GameObject bgGo = new GameObject("HanoiStreetArtBackground");
            bgGo.transform.SetParent(envRoot.transform, false);
            bgGo.transform.position = new Vector3(0f, 1.8f, 0f);
            bgGo.transform.localScale = new Vector3(1.0f, 1.0f, 1f); // Fit camera
            var bgSr = bgGo.AddComponent<SpriteRenderer>();
            bgSr.sprite = streetBgSprite;
            bgSr.sortingOrder = 0;

            // Tự động scale vừa khít khung hình dọc 9:16 (ortho size 7.5 => height 15 units)
            float spriteH = streetBgSprite.rect.height / streetBgSprite.pixelsPerUnit;
            if (spriteH > 0.01f)
            {
                float targetH = 15.2f;
                float s = targetH / spriteH;
                bgGo.transform.localScale = new Vector3(s, s, 1f);
            }
        }
        else
        {
            // Fallback Tường vàng phố cổ
            GameObject wallGo = new GameObject("HanoiOldWall");
            wallGo.transform.SetParent(envRoot.transform, false);
            wallGo.transform.position = new Vector3(0f, 4.2f, 0f);
            wallGo.transform.localScale = new Vector3(9.5f, 6.5f, 1f);
            var wallSr = wallGo.AddComponent<SpriteRenderer>();
            wallSr.sprite = TeaStallPalette.GetFallbackSprite(FallbackSpriteType.RoundedRect);
            wallSr.color = TeaStallPalette.HanoiWallYellow;
            wallSr.sortingOrder = 1;

            // Mái ngói rêu phong
            GameObject roofGo = new GameObject("HanoiTileRoof");
            roofGo.transform.SetParent(envRoot.transform, false);
            roofGo.transform.position = new Vector3(0f, 7.4f, 0f);
            roofGo.transform.localScale = new Vector3(10.0f, 0.9f, 1f);
            var roofSr = roofGo.AddComponent<SpriteRenderer>();
            roofSr.sprite = TeaStallPalette.GetFallbackSprite(FallbackSpriteType.RoundedRect);
            roofSr.color = TeaStallPalette.HanoiRoofRed;
            roofSr.sortingOrder = 3;

            // 2 Cửa sổ gỗ xanh chớp
            CreateHanoiWindow(envRoot.transform, new Vector3(-2.6f, 3.8f, 0f));
            CreateHanoiWindow(envRoot.transform, new Vector3(2.6f, 3.8f, 0f));

            // Tán lá bàng rủ bóng mát
            CreateBangTreeCanopy(envRoot.transform, new Vector3(-3.8f, 5.5f, 0f));

            // Cột điện phố cổ
            CreatePowerPole(envRoot.transform, new Vector3(3.8f, 4.0f, 0f));
        }

        // ==================== 2. MẶT ĐƯỜNG & VỈA HÈ (CHỈ VẼ FALLBACK NẾU CHƯA CÓ BG) ====================
        if (streetBgSprite == null)
        {
            GameObject pavementGo = new GameObject("HanoiPavement");
            pavementGo.transform.SetParent(envRoot.transform, false);
            pavementGo.transform.position = new Vector3(0f, -0.6f, 0f);
            pavementGo.transform.localScale = new Vector3(9.5f, 3.4f, 1f);
            var pavSr = pavementGo.AddComponent<SpriteRenderer>();
            pavSr.sprite = TeaStallPalette.GetFallbackSprite(FallbackSpriteType.RoundedRect);
            pavSr.color = new Color(TeaStallPalette.PavementDark.r, TeaStallPalette.PavementDark.g, TeaStallPalette.PavementDark.b, 0.45f);
            pavSr.sortingOrder = 2;

            // Vạch sơn trắng kẻ ranh giới vỉa hè
            GameObject curbLineGo = new GameObject("PavementCurbLine");
            curbLineGo.transform.SetParent(envRoot.transform, false);
            curbLineGo.transform.position = new Vector3(0f, -2.2f, 0f);
            curbLineGo.transform.localScale = new Vector3(9.5f, 0.08f, 1f);
            var curbSr = curbLineGo.AddComponent<SpriteRenderer>();
            curbSr.sprite = TeaStallPalette.GetFallbackSprite(FallbackSpriteType.RoundedRect);
            curbSr.color = Color.white;
            curbSr.sortingOrder = 3;
        }

        // ==================== 3. QUẦY TRÀ ĐÁ CỦA BÀ TÁM ====================
        Sprite counterSprite = Resources.Load<Sprite>("TeaStall/Sprites/prop_tea_counter");
        GameObject stallTableGo = new GameObject("TeaStallCounterTable");
        stallTableGo.transform.SetParent(envRoot.transform, false);
        stallTableGo.transform.position = new Vector3(0f, 1.25f, 0f);
        var tableSr = stallTableGo.AddComponent<SpriteRenderer>();
        tableSr.sortingOrder = 6;

        if (counterSprite != null)
        {
            tableSr.sprite = counterSprite;
            tableSr.transform.localScale = new Vector3(0.85f, 0.85f, 1f);
        }
        else
        {
            stallTableGo.transform.localScale = new Vector3(3.4f, 0.75f, 1f);
            tableSr.sprite = TeaStallPalette.GetFallbackSprite(FallbackSpriteType.RoundedRect);
            tableSr.color = TeaStallPalette.AntiqueWood;

            // Biển hiệu vỉa hè: "TRÀ ĐÁ - THUỐC LÀO - KẸO LẠC"
            GameObject signboardGo = new GameObject("Signboard");
            signboardGo.transform.SetParent(stallTableGo.transform, false);
            signboardGo.transform.localPosition = new Vector3(0f, -0.05f, 0f);
            var signTmp = signboardGo.AddComponent<TextMeshPro>();
            signTmp.alignment = TextAlignmentOptions.Center;
            signTmp.fontSize = 2.0f;
            signTmp.fontStyle = FontStyles.Bold;
            signTmp.color = TeaStallPalette.NeonGold;
            signTmp.outlineWidth = 0.25f;
            signTmp.outlineColor = Color.black;
            signTmp.sortingOrder = 7;
            signTmp.text = "🍵 TRÀ ĐÁ BÀ TÁM • THUỐC LÀO TIÊN LÃNG 🍵";

            // Props ấm tích, phích Rạng Đông, hộp kẹo lạc, điếu cày
            CreateStallProp(envRoot.transform, new Vector3(-1.0f, 1.6f, 0f), FallbackSpriteType.Circle, new Color(0.85f, 0.75f, 0.50f), new Vector3(0.55f, 0.55f, 1f), 8);
            CreateStallProp(envRoot.transform, new Vector3(-0.55f, 1.65f, 0f), FallbackSpriteType.RoundedRect, TeaStallPalette.RangDongRed, new Vector3(0.35f, 0.65f, 1f), 8);
            CreateStallProp(envRoot.transform, new Vector3(0.55f, 1.6f, 0f), FallbackSpriteType.RoundedRect, new Color(0.95f, 0.90f, 0.75f, 0.85f), new Vector3(0.45f, 0.45f, 1f), 8);
            CreateStallProp(envRoot.transform, new Vector3(1.1f, 1.55f, 0f), FallbackSpriteType.DieuCayPipe, Color.white, new Vector3(0.55f, 0.55f, 1f), 8);
        }

        // ==================== 4. NPC BÀ TÁM TRÀ ĐÁ ====================
        GameObject teaLadyGo = new GameObject("TeaLadyNpc");
        teaLadyGo.transform.SetParent(envRoot.transform, false);
        var teaLadyAgent = teaLadyGo.AddComponent<StreetNpcAgent>();
        teaLadyAgent.SetupAsTeaLady(new Vector3(0f, 1.85f, 0f));
        stallManager.TeaLady = teaLadyAgent;

        // Đèn lồng vàng ấm cúng chiếu sáng quầy Bà Tám
        GameObject stallLightGo = new GameObject("StallLanternLight");
        stallLightGo.transform.SetParent(envRoot.transform, false);
        stallLightGo.transform.position = new Vector3(0f, 2.3f, 0f);
        var lanternLight = stallLightGo.AddComponent<Light2D>();
        lanternLight.lightType = Light2D.LightType.Point;
        lanternLight.color = TeaStallPalette.WarmLanternGlow;
        lanternLight.intensity = 1.35f;
        lanternLight.pointLightOuterRadius = 5.5f;

        // ==================== 5. 4 CỤM BÀN NHỰA VỈA HÈ ====================
        CreatePlasticTable(envRoot.transform, new Vector3(-2.1f, 0.7f, 0f), TeaStallPalette.PlasticBlue);
        CreatePlasticTable(envRoot.transform, new Vector3(2.1f, 0.7f, 0f), TeaStallPalette.PlasticBlue);
        CreatePlasticTable(envRoot.transform, new Vector3(-2.4f, -0.9f, 0f), TeaStallPalette.PlasticRed);
        CreatePlasticTable(envRoot.transform, new Vector3(2.4f, -0.9f, 0f), TeaStallPalette.PlasticRed);
    }

    private static void CreateHanoiWindow(Transform parent, Vector3 pos)
    {
        GameObject winGo = new GameObject("HanoiWindow");
        winGo.transform.SetParent(parent, false);
        winGo.transform.position = pos;
        winGo.transform.localScale = new Vector3(1.4f, 1.8f, 1f);
        var sr = winGo.AddComponent<SpriteRenderer>();
        sr.sprite = TeaStallPalette.GetFallbackSprite(FallbackSpriteType.RoundedRect);
        sr.color = TeaStallPalette.MossGreen;
        sr.sortingOrder = 2;

        for (int i = -3; i <= 3; i++)
        {
            GameObject slatGo = new GameObject("Slat");
            slatGo.transform.SetParent(winGo.transform, false);
            slatGo.transform.localPosition = new Vector3(0f, i * 0.12f, 0f);
            slatGo.transform.localScale = new Vector3(0.85f, 0.04f, 1f);
            var slatSr = slatGo.AddComponent<SpriteRenderer>();
            slatSr.sprite = TeaStallPalette.GetFallbackSprite(FallbackSpriteType.RoundedRect);
            slatSr.color = TeaStallPalette.AntiqueWood;
            slatSr.sortingOrder = 3;
        }
    }

    private static void CreateBangTreeCanopy(Transform parent, Vector3 pos)
    {
        GameObject treeGo = new GameObject("BangTreeCanopy");
        treeGo.transform.SetParent(parent, false);
        treeGo.transform.position = pos;

        for (int i = 0; i < 4; i++)
        {
            GameObject leafGo = new GameObject("LeafCluster_" + i);
            leafGo.transform.SetParent(treeGo.transform, false);
            leafGo.transform.localPosition = new Vector3(i * 0.35f, -i * 0.25f, 0f);
            leafGo.transform.localScale = new Vector3(2.2f - i * 0.3f, 1.6f - i * 0.2f, 1f);
            var sr = leafGo.AddComponent<SpriteRenderer>();
            sr.sprite = TeaStallPalette.GetFallbackSprite(FallbackSpriteType.Circle);
            sr.color = Color.Lerp(TeaStallPalette.MossGreen, new Color(0.35f, 0.60f, 0.30f), i * 0.25f);
            sr.sortingOrder = 4 + i;
        }
    }

    private static void CreatePowerPole(Transform parent, Vector3 pos)
    {
        GameObject poleGo = new GameObject("PowerPole");
        poleGo.transform.SetParent(parent, false);
        poleGo.transform.position = pos;
        poleGo.transform.localScale = new Vector3(0.35f, 4.5f, 1f);
        var sr = poleGo.AddComponent<SpriteRenderer>();
        sr.sprite = TeaStallPalette.GetFallbackSprite(FallbackSpriteType.RoundedRect);
        sr.color = new Color(0.45f, 0.45f, 0.48f);
        sr.sortingOrder = 3;
    }

    private static void CreatePlasticTable(Transform parent, Vector3 pos, Color color)
    {
        GameObject tblGo = new GameObject("PlasticTable");
        tblGo.transform.SetParent(parent, false);
        tblGo.transform.position = pos;
        tblGo.transform.localScale = new Vector3(1.1f, 0.65f, 1f);
        var sr = tblGo.AddComponent<SpriteRenderer>();
        sr.sprite = TeaStallPalette.GetFallbackSprite(FallbackSpriteType.PlasticTable);
        sr.color = color;
        sr.sortingOrder = 7;

        GameObject seedGo = new GameObject("SunflowerPlate");
        seedGo.transform.SetParent(tblGo.transform, false);
        seedGo.transform.localPosition = new Vector3(0f, 0.15f, 0f);
        seedGo.transform.localScale = new Vector3(0.45f, 0.45f, 1f);
        var seedSr = seedGo.AddComponent<SpriteRenderer>();
        seedSr.sprite = TeaStallPalette.GetFallbackSprite(FallbackSpriteType.SunflowerPlate);
        seedSr.sortingOrder = 8;
    }

    private static void CreateStallProp(Transform parent, Vector3 pos, FallbackSpriteType type, Color color, Vector3 scale, int order)
    {
        GameObject propGo = new GameObject("StallProp_" + type);
        propGo.transform.SetParent(parent, false);
        propGo.transform.position = pos;
        propGo.transform.localScale = scale;
        var sr = propGo.AddComponent<SpriteRenderer>();
        sr.sprite = TeaStallPalette.GetFallbackSprite(type);
        sr.color = color;
        sr.sortingOrder = order;
    }
}
