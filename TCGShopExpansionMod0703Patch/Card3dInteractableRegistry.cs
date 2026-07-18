using System.Collections.Generic;

namespace TCGShopExpansionMod0703Patch;

/// <summary>
/// Card3dUIGroup follows InteractableCard3d by position, not hierarchy — reverse map for display detection.
/// </summary>
internal static class Card3dInteractableRegistry
{
    private static readonly Dictionary<Card3dUIGroup, InteractableCard3d> ByCard3d = new();

    public static void Register(Card3dUIGroup? card3d, InteractableCard3d? interactable)
    {
        if (card3d == null)
        {
            return;
        }

        if (interactable == null)
        {
            ByCard3d.Remove(card3d);
            return;
        }

        ByCard3d[card3d] = interactable;
    }

    public static void UnregisterInteractable(InteractableCard3d interactable)
    {
        if (interactable?.m_Card3dUI == null)
        {
            return;
        }

        ByCard3d.Remove(interactable.m_Card3dUI);
    }

    public static InteractableCard3d? FindForCardUi(CardUI cardUi)
    {
        if (cardUi == null)
        {
            return null;
        }

        InteractableCard3d? fromHierarchy = cardUi.GetComponentInParent<InteractableCard3d>();
        if (fromHierarchy != null)
        {
            return fromHierarchy;
        }

        if (CardUiFieldAccess.GetValue(cardUi, "m_Card3dUIGroup") is Card3dUIGroup card3d
            && ByCard3d.TryGetValue(card3d, out InteractableCard3d mapped))
        {
            return mapped;
        }

        return null;
    }
}
