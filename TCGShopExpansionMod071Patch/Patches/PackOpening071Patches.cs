using HarmonyLib;

namespace TCGShopExpansionMod071Patch.Patches;

internal static class PackOpening071Patches
{
    [HarmonyPostfix]
    [HarmonyPatch(typeof(CardOpeningSequence), "OpenScreen")]
    public static void OpenScreen_Postfix(CardOpeningSequence __instance)
    {
        PackOpeningState.SyncFromSequence(__instance);
        SyncAllPackOpeningPresentations(__instance);
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(CardOpeningSequence), "InitOpenSequence")]
    public static void InitOpenSequence_Postfix(CardOpeningSequence __instance)
    {
        PackOpeningState.SyncFromSequence(__instance);
        SyncAllPackOpeningPresentations(__instance);
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(CardOpeningSequence), "Update")]
    public static void CardOpeningSequence_Update_Postfix(CardOpeningSequence __instance)
    {
        if (!__instance.IsActive())
        {
            return;
        }

        PackOpeningState.SyncFromSequence(__instance);

        int state = __instance.m_StateIndex;
        if (state is >= 0 and < 7)
        {
            SyncAllPackOpeningPresentations(__instance);
        }
        else if (state >= 7)
        {
            SyncAllFanRowPresentations(__instance);
        }
    }

    private static void SyncAllPackOpeningPresentations(CardOpeningSequence sequence)
    {
        if (sequence.m_Card3dUIList == null)
        {
            return;
        }

        for (int i = 0; i < sequence.m_Card3dUIList.Count; i++)
        {
            Card3dUIGroup? card3d = sequence.m_Card3dUIList[i];
            CardUI? cardUi = card3d?.m_CardUI;
            CardData? cardData = cardUi?.GetCardData();
            if (cardUi == null || cardData == null || cardData.expansionType != ECardExpansionType.Tetramon)
            {
                continue;
            }

            TetramonOverlay071Patches.ConfigurePackOpeningCardPresentation(cardUi, card3d);

            if (PackOpeningState.ShouldShowFrontDuringPackFlip(card3d))
            {
                TetramonOverlay071Patches.SetCardUI_ApplyTetramonOverlay(cardUi, cardData, forceFrontOverlay: true);
            }
        }
    }

    private static void SyncAllFanRowPresentations(CardOpeningSequence sequence)
    {
        if (sequence.m_Card3dUIList == null)
        {
            return;
        }

        for (int i = 0; i < sequence.m_Card3dUIList.Count; i++)
        {
            Card3dUIGroup? card3d = sequence.m_Card3dUIList[i];
            if (card3d == null || !card3d.gameObject.activeSelf)
            {
                continue;
            }

            CardUI? cardUi = card3d.m_CardUI;
            CardData? cardData = cardUi?.GetCardData();
            if (cardUi == null || cardData == null || cardData.expansionType != ECardExpansionType.Tetramon)
            {
                continue;
            }

            TetramonOverlay071Patches.ApplyPackOpeningFanRowPresentation(cardUi, card3d);
            TetramonOverlay071Patches.SetCardUI_ApplyTetramonOverlay(cardUi, cardData, forceFrontOverlay: true);
        }
    }
}
