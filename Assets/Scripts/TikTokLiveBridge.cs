using System;
using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace TikTokFactionWar
{
    [Serializable]
    public class TikTokBridgeEvent
    {
        public string eventType;   // chat, gift, like, follow, share, status
        public string userId;
        public string nickname;
        public string avatarUrl;
        public string message;
        public string giftName;
        public int repeatCount;
        public int diamondCount;
        public int value;
    }

    /// <summary>
    /// Connects Unity to the local Node.js TikTok Live Connector (ws://127.0.0.1:8089)
    /// AND provides Keyboard Simulation + Automatic Bot Swarm Mode for instant live testing.
    /// </summary>
    public class TikTokLiveBridge : MonoBehaviour
    {
        public static TikTokLiveBridge Instance { get; private set; }

        [Header("WebSocket Settings")]
        public string bridgeUrl = "ws://127.0.0.1:8089";
        public string targetTikTokUsername = "";
        public bool autoBotSwarm = true;

        [Header("UI Status")]
        public TextMeshProUGUI connectionStatusText;
        public TMP_InputField usernameInputField;

        private ClientWebSocket _ws;
        private CancellationTokenSource _cts;
        private readonly ConcurrentQueue<TikTokBridgeEvent> _eventQueue = new ConcurrentQueue<TikTokBridgeEvent>();
        private float _botTimer;

        private static readonly string[] MockViewerNames =
        {
            "MinhTuan99", "LinhChi_TikTok", "HoangBoss", "BaoNgoc2k", "KayTee_VN",
            "ThanhDat_Pro", "MeoBeo_Live", "HungDragon", "ThaoVy_Xinh", "QuocAnh_Gaming",
            "DucPhuc_Fan", "SieuCapVip", "TigerKing88", "Anna_Nguyen", "PhongThan_VN"
        };

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            UpdateStatusLabel("Chế độ Giả Lập Live (Bấm B bật/tắt Bot, 1/2/Q/W/E/R/T test quà)");
            _ = TryConnectWebSocketAsync();
        }

        private void OnDestroy()
        {
            DisconnectWebSocket();
        }

        private void Update()
        {
            while (_eventQueue.TryDequeue(out TikTokBridgeEvent ev))
            {
                ProcessTikTokEvent(ev);
            }

            HandleKeyboardShortcuts();

            if (autoBotSwarm && FactionWarManager.Instance != null && FactionWarManager.Instance.CurrentState == GameState.Playing)
            {
                _botTimer += Time.deltaTime;
                if (_botTimer >= 0.48f)
                {
                    _botTimer = 0f;
                    SimulateRandomViewerEvent();
                }
            }
        }

        public void ToggleBotSwarm()
        {
            autoBotSwarm = !autoBotSwarm;
            string stateStr = autoBotSwarm ? "BẬT" : "TẮT";
            UpdateStatusLabel($"Bot Swarm: {stateStr} | Phím 1/2: Lính | Q/W/E/R/T: Quà Đỏ | U/I/O/P/L: Quà Xanh");
            if (FactionWarManager.Instance != null)
            {
                FactionWarManager.Instance.ShowMilestoneBanner($"[BOT SWARM] GIẢ LẬP KHÁN GIẢ: {stateStr}", Palette.AccentGold);
            }
        }

        public void ConnectToStreamerFromInput()
        {
            if (usernameInputField != null && !string.IsNullOrWhiteSpace(usernameInputField.text))
            {
                targetTikTokUsername = usernameInputField.text.Trim();
            }
            _ = SendStreamerConnectCommandAsync(targetTikTokUsername);
        }

        private async Task TryConnectWebSocketAsync()
        {
            DisconnectWebSocket();
            _cts = new CancellationTokenSource();
            _ws = new ClientWebSocket();

            try
            {
                using var connectTimeout = new CancellationTokenSource(TimeSpan.FromMilliseconds(800));
                using var linked = CancellationTokenSource.CreateLinkedTokenSource(_cts.Token, connectTimeout.Token);
                Uri uri = new Uri(bridgeUrl);
                await _ws.ConnectAsync(uri, linked.Token);
                Debug.Log($"[TikTokLiveBridge] Connected to {bridgeUrl}");
                UpdateStatusLabel($"Đã kết nối TikTokBridge ({bridgeUrl})");

                if (!string.IsNullOrEmpty(targetTikTokUsername))
                {
                    await SendStreamerConnectCommandAsync(targetTikTokUsername);
                }

                await ReceiveLoopAsync(_cts.Token);
            }
            catch (Exception)
            {
                // Normal when Node.js server.js is not running yet; keyboard & Bot Swarm remain active
            }
        }

        public async Task SendStreamerConnectCommandAsync(string username)
        {
            if (string.IsNullOrWhiteSpace(username)) return;
            targetTikTokUsername = username.Replace("@", "").Trim();

            if (_ws == null || _ws.State != WebSocketState.Open)
            {
                await TryConnectWebSocketAsync();
            }

            if (_ws != null && _ws.State == WebSocketState.Open)
            {
                string payload = $"{{\"action\":\"connect\",\"username\":\"{targetTikTokUsername}\"}}";
                byte[] bytes = Encoding.UTF8.GetBytes(payload);
                await _ws.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, CancellationToken.None);
                UpdateStatusLabel($"Đang kết nối phòng Live @{targetTikTokUsername}...");
            }
        }

        private async Task ReceiveLoopAsync(CancellationToken token)
        {
            byte[] buffer = new byte[8192];
            while (_ws != null && _ws.State == WebSocketState.Open && !token.IsCancellationRequested)
            {
                try
                {
                    var seg = new ArraySegment<byte>(buffer);
                    WebSocketReceiveResult res = await _ws.ReceiveAsync(seg, token);
                    if (res.MessageType == WebSocketMessageType.Close) break;

                    string json = Encoding.UTF8.GetString(buffer, 0, res.Count);
                    TikTokBridgeEvent ev = JsonUtility.FromJson<TikTokBridgeEvent>(json);
                    if (ev != null)
                    {
                        _eventQueue.Enqueue(ev);
                    }
                }
                catch (Exception)
                {
                    break;
                }
            }
        }

        private void DisconnectWebSocket()
        {
            try
            {
                _cts?.Cancel();
                _ws?.Dispose();
            }
            catch (Exception) { }
            _ws = null;
        }

        public void ProcessTikTokEvent(TikTokBridgeEvent ev)
        {
            if (FactionWarManager.Instance == null || ev == null) return;

            if (ev.eventType == "status")
            {
                UpdateStatusLabel($"TikTok Live: {ev.message}");
                return;
            }

            if (ev.eventType == "chat")
            {
                string msg = (ev.message ?? "").Trim().ToLowerInvariant();
                int preferred = 0;
                if (msg == "1" || msg.Contains("đỏ") || msg.Contains("do") || msg.Contains("red") || msg.Contains("vn"))
                    preferred = 1;
                else if (msg == "2" || msg.Contains("xanh") || msg.Contains("blue") || msg.Contains("qt"))
                    preferred = 2;

                int faction = FactionWarManager.Instance.GetOrAssignViewerFaction(ev.userId, preferred);
                FactionWarManager.Instance.SpawnViewerUnit(ev.userId, ev.nickname, ev.avatarUrl, faction, UnitTier.Infantry);
            }
            else if (ev.eventType == "like" || ev.eventType == "follow" || ev.eventType == "share")
            {
                int faction = FactionWarManager.Instance.GetOrAssignViewerFaction(ev.userId, 0);
                FactionWarManager.Instance.SpawnViewerUnit(ev.userId, ev.nickname, ev.avatarUrl, faction, UnitTier.Infantry);
            }
            else if (ev.eventType == "gift")
            {
                int faction = FactionWarManager.Instance.GetOrAssignViewerFaction(ev.userId, 0);
                int coins = Mathf.Max(1, ev.diamondCount > 0 ? ev.diamondCount : ev.value);

                if (coins >= 2000)
                {
                    FactionWarManager.Instance.TriggerTacticalNuke(ev.userId, ev.nickname, faction);
                }
                else if (coins >= 300)
                {
                    FactionWarManager.Instance.SpawnViewerUnit(ev.userId, ev.nickname, ev.avatarUrl, faction, UnitTier.Dragon);
                }
                else if (coins >= 50)
                {
                    FactionWarManager.Instance.SpawnViewerUnit(ev.userId, ev.nickname, ev.avatarUrl, faction, UnitTier.Mecha);
                }
                else if (coins >= 5)
                {
                    FactionWarManager.Instance.SpawnViewerUnit(ev.userId, ev.nickname, ev.avatarUrl, faction, UnitTier.Tank);
                }
                else
                {
                    FactionWarManager.Instance.SpawnViewerUnit(ev.userId, ev.nickname, ev.avatarUrl, faction, UnitTier.Squad);
                }
            }
        }

        private void HandleKeyboardShortcuts()
        {
            Keyboard kb = Keyboard.current;
            if (kb == null || FactionWarManager.Instance == null) return;

            if (kb.spaceKey.wasPressedThisFrame)
            {
                if (FactionWarManager.Instance.CurrentState == GameState.Title)
                    FactionWarManager.Instance.StartMatchFromTitle();
                else if (FactionWarManager.Instance.CurrentState == GameState.Ended)
                    FactionWarManager.Instance.RestartCleanRun();
            }

            if (kb.escapeKey.wasPressedThisFrame)
            {
                FactionWarManager.Instance.TogglePause();
            }

            if (kb.bKey.wasPressedThisFrame)
            {
                ToggleBotSwarm();
            }

            if (kb.f1Key.wasPressedThisFrame) FactionWarManager.Instance.ApplyThemePreset(0);
            if (kb.f2Key.wasPressedThisFrame) FactionWarManager.Instance.ApplyThemePreset(1);
            if (kb.f3Key.wasPressedThisFrame) FactionWarManager.Instance.ApplyThemePreset(2);
            if (kb.f4Key.wasPressedThisFrame) FactionWarManager.Instance.ApplyThemePreset(3);

            // Top Faction (Phe 1 - Đỏ) hotkeys: 1, Q, W, E, R, T
            if (kb.digit1Key.wasPressedThisFrame)
                FactionWarManager.Instance.SpawnViewerUnit("top_hero", "ChiếnBinhĐỏ", "", 1, UnitTier.Infantry);
            if (kb.qKey.wasPressedThisFrame)
                FactionWarManager.Instance.SpawnViewerUnit("top_rose", "HoaHồngĐỏ", "", 1, UnitTier.Squad);
            if (kb.wKey.wasPressedThisFrame)
                FactionWarManager.Instance.SpawnViewerUnit("top_tank", "TankĐỏ_VIP", "", 1, UnitTier.Tank);
            if (kb.eKey.wasPressedThisFrame)
                FactionWarManager.Instance.SpawnViewerUnit("top_mecha", "MechaĐỏ_Pro", "", 1, UnitTier.Mecha);
            if (kb.rKey.wasPressedThisFrame)
                FactionWarManager.Instance.SpawnViewerUnit("top_dragon", "RồngĐỏ_Boss", "", 1, UnitTier.Dragon);
            if (kb.tKey.wasPressedThisFrame)
                FactionWarManager.Instance.TriggerTacticalNuke("top_whale", "ĐạiGia_PheĐỏ", 1);

            // Bottom Faction (Phe 2 - Xanh) hotkeys: 2, U, I, O, P, L
            if (kb.digit2Key.wasPressedThisFrame)
                FactionWarManager.Instance.SpawnViewerUnit("bot_hero", "ChiếnBinhXanh", "", 2, UnitTier.Infantry);
            if (kb.uKey.wasPressedThisFrame)
                FactionWarManager.Instance.SpawnViewerUnit("bot_rose", "HoaHồngXanh", "", 2, UnitTier.Squad);
            if (kb.iKey.wasPressedThisFrame)
                FactionWarManager.Instance.SpawnViewerUnit("bot_tank", "TankXanh_VIP", "", 2, UnitTier.Tank);
            if (kb.oKey.wasPressedThisFrame)
                FactionWarManager.Instance.SpawnViewerUnit("bot_mecha", "MechaXanh_Pro", "", 2, UnitTier.Mecha);
            if (kb.pKey.wasPressedThisFrame)
                FactionWarManager.Instance.SpawnViewerUnit("bot_dragon", "RồngXanh_Boss", "", 2, UnitTier.Dragon);
            if (kb.lKey.wasPressedThisFrame)
                FactionWarManager.Instance.TriggerTacticalNuke("bot_whale", "ĐạiGia_PheXanh", 2);
        }

        public void SimulateRandomViewerEvent()
        {
            if (FactionWarManager.Instance == null) return;

            int idx = UnityEngine.Random.Range(0, MockViewerNames.Length);
            string nick = MockViewerNames[idx];
            string uid = $"sim_{idx}";
            int faction = (idx % 2 == 0) ? 1 : 2;

            float roll = UnityEngine.Random.value;
            if (roll < 0.62f)
            {
                FactionWarManager.Instance.SpawnViewerUnit(uid, nick, "", faction, UnitTier.Infantry);
            }
            else if (roll < 0.84f)
            {
                FactionWarManager.Instance.SpawnViewerUnit(uid, nick, "", faction, UnitTier.Squad);
            }
            else if (roll < 0.94f)
            {
                FactionWarManager.Instance.SpawnViewerUnit(uid, nick, "", faction, UnitTier.Tank);
            }
            else if (roll < 0.985f)
            {
                FactionWarManager.Instance.SpawnViewerUnit(uid, nick, "", faction, UnitTier.Mecha);
            }
            else
            {
                FactionWarManager.Instance.SpawnViewerUnit(uid, nick, "", faction, UnitTier.Dragon);
            }
        }

        private void UpdateStatusLabel(string text)
        {
            if (connectionStatusText != null)
            {
                connectionStatusText.text = text;
            }
        }
    }
}
