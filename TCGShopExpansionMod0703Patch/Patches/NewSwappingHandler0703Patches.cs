using HarmonyLib;
using TCGShopExpansionMod.Handlers;

namespace TCGShopExpansionMod0703Patch.Patches;

/// <summary>
/// Game 0.70.3 removed MonsterData_ScriptableObject.m_CardBorderList / m_CardBGList /
/// m_CardFrontImageList. Only patch SetCardExtrasImages — Harmony cannot compile patches
/// on ReplaceCard* methods because their IL references the removed fields.
/// </summary>
internal static class NewSwappingHandler0703Patches
{
    public static bool SetCardExtrasImages_Prefix()
    {
        Plugin.Log.LogDebug("Skipping SetCardExtrasImages (obsolete global lists on 0.70.3).");
        return false;
    }
}
