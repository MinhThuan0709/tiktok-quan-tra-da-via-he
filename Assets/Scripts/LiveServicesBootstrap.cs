using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.CloudSave;
using Unity.Services.Core;
using Unity.Services.Leaderboards;
using Unity.Services.RemoteConfig;
using UnityEngine;

namespace TikTokFactionWar
{
    /// <summary>
    /// UGS Identity + Live Floor bootstrapper (Authentication, Cloud Save 'progress', Leaderboards, Remote Config 'liveops_enabled').
    /// Follows build-live-game and craft-game-floors/live-floor.md patterns.
    /// </summary>
    public class LiveServicesBootstrap : MonoBehaviour
    {
        public static LiveServicesBootstrap Instance { get; private set; }

        public struct UserAttributes { }
        public struct AppAttributes { }

        [Serializable]
        public class SavedProgressData
        {
            public int topFactionWins;
            public int bottomFactionWins;
            public int totalMatches;
            public int bestMvpScore;
            public string lastMvpName = "Streamer";
        }

        public bool IsReady { get; private set; }
        public bool LiveOpsEnabled { get; private set; } = true;
        public string PlayerId { get; private set; } = "";
        public string LastLeaderboardRank { get; private set; } = "Rank: unavailable";
        public SavedProgressData Progress { get; private set; } = new SavedProgressData();

        private const string ProgressKey = "progress";
        private const string LeaderboardId = "faction_war_mvp";
        private const string KillSwitchFlag = "liveops_enabled";

        private float _refetchTimer;
        private const float RefetchIntervalSeconds = 120f;

        private async void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            await InitializeAndSignInAsync();
        }

        private void Update()
        {
            if (!IsReady) return;
            _refetchTimer += Time.unscaledDeltaTime;
            if (_refetchTimer >= RefetchIntervalSeconds)
            {
                _refetchTimer = 0f;
                _ = RefreshRemoteConfigAsync();
            }
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (hasFocus && IsReady)
            {
                _ = RefreshRemoteConfigAsync();
            }
        }

        private void OnApplicationQuit()
        {
            if (IsReady && LiveOpsEnabled)
            {
                _ = SaveProgressAsync();
            }
        }

        public async Task InitializeAndSignInAsync()
        {
            try
            {
                if (UnityServices.State != ServicesInitializationState.Initialized)
                {
                    await UnityServices.InitializeAsync();
                }

                if (!AuthenticationService.Instance.IsSignedIn)
                {
                    await AuthenticationService.Instance.SignInAnonymouslyAsync();
                }

                PlayerId = AuthenticationService.Instance.PlayerId;
                IsReady = UnityServices.State == ServicesInitializationState.Initialized &&
                          AuthenticationService.Instance.IsSignedIn &&
                          !string.IsNullOrEmpty(PlayerId);

                Debug.Log($"[LiveServicesBootstrap] Signed in anonymously. PlayerId={PlayerId}");

                await RefreshRemoteConfigAsync();
                if (LiveOpsEnabled)
                {
                    await LoadProgressAsync();
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[LiveServicesBootstrap] UGS init fallback to local state: {ex.Message}");
                LoadLocalFallback();
            }
        }

        public async Task RefreshRemoteConfigAsync()
        {
            if (!IsReady) return;
            try
            {
                var config = await RemoteConfigService.Instance.FetchConfigsAsync(new UserAttributes(), new AppAttributes());
                if (config != null && config.HasKey(KillSwitchFlag))
                {
                    LiveOpsEnabled = config.GetBool(KillSwitchFlag, true);
                }
                else
                {
                    LiveOpsEnabled = true;
                }
                Debug.Log($"[LiveServicesBootstrap] RemoteConfig fetched: {KillSwitchFlag}={LiveOpsEnabled}");
            }
            catch (Exception ex)
            {
                LiveOpsEnabled = true;
                Debug.Log($"[LiveServicesBootstrap] RemoteConfig default on ({ex.Message})");
            }
        }

        public async Task SaveProgressAsync()
        {
            SaveLocalFallback();
            if (!IsReady || !LiveOpsEnabled) return;

            try
            {
                string json = JsonUtility.ToJson(Progress);
                var data = new Dictionary<string, object>
                {
                    { ProgressKey, json }
                };
                await CloudSaveService.Instance.Data.Player.SaveAsync(data);
                Debug.Log($"[LiveServicesBootstrap] CloudSave saved '{ProgressKey}': {json}");
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[LiveServicesBootstrap] CloudSave save warning: {ex.Message}");
            }
        }

        public async Task<SavedProgressData> LoadProgressAsync()
        {
            LoadLocalFallback();
            if (!IsReady || !LiveOpsEnabled) return Progress;

            try
            {
                var keys = new HashSet<string> { ProgressKey };
                var loaded = await CloudSaveService.Instance.Data.Player.LoadAsync(keys);
                if (loaded != null && loaded.TryGetValue(ProgressKey, out var item))
                {
                    string json = item.Value.GetAsString();
                    if (!string.IsNullOrEmpty(json))
                    {
                        Progress = JsonUtility.FromJson<SavedProgressData>(json) ?? new SavedProgressData();
                        Debug.Log($"[LiveServicesBootstrap] CloudSave loaded '{ProgressKey}': {json}");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[LiveServicesBootstrap] CloudSave load warning: {ex.Message}");
            }
            return Progress;
        }

        public async Task RecordMatchOutcomeAsync(int winningFaction, string mvpName, int mvpScore)
        {
            Progress.totalMatches++;
            if (winningFaction == 1) Progress.topFactionWins++;
            else Progress.bottomFactionWins++;

            if (mvpScore > Progress.bestMvpScore)
            {
                Progress.bestMvpScore = mvpScore;
                Progress.lastMvpName = mvpName;
            }

            await SaveProgressAsync();

            if (!IsReady || !LiveOpsEnabled) return;
            try
            {
                var entry = await LeaderboardsService.Instance.AddPlayerScoreAsync(LeaderboardId, Mathf.Max(1, mvpScore));
                if (entry != null)
                {
                    LastLeaderboardRank = $"Rank #{entry.Rank + 1} ({entry.Score:F0} pts)";
                    Debug.Log($"[LiveServicesBootstrap] Leaderboard submitted: {LastLeaderboardRank}");
                }
            }
            catch (Exception ex)
            {
                LastLeaderboardRank = "Rank: unavailable (deploy .lb in Services > Deployment)";
                Debug.Log($"[LiveServicesBootstrap] Leaderboard graceful fallback: {ex.Message}");
            }
        }

        private void SaveLocalFallback()
        {
            PlayerPrefs.SetString(ProgressKey, JsonUtility.ToJson(Progress));
            PlayerPrefs.Save();
        }

        private void LoadLocalFallback()
        {
            if (PlayerPrefs.HasKey(ProgressKey))
            {
                string json = PlayerPrefs.GetString(ProgressKey);
                Progress = JsonUtility.FromJson<SavedProgressData>(json) ?? new SavedProgressData();
            }
        }
    }
}
