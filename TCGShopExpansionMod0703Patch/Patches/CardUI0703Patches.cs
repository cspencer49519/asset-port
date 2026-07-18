using System;
using HarmonyLib;

namespace TCGShopExpansionMod0703Patch.Patches;

internal static class CardUI0703Patches
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
            TetramonOverlay0703Patches.SuppressTetramonHoverChromeBleed(__instance);
        }
        catch (Exception overlayError)
        {
            Plugin.Log.LogWarning($"Pokemon hover chrome fix failed: {overlayError.GetType().Name}: {overlayError.Message}");
        }
    }

    // HO SetFoilMaterialList / LateUpdate material lock can reassign CardFoilRainbow with white _MainTex
    // after our SetCardUI finalizer — re-feed album card art into foil hosts.
    [HarmonyPostfix]
    [HarmonyPriority(Priority.Last)]
    [HarmonyPatch(typeof(CardUI), "SetFoilMaterialList")]
    public static void SetFoilMaterialList_Postfix(CardUI __instance)
    {
        try
        {
            TetramonOverlay0703Patches.RepairAlbumHoFoilMainTex(__instance);
        }
        catch (Exception foilError)
        {
            Plugin.Log.LogWarning($"Album foil MainTex repair failed: {foilError.GetType().Name}: {foilError.Message}");
        }
    }

    [HarmonyPostfix]
    [HarmonyPriority(Priority.Last)]
    [HarmonyPatch(typeof(CardUI), "SetFoilBlendedMaterialList")]
    public static void SetFoilBlendedMaterialList_Postfix(CardUI __instance)
    {
        try
        {
            TetramonOverlay0703Patches.RepairAlbumHoFoilMainTex(__instance);
        }
        catch (Exception foilError)
        {
            Plugin.Log.LogWarning($"Album foil blended MainTex repair failed: {foilError.GetType().Name}: {foilError.Message}");
        }
    }

    private static void ApplyTetramonPresentation(CardUI __instance, CardData cardData)
    {
        if (__instance == null || cardData == null)
        {
            return;
        }

        // Graded slabs: never run Tetramon/Destiny overlay or album foil binding.
        if (cardData.cardGrade > 0)
        {
            TetramonOverlay0703Patches.ApplyGradedCardPresentationPublic(__instance, cardData);
            return;
        }

        // Binder album CardUI instances are reused when cycling F (Tetramon/Destiny/etc.).
        // Non-Tetramon album gets ArtExpander full-card overlay; sell/pack only clear stale overlay.
        if (cardData.expansionType != ECardExpansionType.Tetramon)
        {
            TetramonOverlay0703Patches.ApplyNonTetramonPresentation(__instance, cardData);
            return;
        }

        TetramonOverlay0703Patches.SetCardUI_ApplyTetramonOverlay(__instance, cardData);
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
                    "Further ExpansionMod SetCardUI suppressions will be silent this session (HandleCards is skipped on 0.70.3).");
            }
        }
    }
}
