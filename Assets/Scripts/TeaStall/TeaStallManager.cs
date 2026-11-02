using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using TikTokFactionWar;

namespace TikTokTeaStall
{
    public struct SeatInfo
    {
        public int Index;
        public Vector3 Position;
        public bool IsOccupied;
        public string OccupantUserId;
        public bool IsThrone;
    }

    /// <summary>
    /// Bộ não điều hành toàn bộ Quán Trà Đá Vỉa Hè 2.5D chuẩn mẫu TikTok Live:
    /// - Header Neon + 4 Ô Thống Kê (ĐÃ BÁN, ĐANG NGỒI, LẦN DÔ, KOTH)
    /// - Thanh Ticker Thông Báo chạy ngang
    /// - 2 Bảng Đôi Song Song: THỰC ĐƠN VỈA HÈ (xanh rêu) & ĐẠI GIA VỈA HÈ (tím than)
    /// - 16 Ghế Ngồi phân bố 2 dãy Bàn Trái / Bàn Phải + Ngai Vàng VIP ở trung tâm
    /// - Thanh Tiến Độ "CỤNG LY • QUẨY VỈA HÈ" ở đáy màn hình
    /// - Tích hợp bot simulator + đầy đủ sự kiện thuốc lào, vé số, trật tự phường, bao cả quán
    /// </summary>
    public class TeaStallManager : MonoBehaviour
    {
        public static TeaStallManager Instance { get; private set; }

        [Header("Tham Chiếu Hierarchy")]
        public Transform CustomerParent;
        public Transform EnvironmentParent;
        public StreetNpcAgent TeaLady;

        // Quản lý 16 ghế ngồi
        private readonly SeatInfo[] _seats = new SeatInfo[16];
        private readonly Dictionary<string, TeaCustomerAgent> _customers = new Dictionary<string, TeaCustomerAgent>();

        // Chỉ số thống kê trực tiếp
        public int TotalTeaSold = 128;
        public int TotalCheersCount = 42;
        public float CheersProgress = 44f; // 0 -> 100%
        public string LatestTickerText = "🔥 @Tuan_PhoCo vừa rít Điếu Cày Thuốc Lào • 🍻 Cả quán đồng thanh DÔ lần thứ 42!";

        // Thống kê King of the Hill (KOTH)
        private readonly Dictionary<string, int> _commentCounts = new Dictionary<string, int>();
        private readonly Dictionary<string, int> _likeCounts = new Dictionary<string, int>();
        private readonly Dictionary<string, int> _donateCoins = new Dictionary<string, int>();

        private string _kothCommentLeaderId = "";
        private string _kothLikeLeaderId = "";
        private string _kothDonateLeaderId = "";

        // UI HUD References (World-Space)
        private Transform _hudRoot;
        private TextMeshPro _hudHeaderTitle;
        private TextMeshPro[] _hudStatBoxes = new TextMeshPro[4]; // 0: ĐÃ BÁN, 1: ĐANG NGỒI, 2: LẦN DÔ, 3: KOTH
        private TextMeshPro _hudTickerTmp;
        private TextMeshPro _hudMenuContentTmp;
        private TextMeshPro _hudVipContentTmp;

        // Cheers Progress Bar Visuals
        private Transform _cheersBarFillTr;
        private TextMeshPro _cheersBarTextTmp;

        // Trạng thái sự kiện
        public bool IsPartyActive = false;
        public bool IsPatrolPanic = false;
        private Coroutine _idleTeaseCoroutine;
        private Coroutine _streetNpcSpawnerCoroutine;

        // Bot Simulator
        public bool EnableBotSimulator = true;
        private float _botTimer = 0f;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                if (Application.isPlaying) Destroy(gameObject);
                return;
            }
            Instance = this;

            InitializeSeats();
        }

        private void Start()
        {
            InitializeHud();
            EnsureTeaStallAudio();
            HookTikTokBridge();

            _idleTeaseCoroutine = StartCoroutine(IdleTeaseLoop());
            _streetNpcSpawnerCoroutine = StartCoroutine(StreetNpcSpawnLoop());

            if (TeaLady != null)
            {
                TeaLady.Say("🏮 Quán Trà Đá Bà Tám mở cửa! Bình luận là có trà đá mát rượi ngay!", 5.5f);
            }
        }

        private void Update()
        {
            if (EnableBotSimulator && Application.isPlaying)
            {
                UpdateBotSimulator();
            }
        }

        private void EnsureTeaStallAudio()
        {
            if (TeaStallAudio.Instance == null)
            {
                GameObject audioGo = new GameObject("TeaStallAudio");
                audioGo.AddComponent<TeaStallAudio>();
            }
        }

        // ==================== CẤU HÌNH 16 GHẾ NGỒI 2 DÃY TRÁI - PHẢI ====================
        private void InitializeSeats()
        {
            // Ngai Vàng VIP ở trung tâm tiền cảnh (ghế 0)
            _seats[0] = new SeatInfo { Index = 0, Position = new Vector3(0.0f, -1.1f, 0f), IsOccupied = false, IsThrone = true };

            // DÃY BÀN TRÁI (X ~ -2.2f)
            // Cụm Bàn 1 (Y = +0.2f): 2 ghế
            _seats[1] = new SeatInfo { Index = 1, Position = new Vector3(-2.85f, 0.45f, 0f), IsOccupied = false, IsThrone = false };
            _seats[2] = new SeatInfo { Index = 2, Position = new Vector3(-1.55f, 0.45f, 0f), IsOccupied = false, IsThrone = false };

            // Cụm Bàn 2 (Y = -1.1f): 4 ghế
            _seats[3] = new SeatInfo { Index = 3, Position = new Vector3(-2.85f, -0.85f, 0f), IsOccupied = false, IsThrone = false };
            _seats[4] = new SeatInfo { Index = 4, Position = new Vector3(-1.55f, -0.85f, 0f), IsOccupied = false, IsThrone = false };
            _seats[5] = new SeatInfo { Index = 5, Position = new Vector3(-2.85f, -1.45f, 0f), IsOccupied = false, IsThrone = false };
            _seats[6] = new SeatInfo { Index = 6, Position = new Vector3(-1.55f, -1.45f, 0f), IsOccupied = false, IsThrone = false };

            // Cụm Bàn 3 (Y = -2.6f): 4 ghế
            _seats[7] = new SeatInfo { Index = 7, Position = new Vector3(-2.85f, -2.35f, 0f), IsOccupied = false, IsThrone = false };
            _seats[8] = new SeatInfo { Index = 8, Position = new Vector3(-1.55f, -2.35f, 0f), IsOccupied = false, IsThrone = false };
            _seats[9] = new SeatInfo { Index = 9, Position = new Vector3(-2.85f, -2.95f, 0f), IsOccupied = false, IsThrone = false };
            _seats[10] = new SeatInfo { Index = 10, Position = new Vector3(-1.55f, -2.95f, 0f), IsOccupied = false, IsThrone = false };

            // DÃY BÀN PHẢI (X ~ +2.2f)
            // Cụm Bàn 4 (Y = +0.2f): 2 ghế
            _seats[11] = new SeatInfo { Index = 11, Position = new Vector3(1.55f, 0.45f, 0f), IsOccupied = false, IsThrone = false };
            _seats[12] = new SeatInfo { Index = 12, Position = new Vector3(2.85f, 0.45f, 0f), IsOccupied = false, IsThrone = false };

            // Cụm Bàn 5 (Y = -1.1f): 2 ghế
            _seats[13] = new SeatInfo { Index = 13, Position = new Vector3(1.55f, -0.85f, 0f), IsOccupied = false, IsThrone = false };
            _seats[14] = new SeatInfo { Index = 14, Position = new Vector3(2.85f, -0.85f, 0f), IsOccupied = false, IsThrone = false };

            // Cụm Bàn 6 (Y = -2.6f): 1 ghế
            _seats[15] = new SeatInfo { Index = 15, Position = new Vector3(2.20f, -2.55f, 0f), IsOccupied = false, IsThrone = false };
        }

        // ==================== KHỞI TẠO BỐ CỤC UI CHUẨN MẪU TIKTOK LIVE ====================
        private void InitializeHud()
        {
            GameObject hudGo = new GameObject("TeaStallHudRoot");
            hudGo.transform.position = Vector3.zero;
            _hudRoot = hudGo.transform;

            // 1. HEADER NEON TRÊN CÙNG (Y = +6.85f)
            GameObject headerGo = new GameObject("HudHeaderNeon");
            headerGo.transform.SetParent(_hudRoot, false);
            headerGo.transform.position = new Vector3(0f, 6.85f, 0f);
            _hudHeaderTitle = headerGo.AddComponent<TextMeshPro>();
            _hudHeaderTitle.alignment = TextAlignmentOptions.Center;
            _hudHeaderTitle.fontSize = 3.3f;
            _hudHeaderTitle.fontStyle = FontStyles.Bold;
            _hudHeaderTitle.color = new Color(1.0f, 0.40f, 0.70f); // Hồng neon rực rỡ
            _hudHeaderTitle.outlineWidth = 0.28f;
            _hudHeaderTitle.outlineColor = Color.black;
            _hudHeaderTitle.sortingOrder = 100;
            _hudHeaderTitle.text = "🏮 QUÁN TRÀ ĐÁ CHUNG • BÌNH LUẬN LÀ CÓ NƯỚC 🏮";

            // 2. 4 Ô THỐNG KÊ TRỰC TIẾP (Y = +6.05f)
            string[] statTitles = new string[] { "ĐÃ BÁN", "ĐANG NGỒI", "LẦN DÔ", "KOTH" };
            float[] statX = new float[] { -2.85f, -0.95f, 0.95f, 2.85f };

            for (int i = 0; i < 4; i++)
            {
                // Box nền đen bo góc viền vàng
                GameObject boxGo = new GameObject("StatBox_" + i);
                boxGo.transform.SetParent(_hudRoot, false);
                boxGo.transform.position = new Vector3(statX[i], 6.05f, 0f);
                boxGo.transform.localScale = new Vector3(1.75f, 0.75f, 1f);
                var sr = boxGo.AddComponent<SpriteRenderer>();
                sr.sprite = TeaStallPalette.GetFallbackSprite(FallbackSpriteType.RoundedRect);
                sr.color = new Color(0.12f, 0.08f, 0.06f, 0.92f); // Nâu đậm
                sr.sortingOrder = 95;

                // Chữ trong box
                GameObject txtGo = new GameObject("StatTxt");
                txtGo.transform.SetParent(boxGo.transform, false);
                txtGo.transform.localPosition = new Vector3(0f, 0f, 0f);
                var tmp = txtGo.AddComponent<TextMeshPro>();
                tmp.alignment = TextAlignmentOptions.Center;
                tmp.fontSize = 2.4f;
                tmp.fontStyle = FontStyles.Bold;
                tmp.sortingOrder = 100;
                _hudStatBoxes[i] = tmp;
            }
            UpdateStatBoxes();

            // 3. THANH THÔNG BÁO SỰ KIỆN CHẠY NGANG - TICKER (Y = +5.30f)
            GameObject tickerBgGo = new GameObject("TickerBannerBg");
            tickerBgGo.transform.SetParent(_hudRoot, false);
            tickerBgGo.transform.position = new Vector3(0f, 5.30f, 0f);
            tickerBgGo.transform.localScale = new Vector3(7.6f, 0.52f, 1f);
            var tickerSr = tickerBgGo.AddComponent<SpriteRenderer>();
            tickerSr.sprite = TeaStallPalette.GetFallbackSprite(FallbackSpriteType.RoundedRect);
            tickerSr.color = new Color(0.95f, 0.75f, 0.15f, 0.95f); // Vàng rực nổi bật
            tickerSr.sortingOrder = 95;

            GameObject tickerTxtGo = new GameObject("TickerText");
            tickerTxtGo.transform.SetParent(tickerBgGo.transform, false);
            tickerTxtGo.transform.localPosition = new Vector3(0f, 0.02f, 0f);
            _hudTickerTmp = tickerTxtGo.AddComponent<TextMeshPro>();
            _hudTickerTmp.alignment = TextAlignmentOptions.Center;
            _hudTickerTmp.fontSize = 2.1f;
            _hudTickerTmp.fontStyle = FontStyles.Bold;
            _hudTickerTmp.color = new Color(0.12f, 0.08f, 0.05f); // Chữ nâu sẫm
            _hudTickerTmp.sortingOrder = 100;
            _hudTickerTmp.text = LatestTickerText;

            // 4. BẢNG ĐÔI SONG SONG: THỰC ĐƠN VỈA HÈ & ĐẠI GIA VỈA HÈ (Y: +2.5f -> +4.8f)
            // 4.1 Bảng Trái: THỰC ĐƠN VỈA HÈ (X = -2.0f, Y = 3.65f)
            GameObject menuBoxGo = new GameObject("MenuBoardBox");
            menuBoxGo.transform.SetParent(_hudRoot, false);
            menuBoxGo.transform.position = new Vector3(-1.95f, 3.65f, 0f);
            menuBoxGo.transform.localScale = new Vector3(3.6f, 2.55f, 1f);
            var menuSr = menuBoxGo.AddComponent<SpriteRenderer>();
            menuSr.sprite = TeaStallPalette.GetFallbackSprite(FallbackSpriteType.RoundedRect);
            menuSr.color = new Color(0.05f, 0.16f, 0.12f, 0.95f); // Xanh rêu sẫm #0D261D
            menuSr.sortingOrder = 95;

            GameObject menuTxtGo = new GameObject("MenuContentText");
            menuTxtGo.transform.SetParent(menuBoxGo.transform, false);
            menuTxtGo.transform.localPosition = new Vector3(0f, 0.02f, 0f);
            _hudMenuContentTmp = menuTxtGo.AddComponent<TextMeshPro>();
            _hudMenuContentTmp.alignment = TextAlignmentOptions.TopLeft;
            _hudMenuContentTmp.fontSize = 2.1f;
            _hudMenuContentTmp.lineSpacing = 12f;
            _hudMenuContentTmp.sortingOrder = 100;
            _hudMenuContentTmp.rectTransform.sizeDelta = new Vector2(3.3f, 2.3f);
            _hudMenuContentTmp.text =
                "<b><color=#FFD700>📜 THỰC ĐƠN VỈA HÈ</color></b>\n" +
                "🍵 Trà đá / Dô ........... <color=#00FF88>FREE (Cmt)</color>\n" +
                "🌻 Hướng dương ........ <color=#FFB6C1>20 Tim</color>\n" +
                "🔥 Thuốc lào .............. <color=#FF8C00>1 Xu</color>\n" +
                "🍢 Nem chua rán ....... <color=#FFA500>10 Xu</color>\n" +
                "🎤 Loa kẹo kéo ......... <color=#FF1493>50 Xu</color>\n" +
                "👑 Bao cả quán .......... <color=#FFD700>100 Xu</color>\n" +
                "<i><color=#CCCCCC>Gõ #veso #banhmi #vip đổi danh hiệu!</color></i>";

            // 4.2 Bảng Phải: ĐẠI GIA VỈA HÈ (X = +1.95f, Y = 3.65f)
            GameObject vipBoxGo = new GameObject("VipBoardBox");
            vipBoxGo.transform.SetParent(_hudRoot, false);
            vipBoxGo.transform.position = new Vector3(1.95f, 3.65f, 0f);
            vipBoxGo.transform.localScale = new Vector3(3.6f, 2.55f, 1f);
            var vipSr = vipBoxGo.AddComponent<SpriteRenderer>();
            vipSr.sprite = TeaStallPalette.GetFallbackSprite(FallbackSpriteType.RoundedRect);
            vipSr.color = new Color(0.10f, 0.08f, 0.18f, 0.95f); // Tím than sẫm #1A142B
            vipSr.sortingOrder = 95;

            GameObject vipTxtGo = new GameObject("VipContentText");
            vipTxtGo.transform.SetParent(vipBoxGo.transform, false);
            vipTxtGo.transform.localPosition = new Vector3(0f, 0.02f, 0f);
            _hudVipContentTmp = vipTxtGo.AddComponent<TextMeshPro>();
            _hudVipContentTmp.alignment = TextAlignmentOptions.TopLeft;
            _hudVipContentTmp.fontSize = 2.1f;
            _hudVipContentTmp.lineSpacing = 12f;
            _hudVipContentTmp.sortingOrder = 100;
            _hudVipContentTmp.rectTransform.sizeDelta = new Vector2(3.3f, 2.3f);
            UpdateVipLeaderboardHud();

            // 5. THANH TIẾN ĐỘ "🍻 CỤNG LY • QUẨY VỈA HÈ" Ở ĐÁY MÀN HÌNH (Y = -4.45f)
            GameObject cheersBarRoot = new GameObject("CheersProgressBarRoot");
            cheersBarRoot.transform.SetParent(_hudRoot, false);
            cheersBarRoot.transform.position = new Vector3(0f, -4.45f, 0f);

            // Nền thanh trượt
            GameObject barBgGo = new GameObject("BarBg");
            barBgGo.transform.SetParent(cheersBarRoot.transform, false);
            barBgGo.transform.localScale = new Vector3(7.4f, 0.6f, 1f);
            var barBgSr = barBgGo.AddComponent<SpriteRenderer>();
            barBgSr.sprite = TeaStallPalette.GetFallbackSprite(FallbackSpriteType.RoundedRect);
            barBgSr.color = new Color(0.15f, 0.12f, 0.15f, 0.9f);
            barBgSr.sortingOrder = 95;

            // Thanh Fill vàng cam
            GameObject barFillGo = new GameObject("BarFill");
            barFillGo.transform.SetParent(cheersBarRoot.transform, false);
            barFillGo.transform.localPosition = new Vector3(-3.5f, 0f, 0f);
            _cheersBarFillTr = barFillGo.transform;
            var fillSr = barFillGo.AddComponent<SpriteRenderer>();
            fillSr.sprite = TeaStallPalette.GetFallbackSprite(FallbackSpriteType.RoundedRect);
            fillSr.color = new Color(1.0f, 0.65f, 0.05f); // Vàng cam ấm
            fillSr.sortingOrder = 96;

            // Chữ hiển thị tiến độ
            GameObject barTxtGo = new GameObject("BarText");
            barTxtGo.transform.SetParent(cheersBarRoot.transform, false);
            barTxtGo.transform.localPosition = new Vector3(0f, 0.02f, 0f);
            _cheersBarTextTmp = barTxtGo.AddComponent<TextMeshPro>();
            _cheersBarTextTmp.alignment = TextAlignmentOptions.Center;
            _cheersBarTextTmp.fontSize = 2.1f;
            _cheersBarTextTmp.fontStyle = FontStyles.Bold;
            _cheersBarTextTmp.color = Color.white;
            _cheersBarTextTmp.outlineWidth = 0.22f;
            _cheersBarTextTmp.outlineColor = Color.black;
            _cheersBarTextTmp.sortingOrder = 100;
            UpdateCheersBar();
        }

        private void UpdateStatBoxes()
        {
            if (_hudStatBoxes[0] == null) return;

            string kothName = !string.IsNullOrEmpty(_kothDonateLeaderId) && _customers.TryGetValue(_kothDonateLeaderId, out var c) ? c.Nickname : "Chưa có";
            if (kothName.Length > 8) kothName = kothName.Substring(0, 8) + "..";

            _hudStatBoxes[0].text = $"<color=#AAAAAA><size=70%>ĐÃ BÁN</size></color>\n<color=#FFD700>🍵 {TotalTeaSold}</color>";
            _hudStatBoxes[1].text = $"<color=#AAAAAA><size=70%>ĐANG NGỒI</size></color>\n<color=#00F0FF>🪑 {_customers.Count}/16</color>";
            _hudStatBoxes[2].text = $"<color=#AAAAAA><size=70%>LẦN DÔ</size></color>\n<color=#FF69B4>🍻 {TotalCheersCount}</color>";
            _hudStatBoxes[3].text = $"<color=#AAAAAA><size=70%>KOTH</size></color>\n<color=#FFA500>👑 {kothName}</color>";
        }

        public void SetTickerMessage(string msg)
        {
            LatestTickerText = msg;
            if (_hudTickerTmp != null) _hudTickerTmp.text = msg;
        }

        private void UpdateVipLeaderboardHud()
        {
            if (_hudVipContentTmp == null) return;

            var list = new List<TeaCustomerAgent>(_customers.Values);
            list.Sort((a, b) => b.Exp.CompareTo(a.Exp));

            string t = "<b><color=#FFD700>👑 ĐẠI GIA VỈA HÈ</color></b>\n";
            int count = Mathf.Min(5, list.Count);
            if (count == 0)
            {
                t += "<color=#888888>Chưa có ai ngồi quán...</color>";
            }
            else
            {
                for (int i = 0; i < count; i++)
                {
                    var c = list[i];
                    string medal = i == 0 ? "🥇" : (i == 1 ? "🥈" : (i == 2 ? "🥉" : $"{i + 1}."));
                    string name = c.Nickname.Length > 10 ? c.Nickname.Substring(0, 9) + ".." : c.Nickname;
                    int coins = _donateCoins.ContainsKey(c.UserId) ? _donateCoins[c.UserId] : (c.Exp / 50);
                    t += $"{medal} {name} ......... <color=#FFD700>{coins} xu</color>\n";
                }
            }
            t += "<i><color=#CCCCCC>Tổng xu hôm nay: " + (TotalTeaSold * 3) + " xu</color></i>";
            _hudVipContentTmp.text = t;
        }

        private void UpdateCheersBar()
        {
            if (_cheersBarFillTr == null || _cheersBarTextTmp == null) return;

            float pct = Mathf.Clamp01(CheersProgress / 100f);
            float maxW = 7.2f;
            _cheersBarFillTr.localScale = new Vector3(maxW * pct, 0.48f, 1f);
            _cheersBarFillTr.localPosition = new Vector3(-3.6f + (maxW * pct) / 2f, 0f, 0f);

            int timGoal = (int)(CheersProgress * 2);
            _cheersBarTextTmp.text = $"🍻 CỤNG LY • Bình luận DÔ / Thả tim  |  💖 {timGoal}/200 tim ➔ 🎶 QUẨY {(int)CheersProgress}%";
        }

        public void AddCheersProgress(float amount)
        {
            CheersProgress += amount;
            if (CheersProgress >= 100f)
            {
                CheersProgress = 0f;
                TotalCheersCount++;
                SetTickerMessage($"🍻 CẢ QUÁN ĐỒNG THANH DÔ LẦN THỨ {TotalCheersCount}! BẬT LOA KẸO KÉO QUẨY BANH NÓC!");
                TriggerKeoKeoPartyEvent();
            }
            UpdateCheersBar();
            UpdateStatBoxes();
        }

        // ==================== QUẢN LÝ KHÁCH HÀNG & CHỖ NGỒI ====================

        public TeaCustomerAgent GetOrCreateCustomer(string userId, string nickname, string avatarUrl = "")
        {
            if (string.IsNullOrEmpty(userId)) userId = "viewer_" + UnityEngine.Random.Range(100, 999);
            if (string.IsNullOrEmpty(nickname)) nickname = "Dân Chơi " + UnityEngine.Random.Range(10, 99);

            if (_customers.TryGetValue(userId, out TeaCustomerAgent existing) && existing != null)
            {
                return existing;
            }

            int seatIdx = FindBestSeatIndex();
            Vector3 seatPos = (seatIdx >= 0) ? _seats[seatIdx].Position : new Vector3(UnityEngine.Random.Range(-2.5f, 2.5f), UnityEngine.Random.Range(-2.5f, 0f), 0f);

            GameObject custGo = new GameObject("Customer_" + userId);
            if (CustomerParent != null) custGo.transform.SetParent(CustomerParent, false);
            custGo.transform.position = seatPos;

            TeaCustomerAgent agent = custGo.AddComponent<TeaCustomerAgent>();
            agent.SeatIndex = seatIdx;
            agent.HomeSeatPosition = seatPos;
            agent.IsSeated = true;
            agent.Initialize(userId, nickname, avatarUrl, 0);

            if (seatIdx >= 0)
            {
                _seats[seatIdx].IsOccupied = true;
                _seats[seatIdx].OccupantUserId = userId;
            }

            _customers[userId] = agent;
            TotalTeaSold++;

            if (TeaLady != null)
            {
                string welcome = TeaLadyDialogueBank.GetWelcomeLine(nickname, agent.VipTier);
                TeaLady.Say(welcome, 4.0f);
            }

            UpdateStatBoxes();
            UpdateVipLeaderboardHud();
            return agent;
        }

        private int FindBestSeatIndex()
        {
            for (int i = 1; i < _seats.Length; i++)
            {
                if (!_seats[i].IsOccupied) return i;
            }
            if (!_seats[0].IsOccupied) return 0;
            return -1;
        }

        // ==================== XỬ LÝ SỰ KIỆN TIKTOK LIVE ====================

        public void HandleViewerComment(string userId, string nickname, string commentText)
        {
            var customer = GetOrCreateCustomer(userId, nickname);
            customer.AddExp(10);

            if (!_commentCounts.ContainsKey(userId)) _commentCounts[userId] = 0;
            _commentCounts[userId]++;
            CheckKothCommentLeader(userId);

            // Kiểm tra các từ khóa Dô / Cạn ly để tăng Cheers
            string lower = commentText.ToLower();
            if (lower.Contains("dô") || lower.Contains("zo") || lower.Contains("cạn") || lower.Contains("100%") || lower.Contains("bia") || lower.Contains("trà"))
            {
                AddCheersProgress(8f);
                SetTickerMessage($"🍻 @{nickname} hô 'DÔ'! Cả quán cùng nâng cốc!");
            }

            string smartReply = TeaLadyDialogueBank.GetSmartReplyForComment(customer.Nickname, commentText, out string specialCmd);
            customer.ShowSpeechBubble(commentText, 5.0f);

            if (TeaLady != null)
            {
                TeaLady.Say(smartReply, 4.5f);
            }

            if (!string.IsNullOrEmpty(specialCmd))
            {
                ExecuteSpecialCommand(customer, specialCmd);
            }

            UpdateStatBoxes();
            UpdateVipLeaderboardHud();
        }

        public void HandleViewerLike(string userId, string nickname, int count)
        {
            var customer = GetOrCreateCustomer(userId, nickname);
            customer.AddExp(count * 2);

            if (!_likeCounts.ContainsKey(userId)) _likeCounts[userId] = 0;
            _likeCounts[userId] += count;
            CheckKothLikeLeader(userId);

            // Thả tim tăng tiến độ quẩy
            AddCheersProgress(count * 1.5f);
            customer.CrackSunflowerSeeds();

            UpdateStatBoxes();
            UpdateVipLeaderboardHud();
        }

        public void HandleViewerGift(string userId, string nickname, string giftName, int coins)
        {
            var customer = GetOrCreateCustomer(userId, nickname);
            int expGain = coins * 50;
            customer.AddExp(expGain);

            if (!_donateCoins.ContainsKey(userId)) _donateCoins[userId] = 0;
            _donateCoins[userId] += coins;
            CheckKothDonateLeader(userId);

            SetTickerMessage($"🎁 @{nickname} vừa tặng {giftName} ({coins} xu) cho quán Bà Tám!");

            if (TeaLady != null)
            {
                string reaction = TeaLadyDialogueBank.GetGiftReactionLine(nickname, giftName, coins);
                TeaLady.Say(reaction, 5.0f);
            }

            if (coins >= 100 || giftName.Contains("Bao"))
            {
                TriggerBaoQuanPartyEvent(customer);
            }
            else if (coins >= 50 || giftName.Contains("Loa") || giftName.Contains("Kẹo"))
            {
                TriggerKeoKeoPartyEvent();
            }
            else if (coins >= 10 || giftName.Contains("Nem"))
            {
                customer.EatNemChua();
                if (TeaLady != null) TeaLady.ServeItemToCustomer(customer.transform.position, "Đĩa Nem Chua Rán");
            }
            else if (coins >= 1 || giftName.Contains("Thuốc") || giftName.Contains("Điếu"))
            {
                customer.SmokeDieuCay();
            }
            else
            {
                customer.DrinkTea();
            }

            UpdateStatBoxes();
            UpdateVipLeaderboardHud();
        }

        private void ExecuteSpecialCommand(TeaCustomerAgent customer, string cmd)
        {
            if (cmd.StartsWith("BADGE:"))
            {
                string title = cmd.Substring(6);
                customer.SetCustomTitle(title);
            }
            else if (cmd.StartsWith("CUSTOM_BADGE:"))
            {
                string title = cmd.Substring(13);
                customer.SetCustomTitle(title);
            }
            else if (cmd == "ACTION:BUY_TICKET")
            {
                int luckyNumber = UnityEngine.Random.Range(0, 100);
                bool isJackpot = (luckyNumber == 79 || luckyNumber == 68);

                if (isJackpot)
                {
                    customer.SetCustomTitle("🎰 TỶ PHÚ VÉ SỐ 79");
                    customer.AddExp(500);
                    if (TeaStallAudio.Instance != null) TeaStallAudio.Instance.PlayBaoQuanJackpot();
                    customer.ShowSpeechBubble($"🎉 ÔI TRÚNG ĐỘC ĐẮC SỐ [{luckyNumber:D2}] RỒI BÀ CON ƠI! TÔI THÀNH TỶ PHÚ RỒI!", 5.5f);
                    SetTickerMessage($"🎰 CHÚC MỪNG @{customer.Nickname} VỪA TRÚNG VÉ SỐ ĐỘC ĐẮC [{luckyNumber:D2}]!");
                }
                else
                {
                    customer.AddExp(20);
                    customer.ShowSpeechBubble($"🎟️ Vừa bốc tờ vé số đuôi [{luckyNumber:D2}], chiều nay chờ thần tài gõ cửa!", 4.0f);
                }
            }
            else if (cmd == "ACTION:ORDER_BREAD")
            {
                customer.ShowSpeechBubble("🥖 Bánh mì giòn rụm thơm bơ quá!", 3.5f);
                customer.AddExp(15);
            }
        }

        // ==================== CẬP NHẬT 3 DANH HIỆU ĐỘC TÔN (KOTH) ====================

        private void CheckKothCommentLeader(string userId)
        {
            if (string.IsNullOrEmpty(_kothCommentLeaderId) || _commentCounts[userId] > _commentCounts[_kothCommentLeaderId])
            {
                if (_kothCommentLeaderId != userId)
                {
                    if (!string.IsNullOrEmpty(_kothCommentLeaderId) && _customers.TryGetValue(_kothCommentLeaderId, out var oldL))
                    {
                        oldL.SetKothTitle("");
                    }
                    _kothCommentLeaderId = userId;
                    if (_customers.TryGetValue(userId, out var newL))
                    {
                        newL.SetKothTitle(TeaStallPalette.KOTH_COMMENT);
                    }
                    UpdateStatBoxes();
                }
            }
        }

        private void CheckKothLikeLeader(string userId)
        {
            if (string.IsNullOrEmpty(_kothLikeLeaderId) || _likeCounts[userId] > _likeCounts[_kothLikeLeaderId])
            {
                if (_kothLikeLeaderId != userId)
                {
                    if (!string.IsNullOrEmpty(_kothLikeLeaderId) && _customers.TryGetValue(_kothLikeLeaderId, out var oldL))
                    {
                        oldL.SetKothTitle("");
                    }
                    _kothLikeLeaderId = userId;
                    if (_customers.TryGetValue(userId, out var newL))
                    {
                        newL.SetKothTitle(TeaStallPalette.KOTH_LIKE);
                    }
                    UpdateStatBoxes();
                }
            }
        }

        private void CheckKothDonateLeader(string userId)
        {
            if (string.IsNullOrEmpty(_kothDonateLeaderId) || _donateCoins[userId] > _donateCoins[_kothDonateLeaderId])
            {
                if (_kothDonateLeaderId != userId)
                {
                    if (!string.IsNullOrEmpty(_kothDonateLeaderId) && _customers.TryGetValue(_kothDonateLeaderId, out var oldL))
                    {
                        oldL.SetKothTitle("");
                    }
                    _kothDonateLeaderId = userId;
                    if (_customers.TryGetValue(userId, out var newL))
                    {
                        newL.SetKothTitle(TeaStallPalette.KOTH_DONATE);
                        RelocateToThrone(newL);
                    }
                    UpdateStatBoxes();
                }
            }
        }

        private void RelocateToThrone(TeaCustomerAgent agent)
        {
            Vector3 thronePos = _seats[0].Position;
            agent.HomeSeatPosition = thronePos;
            agent.transform.position = thronePos;
            agent.RefreshVipTierVisuals();
        }

        // ==================== CÁC SỰ KIỆN LỚN CỦA QUÁN ====================

        public void TriggerKeoKeoPartyEvent()
        {
            if (IsPartyActive) return;
            StartCoroutine(KeoKeoPartyCoroutine());
        }

        private IEnumerator KeoKeoPartyCoroutine()
        {
            IsPartyActive = true;
            if (TeaStallAudio.Instance != null) TeaStallAudio.Instance.StartKeoKeoDisco();

            foreach (var kvp in _customers)
            {
                kvp.Value.DanceKeoKeo(7.5f);
            }

            yield return new WaitForSeconds(8.0f);

            if (TeaStallAudio.Instance != null) TeaStallAudio.Instance.StopKeoKeoDisco();
            IsPartyActive = false;
        }

        public void TriggerPatrolPanicEvent()
        {
            if (IsPatrolPanic) return;
            StartCoroutine(PatrolPanicCoroutine());
        }

        private IEnumerator PatrolPanicCoroutine()
        {
            IsPatrolPanic = true;
            SetTickerMessage("🚨 TRẬT TỰ PHƯỜNG ĐI TUẦN! BÀ CON ÔM GHẾ DỌN VÀO TRONG VẠCH SƠN TRẮNG!");

            float wallY = 0.85f;
            int idx = 0;
            foreach (var kvp in _customers)
            {
                float wallX = -3.2f + (idx % 10) * 0.7f;
                Vector3 safePos = new Vector3(wallX, wallY, 0f);
                kvp.Value.PanicRunFromPatrol(safePos);
                idx++;
            }

            yield return new WaitForSeconds(5.5f);
            IsPatrolPanic = false;
            SetTickerMessage("✅ Trật tự phường đã đi qua! Kê ghế ra chém gió tiếp nào bà con!");
        }

        public void TriggerBaoQuanPartyEvent(TeaCustomerAgent host = null)
        {
            StartCoroutine(BaoQuanPartyCoroutine(host));
        }

        private IEnumerator BaoQuanPartyCoroutine(TeaCustomerAgent host)
        {
            if (TeaStallAudio.Instance != null) TeaStallAudio.Instance.PlayBaoQuanJackpot();

            string hostName = host != null ? host.Nickname : "ĐẠI GIA BÍ ẨN";
            SetTickerMessage($"👑 ĐẠI GIA @{hostName} VỪA BAO CẢ QUÁN! TOÀN BỘ KHÁCH ĐƯỢC FREE NƯỚC + 100 EXP!");

            if (TeaLady != null)
            {
                TeaLady.Say($"👑 ĐẠI GIA {hostName} BAO CẢ QUÁN! MỖI NGƯỜI 1 CỐC TRÀ ĐÁ + 1 ĐĨA NEM CHUA + 100 EXP!", 6.0f);
            }

            foreach (var kvp in _customers)
            {
                kvp.Value.AddExp(100);
                kvp.Value.DrinkTea();
            }

            UpdateStatBoxes();
            UpdateVipLeaderboardHud();
            yield return null;
        }

        // ==================== COROUTINES NỀN & NPC SPAWNER ====================

        private IEnumerator IdleTeaseLoop()
        {
            while (true)
            {
                yield return new WaitForSeconds(20f);
                if (TeaLady != null && !IsPartyActive && !IsPatrolPanic)
                {
                    string tease = TeaLadyDialogueBank.GetIdleTeaseLine();
                    TeaLady.Say(tease, 4.0f);
                }
            }
        }

        private IEnumerator StreetNpcSpawnLoop()
        {
            while (true)
            {
                yield return new WaitForSeconds(25f);
                StreetNpcType type = (StreetNpcType)UnityEngine.Random.Range(0, 6);
                SpawnStreetNpc(type);
            }
        }

        public void SpawnStreetNpc(StreetNpcType type)
        {
            GameObject npcGo = new GameObject("StreetNpc_" + type);
            var npc = npcGo.AddComponent<StreetNpcAgent>();
            npc.SetupAsStreetNpc(type);

            Vector3 startPos = new Vector3(-4.8f, -3.4f, 0f);
            Vector3 stopPos  = new Vector3(0.0f, -3.4f, 0f);
            Vector3 exitPos  = new Vector3(4.8f, -3.4f, 0f);

            npc.WalkAcrossStreet(startPos, stopPos, exitPos);
        }

        private void HookTikTokBridge()
        {
            var bridge = FindFirstObjectByType<TikTokLiveBridge>();
            if (bridge != null)
            {
                bridge.OnCommentReceived += (msg) => HandleViewerComment(msg.UniqueId, msg.Nickname, msg.Comment);
                bridge.OnLikeReceived += (like) => HandleViewerLike(like.UniqueId, like.Nickname, like.LikeCount);
                bridge.OnGiftReceived += (gift) => HandleViewerGift(gift.UniqueId, gift.Nickname, gift.GiftName, gift.RepeatCount * gift.DiamondCount);
            }
        }

        // ==================== BOT SIMULATOR VỈA HÈ ====================

        private readonly string[] _botNames = new string[]
        {
            "Hữu Lâm", "Sumi_Yeu", "AnhBaTraCu", "BuonPhien", "HoiMePhim",
            "My_Xinh", "TanPhap", "Bedatmhe", "BeBong", "Tuan_PhoCo"
        };

        private readonly string[] _botComments = new string[]
        {
            "Bà Tám ơi 1 ly trà đá!",
            "1 2 3 DÔÔÔ!",
            "#trada",
            "#veso",
            "Cạn ly bà con ơiii!",
            "#chemgio",
            "Cho đĩa hướng dương bà ơi!",
            "#docthan",
            "Trời hôm nay mát mẻ quá!",
            "#notien"
        };

        private void UpdateBotSimulator()
        {
            _botTimer += Time.deltaTime;
            if (_botTimer > 3.2f)
            {
                _botTimer = 0f;
                string bName = _botNames[UnityEngine.Random.Range(0, _botNames.Length)];
                string bId = "bot_" + bName.Replace(" ", "");
                string bCmt = _botComments[UnityEngine.Random.Range(0, _botComments.Length)];

                HandleViewerComment(bId, bName, bCmt);

                if (UnityEngine.Random.value < 0.45f)
                {
                    HandleViewerLike(bId, bName, UnityEngine.Random.Range(5, 15));
                }
                if (UnityEngine.Random.value < 0.15f)
                {
                    string[] gifts = new string[] { "Điếu cày", "Nem chua", "Hướng dương" };
                    int[] coins = new int[] { 1, 10, 5 };
                    int gIdx = UnityEngine.Random.Range(0, gifts.Length);
                    HandleViewerGift(bId, bName, gifts[gIdx], coins[gIdx]);
                }
            }
        }

        // ==================== THANH ĐIỀU KHIỂN TEST NHANH DƯỚI ĐÁY (ONGUI) ====================

        private void OnGUI()
        {
            float btnW = Screen.width / 4f - 8f;
            float btnH = 36f;
            float y1 = Screen.height - 80f;
            float y2 = Screen.height - 40f;

            GUI.backgroundColor = new Color(0.95f, 0.75f, 0.2f);

            if (GUI.Button(new Rect(4f, y1, btnW, btnH), "+Khách Chat / Dô"))
            {
                string rName = _botNames[UnityEngine.Random.Range(0, _botNames.Length)];
                HandleViewerComment("user_" + UnityEngine.Random.Range(100, 999), rName, "1 2 3 DÔ! Cho cốc trà đá!");
            }
            if (GUI.Button(new Rect(btnW + 8f, y1, btnW, btnH), "+Rít Thuốc Lào (1 Xu)"))
            {
                if (_customers.Count > 0)
                {
                    var firstCust = new List<TeaCustomerAgent>(_customers.Values)[0];
                    HandleViewerGift(firstCust.UserId, firstCust.Nickname, "Điếu cày", 1);
                }
            }
            if (GUI.Button(new Rect((btnW + 8f) * 2, y1, btnW, btnH), "+Mẹt Nem Chua (10 Xu)"))
            {
                if (_customers.Count > 0)
                {
                    var firstCust = new List<TeaCustomerAgent>(_customers.Values)[0];
                    HandleViewerGift(firstCust.UserId, firstCust.Nickname, "Mẹt Nem Chua", 10);
                }
            }
            if (GUI.Button(new Rect((btnW + 8f) * 3, y1, btnW, btnH), "+Anh Grab Ghé"))
            {
                SpawnStreetNpc(StreetNpcType.GrabDriver);
            }

            if (GUI.Button(new Rect(4f, y2, btnW, btnH), "+Cô Vé Số (#veso)"))
            {
                SpawnStreetNpc(StreetNpcType.LotteryVendor);
                if (_customers.Count > 0)
                {
                    var firstCust = new List<TeaCustomerAgent>(_customers.Values)[0];
                    HandleViewerComment(firstCust.UserId, firstCust.Nickname, "#veso");
                }
            }
            if (GUI.Button(new Rect(btnW + 8f, y2, btnW, btnH), "+Loa Kẹo Kéo Quẩy"))
            {
                SpawnStreetNpc(StreetNpcType.StreetSinger);
            }
            if (GUI.Button(new Rect((btnW + 8f) * 2, y2, btnW, btnH), "+TRẬT TỰ PHƯỜNG!"))
            {
                SpawnStreetNpc(StreetNpcType.WardPatrol);
            }
            if (GUI.Button(new Rect((btnW + 8f) * 3, y2, btnW, btnH), "👑 ĐẠI GIA BAO QUÁN!"))
            {
                string vipName = "Đại Gia Hà Thành";
                HandleViewerGift("vip_hathanh", vipName, "Bao Cả Quán", 100);
            }
        }
    }
}
