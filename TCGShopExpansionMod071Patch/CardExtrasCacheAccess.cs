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

    public static Sprite? TryGetCachedSprite(string spriteName)
    {
        if (CardExtrasImagesCacheField?.GetValue(null) is not List<Sprite> cache)
        {
            return null;
        }

        return NewSwappingHandler.TryGetSpriteFromCache(cache, spriteName);
    }
}
