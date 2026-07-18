using System;
using System.Collections.Generic;
using HarmonyLib;
using I2.Loc;
using TCGShopExpansionMod0703Patch;
using UnityEngine;

namespace TCGShopExpansionMod0703Patch.Patches;

/// <summary>
/// Prevents TextureReplacer from corrupting TextMeshPro font materials and ItemData localization keys used by phone UI.
/// TextureReplacer methods are patched manually from Plugin (not via HarmonyPatch type attributes).
/// </summary>
internal static class TextureReplacerPhoneUiGuardPatches
{
    private static readonly string[] ProtectedFontTextureTokens =
    {
        "FredokaOne",
        "LiberationSans",
        "SDF Atlas",
        "SDF border",
    };

    [ThreadStatic]
    private static int doReplaceDepth;

    public static int DoReplaceDepth => doReplaceDepth;

    public static void EnterDoReplaceScope()
    {
        doReplaceDepth++;
    }

    public static void ExitDoReplaceScope()
    {
        if (doReplaceDepth > 0)
        {
            doReplaceDepth--;
        }
    }

    public static bool GetCachedTexture_Prefix(string name, ref Texture2D __result)
    {
        if (IsProtectedFontTextureName(name))
        {
            __result = null!;
            return false;
        }

        return true;
    }

    public static bool GetCachedTexture_Static_Prefix(string name, ref Texture2D __result)
    {
        if (IsProtectedFontTextureName(name))
        {
            __result = null!;
            return false;
        }

        return true;
    }

    public static bool ForceWhiteIfNotGrayOrWhite_Prefix(Material m)
    {
        if (m?.shader == null)
        {
            return true;
        }

        string shaderName = m.shader.name;
        if (shaderName.IndexOf("TextMeshPro", StringComparison.OrdinalIgnoreCase) >= 0
            || shaderName.IndexOf("TMP", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            return false;
        }

        return true;
    }

    public static bool UpdateTitleTexts_Prefix()
    {
        System.Type? textureReplacer = ResolveTextureReplacerPluginType();
        if (textureReplacer == null)
        {
            return true;
        }

        System.Reflection.FieldInfo? field = AccessTools.Field(textureReplacer, "phone_scannername");
        string? scannerName = field?.GetValue(null) as string;
        return !string.IsNullOrEmpty(scannerName);
    }

    public static void ReplaceItemDataInList_Prefix(List<ItemData> spriteList, ref Dictionary<int, string>? __state)
    {
        if (spriteList == null || spriteList.Count == 0)
        {
            return;
        }

        __state = new Dictionary<int, string>(spriteList.Count);
        for (int i = 0; i < spriteList.Count; i++)
        {
            __state[i] = spriteList[i].name ?? string.Empty;
        }
    }

    public static void ReplaceItemDataInList_Postfix(List<ItemData> spriteList, Dictionary<int, string>? __state)
    {
        if (spriteList == null || __state == null || __state.Count == 0)
        {
            return;
        }

        for (int i = 0; i < spriteList.Count; i++)
        {
            if (!__state.TryGetValue(i, out string originalKey) || string.IsNullOrEmpty(originalKey))
            {
                continue;
            }

            string current = spriteList[i].name ?? string.Empty;
            if (string.Equals(current, originalKey, StringComparison.Ordinal))
            {
                continue;
            }

            string translatedCurrent = LocalizationManager.GetTranslation(current);
            if (string.IsNullOrEmpty(translatedCurrent))
            {
                spriteList[i].name = originalKey;
            }
        }
    }

    public static void FixPhone_Postfix()
    {
        try
        {
            PhoneManager? phoneManager = PhoneManagerAccess.FindPhoneManager();
            PhoneCanvasRepair.EnsurePhoneCanvasesReady(phoneManager?.m_PhoneGrp);
            PhoneUiLateRepairBehaviour.RequestDeferredMaterialSweep();
            PhoneUi0703Patches.SchedulePhoneHomeRepair();
        }
        catch (System.Exception ex)
        {
            Plugin.Log.LogWarning($"Phone UI repair after TextureReplacer.FixPhone failed: {ex.Message}");
        }
    }

    public static void DoReplace_Prefix()
    {
        PhoneFontMaterialSnapshot.CaptureIfNeeded();
        EnterDoReplaceScope();
    }

    public static void DoReplace_Finalizer()
    {
        ExitDoReplaceScope();
        PhoneUiLateRepairBehaviour.RequestDeferredMaterialSweep();
    }

    internal static System.Type? ResolveTextureReplacerPluginType()
    {
        foreach (System.Reflection.Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            try
            {
                if (!string.Equals(assembly.GetName().Name, "TextureReplacer", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                return assembly.GetType("TextureReplacer.BepInExPlugin");
            }
            catch
            {
                // ignored
            }
        }

        return AccessTools.TypeByName("TextureReplacer.BepInExPlugin");
    }

    internal static bool IsProtectedFontTextureName(string? name)
    {
        if (string.IsNullOrEmpty(name))
        {
            return false;
        }

        foreach (string token in ProtectedFontTextureTokens)
        {
            if (name.IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return true;
            }
        }

        return false;
    }
}
