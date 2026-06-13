using TCGShopExpansionMod.Handlers;

namespace TCGShopExpansionMod071Patch.Patches;

/// <summary>
/// ExpansionMod SetCardBacks applies the full texture atlas without sprite UVs.
/// Re-route Tetramon back mesh updates so front faces hide the mesh entirely.
/// </summary>
internal static class ExtrasHandler071Patches
{
    public static void SetCardBacks_Postfix(Card3dUIGroup __0)
    {
        AfterExpansionModSetCardBack(__0);
    }

    public static void SetCardBackPackOpening_Postfix(Card3dUIGroup __0)
    {
        AfterExpansionModSetCardBack(__0);
    }

    private static void AfterExpansionModSetCardBack(Card3dUIGroup card3dUI)
    {
        CardUI? cardUi = card3dUI?.m_CardUI;
        CardData? cardData = cardUi?.GetCardData();
        if (cardUi == null || cardData == null || cardData.expansionType != ECardExpansionType.Tetramon)
        {
            return;
        }

        if (PackOpeningState.IsPackOpeningInProgress())
        {
            TetramonOverlay071Patches.SyncPackOpeningBackMesh(cardUi, card3dUI);
            return;
        }

        TetramonOverlay071Patches.ConfigureCard3dForFrontDisplay(cardUi);
    }
}
