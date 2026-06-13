using System.Collections.Generic;
using System.Reflection;
using TCGShopExpansionMod.Handlers;
using UnityEngine;

namespace TCGShopExpansionMod071Patch;

internal static class CardExtrasCacheAccess
{
    private static readonly FieldInfo? CardExtrasImagesCacheField = typeof(NewSwappingHandler).Assembly
        .GetType("TCGShopExpansionMod.Handlers.CacheHandler")
        ?.GetField("cardExtrasImagesCache", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

    private static readonly FieldInfo? OriginalCardBackTextureField = typeof(NewSwappingHandler).Assembly
        .GetType("TCGShopExpansionMod.Handlers.CacheHandler")
        ?.GetField("originalCardBackTexture", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

    public static Sprite? TryGetCachedSprite(string spriteName)
    {
        if (CardExtrasImagesCacheField?.GetValue(null) is List<Sprite> cache)
        {
            Sprite? fromCache = NewSwappingHandler.TryGetSpriteFromCache(cache, spriteName);
            if (fromCache != null)
            {
                return fromCache;
            }
        }

        if (spriteName == "T_CardBackMesh"
            && OriginalCardBackTextureField?.GetValue(null) is Sprite originalBack)
        {
            return originalBack;
        }

        return null;
    }
}
