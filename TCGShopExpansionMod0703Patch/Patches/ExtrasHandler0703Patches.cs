using TCGShopExpansionMod.Handlers;

namespace TCGShopExpansionMod0703Patch.Patches;

/// <summary>
/// ExpansionMod SetCardBacks applies the full texture atlas without sprite UVs.
/// Re-route back mesh / canvas updates for Tetramon and Destiny/Trainer shelf displays.
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
        if (cardUi == null || cardData == null)
        {
            return;
        }

        // Graded: shelf keeps opposite-face back; album/held only hide backs + keep face in slab.
        if (cardData.cardGrade > 0)
        {
            InteractableCard3d? interactable = Card3dInteractableRegistry.FindForCardUi(cardUi);
            bool onShelf = interactable != null && interactable.IsDisplayedOnShelf();
            if (onShelf)
            {
                TetramonOverlay0703Patches.ApplyGradedHeldPresentation(cardUi, card3dUI);
            }
            else
            {
                // Do not call ConfigureCard3dForFrontDisplay — that re-enables shop card back.
                if (cardData.expansionType != ECardExpansionType.Tetramon)
                {
                    TetramonOverlay0703Patches.ApplyGradedCardPresentationPublic(cardUi, cardData);
                }
                else
                {
                    TetramonOverlay0703Patches.ApplyGradedHeldPresentation(cardUi, card3dUI);
                }

                TetramonOverlay0703Patches.HideGradedCardBackFaces(cardUi);
            }

            return;
        }

        if (cardData.expansionType != ECardExpansionType.Tetramon)
        {
            if (PackOpeningState.IsPackOpeningInProgress())
            {
                return;
            }

            if (CardUiDisplayContext.ShouldUseRotatableWorldCardBack(cardUi))
            {
                TetramonOverlay0703Patches.ConfigureCard3dForFrontDisplay(cardUi);
                return;
            }

            TetramonOverlay0703Patches.ApplyFlatScreenCardPresentation(cardUi, card3dUI);
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
