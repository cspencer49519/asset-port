using System;
using System.Collections.Generic;
using System.Reflection;
using TCGShopExpansionMod.Handlers;
using UnityEngine;
using UnityEngine.UI;

namespace TCGShopExpansionMod0703Patch.Patches;

/// <summary>
/// Game 0.70.3 removed CardUI.m_MonsterImage. Pokemon/Tetramon art comes from ArtExpander cardart.assets.
/// </summary>
internal static class TetramonOverlay0703Patches
{
    private const float FullCardMinHeight = 420f;
    private const float FullCardMinWidth = 280f;
    private const float PackBackMeshOverscan = 1f;

    private static readonly string[] CardTextFieldNames =
    {
        "m_MonsterNameText",
        "m_DescriptionText",
        "m_ArtistText",
        "m_NumberText",
        "m_RarityText",
        "m_Stat1Text",
        "m_Stat2Text",
        "m_Stat3Text",
        "m_Stat4Text",
        "m_FirstEditionText",
        "m_EvoPreviousStageNameText"
    };

    private static readonly (string TextProperty, string EnabledProperty, string FieldName, bool ForceOnFallback)[] ConfigTextBindings =
    {
        ("Name", "NameEnabled", "m_MonsterNameText", true),
        ("Description", "DescriptionEnabled", "m_DescriptionText", true),
        ("ArtistText", "ArtistTextEnabled", "m_ArtistText", false),
        ("Number", "NumberEnabled", "m_NumberText", false),
        ("Rarity", "RarityEnabled", "m_RarityText", false),
        ("Stat1", "Stat1Enabled", "m_Stat1Text", false),
        ("Stat2", "Stat2Enabled", "m_Stat2Text", false),
        ("Stat3", "Stat3Enabled", "m_Stat3Text", false),
        ("Stat4", "Stat4Enabled", "m_Stat4Text", false),
        ("EditionText", "EditionTextEnabled", "m_FirstEditionText", false)
    };

    private static bool LoggedFirstFullCard;
    private static bool LoggedFirstCenterArt;
    private static bool LoggedMissingArt;

    /// <summary>
    /// Skip ExpansionMod HandleCards on 0.70.3. That method always reads removed CardUI fields
    /// (m_GhostCard for Ghost, m_FullArtCard for FullArt, m_MonsterImage for config-driven cards).
    /// Tetramon presentation is applied by SetCardUI_ApplyTetramonOverlay instead.
    /// </summary>
    public static bool SkipMainPostfixForTetramon_Prefix(CardUI __instance, CardData cardData)
    {
        return false;
    }

    /// <summary>
    /// SetCardExtrasImages is skipped on 0.70.3; assign the Pokemon CardBack sprite ExpansionMod would have applied.
    /// </summary>
    public static void ApplyTetramonCardBackFromCache(CardUI cardUi)
    {
        if (cardUi.m_CardBackImage == null)
        {
            return;
        }

        Sprite? cardBack = CardExtrasCacheAccess.TryGetUiCardBackSprite();
        if (cardBack == null)
        {
            return;
        }

        cardUi.m_CardBackImage.sprite = cardBack;
    }

    /// <summary>
    /// Non-Tetramon SetCardUI (Destiny/Trainer/Ghost/etc.): same ArtExpander full-card overlay
    /// as album for shelf, trade, and pack — album-only overlay left HO foil scrambling world cards.
    /// </summary>
    public static void ApplyNonTetramonPresentation(CardUI cardUi, CardData cardData)
    {
        if (cardUi == null || cardData == null)
        {
            return;
        }

        ApplyNonTetramonFacePresentation(cardUi, cardData);
    }

    /// <summary>
    /// Album binder slots reuse CardUI when F switches expansion. Disable leftover Tetramon
    /// overlay and restore chrome so Destiny/Ghost ArtExpander art is visible again.
    /// </summary>
    public static void ClearStaleOverlayAfterExpansionSwitch(CardUI cardUi)
    {
        if (cardUi == null)
        {
            return;
        }

        CardData? cardData = cardUi.GetCardData();
        if (cardData != null)
        {
            ApplyNonTetramonFacePresentation(cardUi, cardData);
            return;
        }

        ClearStaleOverlayChromeOnly(cardUi);
        EnsureReadableWithoutFullCardOverlay(cardUi);
    }

    private static void ApplyNonTetramonFacePresentation(CardUI cardUi, CardData cardData)
    {
        // Graded: paint ArtExpander art onto m_CardFrontImage (scaled into the slab by ShowGradedCardCase).
        if (cardData.cardGrade > 0)
        {
            ApplyGradedNonTetramonFace(cardUi, cardData);
            FinalizeNonTetramonWorldPresentation(cardUi, cardData);
            return;
        }

        Sprite? cardArt = ArtExpanderBridge.LoadCardArt(cardData);
        bool fromBridge = cardArt != null;
        if (cardArt == null)
        {
            cardArt = ResolveCardArt(cardUi, cardData, out fromBridge);
        }

        // Same rule as Pokemon: any ArtExpander sprite (or full-card-sized art) covers the face.
        if (cardArt != null && (fromBridge || LooksLikeFullCard(cardArt)))
        {
            ApplyFullCardOverlay(cardUi, cardArt);
            EnsureAlbumArtAndFoilLayering(cardUi);
            FinalizeNonTetramonWorldPresentation(cardUi, cardData);
            return;
        }

        ClearStaleOverlayChromeOnly(cardUi);
        StripAlbumHoFoilMaterials(cardUi);
        EnsureReadableWithoutFullCardOverlay(cardUi);
        FinalizeNonTetramonWorldPresentation(cardUi, cardData);
    }

    /// <summary>
    /// Shelf Destiny/Trainer must get an opaque expansion card back — otherwise the front overlay
    /// shows through from behind (front on both sides).
    /// </summary>
    private static void FinalizeNonTetramonWorldPresentation(CardUI cardUi, CardData cardData)
    {
        if (cardUi == null || cardData == null)
        {
            return;
        }

        if (PackOpeningState.IsPackOpeningInProgress() || PackOpeningState.IsFanRowVisible())
        {
            return;
        }

        // Graded Destiny/Ghost: FlatScreen re-enables CardFront at full binder scale and undoes
        // GradedCardFrontScaling — that is the "card peeking behind empty slab" loop.
        if (cardData.cardGrade > 0)
        {
            return;
        }

        if (CardUiDisplayContext.IsBinderAlbumCard(cardUi)
            || CardUiDisplayContext.IsFlatAlbumOrBinderCard(cardUi))
        {
            ApplyFlatScreenCardPresentation(cardUi);
            return;
        }

        if (CardUiDisplayContext.ShouldUseRotatableWorldCardBack(cardUi))
        {
            ConfigureCard3dForFrontDisplay(cardUi);
            return;
        }

        ApplyFlatScreenCardPresentation(cardUi);
    }

    /// <summary>
    /// Graded Destiny/Trainer/Ghost album slabs are 3D (Card3dUIGroup.m_GradedCardGrp).
    /// SetSimplifyCardDistanceCull(true) disables m_GradedCardCullGrp — that is the face volume
    /// inside the plastic, so the window looks empty while the 3D PSA label still shows CardBack
    /// (TextureReplacer). Fix: keep CullGrp on, paint ArtExpander art on CardFront at
    /// GradedCardFrontScaling, clear CardBack/Scratch on 3D slab renderers, clear UI scratch slot.
    /// </summary>
    private static int _gradedNonTetramonApplyDepth;
    private static bool _loggedGradedTextureSlot;
    private static bool _loggedGradedNonTetramonFace;

    private static void ApplyGradedNonTetramonFace(CardUI cardUi, CardData cardData, bool albumSimplified = false)
    {
        if (cardData.expansionType == ECardExpansionType.Tetramon)
        {
            ApplyGradedCardPresentation(cardUi, cardData);
            return;
        }

        if (_gradedNonTetramonApplyDepth > 0)
        {
            return;
        }

        _gradedNonTetramonApplyDepth++;
        try
        {
            ApplyGradedNonTetramonFaceCore(cardUi, cardData, albumSimplified);
        }
        finally
        {
            _gradedNonTetramonApplyDepth--;
        }
    }

    private static void ApplyGradedNonTetramonFaceCore(CardUI cardUi, CardData cardData, bool albumSimplified)
    {
        Sprite? cardArt = ArtExpanderBridge.LoadCardArt(cardData);
        bool fromBridge = cardArt != null;
        if (cardArt == null)
        {
            cardArt = ResolveCardArt(cardUi, cardData, out fromBridge);
        }

        HideDuplicateAlbumFoilHosts(cardUi);
        StripAlbumHoFoilMaterials(cardUi);
        DisableOverlayImage(cardUi);
        HideGradedCardBackFaces(cardUi);
        DisableGradedHeaderTextureSlot(cardUi);
        RestoreGraded3dSlabFaceVisibility(cardUi);

        if (cardArt == null || !(fromBridge || LooksLikeFullCard(cardArt)))
        {
            ApplyGradedCardPresentation(cardUi, cardData);
            HideGradedCardBackFaces(cardUi);
            DisableGradedHeaderTextureSlot(cardUi);
            HideOccludingGradedSlabMeshes(cardUi);
            EnsureGradeLabelTextsVisible(cardUi);
            return;
        }

        if (cardUi.m_GradedCardCaseGrp == null)
        {
            ApplyGradedCardPresentation(cardUi, cardData);
            HideGradedCardBackFaces(cardUi);
            DisableGradedHeaderTextureSlot(cardUi);
            return;
        }

        EnsureCardShellVisible(cardUi);

        if (!albumSimplified)
        {
            try
            {
                cardUi.m_Show2DGradedCase = true;
                cardUi.ShowGradedCardCase(isShow: true);
            }
            catch
            {
                // Older CardUI without graded helpers.
            }
        }

        // 3D Base/Top are translucent empty or opaque covers for Destiny. Drive the slab from UI.
        HideOccludingGradedSlabMeshes(cardUi);
        BlankGraded3dSlabCardBackMaterials(cardUi);
        HideGradedCardBackFaces(cardUi);
        DisableGradedHeaderTextureSlot(cardUi);
        ApplyGradedCaseFaceArt(cardUi, cardArt);
        HideOversizedCardFrontBehindSlab(cardUi);
        HideDuplicateAlbumFoilHosts(cardUi);
        StripAlbumHoFoilMaterials(cardUi);
        EnsureGradeLabelTextsVisible(cardUi);
        DumpGradedSlabHierarchyOnce(cardUi);

        cardUi.m_GradedCardCaseGrp.SetActive(true);
        cardUi.m_GradedCardCaseGrp.transform.SetAsLastSibling();
        AlbumHoFoilRepairBehaviour.EnsureOn(cardUi);

        if (!_loggedGradedNonTetramonFace)
        {
            _loggedGradedNonTetramonFace = true;
            Plugin.Log.LogInfo(
                $"Graded non-Tetramon: UI GradedFace slab {cardArt.name} "
                + $"({cardArt.rect.width}x{cardArt.rect.height}), expansion={cardData.expansionType}, "
                + $"grade={cardData.cardGrade}, simplified={albumSimplified}.");
        }
    }

    /// <summary>
    /// Keep CardUIAnimGrp on. Hard-hide 3D slab plastics — Destiny Base/Top leave empty
    /// translucent shells; GradedFace0703 + UI chrome supply the visible slab.
    /// </summary>
    private static void RestoreGraded3dSlabFaceVisibility(CardUI cardUi)
    {
        Card3dUIGroup? card3d = CardUiDisplayContext.ResolveCard3dGroup(cardUi);
        if (card3d == null)
        {
            return;
        }

        if (card3d.m_CardUIAnimGrp != null)
        {
            card3d.m_CardUIAnimGrp.gameObject.SetActive(true);
        }

        card3d.m_IgnoreCulling = true;
        try
        {
            card3d.SetAlwaysCulling(alwaysCulling: false, setVisibilityInstant: true);
        }
        catch
        {
            // Older signature.
        }

        if (card3d.m_GradedCardGrp != null)
        {
            card3d.m_GradedCardGrp.SetActive(true);
        }

        if (card3d.m_GradedCaseCullCardFrontMeshBlocker != null)
        {
            card3d.m_GradedCaseCullCardFrontMeshBlocker.SetActive(false);
        }

        if (card3d.m_GradedCaseCullCardBackMeshBlocker != null)
        {
            card3d.m_GradedCaseCullCardBackMeshBlocker.SetActive(false);
        }

        HideOccludingGradedSlabMeshes(cardUi);

        if (card3d.m_GradedCardBrightnessControl != null)
        {
            card3d.m_GradedCardBrightnessControl.enabled = false;
            if (card3d.m_GradedCardBrightnessControl.gameObject != null)
            {
                card3d.m_GradedCardBrightnessControl.gameObject.SetActive(false);
            }
        }
    }

    private static void HideOccludingGradedSlabMeshes(CardUI cardUi)
    {
        Card3dUIGroup? card3d = CardUiDisplayContext.ResolveCard3dGroup(cardUi);
        if (card3d == null)
        {
            return;
        }

        if (card3d.m_GradedCardCullGrp != null)
        {
            card3d.m_GradedCardCullGrp.SetActive(false);
            ForceDisableSlabRenderersUnder(card3d.m_GradedCardCullGrp.transform);
        }

        if (card3d.m_SlabTopLayerMesh != null)
        {
            ForceDisableSlabObject(card3d.m_SlabTopLayerMesh);
        }

        if (card3d.m_GradedCardGrp != null)
        {
            ForceDisableSlabRenderersUnder(card3d.m_GradedCardGrp.transform);
            DisableNamedChildren(card3d.m_GradedCardGrp.transform, "Slab_BaseMesh");
            DisableNamedChildren(card3d.m_GradedCardGrp.transform, "Slab_TopLayerMesh");
            DisableNamedChildren(card3d.m_GradedCardGrp.transform, "CardBackMeshBlocker");
            DisableNamedChildren(card3d.m_GradedCardGrp.transform, "CardFrontMeshBlocker");
        }
    }

    private static void ForceDisableSlabRenderersUnder(Transform root)
    {
        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(includeInactive: true);
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer? renderer = renderers[i];
            if (renderer == null || renderer.gameObject == null)
            {
                continue;
            }

            string objectName = renderer.gameObject.name ?? string.Empty;
            bool slabPlastic = objectName.IndexOf("Slab_", StringComparison.OrdinalIgnoreCase) >= 0
                || objectName.IndexOf("MeshBlocker", StringComparison.OrdinalIgnoreCase) >= 0
                || objectName.IndexOf("TopLayer", StringComparison.OrdinalIgnoreCase) >= 0
                || objectName.IndexOf("BaseMesh", StringComparison.OrdinalIgnoreCase) >= 0;
            if (!slabPlastic)
            {
                continue;
            }

            ForceDisableSlabObject(renderer.gameObject);
        }
    }

    private static void DestroyUiOnlyGradedSlabArtifacts(CardUI cardUi)
    {
        // Kept for callers that tear down experimental UI; 1.1.075 recreates chrome via EnsureGradedCaseChrome.
        _ = cardUi;
    }

    private static void ReparentGradeTmpToCase(object? tmp, Transform caseRoot)
    {
        if (tmp is not Behaviour behaviour || behaviour.transform == null)
        {
            return;
        }

        if (behaviour.transform.parent != caseRoot)
        {
            behaviour.transform.SetParent(caseRoot, false);
        }
    }

    private static void RestoreSlabObject(GameObject? go)
    {
        if (go == null)
        {
            return;
        }

        if (go.transform.localScale == Vector3.zero)
        {
            go.transform.localScale = Vector3.one;
        }

        go.SetActive(true);
        Renderer[] renderers = go.GetComponentsInChildren<Renderer>(includeInactive: true);
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] != null)
            {
                renderers[i].enabled = true;
            }
        }
    }

    private static void ForceDisableSlabObject(GameObject go)
    {
        go.SetActive(false);
        go.transform.localScale = Vector3.zero;
        Renderer[] renderers = go.GetComponentsInChildren<Renderer>(includeInactive: true);
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] != null)
            {
                renderers[i].enabled = false;
            }
        }
    }

    private static void DisableNamedChildren(Transform? root, string childName)
    {
        if (root == null)
        {
            return;
        }

        Transform[] all = root.GetComponentsInChildren<Transform>(includeInactive: true);
        for (int i = 0; i < all.Length; i++)
        {
            Transform? t = all[i];
            if (t != null && string.Equals(t.name, childName, StringComparison.OrdinalIgnoreCase))
            {
                ForceDisableSlabObject(t.gameObject);
            }
        }
    }

    private static void EnableNamedChildren(Transform? root, string childName)
    {
        if (root == null)
        {
            return;
        }

        Transform[] all = root.GetComponentsInChildren<Transform>(includeInactive: true);
        for (int i = 0; i < all.Length; i++)
        {
            Transform? t = all[i];
            if (t == null || !string.Equals(t.name, childName, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (t.localScale == Vector3.zero)
            {
                t.localScale = Vector3.one;
            }

            t.gameObject.SetActive(true);
            Renderer[] renderers = t.GetComponentsInChildren<Renderer>(includeInactive: true);
            for (int r = 0; r < renderers.Length; r++)
            {
                if (renderers[r] != null)
                {
                    renderers[r].enabled = true;
                }
            }
        }
    }

    private static void HideOversizedCardFrontBehindSlab(CardUI cardUi)
    {
        SetCardFrontCanvasActive(cardUi, active: false);
        if (cardUi.m_CardFront != null)
        {
            cardUi.m_CardFront.SetActive(false);
            cardUi.m_CardFront.transform.localScale = Vector3.zero;
        }
    }

    private static Image GetOrCreateGradedCaseFaceImage(CardUI cardUi)
    {
        Transform caseRoot = cardUi.m_GradedCardCaseGrp.transform;
        Transform? existing = caseRoot.Find(GradedCaseFaceObjectName);
        if (existing != null && existing.TryGetComponent(out Image cached))
        {
            return cached;
        }

        GameObject faceObject = new(GradedCaseFaceObjectName);
        faceObject.transform.SetParent(caseRoot, false);
        Image image = faceObject.AddComponent<Image>();
        image.raycastTarget = false;
        image.maskable = true;
        return image;
    }

    private const string GradedCaseChromeObjectName = "GradedCaseChrome0703";
    private const string GradedCaseHeaderBarObjectName = "GradedCaseHeaderBar0703";
    private const float GradedCardAspect = 2.5f / 3.5f;

    private static void ApplyGradedCaseFaceArt(CardUI cardUi, Sprite cardArt)
    {
        EnsureGradedCaseChrome(cardUi);

        Image caseFace = GetOrCreateGradedCaseFaceImage(cardUi);
        caseFace.sprite = cardArt;
        caseFace.type = Image.Type.Simple;
        caseFace.preserveAspect = false;
        caseFace.color = Color.white;
        caseFace.raycastTarget = false;
        caseFace.maskable = true;
        caseFace.material = null;
        caseFace.enabled = true;
        caseFace.gameObject.SetActive(true);

        RectTransform? caseRect = cardUi.m_GradedCardCaseGrp.transform as RectTransform;
        RectTransform faceRect = caseFace.rectTransform;
        faceRect.SetParent(cardUi.m_GradedCardCaseGrp.transform, false);
        faceRect.localScale = Vector3.one;
        faceRect.localRotation = Quaternion.identity;
        LayoutGradedFaceInCaseWindow(faceRect, caseRect);
        // chrome(0) header(1) face(2) — TMP last via EnsureGradeLabelTextsVisible
        faceRect.SetSiblingIndex(2);

        if (!_loggedGradedFaceLayout)
        {
            _loggedGradedFaceLayout = true;
            Plugin.Log.LogInfo(
                $"GradedFace UI-only layout pos={faceRect.anchoredPosition} sizeDelta={faceRect.sizeDelta} "
                + $"caseSize={(caseRect != null ? caseRect.rect.size.ToString() : "n/a")}");
        }
    }

    private static void EnsureGradedCaseChrome(CardUI cardUi)
    {
        Transform caseRoot = cardUi.m_GradedCardCaseGrp.transform;
        Sprite white = SolidWhiteUiSprite.Get();

        Image chrome = GetOrCreateCaseImage(caseRoot, GradedCaseChromeObjectName);
        chrome.sprite = white;
        chrome.type = Image.Type.Simple;
        chrome.color = new Color(0.08f, 0.08f, 0.09f, 1f);
        chrome.material = null; // default UI queue ~3000; TMP repaired to 3100 draws above
        chrome.raycastTarget = false;
        chrome.enabled = true;
        chrome.gameObject.SetActive(true);
        RectTransform chromeRect = chrome.rectTransform;
        chromeRect.anchorMin = Vector2.zero;
        chromeRect.anchorMax = Vector2.one;
        chromeRect.offsetMin = Vector2.zero;
        chromeRect.offsetMax = Vector2.zero;
        chromeRect.localScale = Vector3.one;
        chromeRect.SetAsFirstSibling();

        Image headerBar = GetOrCreateCaseImage(caseRoot, GradedCaseHeaderBarObjectName);
        headerBar.sprite = white;
        headerBar.type = Image.Type.Simple;
        headerBar.color = new Color(0.02f, 0.02f, 0.03f, 1f);
        headerBar.material = null;
        headerBar.raycastTarget = false;
        headerBar.enabled = true;
        headerBar.gameObject.SetActive(true);
        RectTransform headerRect = headerBar.rectTransform;
        headerRect.anchorMin = new Vector2(0.04f, 0.78f);
        headerRect.anchorMax = new Vector2(0.96f, 0.98f);
        headerRect.offsetMin = Vector2.zero;
        headerRect.offsetMax = Vector2.zero;
        headerRect.localScale = Vector3.one;
        headerRect.SetSiblingIndex(1);

        // Remove nested Canvas from 1.1.073 — it broke chrome/face drawing.
        Canvas? nested = headerBar.GetComponent<Canvas>();
        if (nested != null)
        {
            UnityEngine.Object.Destroy(nested);
        }

        UnityEngine.UI.GraphicRaycaster? raycaster = headerBar.GetComponent<UnityEngine.UI.GraphicRaycaster>();
        if (raycaster != null)
        {
            UnityEngine.Object.Destroy(raycaster);
        }

        // Nudge case slightly toward camera so UI draws over any residual 3D mesh.
        if (caseRoot is RectTransform caseRt)
        {
            Vector3 lp = caseRt.localPosition;
            if (Mathf.Abs(lp.z) < 0.001f)
            {
                caseRt.localPosition = new Vector3(lp.x, lp.y, -2f);
            }
        }
    }

    private static Image GetOrCreateCaseImage(Transform caseRoot, string objectName)
    {
        Transform? existing = caseRoot.Find(objectName);
        if (existing != null && existing.TryGetComponent(out Image cached))
        {
            return cached;
        }

        GameObject go = new(objectName);
        go.transform.SetParent(caseRoot, false);
        Image image = go.AddComponent<Image>();
        image.raycastTarget = false;
        return image;
    }

    private static void LayoutGradedFaceInCaseWindow(RectTransform faceRect, RectTransform? caseRect)
    {
        _ = caseRect;
        // Fill nearly the whole case under a thin header band.
        faceRect.anchorMin = new Vector2(0.05f, 0.03f);
        faceRect.anchorMax = new Vector2(0.95f, 0.76f);
        faceRect.pivot = new Vector2(0.5f, 0.5f);
        faceRect.offsetMin = Vector2.zero;
        faceRect.offsetMax = Vector2.zero;
        faceRect.anchoredPosition = Vector2.zero;
        faceRect.sizeDelta = Vector2.zero;
    }

    private static void DestroyFakeGradedCaseChrome(CardUI cardUi)
    {
        if (cardUi.m_GradedCardCaseGrp == null)
        {
            return;
        }

        Transform caseRoot = cardUi.m_GradedCardCaseGrp.transform;
        DestroyNamedChild(caseRoot, GradedCaseChromeObjectName);
        DestroyNamedChild(caseRoot, GradedCaseHeaderBarObjectName);
    }

    private static void DestroyNamedChild(Transform parent, string childName)
    {
        Transform? existing = parent.Find(childName);
        if (existing != null)
        {
            UnityEngine.Object.Destroy(existing.gameObject);
        }
    }

    private static bool _loggedGradedFaceLayout;

    /// <summary>Unused — kept so older repair call sites compile if referenced.</summary>
    private static void ResetGradedSlabPlasticMeshes(CardUI cardUi)
    {
        HideOccludingGradedSlabMeshes(cardUi);
    }

    private static void ApplyGradedArtTo3dFaceMeshes(CardUI cardUi, Sprite cardArt)
    {
        _ = cardUi;
        _ = cardArt;
    }

    private static bool _loggedGradedSlabDump;

    private static void DumpGradedSlabHierarchyOnce(CardUI cardUi)
    {
        if (_loggedGradedSlabDump)
        {
            return;
        }

        _loggedGradedSlabDump = true;
        Card3dUIGroup? card3d = CardUiDisplayContext.ResolveCard3dGroup(cardUi);
        Plugin.Log.LogWarning("=== Graded slab hierarchy dump (once) ===");
        DumpImagesUnder("CardUI", cardUi.transform);
        if (card3d != null)
        {
            DumpImagesUnder("Card3d", card3d.transform);
            DumpRenderersUnder("Card3d", card3d.transform);
            Plugin.Log.LogWarning(
                $"card3d flags: gradedGrp={card3d.m_GradedCardGrp?.activeSelf} "
                + $"cullGrp={card3d.m_GradedCardCullGrp?.activeSelf} "
                + $"slabTop={card3d.m_SlabTopLayerMesh?.activeSelf} "
                + $"animGrp={card3d.m_CardUIAnimGrp?.gameObject.activeSelf} "
                + $"frontBlocker={card3d.m_GradedCaseCullCardFrontMeshBlocker?.activeSelf} "
                + $"frontMeshPos={card3d.m_CardFrontMeshPos?.activeSelf}");
        }
        else
        {
            Plugin.Log.LogWarning("card3d=null");
        }

        if (cardUi.m_CardFront != null)
        {
            Transform ft = cardUi.m_CardFront.transform;
            Plugin.Log.LogWarning(
                $"CardFront active={cardUi.m_CardFront.activeSelf} scale={ft.localScale} "
                + $"pos={ft.localPosition} sprite={cardUi.m_CardFrontImage?.sprite?.name}");
        }
    }

    private static void DumpImagesUnder(string label, Transform root)
    {
        Image[] images = root.GetComponentsInChildren<Image>(includeInactive: true);
        int logged = 0;
        for (int i = 0; i < images.Length && logged < 40; i++)
        {
            Image? image = images[i];
            if (image == null)
            {
                continue;
            }

            string spriteName = image.sprite != null ? image.sprite.name : "null";
            RectTransform rt = image.rectTransform;
            Plugin.Log.LogWarning(
                $"{label} Image[{logged}] path={GetTransformPath(image.transform)} "
                + $"sprite={spriteName} enabled={image.enabled} active={image.gameObject.activeInHierarchy} "
                + $"size={rt.sizeDelta} scale={rt.localScale}");
            logged++;
        }
    }

    private static void DumpRenderersUnder(string label, Transform root)
    {
        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(includeInactive: true);
        int logged = 0;
        for (int i = 0; i < renderers.Length && logged < 40; i++)
        {
            Renderer? renderer = renderers[i];
            if (renderer == null)
            {
                continue;
            }

            Material? mat = renderer.sharedMaterial;
            Texture? tex = mat != null
                ? (mat.HasProperty("_MainTex") ? mat.GetTexture("_MainTex") : mat.mainTexture)
                : null;
            Plugin.Log.LogWarning(
                $"{label} Renderer[{logged}] path={GetTransformPath(renderer.transform)} "
                + $"mat={mat?.name} tex={tex?.name} enabled={renderer.enabled} "
                + $"active={renderer.gameObject.activeInHierarchy}");
            logged++;
        }
    }

    private static string GetTransformPath(Transform t)
    {
        string path = t.name;
        Transform? parent = t.parent;
        int depth = 0;
        while (parent != null && depth < 6)
        {
            path = parent.name + "/" + path;
            parent = parent.parent;
            depth++;
        }

        return path;
    }

    /// <summary>
    /// 3D slab PSA label meshes often sample CardBack after TextureReplacer. Blank those.
    /// </summary>
    private static void BlankGraded3dSlabCardBackMaterials(CardUI cardUi)
    {
        Card3dUIGroup? card3d = CardUiDisplayContext.ResolveCard3dGroup(cardUi);
        if (card3d == null)
        {
            return;
        }

        BlankCardBackScratchRenderersUnder(card3d.m_GradedCardGrp != null ? card3d.m_GradedCardGrp.transform : null);
        BlankCardBackScratchRenderersUnder(card3d.m_GradedCardCullGrp != null ? card3d.m_GradedCardCullGrp.transform : null);
        BlankCardBackScratchRenderersUnder(card3d.m_SlabTopLayerMesh != null ? card3d.m_SlabTopLayerMesh.transform : null);
        BlankCardBackScratchRenderersUnder(card3d.transform);
    }

    private static void BlankCardBackScratchRenderersUnder(Transform? root)
    {
        if (root == null)
        {
            return;
        }

        Texture2D clearTex = GradedScratchClearSprite.ClearTexture;
        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(includeInactive: true);
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer? renderer = renderers[i];
            if (renderer == null)
            {
                continue;
            }

            string objectName = renderer.gameObject != null ? renderer.gameObject.name : string.Empty;
            Material[] materials = renderer.materials;
            bool changed = false;
            for (int m = 0; m < materials.Length; m++)
            {
                Material? mat = materials[m];
                if (mat == null)
                {
                    continue;
                }

                string matName = mat.name ?? string.Empty;
                Texture? mainTex = mat.HasProperty("_MainTex") ? mat.GetTexture("_MainTex") : mat.mainTexture;
                string texName = mainTex != null ? mainTex.name ?? string.Empty : string.Empty;

                // Never wipe Slab_TopLayer / Slab_Base materials — that turns the chrome opaque
                // grey and hides the card window. CardBack/Scratch only on non-slab objects.
                bool isSlabPlastic = objectName.IndexOf("Slab_", StringComparison.OrdinalIgnoreCase) >= 0
                    || objectName.IndexOf("TopLayer", StringComparison.OrdinalIgnoreCase) >= 0
                    || objectName.IndexOf("BaseMesh", StringComparison.OrdinalIgnoreCase) >= 0;
                if (isSlabPlastic)
                {
                    continue;
                }

                bool scratchOrBack = matName.IndexOf("Scratch", StringComparison.OrdinalIgnoreCase) >= 0
                    || matName.IndexOf("GradeCard", StringComparison.OrdinalIgnoreCase) >= 0
                    || matName.IndexOf("CardBack", StringComparison.OrdinalIgnoreCase) >= 0
                    || texName.IndexOf("Scratch", StringComparison.OrdinalIgnoreCase) >= 0
                    || texName.IndexOf("GradeCard", StringComparison.OrdinalIgnoreCase) >= 0
                    || texName.IndexOf("CardBack", StringComparison.OrdinalIgnoreCase) >= 0
                    || objectName.IndexOf("Scratch", StringComparison.OrdinalIgnoreCase) >= 0
                    || objectName.IndexOf("CardBack", StringComparison.OrdinalIgnoreCase) >= 0;

                if (!scratchOrBack)
                {
                    continue;
                }

                if (objectName.IndexOf("Front", StringComparison.OrdinalIgnoreCase) >= 0
                    || objectName.IndexOf("Face", StringComparison.OrdinalIgnoreCase) >= 0
                    || objectName.IndexOf("CardArt", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    continue;
                }

                if (mat.HasProperty("_MainTex"))
                {
                    mat.SetTexture("_MainTex", clearTex);
                }

                if (mat.HasProperty("_BaseMap"))
                {
                    mat.SetTexture("_BaseMap", clearTex);
                }

                mat.mainTexture = clearTex;
                if (mat.HasProperty("_Color"))
                {
                    mat.SetColor("_Color", Color.clear);
                }

                changed = true;
            }

            if (changed)
            {
                renderer.materials = materials;
            }

            var block = new MaterialPropertyBlock();
            renderer.GetPropertyBlock(block);
            if (block.HasTexture("_MainTex"))
            {
                Texture? blockTex = block.GetTexture("_MainTex");
                string blockName = blockTex != null ? blockTex.name ?? string.Empty : string.Empty;
                if (blockName.IndexOf("CardBack", StringComparison.OrdinalIgnoreCase) >= 0
                    || blockName.IndexOf("Scratch", StringComparison.OrdinalIgnoreCase) >= 0
                    || blockName.IndexOf("GradeCard", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    block.SetTexture("_MainTex", clearTex);
                    renderer.SetPropertyBlock(block);
                }
            }
        }
    }

    private static void FitCardFrontIntoGradedSlabWindow(CardUI cardUi)
    {
        if (cardUi.m_CardFront == null || cardUi.m_GradedCardFrontScaling == null)
        {
            return;
        }

        Transform front = cardUi.m_CardFront.transform;
        Transform scaling = cardUi.m_GradedCardFrontScaling;
        cardUi.m_CardFront.SetActive(true);
        front.localPosition = scaling.localPosition;
        front.localScale = scaling.localScale;
        front.localRotation = scaling.localRotation;
    }

    private static void PaintGradedCardFrontArt(CardUI cardUi, Sprite cardArt)
    {
        // Do NOT call HideVanillaChromeWhenOverlayShown — it disables m_CardFrontImage.
        HideCenterFrameArt(cardUi);
        HideDuplicateTextWhenOverlayShown(cardUi);
        HideDuplicateAlbumFoilHosts(cardUi);
        StripAlbumHoFoilMaterials(cardUi);
        SetImageEnabled(cardUi.m_BrightnessControl, false);
        SetImageEnabled(cardUi.m_CardBGImage, false);
        SetImageEnabled(cardUi.m_CardBorderImage, false);
        SetImageEnabled(cardUi.m_CardFullBGImage, false);
        SetImageEnabled(cardUi.m_CardFullTransparentLayerBGImage, false);
        SetImageEnabled(cardUi.m_RarityImage, false);
        SetImageEnabled(cardUi.m_FadeBarTopImage, false);
        SetImageEnabled(cardUi.m_FadeBarBtmImage, false);
        SetImageEnabled(cardUi.m_StatImage, false);

        ApplySpriteToCardFrontImage(cardUi.m_CardFrontImage, cardArt);
        ResetImageToDefaultUiMaterial(cardUi.m_CardFrontImage);
        // Top layer doubles / scrambles under HO — keep a single face image.
        SetImageEnabled(cardUi.m_CardFrontImageTopLayer, false);

        DisableGradedTextureMask(cardUi);
    }

    private static void DisableGradedTextureMask(CardUI cardUi)
    {
        if (cardUi.m_GradedCardTextureImage != null
            && cardUi.m_GradedCardTextureImage.transform.parent != null)
        {
            Transform mask = cardUi.m_GradedCardTextureImage.transform.parent;
            if (mask.name.IndexOf("GradedCardTextureMask", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                mask.gameObject.SetActive(false);
            }
        }

        Transform? front = cardUi.m_CardFront != null ? cardUi.m_CardFront.transform : null;
        if (front == null)
        {
            return;
        }

        Transform? maskByName = front.Find("GradedCardTextureMask");
        if (maskByName != null)
        {
            maskByName.gameObject.SetActive(false);
        }
    }

    private static void ApplySpriteToCardFrontImage(Image? image, Sprite cardArt)
    {
        if (image == null)
        {
            return;
        }

        image.sprite = cardArt;
        image.type = Image.Type.Simple;
        image.preserveAspect = false;
        image.color = Color.white;
        image.raycastTarget = false;
        image.maskable = true;
        image.material = null;
        image.enabled = true;
        if (image.gameObject != null)
        {
            image.gameObject.SetActive(true);
        }
    }

    private const string GradedCaseFaceObjectName = "GradedFace0703";

    private static void DisableLegacyGradedCaseFace(CardUI cardUi)
    {
        // No-op: GradedFace0703 + chrome are the Destiny/Ghost graded UI slab.
        _ = cardUi;
    }

    private static void DisableGradedHeaderTextureSlot(CardUI cardUi)
    {
        Sprite clearScratch = GradedScratchClearSprite.Get();

        if (cardUi.m_GradedCardTextureImage != null)
        {
            Image slot = cardUi.m_GradedCardTextureImage;
            if (!_loggedGradedTextureSlot)
            {
                _loggedGradedTextureSlot = true;
                Plugin.Log.LogInfo(
                    $"Hard-disabling graded header texture slot (was sprite={slot.sprite?.name ?? "null"}). "
                    + "UI scratch + 3D slab label CardBack cleared separately.");
            }

            slot.sprite = clearScratch;
            slot.overrideSprite = clearScratch;
            slot.material = null;
            slot.color = Color.clear;
            slot.enabled = false;
            if (slot.gameObject != null)
            {
                slot.gameObject.SetActive(false);
            }
        }

        if (cardUi.m_GradedCardCaseGrp == null)
        {
            return;
        }

        Transform caseRoot = cardUi.m_GradedCardCaseGrp.transform;
        Image[] images = caseRoot.GetComponentsInChildren<Image>(includeInactive: true);
        for (int i = 0; i < images.Length; i++)
        {
            Image? image = images[i];
            if (image == null)
            {
                continue;
            }

            string objectName = image.gameObject != null ? image.gameObject.name : string.Empty;
            if (objectName == GradedCaseFaceObjectName
                || objectName == GradedCaseChromeObjectName
                || objectName == GradedCaseHeaderBarObjectName)
            {
                continue;
            }

            string spriteName = image.sprite != null ? image.sprite.name ?? string.Empty : string.Empty;
            bool scratchOrBack = spriteName.IndexOf("Scratch", StringComparison.OrdinalIgnoreCase) >= 0
                || spriteName.IndexOf("GradeCard", StringComparison.OrdinalIgnoreCase) >= 0
                || spriteName.IndexOf("CardBack", StringComparison.OrdinalIgnoreCase) >= 0
                || spriteName.IndexOf("card_back", StringComparison.OrdinalIgnoreCase) >= 0
                || objectName.IndexOf("Scratch", StringComparison.OrdinalIgnoreCase) >= 0
                || objectName.IndexOf("GradedCardTexture", StringComparison.OrdinalIgnoreCase) >= 0;

            if (!scratchOrBack)
            {
                continue;
            }

            image.sprite = clearScratch;
            image.overrideSprite = clearScratch;
            image.material = null;
            image.color = Color.clear;
            image.enabled = false;
            if (image.gameObject != null)
            {
                image.gameObject.SetActive(false);
            }
        }

        BlankCardBackScratchRenderersUnder(caseRoot);
    }

    private static void EnsureGradeLabelTextsVisible(CardUI cardUi)
    {
        Card3dUIGroup? card3d = CardUiDisplayContext.ResolveCard3dGroup(cardUi);
        if (card3d != null)
        {
            DisableGradeTmp(card3d.m_GradeNumberText, cardUi.m_GradeNumberText);
            DisableGradeTmp(card3d.m_GradeDescriptionText, cardUi.m_GradeDescriptionText);
            DisableGradeTmp(card3d.m_GradeNameText, cardUi.m_GradeNameText);
            DisableGradeTmp(card3d.m_GradeExpansionRarityText, cardUi.m_GradeExpansionRarityText);
            DisableGradeTmp(card3d.m_GradeSerialText, cardUi.m_GradeSerialText);
        }

        RefreshGradeLabelContent(cardUi);
        // Parent into header Image (no nested Canvas — that killed chrome in 1.1.073).
        LayoutGradeLabelsOnSlabHeader(cardUi);

        SetTmpEnabled(cardUi.m_GradeNumberText, true);
        SetTmpEnabled(cardUi.m_GradeDescriptionText, true);
        SetTmpEnabled(cardUi.m_GradeNameText, true);
        SetTmpEnabled(cardUi.m_GradeExpansionRarityText, true);
        SetTmpEnabled(cardUi.m_GradeSerialText, true);

        RepairGradedGradeTmp(cardUi.m_GradeNumberText);
        RepairGradedGradeTmp(cardUi.m_GradeDescriptionText);
        RepairGradedGradeTmp(cardUi.m_GradeNameText);
        RepairGradedGradeTmp(cardUi.m_GradeExpansionRarityText);
        RepairGradedGradeTmp(cardUi.m_GradeSerialText);

        if (!_loggedGradeTmpPaths && cardUi.m_GradeNumberText is TMPro.TextMeshProUGUI gradeNumber)
        {
            _loggedGradeTmpPaths = true;
            Material? mat = gradeNumber.fontMaterial;
            Plugin.Log.LogWarning(
                $"Grade TMP text='{gradeNumber.text}' size={gradeNumber.rectTransform.rect.size} "
                + $"queue={mat?.renderQueue} font={gradeNumber.font?.name} "
                + $"path={GetTransformPath(gradeNumber.transform)}");
        }
    }

    private static void RepairGradedGradeTmp(object? tmp)
    {
        if (tmp is not TMPro.TextMeshProUGUI label || label.font == null)
        {
            return;
        }

        PhoneFontMaterialSnapshot.CaptureIfNeeded();
        Material hudMaterial = PhoneFontMaterialSnapshot.CreateHudLabelMaterial(label.font);
        hudMaterial.renderQueue = 3100;
        label.fontMaterial = hudMaterial;
        label.color = Color.white;
        label.faceColor = new Color32(255, 255, 255, 255);
        label.enabled = true;
        label.gameObject.SetActive(true);

        CanvasRenderer? canvasRenderer = label.canvasRenderer;
        if (canvasRenderer != null)
        {
            canvasRenderer.SetColor(Color.white);
            canvasRenderer.SetAlpha(1f);
            canvasRenderer.cull = false;
            canvasRenderer.cullTransparentMesh = false;
            canvasRenderer.materialCount = 1;
            canvasRenderer.SetMaterial(hudMaterial, 0);
            if (label.font.atlasTexture != null)
            {
                canvasRenderer.SetTexture(label.font.atlasTexture);
            }
        }

        label.SetAllDirty();
        if (label.gameObject.activeInHierarchy)
        {
            label.ForceMeshUpdate(true, false);
        }
    }

    private static void RefreshGradeLabelContent(CardUI cardUi)
    {
        CardData? cardData = cardUi.GetCardData();
        if (cardData == null || cardData.cardGrade <= 0)
        {
            return;
        }

        SetTmpText(cardUi.m_GradeNumberText, cardData.cardGrade.ToString());

        try
        {
            string gradeName = GameInstance.GetCardGradeString(cardData.cardGrade);
            SetTmpText(cardUi.m_GradeDescriptionText, gradeName);
        }
        catch
        {
            // Older GameInstance.
        }

        string monsterName = string.Empty;
        if (cardUi.m_MonsterNameText is TMPro.TMP_Text monsterTmp)
        {
            monsterName = monsterTmp.text ?? string.Empty;
        }

        if (string.IsNullOrEmpty(monsterName))
        {
            monsterName = cardData.monsterType.ToString();
        }

        SetTmpText(cardUi.m_GradeNameText, monsterName);

        try
        {
            string expansion = cardData.expansionType.ToString()
                + " "
                + CPlayerData.GetFullCardTypeName(cardData);
            SetTmpText(cardUi.m_GradeExpansionRarityText, expansion);
        }
        catch
        {
            SetTmpText(cardUi.m_GradeExpansionRarityText, cardData.expansionType.ToString());
        }
    }

    private static void SetTmpText(object? tmp, string text)
    {
        if (tmp is TMPro.TMP_Text tmpText)
        {
            tmpText.text = text ?? string.Empty;
            tmpText.ForceMeshUpdate(true, true);
        }
    }

    /// <summary>
    /// Place grade TMP inside the header bar so they paint above the dark chrome.
    /// </summary>
    private static void LayoutGradeLabelsOnSlabHeader(CardUI cardUi)
    {
        if (cardUi.m_GradedCardCaseGrp == null)
        {
            return;
        }

        Transform caseRoot = cardUi.m_GradedCardCaseGrp.transform;
        Transform headerRoot = caseRoot.Find(GradedCaseHeaderBarObjectName) ?? caseRoot;

        // Name left, grade number + description right — inside header bar local space.
        LayoutGradeTmpInCase(cardUi.m_GradeNameText, headerRoot, new Vector2(0.04f, 0.45f), new Vector2(0.62f, 0.95f), leftAligned: true);
        LayoutGradeTmpInCase(cardUi.m_GradeNumberText, headerRoot, new Vector2(0.68f, 0.50f), new Vector2(0.96f, 0.98f), leftAligned: false);
        LayoutGradeTmpInCase(cardUi.m_GradeDescriptionText, headerRoot, new Vector2(0.68f, 0.05f), new Vector2(0.96f, 0.48f), leftAligned: false);
        LayoutGradeTmpInCase(cardUi.m_GradeExpansionRarityText, headerRoot, new Vector2(0.04f, 0.05f), new Vector2(0.62f, 0.42f), leftAligned: true);
        LayoutGradeTmpInCase(cardUi.m_GradeSerialText, headerRoot, new Vector2(0.04f, 0.00f), new Vector2(0.45f, 0.20f), leftAligned: true);
    }

    private static void LayoutGradeTmpInCase(
        object? tmp,
        Transform caseRoot,
        Vector2 anchorMin,
        Vector2 anchorMax,
        bool leftAligned)
    {
        if (tmp is not Behaviour behaviour || behaviour.transform == null)
        {
            return;
        }

        Transform transform = behaviour.transform;
        transform.SetParent(caseRoot, false);
        transform.localScale = Vector3.one;
        transform.localRotation = Quaternion.identity;
        transform.SetAsLastSibling();

        if (transform is not RectTransform rect)
        {
            return;
        }

        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = Vector2.zero;

        if (tmp is TMPro.TMP_Text tmpText)
        {
            tmpText.enableAutoSizing = true;
            tmpText.fontSizeMin = 10f;
            tmpText.fontSizeMax = 64f;
            tmpText.overflowMode = TMPro.TextOverflowModes.Ellipsis;
            tmpText.alignment = leftAligned
                ? TMPro.TextAlignmentOptions.MidlineLeft
                : TMPro.TextAlignmentOptions.MidlineRight;
            tmpText.color = Color.white;
            tmpText.alpha = 1f;
            try
            {
                tmpText.ForceMeshUpdate(true, true);
            }
            catch
            {
                // TMP mesh update can fail before canvas is ready.
            }
        }
    }

    private static void DisableGradeTmp(object? candidate, object? keepIfSameAs)
    {
        if (candidate is not Behaviour behaviour)
        {
            return;
        }

        if (keepIfSameAs is Behaviour keep && ReferenceEquals(behaviour, keep))
        {
            return;
        }

        behaviour.enabled = false;
        if (behaviour.gameObject != null)
        {
            behaviour.gameObject.SetActive(false);
        }
    }

    private static bool _loggedGradeTmpPaths;

    private static void SetTmpEnabled(object? tmp, bool enabled)
    {
        if (tmp is not Behaviour behaviour)
        {
            return;
        }

        behaviour.enabled = enabled;
        if (behaviour.gameObject != null)
        {
            behaviour.gameObject.SetActive(enabled);
        }
    }

    public static void HideGradedCardBackFaces(CardUI cardUi)
    {
        SuppressFlatUiCardBack(cardUi);
        DisableGradedCaseBackAndBackBlocker(cardUi);

        Card3dUIGroup? card3d = CardUiDisplayContext.ResolveCard3dGroup(cardUi);
        if (card3d?.m_CardBackMesh != null)
        {
            card3d.m_CardBackMesh.SetActive(false);
        }

        if (cardUi.m_CardBackImage != null)
        {
            cardUi.m_CardBackImage.enabled = false;
            if (cardUi.m_CardBackImage.gameObject != null)
            {
                cardUi.m_CardBackImage.gameObject.SetActive(false);
            }
        }

        if (cardUi.m_CardBack != null)
        {
            cardUi.m_CardBack.SetActive(false);
        }

        DisableCardBackSpritesUnderTransform(cardUi.m_GradedCardCaseGrp != null
            ? cardUi.m_GradedCardCaseGrp.transform
            : null);
        DisableCardBackSpritesUnderTransform(cardUi.transform);
    }

    private static void DisableCardBackSpritesUnderTransform(Transform? root)
    {
        if (root == null)
        {
            return;
        }

        Image[] images = root.GetComponentsInChildren<Image>(includeInactive: true);
        for (int i = 0; i < images.Length; i++)
        {
            Image? image = images[i];
            if (image == null || image.sprite == null)
            {
                continue;
            }

            string spriteName = image.sprite.name ?? string.Empty;
            string objectName = image.gameObject != null ? image.gameObject.name : string.Empty;
            if (objectName == GradedCaseFaceObjectName || objectName == "TetramonOverlay0703")
            {
                continue;
            }

            bool looksLikeBack = spriteName.IndexOf("CardBack", StringComparison.OrdinalIgnoreCase) >= 0
                || spriteName.IndexOf("card_back", StringComparison.OrdinalIgnoreCase) >= 0
                || spriteName.IndexOf("Card_Back", StringComparison.OrdinalIgnoreCase) >= 0
                || objectName.IndexOf("CardBack", StringComparison.OrdinalIgnoreCase) >= 0;

            if (!looksLikeBack)
            {
                continue;
            }

            image.enabled = false;
            if (image.gameObject != null)
            {
                image.gameObject.SetActive(false);
            }
        }
    }

    private static void DisableGradedCaseBackAndBackBlocker(CardUI cardUi)
    {
        if (cardUi.m_GradedCardCaseBackGrp != null)
        {
            cardUi.m_GradedCardCaseBackGrp.SetActive(false);
        }

        Card3dUIGroup? card3d = CardUiDisplayContext.ResolveCard3dGroup(cardUi);
        if (card3d?.m_GradedCaseCullCardBackMeshBlocker != null)
        {
            card3d.m_GradedCaseCullCardBackMeshBlocker.SetActive(false);
        }
    }

    public static void AfterGradedCaseLayoutChanged(CardUI cardUi, bool albumSimplified = false)
    {
        CardData? cardData = cardUi?.GetCardData();
        if (cardData == null || cardData.cardGrade <= 0 || cardData.expansionType == ECardExpansionType.Tetramon)
        {
            return;
        }

        if (cardUi!.m_GradedCardCaseGrp == null)
        {
            return;
        }

        ApplyGradedNonTetramonFace(cardUi, cardData, albumSimplified);
    }

    /// <summary>
    /// Album distance-cull turns CullGrp off every call — re-assert Destiny/Ghost graded faces.
    /// </summary>
    public static void AfterSimplifyCardDistanceCull(Card3dUIGroup card3d, bool isCull)
    {
        if (card3d?.m_CardUI == null)
        {
            return;
        }

        CardData? cardData = card3d.m_CardUI.GetCardData();
        if (cardData == null || cardData.cardGrade <= 0 || cardData.expansionType == ECardExpansionType.Tetramon)
        {
            return;
        }

        RestoreGraded3dSlabFaceVisibility(card3d.m_CardUI);
        if (isCull)
        {
            AfterGradedCaseLayoutChanged(card3d.m_CardUI, albumSimplified: true);
        }
    }

    public static void RepairGradedNonTetramonFace(CardUI cardUi, CardData cardData)
    {
        if (cardUi == null || cardData == null || cardUi.m_GradedCardCaseGrp == null)
        {
            return;
        }

        HideDuplicateAlbumFoilHosts(cardUi);
        HideGradedCardBackFaces(cardUi);
        DisableGradedHeaderTextureSlot(cardUi);
        RestoreGraded3dSlabFaceVisibility(cardUi);
        HideOccludingGradedSlabMeshes(cardUi);
        BlankGraded3dSlabCardBackMaterials(cardUi);
        DisableOverlayImage(cardUi);
        EnsureGradeLabelTextsVisible(cardUi);

        Sprite? cardArt = ArtExpanderBridge.LoadCardArt(cardData) ?? ResolveCardArt(cardUi, cardData, out _);
        if (cardArt == null)
        {
            Transform? faceTransform = cardUi.m_GradedCardCaseGrp.transform.Find(GradedCaseFaceObjectName);
            if (faceTransform != null
                && faceTransform.TryGetComponent(out Image existingFace)
                && existingFace.sprite != null)
            {
                cardArt = existingFace.sprite;
            }
        }

        if (cardArt != null)
        {
            ApplyGradedCaseFaceArt(cardUi, cardArt);
        }

        HideOversizedCardFrontBehindSlab(cardUi);
        HideOccludingGradedSlabMeshes(cardUi);
        HideGradedCardBackFaces(cardUi);
        DisableGradedHeaderTextureSlot(cardUi);
        EnsureGradeLabelTextsVisible(cardUi);
        cardUi.m_GradedCardCaseGrp.SetActive(true);
        cardUi.m_GradedCardCaseGrp.transform.SetAsLastSibling();
    }

    public static void ApplyGradedCardPresentationPublic(CardUI cardUi, CardData cardData)
    {
        if (cardData != null && cardData.expansionType != ECardExpansionType.Tetramon)
        {
            ApplyGradedNonTetramonFace(cardUi, cardData);
            return;
        }

        ApplyGradedCardPresentation(cardUi, cardData);
    }

    private static void ApplyGradedCardPresentation(CardUI cardUi, CardData cardData)
    {
        DisableOverlayImage(cardUi);
        RestoreCenterFrameVisibility(cardUi);
        RestoreVanillaChromeVisibility(cardUi);
        RestoreDuplicateTextVisibility(cardUi);
        HideDuplicateAlbumFoilHosts(cardUi);
        StripAlbumHoFoilMaterials(cardUi);

        try
        {
            cardUi.m_Show2DGradedCase = true;
            cardUi.ShowGradedCardCase(isShow: true);
        }
        catch
        {
            // Older CardUI without graded helpers.
        }

        if (cardUi.m_GradedCardCaseGrp != null)
        {
            cardUi.m_GradedCardCaseGrp.transform.SetAsLastSibling();
        }

        // LateUpdate: HO re-enables foil over the slab on album and world cards.
        AlbumHoFoilRepairBehaviour.EnsureOn(cardUi);
    }

    /// <summary>Album/held graded pickup: front + slab toward camera, no UI/mesh card back.</summary>
    public static void ApplyGradedHeldPresentation(CardUI cardUi, Card3dUIGroup? card3d = null)
    {
        CardData? cardData = cardUi.GetCardData();
        if (cardData == null || cardData.cardGrade <= 0)
        {
            return;
        }

        if (cardData.expansionType != ECardExpansionType.Tetramon)
        {
            ApplyGradedNonTetramonFace(cardUi, cardData);
        }
        else
        {
            ApplyGradedCardPresentation(cardUi, cardData);
        }

        card3d ??= CardUiDisplayContext.ResolveCard3dGroup(cardUi);
        InteractableCard3d? interactable = Card3dInteractableRegistry.FindForCardUi(cardUi);
        bool onShelf = interactable != null && interactable.IsDisplayedOnShelf();

        if (onShelf)
        {
            // Graded on display still needs an opaque back when viewed from behind.
            ConfigureCard3dForFrontDisplay(cardUi);
            return;
        }

        // Album / held: never re-enable shop card back.
        HideGradedCardBackFaces(cardUi);
        if (cardData.expansionType != ECardExpansionType.Tetramon)
        {
            FitCardFrontIntoGradedSlabWindow(cardUi);
            RestoreGraded3dSlabFaceVisibility(cardUi);
            HideOccludingGradedSlabMeshes(cardUi);
            DisableGradedHeaderTextureSlot(cardUi);
        }
        else
        {
            SetCardFrontCanvasActive(cardUi, active: true);
        }

        if (card3d?.m_CardBackMesh != null)
        {
            card3d.m_CardBackMesh.SetActive(false);
        }
    }

    private static void ClearStaleOverlayChromeOnly(CardUI cardUi)
    {
        if (FindOverlayTransform(cardUi) == null)
        {
            return;
        }

        DisableOverlayImage(cardUi);
        RestoreCenterFrameVisibility(cardUi);
        RestoreVanillaChromeVisibility(cardUi);
        RestoreDuplicateTextVisibility(cardUi);
    }

    /// <summary>
    /// After full-card overlay (any context): keep ArtExpander face readable and only enable
    /// HO foil hosts that can be bound to card art. HO currently caches 0 foil configs, so
    /// unbound hosts scramble shelf/trade faces the same way album used to.
    /// </summary>
    public static void EnsureAlbumArtAndFoilLayering(CardUI cardUi)
    {
        if (cardUi == null)
        {
            return;
        }

        CardData? cardData = cardUi.GetCardData();
        if (cardData != null && cardData.cardGrade > 0)
        {
            HideDuplicateAlbumFoilHosts(cardUi);
            if (cardData.expansionType != ECardExpansionType.Tetramon)
            {
                RepairGradedNonTetramonFace(cardUi, cardData);
            }

            return;
        }

        Sprite? cardArt = null;
        if (FindOverlayTransform(cardUi) is Transform overlayTransform
            && overlayTransform.TryGetComponent(out Image overlay)
            && overlay.enabled
            && overlay.sprite != null)
        {
            // Readable face under foil; CardFoilRainbow on this Image previously made faces invisible.
            overlay.material = null;
            overlay.color = Color.white;
            cardArt = overlay.sprite;
        }

        if (cardArt == null)
        {
            HideDuplicateAlbumFoilHosts(cardUi);
            AlbumHoFoilRepairBehaviour.EnsureOn(cardUi);
            return;
        }

        ShowAlbumFoilHosts(cardUi);
        int boundHosts = BindAlbumFoilHostsToCardArt(cardUi, cardArt);
        // No HO materials bound (typical when HO foil configs = 0): keep hosts off so the overlay shows.
        if (boundHosts <= 0)
        {
            HideDuplicateAlbumFoilHosts(cardUi);
        }
        else
        {
            BringFoilLayersAboveCardArt(cardUi);
            LogAlbumHoloBindOnce(cardUi, cardArt, boundHosts);
        }

        AlbumHoFoilRepairBehaviour.EnsureOn(cardUi);
    }

    /// <summary>
    /// Shelf DisplayCulling re-enables foil hosts after SetCardUI — re-apply overlay foil policy.
    /// </summary>
    public static void ReassertWorldCardFoilPolicy(CardUI? cardUi)
    {
        if (cardUi == null)
        {
            return;
        }

        CardData? cardData = cardUi.GetCardData();
        if (cardData != null && cardData.cardGrade > 0)
        {
            HideDuplicateAlbumFoilHosts(cardUi);
            if (cardData.expansionType != ECardExpansionType.Tetramon)
            {
                RepairGradedNonTetramonFace(cardUi, cardData);
            }

            AlbumHoFoilRepairBehaviour.EnsureOn(cardUi);
            return;
        }

        if (FindOverlayTransform(cardUi) is Transform overlayTransform
            && overlayTransform.TryGetComponent(out Image overlay)
            && overlay.enabled
            && overlay.sprite != null)
        {
            EnsureAlbumArtAndFoilLayering(cardUi);
            return;
        }

        // No full-card overlay: unbound HO / foil cull hosts scramble vanilla ArtExpander chrome.
        HideDuplicateAlbumFoilHosts(cardUi);
        AlbumHoFoilRepairBehaviour.EnsureOn(cardUi);
    }

    private static bool _loggedAlbumHoloBind;

    private static void LogAlbumHoloBindOnce(CardUI cardUi, Sprite cardArt, int boundHosts)
    {
        if (_loggedAlbumHoloBind)
        {
            return;
        }

        _loggedAlbumHoloBind = true;
        string shaderSample = "(none)";
        string mainTexSample = "(none)";
        if (cardUi.m_FoilShowList != null)
        {
            for (int i = 0; i < cardUi.m_FoilShowList.Count; i++)
            {
                Image? image = cardUi.m_FoilShowList[i];
                if (image == null || !image.isActiveAndEnabled)
                {
                    continue;
                }

                Material? mat = image.materialForRendering ?? image.material;
                if (!IsHoFoilMaterial(mat))
                {
                    continue;
                }

                shaderSample = mat!.shader != null ? mat.shader.name : mat.name;
                Texture? mainTex = mat.HasProperty("_MainTex") ? mat.GetTexture("_MainTex") : mat.mainTexture;
                if (mainTex != null)
                {
                    bool isWhite = ReferenceEquals(mainTex, Texture2D.whiteTexture)
                        || string.Equals(mainTex.name, "UnityWhite", StringComparison.OrdinalIgnoreCase);
                    mainTexSample = isWhite
                        ? $"{mainTex.name} (WHITE — holo will not show art)"
                        : $"{mainTex.name} ({mainTex.width}x{mainTex.height})";
                }
                else
                {
                    mainTexSample = "null";
                }

                break;
            }
        }

        Plugin.Log.LogInfo(
            $"Album HO bind: hosts={boundHosts}, art={cardArt.name} ({cardArt.rect.width}x{cardArt.rect.height}), "
            + $"sampleShader={shaderSample}, sampleMainTex={mainTexSample}");
    }

    /// <summary>
    /// Graded album cards: vanilla ShowGradedCardCase shrinks m_CardFront into the slab window.
    /// </summary>
    private static void ReapplyGradedCaseLayout(CardUI cardUi)
    {
        CardData? cardData = cardUi.GetCardData();
        if (cardData == null || cardData.cardGrade <= 0)
        {
            return;
        }

        if (cardUi.m_GradedCardCaseGrp == null)
        {
            return;
        }

        try
        {
            cardUi.m_Show2DGradedCase = true;
            cardUi.ShowGradedCardCase(isShow: true);
        }
        catch
        {
            // Older CardUI without graded helpers.
        }

        // Case frame above the scaled card face.
        cardUi.m_GradedCardCaseGrp.transform.SetAsLastSibling();
    }

    /// <summary>
    /// Destiny/Trainer (and album pages) without a full-card overlay — hide opaque HO foil hosts and
    /// strip CardFoil materials from chrome so ArtExpander/vanilla art is visible.
    /// </summary>
    public static void EnsureAlbumReadableWithoutTetramonOverlay(CardUI cardUi)
    {
        EnsureReadableWithoutFullCardOverlay(cardUi);
    }

    public static void EnsureReadableWithoutFullCardOverlay(CardUI cardUi)
    {
        if (cardUi == null)
        {
            return;
        }

        HideDuplicateAlbumFoilHosts(cardUi);
        StripAlbumHoFoilMaterials(cardUi);
        RestoreCenterFrameVisibility(cardUi);

        if (cardUi.m_CenterFrameImage != null)
        {
            cardUi.m_CenterFrameImage.material = null;
            cardUi.m_CenterFrameImage.enabled = true;
            cardUi.m_CenterFrameImage.color = Color.white;
        }

        ResetImageToDefaultUiMaterial(cardUi.m_CardFrontImage);
        ResetImageToDefaultUiMaterial(cardUi.m_CardFrontImageTopLayer);
        ResetImageToDefaultUiMaterial(cardUi.m_CardBGImage);
        ResetImageToDefaultUiMaterial(cardUi.m_CardFullBGImage);
        ResetImageToDefaultUiMaterial(cardUi.m_CardBorderImage);
        ResetImageToDefaultUiMaterial(cardUi.m_CardFullTransparentLayerBGImage);

        AlbumHoFoilRepairBehaviour.EnsureOn(cardUi);
    }

    private static void ShowAlbumFoilHosts(CardUI cardUi)
    {
        // Only enable hosts that already carry HO CardFoil materials. Enabling every foil Image
        // (hosts=13, shader=none) covered graded cases and did not produce holographics.
        int enabled = EnableOnlyHoFoilImages(cardUi.m_FoilShowList)
            + EnableOnlyHoFoilImages(cardUi.m_FoilBlendedShowList);

        if (cardUi.m_FoilGrp != null)
        {
            bool anyHoUnderGrp = false;
            Image[] images = cardUi.m_FoilGrp.GetComponentsInChildren<Image>(includeInactive: true);
            for (int i = 0; i < images.Length; i++)
            {
                if (IsHoFoilMaterial(images[i]?.material) || IsHoFoilMaterial(images[i]?.materialForRendering))
                {
                    anyHoUnderGrp = true;
                    images[i]!.enabled = true;
                    images[i]!.gameObject.SetActive(true);
                }
            }

            cardUi.m_FoilGrp.SetActive(anyHoUnderGrp || enabled > 0);
        }

        if (enabled > 0 || (cardUi.m_FoilGrp != null && cardUi.m_FoilGrp.activeSelf))
        {
            try
            {
                cardUi.SetFoilCullListVisibility(isActive: true);
            }
            catch
            {
                // Older CardUI builds without cull lists.
            }
        }
    }

    private static int EnableOnlyHoFoilImages(List<Image>? list)
    {
        if (list == null)
        {
            return 0;
        }

        int enabled = 0;
        for (int i = 0; i < list.Count; i++)
        {
            Image? image = list[i];
            if (image == null)
            {
                continue;
            }

            if (!IsHoFoilMaterial(image.material) && !IsHoFoilMaterial(image.materialForRendering))
            {
                image.enabled = false;
                continue;
            }

            image.enabled = true;
            if (image.gameObject != null)
            {
                image.gameObject.SetActive(true);
            }

            enabled++;
        }

        return enabled;
    }

    private static int BindAlbumFoilHostsToCardArt(CardUI cardUi, Sprite cardArt)
    {
        CardData? cardData = cardUi.GetCardData();
        bool gradedAlbum = cardData != null
            && cardData.cardGrade > 0
            && CardUiDisplayContext.IsBinderAlbumCard(cardUi);
        Transform? cardFront = cardUi.m_CardFront != null ? cardUi.m_CardFront.transform : null;

        int bound = 0;
        bound += BindFoilImageListToCardArt(cardUi.m_FoilShowList, cardArt, cardFront, gradedAlbum);
        bound += BindFoilImageListToCardArt(cardUi.m_FoilBlendedShowList, cardArt, cardFront, gradedAlbum);

        if (cardUi.m_FoilGrp == null)
        {
            return bound;
        }

        if (gradedAlbum && cardFront != null && !cardUi.m_FoilGrp.transform.IsChildOf(cardFront))
        {
            return bound;
        }

        Image[] foilGroupImages = cardUi.m_FoilGrp.GetComponentsInChildren<Image>(includeInactive: true);
        for (int i = 0; i < foilGroupImages.Length; i++)
        {
            if (BindFoilImageToCardArt(foilGroupImages[i], cardArt))
            {
                bound++;
            }
        }

        return bound;
    }

    private static int BindFoilImageListToCardArt(
        List<Image>? list,
        Sprite cardArt,
        Transform? cardFront = null,
        bool gradedAlbum = false)
    {
        if (list == null)
        {
            return 0;
        }

        int bound = 0;
        for (int i = 0; i < list.Count; i++)
        {
            Image? image = list[i];
            if (gradedAlbum && cardFront != null && image != null && !image.transform.IsChildOf(cardFront))
            {
                continue;
            }

            if (BindFoilImageToCardArt(image, cardArt))
            {
                bound++;
            }
        }

        return bound;
    }

    private static bool BindFoilImageToCardArt(Image? foilImage, Sprite cardArt)
    {
        if (foilImage == null || cardArt == null || cardArt.texture == null)
        {
            return false;
        }

        Material? mat = foilImage.material;
        if (!IsHoFoilMaterial(mat))
        {
            mat = foilImage.materialForRendering;
        }

        // Do not activate non-HO foil Images — that blanked graded cases (hosts=13, shader=none).
        if (!IsHoFoilMaterial(mat))
        {
            return false;
        }

        foilImage.sprite = cardArt;
        foilImage.type = Image.Type.Simple;
        foilImage.preserveAspect = true;
        foilImage.color = Color.white;
        foilImage.enabled = true;
        if (foilImage.gameObject != null)
        {
            foilImage.gameObject.SetActive(true);
        }

        BindArtTextureToHoMaterial(mat!, cardArt);
        foilImage.SetMaterialDirty();
        foilImage.SetVerticesDirty();
        return true;
    }

    private static void BindArtTextureToHoMaterial(Material mat, Sprite artSprite)
    {
        Texture artTex = artSprite.texture;
        GetSpriteUv(artSprite, out Vector2 scale, out Vector2 offset);

        mat.mainTexture = artTex;
        if (mat.HasProperty("_MainTex"))
        {
            mat.SetTexture("_MainTex", artTex);
            mat.SetTextureScale("_MainTex", scale);
            mat.SetTextureOffset("_MainTex", offset);
        }

        if (mat.HasProperty("_BaseMap"))
        {
            mat.SetTexture("_BaseMap", artTex);
            mat.SetTextureScale("_BaseMap", scale);
            mat.SetTextureOffset("_BaseMap", offset);
        }

        // If HO left FoilTex as white, use card art so the foil pass still has readable content.
        if (mat.HasProperty("_FoilTex"))
        {
            Texture? foilTex = mat.GetTexture("_FoilTex");
            if (foilTex == null || ReferenceEquals(foilTex, Texture2D.whiteTexture))
            {
                mat.SetTexture("_FoilTex", artTex);
                mat.SetTextureScale("_FoilTex", scale);
                mat.SetTextureOffset("_FoilTex", offset);
            }
        }
    }

    private static void GetSpriteUv(Sprite sprite, out Vector2 scale, out Vector2 offset)
    {
        Texture texture = sprite.texture;
        Rect rect = sprite.textureRect;
        if (texture == null || texture.width <= 0 || texture.height <= 0)
        {
            scale = Vector2.one;
            offset = Vector2.zero;
            return;
        }

        scale = new Vector2(rect.width / texture.width, rect.height / texture.height);
        offset = new Vector2(rect.x / texture.width, rect.y / texture.height);
    }

    private static void StripAlbumHoFoilMaterials(CardUI cardUi)
    {
        Image[] images = cardUi.GetComponentsInChildren<Image>(includeInactive: true);
        for (int i = 0; i < images.Length; i++)
        {
            Image? image = images[i];
            if (image == null)
            {
                continue;
            }

            // Keep our art overlay; strip HO CardFoil* from everything else.
            if (image.gameObject != null
                && string.Equals(image.gameObject.name, "TetramonOverlay0703", StringComparison.Ordinal))
            {
                continue;
            }

            if (!IsHoFoilMaterial(image.material) && !IsHoFoilMaterial(image.materialForRendering))
            {
                continue;
            }

            image.material = null;
            string objectName = image.gameObject != null ? image.gameObject.name : string.Empty;
            if (objectName.IndexOf("Foil", StringComparison.OrdinalIgnoreCase) >= 0
                || objectName.IndexOf("Mask_Blended", StringComparison.OrdinalIgnoreCase) >= 0
                || objectName.IndexOf("Mask_Plain", StringComparison.OrdinalIgnoreCase) >= 0
                || objectName.IndexOf("Glitter", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                image.enabled = false;
                if (image.gameObject != null)
                {
                    image.gameObject.SetActive(false);
                }
            }
        }
    }

    private static void ResetImageToDefaultUiMaterial(Image? image)
    {
        if (image == null)
        {
            return;
        }

        image.material = null;
    }

    private static bool IsHoFoilMaterial(Material? mat)
    {
        if (mat?.shader == null)
        {
            return false;
        }

        string shaderName = mat.shader.name ?? string.Empty;
        string matName = mat.name ?? string.Empty;
        return shaderName.IndexOf("CardFoil", StringComparison.OrdinalIgnoreCase) >= 0
            || shaderName.IndexOf("Holo", StringComparison.OrdinalIgnoreCase) >= 0
            || matName.IndexOf("CardFoil", StringComparison.OrdinalIgnoreCase) >= 0
            || matName.IndexOf("Holo", StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private static void HideDuplicateAlbumFoilHosts(CardUI cardUi)
    {
        try
        {
            cardUi.SetFoilCullListVisibility(isActive: false);
        }
        catch
        {
            // Older CardUI builds without cull lists.
        }

        SetFoilImageListActive(cardUi.m_FoilShowList, active: false);
        SetFoilImageListActive(cardUi.m_FoilBlendedShowList, active: false);

        if (cardUi.m_FoilGrp != null)
        {
            cardUi.m_FoilGrp.SetActive(false);
        }
    }

    private static void SetFoilImageListActive(List<Image>? list, bool active)
    {
        if (list == null)
        {
            return;
        }

        for (int i = 0; i < list.Count; i++)
        {
            Image? image = list[i];
            if (image == null)
            {
                continue;
            }

            image.enabled = active;
            if (image.gameObject != null)
            {
                image.gameObject.SetActive(active);
            }
        }
    }

    /// <summary>
    /// LateUpdate re-assert: after HO HoloFixMatLock, keep foil hosts bound or hidden.
    /// Runs for album and world cards (Destiny shelf/trade previously skipped this).
    /// </summary>
    public static void RepairAlbumHoFoilMainTex(CardUI cardUi)
    {
        if (cardUi == null)
        {
            return;
        }

        CardData? cardData = cardUi.GetCardData();
        if (cardData == null)
        {
            return;
        }

        if (cardData.cardGrade > 0)
        {
            // HO re-enables foil hosts over the slab window every frame — keep them off.
            // Do not re-enter ShowGradedCardCase / full apply (that re-scrambled every LateUpdate).
            HideDuplicateAlbumFoilHosts(cardUi);
            if (cardData.expansionType != ECardExpansionType.Tetramon)
            {
                RepairGradedNonTetramonFace(cardUi, cardData);
            }

            return;
        }

        if (FindOverlayTransform(cardUi) is Transform overlayTransform
            && overlayTransform.TryGetComponent(out Image overlay)
            && overlay.enabled
            && overlay.sprite != null)
        {
            EnsureAlbumArtAndFoilLayering(cardUi);
            return;
        }

        if (cardData.expansionType != ECardExpansionType.Tetramon)
        {
            Sprite? art = ArtExpanderBridge.LoadCardArt(cardData);
            if (art != null)
            {
                ApplyFullCardOverlay(cardUi, art);
                EnsureAlbumArtAndFoilLayering(cardUi);
                return;
            }
        }

        EnsureReadableWithoutFullCardOverlay(cardUi);
    }

    private static void BringFoilLayersAboveCardArt(CardUI cardUi)
    {
        CardData? cardData = cardUi.GetCardData();
        bool gradedAlbum = cardData != null
            && cardData.cardGrade > 0
            && CardUiDisplayContext.IsBinderAlbumCard(cardUi);

        // Graded: keep foil with the card face under the slab — do not promote foil above the case.
        if (gradedAlbum && cardUi.m_CardFront != null)
        {
            BringFoilLayersAboveCardArtUnderParent(cardUi, cardUi.m_CardFront.transform);
            return;
        }

        if (cardUi.m_FoilGrp != null)
        {
            cardUi.m_FoilGrp.transform.SetAsLastSibling();
        }

        BringFoilImageListToLastSibling(cardUi.m_FoilShowList);
        BringFoilImageListToLastSibling(cardUi.m_FoilBlendedShowList);
    }

    private static void BringFoilLayersAboveCardArtUnderParent(CardUI cardUi, Transform cardFront)
    {
        if (cardUi.m_FoilGrp != null && cardUi.m_FoilGrp.transform.IsChildOf(cardFront))
        {
            cardUi.m_FoilGrp.transform.SetAsLastSibling();
        }

        BringFoilImageListToLastSiblingUnderParent(cardUi.m_FoilShowList, cardFront);
        BringFoilImageListToLastSiblingUnderParent(cardUi.m_FoilBlendedShowList, cardFront);

        if (FindOverlayTransform(cardUi) is Transform overlayTransform
            && overlayTransform.IsChildOf(cardFront))
        {
            // Face under foil hosts that are also under CardFront.
            overlayTransform.SetAsFirstSibling();
        }
    }

    private static void BringFoilImageListToLastSibling(List<Image>? list)
    {
        if (list == null)
        {
            return;
        }

        for (int i = 0; i < list.Count; i++)
        {
            Image? foilImage = list[i];
            if (foilImage != null)
            {
                foilImage.transform.SetAsLastSibling();
            }
        }
    }

    private static void BringFoilImageListToLastSiblingUnderParent(List<Image>? list, Transform parent)
    {
        if (list == null)
        {
            return;
        }

        for (int i = 0; i < list.Count; i++)
        {
            Image? foilImage = list[i];
            if (foilImage != null && foilImage.transform.IsChildOf(parent))
            {
                foilImage.transform.SetAsLastSibling();
            }
        }
    }

    public static void SetCardUI_ApplyTetramonOverlay(CardUI __instance, CardData cardData, bool forceFrontOverlay = false)
    {
        if (__instance == null || cardData == null)
        {
            return;
        }

        if (cardData.expansionType != ECardExpansionType.Tetramon)
        {
            ClearStaleOverlayAfterExpansionSwitch(__instance);
            return;
        }

        Card3dUIGroup? card3d = CardUiDisplayContext.ResolveCard3dGroup(__instance);
        if (!forceFrontOverlay && PackOpeningState.IsPackOpeningInProgress())
        {
            if (card3d != null && PackOpeningState.ShouldShowFrontDuringPackFlip(card3d))
            {
                ApplyTetramonFrontOverlay(__instance, cardData);
            }
            else
            {
                ConfigurePackOpeningCardPresentation(__instance, card3d);
            }

            return;
        }

        ApplyTetramonCardBackFromCache(__instance);
        ApplyTetramonFrontOverlay(__instance, cardData);
    }

    /// <summary>
    /// Vanilla rip, driven by the card's real orientation: the deck spawns face-down (back toward camera) so
    /// every card shows the blue Pokemon back; around state 5 the stack rotates face-up and each card shows its
    /// face. We always render the camera-facing side, so no mirroring is needed — the blue back is on the back
    /// canvas (camera-facing while face-down) and the face is on the front canvas (camera-facing once flipped).
    /// </summary>
    public static void ConfigurePackOpeningCardPresentation(CardUI cardUi, Card3dUIGroup? card3d = null)
    {
        card3d ??= CardUiDisplayContext.ResolveCard3dGroup(cardUi);

        if (card3d != null && !PackOpeningState.IsCardFrontTowardCamera(card3d))
        {
            ApplyPackOpeningStackTopBack(cardUi, card3d);
            return;
        }

        ApplyPackOpeningFlipFrontPresentation(cardUi, card3d);
    }

    private static void HidePackOpeningFrontFace(CardUI cardUi)
    {
        DisableOverlayImage(cardUi);
        HideCenterFrameArt(cardUi);
        HideVanillaChromeWhenOverlayShown(cardUi);
        HideDuplicateTextWhenOverlayShown(cardUi);
        SuppressFrontOverlayDuringPackBack(cardUi);
        SuppressPackOpeningFoilMask(cardUi);
    }

    /// <summary>
    /// Post-rip deck stack (card face-down, facing=-1): show the blue Pokemon back via the UI back canvas.
    /// Diagnostics proved the 3D back mesh is unusable here — it is a child of the disabled, zero-scaled
    /// Card3dUIGroup (the animation owns that group), so the MeshRenderer collapses to an invisible point and
    /// activating its parent breaks the open. The UI canvas renders fine at scale 0, but this game's UI is
    /// single-sided (backface-culled), so a face-down back canvas faces away and shows nothing. Mirroring the
    /// back canvas (negative X scale) flips its normal toward the camera so the near-symmetric blue back renders
    /// at the rip. Confined to this rip branch so the working face-up flip presentation is untouched.
    /// </summary>
    private static void ApplyPackOpeningStackTopBack(CardUI cardUi, Card3dUIGroup? card3d = null)
    {
        HidePackOpeningFrontFace(cardUi);

        card3d ??= CardUiDisplayContext.ResolveCard3dGroup(cardUi);
        SetPackCardUiAnimGroupVisible(card3d, visible: true);
        SetCardFrontCanvasActive(cardUi, active: false);
        PreparePackSingleCardBackImage(cardUi, faceCamera: true);
        HidePackOpeningBackMesh(card3d);
    }

    private static bool _loggedBackMeshShow;

    /// <summary>Re-arm the back-mesh diagnostic so each pack open logs the mesh render state once.</summary>
    public static void ResetBackMeshDiagnostics()
    {
        _loggedBackMeshShow = false;
    }

    /// <summary>Enable the 3D back mesh with its natural Pokemon texture (no UV/material override).</summary>
    private static void ShowPackOpeningBackMesh(Card3dUIGroup? card3d)
    {
        if (card3d?.m_CardBackMesh == null)
        {
            return;
        }

        card3d.m_CardBackMesh.SetActive(true);
        card3d.m_CardBackMesh.transform.localScale = Vector3.one;
        SetCard3dBackMeshVisible(card3d, visible: true);

        if (!_loggedBackMeshShow)
        {
            _loggedBackMeshShow = true;
            Renderer? renderer = card3d.m_CardBackMesh.GetComponent<Renderer>();
            string layer = LayerMask.LayerToName(card3d.m_CardBackMesh.layer);
            string mat = renderer?.material != null ? renderer.material.name : "null";
            float alpha = renderer?.material != null && renderer.material.HasProperty("_Color")
                ? renderer.material.color.a
                : -1f;
            Plugin.Log.LogWarning(
                $"BackMesh show: active={card3d.m_CardBackMesh.activeInHierarchy} rendererEnabled={renderer?.enabled} " +
                $"isVisible={renderer?.isVisible} layer={layer} mat={mat} alpha={alpha:F2} " +
                $"scale={card3d.m_CardBackMesh.transform.lossyScale}");

            // Walk the ancestor chain so we can see exactly which parent is inactive / zero-scaled (the disabled
            // 3D card model that hides the back mesh during the UI-driven open).
            Transform? walk = card3d.m_CardBackMesh.transform.parent;
            int depth = 0;
            while (walk != null && depth < 8)
            {
                Plugin.Log.LogWarning(
                    $"BackMesh ancestor[{depth}] name={walk.name} activeSelf={walk.gameObject.activeSelf} " +
                    $"localScale={walk.localScale} localPos={walk.localPosition}");
                walk = walk.parent;
                depth++;
            }
        }
    }

    private static void ApplyPackOpeningActiveFlipBack(CardUI cardUi, Card3dUIGroup? card3d = null)
    {
        ApplyPokemonBackFaceUi(cardUi, card3d);
    }

    /// <summary>
    /// Show the Pokemon card back on a face-down pack card using the UI back canvas only.
    /// The 3D back mesh is intentionally hidden: with a transparent/atlas texture it renders in front of the
    /// back canvas and occludes it, leaving the deck back blank (see deck-back regression).
    /// </summary>
    private static void ApplyPokemonBackFaceUi(CardUI cardUi, Card3dUIGroup? card3d = null)
    {
        HidePackOpeningFrontFace(cardUi);

        card3d ??= CardUiDisplayContext.ResolveCard3dGroup(cardUi);
        SetPackCardUiAnimGroupVisible(card3d, visible: true);
        SetCardFrontCanvasActive(cardUi, active: false);
        PreparePackSingleCardBackImage(cardUi);
        HidePackOpeningBackMesh(card3d);
    }

    public static void ApplyPackOpeningFlipFrontPresentation(CardUI cardUi, Card3dUIGroup? card3d = null)
    {
        card3d ??= CardUiDisplayContext.ResolveCard3dGroup(cardUi);
        SetPackCardUiAnimGroupVisible(card3d, visible: true);
        SetCardFrontCanvasActive(cardUi, active: true);
        SetCardFrontMirrored(cardUi, mirrored: false);
        DisableUiCardBackCover(cardUi);
        SetCardBackCanvasActive(cardUi, active: false);
        SetCardBackFlipped(cardUi, flipped: false);
        SuppressPackOpeningFoilMask(cardUi);
        HidePackOpeningBackMesh(card3d);
    }

    /// <summary>Buried stack cards: hide faces but keep anim grp alive for vanilla motion / fan row.</summary>
    private static void ApplyPackOpeningHiddenInStack(CardUI cardUi, Card3dUIGroup? card3d = null)
    {
        DisableOverlayImage(cardUi);
        HideCenterFrameArt(cardUi);

        card3d ??= CardUiDisplayContext.ResolveCard3dGroup(cardUi);
        SetPackCardUiAnimGroupVisible(card3d, visible: true);
        SetCardFrontCanvasActive(cardUi, active: false);
        DisableUiCardBackCover(cardUi);
        SetCardBackCanvasActive(cardUi, active: false);
        SuppressFrontOverlayDuringPackBack(cardUi);
        SuppressPackOpeningFoilMask(cardUi);
        HidePackOpeningBackMesh(card3d);
    }

    /// <summary>Restore card UI shell before applying front overlays in the fan row (state 7+).</summary>
    public static void ApplyPackOpeningFanRowPresentation(CardUI cardUi, Card3dUIGroup? card3d = null)
    {
        card3d ??= CardUiDisplayContext.ResolveCard3dGroup(cardUi);
        if (card3d != null)
        {
            card3d.m_IgnoreCulling = true;
        }

        SetPackCardUiAnimGroupVisible(card3d, visible: true);
        SetCardFrontCanvasActive(cardUi, active: true);
        SetCardBackCanvasActive(cardUi, active: false);
        DisableUiCardBackCover(cardUi);
        HidePackOpeningBackMesh(card3d);
        ApplyFlatScreenCardPresentation(cardUi, card3d);
    }

    private static void PreparePackSingleCardBackImage(CardUI cardUi, bool faceCamera = false)
    {
        if (cardUi.m_CardBackImage == null)
        {
            return;
        }

        Sprite? backSprite = CardExtrasCacheAccess.TryGetPokemonUiBackSprite()
            ?? CardExtrasCacheAccess.TryGetUiCardBackSprite()
            ?? cardUi.m_CardBackImage.sprite;
        if (backSprite == null && cardUi.GetCardData() is CardData cardData)
        {
            backSprite = CSingleton<InventoryBase>.Instance.m_MonsterData_SO.GetCardBackSprite(cardData.expansionType);
        }

        if (backSprite == null)
        {
            return;
        }

        SetCardBackCanvasActive(cardUi, active: true);
        SetCardBackMirrored(cardUi, mirrored: false);
        // At the rip the card is face-down (back canvas forward points away from camera, so it is backface-culled
        // exactly like the front). A 180 deg Y rotation negates the canvas forward so it faces the camera and
        // renders; negating scale alone leaves forward unchanged and never shows it (proven in the v1.0.98 log).
        SetCardBackFlipped(cardUi, flipped: faceCamera);
        cardUi.m_CardBackImage.enabled = true;
        cardUi.m_CardBackImage.sprite = backSprite;
        cardUi.m_CardBackImage.type = Image.Type.Simple;
        cardUi.m_CardBackImage.preserveAspect = true;
        cardUi.m_CardBackImage.color = Color.white;
        StretchImageToFill(cardUi.m_CardBackImage.rectTransform);
    }

    private static void SetCardBackCanvasActive(CardUI cardUi, bool active)
    {
        if (cardUi.m_CardBack != null)
        {
            cardUi.m_CardBack.SetActive(active);
        }
    }

    private static void SetPackCardUiAnimGroupVisible(Card3dUIGroup? card3d, bool visible)
    {
        if (card3d?.m_CardUIAnimGrp != null)
        {
            card3d.m_CardUIAnimGrp.gameObject.SetActive(visible);
        }
    }

    /// <summary>Clear vanilla GetCardBackSprite on all pack cards before routing one Pokemon back.</summary>
    public static void ForceDisableAllPackOpeningBackUi(CardOpeningSequence sequence)
    {
        if (sequence.m_Card3dUIList == null)
        {
            return;
        }

        for (int i = 0; i < sequence.m_Card3dUIList.Count; i++)
        {
            CardUI? cardUi = sequence.m_Card3dUIList[i]?.m_CardUI;
            if (cardUi == null)
            {
                continue;
            }

            DisableUiCardBackCover(cardUi);
            SetCardBackCanvasActive(cardUi, active: false);
        }
    }

    /// <summary>Force all pack card back meshes off before vanilla/ExpansionMod re-enable them.</summary>
    public static void ForceHideAllPackOpeningBackMeshes(CardOpeningSequence sequence)
    {
        if (sequence.m_Card3dUIList == null)
        {
            return;
        }

        for (int i = 0; i < sequence.m_Card3dUIList.Count; i++)
        {
            Card3dUIGroup? card3d = sequence.m_Card3dUIList[i];
            if (card3d == null)
            {
                continue;
            }

            HidePackOpeningBackMesh(card3d);
        }
    }

    private static void HidePackOpeningBackMesh(Card3dUIGroup? card3d)
    {
        if (card3d?.m_CardBackMesh == null)
        {
            return;
        }

        SetCard3dBackMeshVisible(card3d, visible: false);
        card3d.m_CardBackMesh.SetActive(false);
        card3d.m_CardBackMesh.transform.localScale = Vector3.zero;
    }

    /// <summary>Legacy entry point — delegates to ConfigurePackOpeningCardPresentation.</summary>
    public static void ApplyPackOpeningBackOnly(CardUI cardUi, Card3dUIGroup? card3d = null)
    {
        ConfigurePackOpeningCardPresentation(cardUi, card3d);
    }

    /// <summary>Shop / held 3D cards: apply Pokemon sprite UVs on the back mesh material.</summary>
    public static void ConfigureCard3dBackPresentation(CardUI cardUi, Card3dUIGroup? card3d = null)
    {
        ApplyPackStackBackMeshSync(cardUi, card3d);
    }

    private static void ApplyPackStackBackMeshSync(CardUI cardUi, Card3dUIGroup? card3d = null)
    {
        card3d ??= CardUiFieldAccess.GetValue(cardUi, "m_Card3dUIGroup") as Card3dUIGroup;
        if (card3d?.m_CardBackMesh == null)
        {
            return;
        }

        card3d.m_CardBackMesh.SetActive(true);
        SetCard3dBackMeshVisible(card3d, visible: true);
        card3d.m_CardBackMesh.transform.localScale = Vector3.one;
        SyncCard3dBackMeshFromUiBack(cardUi, card3d, overscan: PackBackMeshOverscan, usePackStackBack: true);
    }

    private static void ApplyPackSingleCardBackMeshSync(CardUI cardUi, Card3dUIGroup? card3d = null)
    {
        card3d ??= CardUiDisplayContext.ResolveCard3dGroup(cardUi);
        if (card3d?.m_CardBackMesh == null)
        {
            return;
        }

        card3d.m_CardBackMesh.SetActive(true);
        SetCard3dBackMeshVisible(card3d, visible: true);
        card3d.m_CardBackMesh.transform.localScale = Vector3.one;
        SyncCard3dBackMeshFromUiBack(cardUi, card3d, overscan: 1f, usePackStackBack: false);
    }

    private static void ApplyTetramonFrontOverlay(CardUI __instance, CardData cardData)
    {
        SetCardFrontCanvasActive(__instance, active: true);

        Card3dUIGroup? card3d = CardUiDisplayContext.ResolveCard3dGroup(__instance);
        bool binderAlbum = CardUiDisplayContext.IsBinderAlbumCard(__instance);
        if (binderAlbum)
        {
            ApplyFlatScreenCardPresentation(__instance, card3d);
        }
        else if (CardUiDisplayContext.ShouldUseRotatableWorldCardBack(__instance))
        {
            PrepareShopDisplayCardBack(__instance);
        }
        else
        {
            ApplyFlatScreenCardPresentation(__instance, card3d);
        }

        // Graded: never full-card overlay — restores chrome inside the slab case.
        if (cardData.cardGrade > 0)
        {
            ApplyGradedCardPresentation(__instance, cardData);
            FinalizeCard3dPresentation(__instance);
            return;
        }

        Sprite? cardArt = ResolveCardArt(__instance, cardData, out bool fromBridge);
        object? cardConfig = NewSwappingHandler.TryGetCardFromCache(cardData);

        if (cardArt != null && (fromBridge || LooksLikeFullCard(cardArt)))
        {
            ApplyFullCardOverlay(__instance, cardArt);
            if (binderAlbum)
            {
                EnsureAlbumArtAndFoilLayering(__instance);
            }

            FinalizeCard3dPresentation(__instance);
            return;
        }

        DisableOverlayImage(__instance);

        if (cardArt != null)
        {
            ApplyCenterArtLayout(__instance, cardArt, cardConfig);
            if (binderAlbum)
            {
                EnsureAlbumArtAndFoilLayering(__instance);
            }

            FinalizeCard3dPresentation(__instance);
            return;
        }

        RestoreCenterFrameIcon(__instance, cardData);
        ApplyNoArtFallback(__instance, cardConfig, cardData);
        if (binderAlbum)
        {
            EnsureAlbumArtAndFoilLayering(__instance);
        }

        FinalizeCard3dPresentation(__instance);
    }

    /// <summary>Pack opening: Pokemon UI backs only — never touch T_CardBackMesh.</summary>
    public static void SyncPackOpeningBackMesh(CardUI cardUi, Card3dUIGroup? card3d = null)
    {
        card3d ??= CardUiFieldAccess.GetValue(cardUi, "m_Card3dUIGroup") as Card3dUIGroup;
        if (card3d == null || !cardUi.IsCard3dUIGroupSet())
        {
            return;
        }

        ConfigurePackOpeningCardPresentation(cardUi, card3d);
    }

    private static void FinalizeCard3dPresentation(CardUI cardUi)
    {
        Card3dUIGroup? card3d = CardUiDisplayContext.ResolveCard3dGroup(cardUi);

        if (CardUiDisplayContext.IsBinderAlbumCard(cardUi))
        {
            ApplyFlatScreenCardPresentation(cardUi, card3d);
            return;
        }

        if (card3d == null && !cardUi.IsCard3dUIGroupSet())
        {
            return;
        }

        card3d ??= CardUiFieldAccess.GetValue(cardUi, "m_Card3dUIGroup") as Card3dUIGroup;

        if (!CardUiDisplayContext.ShouldUseRotatableWorldCardBack(cardUi)
            && !PackOpeningState.IsPackOpeningInProgress()
            && !PackOpeningState.IsFanRowVisible())
        {
            ApplyFlatScreenCardPresentation(cardUi, card3d);
            return;
        }

        if (PackOpeningState.IsPackOpeningInProgress())
        {
            if (card3d != null && PackOpeningState.ShouldShowFrontDuringPackFlip(card3d))
            {
                ApplyPackOpeningFlipFrontPresentation(cardUi, card3d);
                return;
            }

            if (card3d != null && PackOpeningState.ShouldShowActiveCardFlipBack(card3d))
            {
                ApplyPackOpeningActiveFlipBack(cardUi, card3d);
                return;
            }

            ConfigurePackOpeningCardPresentation(cardUi, card3d);
            return;
        }

        if (PackOpeningState.IsFanRowVisible() && card3d?.m_CardBackMesh != null)
        {
            ApplyPackOpeningFanRowPresentation(cardUi, card3d);
            return;
        }

        ConfigureCard3dForFrontDisplay(cardUi);
    }

    /// <summary>
    /// Shop display: front canvas + overlay; 3D back mesh with single-card UVs for viewing from behind.
    /// </summary>
    public static void ConfigureCard3dForFrontDisplay(CardUI cardUi)
    {
        Card3dUIGroup? card3d = CardUiDisplayContext.ResolveCard3dGroup(cardUi);
        if (card3d == null)
        {
            return;
        }

        InteractableCard3d? interactable = Card3dInteractableRegistry.FindForCardUi(cardUi);
        bool onDisplayShelf = interactable != null && interactable.IsDisplayedOnShelf();
        if (onDisplayShelf)
        {
            InteractableCard3d0703Patches.AlignDisplayCardUiToSlot(interactable!);
        }

        SetCardFrontCanvasActive(cardUi, active: true);
        SetCardFrontMirrored(cardUi, mirrored: false);
        PrepareShopDisplayCardBack(cardUi);
        ApplyShopDisplayBackMesh(cardUi, card3d, onDisplayShelf);
        if (onDisplayShelf)
        {
            EnsureDisplayCardRenderPriority(cardUi);
        }
    }

    private static void ApplyShopDisplayBackMesh(CardUI cardUi, Card3dUIGroup card3d, bool onDisplayShelf = false)
    {
        if (card3d.m_CardBackMesh == null)
        {
            return;
        }

        if (onDisplayShelf)
        {
            // m_CardBackMesh is not parented under m_CardUIAnimGrp, so it does not inherit the
            // shelf orientation flip and paints the blue Pokemon back over the front row.
            SetCard3dBackMeshVisible(card3d, visible: false);
            card3d.m_CardBackMesh.SetActive(false);
            return;
        }

        SyncCard3dBackMeshFromUiBack(cardUi, card3d, overscan: 1f, usePackStackBack: false);
        card3d.m_CardBackMesh.SetActive(true);
        SetCard3dBackMeshVisible(card3d, visible: true);
    }

    private const float DisplayBackUiOverscanScale = 1.14f;
    private const float DisplayBackUiBleedPixels = 14f;

    /// <summary>Opaque Pokemon back on m_CardBack (opposite face from the front overlay).</summary>
    private static void PrepareShopDisplayCardBack(CardUI cardUi)
    {
        if (cardUi.m_CardBackImage == null)
        {
            return;
        }

        Sprite? backSprite = ResolveShopDisplayBackSprite(cardUi);
        if (backSprite == null)
        {
            return;
        }

        if (cardUi.m_CardBack != null)
        {
            cardUi.m_CardBack.SetActive(true);
        }

        SetCardBackMirrored(cardUi, mirrored: false);
        SetCardBackFlipped(cardUi, flipped: false);
        cardUi.m_CardBackImage.enabled = true;
        cardUi.m_CardBackImage.sprite = backSprite;
        cardUi.m_CardBackImage.type = Image.Type.Simple;
        cardUi.m_CardBackImage.preserveAspect = false;
        cardUi.m_CardBackImage.color = Color.white;
        StretchShopDisplayBackImage(cardUi, cardUi.m_CardBackImage.rectTransform);
    }

    /// <summary>
    /// ExpansionMod SetCardBacks leaves the full atlas on m_CardBackImage.
    /// Prefer expansion-specific backs for Destiny/Trainer/Ghost; Pokemon TR CardBack for Tetramon.
    /// </summary>
    private static Sprite? ResolveShopDisplayBackSprite(CardUI cardUi)
    {
        CardData? cardData = cardUi.GetCardData();
        if (cardData != null && cardData.expansionType != ECardExpansionType.Tetramon)
        {
            try
            {
                Sprite? expansionBack = CSingleton<InventoryBase>.Instance.m_MonsterData_SO
                    .GetCardBackSprite(cardData.expansionType);
                if (expansionBack != null && expansionBack.rect.width > 1f && expansionBack.rect.height > 1f)
                {
                    return expansionBack;
                }
            }
            catch
            {
                // Fall through to shared Pokemon/UI backs.
            }
        }

        Sprite? backSprite = CardExtrasCacheAccess.TryGetPokemonUiBackSprite()
            ?? CardExtrasCacheAccess.TryGetUiCardBackSprite();
        if (backSprite != null)
        {
            return backSprite;
        }

        Sprite? existing = cardUi.m_CardBackImage != null ? cardUi.m_CardBackImage.sprite : null;
        if (existing != null && existing.rect.width > 1f && existing.rect.height > 1f)
        {
            return existing;
        }

        if (cardData != null)
        {
            return CSingleton<InventoryBase>.Instance.m_MonsterData_SO.GetCardBackSprite(cardData.expansionType);
        }

        return null;
    }

    /// <summary>Shop / held cards: fix atlas UVs on the back mesh without forcing it visible.</summary>
    public static void SyncShopBackMeshUvOnly(CardUI cardUi, Card3dUIGroup? card3d = null)
    {
        card3d ??= CardUiFieldAccess.GetValue(cardUi, "m_Card3dUIGroup") as Card3dUIGroup;
        if (card3d?.m_CardBackMesh == null)
        {
            return;
        }

        SyncCard3dBackMeshFromUiBack(cardUi, card3d, overscan: 1f, usePackStackBack: false);
    }

    /// <summary>After ExpansionMod SetCardBacks on shop display cards.</summary>
    public static void SyncTetramonCardBackAfterExpansionMod(CardUI cardUi, Card3dUIGroup card3d)
    {
        ConfigureCard3dForFrontDisplay(cardUi);
    }

    /// <summary>
    /// Keep vanilla hover dimming while preventing the solid green type-background from bleeding through overlays.
    /// </summary>
    public static void SuppressTetramonHoverChromeBleed(CardUI cardUi)
    {
        if (cardUi == null || cardUi.GetCardData()?.expansionType != ECardExpansionType.Tetramon)
        {
            return;
        }

        if (!CardUiDisplayContext.IsFlatAlbumOrBinderCard(cardUi) || !HasActiveTetramonOverlay(cardUi))
        {
            return;
        }

        SetImageEnabled(cardUi.m_CardBGImage, false);
        SetImageEnabled(cardUi.m_CardFrontImage, false);
        SetImageEnabled(cardUi.m_CardFrontImageTopLayer, false);
        SetImageEnabled(cardUi.m_CardBorderImage, false);
        SetImageEnabled(cardUi.m_CardFullBGImage, false);
        SetImageEnabled(cardUi.m_CardFullTransparentLayerBGImage, false);
        SetImageEnabled(cardUi.m_PlayEffectBGImage, false);
        SetImageEnabled(cardUi.m_BrightnessControl, true);
    }

    private const int DisplayCardRenderQueue = 3005;

    private static void EnsureDisplayCardRenderPriority(CardUI cardUi)
    {
        Canvas? canvas = cardUi.GetComponentInParent<Canvas>();
        if (canvas != null && canvas.renderMode == RenderMode.WorldSpace)
        {
            canvas.overrideSorting = true;
            if (canvas.sortingOrder < 10)
            {
                canvas.sortingOrder = 10;
            }
        }

        BoostImageRenderQueue(cardUi.m_CardFrontImage);
        BoostImageRenderQueue(cardUi.m_BrightnessControl);
        BoostImageRenderQueue(cardUi.m_CardBackImage);

        if (FindOverlayTransform(cardUi) is Transform overlayTransform
            && overlayTransform.TryGetComponent(out Image overlay))
        {
            BoostImageRenderQueue(overlay);
        }
    }

    private static void BoostImageRenderQueue(Image? image)
    {
        if (image == null)
        {
            return;
        }

        Material? material = image.materialForRendering;
        if (material != null && material.renderQueue < DisplayCardRenderQueue)
        {
            material.renderQueue = DisplayCardRenderQueue;
        }
    }

    private static bool HasActiveTetramonOverlay(CardUI cardUi)
    {
        return FindOverlayTransform(cardUi) is Transform overlayTransform
            && overlayTransform.TryGetComponent(out Image overlay)
            && overlay.enabled;
    }

    private static void SuppressFlatUiCardBack(CardUI cardUi)
    {
        DisableUiCardBackCover(cardUi);

        if (cardUi.m_CardBack != null)
        {
            cardUi.m_CardBack.SetActive(false);
        }
    }

    public static void ApplyFlatScreenCardPresentation(CardUI cardUi, Card3dUIGroup? card3d = null)
    {
        SuppressFlatUiCardBack(cardUi);
        SetCardFrontCanvasActive(cardUi, active: true);
        SetCardFrontMirrored(cardUi, mirrored: false);

        card3d ??= CardUiDisplayContext.ResolveCard3dGroup(cardUi);
        if (card3d?.m_CardBackMesh != null)
        {
            card3d.m_CardBackMesh.SetActive(false);
        }
    }

    private static void SetCardFrontCanvasActive(CardUI cardUi, bool active)
    {
        if (cardUi.m_CardFront != null)
        {
            cardUi.m_CardFront.SetActive(active);
        }
    }

    /// <summary>
    /// During the pack flip the card never physically rotates to face the camera (facing stays back-to-camera
    /// in this port), so its front canvas renders horizontally mirrored. Negating the front canvas X scale
    /// cancels that mirror; every other context (fan row, shop, held) views the front normally and resets it.
    /// </summary>
    private static void SetCardFrontMirrored(CardUI cardUi, bool mirrored)
    {
        SetTransformMirrored(cardUi.m_CardFront != null ? cardUi.m_CardFront.transform : null, mirrored);
    }

    /// <summary>
    /// The UI back canvas sits on the same card and views the same way as the front, so during the open it
    /// renders horizontally reversed exactly like the front does. Mirror it the same way so the blue Pokemon
    /// back reads correctly; reset it for shop/held contexts.
    /// </summary>
    private static void SetCardBackMirrored(CardUI cardUi, bool mirrored)
    {
        SetTransformMirrored(cardUi.m_CardBack != null ? cardUi.m_CardBack.transform : null, mirrored);
    }

    /// <summary>
    /// Rotate the UI back canvas 180 deg about Y so it faces the camera while the card is face-down at the rip.
    /// Unlike a scale flip, rotating actually changes transform.forward, which is what backface culling keys on.
    /// Reset to identity for every other context so the canvas tracks the card normally.
    /// </summary>
    private static void SetCardBackFlipped(CardUI cardUi, bool flipped)
    {
        if (cardUi.m_CardBack == null)
        {
            return;
        }

        Transform t = cardUi.m_CardBack.transform;
        Quaternion target = flipped ? Quaternion.Euler(0f, 180f, 0f) : Quaternion.identity;
        if (Quaternion.Angle(t.localRotation, target) > 0.1f)
        {
            t.localRotation = target;
        }
    }

    private static void SetTransformMirrored(Transform? target, bool mirrored)
    {
        if (target == null)
        {
            return;
        }

        Vector3 scale = target.localScale;
        float magnitudeX = Mathf.Abs(scale.x);
        float targetX = mirrored ? -magnitudeX : magnitudeX;
        if (!Mathf.Approximately(scale.x, targetX))
        {
            target.localScale = new Vector3(targetX, scale.y, scale.z);
        }
    }

    /// <summary>Pack flip: show 3D back mesh with correct sprite UVs aligned to the card face.</summary>
    public static void ApplyPackFlipBackMeshSync(CardUI cardUi, Card3dUIGroup? card3d = null)
    {
        ApplyPackStackBackMeshSync(cardUi, card3d);
    }

    /// <summary>After ExpansionMod SetCardBacks during pack open: fix UVs only — never call SetCardBacks again.</summary>
    public static void SyncTetramonPackBackAfterExpansionMod(CardUI cardUi, Card3dUIGroup card3d)
    {
        ConfigurePackOpeningCardPresentation(cardUi, card3d);
    }

    private static void EnsurePackUiCardBackCover(CardUI cardUi)
    {
        Sprite? backSprite = CardExtrasCacheAccess.TryGetUiCardBackSprite()
            ?? cardUi.m_CardBackImage?.sprite;
        if (backSprite == null)
        {
            return;
        }

        if (cardUi.m_CardBack != null)
        {
            cardUi.m_CardBack.SetActive(true);
        }

        if (cardUi.m_CardBackImage == null)
        {
            return;
        }

        cardUi.m_CardBackImage.enabled = true;
        cardUi.m_CardBackImage.sprite = backSprite;
        cardUi.m_CardBackImage.type = Image.Type.Simple;
        cardUi.m_CardBackImage.preserveAspect = false;
        cardUi.m_CardBackImage.color = Color.white;
        StretchImageToFill(cardUi.m_CardBackImage.rectTransform);
    }

    private static void SuppressPackOpeningFoilMask(CardUI cardUi)
    {
        if (CardUiFieldAccess.GetValue(cardUi, "m_CardFoilMaskImage") is Image foilMask)
        {
            foilMask.enabled = false;
        }
    }

    private static void SuppressFrontOverlayDuringPackBack(CardUI cardUi)
    {
        if (FindOverlayTransform(cardUi) is Transform overlayTransform
            && overlayTransform.TryGetComponent(out Image overlay))
        {
            overlay.enabled = false;
        }
    }

    private static void DisableUiCardBackCover(CardUI cardUi)
    {
        if (cardUi.m_CardBackImage != null)
        {
            cardUi.m_CardBackImage.enabled = false;
        }
    }

    private static void SetCard3dBackMeshVisible(Card3dUIGroup card3d, bool visible)
    {
        if (card3d.m_CardBackMesh == null)
        {
            return;
        }

        Renderer? renderer = card3d.m_CardBackMesh.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.enabled = visible;
        }
    }

    private static void SyncCard3dBackMeshFromUiBack(
        CardUI cardUi,
        Card3dUIGroup card3d,
        float overscan = 1f,
        bool usePackStackBack = true)
    {
        Sprite? backSprite;
        if (usePackStackBack)
        {
            backSprite = CardExtrasCacheAccess.TryGetStackBackMeshSprite()
                ?? cardUi.m_CardBackImage?.sprite;
        }
        else
        {
            backSprite = ResolveShopDisplayBackSprite(cardUi)
                ?? cardUi.m_CardBackImage?.sprite;
        }

        if (backSprite == null || card3d.m_CardBackMesh == null)
        {
            return;
        }

        Renderer? renderer = card3d.m_CardBackMesh.GetComponent<Renderer>();
        Material? material = renderer != null ? renderer.material : null;
        if (material == null)
        {
            return;
        }

        ApplySpriteToMaterial(material, backSprite, overscan);

        card3d.m_CardBackMesh.transform.localScale = overscan > 1f
            ? Vector3.one * overscan
            : Vector3.one;
    }

    private static void StretchImageToFill(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.localScale = Vector3.one;
    }

    private static void StretchShopDisplayBackImage(CardUI cardUi, RectTransform rect)
    {
        if (cardUi.m_CardBack != null)
        {
            rect.SetParent(cardUi.m_CardBack.transform, false);
        }

        StretchImageToFill(rect);
        rect.offsetMin = new Vector2(-DisplayBackUiBleedPixels, -DisplayBackUiBleedPixels);
        rect.offsetMax = new Vector2(DisplayBackUiBleedPixels, DisplayBackUiBleedPixels);
        rect.localRotation = Quaternion.identity;
        rect.localScale = Vector3.one * DisplayBackUiOverscanScale;
    }

    private static void ApplySpriteToMaterial(Material material, Sprite sprite, float overscan = 1f)
    {
        Texture texture = sprite.texture;
        Rect rect = sprite.rect;
        float invWidth = 1f / texture.width;
        float invHeight = 1f / texture.height;
        Vector2 scale = new(rect.width * invWidth / overscan, rect.height * invHeight / overscan);
        Vector2 offset = new(
            rect.x * invWidth + (scale.x * (overscan - 1f) * 0.5f),
            rect.y * invHeight + (scale.y * (overscan - 1f) * 0.5f));

        material.mainTexture = texture;
        material.mainTextureScale = scale;
        material.mainTextureOffset = offset;

        if (material.HasProperty("_BaseMap"))
        {
            material.SetTexture("_BaseMap", texture);
            material.SetTextureScale("_BaseMap", scale);
            material.SetTextureOffset("_BaseMap", offset);
        }

        if (material.HasProperty("_EmissionMap"))
        {
            material.SetTexture("_EmissionMap", texture);
            material.SetTextureScale("_EmissionMap", scale);
            material.SetTextureOffset("_EmissionMap", offset);
        }
    }

    private static Sprite? ResolveCardArt(CardUI cardUi, CardData cardData, out bool fromBridge)
    {
        fromBridge = false;

        // ArtExpander cache is authoritative; center frame may hold a wrongly scaled interim sprite.
        Sprite? bridgeArt = ArtExpanderBridge.LoadCardArt(cardData);
        if (bridgeArt != null)
        {
            fromBridge = true;
            return bridgeArt;
        }

        if (cardUi.m_CenterFrameImage == null || !cardUi.m_CenterFrameImage.enabled)
        {
            return null;
        }

        Sprite? fromCenter = cardUi.m_CenterFrameImage.sprite;
        if (fromCenter != null && !IsVanillaIcon(fromCenter, cardData))
        {
            return fromCenter;
        }

        return null;
    }

    private static bool IsVanillaIcon(Sprite sprite, CardData cardData)
    {
        MonsterData monsterData = InventoryBase.GetMonsterData(cardData.monsterType);
        Sprite? icon = monsterData.GetIcon(cardData.expansionType);
        return icon != null && ReferenceEquals(sprite, icon);
    }

    private static bool LooksLikeFullCard(Sprite sprite)
    {
        float width = sprite.rect.width;
        float height = sprite.rect.height;
        if (width <= 1f || height <= 1f)
        {
            return false;
        }

        if (height >= FullCardMinHeight && width >= FullCardMinWidth)
        {
            return true;
        }

        if (height > width * 1.1f && height >= 350f)
        {
            return true;
        }

        // Portrait scans that fall below strict thresholds but are clearly not center icons.
        return height >= 300f && height >= width * 0.85f;
    }

    private static void ApplyFullCardOverlay(CardUI cardUi, Sprite cardArt)
    {
        EnsureCardShellVisible(cardUi);
        CleanupLegacyPackArtifacts(cardUi);

        Image target = GetOrCreateOverlayImage(cardUi);
        target.sprite = cardArt;
        target.type = Image.Type.Simple;
        target.enabled = true;
        target.preserveAspect = true;
        target.color = Color.white;
        target.raycastTarget = false;
        target.maskable = true;

        StretchOverlayToCardFront(cardUi, target.rectTransform);
        RemoveFrontBlocker(cardUi);
        target.rectTransform.SetAsLastSibling();

        HideCenterFrameArt(cardUi);
        HideVanillaChromeWhenOverlayShown(cardUi);
        HideDuplicateTextWhenOverlayShown(cardUi);
        SetImageEnabled(cardUi.m_BrightnessControl, false);

        if (CardUiDisplayContext.IsBinderAlbumCard(cardUi))
        {
            // Album: bind HO foil hosts to overlay art so holographics modulate the face.
            EnsureAlbumArtAndFoilLayering(cardUi);
        }
        else
        {
            // Shop / trade / pack: same foil policy — unbound HO hosts scramble Destiny/Trainer.
            target.material = null;
            EnsureAlbumArtAndFoilLayering(cardUi);
            ReapplyGradedCaseLayout(cardUi);
        }

        if (!LoggedFirstFullCard)
        {
            LoggedFirstFullCard = true;
            Plugin.Log.LogInfo(
                $"Pokemon full-card art from cardart.assets ({cardArt.name}, {cardArt.rect.width}x{cardArt.rect.height}).");
        }
    }

    private static void ApplyCenterArtLayout(CardUI cardUi, Sprite centerArt, object? cardConfig)
    {
        DisableOverlayImage(cardUi);

        if (cardUi.m_CenterFrameImageGrp != null)
        {
            cardUi.m_CenterFrameImageGrp.SetActive(true);
        }

        if (cardUi.m_CenterFrameMaskGrp != null)
        {
            cardUi.m_CenterFrameMaskGrp.SetActive(true);
        }

        if (cardUi.m_CenterFrameImage != null)
        {
            cardUi.m_CenterFrameImage.enabled = true;
            cardUi.m_CenterFrameImage.sprite = centerArt;
            cardUi.m_CenterFrameImage.preserveAspect = true;
            cardUi.m_CenterFrameImage.color = Color.white;
        }

        ApplyCenterFrameTransform(cardUi, cardConfig);
        ApplyTextFromConfig(cardUi, cardConfig, forceReadableFallback: true);

        if (!LoggedFirstCenterArt)
        {
            LoggedFirstCenterArt = true;
            Plugin.Log.LogInfo(
                $"Pokemon center art from cardart.assets ({centerArt.name}, {centerArt.rect.width}x{centerArt.rect.height}).");
        }
    }

    private static void ApplyCenterFrameTransform(CardUI cardUi, object? cardConfig)
    {
        if (!TryGetMonsterImageLayout(cardConfig, out Vector2 configSize, out Vector2 configPosition))
        {
            ResetCenterFrameTransform(cardUi);
            return;
        }

        if (configSize.x <= 1f || configSize.y <= 1f)
        {
            ResetCenterFrameTransform(cardUi);
            return;
        }

        RectTransform? artRect = cardUi.m_CenterFrameImage != null
            ? cardUi.m_CenterFrameImage.rectTransform
            : null;
        if (artRect == null)
        {
            return;
        }

        artRect.sizeDelta = configSize;
        artRect.anchoredPosition = configPosition;
        artRect.localScale = Vector3.one;

        if (cardUi.m_CenterFrameImageGrp != null)
        {
            Transform artGrp = cardUi.m_CenterFrameImageGrp.transform;
            artGrp.localScale = Vector3.one;
            artGrp.localPosition = Vector3.zero;
        }

        if (cardUi.m_CenterFrameMaskGrp != null)
        {
            Transform maskGrp = cardUi.m_CenterFrameMaskGrp.transform;
            maskGrp.localScale = Vector3.one;
            maskGrp.localPosition = Vector3.zero;
        }
    }

    private static void ResetCenterFrameTransform(CardUI cardUi)
    {
        if (cardUi.m_CenterFrameImage != null)
        {
            RectTransform artRect = cardUi.m_CenterFrameImage.rectTransform;
            artRect.localScale = Vector3.one;
            artRect.anchoredPosition = Vector2.zero;
        }

        if (cardUi.m_CenterFrameImageGrp != null)
        {
            Transform artGrp = cardUi.m_CenterFrameImageGrp.transform;
            artGrp.localScale = Vector3.one;
            artGrp.localPosition = Vector3.zero;
        }

        if (cardUi.m_CenterFrameMaskGrp != null)
        {
            Transform maskGrp = cardUi.m_CenterFrameMaskGrp.transform;
            maskGrp.localScale = Vector3.one;
            maskGrp.localPosition = Vector3.zero;
        }
    }

    private static void StretchOverlayToCardFront(CardUI cardUi, RectTransform overlayRect)
    {
        RectTransform? frontRoot = GetCardFrontRect(cardUi);
        if (frontRoot == null)
        {
            return;
        }

        RectTransform? template = cardUi.m_CardBorderImage?.rectTransform
            ?? cardUi.m_CardFrontImage?.rectTransform;

        overlayRect.SetParent(frontRoot, false);
        overlayRect.localRotation = Quaternion.identity;

        if (template != null && template != frontRoot)
        {
            CopyRectTransformLayout(template, overlayRect);
            return;
        }

        StretchImageToFill(overlayRect);
    }

    private static void CopyRectTransformLayout(RectTransform source, RectTransform target)
    {
        target.anchorMin = source.anchorMin;
        target.anchorMax = source.anchorMax;
        target.pivot = source.pivot;
        target.anchoredPosition = source.anchoredPosition;
        target.sizeDelta = source.sizeDelta;
        target.localScale = Vector3.one;
    }

    private static RectTransform? GetCardFrontRect(CardUI cardUi)
    {
        if (cardUi.m_CardFront != null)
        {
            return cardUi.m_CardFront.transform as RectTransform;
        }

        return cardUi.transform as RectTransform;
    }

    private static void RemoveFrontBlocker(CardUI cardUi)
    {
        Transform? blockerTransform = GetCardFrontTransform(cardUi).Find("TetramonFrontBlocker0703");
        if (blockerTransform != null)
        {
            blockerTransform.gameObject.SetActive(false);
        }
    }

    private static Transform GetCardFrontTransform(CardUI cardUi)
    {
        if (cardUi.m_CardFront != null)
        {
            return cardUi.m_CardFront.transform;
        }

        return cardUi.transform;
    }

    private static void EnsureCardShellVisible(CardUI cardUi)
    {
        if (cardUi.m_CardFront != null)
        {
            cardUi.m_CardFront.SetActive(true);
        }

        if (CardUiDisplayContext.ShouldUseRotatableWorldCardBack(cardUi) && cardUi.m_CardBack != null)
        {
            cardUi.m_CardBack.SetActive(true);
        }
    }

    private static void CleanupLegacyPackArtifacts(CardUI cardUi)
    {
        Transform? legacyCover = cardUi.transform.Find("TetramonPackBackCover0703");
        if (legacyCover != null)
        {
            UnityEngine.Object.Destroy(legacyCover.gameObject);
        }
    }

    private static Transform? FindOverlayTransform(CardUI cardUi)
    {
        Transform? underFront = GetCardFrontTransform(cardUi).Find("TetramonOverlay0703");
        if (underFront != null)
        {
            return underFront;
        }

        return cardUi.transform.Find("TetramonOverlay0703");
    }

    private static Image GetOrCreateOverlayImage(CardUI cardUi)
    {
        if (FindOverlayTransform(cardUi) is Transform overlayTransform
            && overlayTransform.TryGetComponent(out Image cachedOverlay))
        {
            return cachedOverlay;
        }

        GameObject overlayObject = new("TetramonOverlay0703");
        RectTransform? frontRoot = GetCardFrontRect(cardUi);
        overlayObject.transform.SetParent(frontRoot != null ? frontRoot : cardUi.transform, false);
        Image image = overlayObject.AddComponent<Image>();
        image.maskable = true;
        StretchImageToFill(image.rectTransform);
        return image;
    }

    private static void DisableOverlayImage(CardUI cardUi)
    {
        if (FindOverlayTransform(cardUi) is Transform overlayTransform
            && overlayTransform.TryGetComponent(out Image overlay))
        {
            overlay.enabled = false;
        }

        RemoveFrontBlocker(cardUi);
    }

    private static void RestoreCenterFrameVisibility(CardUI cardUi)
    {
        if (cardUi.m_CenterFrameImageGrp != null)
        {
            cardUi.m_CenterFrameImageGrp.SetActive(true);
        }

        if (cardUi.m_CenterFrameMaskGrp != null)
        {
            cardUi.m_CenterFrameMaskGrp.SetActive(true);
        }

        if (cardUi.m_CenterFrameImage != null)
        {
            cardUi.m_CenterFrameImage.enabled = true;
        }
    }

    private static void RestoreVanillaChromeVisibility(CardUI cardUi)
    {
        SetImageEnabled(cardUi.m_CardFrontImage, true);
        SetImageEnabled(cardUi.m_CardFrontImageTopLayer, true);
        SetImageEnabled(cardUi.m_CardBGImage, true);
        SetImageEnabled(cardUi.m_CardBorderImage, true);
        SetImageEnabled(cardUi.m_CardFullBGImage, true);
        SetGameObjectActive(cardUi.m_CardBGImage?.gameObject, true);
        SetGameObjectActive(cardUi.m_CardFullBGImage?.gameObject, true);
        SetImageEnabled(cardUi.m_CardFullTransparentLayerBGImage, true);
        SetImageEnabled(cardUi.m_RarityImage, true);
        SetImageEnabled(cardUi.m_FadeBarTopImage, true);
        SetImageEnabled(cardUi.m_FadeBarBtmImage, true);
        SetImageEnabled(cardUi.m_StatImage, true);
        SetImageEnabled(cardUi.m_EvoBGImage, true);
        SetImageEnabled(cardUi.m_DescriptionBGImage, true);
        SetImageEnabled(cardUi.m_PlayEffectBGImage, true);
        SetImageEnabled(cardUi.m_BrightnessControl, true);

        SetGameObjectActive(cardUi.m_FadeBarTopImage?.gameObject, true);
        SetGameObjectActive(cardUi.m_FadeBarBtmImage?.gameObject, true);
        SetGameObjectActive(cardUi.m_DescriptionBGImage?.gameObject, true);
        SetGameObjectActive(cardUi.m_StatImage?.gameObject, true);
        SetGameObjectActive(cardUi.m_PlayEffectBGImage?.gameObject, true);
        SetGameObjectActive(cardUi.m_EvoBGImage?.gameObject, true);

        if (CardUiFieldAccess.GetValue(cardUi, "m_StatGrp") is GameObject statGrp)
        {
            statGrp.SetActive(true);
        }

        if (CardUiFieldAccess.GetValue(cardUi, "m_DescriptionGrp") is GameObject descriptionGrp)
        {
            descriptionGrp.SetActive(true);
        }

        if (CardUiFieldAccess.GetValue(cardUi, "m_EvoGrp") is GameObject evoGrpOnly)
        {
            evoGrpOnly.SetActive(true);
        }

        if (CardUiFieldAccess.GetValue(cardUi, "m_EvoBasicGrp") is GameObject evoBasicGrp)
        {
            evoBasicGrp.SetActive(true);
        }

        if (cardUi.m_CardBorderMask != null)
        {
            cardUi.m_CardBorderMask.enabled = true;
        }

        if (CardUiFieldAccess.GetValue(cardUi, "m_EvoAndArtistNameGrp") is GameObject evoGrp)
        {
            evoGrp.SetActive(true);
        }
    }

    private static void RestoreDuplicateTextVisibility(CardUI cardUi)
    {
        foreach (string fieldName in CardTextFieldNames)
        {
            SetBehaviourEnabled(GetCardUiFieldValue(cardUi, fieldName), enabled: true);
        }
    }

    private static void HideCenterFrameArt(CardUI cardUi)
    {
        if (cardUi.m_CenterFrameImage != null)
        {
            cardUi.m_CenterFrameImage.enabled = false;
        }

        if (cardUi.m_CenterFrameImageGrp != null)
        {
            cardUi.m_CenterFrameImageGrp.SetActive(false);
        }

        if (cardUi.m_CenterFrameMaskGrp != null)
        {
            cardUi.m_CenterFrameMaskGrp.SetActive(false);
        }
    }

    private static void RestoreCenterFrameIcon(CardUI cardUi, CardData cardData)
    {
        DisableOverlayImage(cardUi);

        if (cardUi.m_CenterFrameImageGrp != null)
        {
            cardUi.m_CenterFrameImageGrp.SetActive(true);
        }

        if (cardUi.m_CenterFrameMaskGrp != null)
        {
            cardUi.m_CenterFrameMaskGrp.SetActive(true);
        }

        if (cardUi.m_CenterFrameImage == null)
        {
            return;
        }

        cardUi.m_CenterFrameImage.enabled = true;
        MonsterData monsterData = InventoryBase.GetMonsterData(cardData.monsterType);
        Sprite? icon = monsterData.GetIcon(cardData.expansionType);
        if (icon != null)
        {
            cardUi.m_CenterFrameImage.sprite = icon;
            cardUi.m_CenterFrameImage.preserveAspect = true;
        }

        ResetCenterFrameTransform(cardUi);
    }

    private static void ApplyNoArtFallback(CardUI cardUi, object? cardConfig, CardData cardData)
    {
        ApplyTextFromConfig(cardUi, cardConfig, forceReadableFallback: true);
        SetImageEnabled(cardUi.m_StatImage, false);

        if (!LoggedMissingArt)
        {
            LoggedMissingArt = true;
            Plugin.Log.LogWarning(
                $"No cardart.assets entry for '{cardData.monsterType}' border '{cardData.borderType}'. " +
                "Showing icon + config text.");
        }
    }

    private static void ApplyTextFromConfig(CardUI cardUi, object? cardConfig, bool forceReadableFallback)
    {
        if (cardConfig == null)
        {
            SetBehaviourEnabled(GetCardUiFieldValue(cardUi, "m_MonsterNameText"), true);
            SetBehaviourEnabled(GetCardUiFieldValue(cardUi, "m_DescriptionText"), true);
            return;
        }

        Type configType = cardConfig.GetType();
        foreach ((string textProperty, string enabledProperty, string fieldName, bool forceOnFallback) in ConfigTextBindings)
        {
            object? textComponent = GetCardUiFieldValue(cardUi, fieldName);
            PropertyInfo? textValueProperty = configType.GetProperty(textProperty);
            PropertyInfo? enabledValueProperty = configType.GetProperty(enabledProperty);

            if (textValueProperty != null && textComponent != null)
            {
                SetComponentText(textComponent, textValueProperty.GetValue(cardConfig)?.ToString());
            }

            bool enabled = enabledValueProperty?.GetValue(cardConfig) is bool configEnabled && configEnabled;
            if (forceReadableFallback && forceOnFallback)
            {
                enabled = true;
            }

            SetBehaviourEnabled(textComponent, enabled);
        }
    }

    private static void SetComponentText(object component, string? value)
    {
        if (component == null || string.IsNullOrEmpty(value))
        {
            return;
        }

        PropertyInfo? textProperty = component.GetType().GetProperty("text");
        textProperty?.SetValue(component, value);
    }

    private static void HideVanillaChromeWhenOverlayShown(CardUI cardUi)
    {
        SetImageEnabled(cardUi.m_CardFrontImage, false);
        SetImageEnabled(cardUi.m_CardFrontImageTopLayer, false);
        SetImageEnabled(cardUi.m_CardBGImage, false);
        SetImageEnabled(cardUi.m_CardBorderImage, false);
        SetImageEnabled(cardUi.m_CardFullBGImage, false);
        SetGameObjectActive(cardUi.m_CardBGImage?.gameObject, false);
        SetGameObjectActive(cardUi.m_CardFullBGImage?.gameObject, false);
        SetImageEnabled(cardUi.m_CardFullTransparentLayerBGImage, false);
        SetImageEnabled(cardUi.m_RarityImage, false);
        SetImageEnabled(cardUi.m_FadeBarTopImage, false);
        SetImageEnabled(cardUi.m_FadeBarBtmImage, false);
        SetImageEnabled(cardUi.m_StatImage, false);
        SetImageEnabled(cardUi.m_EvoBGImage, false);
        SetImageEnabled(cardUi.m_DescriptionBGImage, false);
        SetImageEnabled(cardUi.m_PlayEffectBGImage, false);
        SetImageEnabled(cardUi.m_BrightnessControl, false);

        SetGameObjectActive(cardUi.m_FadeBarTopImage?.gameObject, false);
        SetGameObjectActive(cardUi.m_FadeBarBtmImage?.gameObject, false);
        SetGameObjectActive(cardUi.m_DescriptionBGImage?.gameObject, false);
        SetGameObjectActive(cardUi.m_StatImage?.gameObject, false);
        SetGameObjectActive(cardUi.m_PlayEffectBGImage?.gameObject, false);
        SetGameObjectActive(cardUi.m_EvoBGImage?.gameObject, false);

        if (CardUiFieldAccess.GetValue(cardUi, "m_StatGrp") is GameObject statGrp)
        {
            statGrp.SetActive(false);
        }

        if (CardUiFieldAccess.GetValue(cardUi, "m_DescriptionGrp") is GameObject descriptionGrp)
        {
            descriptionGrp.SetActive(false);
        }

        if (CardUiFieldAccess.GetValue(cardUi, "m_EvoGrp") is GameObject evoGrpOnly)
        {
            evoGrpOnly.SetActive(false);
        }

        if (CardUiFieldAccess.GetValue(cardUi, "m_EvoBasicGrp") is GameObject evoBasicGrp)
        {
            evoBasicGrp.SetActive(false);
        }

        if (cardUi.m_CardBorderMask != null)
        {
            cardUi.m_CardBorderMask.enabled = false;
        }

        if (CardUiFieldAccess.GetValue(cardUi, "m_EvoAndArtistNameGrp") is GameObject evoGrp)
        {
            evoGrp.SetActive(false);
        }
    }

    private static void HideDuplicateTextWhenOverlayShown(CardUI cardUi)
    {
        foreach (string fieldName in CardTextFieldNames)
        {
            SetBehaviourEnabled(GetCardUiFieldValue(cardUi, fieldName));
        }
    }

    private static object? GetCardUiFieldValue(CardUI cardUi, string fieldName)
    {
        return CardUiFieldAccess.GetValue(cardUi, fieldName);
    }

    private static void SetGameObjectActive(GameObject? gameObject, bool active)
    {
        if (gameObject != null)
        {
            gameObject.SetActive(active);
        }
    }

    private static void SetImageEnabled(Image? image, bool enabled)
    {
        if (image != null)
        {
            image.enabled = enabled;
        }
    }

    private static void SetBehaviourEnabled(object? component, bool enabled = false)
    {
        if (component is Behaviour behaviour)
        {
            behaviour.enabled = enabled;
        }
    }

    private static bool TryGetMonsterImageLayout(object? cardConfig, out Vector2 size, out Vector2 position)
    {
        size = Vector2.zero;
        position = Vector2.zero;
        if (cardConfig == null)
        {
            return false;
        }

        Type configType = cardConfig.GetType();
        if (configType.GetProperty("MonsterImageSize")?.GetValue(cardConfig) is Vector2 configSize)
        {
            size = configSize;
        }

        if (configType.GetProperty("MonsterImagePosition")?.GetValue(cardConfig) is Vector2 configPosition)
        {
            position = configPosition;
        }

        return size != Vector2.zero || position != Vector2.zero;
    }
}
