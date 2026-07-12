using System;
using System.Collections.Generic;
using HarmonyLib;
using I2.Loc;
using TMPro;
using UnityEngine.UI;

namespace TCGShopExpansionMod071Patch.Patches;

/// <summary>
/// Vanilla UpdateSetUI indexes m_GradeCardPanelUIList[i] for every card in the set with no clamp.
/// Mods or oversized submissions can throw ArgumentOutOfRangeException.
/// </summary>
internal static class GradedCardSetCheckStatusScreen071Patches
{
    [HarmonyPrefix]
    [HarmonyPatch(typeof(GradedCardSetCheckStatusScreen), nameof(GradedCardSetCheckStatusScreen.UpdateSetUI))]
    public static bool UpdateSetUI_Prefix(GradedCardSetCheckStatusScreen __instance, int setSlotIndex)
    {
        try
        {
            List<GradeCardPanelUI>? panels = __instance.m_GradeCardPanelUIList;
            if (panels == null || panels.Count == 0)
            {
                return false;
            }

            List<GradeCardSubmitSet>? inProgress = CPlayerData.m_GradeCardInProgressList;
            if (inProgress != null
                && setSlotIndex >= 0
                && setSlotIndex < inProgress.Count
                && inProgress[setSlotIndex] != null)
            {
                GradeCardSubmitSet submitSet = inProgress[setSlotIndex];
                GradeCardServiceData? serviceData = CSingleton<InventoryBase>.Instance?.m_MonsterData_SO
                    ?.GetGradeCardServiceData(submitSet.m_ServiceLevel);

                if (serviceData != null && __instance.m_SetNameText != null)
                {
                    string translation = LocalizationManager.GetTranslation("XXX Set YYY");
                    translation = translation.Replace("XXX", serviceData.GetServiceName());
                    translation = translation.Replace("YYY", (setSlotIndex + 1).ToString());
                    __instance.m_SetNameText.text = translation;
                }

                if (__instance.m_DeliveryDaysText != null && serviceData != null)
                {
                    int daysLeft = serviceData.m_ServiceDays - submitSet.m_DayPassed;
                    string key = daysLeft > 1 ? "Delivery in XXX days" : "Delivery in XXX day";
                    __instance.m_DeliveryDaysText.text =
                        LocalizationManager.GetTranslation(key).Replace("XXX", daysLeft.ToString());
                }

                List<CardData>? cards = submitSet.m_CardDataList;
                int cardCount = cards?.Count ?? 0;
                if (cardCount > panels.Count)
                {
                    Plugin.Log.LogWarning(
                        $"Graded set slot {setSlotIndex} has {cardCount} cards but only {panels.Count} panels; clamping display.");
                }

                for (int i = 0; i < panels.Count; i++)
                {
                    GradeCardPanelUI? panel = panels[i];
                    if (panel == null)
                    {
                        continue;
                    }

                    CardData? card = (cards != null && i < cardCount) ? cards[i] : null;
                    panel.UpdateCardUI(card);
                }
            }
            else
            {
                for (int i = 0; i < panels.Count; i++)
                {
                    panels[i]?.UpdateCardUI(null);
                }
            }

            int pageMaxIndex = AccessTools.Field(typeof(GradedCardSetCheckStatusScreen), "m_PageMaxIndex")
                ?.GetValue(__instance) is int max
                ? max
                : Math.Max(0, (inProgress?.Count ?? 1) - 1);
            int pageIndex = AccessTools.Field(typeof(GradedCardSetCheckStatusScreen), "m_PageIndex")
                ?.GetValue(__instance) is int page
                ? page
                : setSlotIndex;

            if (__instance.m_PageText != null)
            {
                __instance.m_PageText.text = $"{setSlotIndex + 1} / {pageMaxIndex + 1}";
            }

            if (__instance.m_NextButton != null)
            {
                __instance.m_NextButton.interactable = pageIndex < pageMaxIndex;
            }

            if (__instance.m_PreviousButton != null)
            {
                __instance.m_PreviousButton.interactable = pageIndex > 0;
            }

            return false;
        }
        catch (Exception ex)
        {
            Plugin.Log.LogWarning($"GradedCardSetCheckStatusScreen.UpdateSetUI safe path failed: {ex.GetType().Name}: {ex.Message}");
            return true;
        }
    }
}
