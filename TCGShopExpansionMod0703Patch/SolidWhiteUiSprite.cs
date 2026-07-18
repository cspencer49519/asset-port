using UnityEngine;

namespace TCGShopExpansionMod0703Patch;

/// <summary>
/// 1x1 white sprite for solid UI Image fills (slab chrome). Null sprites render as error magenta/green.
/// </summary>
internal static class SolidWhiteUiSprite
{
    private static Sprite? _sprite;
    private static Texture2D? _texture;

    public static Sprite Get()
    {
        if (_sprite != null && _texture != null)
        {
            return _sprite;
        }

        _texture = new Texture2D(2, 2, TextureFormat.RGBA32, mipChain: false)
        {
            name = "SolidWhiteUi0703",
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp,
        };
        _texture.SetPixels(new[] { Color.white, Color.white, Color.white, Color.white });
        _texture.Apply(updateMipmaps: false, makeNoLongerReadable: true);

        _sprite = Sprite.Create(
            _texture,
            new Rect(0f, 0f, 2f, 2f),
            new Vector2(0.5f, 0.5f),
            100f);
        _sprite.name = "SolidWhiteUi0703";
        return _sprite;
    }
}
