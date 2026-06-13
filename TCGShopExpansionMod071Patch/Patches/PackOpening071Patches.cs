using HarmonyLib;

namespace TCGShopExpansionMod071Patch.Patches;

internal static class PackOpening071Patches
{
    private static int _lastObservedStateIndex = -1;

    [HarmonyPostfix]
    [HarmonyPatch(typeof(CardOpeningSequence), "InitOpenSequence")]
    public static void InitOpenSequence_Postfix(CardOpeningSequence __instance)
    {
        SyncPackBackMeshes(__instance);
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(CardOpeningSequence), "Update")]
    public static void CardOpeningSequence_Update_Postfix(CardOpeningSequence __instance)
    {
        if (!__instance.IsActive())
        {
            _lastObservedStateIndex = -1;
            return;
        }

        int state = __instance.m_StateIndex;
        if (state is >= 0 and < 7)
        {
            SyncPackBackMeshes(__instance);
        }
        else if (state >= 7 && _lastObservedStateIndex < 7)
        {
            RefreshAllTetramonOverlays(__instance);
        }

        _lastObservedStateIndex = state;
    }

    private static void SyncPackBackMeshes(CardOpeningSequence sequence)
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
            if (cardData == null || cardData.expansionType != ECardExpansionType.Tetramon)
            {
                continue;
            }

            TetramonOverlay071Patches.SyncPackOpeningBackMesh(cardUi, card3d);
        }
    }

    private static void RefreshAllTetramonOverlays(CardOpeningSequence sequence)
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
            if (cardData == null || cardData.expansionType != ECardExpansionType.Tetramon)
            {
                continue;
            }

            TetramonOverlay071Patches.SetCardUI_ApplyTetramonOverlay(cardUi, cardData);
        }
    }
}
