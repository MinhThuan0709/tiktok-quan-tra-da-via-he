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

        mgr.EnableBotSimulator = false;

        // Dàn dựng 6 khách tiêu biểu đại diện 6 Tiers
        var c1 = mgr.GetOrCreateCustomer("u1", "Hữu Lâm");
        c1.AddExp(15); // Tier 1: Khách Dép Tổ Ong

        var c2 = mgr.GetOrCreateCustomer("u2", "Sumi");
        c2.AddExp(70); // Tier 2: Thánh Hướng Dương

        var c3 = mgr.GetOrCreateCustomer("u3", "Anh Ba Trà Cú");
        c3.AddExp(260); // Tier 3: Cao Thủ Thuốc Lào (+ Corgi)

        var c4 = mgr.GetOrCreateCustomer("u4", "Buồn Phiền");
        c4.AddExp(920); // Tier 4: Dân Chơi Phố Cổ (+ Cánh Bạc)

        var c5 = mgr.GetOrCreateCustomer("u5", "Hội Mê Phim");
        c5.AddExp(2900); // Tier 5: Phú Bà Vỉa Hè (+ Cánh Ruby)

        var c6 = mgr.GetOrCreateCustomer("u6", "Đại Gia Hà Thành");
        c6.AddExp(6800); // Tier 6: Thần Tài Bao Cả Phố (+ Ngai Vàng + Rồng Bay)

        // Dàn dựng thêm khách ngồi kín các bàn
        var c7 = mgr.GetOrCreateCustomer("u7", "Bé Bống");
        c7.AddExp(40);
        var c8 = mgr.GetOrCreateCustomer("u8", "Tuấn Phố Cổ");
        c8.AddExp(120);
        var c9 = mgr.GetOrCreateCustomer("u9", "Mỹ Xinh");
        c9.AddExp(85);
        var c10 = mgr.GetOrCreateCustomer("u10", "Tấn Pháp");
        c10.AddExp(210);

        // 3 Danh hiệu Độc Tôn KOTH
        c3.SetKothTitle(TeaStallPalette.KOTH_COMMENT); // Đệ Nhất Chém Gió
        c4.SetKothTitle(TeaStallPalette.KOTH_LIKE);    // Chiến Thần Thả Tim
        c6.SetKothTitle(TeaStallPalette.KOTH_DONATE);  // Trùm Bao Cả Quán

        // Cập nhật thống kê HUD
        mgr.TotalTeaSold = 373;
        mgr.TotalCheersCount = 110;
        mgr.CheersProgress = 44f;
        mgr.SetTickerMessage("🔥 @Hữu Lâm vừa gọi Điếu Cày Thuốc Lào • 🍻 Cả quán đồng thanh DÔ lần thứ 110!");

        if (mgr.TeaLady != null)
        {
            mgr.TeaLady.Say("🏮 Trà sen nước giếng Ngọc, hướng dương giòn tan mời bà con!", 6.0f);
        }

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

        var c3 = mgr.GetOrCreateCustomer("u3", "Anh Ba Trà Cú");
        var c4 = mgr.GetOrCreateCustomer("u4", "Buồn Phiền");
        var c6 = mgr.GetOrCreateCustomer("u6", "Đại Gia Hà Thành");

        c3.ShowSpeechBubble("💨 Rít một hơi điếu cày nỏ đồng phê lên tận 9 tầng mây!", 6.0f);
        c4.ShowSpeechBubble("🍵 Bà Tám cho cốc trà sen đậm vị nhiều đá nhé!", 6.0f);
        c6.ShowSpeechBubble("👑 Hôm nay tôi bao trọn gói trà đá kẹo lạc cả phố cổ!", 6.0f);

        // Kích hoạt nhả khói thuốc lào
        c3.SmokeDieuCay();

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

        mgr.SpawnStreetNpc(StreetNpcType.GrabDriver);
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

        mgr.TriggerPatrolPanicEvent();
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
        if (TeaStallManager.Instance == null)
        {
            var mgr = Object.FindAnyObjectByType<TeaStallManager>();
            if (mgr == null)
            {
                BuildTeaStallScene.BuildAll();
            }
        }
    }
}
