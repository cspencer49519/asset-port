using TCGShopExpansionMod.Handlers;

namespace TCGShopExpansionMod0703Patch.Patches;

/// <summary>
/// ExpansionMod SetCardBacks applies the full texture atlas without sprite UVs.
/// Re-route Tetramon back mesh updates so pack cards get correct sprite UVs.
/// </summary>
internal static class ExtrasHandler0703Patches
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

        // Graded slabs: front + case toward camera (no rotatable back / flat-scale reset).
        if (cardData.cardGrade > 0)
        {
            TetramonOverlay0703Patches.ApplyGradedHeldPresentation(cardUi, card3dUI);
            return;
        }

        if (PackOpeningState.IsPackOpeningInProgress())
        {
            if (PackOpeningState.ShouldShowFrontDuringPackFlip(card3dUI))
            {
                TetramonOverlay0703Patches.ApplyPackOpeningFlipFrontPresentation(cardUi, card3dUI);
            }
            else
            {
                TetramonOverlay0703Patches.ConfigurePackOpeningCardPresentation(cardUi, card3dUI);
            }

            return;
        }

        if (CardUiDisplayContext.IsBinderAlbumCard(cardUi)
            || CardUiDisplayContext.IsFlatAlbumOrBinderCard(cardUi)
            || !CardUiDisplayContext.ShouldUseRotatableWorldCardBack(cardUi))
        {
            TetramonOverlay0703Patches.ApplyFlatScreenCardPresentation(cardUi, card3dUI);
            return;
        }

        TetramonOverlay0703Patches.SyncTetramonCardBackAfterExpansionMod(cardUi, card3dUI);
    }
}
