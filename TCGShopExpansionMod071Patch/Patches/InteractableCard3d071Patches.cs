using HarmonyLib;
using UnityEngine;

namespace TCGShopExpansionMod071Patch.Patches;

internal static class InteractableCard3d071Patches
{
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

        AlignDisplayCardUiToSlot(interactable);
        TetramonOverlay071Patches.SetCardUI_ApplyTetramonOverlay(cardUi, cardData, forceFrontOverlay: true);
    }

    /// <summary>
    /// Shelf slots point along transform.forward; ensure m_CardFront faces that direction so customers see the art.
    /// </summary>
    private static void AlignDisplayCardUiToSlot(InteractableCard3d interactable)
    {
        Card3dUIGroup? card3d = interactable.m_Card3dUI;
        CardUI? cardUi = card3d?.m_CardUI;
        if (card3d?.m_CardUIAnimGrp == null || cardUi?.m_CardFront == null)
        {
            return;
        }

        Vector3 slotForward = interactable.transform.forward;
        Vector3 frontNormal = cardUi.m_CardFront.transform.forward;
        bool frontFacesSlot = Vector3.Dot(frontNormal, slotForward) >= 0f;

        Transform animGrp = card3d.m_CardUIAnimGrp;
        Vector3 euler = animGrp.localEulerAngles;
        float targetY = frontFacesSlot ? 0f : 180f;
        if (Mathf.Abs(Mathf.DeltaAngle(euler.y, targetY)) > 1f)
        {
            animGrp.localRotation = Quaternion.Euler(euler.x, targetY, euler.z);
        }
    }
}
