using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace TCGShopExpansionMod071Patch;

/// <summary>
/// Keeps pristine copies of Fredoka TMP materials before TextureReplacer mutates shared assets.
/// </summary>
internal static class PhoneFontMaterialSnapshot
{
    private static readonly Dictionary<string, Material> Snapshots = new(StringComparer.Ordinal);
    private static bool captured;

    public static void CaptureIfNeeded()
    {
        if (captured)
        {
            return;
        }

        TMP_FontAsset[] fonts = Resources.FindObjectsOfTypeAll<TMP_FontAsset>();
        foreach (TMP_FontAsset font in fonts)
        {
            if (font == null || string.IsNullOrEmpty(font.name) || font.material == null)
            {
                continue;
            }

            if (!font.name.StartsWith("FredokaOne", StringComparison.Ordinal)
                && !font.name.StartsWith("LiberationSans", StringComparison.Ordinal))
            {
                continue;
            }

            if (Snapshots.ContainsKey(font.name))
            {
                continue;
            }

            Snapshots[font.name] = new Material(font.material)
            {
                name = $"PhoneFontSnapshot_{font.name}",
            };

            if (font.atlasTexture != null)
            {
                Snapshots[font.name].mainTexture = font.atlasTexture;
            }
        }

        captured = Snapshots.Count > 0;
        if (captured)
        {
            Plugin.Log.LogInfo($"Phone font material snapshot captured {Snapshots.Count} TMP material(s).");
        }
    }

    public static void RestoreSharedFontMaterial(TMP_FontAsset font)
    {
        if (font == null || font.material == null)
        {
            return;
        }

        if (!Snapshots.TryGetValue(font.name, out Material? snapshot))
        {
            return;
        }

        Material restored = new Material(snapshot)
        {
            name = font.material.name,
        };

        if (font.atlasTexture != null)
        {
            restored.mainTexture = font.atlasTexture;
        }

        font.material = restored;
    }

    public static Material CreateLabelMaterial(TMP_FontAsset font)
    {
        Material source = Snapshots.TryGetValue(font.name, out Material? snapshot)
            ? snapshot
            : font.material;

        Material labelMaterial = new Material(source)
        {
            name = $"PhoneLabel_{font.name}",
        };

        if (font.atlasTexture != null)
        {
            labelMaterial.mainTexture = font.atlasTexture;
        }

        return labelMaterial;
    }

    public static bool IsBorderFont(TMP_FontAsset? font)
    {
        return font != null
            && font.name.IndexOf("border", StringComparison.OrdinalIgnoreCase) >= 0;
    }
}
