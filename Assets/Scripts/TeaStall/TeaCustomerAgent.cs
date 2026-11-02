using System;
using System.Collections;
using TMPro;
using UnityEngine;
using TikTokFactionWar;

namespace TikTokTeaStall
{
    /// <summary>
    /// Điều khiển khách hàng vỉa hè với hệ thống UI 3 Tầng trên đầu chuẩn ZingMe Avatar:
    /// - Tầng 1: Avatar tròn + Tên @Nickname
    /// - Tầng 2: Khung Danh Hiệu Nghệ Thuật (Tâm điểm thị giác) + Huy hiệu Độc Tôn (KOTH)
    /// - Tầng 3: Bong Bóng Thoại trực tiếp nảy lên sống động
    /// Kèm trọn bộ hoạt ảnh vỉa hè: Rít thuốc lào, Uống trà đá, Cắn hướng dương, Ôm ghế chạy trật tự phường!
    /// </summary>
    public class TeaCustomerAgent : MonoBehaviour
    {
        [Header("Thông tin Người Xem")]
        public string UserId;
        public string Nickname;
        public int Exp;
        public int VipTier = 1;
        public string CustomTitle = "";
        public string CurrentKothTitle = ""; // KOTH: Đệ Nhất Chém Gió, Chiến Thần Thả Tim, Trùm Bao Cả Quán
        public int SeatIndex = -1;
        public Vector3 HomeSeatPosition;

        [Header("Trạng thái Hoạt động")]
        public bool IsSeated = false;
        public bool IsDancing = false;
        public bool IsPanicking = false;

        // Visual Components
        private SpriteRenderer _bodySr;
        private SpriteRenderer _chairSr;
        private SpriteRenderer _wingsSr;
        private SpriteRenderer _petSr;
        private SpriteRenderer _tableItemSr;

        // Cụm UI 3 Tầng Trên Đầu (Overhead Stack)
        private Transform _overheadStack;
        
        // Tầng 1: Avatar & Nickname
        private Transform _tier1Root;
        private SpriteRenderer _avatarSr;
        private TextMeshPro _nameTmp;

        // Tầng 2: Khung Danh Hiệu Nghệ Thuật (TÂM ĐIỂM)
        private Transform _tier2TitleRoot;
        private SpriteRenderer _badgeFrameSr;
        private SpriteRenderer _badgeLeftWingSr;
        private SpriteRenderer _badgeRightWingSr;
        private SpriteRenderer _badgeGlowSr;
        private TextMeshPro _badgeTitleTmp;
        private TextMeshPro _kothBadgeTmp;

        // Tầng 3: Bong Bóng Chat (Trên cùng)
        private Transform _tier3SpeechBubbleRoot;
        private SpriteRenderer _speechBubbleSr;
        private SpriteRenderer _speechTailSr;
        private TextMeshPro _speechTmp;
        private Coroutine _speechBubbleCoroutine;

        // Particle/VFX Pool cho khói thuốc lào & vỏ hướng dương
        private readonly System.Collections.Generic.List<GameObject> _smokePool = new System.Collections.Generic.List<GameObject>();

        private void Awake()
        {
            BuildCustomerHierarchy();
        }

        private void Update()
        {
            AnimateVisuals();
        }

        /// <summary>
        /// Khởi tạo toàn bộ Hierarchy hiển thị 2.5D và Cụm UI 3 Tầng trên đầu bằng code thuần.
        /// </summary>
        private void BuildCustomerHierarchy()
        {
            // 1. Thân nhân vật
            GameObject bodyGo = new GameObject("BodyVisual");
            bodyGo.transform.SetParent(transform, false);
            _bodySr = bodyGo.AddComponent<SpriteRenderer>();
            _bodySr.sortingOrder = 10;

            // 2. Ghế ngồi phía dưới
            GameObject chairGo = new GameObject("ChairVisual");
            chairGo.transform.SetParent(transform, false);
            chairGo.transform.localPosition = new Vector3(0f, -0.25f, 0f);
            _chairSr = chairGo.AddComponent<SpriteRenderer>();
            _chairSr.sortingOrder = 8;

            // 3. Đôi cánh sau lưng (VIP)
            GameObject wingsGo = new GameObject("BackWings");
            wingsGo.transform.SetParent(transform, false);
            wingsGo.transform.localPosition = new Vector3(0f, 0.25f, 0f);
            _wingsSr = wingsGo.AddComponent<SpriteRenderer>();
            _wingsSr.sortingOrder = 7;
            _wingsSr.gameObject.SetActive(false);

            // 4. Thú cưng mini (Corgi / Rồng con)
            GameObject petGo = new GameObject("MiniPet");
            petGo.transform.SetParent(transform, false);
            petGo.transform.localPosition = new Vector3(0.55f, -0.15f, 0f);
            _petSr = petGo.AddComponent<SpriteRenderer>();
            _petSr.sortingOrder = 12;
            _petSr.gameObject.SetActive(false);

            // 5. Đồ uống / Đĩa hạt trên bàn
            GameObject itemGo = new GameObject("TableItem");
            itemGo.transform.SetParent(transform, false);
            itemGo.transform.localPosition = new Vector3(0.25f, -0.05f, 0f);
            _tableItemSr = itemGo.AddComponent<SpriteRenderer>();
            _tableItemSr.sortingOrder = 14;

            // ==================== CỤM UI 3 TẦNG TRÊN ĐẦU ====================
            GameObject stackGo = new GameObject("OverheadStack");
            stackGo.transform.SetParent(transform, false);
            stackGo.transform.localPosition = Vector3.zero;
            _overheadStack = stackGo.transform;

            // ----- TẦNG 1 (Sát đầu: Y = +0.95f): Avatar tròn + Tên @Nickname -----
            GameObject t1Go = new GameObject("Tier1_NameAvatar");
            t1Go.transform.SetParent(_overheadStack, false);
            t1Go.transform.localPosition = new Vector3(0f, 0.95f, 0f);
            _tier1Root = t1Go.transform;

            GameObject avGo = new GameObject("AvatarIcon");
            avGo.transform.SetParent(_tier1Root, false);
            avGo.transform.localPosition = new Vector3(-0.65f, 0f, 0f);
            avGo.transform.localScale = new Vector3(0.32f, 0.32f, 1f);
            _avatarSr = avGo.AddComponent<SpriteRenderer>();
            _avatarSr.sortingOrder = 30;
            _avatarSr.sprite = TeaStallPalette.GetFallbackSprite(FallbackSpriteType.Circle);

            GameObject nameGo = new GameObject("NicknameText");
            nameGo.transform.SetParent(_tier1Root, false);
            nameGo.transform.localPosition = new Vector3(0.05f, 0f, 0f);
            _nameTmp = nameGo.AddComponent<TextMeshPro>();
            _nameTmp.alignment = TextAlignmentOptions.MidlineLeft;
            _nameTmp.fontSize = 2.4f;
            _nameTmp.color = Color.white;
            _nameTmp.fontStyle = FontStyles.Bold;
            _nameTmp.sortingOrder = 30;
            _nameTmp.text = "@Khách";

            // ----- TẦNG 2 (TÂM ĐIỂM HÚT MẮT: Y = +1.65f): Khung Danh Hiệu Nghệ Thuật -----
            GameObject t2Go = new GameObject("Tier2_TitleBadgeRoot");
            t2Go.transform.SetParent(_overheadStack, false);
            t2Go.transform.localPosition = new Vector3(0f, 1.65f, 0f);
            _tier2TitleRoot = t2Go.transform;

            // Hào quang tỏa sáng nền
            GameObject glowGo = new GameObject("BadgeGlow");
            glowGo.transform.SetParent(_tier2TitleRoot, false);
            glowGo.transform.localScale = new Vector3(1.4f, 0.55f, 1f);
            _badgeGlowSr = glowGo.AddComponent<SpriteRenderer>();
            _badgeGlowSr.sortingOrder = 24;
            _badgeGlowSr.sprite = TeaStallPalette.GetFallbackSprite(FallbackSpriteType.RoundedRect);
            _badgeGlowSr.color = new Color(1f, 0.8f, 0f, 0.35f);

            // Cánh trái khung danh hiệu
            GameObject lWingGo = new GameObject("BadgeLeftWing");
            lWingGo.transform.SetParent(_tier2TitleRoot, false);
            lWingGo.transform.localPosition = new Vector3(-1.1f, 0f, 0f);
            lWingGo.transform.localScale = new Vector3(0.45f, 0.45f, 1f);
            _badgeLeftWingSr = lWingGo.AddComponent<SpriteRenderer>();
            _badgeLeftWingSr.sortingOrder = 25;
            _badgeLeftWingSr.sprite = TeaStallPalette.GetFallbackSprite(FallbackSpriteType.WingPair);

            // Cánh phải khung danh hiệu
            GameObject rWingGo = new GameObject("BadgeRightWing");
            rWingGo.transform.SetParent(_tier2TitleRoot, false);
            rWingGo.transform.localPosition = new Vector3(1.1f, 0f, 0f);
            rWingGo.transform.localScale = new Vector3(-0.45f, 0.45f, 1f); // lật ngang
            _badgeRightWingSr = rWingGo.AddComponent<SpriteRenderer>();
            _badgeRightWingSr.sortingOrder = 25;
            _badgeRightWingSr.sprite = TeaStallPalette.GetFallbackSprite(FallbackSpriteType.WingPair);

            // Khung chính
            GameObject badgeFrameGo = new GameObject("BadgeFrame");
            badgeFrameGo.transform.SetParent(_tier2TitleRoot, false);
            badgeFrameGo.transform.localScale = new Vector3(1.9f, 0.45f, 1f);
            _badgeFrameSr = badgeFrameGo.AddComponent<SpriteRenderer>();
            _badgeFrameSr.sortingOrder = 26;
            _badgeFrameSr.sprite = TeaStallPalette.GetFallbackSprite(FallbackSpriteType.BadgeFrameWood);

            // Chữ Danh Hiệu Cấp Bậc / Tùy Biến
            GameObject titleTxtGo = new GameObject("BadgeTitleText");
            titleTxtGo.transform.SetParent(_tier2TitleRoot, false);
            titleTxtGo.transform.localPosition = new Vector3(0f, 0f, 0f);
            _badgeTitleTmp = titleTxtGo.AddComponent<TextMeshPro>();
            _badgeTitleTmp.alignment = TextAlignmentOptions.Center;
            _badgeTitleTmp.fontSize = 2.6f;
            _badgeTitleTmp.fontStyle = FontStyles.Bold;
            _badgeTitleTmp.outlineWidth = 0.25f;
            _badgeTitleTmp.outlineColor = Color.black;
            _badgeTitleTmp.sortingOrder = 28;
            _badgeTitleTmp.text = "🩴 KHÁCH DÉP TỔ ONG";

            // Huy Hiệu Độc Tôn Cướp Ngôi (King of the Hill) nằm trên đỉnh khung danh hiệu
            GameObject kothTxtGo = new GameObject("KothBadgeText");
            kothTxtGo.transform.SetParent(_tier2TitleRoot, false);
            kothTxtGo.transform.localPosition = new Vector3(0f, 0.35f, 0f);
            _kothBadgeTmp = kothTxtGo.AddComponent<TextMeshPro>();
            _kothBadgeTmp.alignment = TextAlignmentOptions.Center;
            _kothBadgeTmp.fontSize = 2.2f;
            _kothBadgeTmp.fontStyle = FontStyles.Bold;
            _kothBadgeTmp.outlineWidth = 0.22f;
            _kothBadgeTmp.outlineColor = new Color(0.1f, 0f, 0.1f);
            _kothBadgeTmp.sortingOrder = 29;
            _kothBadgeTmp.text = "";
            _kothBadgeTmp.gameObject.SetActive(false);

            // ----- TẦNG 3 (Trên cùng: Y = +2.45f): Bong Bóng Chat ZingMe -----
            GameObject t3Go = new GameObject("Tier3_SpeechBubbleRoot");
            t3Go.transform.SetParent(_overheadStack, false);
            t3Go.transform.localPosition = new Vector3(0f, 2.45f, 0f);
            _tier3SpeechBubbleRoot = t3Go.transform;

            GameObject bubBgGo = new GameObject("BubbleBackground");
            bubBgGo.transform.SetParent(_tier3SpeechBubbleRoot, false);
            bubBgGo.transform.localScale = new Vector3(2.5f, 0.9f, 1f);
            _speechBubbleSr = bubBgGo.AddComponent<SpriteRenderer>();
            _speechBubbleSr.sortingOrder = 40;
            _speechBubbleSr.sprite = TeaStallPalette.GetFallbackSprite(FallbackSpriteType.RoundedRect);
            _speechBubbleSr.color = new Color(0.98f, 0.98f, 0.94f, 0.98f);

            // Mũi tên đuôi bong bóng trỏ xuống
            GameObject tailGo = new GameObject("BubbleTail");
            tailGo.transform.SetParent(_tier3SpeechBubbleRoot, false);
            tailGo.transform.localPosition = new Vector3(0f, -0.45f, 0f);
            tailGo.transform.localScale = new Vector3(0.2f, 0.2f, 1f);
            _speechTailSr = tailGo.AddComponent<SpriteRenderer>();
            _speechTailSr.sortingOrder = 41;
            _speechTailSr.sprite = TeaStallPalette.GetFallbackSprite(FallbackSpriteType.Circle);
            _speechTailSr.color = new Color(0.98f, 0.98f, 0.94f, 0.98f);

            GameObject bubbleTxtGo = new GameObject("SpeechText");
            bubbleTxtGo.transform.SetParent(_tier3SpeechBubbleRoot, false);
            bubbleTxtGo.transform.localPosition = new Vector3(0f, 0.02f, 0f);
            _speechTmp = bubbleTxtGo.AddComponent<TextMeshPro>();
            _speechTmp.alignment = TextAlignmentOptions.Center;
            _speechTmp.fontSize = 2.4f;
            _speechTmp.color = new Color(0.12f, 0.10f, 0.12f);
            _speechTmp.fontStyle = FontStyles.Bold;
            _speechTmp.sortingOrder = 42;
            _speechTmp.text = "";
            _speechTmp.rectTransform.sizeDelta = new Vector2(2.3f, 0.8f);

            _tier3SpeechBubbleRoot.gameObject.SetActive(false);
        }

        /// <summary>
        /// Khởi tạo dữ liệu khách hàng ban đầu.
        /// </summary>
        public void Initialize(string userId, string nickname, string avatarUrl = "", int initialExp = 0)
        {
            UserId = userId;
            Nickname = string.IsNullOrEmpty(nickname) ? "Khách Vỉa Hè" : nickname;
            Exp = initialExp;
            _nameTmp.text = Nickname.StartsWith("@") ? Nickname : "@" + Nickname;

            // Nạp Avatar (AvatarCache hoặc fallback)
            if (AvatarCache.Instance != null)
            {
                _avatarSr.sprite = AvatarCache.Instance.GetOrRequestAvatar(userId, avatarUrl, 0, (sp) =>
                {
                    if (_avatarSr != null && sp != null) _avatarSr.sprite = sp;
                });
            }
            else
            {
                _avatarSr.sprite = TeaStallPalette.GetFallbackSprite(FallbackSpriteType.Circle);
            }

            // Đồng bộ Tier & Giao diện tương ứng
            RefreshVipTierVisuals();

            // Đồ trên bàn mặc định: Cốc trà đá hổ phách
            _tableItemSr.sprite = TeaStallPalette.GetFallbackSprite(FallbackSpriteType.IcedTeaGlass);
            _tableItemSr.transform.localScale = new Vector3(0.38f, 0.38f, 1f);
        }

        /// <summary>
        /// Tăng EXP và kiểm tra nâng cấp Tier.
        /// </summary>
        public void AddExp(int amount)
        {
            Exp += amount;
            var newConfig = TeaStallPalette.GetTierByExp(Exp);
            if (newConfig.Tier != VipTier)
            {
                UpgradeTier(newConfig.Tier);
            }
            else
            {
                RefreshVipTierVisuals();
            }
        }

        /// <summary>
        /// Thăng cấp VIP Tier với hiệu ứng Fanfare rực rỡ!
        /// </summary>
        public void UpgradeTier(int newTier)
        {
            VipTier = newTier;
            RefreshVipTierVisuals();

            if (TeaStallAudio.Instance != null)
            {
                TeaStallAudio.Instance.PlayTitleUpgradeFanfare();
            }

            // Popup chúc mừng
            ShowSpeechBubble($"🎉 THĂNG CẤP DANH HIỆU {TeaStallPalette.GetTierConfig(VipTier).Title}!", 4.5f);
            StartCoroutine(PopScaleCoroutine(_tier2TitleRoot, 1.45f, 0.4f));
        }

        /// <summary>
        /// Cập nhật toàn bộ visual danh hiệu, khung, cánh, skin và ghế ngồi theo Tier.
        /// </summary>
        public void RefreshVipTierVisuals()
        {
            var config = TeaStallPalette.GetTierByExp(Exp);
            VipTier = config.Tier;

            // 1. Skin Thân Nhân Vật
            _bodySr.sprite = TeaStallPalette.GetOrLoadSprite(config.SpriteResource, FallbackSpriteType.Circle);
            _bodySr.color = Color.white;
            _bodySr.transform.localScale = new Vector3(0.55f, 0.55f, 1f);

            // 2. Ghế ngồi
            if (config.IsThroneChair)
            {
                _chairSr.sprite = TeaStallPalette.GetFallbackSprite(FallbackSpriteType.SofaThrone);
                _chairSr.color = Color.white;
                _chairSr.transform.localScale = new Vector3(0.75f, 0.75f, 1f);
            }
            else
            {
                _chairSr.sprite = TeaStallPalette.GetFallbackSprite(
                    config.ChairColor == TeaStallPalette.PlasticRed ? FallbackSpriteType.PlasticChairRed : FallbackSpriteType.PlasticChairBlue
                );
                _chairSr.color = Color.white;
                _chairSr.transform.localScale = new Vector3(0.6f, 0.6f, 1f);
            }

            // 3. Cánh lưng
            _wingsSr.gameObject.SetActive(config.HasWings);
            if (config.HasWings)
            {
                _wingsSr.sprite = TeaStallPalette.GetFallbackSprite(FallbackSpriteType.WingPair);
                _wingsSr.color = config.FrameGlowColor;
                _wingsSr.transform.localScale = new Vector3(0.65f, 0.65f, 1f);
            }

            // 4. Thú cưng đi kèm
            _petSr.gameObject.SetActive(config.HasPet);
            if (config.HasPet)
            {
                _petSr.sprite = TeaStallPalette.GetFallbackSprite(
                    config.PetType == "dragon" ? FallbackSpriteType.DragonPet : FallbackSpriteType.CorgiPet
                );
                _petSr.transform.localScale = new Vector3(0.35f, 0.35f, 1f);
            }

            // 5. Khung Danh Hiệu & Chữ Tầng 2
            _badgeFrameSr.sprite = TeaStallPalette.GetOrLoadSprite(config.BadgeResource, 
                config.Tier >= 6 ? FallbackSpriteType.BadgeFrameRoyalDragon :
                config.Tier >= 4 ? FallbackSpriteType.BadgeFrameNeonWings :
                config.Tier == 3 ? FallbackSpriteType.BadgeFrameFire : FallbackSpriteType.BadgeFrameWood
            );
            _badgeGlowSr.color = new Color(config.FrameGlowColor.r, config.FrameGlowColor.g, config.FrameGlowColor.b, 0.35f);
            _badgeLeftWingSr.color = config.FrameGlowColor;
            _badgeRightWingSr.color = config.FrameGlowColor;

            string displayTitle = !string.IsNullOrEmpty(CustomTitle) ? CustomTitle : config.Title;
            _badgeTitleTmp.text = displayTitle;
            _badgeTitleTmp.color = config.TitleColor;

            // 6. Cập nhật dòng King of the Hill (KOTH) nếu có
            if (!string.IsNullOrEmpty(CurrentKothTitle))
            {
                _kothBadgeTmp.gameObject.SetActive(true);
                _kothBadgeTmp.text = CurrentKothTitle;
                if (CurrentKothTitle.Contains("CHÉM GIÓ")) _kothBadgeTmp.color = TeaStallPalette.KothCommentColor;
                else if (CurrentKothTitle.Contains("THẢ TIM")) _kothBadgeTmp.color = TeaStallPalette.KothLikeColor;
                else _kothBadgeTmp.color = TeaStallPalette.KothDonateColor;
            }
            else
            {
                _kothBadgeTmp.gameObject.SetActive(false);
            }
        }

        public void SetCustomTitle(string title)
        {
            CustomTitle = title;
            RefreshVipTierVisuals();
        }

        public void SetKothTitle(string kothTitle)
        {
            CurrentKothTitle = kothTitle;
            RefreshVipTierVisuals();
        }

        // ==================== CÁC HOẠT ẢNH VỈA HÈ SỐNG ĐỘNG ====================

        public void ShowSpeechBubble(string message, float duration = 5.5f)
        {
            if (_speechBubbleCoroutine != null) StopCoroutine(_speechBubbleCoroutine);
            _speechBubbleCoroutine = StartCoroutine(SpeechBubbleCoroutine(message, duration));
        }

        private IEnumerator SpeechBubbleCoroutine(string text, float duration)
        {
            _speechTmp.text = text;
            _tier3SpeechBubbleRoot.gameObject.SetActive(true);

            if (TeaStallAudio.Instance != null)
            {
                TeaStallAudio.Instance.PlayChatPop();
            }

            // Ease In-Out Scale (Nảy lên)
            float elapsed = 0f;
            float popDur = 0.22f;
            _tier3SpeechBubbleRoot.localScale = Vector3.zero;

            while (elapsed < popDur)
            {
                elapsed += Time.deltaTime;
                float progress = elapsed / popDur;
                float s = Ease.OutBack(progress);
                _tier3SpeechBubbleRoot.localScale = new Vector3(s, s, 1f);
                yield return null;
            }
            _tier3SpeechBubbleRoot.localScale = Vector3.one;

            yield return new WaitForSeconds(duration);

            // Thu nhỏ biến mất
            elapsed = 0f;
            while (elapsed < 0.18f)
            {
                elapsed += Time.deltaTime;
                float s = 1f - Mathf.Clamp01(elapsed / 0.18f);
                _tier3SpeechBubbleRoot.localScale = new Vector3(s, s, 1f);
                yield return null;
            }

            _tier3SpeechBubbleRoot.gameObject.SetActive(false);
        }

        /// <summary>
        /// Uống trà đá: Giơ cốc trà lên, bọt khí nổi lên, hiện "+1 Sảng Khoái!"
        /// </summary>
        public void DrinkTea()
        {
            StartCoroutine(DrinkTeaCoroutine());
        }

        private IEnumerator DrinkTeaCoroutine()
        {
            if (TeaStallAudio.Instance != null) TeaStallAudio.Instance.PlayTeaPour();

            Vector3 startPos = _tableItemSr.transform.localPosition;
            Vector3 mouthPos = new Vector3(0.05f, 0.15f, 0f);

            // Nâng cốc lên miệng
            yield return StartCoroutine(MoveTransform(_tableItemSr.transform, mouthPos, 0.25f));
            yield return new WaitForSeconds(0.6f);

            // Đặt cốc lại bàn
            yield return StartCoroutine(MoveTransform(_tableItemSr.transform, startPos, 0.25f));

            ShowSpeechBubble("🍵 Mát ruột tỉnh cả người!", 3.5f);
        }

        /// <summary>
        /// Cắn hạt hướng dương: Tiếng rốp tách, bắn mảnh vỏ bay vèo ra đất.
        /// </summary>
        public void CrackSunflowerSeeds()
        {
            if (TeaStallAudio.Instance != null) TeaStallAudio.Instance.PlaySunflowerCrack();
            StartCoroutine(PopScaleCoroutine(_tableItemSr.transform, 1.25f, 0.15f));
            ShowSpeechBubble("🌻 Cắn hạt hướng dương giòn tan!", 2.5f);
        }

        /// <summary>
        /// Rít Điếu Cày Thuốc Lào: Ríttt kéo dài... thả 5 vòng khói bay bồng bềnh + lắc lư "Phê quááá... ☁️"
        /// </summary>
        public void SmokeDieuCay()
        {
            StartCoroutine(SmokeDieuCayCoroutine());
        }

        private IEnumerator SmokeDieuCayCoroutine()
        {
            if (TeaStallAudio.Instance != null) TeaStallAudio.Instance.PlayDieuCayRiiit();

            // Đổi đồ trên bàn thành điếu cày
            Sprite oldSprite = _tableItemSr.sprite;
            _tableItemSr.sprite = TeaStallPalette.GetFallbackSprite(FallbackSpriteType.DieuCayPipe);
            _tableItemSr.transform.localScale = new Vector3(0.45f, 0.45f, 1f);

            // Giơ điếu cày rít
            Vector3 mouthPos = new Vector3(0.08f, 0.12f, 0f);
            yield return StartCoroutine(MoveTransform(_tableItemSr.transform, mouthPos, 0.35f));

            yield return new WaitForSeconds(1.5f);

            // Nhả 5 vòng khói trắng bay lên trời
            for (int i = 0; i < 5; i++)
            {
                SpawnSmokeRing(transform.position + new Vector3(0.1f, 0.4f, 0f));
                yield return new WaitForSeconds(0.22f);
            }

            // Đặt lại điếu cày
            _tableItemSr.transform.localPosition = new Vector3(0.25f, -0.05f, 0f);
            _tableItemSr.sprite = oldSprite;

            // Người nghiêng ngả say sưa 3s
            ShowSpeechBubble("☁️ Phê quááá... 9 tầng mây rồi!", 3.5f);
            float elapsed = 0f;
            while (elapsed < 3.0f)
            {
                elapsed += Time.deltaTime;
                float tilt = Mathf.Sin(elapsed * 8f) * 6f;
                _bodySr.transform.localRotation = Quaternion.Euler(0f, 0f, tilt);
                yield return null;
            }
            _bodySr.transform.localRotation = Quaternion.identity;
        }

        private void SpawnSmokeRing(Vector3 worldPos)
        {
            GameObject smokeGo = new GameObject("SmokeRing");
            smokeGo.transform.position = worldPos;
            SpriteRenderer sr = smokeGo.AddComponent<SpriteRenderer>();
            sr.sprite = TeaStallPalette.GetFallbackSprite(FallbackSpriteType.SmokeRing);
            sr.sortingOrder = 50;
            StartCoroutine(AnimateSmokeRing(smokeGo));
        }

        private IEnumerator AnimateSmokeRing(GameObject smokeGo)
        {
            float elapsed = 0f;
            float duration = 1.8f;
            Vector3 startPos = smokeGo.transform.position;
            Vector3 endPos = startPos + new Vector3(UnityEngine.Random.Range(-0.3f, 0.3f), UnityEngine.Random.Range(1.2f, 1.8f), 0f);

            SpriteRenderer sr = smokeGo.GetComponent<SpriteRenderer>();

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                smokeGo.transform.position = Vector3.Lerp(startPos, endPos, t);
                float scale = Mathf.Lerp(0.2f, 0.75f, t);
                smokeGo.transform.localScale = new Vector3(scale, scale, 1f);
                if (sr != null)
                {
                    Color c = sr.color;
                    c.a = 1f - t;
                    sr.color = c;
                }
                yield return null;
            }

            Destroy(smokeGo);
        }

        /// <summary>
        /// Ăn mẹt nem chua rán giòn rụm nóng hổi
        /// </summary>
        public void EatNemChua()
        {
            ShowSpeechBubble("🥢 Nem chua rán giòn rụm thơm lừng!", 3.0f);
            StartCoroutine(PopScaleCoroutine(_bodySr.transform, 1.15f, 0.2f));
        }

        /// <summary>
        /// Quẩy loa kẹo kéo: Nhún nhảy theo nhịp nhạc disco
        /// </summary>
        public void DanceKeoKeo(float duration)
        {
            StartCoroutine(DanceCoroutine(duration));
        }

        private IEnumerator DanceCoroutine(float duration)
        {
            IsDancing = true;
            ShowSpeechBubble("💃 Quẩy lên bà con phố cổ ơiii!", duration);
            float elapsed = 0f;

            Vector3 basePos = transform.position;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float hop = Mathf.Abs(Mathf.Sin(elapsed * 10f)) * 0.18f;
                float lean = Mathf.Sin(elapsed * 8f) * 8f;
                transform.position = basePos + new Vector3(0f, hop, 0f);
                _bodySr.transform.localRotation = Quaternion.Euler(0f, 0f, lean);
                yield return null;
            }

            transform.position = basePos;
            _bodySr.transform.localRotation = Quaternion.identity;
            IsDancing = false;
        }

        /// <summary>
        /// Ôm ghế chạy nép vào tường né Trật Tự Phường!
        /// </summary>
        public void PanicRunFromPatrol(Vector3 safeWallPos)
        {
            StartCoroutine(PanicRunCoroutine(safeWallPos));
        }

        private IEnumerator PanicRunCoroutine(Vector3 safeWallPos)
        {
            IsPanicking = true;
            ShowSpeechBubble("😱 TRẬT TỰ PHƯỜNG! Ôm ghế né né!", 4.0f);

            // 1. Ôm ghế chạy lùi vào mép tường
            yield return StartCoroutine(MoveTransform(transform, safeWallPos, 0.65f));

            // Run rẩy nép sát tường trong lúc trật tự đi qua
            float waitTime = 4.5f;
            float elapsed = 0f;
            while (elapsed < waitTime)
            {
                elapsed += Time.deltaTime;
                float shiver = Mathf.Sin(elapsed * 30f) * 0.03f;
                _bodySr.transform.localPosition = new Vector3(shiver, 0f, 0f);
                yield return null;
            }
            _bodySr.transform.localPosition = Vector3.zero;

            // 2. Trật tự đi rồi, hí hửng vác ghế quay lại bàn
            ShowSpeechBubble("😂 Hú hồn! Trật tự đi rồi, kê ghế lại chém gió tiếp!", 3.5f);
            yield return StartCoroutine(MoveTransform(transform, HomeSeatPosition, 0.65f));

            IsPanicking = false;
        }

        // ==================== TWEEN & ANIMATION HELPERS ====================

        private void AnimateVisuals()
        {
            // Cánh vỗ nhịp nhàng ở Tầng 2
            if (_badgeLeftWingSr != null && _badgeRightWingSr != null)
            {
                float flap = Mathf.Sin(Time.time * 4.5f) * 12f;
                _badgeLeftWingSr.transform.localRotation = Quaternion.Euler(0f, 0f, flap);
                _badgeRightWingSr.transform.localRotation = Quaternion.Euler(0f, 0f, -flap);
            }

            // Hào quang danh hiệu co giãn thở nhẹ
            if (_badgeGlowSr != null)
            {
                float pulse = 1.35f + Mathf.Sin(Time.time * 3f) * 0.08f;
                _badgeGlowSr.transform.localScale = new Vector3(pulse, 0.55f, 1f);
            }

            // Cánh sau lưng vỗ nhịp
            if (_wingsSr != null && _wingsSr.gameObject.activeSelf)
            {
                float wingFlap = Mathf.Sin(Time.time * 5f) * 8f;
                _wingsSr.transform.localRotation = Quaternion.Euler(0f, 0f, wingFlap);
            }

            // Thú cưng mini nhún nhảy
            if (_petSr != null && _petSr.gameObject.activeSelf)
            {
                float petBob = Mathf.Sin(Time.time * 6f) * 0.04f;
                _petSr.transform.localPosition = new Vector3(0.55f, -0.15f + petBob, 0f);
            }
        }

        private IEnumerator MoveTransform(Transform tr, Vector3 target, float duration)
        {
            Vector3 start = tr.localPosition;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                tr.localPosition = Vector3.Lerp(start, target, Ease.OutCubic(t));
                yield return null;
            }
            tr.localPosition = target;
        }

        private IEnumerator PopScaleCoroutine(Transform tr, float maxScaleMult, float duration)
        {
            Vector3 orig = tr.localScale;
            Vector3 peaked = orig * maxScaleMult;
            float half = duration * 0.5f;

            float elapsed = 0f;
            while (elapsed < half)
            {
                elapsed += Time.deltaTime;
                tr.localScale = Vector3.Lerp(orig, peaked, elapsed / half);
                yield return null;
            }

            elapsed = 0f;
            while (elapsed < half)
            {
                elapsed += Time.deltaTime;
                tr.localScale = Vector3.Lerp(peaked, orig, elapsed / half);
                yield return null;
            }
            tr.localScale = orig;
        }
    }
}
