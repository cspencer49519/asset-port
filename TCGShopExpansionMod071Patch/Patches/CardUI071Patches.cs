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
            ApplyTetramonPresentation(__instance, cardData);
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

    [HarmonyPostfix]
    [HarmonyPriority(Priority.Last)]
    [HarmonyPatch(typeof(CardUI), "SetCardUI", new[] { typeof(CardData) })]
    public static void SetCardUI_Postfix_Last(CardUI __instance, CardData cardData)
    {
        try
        {
            ApplyTetramonPresentation(__instance, cardData);
        }
        catch (Exception overlayError)
        {
            Plugin.Log.LogWarning($"Pokemon card overlay (late) failed: {overlayError.GetType().Name}: {overlayError.Message}");
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(CardUI), "SetBrightness", new[] { typeof(float) })]
    public static void SetBrightness_Postfix(CardUI __instance)
    {
        try
        {
            TetramonOverlay071Patches.SuppressTetramonHoverChromeBleed(__instance);
        }
        catch (Exception overlayError)
        {
            Plugin.Log.LogWarning($"Pokemon hover chrome fix failed: {overlayError.GetType().Name}: {overlayError.Message}");
        }
    }

    private static void ApplyTetramonPresentation(CardUI __instance, CardData cardData)
    {
        if (__instance == null || cardData == null || cardData.expansionType != ECardExpansionType.Tetramon)
        {
            return;
        }

        TetramonOverlay071Patches.SetCardUI_ApplyTetramonOverlay(__instance, cardData);
    }
}
