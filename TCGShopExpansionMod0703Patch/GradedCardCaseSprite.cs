using System.IO;
using UnityEngine;

namespace TCGShopExpansionMod0703Patch;

/// <summary>
/// Restores the graded slab frame sprite. The asset-port GradedCardCase texture is a black stub,
/// so we prefer CardExtras/TR disk overrides, then a procedural PSA-style case with a clear window.
/// </summary>
internal static class GradedCardCaseSprite
{
    private const string SpriteName = "GradedCardCase";
    private const int TextureSize = 512;
    private static Sprite? _sprite;
    private static Texture2D? _texture;
    private static bool _loggedSource;

    /// <summary>
    /// The case RectTransform is square (900x900), but a real graded slab is PORTRAIT. Draw the
    /// slab into a centered portrait footprint of the square rect and leave the rest transparent
    /// so the binder pocket shows around the slab instead of a big square box.
    /// </summary>
    public static readonly Vector2 SlabAnchorMin = new(0.17f, 0.05f);
    public static readonly Vector2 SlabAnchorMax = new(0.83f, 0.95f);

    /// <summary>Normalized card window under the header plate — tuned to the 2.5:3.5 card aspect.</summary>
    public static readonly Vector2 FaceAnchorMin = new(0.26f, 0.09f);
    public static readonly Vector2 FaceAnchorMax = new(0.74f, 0.75f);

    /// <summary>Normalized header label band (for grade TMP).</summary>
    public static readonly Vector2 HeaderAnchorMin = new(0.21f, 0.78f);
    public static readonly Vector2 HeaderAnchorMax = new(0.79f, 0.92f);

    public static Sprite Get()
    {
        if (_sprite != null)
        {
            return _sprite;
        }

        // Asset-port CardExtras ships a black GradedCardCase stub — skip cache, prefer TR/procedural.
        Sprite? fromTr = TryLoadFromTextureReplacer();
        if (fromTr != null && !LooksLikeBrokenStub(fromTr))
        {
            _sprite = fromTr;
            LogSource("TextureReplacer/objects_textures");
            return _sprite;
        }

        Sprite? fromPlugin = TryLoadFromPluginResources();
        if (fromPlugin != null && !LooksLikeBrokenStub(fromPlugin))
        {
            _sprite = fromPlugin;
            LogSource("plugin Resources");
            return _sprite;
        }

        // Do NOT use FindObjectsOfTypeAll / CardExtras for GradedCardCase — asset-port left a
        // non-readable black stub in memory that LooksLikeBrokenStub cannot sample.

        _sprite = CreateProceduralSlabSprite();
        LogSource($"procedural {TextureSize}x{TextureSize}");
        return _sprite;
    }

    private static void LogSource(string source)
    {
        if (_loggedSource)
        {
            return;
        }

        _loggedSource = true;
        Plugin.Log.LogInfo($"GradedCardCase sprite restored from {source}.");
    }

    private static bool LooksLikeBrokenStub(Sprite sprite)
    {
        Texture2D? tex = sprite.texture;
        if (tex == null)
        {
            return true;
        }

        // Port stub is tiny / solid black (~2KB png / low resolution).
        if (tex.width <= 64 || tex.height <= 64)
        {
            return true;
        }

        try
        {
            // Readable check — if we can't sample, assume usable.
            Color32[] pixels = tex.GetPixels32();
            if (pixels == null || pixels.Length == 0)
            {
                return false;
            }

            long luma = 0;
            int alphaHits = 0;
            int step = Mathf.Max(1, pixels.Length / 256);
            int samples = 0;
            for (int i = 0; i < pixels.Length; i += step)
            {
                Color32 p = pixels[i];
                luma += p.r + p.g + p.b;
                if (p.a > 8)
                {
                    alphaHits++;
                }

                samples++;
            }

            if (samples <= 0)
            {
                return true;
            }

            float avg = luma / (float)(samples * 3);
            // Solid black / empty.
            return avg < 4f && alphaHits > samples / 2;
        }
        catch
        {
            return false;
        }
    }

    private static Sprite? TryLoadFromTextureReplacer()
    {
        string path = Path.Combine(
            BepInEx.Paths.PluginPath,
            "TextureReplacer",
            "objects_textures",
            SpriteName + ".png");
        return LoadSpriteFile(path);
    }

    private static Sprite? TryLoadFromPluginResources()
    {
        string path = Path.Combine(
            Path.GetDirectoryName(typeof(GradedCardCaseSprite).Assembly.Location) ?? string.Empty,
            "Resources",
            SpriteName + ".png");
        return LoadSpriteFile(path);
    }

    private static Sprite? LoadSpriteFile(string path)
    {
        if (!File.Exists(path))
        {
            return null;
        }

        try
        {
            byte[] bytes = File.ReadAllBytes(path);
            Texture2D texture = new(2, 2, TextureFormat.RGBA32, mipChain: false)
            {
                name = SpriteName,
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
            };
            if (!texture.LoadImage(bytes))
            {
                Object.Destroy(texture);
                return null;
            }

            Sprite sprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, texture.width, texture.height),
                new Vector2(0.5f, 0.5f),
                100f);
            sprite.name = SpriteName;
            return sprite;
        }
        catch
        {
            return null;
        }
    }

    private static Sprite? TryFindLoadedSprite()
    {
        Sprite[] sprites = Resources.FindObjectsOfTypeAll<Sprite>();
        for (int i = 0; i < sprites.Length; i++)
        {
            Sprite? sprite = sprites[i];
            if (sprite == null || sprite.name == null)
            {
                continue;
            }

            if (string.Equals(sprite.name, SpriteName, System.StringComparison.OrdinalIgnoreCase)
                || string.Equals(sprite.name, SpriteName + "(Clone)", System.StringComparison.OrdinalIgnoreCase))
            {
                return sprite;
            }
        }

        return null;
    }

    private static Sprite CreateProceduralSlabSprite()
    {
        _texture = new Texture2D(TextureSize, TextureSize, TextureFormat.RGBA32, mipChain: false)
        {
            name = "GradedCardCase0703",
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp,
        };

        Color32 clear = new(0, 0, 0, 0);
        Color32 windowMatte = new(18, 20, 24, 255);
        Color32 plastic = new(72, 78, 88, 255);
        Color32 plasticHi = new(175, 182, 195, 240);
        Color32 plasticLo = new(36, 40, 46, 255);
        Color32 header = new(14, 15, 18, 255);
        Color32 silver = new(175, 182, 192, 230);

        Color32[] pixels = new Color32[TextureSize * TextureSize];
        for (int i = 0; i < pixels.Length; i++)
        {
            pixels[i] = clear;
        }

        // Portrait slab footprint (matches SlabAnchor fractions of the square rect).
        int x0 = Mathf.RoundToInt(TextureSize * SlabAnchorMin.x);
        int y0 = Mathf.RoundToInt(TextureSize * SlabAnchorMin.y);
        int x1 = Mathf.RoundToInt(TextureSize * SlabAnchorMax.x) - 1;
        int y1 = Mathf.RoundToInt(TextureSize * SlabAnchorMax.y) - 1;

        FillRoundedRect(pixels, TextureSize, x0, y0, x1, y1, 18, plastic);
        StrokeRoundedRect(pixels, TextureSize, x0, y0, x1, y1, 18, plasticHi, 3);
        StrokeRoundedRect(pixels, TextureSize, x0 + 5, y0 + 5, x1 - 5, y1 - 5, 14, plasticLo, 2);

        // Header band (matches HeaderAnchor fractions).
        int headerL = Mathf.RoundToInt(TextureSize * HeaderAnchorMin.x);
        int headerR = Mathf.RoundToInt(TextureSize * HeaderAnchorMax.x) - 1;
        int headerB = Mathf.RoundToInt(TextureSize * HeaderAnchorMin.y);
        int headerT = Mathf.RoundToInt(TextureSize * HeaderAnchorMax.y) - 1;
        FillRoundedRect(pixels, TextureSize, headerL, headerB, headerR, headerT, 6, header);
        FillRect(pixels, TextureSize, headerL + 4, headerB - 3, headerR - 4, headerB - 1, silver);

        // Card window matte (matches FaceAnchor fractions). GradedFace art draws on top.
        int winL = Mathf.RoundToInt(TextureSize * FaceAnchorMin.x);
        int winR = Mathf.RoundToInt(TextureSize * FaceAnchorMax.x) - 1;
        int winB = Mathf.RoundToInt(TextureSize * FaceAnchorMin.y);
        int winT = Mathf.RoundToInt(TextureSize * FaceAnchorMax.y) - 1;
        FillRect(pixels, TextureSize, winL, winB, winR, winT, windowMatte);
        StrokeRect(pixels, TextureSize, winL, winB, winR, winT, plasticLo, 2);

        _texture.SetPixels32(pixels);
        _texture.Apply(updateMipmaps: false, makeNoLongerReadable: false);

        Sprite sprite = Sprite.Create(
            _texture,
            new Rect(0f, 0f, TextureSize, TextureSize),
            new Vector2(0.5f, 0.5f),
            100f);
        sprite.name = "GradedCardCase0703";
        return sprite;
    }

    private static void FillRect(Color32[] pixels, int size, int x0, int y0, int x1, int y1, Color32 color)
    {
        x0 = Mathf.Clamp(x0, 0, size - 1);
        x1 = Mathf.Clamp(x1, 0, size - 1);
        y0 = Mathf.Clamp(y0, 0, size - 1);
        y1 = Mathf.Clamp(y1, 0, size - 1);
        for (int y = y0; y <= y1; y++)
        {
            int row = y * size;
            for (int x = x0; x <= x1; x++)
            {
                pixels[row + x] = color;
            }
        }
    }

    private static void StrokeRect(Color32[] pixels, int size, int x0, int y0, int x1, int y1, Color32 color, int width)
    {
        for (int i = 0; i < width; i++)
        {
            FillRect(pixels, size, x0 + i, y0 + i, x1 - i, y0 + i, color);
            FillRect(pixels, size, x0 + i, y1 - i, x1 - i, y1 - i, color);
            FillRect(pixels, size, x0 + i, y0 + i, x0 + i, y1 - i, color);
            FillRect(pixels, size, x1 - i, y0 + i, x1 - i, y1 - i, color);
        }
    }

    private static void FillRoundedRect(
        Color32[] pixels,
        int size,
        int x0,
        int y0,
        int x1,
        int y1,
        int radius,
        Color32 color)
    {
        radius = Mathf.Max(0, Mathf.Min(radius, (x1 - x0) / 2, (y1 - y0) / 2));
        for (int y = y0; y <= y1; y++)
        {
            for (int x = x0; x <= x1; x++)
            {
                if (InsideRoundedRect(x, y, x0, y0, x1, y1, radius))
                {
                    pixels[y * size + x] = color;
                }
            }
        }
    }

    private static void StrokeRoundedRect(
        Color32[] pixels,
        int size,
        int x0,
        int y0,
        int x1,
        int y1,
        int radius,
        Color32 color,
        int width)
    {
        for (int i = 0; i < width; i++)
        {
            FillRoundedRectOutline(pixels, size, x0 + i, y0 + i, x1 - i, y1 - i, Mathf.Max(0, radius - i), color);
        }
    }

    private static void FillRoundedRectOutline(
        Color32[] pixels,
        int size,
        int x0,
        int y0,
        int x1,
        int y1,
        int radius,
        Color32 color)
    {
        for (int y = y0; y <= y1; y++)
        {
            for (int x = x0; x <= x1; x++)
            {
                bool inside = InsideRoundedRect(x, y, x0, y0, x1, y1, radius);
                bool inner = InsideRoundedRect(x, y, x0 + 1, y0 + 1, x1 - 1, y1 - 1, Mathf.Max(0, radius - 1));
                if (inside && !inner)
                {
                    pixels[y * size + x] = color;
                }
            }
        }
    }

    private static bool InsideRoundedRect(int x, int y, int x0, int y0, int x1, int y1, int radius)
    {
        if (x < x0 || x > x1 || y < y0 || y > y1)
        {
            return false;
        }

        if (radius <= 0)
        {
            return true;
        }

        // Corner centers.
        if (x < x0 + radius && y < y0 + radius)
        {
            return DistSq(x, y, x0 + radius, y0 + radius) <= radius * radius;
        }

        if (x > x1 - radius && y < y0 + radius)
        {
            return DistSq(x, y, x1 - radius, y0 + radius) <= radius * radius;
        }

        if (x < x0 + radius && y > y1 - radius)
        {
            return DistSq(x, y, x0 + radius, y1 - radius) <= radius * radius;
        }

        if (x > x1 - radius && y > y1 - radius)
        {
            return DistSq(x, y, x1 - radius, y1 - radius) <= radius * radius;
        }

        return true;
    }

    private static int DistSq(int x0, int y0, int x1, int y1)
    {
        int dx = x0 - x1;
        int dy = y0 - y1;
        return (dx * dx) + (dy * dy);
    }
}
