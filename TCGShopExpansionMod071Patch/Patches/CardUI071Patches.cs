using System;
using HarmonyLib;

namespace TCGShopExpansionMod071Patch.Patches;

internal static class CardUI071Patches
{
    private const int MaxSuppressedSetCardUiWarnings = 5;
    private static int SuppressedSetCardUiWarningCount;

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
            LogSuppressedSetCardUiError(__exception);
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

    private static void LogSuppressedSetCardUiError(Exception exception)
    {
        SuppressedSetCardUiWarningCount++;
        if (SuppressedSetCardUiWarningCount <= MaxSuppressedSetCardUiWarnings)
        {
            Plugin.Log.LogWarning($"Suppressed ExpansionMod SetCardUI error: {exception.GetType().Name}");
            if (SuppressedSetCardUiWarningCount == MaxSuppressedSetCardUiWarnings)
            {
                Plugin.Log.LogWarning(
                    "Further ExpansionMod SetCardUI suppressions will be silent this session (HandleCards is skipped on 0.71).");
            }
        }
    }
}
