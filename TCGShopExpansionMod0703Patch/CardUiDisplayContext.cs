using UnityEngine;

namespace TCGShopExpansionMod0703Patch;

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

    /// <summary>
    /// Binder album slots use Card3dUIGroup on BinderPageGrp; some album paths use InteractableCard3d flags.
    /// </summary>
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

        if (ResolveCard3dGroup(cardUi)?.GetComponentInParent<BinderPageGrp>() != null)
        {
            return true;
        }

        InteractableCard3d? interactable = Card3dInteractableRegistry.FindForCardUi(cardUi);
        return interactable != null
            && (interactable.m_IsCardAlbumCard || interactable.m_CollectionBinderFlipAnimCtrl != null);
    }

    /// <summary>
    /// Rotatable Pokemon card back is only for shelf/display stands (walk behind the stand).
    /// Held cards and album picks must stay front-facing.
    /// </summary>
    public static bool ShouldUseRotatableWorldCardBack(CardUI cardUi)
    {
        if (cardUi == null || IsFlatAlbumOrBinderCard(cardUi))
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

        if (interactable.m_IsCardAlbumCard || interactable.m_CollectionBinderFlipAnimCtrl != null)
        {
            return false;
        }

        // Only shelf-mounted display cards need a visible back when viewed from behind.
        // Held cards (album pickup, hand) must stay front-facing.
        return interactable.IsDisplayedOnShelf();
    }

    /// <summary>Binder page slots and 3D album interactables use flat front-only UI.</summary>
    public static bool IsFlatAlbumOrBinderCard(CardUI cardUi)
    {
        if (IsBinderAlbumCard(cardUi))
        {
            return true;
        }

        InteractableCard3d? interactable = Card3dInteractableRegistry.FindForCardUi(cardUi);
        return interactable != null
            && (interactable.m_IsCardAlbumCard || interactable.m_CollectionBinderFlipAnimCtrl != null);
    }
}
