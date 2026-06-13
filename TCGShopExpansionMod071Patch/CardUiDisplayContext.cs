namespace TCGShopExpansionMod071Patch;

/// <summary>
/// Binder album slots use Card3dUIGroup on BinderPageGrp; display stands use followed Card3dUIGroup.
/// </summary>
internal static class CardUiDisplayContext
{
    public static Card3dUIGroup? ResolveCard3dGroup(CardUI cardUi)
    {
        if (cardUi == null)
        {
            return null;
        }

        if (CardUiFieldAccess.GetValue(cardUi, "m_Card3dUIGroup") is Card3dUIGroup fromField)
        {
            return fromField;
        }

        return cardUi.GetComponentInParent<Card3dUIGroup>();
    }

    public static bool IsBinderAlbumCard(CardUI cardUi)
    {
        if (cardUi == null)
        {
            return false;
        }

        if (cardUi.GetComponentInParent<BinderPageGrp>() != null)
        {
            return true;
        }

        return ResolveCard3dGroup(cardUi)?.GetComponentInParent<BinderPageGrp>() != null;
    }

    public static bool ShouldUseRotatableWorldCardBack(CardUI cardUi)
    {
        if (cardUi == null || IsBinderAlbumCard(cardUi))
        {
            return false;
        }

        if (PackOpeningState.IsPackOpeningInProgress() || PackOpeningState.IsFanRowVisible())
        {
            return false;
        }

        if (ResolveCard3dGroup(cardUi) == null)
        {
            return false;
        }

        InteractableCard3d? interactable = Card3dInteractableRegistry.FindForCardUi(cardUi);
        if (interactable == null)
        {
            return false;
        }

        if (interactable.m_IsCardAlbumCard)
        {
            return false;
        }

        if (interactable.m_CollectionBinderFlipAnimCtrl != null)
        {
            return false;
        }

        return true;
    }
}
