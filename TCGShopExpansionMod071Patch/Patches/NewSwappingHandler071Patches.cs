using HarmonyLib;
using TCGShopExpansionMod.Handlers;

namespace TCGShopExpansionMod071Patch.Patches;

/// <summary>
/// Game 0.71 removed MonsterData_ScriptableObject.m_CardBorderList / m_CardBGList /
/// m_CardFrontImageList. Only patch SetCardExtrasImages — Harmony cannot compile patches
/// on ReplaceCard* methods because their IL references the removed fields.
/// </summary>
internal static class NewSwappingHandler071Patches
{
    public static bool SetCardExtrasImages_Prefix()
    {
        Plugin.Log.LogDebug("Skipping SetCardExtrasImages (obsolete global lists on 0.71).");
        return false;
    }
}
