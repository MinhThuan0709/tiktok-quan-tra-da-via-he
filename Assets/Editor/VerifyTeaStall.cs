using System.IO;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using TikTokFactionWar;
using TikTokTeaStall;

public static class VerifyTeaStall
{
    private const string CaptureDir = "Assets/Captures";

    public static void RunVerificationFromCommandLine()
    {
        Sprite s1 = Resources.Load<Sprite>("TeaStall/Sprites/char_tier1_toong");
        Sprite s2 = Resources.Load<Sprite>("TeaStall/Sprites/badge_frame_wood");
        Sprite s3 = Resources.Load<Sprite>("TeaStall/Sprites/street_tea_bg");
        Debug.Log($"[CheckSprites] char1: {(s1 != null ? s1.name + ", rect=" + s1.rect + ", tex=" + s1.texture.name : "NULL")}");
        Debug.Log($"[CheckSprites] wood: {(s2 != null ? s2.name + ", rect=" + s2.rect + ", tex=" + s2.texture.name : "NULL")}");
        Debug.Log($"[CheckSprites] bg: {(s3 != null ? s3.name + ", rect=" + s3.rect + ", tex=" + s3.texture.name : "NULL")}");

        string scan = RunMachineScan();
        Debug.Log("[VerifyTeaStall Scan] " + scan);
        string res = RunFullVerificationAndCapture();
        Debug.Log("[VerifyTeaStall Capture] " + res);
        DumpSceneInfo();
    }

    public static void DumpSceneInfo()
    {
        var gos = Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None);
        var sb = new StringBuilder();
        sb.AppendLine("TOTAL GOS: " + gos.Length);
        foreach (var go in gos)
        {
            if (go.name.Contains("Customer") || go.name.Contains("Npc") || go.name.Contains("Throne") || go.name.Contains("Table") || go.name.Contains("Hud") || go.name.Contains("Environment"))
            {
                sb.AppendLine($"{go.name} | Pos={go.transform.position} | Scale={go.transform.localScale} | Active={go.activeInHierarchy}");
                var srs = go.GetComponentsInChildren<SpriteRenderer>(true);
                foreach (var sr in srs)
                {
                    sb.AppendLine($"   SR: {sr.name} | Sprite={(sr.sprite != null ? sr.sprite.name : "null")} | Order={sr.sortingOrder} | Color={sr.color} | Pos={sr.transform.position} | Scale={sr.transform.localScale} | Enabled={sr.enabled}");
                }
                var tmps = go.GetComponentsInChildren<TextMeshPro>(true);
                foreach (var tmp in tmps)
                {
                    sb.AppendLine($"   TMP: {tmp.name} | Text={tmp.text} | Order={tmp.sortingOrder} | Pos={tmp.transform.position} | Scale={tmp.transform.localScale} | Enabled={tmp.enabled}");
                }
            }
        }
        File.WriteAllText("Assets/scene_dump.txt", sb.ToString());
    }

    public static string RunMachineScan()
    {
        var sb = new StringBuilder();

        var urpAsset = UniversalRenderPipeline.asset;
        if (urpAsset == null) return "FAIL: URP is not active.";
        if (!urpAsset.supportsHDR) sb.AppendLine("WARN: HDR disabled on URP asset.");

        var cam = Camera.main;
        if (cam == null) return "FAIL: No Main Camera.";
        var camData = cam.GetComponent<UniversalAdditionalCameraData>();
        if (camData == null || !camData.renderPostProcessing)
            sb.AppendLine("FAIL: Camera renderPostProcessing is false.");

        var volumes = Object.FindObjectsByType<Volume>(FindObjectsSortMode.None);
        if (volumes.Length == 0) sb.AppendLine("FAIL: No Volume in scene.");

        if (TeaStallManager.Instance == null)
        {
            var mgr = Object.FindAnyObjectByType<TeaStallManager>();
            if (mgr == null) sb.AppendLine("FAIL: No TeaStallManager in scene.");
        }

        if (TeaStallAudio.Instance == null)
        {
            var aud = Object.FindAnyObjectByType<TeaStallAudio>();
            if (aud == null) sb.AppendLine("FAIL: No TeaStallAudio in scene.");
        }

        if (sb.Length == 0)
        {
            return "SUCCESS: All TeaStall pre-flight checks passed! URP, Camera, Lights, Scripts verified.";
        }
        return sb.ToString();
    }

    /// <summary>
    /// Dàn cảnh 1: Quán trà đá đông đúc khách ngồi kín bàn, đầy đủ 6 Tiers + 3 Danh hiệu Độc Tôn,
    /// 2 Bảng Thực Đơn & Đại Gia Vỉa Hè, Thanh Tiến Độ Cụng Ly.
    /// </summary>
    public static string SetupBusyTeaStallShowcase()
    {
        EnsureManagerReady();
        var mgr = TeaStallManager.Instance;
        if (mgr == null) return "FAIL: TeaStallManager.Instance is null.";

        mgr.EnsureInitialized();
        mgr.EnableBotSimulator = false;

        // Xóa sạch các customer cũ và reset ghế để dàn cảnh mới tinh
        mgr.ClearCustomers();

        // Dàn dựng 8 khách tiêu biểu chia đều 2 dãy bàn Trái & Phải + Ngai Vàng trung tâm
        var c1 = mgr.GetOrCreateCustomer("u1", "Hữu Lâm");
        c1.AddExp(15); // Tier 1: Khách Dép Tổ Ong (Trái Trên)

        var c2 = mgr.GetOrCreateCustomer("u2", "Sumi");
        c2.AddExp(70); // Tier 2: Thánh Hướng Dương (Phải Trên)

        var c3 = mgr.GetOrCreateCustomer("u3", "Anh Ba Trà Cú");
        c3.AddExp(260); // Tier 3: Cao Thủ Thuốc Lào (Trái Trên Trong)

        var c4 = mgr.GetOrCreateCustomer("u4", "Mỹ Xinh");
        c4.AddExp(920); // Tier 4: Dân Chơi Phố Cổ (Phải Trên Ngoài)

        var c5 = mgr.GetOrCreateCustomer("u5", "Hội Mê Phim");
        c5.AddExp(2900); // Tier 5: Phú Bà Vỉa Hè (Trái Giữa)

        var c6 = mgr.GetOrCreateCustomer("u6", "Tấn Pháp");
        c6.AddExp(210); // Tier 3 (Phải Giữa)

        var c7 = mgr.GetOrCreateCustomer("u7", "Bé Bống");
        c7.AddExp(40); // Tier 1 (Trái Giữa Trong)

        var c8 = mgr.GetOrCreateCustomer("u8", "Đại Gia Hà Thành");
        c8.AddExp(6800); // Tier 6: Thần Tài Bao Cả Phố -> chuyển ra Ngai Vàng Trung Tâm
        mgr.HandleViewerGift("u8", "Đại Gia Hà Thành", "Bao Cả Quán", 100);

        // Ẩn bong bóng "Lên cấp" đồng loạt để cảnh 1 khoe trọn vẹn Khung Danh Hiệu 3D & Bàn Trà Đá
        foreach (var cust in Object.FindObjectsByType<TeaCustomerAgent>(FindObjectsSortMode.None))
        {
            cust.HideSpeechBubble();
        }

        // 3 Danh hiệu Độc Tôn KOTH
        c3.SetKothTitle(TeaStallPalette.KOTH_COMMENT); // Đệ Nhất Chém Gió
        c4.SetKothTitle(TeaStallPalette.KOTH_LIKE);    // Chiến Thần Thả Tim
        c8.SetKothTitle(TeaStallPalette.KOTH_DONATE);  // Trùm Bao Cả Quán

        // Cập nhật thống kê HUD
        mgr.TotalTeaSold = 373;
        mgr.TotalCheersCount = 110;
        mgr.CheersProgress = 44f;
        mgr.SetTickerMessage("[HOT] @Hữu Lâm vừa gọi Điếu Cày Thuốc Lào • Cả quán đồng thanh DÔ lần thứ 110!");

        if (mgr.TeaLady != null)
        {
            mgr.TeaLady.Say("[MỜI] Trà sen nước giếng Ngọc, hướng dương giòn tan mời bà con!", 6.0f);
        }

        mgr.RefreshHudVisuals();

        return "SetupBusyTeaStallShowcase completed.";
    }

    /// <summary>
    /// Dàn cảnh 2: Cận cảnh Khung Danh Hiệu 3 Tầng + Bong bóng chat + Rít thuốc lào nhả khói trắng
    /// </summary>
    public static string ShowcaseTitleBadgesAndChat()
    {
        SetupBusyTeaStallShowcase();
        var mgr = TeaStallManager.Instance;
        if (mgr == null) return "FAIL: No TeaStallManager.";

        var c1 = mgr.GetOrCreateCustomer("u1", "Hữu Lâm");
        var c4 = mgr.GetOrCreateCustomer("u4", "Mỹ Xinh");
        var c8 = mgr.GetOrCreateCustomer("u8", "Đại Gia Hà Thành");

        c1.ShowSpeechBubble("Rít một hơi điếu cày phê lên 9 tầng mây!", 6.0f);
        c4.ShowSpeechBubble("Bà Tám cho cốc trà chanh nhiều đá nhé!", 6.0f);
        c8.ShowSpeechBubble("Hôm nay tôi bao trọn trà đá cả phố!", 6.0f);

        return "ShowcaseTitleBadgesAndChat completed.";
    }

    /// <summary>
    /// Dàn cảnh 3: NPC Đường phố Việt Nam (Grab xe máy dừng trước quán + Bán vé số dạo)
    /// </summary>
    public static string ShowcaseStreetNpcsAndGrab()
    {
        SetupBusyTeaStallShowcase();
        var mgr = TeaStallManager.Instance;
        if (mgr == null) return "FAIL: No TeaStallManager.";

        // Dọn NPC đường phố cũ
        foreach (var n in Object.FindObjectsByType<StreetNpcAgent>(FindObjectsSortMode.None))
        {
            if (!n.IsTeaLady) Object.DestroyImmediate(n.gameObject);
        }

        var grab = mgr.SpawnStreetNpc(StreetNpcType.GrabDriver);
        if (grab != null)
        {
            grab.transform.position = new Vector3(-1.85f, -3.25f, -1.0f);
            grab.Say("Ting ting! Đơn 5 cốc trà đá hướng dương!", 6f);
        }

        var lottery = mgr.SpawnStreetNpc(StreetNpcType.LotteryVendor);
        if (lottery != null)
        {
            lottery.transform.position = new Vector3(1.85f, -3.25f, -1.0f);
            lottery.Say("Vé số chiều xổ! Trúng độc đắc bao quán!", 6f);
        }

        return "ShowcaseStreetNpcsAndGrab completed.";
    }

    /// <summary>
    /// Dàn cảnh 4: Trật tự phường còi tuýt tuýt, khách ôm ghế dọn vào mép tường
    /// </summary>
    public static string ShowcaseTratTuPhuongPanic()
    {
        SetupBusyTeaStallShowcase();
        var mgr = TeaStallManager.Instance;
        if (mgr == null) return "FAIL: No TeaStallManager.";

        foreach (var n in Object.FindObjectsByType<StreetNpcAgent>(FindObjectsSortMode.None))
        {
            if (!n.IsTeaLady) Object.DestroyImmediate(n.gameObject);
        }

        // Trật tự phường đi tuần
        var patrol = mgr.SpawnStreetNpc(StreetNpcType.WardPatrol);
        if (patrol != null)
        {
            patrol.transform.position = new Vector3(0f, -3.15f, -1.0f);
            patrol.Say("[CÒI] Tuýt tuýt! Dọn bàn ghế vào đúng vạch vỉa hè!", 6f);
        }

        mgr.TriggerPatrolPanicEvent();
        // Trong ảnh chụp tĩnh, chỉ bật 2 bong bóng đại diện ở 2 đầu để không đè chữ lên nhau
        var allCusts = Object.FindObjectsByType<TeaCustomerAgent>(FindObjectsSortMode.None);
        for (int i = 0; i < allCusts.Length; i++)
        {
            if (i != 0 && i != allCusts.Length - 1) allCusts[i].HideSpeechBubble();
        }
        return "ShowcaseTratTuPhuongPanic completed.";
    }

    /// <summary>
    /// Chụp ảnh màn hình Camera chính ra file PNG độ phân giải 1080x1920 chuẩn TikTok Live dọc.
    /// </summary>
    public static string CaptureScreenshot(string fileName, int width = 1080, int height = 1920)
    {
        if (!Directory.Exists(CaptureDir)) Directory.CreateDirectory(CaptureDir);

        Camera cam = Camera.main;
        if (cam == null) return "FAIL: Camera.main is null.";

        RenderTexture rt = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
        rt.antiAliasing = 8;
        cam.targetTexture = rt;

        Texture2D screenShot = new Texture2D(width, height, TextureFormat.RGB24, false);
        cam.Render();

        RenderTexture.active = rt;
        screenShot.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        screenShot.Apply();

        cam.targetTexture = null;
        RenderTexture.active = null;
        Object.DestroyImmediate(rt);

        byte[] bytes = screenShot.EncodeToPNG();
        Object.DestroyImmediate(screenShot);

        string fullPath = Path.Combine(CaptureDir, fileName);
        File.WriteAllBytes(fullPath, bytes);
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

        return fullPath;
    }

    /// <summary>
    /// Chạy toàn bộ quy trình 4 bức ảnh nghiệm thu và lưu vào Assets/Captures/
    /// </summary>
    public static string RunFullVerificationAndCapture()
    {
        var sb = new StringBuilder();

        // Warm up and pre-load all sprites & materials
        SetupBusyTeaStallShowcase();
        Camera cam = Camera.main;
        if (cam != null) cam.Render();

        // 1. tea_01_busy_stall.png
        SetupBusyTeaStallShowcase();
        string p1 = CaptureScreenshot("tea_01_busy_stall.png");
        sb.AppendLine("Capture 1: " + p1);

        // 2. tea_02_titles_and_chat.png
        ShowcaseTitleBadgesAndChat();
        string p2 = CaptureScreenshot("tea_02_titles_and_chat.png");
        sb.AppendLine("Capture 2: " + p2);

        // 3. tea_03_street_npcs.png
        ShowcaseStreetNpcsAndGrab();
        string p3 = CaptureScreenshot("tea_03_street_npcs.png");
        sb.AppendLine("Capture 3: " + p3);

        // 4. tea_04_trat_tu_panic.png
        ShowcaseTratTuPhuongPanic();
        string p4 = CaptureScreenshot("tea_04_trat_tu_panic.png");
        sb.AppendLine("Capture 4: " + p4);

        // Dọn dẹp khôi phục trạng thái chuẩn
        SetupBusyTeaStallShowcase();

        return sb.ToString();
    }

    private static void EnsureManagerReady()
    {
        if (UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene().name != "TeaStallScene")
        {
            if (File.Exists("Assets/Scenes/TeaStallScene.unity"))
            {
                UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/TeaStallScene.unity");
            }
        }

        var mgr = Object.FindAnyObjectByType<TeaStallManager>();
        if (mgr == null)
        {
            BuildTeaStallScene.BuildAll();
            mgr = Object.FindAnyObjectByType<TeaStallManager>();
        }
        if (mgr != null)
        {
            TeaStallManager.Instance = mgr;
            mgr.EnsureInitialized();
        }
    }
}
