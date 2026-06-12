using System;
using HarmonyLib;

namespace TCGShopExpansionMod071Patch.Patches;

internal static class CardUI071Patches
{
    [HarmonyFinalizer]
    [HarmonyPatch(typeof(CardUI), "SetCardUI", new[] { typeof(CardData) })]
    public static Exception SetCardUI_Finalizer(CardUI __instance, CardData cardData, Exception __exception)
    {
        try
        {
            if (__instance != null && cardData != null && cardData.expansionType == ECardExpansionType.Tetramon)
            {
                TetramonOverlay071Patches.SetCardUI_ApplyTetramonOverlay(__instance, cardData);
            }
        }
        catch (Exception overlayError)
        {
            Plugin.Log.LogWarning($"Pokemon card overlay failed: {overlayError.GetType().Name}: {overlayError.Message}");
        }

        if (__exception != null)
        {
            Plugin.Log.LogWarning($"Suppressed ExpansionMod SetCardUI error: {__exception.GetType().Name}");
            return null;
        }

        return null;
    }
}
