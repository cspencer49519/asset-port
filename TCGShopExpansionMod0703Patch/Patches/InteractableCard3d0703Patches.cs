using HarmonyLib;
using UnityEngine;

namespace TCGShopExpansionMod0703Patch.Patches;

internal static class InteractableCard3d0703Patches
{
    [HarmonyPostfix]
    [HarmonyPatch(typeof(InteractableCardCompartment), "SetCardOnShelf")]
    public static void SetCardOnShelf_Postfix(InteractableCard3d card)
    {
        if (card?.m_Card3dUI == null || !card.IsDisplayedOnShelf())
        {
            return;
        }

        Card3dInteractableRegistry.Register(card.m_Card3dUI, card);
        EnableDisplayCulling(card.m_Card3dUI);
        RefreshDisplayPresentation(card);
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(InteractableCard3d), "SetIsDisplayedOnShelf")]
    public static void SetIsDisplayedOnShelf_Postfix(InteractableCard3d __instance, bool isDisplayedOnShelf)
    {
        if (!isDisplayedOnShelf || __instance.m_Card3dUI == null)
        {
            return;
        }

        Card3dInteractableRegistry.Register(__instance.m_Card3dUI, __instance);
        EnableDisplayCulling(__instance.m_Card3dUI);
        RefreshDisplayPresentation(__instance);
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(InteractableCard3d), "Start")]
    public static void Start_Postfix(InteractableCard3d __instance)
    {
        if (__instance.m_Card3dUI != null && __instance.m_IsCard3dUIFollow)
        {
            Card3dInteractableRegistry.Register(__instance.m_Card3dUI, __instance);
            if (__instance.IsDisplayedOnShelf())
            {
                EnableDisplayCulling(__instance.m_Card3dUI);
                RefreshDisplayPresentation(__instance);
            }
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(InteractableCard3d), "SetCardUIFollow")]
    public static void SetCardUIFollow_Postfix(InteractableCard3d __instance, Card3dUIGroup card3dUI)
    {
        Card3dInteractableRegistry.Register(card3dUI, __instance);
        if (__instance.IsDisplayedOnShelf())
        {
            EnableDisplayCulling(card3dUI);
            RefreshDisplayPresentation(__instance);
            return;
        }

        CardUI? cardUi = card3dUI?.m_CardUI;
        if (cardUi == null)
        {
            return;
        }

        // Graded: slab + front toward camera; skip flat-screen scale reset that undoes ShowGradedCardCase.
        CardData? cardData = cardUi.GetCardData();
        if (cardData != null && cardData.cardGrade > 0)
        {
            TetramonOverlay0703Patches.ApplyGradedHeldPresentation(cardUi, card3dUI);
            return;
        }

        // Album pickup / held card: keep front face toward camera (never shop-display back).
        if (__instance.m_IsCardAlbumCard
            || __instance.m_CollectionBinderFlipAnimCtrl != null
            || !CardUiDisplayContext.ShouldUseRotatableWorldCardBack(cardUi))
        {
            TetramonOverlay0703Patches.ApplyFlatScreenCardPresentation(cardUi, card3dUI);
        }
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(InteractableCard3d), "OnDestroyed")]
    public static void OnDestroyed_Prefix(InteractableCard3d __instance)
    {
        Card3dInteractableRegistry.UnregisterInteractable(__instance);
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(InteractableCard3d), "OnFinishLerp")]
    public static void OnFinishLerp_Postfix(InteractableCard3d __instance)
    {
        if (!__instance.IsDisplayedOnShelf())
        {
            return;
        }

        RefreshDisplayPresentation(__instance);
    }

    private static void EnableDisplayCulling(Card3dUIGroup card3d)
    {
        card3d.m_IgnoreCulling = true;
        card3d.SetAlwaysCulling(alwaysCulling: false);
        card3d.SetSimplifyCardDistanceCull(isCull: false);
        if (card3d.m_CardUIAnimGrp != null)
        {
            card3d.m_CardUIAnimGrp.gameObject.SetActive(true);
        }

        card3d.m_CardUI.SetFoilCullListVisibility(isActive: true);
        card3d.m_CardUI.ResetFarDistanceCull();
    }

    private static void RefreshDisplayPresentation(InteractableCard3d interactable)
    {
        CardUI? cardUi = interactable.m_Card3dUI?.m_CardUI;
        CardData? cardData = cardUi?.GetCardData();
        if (cardUi == null || cardData == null || cardData.expansionType != ECardExpansionType.Tetramon)
        {
            return;
        }

        if (!CardUiDisplayContext.ShouldUseRotatableWorldCardBack(cardUi))
        {
            return;
        }

        TetramonOverlay0703Patches.ConfigureCard3dForFrontDisplay(cardUi);
    }

    /// <summary>
    /// Display arms mount with the card back toward +putCardLocation.forward. Flip m_CardUIAnimGrp
    /// when that mount direction faces the shop aisle so overlay art faces customers.
    /// </summary>
    public static void AlignDisplayCardUiToSlot(InteractableCard3d interactable)
    {
        Card3dUIGroup? card3d = interactable.m_Card3dUI;
        CardUI? cardUi = card3d?.m_CardUI;
        if (card3d?.m_CardUIAnimGrp == null || cardUi?.m_CardFront == null)
        {
            return;
        }

        Vector3 toCustomer = ResolveDisplayCustomerViewDirection(interactable);
        float targetY = ResolveDisplayAnimGroupTargetY(interactable, cardUi, toCustomer);

        Transform animGrp = card3d.m_CardUIAnimGrp;
        Vector3 euler = animGrp.localEulerAngles;
        if (Mathf.Abs(Mathf.DeltaAngle(euler.y, targetY)) > 1f)
        {
            animGrp.localRotation = Quaternion.Euler(euler.x, targetY, euler.z);
        }
    }

    private static float ResolveDisplayAnimGroupTargetY(
        InteractableCard3d interactable,
        CardUI cardUi,
        Vector3 toCustomer)
    {
        Transform? putLocation = ResolvePutCardLocation(interactable);
        if (putLocation != null)
        {
            Vector3 mountForward = putLocation.forward;
            mountForward.y = 0f;
            if (mountForward.sqrMagnitude > 0.0001f)
            {
                mountForward.Normalize();
                bool mountBackFacesCustomer = Vector3.Dot(mountForward, toCustomer) >= 0f;
                return mountBackFacesCustomer ? 0f : 180f;
            }
        }

        Vector3 frontNormal = cardUi.m_CardFront.transform.forward;
        return Vector3.Dot(frontNormal, toCustomer) >= 0f ? 180f : 0f;
    }

    private static Transform? ResolvePutCardLocation(InteractableCard3d interactable)
    {
        InteractableCardCompartment? compartment = interactable.GetComponentInParent<InteractableCardCompartment>();
        if (compartment == null)
        {
            return null;
        }

        CardData? cardData = interactable.m_Card3dUI?.m_CardUI?.GetCardData();
        if (compartment.m_NoneGradedCardUseAltCardLocation
            && cardData != null
            && cardData.cardGrade == 0
            && compartment.m_PutCardLocationAlt != null)
        {
            return compartment.m_PutCardLocationAlt;
        }

        return compartment.m_PutCardLocation;
    }

    private static Vector3 ResolveDisplayCustomerViewDirection(InteractableCard3d interactable)
    {
        InteractableCardCompartment? compartment = interactable.GetComponentInParent<InteractableCardCompartment>();
        Transform? customerStand = compartment?.m_CustomerStandLoc;
        Transform? putLocation = ResolvePutCardLocation(interactable);
        Vector3 cardPosition = putLocation != null ? putLocation.position : interactable.transform.position;

        if (customerStand != null)
        {
            Vector3 toCustomer = customerStand.position - cardPosition;
            toCustomer.y = 0f;
            if (toCustomer.sqrMagnitude > 0.0001f)
            {
                return toCustomer.normalized;
            }
        }

        return interactable.transform.forward;
    }
}
