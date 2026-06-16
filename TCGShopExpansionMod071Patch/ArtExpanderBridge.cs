using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace TCGShopExpansionMod071Patch;

/// <summary>
/// Resolves card art through ArtExpander's in-memory cache. Never opens cardart.assets directly.
/// Plain reflection only — no Harmony, Chainloader, or LINQ.
/// </summary>
internal static class ArtExpanderBridge
{
    private const BindingFlags StaticAny = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
    private const BindingFlags InstanceAny = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

    private static bool _initialized;
    private static bool _available;
    private static readonly List<object> ArtCaches = new();
    private static MethodInfo? _resolveArtPath;
    private static MethodInfo? _loadSprite;
    private static bool _loggedAssetNameDump;

    public static void TryInitialize()
    {
        EnsureInitialized();
    }

    /// <summary>
    /// One-time diagnostic: enumerate every asset name in the ArtExpander bundle(s) and log the ones that
    /// look like a card back. cardart.assets is ~15GB so it can only be inspected via the already-loaded
    /// bundle in-process; this reflects into ArtCache._bundleLoader.GetAllAssetNames().
    /// </summary>
    public static void DumpBackAssetNames()
    {
        if (_loggedAssetNameDump || !EnsureInitialized())
        {
            return;
        }

        _loggedAssetNameDump = true;

        try
        {
            int caches = 0;
            foreach (object cache in ArtCaches)
            {
                string[]? names = GetAllAssetNames(cache);
                if (names == null)
                {
                    continue;
                }

                caches++;
                int matches = 0;
                for (int i = 0; i < names.Length; i++)
                {
                    string lower = names[i].ToLowerInvariant();
                    if (lower.Contains("back") || lower.Contains("pokemon") || lower.Contains("sleeve") || lower.Contains("reverse"))
                    {
                        Plugin.Log.LogWarning($"BundleBackAsset[{caches}]: {names[i]}");
                        matches++;
                    }
                }

                Plugin.Log.LogWarning($"ArtExpander bundle cache #{caches}: {names.Length} assets, {matches} back-like names.");
            }

            if (caches == 0)
            {
                Plugin.Log.LogWarning("DumpBackAssetNames: no enumerable bundle caches found.");
            }
        }
        catch (Exception ex)
        {
            Plugin.Log.LogWarning($"DumpBackAssetNames failed: {ex.Message}");
        }
    }

    /// <summary>Load any sprite from the ArtExpander bundle(s) by its exact asset path/name.</summary>
    public static Sprite? LoadSpriteByBundlePath(string assetPath)
    {
        if (string.IsNullOrEmpty(assetPath) || !EnsureInitialized() || _loadSprite == null)
        {
            return null;
        }

        for (int i = 0; i < ArtCaches.Count; i++)
        {
            if (_loadSprite.Invoke(ArtCaches[i], new object[] { assetPath }) is Sprite sprite && sprite != null)
            {
                return sprite;
            }
        }

        return null;
    }

    private static string[]? GetAllAssetNames(object cache)
    {
        FieldInfo? loaderField = cache.GetType().GetField("_bundleLoader", InstanceAny);
        object? loader = loaderField?.GetValue(cache);
        if (loader == null)
        {
            return null;
        }

        MethodInfo? getNames = loader.GetType().GetMethod("GetAllAssetNames", InstanceAny);
        return getNames?.Invoke(loader, Array.Empty<object>()) as string[];
    }

    private static readonly ECardBorderType[] BorderFallbackOrder =
    {
        ECardBorderType.Base,
        ECardBorderType.FirstEdition,
        ECardBorderType.FullArt,
        ECardBorderType.Silver,
        ECardBorderType.Gold,
        ECardBorderType.EX,
        (ECardBorderType)(-1)
    };

    public static Sprite? LoadCardArt(CardData cardData)
    {
        if (cardData == null || !EnsureInitialized() || !_available)
        {
            return null;
        }

        Sprite? sprite = TryLoadFromCaches(cardData);
        if (sprite != null)
        {
            return sprite;
        }

        if (cardData.isFoil)
        {
            CardData noFoil = CloneCardData(cardData);
            noFoil.isFoil = false;
            sprite = TryLoadFromCaches(noFoil);
            if (sprite != null)
            {
                return sprite;
            }
        }

        for (int i = 0; i < BorderFallbackOrder.Length; i++)
        {
            ECardBorderType fallbackBorder = BorderFallbackOrder[i];
            if (fallbackBorder == cardData.borderType)
            {
                continue;
            }

            CardData altBorder = CloneCardData(cardData);
            altBorder.borderType = fallbackBorder;
            sprite = TryLoadFromCaches(altBorder);
            if (sprite != null)
            {
                return sprite;
            }
        }

        return null;
    }

    private static Sprite? TryLoadFromCaches(CardData cardData)
    {
        for (int i = 0; i < ArtCaches.Count; i++)
        {
            Sprite? sprite = LoadFromCache(ArtCaches[i], cardData);
            if (sprite != null)
            {
                return sprite;
            }
        }

        return null;
    }

    private static CardData CloneCardData(CardData source)
    {
        CardData clone = new();
        clone.CopyData(source);
        return clone;
    }

    private static Sprite? LoadFromCache(object cache, CardData cardData)
    {
        if (_resolveArtPath == null || _loadSprite == null)
        {
            return null;
        }

        ParameterInfo[] parameters = _resolveArtPath.GetParameters();
        object[] args = parameters.Length switch
        {
            5 => new object[]
            {
                cardData.monsterType,
                cardData.borderType,
                cardData.expansionType,
                cardData.isDestiny,
                cardData.isFoil
            },
            4 => new object[]
            {
                cardData.monsterType,
                cardData.borderType,
                cardData.expansionType,
                cardData.isDestiny
            },
            _ => null
        };

        if (args == null)
        {
            return null;
        }

        if (_resolveArtPath.Invoke(cache, args) is not string artPath || string.IsNullOrEmpty(artPath))
        {
            return null;
        }

        return _loadSprite.Invoke(cache, new object[] { artPath }) as Sprite;
    }

    private static bool EnsureInitialized()
    {
        if (_initialized)
        {
            return _available;
        }

        _initialized = true;

        try
        {
            Type? pluginType = FindArtExpanderPluginType();
            if (pluginType == null)
            {
                Plugin.Log.LogWarning("ArtExpander bridge: Plugin type not found.");
                return false;
            }

            CollectArtCaches(pluginType);
            if (ArtCaches.Count == 0)
            {
                Plugin.Log.LogWarning("ArtExpander bridge: no ArtCache fields found.");
                return false;
            }

            Type cacheType = ArtCaches[0].GetType();
            _resolveArtPath = FindResolveArtPathMethod(cacheType);
            _loadSprite = cacheType.GetMethod(
                "LoadSprite",
                InstanceAny,
                binder: null,
                new[] { typeof(string) },
                modifiers: null);

            _available = _resolveArtPath != null && _loadSprite != null;

            if (_available)
            {
                Plugin.Log.LogInfo($"ArtExpander bridge ready ({ArtCaches.Count} cache(s), cardart.assets).");
            }
            else
            {
                Plugin.Log.LogWarning(
                    $"ArtExpander bridge: methods missing (ResolveArtPath={_resolveArtPath != null}, LoadSprite={_loadSprite != null}).");
            }
        }
        catch (Exception ex)
        {
            Plugin.Log.LogWarning($"ArtExpander bridge init failed: {ex.Message}");
            _available = false;
        }

        return _available;
    }

    private static MethodInfo? FindResolveArtPathMethod(Type cacheType)
    {
        foreach (MethodInfo method in cacheType.GetMethods(InstanceAny))
        {
            if (method.Name != "ResolveArtPath")
            {
                continue;
            }

            ParameterInfo[] parameters = method.GetParameters();
            if (parameters.Length >= 4 && parameters[0].ParameterType == typeof(EMonsterType))
            {
                return method;
            }
        }

        return null;
    }

    private static Type? FindArtExpanderPluginType()
    {
        Type? direct = Type.GetType("ArtExpander.Plugin, ArtExpander");
        if (direct != null)
        {
            return direct;
        }

        Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
        for (int i = 0; i < assemblies.Length; i++)
        {
            Assembly assembly = assemblies[i];
            if (!string.Equals(assembly.GetName().Name, "ArtExpander", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            Type? found = assembly.GetType("ArtExpander.Plugin", throwOnError: false);
            if (found != null)
            {
                return found;
            }
        }

        return null;
    }

    private static void CollectArtCaches(Type pluginType)
    {
        string[] preferredNames =
        {
            "art_cache",
            "art_cache_bundle",
            "art_cache_directory"
        };

        for (int i = 0; i < preferredNames.Length; i++)
        {
            TryAddCacheField(pluginType, preferredNames[i]);
        }

        FieldInfo[] fields = pluginType.GetFields(StaticAny);
        for (int i = 0; i < fields.Length; i++)
        {
            FieldInfo field = fields[i];
            if (!IsArtCacheField(field))
            {
                continue;
            }

            AddCacheValue(field.GetValue(null));
        }
    }

    private static void TryAddCacheField(Type pluginType, string fieldName)
    {
        FieldInfo? field = pluginType.GetField(fieldName, StaticAny);
        if (field == null)
        {
            return;
        }

        AddCacheValue(field.GetValue(null));
    }

    private static void AddCacheValue(object? cache)
    {
        if (cache != null && !ArtCaches.Contains(cache))
        {
            ArtCaches.Add(cache);
        }
    }

    private static bool IsArtCacheField(FieldInfo field)
    {
        return string.Equals(field.FieldType.Name, "ArtCache", StringComparison.Ordinal)
            || string.Equals(field.FieldType.FullName, "ArtExpander.Core.ArtCache", StringComparison.Ordinal);
    }
}
