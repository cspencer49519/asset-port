using System.Collections.Generic;
using System.IO;
using System.Reflection;
using TCGShopExpansionMod.Handlers;
using UnityEngine;

namespace TCGShopExpansionMod071Patch;

internal static class CardExtrasCacheAccess
{
    private const string PackStackBackSpriteName = "T_CardBackMesh";
    private const string UiCardBackSpriteName = "CardBack";
    private const string CardExtrasFolderName = "CardExtrasImages";

    private static readonly FieldInfo? CardExtrasImagesCacheField = typeof(NewSwappingHandler).Assembly
        .GetType("TCGShopExpansionMod.Handlers.CacheHandler")
        ?.GetField("cardExtrasImagesCache", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

    private static readonly FieldInfo? OriginalCardExtrasImagesCacheField = typeof(NewSwappingHandler).Assembly
        .GetType("TCGShopExpansionMod.Handlers.CacheHandler")
        ?.GetField("originalCardExtrasImagesCache", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

    private static readonly FieldInfo? OriginalCardBackTextureField = typeof(NewSwappingHandler).Assembly
        .GetType("TCGShopExpansionMod.Handlers.CacheHandler")
        ?.GetField("originalCardBackTexture", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

    private static Sprite? _uiCardBackSprite;
    private static Sprite? _stackBackMeshSprite;
    private static bool _loggedUiCardBackSource;
    private static bool _loggedStackBackSource;

    public static Sprite? TryGetCachedSprite(string spriteName)
    {
        Sprite? fromCustom = TryGetSpriteFromCacheList(CardExtrasImagesCacheField, spriteName);
        if (fromCustom != null)
        {
            return fromCustom;
        }

        Sprite? fromOriginal = TryGetSpriteFromCacheList(OriginalCardExtrasImagesCacheField, spriteName);
        if (fromOriginal != null)
        {
            return fromOriginal;
        }

        if (spriteName == PackStackBackSpriteName
            && OriginalCardBackTextureField?.GetValue(null) is Sprite originalBack)
        {
            return originalBack;
        }

        return TryLoadSpriteFromDisk(spriteName);
    }

    public static Sprite? TryGetUiCardBackSprite()
    {
        if (_uiCardBackSprite != null)
        {
            return _uiCardBackSprite;
        }

        _uiCardBackSprite = TryGetCachedSprite(UiCardBackSpriteName);
        if (_uiCardBackSprite != null && !_loggedUiCardBackSource)
        {
            _loggedUiCardBackSource = true;
            Plugin.Log.LogInfo($"Pack UI card back resolved: {UiCardBackSpriteName} ({_uiCardBackSprite.name})");
        }

        return _uiCardBackSprite;
    }

    public static Sprite? TryGetStackBackMeshSprite()
    {
        if (_stackBackMeshSprite != null)
        {
            return _stackBackMeshSprite;
        }

        _stackBackMeshSprite = TryGetCachedSprite(PackStackBackSpriteName);
        if (_stackBackMeshSprite != null && !_loggedStackBackSource)
        {
            _loggedStackBackSource = true;
            Plugin.Log.LogInfo($"Pack stack mesh back resolved: {PackStackBackSpriteName} ({_stackBackMeshSprite.name})");
        }

        return _stackBackMeshSprite;
    }

    private static Sprite? TryGetSpriteFromCacheList(FieldInfo? cacheField, string spriteName)
    {
        if (cacheField?.GetValue(null) is not List<Sprite> cache)
        {
            return null;
        }

        return NewSwappingHandler.TryGetSpriteFromCache(cache, spriteName);
    }

    private static Sprite? TryLoadSpriteFromDisk(string spriteName)
    {
        string? extrasDir = GetCardExtrasImagesDirectory();
        if (extrasDir == null)
        {
            return null;
        }

        string pngPath = Path.Combine(extrasDir, spriteName + ".png");
        if (!File.Exists(pngPath))
        {
            return null;
        }

        try
        {
            byte[] bytes = File.ReadAllBytes(pngPath);
            Texture2D texture = new(2, 2, TextureFormat.RGBA32, mipChain: false);
            if (!texture.LoadImage(bytes))
            {
                Object.Destroy(texture);
                return null;
            }

            texture.name = spriteName;
            Sprite sprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, texture.width, texture.height),
                new Vector2(0.5f, 0.5f),
                100f);
            sprite.name = spriteName;
            return sprite;
        }
        catch
        {
            return null;
        }
    }

    private static string? GetCardExtrasImagesDirectory()
    {
        System.Type? playerPatches = AccessToolsTypeByName("TCGShopExpansionMod.Patches.PlayerPatches");
        if (playerPatches == null)
        {
            return null;
        }

        FieldInfo? cardExtrasField = playerPatches.GetField(
            "cardExtrasImages",
            BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        if (cardExtrasField?.GetValue(null) is string cardExtrasPath && !string.IsNullOrEmpty(cardExtrasPath))
        {
            return cardExtrasPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        }

        FieldInfo? customRootField = playerPatches.GetField(
            "customExpansionPackImages",
            BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        if (customRootField?.GetValue(null) is string customRoot && !string.IsNullOrEmpty(customRoot))
        {
            return Path.Combine(
                customRoot.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar),
                CardExtrasFolderName);
        }

        return null;
    }

    private static System.Type? AccessToolsTypeByName(string name)
    {
        foreach (System.Reflection.Assembly assembly in System.AppDomain.CurrentDomain.GetAssemblies())
        {
            System.Type? type = assembly.GetType(name, throwOnError: false);
            if (type != null)
            {
                return type;
            }
        }

        return null;
    }
}
