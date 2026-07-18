using UnityEngine;

namespace TCGShopExpansionMod0703Patch;

/// <summary>
/// Clear stand-in for GradeCardScratch* so TextureReplacer cannot paint CardBack into the PSA header.
/// </summary>
internal static class GradedScratchClearSprite
{
    private static Sprite? _sprite;
    private static Texture2D? _texture;

    public static Texture2D ClearTexture
    {
        get
        {
            EnsureCreated();
            return _texture!;
        }
    }

    public static Sprite Get()
    {
        EnsureCreated();
        return _sprite!;
    }

    private static void EnsureCreated()
    {
        if (_sprite != null && _texture != null)
        {
            return;
        }

        _texture = new Texture2D(2, 2, TextureFormat.RGBA32, mipChain: false)
        {
            name = "GradedScratchClear0703",
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp,
        };
        _texture.SetPixels(new[] { Color.clear, Color.clear, Color.clear, Color.clear });
        _texture.Apply(updateMipmaps: false, makeNoLongerReadable: true);

        _sprite = Sprite.Create(
            _texture,
            new Rect(0f, 0f, 2f, 2f),
            new Vector2(0.5f, 0.5f),
            100f);
        _sprite.name = "GradedScratchClear0703";
    }
}
