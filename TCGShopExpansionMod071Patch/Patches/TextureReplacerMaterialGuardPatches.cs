using System;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace TCGShopExpansionMod071Patch.Patches;

/// <summary>
/// Excludes TMP/font materials from TextureReplacer.DoReplace material enumeration.
/// Does not patch Material setters (that froze save load on large scenes).
/// </summary>
internal static class TextureReplacerMaterialGuardPatches
{
    public static void ApplyPatches(Harmony harmony)
    {
        harmony.PatchAll(typeof(TextureReplacerMaterialGuardPatches));
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(Resources), nameof(Resources.FindObjectsOfTypeAll), new[] { typeof(Type) })]
    public static void FindObjectsOfTypeAll_Postfix(Type type, ref UnityEngine.Object[] __result)
    {
        if (TextureReplacerPhoneUiGuardPatches.DoReplaceDepth <= 0
            || type != typeof(Material)
            || __result == null
            || __result.Length == 0)
        {
            return;
        }

        int originalCount = __result.Length;
        List<UnityEngine.Object> filtered = new List<UnityEngine.Object>(originalCount);
        foreach (UnityEngine.Object obj in __result)
        {
            Material? material = obj as Material;
            if (material == null)
            {
                continue;
            }

            try
            {
                if (ShouldProtectMaterialDuringDoReplace(material))
                {
                    continue;
                }
            }
            catch (Exception ex)
            {
                Plugin.Log.LogDebug($"DoReplace material filter skipped one entry: {ex.Message}");
                continue;
            }

            filtered.Add(obj);
        }

        __result = filtered.ToArray();
        Plugin.Log.LogInfo(
            $"DoReplace skipped {originalCount - filtered.Count} TMP/font material(s); processing {filtered.Count}.");
    }

    internal static bool ShouldProtectMaterialDuringDoReplace(Material material)
    {
        Shader? shader = material.shader;
        if (shader == null)
        {
            return false;
        }

        if (IsTmpShader(shader.name))
        {
            return true;
        }

        string materialName = material.name ?? string.Empty;
        return materialName.IndexOf("Fredoka", StringComparison.OrdinalIgnoreCase) >= 0
            || materialName.IndexOf("LiberationSans", StringComparison.OrdinalIgnoreCase) >= 0
            || materialName.IndexOf("SDF", StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private static bool IsTmpShader(string shaderName)
    {
        return shaderName.IndexOf("TextMeshPro", StringComparison.OrdinalIgnoreCase) >= 0
            || shaderName.IndexOf("TMP", StringComparison.OrdinalIgnoreCase) >= 0;
    }
}
