using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace TikTokFactionWar
{
    /// <summary>
    /// Downloads and caches circular TikTok viewer avatars by userId/URL.
    /// Provides instant procedural identicon avatars when offline or while downloading.
    /// </summary>
    public class AvatarCache : MonoBehaviour
    {
        public static AvatarCache Instance { get; private set; }

        private readonly Dictionary<string, Sprite> _avatarByUser = new Dictionary<string, Sprite>();
        private readonly HashSet<string> _downloadingUsers = new HashSet<string>();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                if (Application.isPlaying) Destroy(gameObject);
                return;
            }
            EnsureInitialized();
        }

        public void EnsureInitialized()
        {
            Instance = this;
        }

        public Sprite GetOrRequestAvatar(string userId, string avatarUrl, int factionId, Action<Sprite> onLoaded = null)
        {
            if (string.IsNullOrEmpty(userId)) userId = "viewer";

            if (_avatarByUser.TryGetValue(userId, out Sprite cached) && cached != null)
            {
                onLoaded?.Invoke(cached);
                return cached;
            }

            Sprite fallback = CreateProceduralAvatar(userId, factionId);
            _avatarByUser[userId] = fallback;

            if (!string.IsNullOrEmpty(avatarUrl) && avatarUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase) && !_downloadingUsers.Contains(userId))
            {
                _downloadingUsers.Add(userId);
                StartCoroutine(DownloadAvatarCoroutine(userId, avatarUrl, onLoaded));
            }
            else
            {
                onLoaded?.Invoke(fallback);
            }

            return fallback;
        }

        private IEnumerator DownloadAvatarCoroutine(string userId, string url, Action<Sprite> onLoaded)
        {
            using UnityWebRequest req = UnityWebRequestTexture.GetTexture(url);
            req.timeout = 8;
            yield return req.SendWebRequest();

            _downloadingUsers.Remove(userId);

            if (req.result == UnityWebRequest.Result.Success)
            {
                Texture2D rawTex = DownloadHandlerTexture.GetContent(req);
                if (rawTex != null)
                {
                    Sprite masked = CreateCircularMaskedSprite(rawTex, 64);
                    _avatarByUser[userId] = masked;
                    onLoaded?.Invoke(masked);
                }
            }
        }

        private static Sprite CreateCircularMaskedSprite(Texture2D source, int size)
        {
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };

            Vector2 center = new Vector2(size * 0.5f - 0.5f, size * 0.5f - 0.5f);
            float maxR = size * 0.5f - 1.5f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float d = Vector2.Distance(new Vector2(x, y), center);
                    if (d > maxR)
                    {
                        tex.SetPixel(x, y, Palette.ClearTint);
                    }
                    else if (d > maxR - 3f)
                    {
                        tex.SetPixel(x, y, Palette.AccentGold);
                    }
                    else
                    {
                        Color src = source.GetPixelBilinear(x / (float)size, y / (float)size);
                        tex.SetPixel(x, y, src);
                    }
                }
            }
            tex.Apply();

            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 64f);
        }

        private static Sprite CreateProceduralAvatar(string userId, int factionId)
        {
            int size = 64;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };

            int hash = Mathf.Abs(userId.GetHashCode());
            Color bg = factionId == 1 ? Palette.FactionTopDark : Palette.FactionBottomDark;
            Color accent = (hash % 3 == 0) ? Palette.AccentGold :
                           (hash % 3 == 1) ? Palette.GetFactionSecondary(factionId) : Palette.NeutralLight;

            Vector2 center = new Vector2(size * 0.5f - 0.5f, size * 0.5f - 0.5f);
            float maxR = size * 0.5f - 1.5f;

            // Simple symmetric 5x5 identicon inside circle
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float d = Vector2.Distance(new Vector2(x, y), center);
                    if (d > maxR)
                    {
                        tex.SetPixel(x, y, Palette.ClearTint);
                    }
                    else if (d > maxR - 3.5f)
                    {
                        tex.SetPixel(x, y, Palette.GetFactionPrimary(factionId));
                    }
                    else
                    {
                        int gx = Mathf.Clamp((x - 12) / 8, 0, 4);
                        int gy = Mathf.Clamp((y - 12) / 8, 0, 4);
                        int symX = gx <= 2 ? gx : 4 - gx;
                        int bit = (hash >> (symX * 5 + gy)) & 1;
                        bool inGrid = x >= 12 && x < 52 && y >= 12 && y < 52;
                        tex.SetPixel(x, y, (inGrid && bit == 1) ? accent : bg);
                    }
                }
            }
            tex.Apply();

            Sprite s = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 64f);
            s.name = $"Avatar_{userId}";
            return s;
        }
    }
}
