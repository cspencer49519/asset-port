using HarmonyLib;
using UnityEngine;
using UnityEngine.Rendering;

namespace TCGShopExpansionMod0703Patch.Patches;

internal static class PackOpening0703Patches
{
    [HarmonyPrefix]
    [HarmonyPatch(typeof(CardOpeningSequence), "Start")]
    public static void Start_Prefix(CardOpeningSequence __instance)
    {
        PackOpeningRefsBootstrap.TryBootstrap(__instance);
        PackOpeningRefsBootstrap.TryEnsureCardDataPools(__instance);
    }

    [HarmonyFinalizer]
    [HarmonyPatch(typeof(CardOpeningSequence), "Start")]
    public static System.Exception Start_Finalizer(CardOpeningSequence __instance, System.Exception __exception)
    {
        PackOpeningRefsBootstrap.TryBootstrap(__instance);
        PackOpeningRefsBootstrap.TryRecoverStart(__instance);

        if (__exception != null)
        {
            Plugin.Log.LogWarning(
                $"CardOpeningSequence.Start recovered after bootstrap: {__exception.GetType().Name}");
            return null;
        }

        return null;
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(CardOpeningSequence), "ReadyingCardPack")]
    public static bool ReadyingCardPack_Prefix(CardOpeningSequence __instance, Item item)
    {
        PackOpeningRefsBootstrap.TryBootstrap(__instance);
        PackOpeningRefsBootstrap.TryEnsureCardDataPools(__instance);
        PackOpeningRefsBootstrap.LogPackOpenReadiness(__instance);
        PackOpeningRefsBootstrap.ResetOpenDiagnostics();
        TetramonOverlay0703Patches.ResetBackMeshDiagnostics();
        // The previous open's fan row disabled the pack wrapper renderers; restore them for this pack.
        RestorePackVisuals(__instance);

        if (PackOpeningRefsBootstrap.TryRunReadyingCardPack(__instance, item))
        {
            return false;
        }

        return true;
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(CardOpeningSequence), "GetPackContent")]
    public static void GetPackContent_Prefix(CardOpeningSequence __instance)
    {
        PackOpeningRefsBootstrap.TryBootstrap(__instance);
        PackOpeningRefsBootstrap.TryEnsureCardDataPools(__instance);
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(CardOpeningSequence), "InitOpenSequence")]
    public static bool InitOpenSequence_Prefix(CardOpeningSequence __instance)
    {
        PackOpeningRefsBootstrap.TryBootstrap(__instance);
        PackOpeningRefsBootstrap.TryEnsureCardDataPools(__instance);
        PackOpeningRefsBootstrap.TryRecoverStart(__instance);
        PackOpeningRefsBootstrap.EnsurePackOpenFeedbackIcons(__instance);
        ArtExpanderBridge.DumpBackAssetNames();

        if (PackOpeningRefsBootstrap.TryRunInitOpenSequence(__instance))
        {
            return false;
        }

        return true;
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(CardOpeningSequence), "OpenScreen", typeof(ECollectionPackType), typeof(bool), typeof(bool))]
    public static void OpenScreen_Prefix(CardOpeningSequence __instance)
    {
        PackOpeningRefsBootstrap.TryBootstrap(__instance);
        PackOpeningRefsBootstrap.TryEnsureCardDataPools(__instance);
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(CardOpeningSequence), "OpenScreen", typeof(ECollectionPackType), typeof(bool), typeof(bool))]
    public static void OpenScreen_Postfix(CardOpeningSequence __instance)
    {
        if (!PackOpeningState.ShouldSyncPackPresentation(__instance))
        {
            return;
        }

        PackOpeningState.SyncFromSequence(__instance);
        SyncAllPackOpeningPresentations(__instance);
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(CardOpeningSequence), "InitOpenSequence")]
    public static void InitOpenSequence_Postfix(CardOpeningSequence __instance)
    {
        if (!PackOpeningState.ShouldSyncPackPresentation(__instance))
        {
            return;
        }

        PackOpeningState.SyncFromSequence(__instance);
        SyncAllPackOpeningPresentations(__instance);
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(CardOpeningSequence), "Update")]
    public static void Update_Prefix(CardOpeningSequence __instance)
    {
        if (__instance == null || !__instance.IsActive())
        {
            return;
        }

        PackOpeningRefsBootstrap.TryBootstrap(__instance);
        PackOpeningRefsBootstrap.EnsurePackOpenFeedbackIcons(__instance);
    }

    [HarmonyPostfix]
    [HarmonyPriority(Priority.Last)]
    [HarmonyPatch(typeof(CardOpeningSequence), "Update")]
    public static void CardOpeningSequence_Update_Last(CardOpeningSequence __instance)
    {
        if (!PackOpeningState.ShouldSyncPackPresentation(__instance))
        {
            return;
        }

        try
        {
            PackOpeningState.SyncFromSequence(__instance);
            int state = __instance.m_StateIndex;
            EnsureBackdropBelowPackCards();
            PackOpeningRefsBootstrap.DumpCardStackDiagnostics(__instance);
            if (state is >= 0 and < 7)
            {
                SyncAllPackOpeningPresentations(__instance);
            }
            else if (state >= 7)
            {
                PackOpeningRefsBootstrap.DumpFanDiagnostics(__instance);
                HidePackVisualsDuringFanRow(__instance);
                SyncAllFanRowPresentations(__instance);
            }
        }
        catch (System.Exception ex)
        {
            Plugin.Log.LogWarning($"Pack opening sync failed: {ex.GetType().Name}: {ex.Message}");
        }
    }

    // The pack-opening card faces render at the default UI queue (3000). The vanilla darkening backdrop
    // (m_BlackBGWorldUIFade mesh, MAT_PreviewTransparentBlack) is at renderQueue 3002, so it paints over the
    // cards during the fan. The backdrop only needs to sit above the opaque room (queue 2000) and below the
    // cards, so drop its instanced material just below the card queue. The material is per-instance, so this
    // does not affect any other UI/material.
    private const int PackBackdropRenderQueue = 2999;

    private static void EnsureBackdropBelowPackCards()
    {
        InteractionPlayerController? ipc = CSingleton<InteractionPlayerController>.Instance;
        MaterialFadeInOut? fade = ipc != null ? ipc.m_BlackBGWorldUIFade : null;
        MeshRenderer? mesh = fade != null ? fade.m_Mesh : null;
        if (mesh == null)
        {
            return;
        }

        Material? mat = mesh.material;
        if (mat != null && mat.renderQueue != PackBackdropRenderQueue)
        {
            mat.renderQueue = PackBackdropRenderQueue;
        }
    }

    /// <summary>Re-enable the pack wrapper renderers that the prior fan row disabled, so a new pack shows its wrapper + open animation.</summary>
    public static void RestorePackVisuals(CardOpeningSequence sequence)
    {
        if (sequence.m_CardPackMesh == null)
        {
            return;
        }

        sequence.m_CardPackMesh.enabled = true;

        Transform packRoot = sequence.m_CardPackMesh.transform;
        if (packRoot.parent != null)
        {
            packRoot = packRoot.parent;
        }

        Renderer[] packRenderers = packRoot.GetComponentsInChildren<Renderer>(includeInactive: true);
        for (int i = 0; i < packRenderers.Length; i++)
        {
            packRenderers[i].enabled = true;
        }
    }

    /// <summary>Hide only the pack prop subtree during fan row — never walk to scene root.</summary>
    public static void HidePackVisualsDuringFanRow(CardOpeningSequence sequence)
    {
        if (sequence.m_CardPackMesh != null)
        {
            sequence.m_CardPackMesh.enabled = false;
            sequence.m_CardPackMesh.shadowCastingMode = ShadowCastingMode.Off;
            sequence.m_CardPackMesh.receiveShadows = false;

            Transform packRoot = sequence.m_CardPackMesh.transform;
            if (packRoot.parent != null)
            {
                packRoot = packRoot.parent;
            }

            Renderer[] packRenderers = packRoot.GetComponentsInChildren<Renderer>(includeInactive: true);
            for (int i = 0; i < packRenderers.Length; i++)
            {
                packRenderers[i].enabled = false;
                packRenderers[i].shadowCastingMode = ShadowCastingMode.Off;
            }
        }
    }

    public static void SyncAllPackOpeningPresentations(CardOpeningSequence sequence)
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
            if (cardUi == null || cardData == null || cardData.expansionType != ECardExpansionType.Tetramon)
            {
                continue;
            }

            TetramonOverlay0703Patches.ConfigurePackOpeningCardPresentation(cardUi, card3d);

            // Paint the Pokemon face art only on cards rotated face-up toward the camera; face-down cards show
            // the blue back instead.
            if (PackOpeningState.IsCardFrontTowardCamera(card3d))
            {
                TetramonOverlay0703Patches.SetCardUI_ApplyTetramonOverlay(cardUi, cardData, forceFrontOverlay: true);
            }
        }
    }

    public static void SyncAllFanRowPresentations(CardOpeningSequence sequence)
    {
        if (sequence.m_Card3dUIList == null)
        {
            return;
        }

        for (int i = 0; i < sequence.m_Card3dUIList.Count; i++)
        {
            Card3dUIGroup? card3d = sequence.m_Card3dUIList[i];
            if (card3d == null || !card3d.gameObject.activeSelf)
            {
                continue;
            }

            CardUI? cardUi = card3d.m_CardUI;
            CardData? cardData = cardUi?.GetCardData();
            if (cardUi == null || cardData == null || cardData.expansionType != ECardExpansionType.Tetramon)
            {
                continue;
            }

            TetramonOverlay0703Patches.ApplyPackOpeningFanRowPresentation(cardUi, card3d);
            TetramonOverlay0703Patches.SetCardUI_ApplyTetramonOverlay(cardUi, cardData, forceFrontOverlay: true);
        }
    }
}
