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
    public static void BuildAllFromCommandLine()
    {
        string res = BuildAll();
        Debug.Log("[BuildTeaStallScene] " + res);
    }

    public static string BuildAll()
    {
        TeaStallPalette.ClearCache();

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

        // 4. Thiết lập Main Camera 9:16 chuẩn TikTok Live dọc
        Camera mainCam = Camera.main;
        if (mainCam == null)
        {
            GameObject camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            mainCam = camGo.AddComponent<Camera>();
        }
        mainCam.orthographic = true;
        mainCam.orthographicSize = 7.2f; // Khung nhìn 9:16 dọc trọn vẹn từ Header Neon đến Cheers Bar
        mainCam.backgroundColor = new Color(0.08f, 0.06f, 0.08f);
        mainCam.clearFlags = CameraClearFlags.SolidColor;
        mainCam.transform.position = new Vector3(0f, 1.2f, -10f);

        var camData = mainCam.GetComponent<UniversalAdditionalCameraData>();
        if (camData == null) camData = mainCam.gameObject.AddComponent<UniversalAdditionalCameraData>();
        camData.renderPostProcessing = true;

        if (mainCam.GetComponent<CinemachineBrain>() == null)
        {
            mainCam.gameObject.AddComponent<CinemachineBrain>();
        }

        // Tạo CinemachineCamera 3.x
        GameObject vcamGo = new GameObject("TeaStallCinemachineCamera");
        vcamGo.transform.position = new Vector3(0f, 1.2f, -10f);
        var cmCam = vcamGo.AddComponent<CinemachineCamera>();
        var lens = cmCam.Lens;
        lens.OrthographicSize = 7.2f;
        lens.NearClipPlane = 0.1f;
        lens.FarClipPlane = 100f;
        cmCam.Lens = lens;

        // 5. Ánh sáng 2D ấm cúng phố cổ
        Light2D globalLight = Object.FindAnyObjectByType<Light2D>();
        if (globalLight == null)
        {
            GameObject lightGo = new GameObject("Global Light 2D");
            globalLight = lightGo.AddComponent<Light2D>();
            globalLight.lightType = Light2D.LightType.Global;
        }
        globalLight.color = new Color(1.0f, 0.94f, 0.86f);
        globalLight.intensity = 1.05f;

        // 6. Setup URP Global Volume (Bloom rực rỡ + Vignette hoài cổ)
        SetupTeaStallVolume();

        // 7. Tạo Hệ thống Quản lý GameSystems
        GameObject systemsGo = new GameObject("TeaStallSystems");
        var audioAgent = systemsGo.AddComponent<TeaStallAudio>();
        var avatarCache = systemsGo.AddComponent<AvatarCache>();
        var liveBridge = systemsGo.AddComponent<TikTokLiveBridge>();
        var stallManager = systemsGo.AddComponent<TeaStallManager>();
        stallManager.EnsureInitialized();

        // 8. Dựng Môi trường Vỉa hè Phố Cổ Hà Nội chuẩn mẫu TikTok Live
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

            TextureImporterSettings tis = new TextureImporterSettings();
            ti.ReadTextureSettings(tis);
            tis.spriteMode = (int)SpriteImportMode.Single;
            tis.spriteMeshType = SpriteMeshType.FullRect; // FullRect (0) để không bị rách/méo mesh!
            tis.spriteAlignment = (int)SpriteAlignment.Center;
            tis.spritePivot = new Vector2(0.5f, 0.5f);
            tis.alphaIsTransparency = true;
            tis.mipmapEnabled = false;
            tis.filterMode = FilterMode.Bilinear;
            ti.SetTextureSettings(tis);

            ti.textureType = TextureImporterType.Sprite;
            ti.spriteImportMode = SpriteImportMode.Single;
            ti.alphaIsTransparency = true;
            ti.mipmapEnabled = false;
            ti.filterMode = FilterMode.Bilinear;
            ti.SaveAndReimport();
            anyChanged = true;
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

        GameObject custRoot = new GameObject("CustomersRoot");
        stallManager.CustomerParent = custRoot.transform;

        // ==================== 1. ẢNH NỀN PHỐ CỔ 9:16 ====================
        Sprite streetBgSprite = Resources.Load<Sprite>("TeaStall/Sprites/street_tea_bg");
        if (streetBgSprite != null)
        {
            GameObject bgGo = new GameObject("HanoiStreetArtBackground");
            bgGo.transform.SetParent(envRoot.transform, false);
            bgGo.transform.position = new Vector3(0f, 1.2f, 0f);
            var bgSr = bgGo.AddComponent<SpriteRenderer>();
            bgSr.sprite = streetBgSprite;
            bgSr.sortingOrder = -20;

            float spriteH = streetBgSprite.rect.height / streetBgSprite.pixelsPerUnit;
            if (spriteH > 0.01f)
            {
                float targetH = 14.8f;
                float s = targetH / spriteH;
                bgGo.transform.localScale = new Vector3(s, s, 1f);
            }
        }
        else
        {
            // Procedural Wall
            GameObject wallGo = new GameObject("HanoiOldWall");
            wallGo.transform.SetParent(envRoot.transform, false);
            wallGo.transform.position = new Vector3(0f, 3.8f, 0f);
            wallGo.transform.localScale = new Vector3(8.5f, 7.0f, 1f);
            var wallSr = wallGo.AddComponent<SpriteRenderer>();
            wallSr.sprite = TeaStallPalette.GetFallbackSprite(FallbackSpriteType.RoundedRect);
            wallSr.color = TeaStallPalette.HanoiWallYellow;
            wallSr.sortingOrder = -20;

            // Mặt đường & vỉa hè
            GameObject pavementGo = new GameObject("HanoiPavement");
            pavementGo.transform.SetParent(envRoot.transform, false);
            pavementGo.transform.position = new Vector3(0f, -1.2f, 0f);
            pavementGo.transform.localScale = new Vector3(8.5f, 4.8f, 1f);
            var pavSr = pavementGo.AddComponent<SpriteRenderer>();
            pavSr.sprite = TeaStallPalette.GetFallbackSprite(FallbackSpriteType.RoundedRect);
            pavSr.color = new Color(TeaStallPalette.PavementDark.r, TeaStallPalette.PavementDark.g, TeaStallPalette.PavementDark.b, 0.35f);
            pavSr.sortingOrder = -18;
        }

        // ==================== 2. NPC BÀ TÁM TRÀ ĐÁ & QUẦY BÀN NHỰA ĐỎ MUSE AI ====================
        GameObject teaLadyGo = new GameObject("TeaLadyNpc");
        teaLadyGo.transform.SetParent(envRoot.transform, false);
        teaLadyGo.transform.position = new Vector3(0f, 1.30f, -0.5f);
        var teaLadyAgent = teaLadyGo.AddComponent<StreetNpcAgent>();
        teaLadyAgent.SetupAsTeaLady(new Vector3(0f, 1.30f, -0.5f));
        stallManager.TeaLady = teaLadyAgent;

        // Đặt 4 Bàn Nhựa Đỏ + Trà Đá của MUSE AI (prop_tea_counter) ở 2 dãy Trái - Phải
        Sprite redTableSprite = Resources.Load<Sprite>("TeaStall/Sprites/prop_tea_counter");
        if (redTableSprite != null)
        {
            Vector3[] tablePositions = new Vector3[]
            {
                new Vector3(-1.80f,  0.15f, -0.3f),
                new Vector3( 1.80f,  0.15f, -0.3f),
                new Vector3(-1.80f, -1.20f, -0.3f),
                new Vector3( 1.80f, -1.20f, -0.3f)
            };
            for (int i = 0; i < tablePositions.Length; i++)
            {
                GameObject tblGo = new GameObject("MuseRedPlasticTable_" + i);
                tblGo.transform.SetParent(envRoot.transform, false);
                tblGo.transform.position = tablePositions[i];
                tblGo.transform.localScale = new Vector3(0.22f, 0.22f, 1f);
                var tblSr = tblGo.AddComponent<SpriteRenderer>();
                tblSr.sprite = redTableSprite;
                tblSr.sortingOrder = 5;
            }
        }

        // Đèn lồng vàng chiếu sáng quầy Bà Tám
        GameObject stallLightGo = new GameObject("StallLanternLight");
        stallLightGo.transform.SetParent(envRoot.transform, false);
        stallLightGo.transform.position = new Vector3(0f, 2.1f, -1f);
        var lanternLight = stallLightGo.AddComponent<Light2D>();
        lanternLight.lightType = Light2D.LightType.Point;
        lanternLight.color = TeaStallPalette.WarmLanternGlow;
        lanternLight.intensity = 1.35f;
        lanternLight.pointLightOuterRadius = 5.5f;
    }

    private static void CreatePlasticTable(Transform parent, Vector3 pos, Color color)
    {
        GameObject tblGo = new GameObject("PlasticTable");
        tblGo.transform.SetParent(parent, false);
        tblGo.transform.position = pos;
        tblGo.transform.localScale = new Vector3(1.15f, 0.65f, 1f);
        var sr = tblGo.AddComponent<SpriteRenderer>();
        sr.sprite = TeaStallPalette.GetFallbackSprite(FallbackSpriteType.PlasticTable);
        sr.color = color;
        sr.sortingOrder = 7;

        // Đĩa hướng dương & Cốc trà đá trên bàn
        GameObject seedGo = new GameObject("SunflowerPlate");
        seedGo.transform.SetParent(tblGo.transform, false);
        seedGo.transform.localPosition = new Vector3(-0.25f, 0.12f, 0f);
        seedGo.transform.localScale = new Vector3(0.40f, 0.40f, 1f);
        var seedSr = seedGo.AddComponent<SpriteRenderer>();
        seedSr.sprite = TeaStallPalette.GetFallbackSprite(FallbackSpriteType.SunflowerPlate);
        seedSr.sortingOrder = 8;

        GameObject teaGo = new GameObject("TeaGlass");
        teaGo.transform.SetParent(tblGo.transform, false);
        teaGo.transform.localPosition = new Vector3(0.25f, 0.15f, 0f);
        teaGo.transform.localScale = new Vector3(0.35f, 0.35f, 1f);
        var teaSr = teaGo.AddComponent<SpriteRenderer>();
        teaSr.sprite = TeaStallPalette.GetFallbackSprite(FallbackSpriteType.IcedTeaGlass);
        teaSr.sortingOrder = 8;
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
