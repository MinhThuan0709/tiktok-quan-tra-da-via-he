using System.Collections.Generic;
using UnityEngine;

namespace TikTokFactionWar
{
    /// <summary>
    /// Art Bible & Single Source of Truth for all colors and procedural sprites.
    /// Style: Flat-shaded neon tactical arena: saturated crimson vs cyan battalions clashing on a dark obsidian grid.
    /// Palette: ArenaFloor #121629 · ArenaGrid #1E2642 · FactionTopPrimary #FF3B5C · FactionTopSecondary #FF9EAB
    ///          FactionBottomPrimary #19D3F0 · FactionBottomSecondary #A8F2FF · ShadowDark #090C16 · AccentGold #F7C844
    /// Mood: Tactical night court illuminated by glowing territory frontlines, base cores, and unit plasma trails.
    /// </summary>
    public static class Palette
    {
        public static readonly Color ArenaFloor = Hex("#121629");
        public static readonly Color ArenaGrid = Hex("#1E2642");
        public static readonly Color ArenaLaneLine = Hex("#2B365E");

        public static readonly Color FactionTopPrimary = Hex("#FF3B5C");
        public static readonly Color FactionTopSecondary = Hex("#FF9EAB");
        public static readonly Color FactionTopDark = Hex("#6E1426");

        public static readonly Color FactionBottomPrimary = Hex("#19D3F0");
        public static readonly Color FactionBottomSecondary = Hex("#A8F2FF");
        public static readonly Color FactionBottomDark = Hex("#0B5566");

        public static readonly Color ShadowDark = Hex("#090C16");
        public static readonly Color PanelBackground = Hex("#151B30", 0.92f);
        public static readonly Color PanelBorder = Hex("#33416E", 0.95f);
        public static readonly Color ScrimOverlay = Hex("#090C16", 0.78f);

        public static readonly Color AccentGold = Hex("#F7C844");
        public static readonly Color AccentOrange = Hex("#FF7A2B");
        public static readonly Color AccentViolet = Hex("#9D5CFF");
        public static readonly Color AccentGreen = Hex("#2EE68A");

        public static readonly Color NeutralLight = Hex("#EEF4FF");
        public static readonly Color NeutralMuted = Hex("#9BA8C7");
        public static readonly Color PureTint = Hex("#FFFFFF");
        public static readonly Color ClearTint = Hex("#000000", 0f);

        private static readonly Dictionary<string, Sprite> SpriteCache = new Dictionary<string, Sprite>();
        private static Material _defaultSpriteMaterial;

        public static Color Hex(string hex, float alpha = 1f)
        {
            if (ColorUtility.TryParseHtmlString(hex, out Color c))
            {
                c.a = alpha;
                return c;
            }
            return new Color(0.95f, 0.78f, 0.27f, alpha);
        }

        public static Color WithAlpha(Color c, float alpha)
        {
            return new Color(c.r, c.g, c.b, alpha);
        }

        public static Color GetFactionPrimary(int factionId)
        {
            return factionId == 1 ? FactionTopPrimary : FactionBottomPrimary;
        }

        public static Color GetFactionSecondary(int factionId)
        {
            return factionId == 1 ? FactionTopSecondary : FactionBottomSecondary;
        }

        public static Color GetFactionDark(int factionId)
        {
            return factionId == 1 ? FactionTopDark : FactionBottomDark;
        }

        public static Material GetSpriteMaterial()
        {
            if (_defaultSpriteMaterial != null) return _defaultSpriteMaterial;
            Shader shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
            if (shader == null) shader = Shader.Find("Sprites/Default");
            _defaultSpriteMaterial = new Material(shader) { name = "Palette_SpriteUnlit" };
            return _defaultSpriteMaterial;
        }

        public static Sprite GetSquareSprite()
        {
            const string key = "square_64";
            if (SpriteCache.TryGetValue(key, out Sprite s) && s != null) return s;

            int size = 64;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };
            Color[] pixels = new Color[size * size];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = PureTint;
            tex.SetPixels(pixels);
            tex.Apply();

            s = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 64f);
            s.name = key;
            SpriteCache[key] = s;
            return s;
        }

        public static Sprite GetRoundedRectSprite(int width = 128, int height = 128, int radius = 20, int outlinePx = 4)
        {
            string key = $"rrect9_{width}_{height}_{radius}_{outlinePx}";
            if (SpriteCache.TryGetValue(key, out Sprite s) && s != null) return s;

            Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float dist = SignedDistanceRoundedBox(
                        new Vector2(x - width * 0.5f + 0.5f, y - height * 0.5f + 0.5f),
                        new Vector2(width * 0.5f - 2f, height * 0.5f - 2f),
                        radius);

                    if (dist > 0.5f)
                    {
                        tex.SetPixel(x, y, ClearTint);
                    }
                    else if (dist > -outlinePx)
                    {
                        // Subtle dark outline baked into silhouette for 2D outline discipline
                        tex.SetPixel(x, y, new Color(0.22f, 0.28f, 0.44f, 1f));
                    }
                    else
                    {
                        tex.SetPixel(x, y, PureTint);
                    }
                }
            }
            tex.Apply();

            float b = Mathf.Min(width, height) * 0.28f;
            Vector4 border = new Vector4(b, b, b, b);
            s = Sprite.Create(tex, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, border);
            s.name = key;
            SpriteCache[key] = s;
            return s;
        }

        public static Sprite GetCircleSprite(int size = 64, int outlinePx = 4)
        {
            string key = $"circle_{size}_{outlinePx}";
            if (SpriteCache.TryGetValue(key, out Sprite s) && s != null) return s;

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
                        tex.SetPixel(x, y, ClearTint);
                    else if (outlinePx > 0 && d > maxR - outlinePx)
                        tex.SetPixel(x, y, new Color(0.15f, 0.18f, 0.28f, 1f));
                    else
                        tex.SetPixel(x, y, PureTint);
                }
            }
            tex.Apply();

            s = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 64f);
            s.name = key;
            SpriteCache[key] = s;
            return s;
        }

        public static Sprite GetRingSprite(int size = 64, int thickness = 6)
        {
            string key = $"ring_{size}_{thickness}";
            if (SpriteCache.TryGetValue(key, out Sprite s) && s != null) return s;

            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };

            Vector2 center = new Vector2(size * 0.5f - 0.5f, size * 0.5f - 0.5f);
            float outerR = size * 0.5f - 1.5f;
            float innerR = outerR - thickness;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float d = Vector2.Distance(new Vector2(x, y), center);
                    if (d <= outerR && d >= innerR)
                        tex.SetPixel(x, y, PureTint);
                    else
                        tex.SetPixel(x, y, ClearTint);
                }
            }
            tex.Apply();

            s = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 64f);
            s.name = key;
            SpriteCache[key] = s;
            return s;
        }

        public static Sprite GetEmblemSprite(string emblemType)
        {
            string key = $"emblem_{emblemType}";
            if (SpriteCache.TryGetValue(key, out Sprite s) && s != null) return s;

            int size = 64;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };

            Vector2 c = new Vector2(size * 0.5f, size * 0.5f);
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    Vector2 p = new Vector2(x, y) - c;
                    bool inside = false;

                    if (emblemType == "star")
                    {
                        float angle = Mathf.Atan2(p.y, p.x);
                        float r = p.magnitude;
                        float starR = 16f + 9f * Mathf.Cos(angle * 5f - Mathf.PI * 0.5f);
                        inside = r <= starR;
                    }
                    else if (emblemType == "shield")
                    {
                        float nx = Mathf.Abs(p.x) / 22f;
                        float ny = (p.y + 22f) / 44f;
                        inside = ny >= 0f && ny <= 1f && nx <= Mathf.Lerp(0.15f, 1f, Mathf.Sqrt(ny));
                    }
                    else if (emblemType == "cat")
                    {
                        bool head = (p - new Vector2(0, -4f)).magnitude < 18f;
                        bool leftEar = p.x < -4f && p.x > -18f && p.y > 4f && p.y < 24f && (p.y - 4f) < (p.x + 22f) * 1.3f;
                        bool rightEar = p.x > 4f && p.x < 18f && p.y > 4f && p.y < 24f && (p.y - 4f) < (22f - p.x) * 1.3f;
                        inside = head || leftEar || rightEar;
                    }
                    else if (emblemType == "crown")
                    {
                        bool baseBar = Mathf.Abs(p.x) < 22f && p.y >= -16f && p.y <= -4f;
                        float wave = -4f + 16f * Mathf.Abs(Mathf.Cos(p.x * 0.14f));
                        bool spikes = Mathf.Abs(p.x) < 22f && p.y > -4f && p.y <= wave;
                        inside = baseBar || spikes;
                    }
                    else
                    {
                        // Diamond / Sword emblem
                        inside = (Mathf.Abs(p.x) * 1.3f + Mathf.Abs(p.y)) < 25f;
                    }

                    tex.SetPixel(x, y, inside ? PureTint : ClearTint);
                }
            }
            tex.Apply();

            s = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 64f);
            s.name = key;
            SpriteCache[key] = s;
            return s;
        }

        public static Sprite GetGridArenaSprite()
        {
            const string key = "arena_grid_256";
            if (SpriteCache.TryGetValue(key, out Sprite s) && s != null) return s;

            int w = 256;
            int h = 512;
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    bool isBorder = x < 4 || x >= w - 4 || y < 4 || y >= h - 4;
                    bool isGrid = (x % 32 == 0) || (y % 32 == 0);
                    bool isLane = (x == w / 3) || (x == (w * 2) / 3);

                    if (isBorder)
                        tex.SetPixel(x, y, ArenaLaneLine);
                    else if (isLane && (y % 16 < 10))
                        tex.SetPixel(x, y, ArenaLaneLine);
                    else if (isGrid)
                        tex.SetPixel(x, y, ArenaGrid);
                    else
                        tex.SetPixel(x, y, ArenaFloor);
                }
            }
            tex.Apply();

            s = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 32f);
            s.name = key;
            SpriteCache[key] = s;
            return s;
        }

        private static Material _fxAdditiveMaterial;

        /// <summary>VFX sprite material with URP 2D Unlit / Sprites shader for HDR Bloom support.</summary>
        public static Material GetFxMaterial()
        {
            if (_fxAdditiveMaterial != null) return _fxAdditiveMaterial;

            Shader shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
            if (shader == null) shader = Shader.Find("Sprites/Default");
            _fxAdditiveMaterial = new Material(shader) { name = "Palette_FxAdditive" };
            return _fxAdditiveMaterial;
        }

        public static Sprite GetGeneratedSprite(string spriteName, float pixelsPerUnit = 100f)
        {
            string key = $"gen_{spriteName}_{pixelsPerUnit}";
            if (SpriteCache.TryGetValue(key, out Sprite cached) && cached != null)
                return cached;

            string resPath = $"Sprites/{spriteName}";
            Sprite loadedSprite = Resources.Load<Sprite>(resPath);
            if (loadedSprite != null && Mathf.Abs(loadedSprite.pixelsPerUnit - pixelsPerUnit) < 1f)
            {
                SpriteCache[key] = loadedSprite;
                return loadedSprite;
            }

            Texture2D tex = Resources.Load<Texture2D>(resPath);
            if (tex != null)
            {
                tex.filterMode = FilterMode.Bilinear;
                tex.wrapMode = TextureWrapMode.Clamp;
                Sprite created = Sprite.Create(
                    tex,
                    new Rect(0, 0, tex.width, tex.height),
                    new Vector2(0.5f, 0.5f),
                    pixelsPerUnit,
                    0,
                    SpriteMeshType.FullRect);
                created.name = spriteName;
                SpriteCache[key] = created;
                return created;
            }

            return loadedSprite;
        }

        public static Sprite GetArenaBattlefieldSprite()
        {
            Sprite s = GetGeneratedSprite("arena_battlefield_bg", 100f);
            return s != null ? s : GetGridArenaSprite();
        }

        public static Sprite GetFortressBaseSprite()
        {
            Sprite s = GetGeneratedSprite("fortress_base", 100f);
            return s != null ? s : GetRoundedRectSprite(128, 128, 22, 4);
        }

        public static Sprite GetUnitSprite(UnitTier tier)
        {
            string name = tier switch
            {
                UnitTier.Infantry => "unit_infantry",
                UnitTier.Squad => "unit_infantry",
                UnitTier.Tank => "unit_tank",
                UnitTier.Mecha => "unit_mecha",
                UnitTier.Dragon => "unit_dragon",
                _ => "unit_infantry"
            };
            Sprite s = GetGeneratedSprite(name, 100f);
            return s != null ? s : GetCircleSprite(64, 4);
        }

        private static float SignedDistanceRoundedBox(Vector2 p, Vector2 b, float r)
        {
            Vector2 q = new Vector2(Mathf.Abs(p.x) - b.x + r, Mathf.Abs(p.y) - b.y + r);
            return Vector2.Max(q, Vector2.zero).magnitude + Mathf.Min(Mathf.Max(q.x, q.y), 0f) - r;
        }
    }
}
