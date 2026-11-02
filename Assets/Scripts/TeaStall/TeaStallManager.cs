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
    /// Bộ não điều hành toàn bộ Quán Trà Đá Vỉa Hè 2.5D:
    /// - Quản lý 16 ghế ngồi vỉa hè + Ngai Vàng VIP
    /// - Quản lý khách hàng, tính điểm EXP, thăng cấp Skin/Khung ZingMe
    /// - Xử lý 3 Danh Hiệu Độc Tôn Cướp Ngôi (KOTH Realtime)
    /// - Xử lý Vé Số (#veso), Loa Kẹo Kéo Quẩy, Trật Tự Phường Ôm Ghế, Đại Gia Bao Quán
    /// - HUD 9:16 và Tích hợp TikTok Live Bridge + Bot Simulator
    /// </summary>
    public class TeaStallManager : MonoBehaviour
    {
        public static TeaStallManager Instance { get; private set; }

        [Header("Cấu hình Quán Trà Đá")]
        public Transform CustomerParent;
        public Transform EnvironmentParent;
        public StreetNpcAgent TeaLady;

        // Quản lý ghế ngồi
        private readonly SeatInfo[] _seats = new SeatInfo[16];
        private readonly Dictionary<string, TeaCustomerAgent> _customers = new Dictionary<string, TeaCustomerAgent>();

        // Thống kê King of the Hill (KOTH)
        private readonly Dictionary<string, int> _commentCounts = new Dictionary<string, int>();
        private readonly Dictionary<string, int> _likeCounts = new Dictionary<string, int>();
        private readonly Dictionary<string, int> _donateCoins = new Dictionary<string, int>();

        private string _kothCommentLeaderId = "";
        private string _kothLikeLeaderId = "";
        private string _kothDonateLeaderId = "";

        // UI HUD References (World Space Canvas hoặc TMPro)
        private TextMeshPro _hudHeaderTitle;
        private TextMeshPro _hudKothBanner;
        private TextMeshPro _hudPriceMenu;
        private TextMeshPro _hudVipLeaderboard;

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

            // Bắt đầu chu kỳ khịa yêu khi vắng khách
            _idleTeaseCoroutine = StartCoroutine(IdleTeaseLoop());
            // Bắt đầu sinh NPC đường phố qua lại
            _streetNpcSpawnerCoroutine = StartCoroutine(StreetNpcSpawnLoop());

            // Chào mừng mở quán
            if (TeaLady != null)
            {
                TeaLady.Say("🏮 Quán Trà Đá Bà Tám chính thức mở cửa! Kính mời bà con vỉa hè ghé uống nước chém gió!", 5f);
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

        // ==================== KHỞI TẠO 16 GHẾ NGỒI VỈA HÈ ====================
        private void InitializeSeats()
        {
            // Bố trí 16 ghế theo phối cảnh 2.5D cực đẹp quanh 4 bàn nhựa và 1 Ngai Vàng VIP ở trung tâm
            // Ghế 0: Ngai Vàng VIP (Chính giữa tiền cảnh)
            _seats[0] = new SeatInfo { Index = 0, Position = new Vector3(0.0f, -1.35f, 0f), IsOccupied = false, IsThrone = true };

            // Bàn 1 (Góc trái trên): 3 ghế
            _seats[1] = new SeatInfo { Index = 1, Position = new Vector3(-2.8f, 0.7f, 0f), IsOccupied = false, IsThrone = false };
            _seats[2] = new SeatInfo { Index = 2, Position = new Vector3(-2.0f, 0.9f, 0f), IsOccupied = false, IsThrone = false };
            _seats[3] = new SeatInfo { Index = 3, Position = new Vector3(-1.4f, 0.6f, 0f), IsOccupied = false, IsThrone = false };

            // Bàn 2 (Góc phải trên): 3 ghế
            _seats[4] = new SeatInfo { Index = 4, Position = new Vector3(1.4f, 0.6f, 0f), IsOccupied = false, IsThrone = false };
            _seats[5] = new SeatInfo { Index = 5, Position = new Vector3(2.0f, 0.9f, 0f), IsOccupied = false, IsThrone = false };
            _seats[6] = new SeatInfo { Index = 6, Position = new Vector3(2.8f, 0.7f, 0f), IsOccupied = false, IsThrone = false };

            // Bàn 3 (Góc trái dưới): 4 ghế
            _seats[7] = new SeatInfo { Index = 7, Position = new Vector3(-3.2f, -0.6f, 0f), IsOccupied = false, IsThrone = false };
            _seats[8] = new SeatInfo { Index = 8, Position = new Vector3(-2.4f, -0.8f, 0f), IsOccupied = false, IsThrone = false };
            _seats[9] = new SeatInfo { Index = 9, Position = new Vector3(-1.6f, -0.5f, 0f), IsOccupied = false, IsThrone = false };
            _seats[10] = new SeatInfo { Index = 10, Position = new Vector3(-2.2f, -1.5f, 0f), IsOccupied = false, IsThrone = false };

            // Bàn 4 (Góc phải dưới): 4 ghế
            _seats[11] = new SeatInfo { Index = 11, Position = new Vector3(1.6f, -0.5f, 0f), IsOccupied = false, IsThrone = false };
            _seats[12] = new SeatInfo { Index = 12, Position = new Vector3(2.4f, -0.8f, 0f), IsOccupied = false, IsThrone = false };
            _seats[13] = new SeatInfo { Index = 13, Position = new Vector3(3.2f, -0.6f, 0f), IsOccupied = false, IsThrone = false };
            _seats[14] = new SeatInfo { Index = 14, Position = new Vector3(2.2f, -1.5f, 0f), IsOccupied = false, IsThrone = false };

            // Ghế 15: Ghế danh dự cạnh quầy trà Bà Tám
            _seats[15] = new SeatInfo { Index = 15, Position = new Vector3(-0.9f, 1.4f, 0f), IsOccupied = false, IsThrone = false };
        }

        // ==================== KHỞI TẠO HUD GIAO DIỆN 9:16 ====================
        private void InitializeHud()
        {
            GameObject hudRoot = new GameObject("TeaStallHudRoot");
            hudRoot.transform.position = Vector3.zero;

            // 1. Header Biển Hiệu Đèn Lồng (Y = +6.8f)
            GameObject headerGo = new GameObject("HudHeaderTitle");
            headerGo.transform.SetParent(hudRoot.transform, false);
            headerGo.transform.position = new Vector3(0f, 6.8f, 0f);
            _hudHeaderTitle = headerGo.AddComponent<TextMeshPro>();
            _hudHeaderTitle.alignment = TextAlignmentOptions.Center;
            _hudHeaderTitle.fontSize = 3.6f;
            _hudHeaderTitle.fontStyle = FontStyles.Bold;
            _hudHeaderTitle.color = TeaStallPalette.NeonGold;
            _hudHeaderTitle.outlineWidth = 0.28f;
            _hudHeaderTitle.outlineColor = Color.black;
            _hudHeaderTitle.sortingOrder = 100;
            _hudHeaderTitle.text = "🏮 QUÁN TRÀ ĐÁ BÀ TÁM - PHỐ CỔ HÀ NỘI 🏮";

            // 2. Banner Vinh Danh 3 Vua King of the Hill (Y = +6.0f)
            GameObject kothGo = new GameObject("HudKothBanner");
            kothGo.transform.SetParent(hudRoot.transform, false);
            kothGo.transform.position = new Vector3(0f, 6.05f, 0f);
            _hudKothBanner = kothGo.AddComponent<TextMeshPro>();
            _hudKothBanner.alignment = TextAlignmentOptions.Center;
            _hudKothBanner.fontSize = 2.3f;
            _hudKothBanner.fontStyle = FontStyles.Bold;
            _hudKothBanner.outlineWidth = 0.22f;
            _hudKothBanner.outlineColor = Color.black;
            _hudKothBanner.sortingOrder = 100;
            UpdateKothHudBanner();

            // 3. Menu Bảng Giá Vỉa Hè (Góc trái trên: X = -3.1f, Y = 4.6f)
            GameObject menuGo = new GameObject("HudPriceMenu");
            menuGo.transform.SetParent(hudRoot.transform, false);
            menuGo.transform.position = new Vector3(-3.1f, 4.6f, 0f);
            _hudPriceMenu = menuGo.AddComponent<TextMeshPro>();
            _hudPriceMenu.alignment = TextAlignmentOptions.TopLeft;
            _hudPriceMenu.fontSize = 2.0f;
            _hudPriceMenu.lineSpacing = 15f;
            _hudPriceMenu.color = new Color(1.0f, 0.95f, 0.85f);
            _hudPriceMenu.sortingOrder = 100;
            _hudPriceMenu.text = "<b>📋 BẢNG GIÁ & MENU:</b>\n" +
                                 "• Cmt: Vào quán + Trà đá\n" +
                                 "• #veso: Bốc vé số may mắn\n" +
                                 "• 20 Tim: Đĩa Hướng dương\n" +
                                 "• 1 Xu: Điếu cày Thuốc lào\n" +
                                 "• 10 Xu: Mẹt Nem chua rán\n" +
                                 "• 50 Xu: Loa Kẹo Kéo Vũ Trường\n" +
                                 "• 100 Xu: Bao Cả Quán + Cánh Vàng";

            // 4. Bảng Vàng Khách Quen VIP (Góc phải trên: X = 1.3f, Y = 4.6f)
            GameObject vipGo = new GameObject("HudVipLeaderboard");
            vipGo.transform.SetParent(hudRoot.transform, false);
            vipGo.transform.position = new Vector3(1.3f, 4.6f, 0f);
            _hudVipLeaderboard = vipGo.AddComponent<TextMeshPro>();
            _hudVipLeaderboard.alignment = TextAlignmentOptions.TopLeft;
            _hudVipLeaderboard.fontSize = 2.0f;
            _hudVipLeaderboard.lineSpacing = 15f;
            _hudVipLeaderboard.color = TeaStallPalette.NeonGold;
            _hudVipLeaderboard.sortingOrder = 100;
            UpdateVipLeaderboardHud();
        }

        private void UpdateKothHudBanner()
        {
            string donator = !string.IsNullOrEmpty(_kothDonateLeaderId) && _customers.TryGetValue(_kothDonateLeaderId, out var c1) ? c1.Nickname : "Chưa có";
            string chatter = !string.IsNullOrEmpty(_kothCommentLeaderId) && _customers.TryGetValue(_kothCommentLeaderId, out var c2) ? c2.Nickname : "Chưa có";
            string liker   = !string.IsNullOrEmpty(_kothLikeLeaderId)    && _customers.TryGetValue(_kothLikeLeaderId, out var c3) ? c3.Nickname : "Chưa có";

            _hudKothBanner.text = $"👑 <color=#FFD700>Trùm Bao Quán: {donator}</color>  |  " +
                                  $"🌪️ <color=#00FFFF>Đệ Nhất Gió: {chatter}</color>  |  " +
                                  $"💖 <color=#FF69B4>Chiến Thần Tim: {liker}</color>";
        }

        private void UpdateVipLeaderboardHud()
        {
            var list = new List<TeaCustomerAgent>(_customers.Values);
            list.Sort((a, b) => b.Exp.CompareTo(a.Exp));

            string t = "<b>🏆 BẢNG VÀNG VIP:</b>\n";
            int count = Mathf.Min(4, list.Count);
            if (count == 0)
            {
                t += "<color=#888888>Chưa có khách ngồi...</color>";
            }
            else
            {
                for (int i = 0; i < count; i++)
                {
                    var c = list[i];
                    string medal = i == 0 ? "🥇" : (i == 1 ? "🥈" : (i == 2 ? "🥉" : "🎖️"));
                    t += $"{medal} {c.Nickname} ({c.Exp} EXP)\n";
                }
            }
            _hudVipLeaderboard.text = t;
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

            // Tìm ghế trống
            int seatIdx = FindBestSeatIndex();
            Vector3 seatPos = (seatIdx >= 0) ? _seats[seatIdx].Position : new Vector3(UnityEngine.Random.Range(-3f, 3f), UnityEngine.Random.Range(-1.5f, 0.5f), 0f);

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

            // Chào mừng khách mới
            if (TeaLady != null)
            {
                string welcome = TeaLadyDialogueBank.GetWelcomeLine(nickname, agent.VipTier);
                TeaLady.Say(welcome, 4.0f);
            }

            UpdateVipLeaderboardHud();
            return agent;
        }

        private int FindBestSeatIndex()
        {
            // Nếu ghế thường còn trống thì ưu tiên ghế thường (1-15), dành ghế 0 (Throne) cho đại gia
            for (int i = 1; i < _seats.Length; i++)
            {
                if (!_seats[i].IsOccupied) return i;
            }
            if (!_seats[0].IsOccupied) return 0;
            return -1; // Quán full ghế
        }

        // ==================== XỬ LÝ SỰ KIỆN TIKTOK LIVE ====================

        public void HandleViewerComment(string userId, string nickname, string commentText)
        {
            var customer = GetOrCreateCustomer(userId, nickname);
            customer.AddExp(10); // +10 EXP mỗi bình luận

            // Cập nhật thống kê Comment
            if (!_commentCounts.ContainsKey(userId)) _commentCounts[userId] = 0;
            _commentCounts[userId]++;
            CheckKothCommentLeader(userId);

            // Bắt câu thoại thông minh của Bà Tám
            string smartReply = TeaLadyDialogueBank.GetSmartReplyForComment(customer.Nickname, commentText, out string specialCmd);
            customer.ShowSpeechBubble(commentText, 5.0f);

            if (TeaLady != null)
            {
                TeaLady.Say(smartReply, 4.5f);
            }

            // Xử lý Lệnh Đặc Biệt
            if (!string.IsNullOrEmpty(specialCmd))
            {
                ExecuteSpecialCommand(customer, specialCmd);
            }

            UpdateVipLeaderboardHud();
        }

        public void HandleViewerLike(string userId, string nickname, int count)
        {
            var customer = GetOrCreateCustomer(userId, nickname);
            customer.AddExp(count * 2); // +2 EXP mỗi like

            if (!_likeCounts.ContainsKey(userId)) _likeCounts[userId] = 0;
            _likeCounts[userId] += count;
            CheckKothLikeLeader(userId);

            // Hoạt ảnh cắn hướng dương
            customer.CrackSunflowerSeeds();
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

            // Phản hồi thoại của Bà Tám
            if (TeaLady != null)
            {
                string reaction = TeaLadyDialogueBank.GetGiftReactionLine(nickname, giftName, coins);
                TeaLady.Say(reaction, 5.0f);
            }

            // Kích hoạt tính năng theo giá trị quà tặng
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

            UpdateVipLeaderboardHud();
        }

        // ==================== LOGIC ĐỔI DANH HIỆU & LỆNH ĐẶC BIỆT ====================

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
                // Bốc vé số may mắn
                int luckyNumber = UnityEngine.Random.Range(0, 100);
                bool isJackpot = (luckyNumber == 79 || luckyNumber == 68);

                if (isJackpot)
                {
                    customer.SetCustomTitle("🎰 TỶ PHÚ VÉ SỐ 79");
                    customer.AddExp(500);
                    if (TeaStallAudio.Instance != null) TeaStallAudio.Instance.PlayBaoQuanJackpot();
                    customer.ShowSpeechBubble($"🎉 ÔI TRÚNG ĐỘC ĐẮC SỐ {luckyNumber:D2} RỒI BÀ CON ƠI! TÔI THÀNH TỶ PHÚ RỒI!", 5.5f);
                    if (TeaLady != null) TeaLady.Say($"ỐI GIỜI ƠI! {customer.Nickname} TRÚNG SỐ ĐỘC ĐẮC {luckyNumber:D2}! MAU MAU BAO CẢ QUÁN ĐI EM ƠI!", 5.5f);
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
                    if (!string.IsNullOrEmpty(_kothCommentLeaderId) && _customers.TryGetValue(_kothCommentLeaderId, out var oldLeader))
                    {
                        oldLeader.SetKothTitle("");
                    }
                    _kothCommentLeaderId = userId;
                    if (_customers.TryGetValue(userId, out var newLeader))
                    {
                        newLeader.SetKothTitle(TeaStallPalette.KOTH_COMMENT);
                    }
                    UpdateKothHudBanner();
                }
            }
        }

        private void CheckKothLikeLeader(string userId)
        {
            if (string.IsNullOrEmpty(_kothLikeLeaderId) || _likeCounts[userId] > _likeCounts[_kothLikeLeaderId])
            {
                if (_kothLikeLeaderId != userId)
                {
                    if (!string.IsNullOrEmpty(_kothLikeLeaderId) && _customers.TryGetValue(_kothLikeLeaderId, out var oldLeader))
                    {
                        oldLeader.SetKothTitle("");
                    }
                    _kothLikeLeaderId = userId;
                    if (_customers.TryGetValue(userId, out var newLeader))
                    {
                        newLeader.SetKothTitle(TeaStallPalette.KOTH_LIKE);
                    }
                    UpdateKothHudBanner();
                }
            }
        }

        private void CheckKothDonateLeader(string userId)
        {
            if (string.IsNullOrEmpty(_kothDonateLeaderId) || _donateCoins[userId] > _donateCoins[_kothDonateLeaderId])
            {
                if (_kothDonateLeaderId != userId)
                {
                    if (!string.IsNullOrEmpty(_kothDonateLeaderId) && _customers.TryGetValue(_kothDonateLeaderId, out var oldLeader))
                    {
                        oldLeader.SetKothTitle("");
                    }
                    _kothDonateLeaderId = userId;
                    if (_customers.TryGetValue(userId, out var newLeader))
                    {
                        newLeader.SetKothTitle(TeaStallPalette.KOTH_DONATE);

                        // Đại gia mới được chuyển sang ngồi Ngai Vàng VIP (Seat 0)
                        RelocateToThrone(newLeader);
                    }
                    UpdateKothHudBanner();
                }
            }
        }

        private void RelocateToThrone(TeaCustomerAgent agent)
        {
            // Đưa agent vào vị trí Ngai Vàng Seat 0
            Vector3 thronePos = _seats[0].Position;
            agent.HomeSeatPosition = thronePos;
            agent.transform.position = thronePos;
            agent.RefreshVipTierVisuals();
        }

        // ==================== CÁC SỰ KIỆN LỚN CỦA QUÁN ====================

        /// <summary>
        /// Sự kiện Loa Kẹo Kéo Vũ Trường: Bật nhạc disco, toàn bộ quán quẩy banh nóc trong 8 giây!
        /// </summary>
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

        /// <summary>
        /// Sự kiện Trật Tự Phường Đi Tuần: Toàn bộ khách ôm ghế nép sát tường 5 giây!
        /// </summary>
        public void TriggerPatrolPanicEvent()
        {
            if (IsPatrolPanic) return;
            StartCoroutine(PatrolPanicCoroutine());
        }

        private IEnumerator PatrolPanicCoroutine()
        {
            IsPatrolPanic = true;

            // Vị trí mép tường an toàn phía sau quán (Y = 1.6f - 1.9f)
            float wallY = 1.75f;
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
        }

        /// <summary>
        /// Sự kiện Đại Gia Bao Cả Quán: Pháo hoa nổ vang, phát quà cho toàn thể khách!
        /// </summary>
        public void TriggerBaoQuanPartyEvent(TeaCustomerAgent host = null)
        {
            StartCoroutine(BaoQuanPartyCoroutine(host));
        }

        private IEnumerator BaoQuanPartyCoroutine(TeaCustomerAgent host)
        {
            if (TeaStallAudio.Instance != null) TeaStallAudio.Instance.PlayBaoQuanJackpot();

            string hostName = host != null ? host.Nickname : "ĐẠI GIA BÍ ẨN";
            if (TeaLady != null)
            {
                TeaLady.Say($"👑 ĐẠI GIA {hostName} BAO CẢ QUÁN! MỖI NGƯỜI 1 CỐC TRÀ ĐÁ + 1 ĐĨA NEM CHUA + 100 EXP!", 6.0f);
            }

            // Tặng EXP và trà miễn phí cho toàn bộ khách trong quán
            foreach (var kvp in _customers)
            {
                kvp.Value.AddExp(100);
                kvp.Value.DrinkTea();
            }

            UpdateVipLeaderboardHud();
            yield return null;
        }

        // ==================== COROUTINES NỀN & NPC SPAWNER ====================

        private IEnumerator IdleTeaseLoop()
        {
            while (true)
            {
                yield return new WaitForSeconds(18f);
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
                yield return new WaitForSeconds(22f);

                // Chọn ngẫu nhiên 1 trong 6 NPC đường phố
                StreetNpcType type = (StreetNpcType)UnityEngine.Random.Range(0, 6);
                SpawnStreetNpc(type);
            }
        }

        public void SpawnStreetNpc(StreetNpcType type)
        {
            GameObject npcGo = new GameObject("StreetNpc_" + type);
            var npc = npcGo.AddComponent<StreetNpcAgent>();
            npc.SetupAsStreetNpc(type);

            // Đi từ mép trái qua mép phải vỉa hè
            Vector3 startPos = new Vector3(-4.8f, -2.5f, 0f);
            Vector3 stopPos  = new Vector3(0.0f, -2.5f, 0f);
            Vector3 exitPos  = new Vector3(4.8f, -2.5f, 0f);

            npc.WalkAcrossStreet(startPos, stopPos, exitPos);
        }

        // ==================== TIKTOK LIVE BRIDGE HOOK ====================

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
            "Tài Xế Hùng", "Hoa Hướng Dương", "Tuấn Phố Cổ", "Lan Trà Đào", "Bác Ba Phi",
            "Cường Đô La", "Mai Điếu Cày", "Hải Phòng 15", "Hà Nội Băm Sáu", "Thần Tài 79"
        };

        private readonly string[] _botComments = new string[]
        {
            "Bà Tám ơi cho cốc trà đá nhiều đá!",
            "#trada",
            "#veso",
            "Trời hôm nay oi bức thế nhờ!",
            "#chemgio",
            "Quán này đông vui quá bà con ơi!",
            "#docthan",
            "Bà Tám dạo này nhìn trẻ thế!",
            "Cho đĩa nem chua rán bà ơiii!",
            "#notien"
        };

        private void UpdateBotSimulator()
        {
            _botTimer += Time.deltaTime;
            if (_botTimer > 3.5f)
            {
                _botTimer = 0f;
                string bName = _botNames[UnityEngine.Random.Range(0, _botNames.Length)];
                string bId = "bot_" + bName.Replace(" ", "");
                string bCmt = _botComments[UnityEngine.Random.Range(0, _botComments.Length)];

                HandleViewerComment(bId, bName, bCmt);

                // Thi thoảng bot thả tim hoặc donate quà
                if (UnityEngine.Random.value < 0.4f)
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

        // ==================== THANH ĐIỀU KHIỂN NHANH DƯỚI ĐÁY (ONGUI) ====================

        private void OnGUI()
        {
            // Hiển thị thanh nút bấm test tính năng ở đáy màn hình
            float btnW = Screen.width / 4f - 8f;
            float btnH = 38f;
            float y1 = Screen.height - 84f;
            float y2 = Screen.height - 42f;

            GUI.backgroundColor = new Color(0.95f, 0.75f, 0.2f);

            // Hàng 1
            if (GUI.Button(new Rect(4f, y1, btnW, btnH), "+Khách Chat"))
            {
                string rName = _botNames[UnityEngine.Random.Range(0, _botNames.Length)];
                HandleViewerComment("user_" + UnityEngine.Random.Range(100, 999), rName, "Bà Tám ơi cốc trà đá 3 ngàn!");
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

            // Hàng 2
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
