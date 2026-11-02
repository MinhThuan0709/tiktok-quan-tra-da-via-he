using System.Collections.Generic;
using UnityEngine;

namespace TikTokTeaStall
{
    public enum FallbackSpriteType
    {
        Circle,
        RoundedRect,
        PlasticChairBlue,
        PlasticChairRed,
        PlasticTable,
        IcedTeaGlass,
        SunflowerPlate,
        DieuCayPipe,
        SmokeRing,
        NemChuaPlate,
        BreadLoaf,
        LotteryTicket,
        GrabBike,
        BadgeFrameWood,
        BadgeFrameFire,
        BadgeFrameNeonWings,
        BadgeFrameRubyWings,
        BadgeFrameRoyalDragon,
        WingPair,
        CrownGolden,
        DragonPet,
        CorgiPet,
        ChatBubbleNineSlice,
        SofaThrone,
        TeaLadyAvatar,
        StreetVendorCart
    }

    [System.Serializable]
    public class VipTierConfig
    {
        public int Tier;
        public int RequiredExp;
        public string Title;
        public string SpriteResource;
        public string BadgeResource;
        public Color TitleColor;
        public Color FrameGlowColor;
        public Color ChairColor;
        public bool HasWings;
        public bool HasPet;
        public string PetType; // "corgi" or "dragon"
        public bool IsThroneChair;

        public VipTierConfig(int tier, int exp, string title, string spriteRes, string badgeRes, 
                             Color titleColor, Color glowColor, Color chairColor, 
                             bool wings = false, bool pet = false, string petType = "", bool throne = false)
        {
            Tier = tier;
            RequiredExp = exp;
            Title = title;
            SpriteResource = spriteRes;
            BadgeResource = badgeRes;
            TitleColor = titleColor;
            FrameGlowColor = glowColor;
            ChairColor = chairColor;
            HasWings = wings;
            HasPet = pet;
            PetType = petType;
            IsThroneChair = throne;
        }
    }

    /// <summary>
    /// Bảng màu ấm áp phố cổ Hà Nội + ZingMe Neon VIP & Quản lý Procedural Fallback Sprites.
    /// Đảm bảo đồ họa không bao giờ bị null khi thiếu sprite bên ngoài.
    /// </summary>
    public static class TeaStallPalette
    {
        // ==================== BẢNG MÀU PHỐ CỔ HÀ NỘI ====================
        public static readonly Color HanoiWallYellow    = new Color(0.95f, 0.80f, 0.45f); // Tường vàng phố cổ đặc trưng
        public static readonly Color HanoiRoofRed       = new Color(0.80f, 0.28f, 0.18f); // Ngói đỏ rêu phong
        public static readonly Color MossGreen          = new Color(0.24f, 0.46f, 0.28f); // Rêu tường cổ & tán bàng xanh
        public static readonly Color AntiqueWood        = new Color(0.35f, 0.20f, 0.12f); // Cửa gỗ nâu trầm xưa
        public static readonly Color PavementDark       = new Color(0.42f, 0.40f, 0.38f); // Đá vỉa hè cổ
        public static readonly Color PavementLight      = new Color(0.68f, 0.65f, 0.62f); // Vạch kẻ vỉa hè
        public static readonly Color PlasticBlue        = new Color(0.08f, 0.54f, 0.92f); // Ghế nhựa Duy Tân xanh biển
        public static readonly Color PlasticRed         = new Color(0.92f, 0.22f, 0.18f); // Ghế nhựa đỏ rực
        public static readonly Color TeaAmber           = new Color(0.98f, 0.68f, 0.12f); // Nước trà đá hổ phách sóng sánh
        public static readonly Color IceCubeWhite       = new Color(0.90f, 0.97f, 1.00f, 0.85f); // Viên đá trong veo
        public static readonly Color SmokePale          = new Color(0.88f, 0.92f, 0.98f, 0.80f); // Làn khói thuốc lào bảng lảng
        public static readonly Color RangDongRed        = new Color(0.85f, 0.15f, 0.15f); // Phích nước Rạng Đông huyền thoại
        public static readonly Color WarmLanternGlow    = new Color(1.00f, 0.82f, 0.42f); // Ánh đèn lồng ấm cúng ban tối
        public static readonly Color BambooBrown        = new Color(0.72f, 0.54f, 0.30f); // Tre ống điếu cày

        // ==================== BẢNG MÀU ZINGME NEON VIP ====================
        public static readonly Color NeonGold           = new Color(1.00f, 0.84f, 0.00f); // #FFD700
        public static readonly Color NeonCyan           = new Color(0.00f, 0.95f, 1.00f); // #00F0FF
        public static readonly Color NeonPurple         = new Color(0.70f, 0.25f, 1.00f); // #B340FF
        public static readonly Color NeonRuby           = new Color(1.00f, 0.15f, 0.55f); // #FF268C
        public static readonly Color RoyalDragonGold    = new Color(1.00f, 0.60f, 0.00f); // #FFA500
        public static readonly Color GlossWhite         = new Color(1.00f, 1.00f, 1.00f);
        public static readonly Color DeepInkBlack       = new Color(0.10f, 0.08f, 0.10f);

        // ==================== 3 DANH HIỆU ĐỘC TÔN (KING OF THE HILL) ====================
        public const string KOTH_COMMENT = "[KOTH] ĐỆ NHẤT CHÉM GIÓ";
        public const string KOTH_LIKE    = "[KOTH] CHIẾN THẦN THẢ TIM";
        public const string KOTH_DONATE  = "[KOTH] TRÙM BAO CẢ QUÁN";

        public static readonly Color KothCommentColor = new Color(0.20f, 0.90f, 1.00f); // Bão xanh lốc xoáy
        public static readonly Color KothLikeColor    = new Color(1.00f, 0.30f, 0.65f); // Tim hồng neon
        public static readonly Color KothDonateColor  = new Color(1.00f, 0.85f, 0.10f); // Vương miện rồng vàng

        // ==================== 6 CẤP BẬC DANH HIỆU & SKIN ZINGME ====================
        private static readonly VipTierConfig[] Tiers = new VipTierConfig[]
        {
            new VipTierConfig(
                tier: 1,
                exp: 0,
                title: "[T1] DÉP TỔ ONG",
                spriteRes: "char_tier1_toong",
                badgeRes: "badge_frame_wood",
                titleColor: new Color(0.95f, 0.95f, 0.95f),
                glowColor: new Color(0.60f, 0.45f, 0.30f),
                chairColor: PlasticBlue
            ),
            new VipTierConfig(
                tier: 2,
                exp: 50,
                title: "[T2] HƯỚNG DƯƠNG",
                spriteRes: "char_tier2_sunflower",
                badgeRes: "badge_frame_wood",
                titleColor: new Color(1.00f, 0.92f, 0.30f),
                glowColor: new Color(0.25f, 0.85f, 0.45f),
                chairColor: PlasticBlue
            ),
            new VipTierConfig(
                tier: 3,
                exp: 200,
                title: "[T3] THUỐC LÀO",
                spriteRes: "char_tier3_street",
                badgeRes: "badge_frame_neon_wings",
                titleColor: new Color(1.00f, 0.55f, 0.20f),
                glowColor: NeonPurple,
                chairColor: PlasticRed,
                wings: false,
                pet: true,
                petType: "corgi"
            ),
            new VipTierConfig(
                tier: 4,
                exp: 800,
                title: "[T4] DÂN CHƠI",
                spriteRes: "char_tier4_cyber",
                badgeRes: "badge_frame_neon_wings",
                titleColor: NeonCyan,
                glowColor: NeonCyan,
                chairColor: PlasticRed,
                wings: true
            ),
            new VipTierConfig(
                tier: 5,
                exp: 2500,
                title: "[T5] PHÚ BÀ",
                spriteRes: "char_tier5_phuba",
                badgeRes: "badge_frame_royal_dragon",
                titleColor: NeonRuby,
                glowColor: NeonRuby,
                chairColor: new Color(0.75f, 0.15f, 0.40f),
                wings: true,
                pet: false
            ),
            new VipTierConfig(
                tier: 6,
                exp: 6000,
                title: "[VIP] THẦN TÀI",
                spriteRes: "char_tier6_vip",
                badgeRes: "badge_frame_royal_dragon",
                titleColor: NeonGold,
                glowColor: RoyalDragonGold,
                chairColor: NeonGold,
                wings: true,
                pet: true,
                petType: "dragon",
                throne: true
            )
        };

        public static VipTierConfig GetTierConfig(int tierLevel)
        {
            if (tierLevel < 1) tierLevel = 1;
            if (tierLevel > 6) tierLevel = 6;
            return Tiers[tierLevel - 1];
        }

        public static VipTierConfig GetTierByExp(int exp)
        {
            for (int i = Tiers.Length - 1; i >= 0; i--)
            {
                if (exp >= Tiers[i].RequiredExp)
                {
                    return Tiers[i];
                }
            }
            return Tiers[0];
        }

        public static int GetNextTierExp(int exp)
        {
            for (int i = 0; i < Tiers.Length; i++)
            {
                if (Tiers[i].RequiredExp > exp)
                {
                    return Tiers[i].RequiredExp;
                }
            }
            return Tiers[Tiers.Length - 1].RequiredExp;
        }

        // ==================== PROCEDURAL SPRITE CACHE ====================
        private static readonly Dictionary<FallbackSpriteType, Sprite> FallbackCache = new Dictionary<FallbackSpriteType, Sprite>();
        private static readonly Dictionary<string, Sprite> LoadedSpriteCache = new Dictionary<string, Sprite>();

        public static void ClearCache()
        {
            LoadedSpriteCache.Clear();
            FallbackCache.Clear();
        }

        /// <summary>
        /// Nạp sprite từ Resources/TeaStall/Sprites hoặc fallback procedural đẹp mắt.
        /// </summary>
        public static Sprite GetOrLoadSprite(string resourceName, FallbackSpriteType fallbackType)
        {
            if (!string.IsNullOrEmpty(resourceName))
            {
                if (LoadedSpriteCache.TryGetValue(resourceName, out Sprite cached) && cached != null)
                {
                    return cached;
                }

                // Cố gắng load từ Resources (kể cả Wave 2 hoặc fallback sang Wave 1 của MUSE AI)
                Sprite resSprite = Resources.Load<Sprite>("TeaStall/Sprites/" + resourceName);
                if (resSprite == null)
                {
                    resSprite = Resources.Load<Sprite>(resourceName);
                }
                if (resSprite == null)
                {
                    // Nếu đang chờ MUSE AI push Wave 2 thì tự động dùng Sprite Wave 1 của MUSE AI (TUYỆT ĐỐI KHÔNG VẼ HÌNH TỰ CHẾ)
                    string wave1Alias = null;
                    if (resourceName == "char_tier2_sunflower") wave1Alias = "char_tier1_toong";
                    else if (resourceName == "char_tier4_cyber") wave1Alias = "char_tier3_street";
                    else if (resourceName == "char_tier5_phuba") wave1Alias = "char_tier6_vip";
                    else if (resourceName == "npc_shoe_shiner") wave1Alias = "char_tier1_toong";

                    if (!string.IsNullOrEmpty(wave1Alias))
                    {
                        resSprite = Resources.Load<Sprite>("TeaStall/Sprites/" + wave1Alias);
                    }
                }
                if (resSprite != null)
                {
                    LoadedSpriteCache[resourceName] = resSprite;
                    return resSprite;
                }
            }

            // Trả về Fallback Sprite Procedural
            return GetFallbackSprite(fallbackType);
        }

        public static Sprite GetFallbackSprite(FallbackSpriteType type)
        {
            if (FallbackCache.TryGetValue(type, out Sprite existing) && existing != null)
            {
                return existing;
            }

            if (type == FallbackSpriteType.RoundedRect || type == FallbackSpriteType.ChatBubbleNineSlice)
            {
                Sprite sp = Resources.Load<Sprite>("TeaStall/Sprites/ui_box_white");
                if (sp != null)
                {
                    FallbackCache[type] = sp;
                    return sp;
                }
            }
            else if (type == FallbackSpriteType.Circle)
            {
                Sprite sp = Resources.Load<Sprite>("TeaStall/Sprites/ui_circle_white");
                if (sp != null)
                {
                    FallbackCache[type] = sp;
                    return sp;
                }
            }

            Sprite created = GenerateFallbackSprite(type);
            FallbackCache[type] = created;
            return created;
        }

        private static Sprite GenerateFallbackSprite(FallbackSpriteType type)
        {
            int w = 128;
            int h = 128;
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            Color[] pixels = new Color[w * h];
            Color clear = new Color(0, 0, 0, 0);

            for (int i = 0; i < pixels.Length; i++) pixels[i] = clear;

            switch (type)
            {
                case FallbackSpriteType.Circle:
                    DrawFilledCircle(pixels, w, h, w / 2f, h / 2f, w * 0.45f, Color.white, DeepInkBlack, 4f);
                    break;

                case FallbackSpriteType.RoundedRect:
                case FallbackSpriteType.ChatBubbleNineSlice:
                    DrawRoundedRect(pixels, w, h, 6, 6, w - 12, h - 12, 16, new Color(0.98f, 0.97f, 0.94f), DeepInkBlack, 4f);
                    break;

                case FallbackSpriteType.PlasticChairBlue:
                    DrawPlasticChair(pixels, w, h, PlasticBlue);
                    break;

                case FallbackSpriteType.PlasticChairRed:
                    DrawPlasticChair(pixels, w, h, PlasticRed);
                    break;

                case FallbackSpriteType.PlasticTable:
                    DrawPlasticTable(pixels, w, h, new Color(0.85f, 0.35f, 0.20f));
                    break;

                case FallbackSpriteType.IcedTeaGlass:
                    DrawIcedTeaGlass(pixels, w, h);
                    break;

                case FallbackSpriteType.SunflowerPlate:
                    DrawSunflowerPlate(pixels, w, h);
                    break;

                case FallbackSpriteType.DieuCayPipe:
                    DrawDieuCayPipe(pixels, w, h);
                    break;

                case FallbackSpriteType.SmokeRing:
                    DrawSmokeRing(pixels, w, h);
                    break;

                case FallbackSpriteType.BadgeFrameWood:
                    DrawBadgeFrame(pixels, w, h, new Color(0.55f, 0.35f, 0.18f), new Color(0.85f, 0.70f, 0.45f), false);
                    break;

                case FallbackSpriteType.BadgeFrameFire:
                    DrawBadgeFrame(pixels, w, h, new Color(0.85f, 0.25f, 0.10f), new Color(1.0f, 0.70f, 0.10f), true);
                    break;

                case FallbackSpriteType.BadgeFrameNeonWings:
                    DrawBadgeFrame(pixels, w, h, new Color(0.0f, 0.65f, 0.95f), NeonCyan, true);
                    break;

                case FallbackSpriteType.BadgeFrameRubyWings:
                    DrawBadgeFrame(pixels, w, h, new Color(0.85f, 0.10f, 0.45f), NeonRuby, true);
                    break;

                case FallbackSpriteType.BadgeFrameRoyalDragon:
                    DrawBadgeFrame(pixels, w, h, new Color(0.85f, 0.45f, 0.05f), NeonGold, true);
                    break;

                case FallbackSpriteType.WingPair:
                    DrawWings(pixels, w, h, NeonCyan);
                    break;

                case FallbackSpriteType.CrownGolden:
                    DrawCrown(pixels, w, h);
                    break;

                case FallbackSpriteType.DragonPet:
                    DrawMiniPet(pixels, w, h, RoyalDragonGold, "DRAGON");
                    break;

                case FallbackSpriteType.CorgiPet:
                    DrawMiniPet(pixels, w, h, new Color(0.95f, 0.65f, 0.25f), "CORGI");
                    break;

                case FallbackSpriteType.SofaThrone:
                    DrawSofaThrone(pixels, w, h);
                    break;

                case FallbackSpriteType.TeaLadyAvatar:
                    DrawTeaLadyHead(pixels, w, h);
                    break;

                case FallbackSpriteType.GrabBike:
                    DrawGrabBike(pixels, w, h);
                    break;

                case FallbackSpriteType.LotteryTicket:
                    DrawLotteryTicket(pixels, w, h);
                    break;

                case FallbackSpriteType.BreadLoaf:
                    DrawBreadLoaf(pixels, w, h);
                    break;

                default:
                    DrawFilledCircle(pixels, w, h, w / 2f, h / 2f, w * 0.4f, Color.white, DeepInkBlack, 3f);
                    break;
            }

            tex.SetPixels(pixels);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f);
        }

        // ==================== PROCEDURAL DRAWING HELPERS ====================
        private static void DrawFilledCircle(Color[] px, int w, int h, float cx, float cy, float radius, Color fill, Color border, float borderWidth)
        {
            float rSq = radius * radius;
            float innerRSq = (radius - borderWidth) * (radius - borderWidth);

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float dx = x - cx;
                    float dy = y - cy;
                    float distSq = dx * dx + dy * dy;

                    if (distSq <= rSq)
                    {
                        if (distSq >= innerRSq && borderWidth > 0f)
                        {
                            px[y * w + x] = border;
                        }
                        else
                        {
                            // Điểm bóng sáng nhẹ góc trên trái
                            float shine = Mathf.Clamp01(1f - (Mathf.Abs(x - (cx - radius * 0.35f)) + Mathf.Abs(y - (cy + radius * 0.35f))) / (radius * 1.2f));
                            px[y * w + x] = Color.Lerp(fill, GlossWhite, shine * 0.35f);
                        }
                    }
                }
            }
        }

        private static void DrawRoundedRect(Color[] px, int w, int h, int rx, int ry, int rw, int rh, int radius, Color fill, Color border, float borderWidth)
        {
            for (int y = ry; y < ry + rh && y < h; y++)
            {
                for (int x = rx; x < rx + rw && x < w; x++)
                {
                    int cornerDistX = Mathf.Min(x - rx, (rx + rw - 1) - x);
                    int cornerDistY = Mathf.Min(y - ry, (ry + rh - 1) - y);

                    bool inCorner = (cornerDistX < radius && cornerDistY < radius);
                    bool inside = true;

                    if (inCorner)
                    {
                        float dx = radius - cornerDistX;
                        float dy = radius - cornerDistY;
                        inside = (dx * dx + dy * dy) <= (radius * radius);
                    }

                    if (inside)
                    {
                        bool isBorder = (x - rx < borderWidth || (rx + rw - 1) - x < borderWidth ||
                                         y - ry < borderWidth || (ry + rh - 1) - y < borderWidth);
                        px[y * w + x] = isBorder ? border : fill;
                    }
                }
            }
        }

        private static void DrawPlasticChair(Color[] px, int w, int h, Color chairColor)
        {
            Color shadowColor = chairColor * 0.65f;
            Color highlightColor = Color.Lerp(chairColor, Color.white, 0.35f);

            // Tựa lưng ghế
            DrawRoundedRect(px, w, h, 24, 52, 80, 56, 12, chairColor, DeepInkBlack, 3f);
            // Các lỗ thoát khí lưng ghế nhựa huyền thoại
            for (int i = 0; i < 3; i++)
            {
                DrawRoundedRect(px, w, h, 36 + i * 20, 68, 12, 28, 4, DeepInkBlack, DeepInkBlack, 1f);
            }
            // Mặt ngồi ghế
            DrawRoundedRect(px, w, h, 16, 40, 96, 22, 6, highlightColor, DeepInkBlack, 3f);
            // 4 chân ghế choãi góc
            DrawRoundedRect(px, w, h, 20, 8, 14, 34, 4, shadowColor, DeepInkBlack, 2f);
            DrawRoundedRect(px, w, h, 94, 8, 14, 34, 4, shadowColor, DeepInkBlack, 2f);
            DrawRoundedRect(px, w, h, 38, 12, 10, 30, 2, chairColor, DeepInkBlack, 2f);
            DrawRoundedRect(px, w, h, 80, 12, 10, 30, 2, chairColor, DeepInkBlack, 2f);
        }

        private static void DrawPlasticTable(Color[] px, int w, int h, Color tableColor)
        {
            // Mặt bàn nhựa hình chữ nhật bo tròn
            DrawRoundedRect(px, w, h, 12, 54, 104, 36, 8, tableColor, DeepInkBlack, 4f);
            // 4 chân bàn to khỏe
            DrawRoundedRect(px, w, h, 20, 10, 16, 46, 4, tableColor * 0.7f, DeepInkBlack, 2f);
            DrawRoundedRect(px, w, h, 92, 10, 16, 46, 4, tableColor * 0.7f, DeepInkBlack, 2f);
        }

        private static void DrawIcedTeaGlass(Color[] px, int w, int h)
        {
            // Thân cốc thủy tinh
            Color glassEdge = new Color(0.70f, 0.85f, 0.95f, 0.9f);
            DrawRoundedRect(px, w, h, 36, 16, 56, 92, 6, TeaAmber, glassEdge, 4f);
            // Cục đá vuông nổi bồng bềnh
            DrawRoundedRect(px, w, h, 46, 70, 20, 20, 4, IceCubeWhite, glassEdge, 2f);
            DrawRoundedRect(px, w, h, 64, 52, 22, 22, 4, IceCubeWhite, glassEdge, 2f);
            // Bọt trà trên miệng cốc
            for (int i = 0; i < 5; i++)
            {
                DrawFilledCircle(px, w, h, 44 + i * 10, 102, 5, Color.white, glassEdge, 1f);
            }
        }

        private static void DrawSunflowerPlate(Color[] px, int w, int h)
        {
            // Đĩa nhựa đỏ
            DrawFilledCircle(px, w, h, w / 2f, h / 2f, 52f, PlasticRed, DeepInkBlack, 4f);
            // Đống hạt hướng dương đen sọc trắng
            for (int i = 0; i < 28; i++)
            {
                float angle = i * 2.4f;
                float dist = (i % 4) * 8f;
                float sx = w / 2f + Mathf.Cos(angle) * dist;
                float sy = h / 2f + Mathf.Sin(angle) * dist;
                DrawFilledCircle(px, w, h, sx, sy, 5.5f, new Color(0.18f, 0.16f, 0.14f), new Color(0.95f, 0.90f, 0.75f), 1.5f);
            }
        }

        private static void DrawDieuCayPipe(Color[] px, int w, int h)
        {
            // Ống tre điếu cày dài nghiêng chéo
            for (int t = 20; t < 108; t++)
            {
                int x = t;
                int y = t / 2 + 30;
                for (int th = -6; th <= 6; th++)
                {
                    int py = y + th;
                    if (py >= 0 && py < h && x < w)
                    {
                        px[py * w + x] = (Mathf.Abs(th) >= 5) ? DeepInkBlack : BambooBrown;
                    }
                }
            }
            // Nỏ điếu bằng đồng vàng hướng lên
            DrawRoundedRect(px, w, h, 44, 60, 18, 22, 4, NeonGold, DeepInkBlack, 2f);
        }

        private static void DrawSmokeRing(Color[] px, int w, int h)
        {
            // Vòng khói mờ ảo
            float cx = w / 2f;
            float cy = h / 2f;
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), new Vector2(cx, cy));
                    if (dist > 18f && dist < 42f)
                    {
                        float alpha = Mathf.Sin((dist - 18f) / 24f * Mathf.PI) * 0.75f;
                        px[y * w + x] = new Color(SmokePale.r, SmokePale.g, SmokePale.b, alpha);
                    }
                }
            }
        }

        private static void DrawBadgeFrame(Color[] px, int w, int h, Color baseColor, Color glowColor, bool isWings)
        {
            // Khung viền danh hiệu hình chữ nhật bo góc nghệ thuật
            DrawRoundedRect(px, w, h, 8, 20, w - 16, h - 40, 14, baseColor, DeepInkBlack, 4f);
            // Viền ánh hào quang neon phía trong
            DrawRoundedRect(px, w, h, 14, 26, w - 28, h - 52, 10, new Color(0.12f, 0.10f, 0.12f, 0.9f), glowColor, 3f);

            // Hai viên ngọc góc khung
            DrawFilledCircle(px, w, h, 16, 28, 6f, glowColor, GlossWhite, 1.5f);
            DrawFilledCircle(px, w, h, w - 16, 28, 6f, glowColor, GlossWhite, 1.5f);
            DrawFilledCircle(px, w, h, 16, h - 28, 6f, glowColor, GlossWhite, 1.5f);
            DrawFilledCircle(px, w, h, w - 16, h - 28, 6f, glowColor, GlossWhite, 1.5f);

            if (isWings)
            {
                // Cánh neon 2 bên rìa ngoài
                for (int y = 30; y < 98; y++)
                {
                    int span = (int)(Mathf.Sin((y - 30f) / 68f * Mathf.PI) * 14f);
                    for (int x = 0; x < span; x++)
                    {
                        if (x < w) px[y * w + x] = glowColor;
                        int rx = w - 1 - x;
                        if (rx >= 0) px[y * w + rx] = glowColor;
                    }
                }
            }
        }

        private static void DrawWings(Color[] px, int w, int h, Color wingColor)
        {
            // Cặp cánh ZingMe Avatar xòe rộng
            for (int y = 20; y < 110; y++)
            {
                float t = (y - 20f) / 90f;
                int wingW = (int)(Mathf.Sin(t * Mathf.PI) * 44f);
                // Cánh trái
                for (int x = 60 - wingW; x <= 60; x++)
                {
                    if (x >= 0 && x < w) px[y * w + x] = wingColor;
                }
                // Cánh phải
                for (int x = 68; x <= 68 + wingW; x++)
                {
                    if (x >= 0 && x < w) px[y * w + x] = wingColor;
                }
            }
        }

        private static void DrawCrown(Color[] px, int w, int h)
        {
            // Vương miện hoàng kim 3 đỉnh
            DrawRoundedRect(px, w, h, 28, 28, 72, 16, 3, NeonGold, DeepInkBlack, 2f);
            // 3 đỉnh tam giác
            for (int y = 44; y < 90; y++)
            {
                int leftPeakX = 36;
                int centerPeakX = 64;
                int rightPeakX = 92;

                int halfW = (int)((90 - y) * 0.35f);
                for (int dx = -halfW; dx <= halfW; dx++)
                {
                    px[y * w + (leftPeakX + dx)] = NeonGold;
                    px[y * w + (centerPeakX + dx)] = NeonGold;
                    px[y * w + (rightPeakX + dx)] = NeonGold;
                }
            }
            // Đính hồng ngọc
            DrawFilledCircle(px, w, h, 64, 76, 5f, NeonRuby, GlossWhite, 1.5f);
        }

        private static void DrawMiniPet(Color[] px, int w, int h, Color petColor, string type)
        {
            // Thân thú cưng tròn tròn chibi
            DrawFilledCircle(px, w, h, w / 2f, h / 2f - 6, 28f, petColor, DeepInkBlack, 3f);
            // Đôi tai
            DrawFilledCircle(px, w, h, w / 2f - 16, h / 2f + 20, 10f, petColor, DeepInkBlack, 2f);
            DrawFilledCircle(px, w, h, w / 2f + 16, h / 2f + 20, 10f, petColor, DeepInkBlack, 2f);
            // Hai mắt to lấp lánh kiểu anime
            DrawFilledCircle(px, w, h, w / 2f - 8, h / 2f - 2, 4f, DeepInkBlack, GlossWhite, 1f);
            DrawFilledCircle(px, w, h, w / 2f + 8, h / 2f - 2, 4f, DeepInkBlack, GlossWhite, 1f);
        }

        private static void DrawSofaThrone(Color[] px, int w, int h)
        {
            // Ngai vàng hoàng gia nệm nhung đỏ viền vàng
            DrawRoundedRect(px, w, h, 14, 16, 100, 96, 14, NeonGold, DeepInkBlack, 4f);
            DrawRoundedRect(px, w, h, 24, 30, 80, 72, 8, new Color(0.75f, 0.12f, 0.18f), NeonGold, 3f);
            // 2 tay vịn ngai vàng chạm rồng
            DrawRoundedRect(px, w, h, 8, 24, 20, 48, 6, RoyalDragonGold, DeepInkBlack, 2f);
            DrawRoundedRect(px, w, h, 100, 24, 20, 48, 6, RoyalDragonGold, DeepInkBlack, 2f);
        }

        private static void DrawTeaLadyHead(Color[] px, int w, int h)
        {
            // Khuôn mặt phúc hậu của Bà Tám
            DrawFilledCircle(px, w, h, w / 2f, h / 2f, 42f, new Color(0.98f, 0.85f, 0.72f), DeepInkBlack, 3f);
            // Khăn rằn / nón lá / búi tóc củ hành phố cổ
            DrawFilledCircle(px, w, h, w / 2f, h / 2f + 38, 22f, DeepInkBlack, DeepInkBlack, 1f);
            // Nụ cười tươi rói
            DrawFilledCircle(px, w, h, w / 2f - 14, h / 2f + 6, 5f, DeepInkBlack, GlossWhite, 1f);
            DrawFilledCircle(px, w, h, w / 2f + 14, h / 2f + 6, 5f, DeepInkBlack, GlossWhite, 1f);
            // Nốt ruồi duyên cạnh mép
            DrawFilledCircle(px, w, h, w / 2f + 16, h / 2f - 14, 3f, DeepInkBlack, DeepInkBlack, 1f);
        }

        private static void DrawGrabBike(Color[] px, int w, int h)
        {
            // Màu xanh Grab đặc trưng
            Color grabGreen = new Color(0.0f, 0.70f, 0.35f);
            DrawRoundedRect(px, w, h, 20, 36, 88, 56, 12, grabGreen, DeepInkBlack, 3f);
            // 2 bánh xe đen
            DrawFilledCircle(px, w, h, 34, 30, 18f, DeepInkBlack, Color.gray, 4f);
            DrawFilledCircle(px, w, h, 94, 30, 18f, DeepInkBlack, Color.gray, 4f);
            // Gương chiếu hậu
            DrawRoundedRect(px, w, h, 84, 88, 6, 16, 2, DeepInkBlack, DeepInkBlack, 1f);
        }

        private static void DrawLotteryTicket(Color[] px, int w, int h)
        {
            // Vé số kiến thiết hình chữ nhật viền hồng cam
            DrawRoundedRect(px, w, h, 16, 32, 96, 64, 6, new Color(1.0f, 0.95f, 0.80f), new Color(0.95f, 0.25f, 0.40f), 4f);
            // Dãy số may mắn 79
            DrawRoundedRect(px, w, h, 32, 48, 64, 30, 4, new Color(0.95f, 0.20f, 0.20f), DeepInkBlack, 2f);
        }

        private static void DrawBreadLoaf(Color[] px, int w, int h)
        {
            // Ổ bánh mì vàng ươm giòn rụm
            Color crustGold = new Color(0.92f, 0.65f, 0.20f);
            DrawRoundedRect(px, w, h, 18, 40, 92, 48, 18, crustGold, DeepInkBlack, 3f);
            // 3 vệt rạch khía bánh mì
            for (int i = 0; i < 3; i++)
            {
                DrawRoundedRect(px, w, h, 36 + i * 22, 54, 8, 20, 3, new Color(0.98f, 0.88f, 0.50f), DeepInkBlack, 1f);
            }
        }
    }
}
