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
            if (PackOpeningState.ShouldShowFrontDuringPackFlip(card3dUI))
            {
                TetramonOverlay071Patches.ApplyPackOpeningFlipFrontPresentation(cardUi, card3dUI);
            }
            else
            {
                TetramonOverlay071Patches.ConfigurePackOpeningCardPresentation(cardUi, card3dUI);
            }

            return;
        }

        if (CardUiDisplayContext.IsBinderAlbumCard(cardUi))
        {
            TetramonOverlay071Patches.ApplyFlatScreenCardPresentation(cardUi, card3dUI);
            return;
        }

        if (!CardUiDisplayContext.ShouldUseRotatableWorldCardBack(cardUi))
        {
            TetramonOverlay071Patches.ApplyFlatScreenCardPresentation(cardUi, card3dUI);
            return;
        }

        TetramonOverlay071Patches.SyncTetramonCardBackAfterExpansionMod(cardUi, card3dUI);
    }
}
