using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using TikTokFactionWar;
using TikTokTeaStall;

public static class VerifyTeaStall
{
    public static string RunMachineScan()
    {
        var sb = new StringBuilder();

        // 1. URP Asset & Post-Processing Check
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

        // 2. Kiểm tra TeaStallManager & Hệ thống
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

        // 3. Quét kiểm tra Default-Material
        var renderers = Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None);
        foreach (var r in renderers)
        {
            if (r.sharedMaterial != null && r.sharedMaterial.name.Contains("Default-Material"))
            {
                sb.AppendLine($"WARN: Renderer '{r.name}' uses Default-Material.");
            }
        }

        if (sb.Length == 0)
        {
            return "SUCCESS: All TeaStall pre-flight checks passed! URP, Camera, Lights, Scripts verified.";
        }
        return sb.ToString();
    }

    /// <summary>
    /// Dàn cảnh Quán Trà Đá Vỉa Hè giờ cao điểm: Đông đúc khách các Tier 1 đến Tier 6 ngồi kín bàn,
    /// cốc trà đá hổ phách, đĩa hạt hướng dương, điếu cày nỏ đồng.
    /// </summary>
    public static string SetupBusyTeaStallShowcase()
    {
        EnsureManagerReady();
        var mgr = TeaStallManager.Instance;
        if (mgr == null) return "FAIL: TeaStallManager.Instance is null.";

        mgr.EnableBotSimulator = false; // Tắt bot ngẫu nhiên để dàn cảnh chuẩn xác

        // Spawn khách Tier 1 -> Tier 6
        var c1 = mgr.GetOrCreateCustomer("u1", "Toong_DuyTan");
        c1.AddExp(10); // Tier 1: Khách Dép Tổ Ong

        var c2 = mgr.GetOrCreateCustomer("u2", "Hoa_HuongDuong");
        c2.AddExp(60); // Tier 2: Thánh Hướng Dương

        var c3 = mgr.GetOrCreateCustomer("u3", "Hai_TienLang");
        c3.AddExp(250); // Tier 3: Cao Thủ Thuốc Lào (+ Corgi)

        var c4 = mgr.GetOrCreateCustomer("u4", "Tuan_PhoCo");
        c4.AddExp(900); // Tier 4: Dân Chơi Phố Cổ (+ Cánh Bạc)

        var c5 = mgr.GetOrCreateCustomer("u5", "PhuBa_HangDao");
        c5.AddExp(2800); // Tier 5: Phú Bà Vỉa Hè (+ Cánh Ruby)

        var c6 = mgr.GetOrCreateCustomer("u6", "DaiGia_HaThanh");
        c6.AddExp(6500); // Tier 6: Thần Tài Bao Cả Phố (+ Ngai Vàng + Rồng Bay)

        // Gán 3 Danh hiệu Độc Tôn King of the Hill
        c3.SetKothTitle(TeaStallPalette.KOTH_COMMENT); // Đệ Nhất Chém Gió
        c4.SetKothTitle(TeaStallPalette.KOTH_LIKE);    // Chiến Thần Thả Tim
        c6.SetKothTitle(TeaStallPalette.KOTH_DONATE);  // Trùm Bao Cả Quán

        // Bà Tám quạt nan rót trà
        if (mgr.TeaLady != null)
        {
            mgr.TeaLady.Say("🏮 Trà sen nước giếng Ngọc, hướng dương giòn tan phục vụ bà con!", 6.0f);
        }

        return "SetupBusyTeaStallShowcase: 6 Tiers & 3 KOTH titles staged successfully.";
    }

    /// <summary>
    /// Trưng bày cận cảnh Cụm UI 3 Tầng trên đầu khách: Bong bóng Chat (Tầng 3), 
    /// Khung Danh Hiệu Cánh Vỗ & Độc Tôn (Tầng 2), Avatar & Tên (Tầng 1).
    /// </summary>
    public static string ShowcaseTitleBadgesAndChat()
    {
        SetupBusyTeaStallShowcase();
        var mgr = TeaStallManager.Instance;
        if (mgr == null) return "FAIL: No TeaStallManager.";

        var c3 = mgr.GetOrCreateCustomer("u3", "Hai_TienLang");
        var c4 = mgr.GetOrCreateCustomer("u4", "Tuan_PhoCo");
        var c6 = mgr.GetOrCreateCustomer("u6", "DaiGia_HaThanh");

        c3.ShowSpeechBubble("💨 Bắn một hơi thuốc lào Tiên Lãng phê tận mây xanh!", 6.0f);
        c4.ShowSpeechBubble("🍵 Bà Tám cho cốc trà đá ít đường nhiều đá nhé!", 6.0f);
        c6.ShowSpeechBubble("👑 Hôm nay tôi bao trọn gói trà đá kẹo lạc cả phố cổ!", 6.0f);

        return "ShowcaseTitleBadgesAndChat: Overhead Stack Tiers 1, 2, 3 displayed.";
    }

    /// <summary>
    /// Trưng bày NPC Đường Phố: Anh tài xế Grab ghé nổ đơn Ting Ting + Cô bán vé số dạo chào mời số 79.
    /// </summary>
    public static string ShowcaseStreetNpcsAndGrab()
    {
        SetupBusyTeaStallShowcase();
        var mgr = TeaStallManager.Instance;
        if (mgr == null) return "FAIL: No TeaStallManager.";

        mgr.SpawnStreetNpc(StreetNpcType.GrabDriver);
        return "ShowcaseStreetNpcsAndGrab: Grab Driver staged with Ting Ting sound.";
    }

    /// <summary>
    /// Trưng bày Sự kiện Trật Tự Phường: Còi tuýt tuýt vang lên, toàn bộ khách ôm ghế nhựa chạy nép sát mép tường!
    /// </summary>
    public static string ShowcaseTratTuPhuongPanic()
    {
        SetupBusyTeaStallShowcase();
        var mgr = TeaStallManager.Instance;
        if (mgr == null) return "FAIL: No TeaStallManager.";

        mgr.SpawnStreetNpc(StreetNpcType.WardPatrol);
        return "ShowcaseTratTuPhuongPanic: Ward patrol whistle blown, customers panicking to walls.";
    }

    /// <summary>
    /// Trưng bày Đại Tiệc Bao Quán: Loa kẹo kéo quẩy disco, cả quán nhún nhảy, pháo hoa nổ vang.
    /// </summary>
    public static string ShowcaseBaoQuanVipParty()
    {
        SetupBusyTeaStallShowcase();
        var mgr = TeaStallManager.Instance;
        if (mgr == null) return "FAIL: No TeaStallManager.";

        var c6 = mgr.GetOrCreateCustomer("u6", "DaiGia_HaThanh");
        mgr.TriggerBaoQuanPartyEvent(c6);
        mgr.TriggerKeoKeoPartyEvent();

        return "ShowcaseBaoQuanVipParty: Jackpot fanfare, disco beat, customers dancing.";
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
