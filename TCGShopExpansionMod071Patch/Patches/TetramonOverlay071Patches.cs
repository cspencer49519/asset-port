using System;
using System.Reflection;
using TCGShopExpansionMod.Handlers;
using UnityEngine;
using UnityEngine.UI;

namespace TCGShopExpansionMod071Patch.Patches;

/// <summary>
/// Game 0.71 removed CardUI.m_MonsterImage. Pokemon/Tetramon art comes from ArtExpander cardart.assets.
/// </summary>
internal static class TetramonOverlay071Patches
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

    /// <summary>Skip ExpansionMod HandleCards for Tetramon on 0.71 (uses removed CardUI fields).</summary>
    public static bool SkipMainPostfixForTetramon_Prefix(CardUI __instance, CardData cardData)
    {
        if (cardData != null && cardData.expansionType == ECardExpansionType.Tetramon)
        {
            return false;
        }

        return true;
    }

    public static void SetCardUI_ApplyTetramonOverlay(CardUI __instance, CardData cardData, bool forceFrontOverlay = false)
    {
        if (__instance == null || cardData == null || cardData.expansionType != ECardExpansionType.Tetramon)
        {
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

        ApplyTetramonFrontOverlay(__instance, cardData);
    }

    /// <summary>Route each pack card to stack-top back, flip front, or hidden-in-stack.</summary>
    public static void ConfigurePackOpeningCardPresentation(CardUI cardUi, Card3dUIGroup? card3d = null)
    {
        card3d ??= CardUiDisplayContext.ResolveCard3dGroup(cardUi);

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

        if (card3d != null && PackOpeningState.ShouldShowPackBackFace(card3d))
        {
            ApplyPackOpeningStackTopBack(cardUi, card3d);
            return;
        }

        if (PackOpeningState.IsPackFlipState())
        {
            ApplyPackOpeningHiddenInStack(cardUi, card3d);
            return;
        }

        ApplyPackOpeningHiddenInStack(cardUi, card3d);
    }

    /// <summary>Pack rip: stacked Pokemon back on the top-of-deck card (ExpansionMod last slot).</summary>
    private static void ApplyPackOpeningStackTopBack(CardUI cardUi, Card3dUIGroup? card3d = null)
    {
        DisableOverlayImage(cardUi);
        HideCenterFrameArt(cardUi);
        HideVanillaChromeWhenOverlayShown(cardUi);
        HideDuplicateTextWhenOverlayShown(cardUi);

        card3d ??= CardUiDisplayContext.ResolveCard3dGroup(cardUi);
        SetPackCardUiAnimGroupVisible(card3d, visible: true);
        SetCardFrontCanvasActive(cardUi, active: false);
        SuppressFrontOverlayDuringPackBack(cardUi);
        EnsurePackUiCardBackCover(cardUi);
        ApplyPackStackBackMeshSync(cardUi, card3d);
    }

    /// <summary>Active card at the start of its flip: single Pokemon back before the front reveal.</summary>
    private static void ApplyPackOpeningActiveFlipBack(CardUI cardUi, Card3dUIGroup? card3d = null)
    {
        DisableOverlayImage(cardUi);
        HideCenterFrameArt(cardUi);
        HideVanillaChromeWhenOverlayShown(cardUi);
        HideDuplicateTextWhenOverlayShown(cardUi);

        card3d ??= CardUiDisplayContext.ResolveCard3dGroup(cardUi);
        SetPackCardUiAnimGroupVisible(card3d, visible: true);
        SetCardFrontCanvasActive(cardUi, active: false);
        SuppressFrontOverlayDuringPackBack(cardUi);
        PreparePackSingleCardBackImage(cardUi);
        ApplyPackSingleCardBackMeshSync(cardUi, card3d);
    }

    private static void PreparePackSingleCardBackImage(CardUI cardUi)
    {
        if (cardUi.m_CardBackImage == null)
        {
            return;
        }

        Sprite? backSprite = cardUi.m_CardBackImage.sprite;
        if (backSprite == null && cardUi.GetCardData() is CardData cardData)
        {
            backSprite = CSingleton<InventoryBase>.Instance.m_MonsterData_SO.GetCardBackSprite(cardData.expansionType);
        }

        if (backSprite == null)
        {
            return;
        }

        SetCardBackCanvasActive(cardUi, active: true);
        cardUi.m_CardBackImage.enabled = true;
        cardUi.m_CardBackImage.sprite = backSprite;
        cardUi.m_CardBackImage.type = Image.Type.Simple;
        cardUi.m_CardBackImage.preserveAspect = true;
        cardUi.m_CardBackImage.color = Color.white;
        StretchImageToFill(cardUi.m_CardBackImage.rectTransform);
    }

    /// <summary>Card currently flipping: front canvas only, no back mesh or UI back cover.</summary>
    public static void ApplyPackOpeningFlipFrontPresentation(CardUI cardUi, Card3dUIGroup? card3d = null)
    {
        card3d ??= CardUiDisplayContext.ResolveCard3dGroup(cardUi);
        SetPackCardUiAnimGroupVisible(card3d, visible: true);
        SetCardFrontCanvasActive(cardUi, active: true);
        DisableUiCardBackCover(cardUi);
        SetCardBackCanvasActive(cardUi, active: false);
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
        HidePackOpeningBackMesh(card3d);
    }

    /// <summary>Restore card UI shell before applying front overlays in the fan row (state 7+).</summary>
    public static void ApplyPackOpeningFanRowPresentation(CardUI cardUi, Card3dUIGroup? card3d = null)
    {
        card3d ??= CardUiDisplayContext.ResolveCard3dGroup(cardUi);
        SetPackCardUiAnimGroupVisible(card3d, visible: true);
        SetCardFrontCanvasActive(cardUi, active: true);
        SetCardBackCanvasActive(cardUi, active: false);
        DisableUiCardBackCover(cardUi);
        HidePackOpeningBackMesh(card3d);
        ApplyFlatScreenCardPresentation(cardUi, card3d);
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

    private static void HidePackOpeningBackMesh(Card3dUIGroup? card3d)
    {
        if (card3d?.m_CardBackMesh == null)
        {
            return;
        }

        SetCard3dBackMeshVisible(card3d, visible: false);
        card3d.m_CardBackMesh.SetActive(false);
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
        SyncCard3dBackMeshFromUiBack(cardUi, card3d, overscan: 1f, usePackStackBack: false);
    }

    private static void ApplyTetramonFrontOverlay(CardUI __instance, CardData cardData)
    {
        SetCardFrontCanvasActive(__instance, active: true);

        Card3dUIGroup? card3d = CardUiDisplayContext.ResolveCard3dGroup(__instance);
        if (CardUiDisplayContext.IsBinderAlbumCard(__instance))
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

        Sprite? cardArt = ResolveCardArt(__instance, cardData, out bool fromBridge);
        object? cardConfig = NewSwappingHandler.TryGetCardFromCache(cardData);

        if (cardArt != null && (fromBridge || LooksLikeFullCard(cardArt)))
        {
            ApplyFullCardOverlay(__instance, cardArt);
            FinalizeCard3dPresentation(__instance);
            return;
        }

        DisableOverlayImage(__instance);

        if (cardArt != null)
        {
            ApplyCenterArtLayout(__instance, cardArt, cardConfig);
            FinalizeCard3dPresentation(__instance);
            return;
        }

        RestoreCenterFrameIcon(__instance, cardData);
        ApplyNoArtFallback(__instance, cardConfig, cardData);
        FinalizeCard3dPresentation(__instance);
    }

    /// <summary>
    /// Pack opening: keep the 3D back mesh alive with correct Pokemon sprite UVs.
    /// Stack cards waiting to flip also get an opaque UI back cover so the previous card front cannot bleed through.
    /// </summary>
    public static void SyncPackOpeningBackMesh(CardUI cardUi, Card3dUIGroup? card3d = null)
    {
        card3d ??= CardUiFieldAccess.GetValue(cardUi, "m_Card3dUIGroup") as Card3dUIGroup;
        if (card3d == null || !cardUi.IsCard3dUIGroupSet())
        {
            return;
        }

        ApplyPackFlipBackMeshSync(cardUi, card3d);

        if (PackOpeningState.ShouldShowPackBackFace(card3d))
        {
            SuppressFrontOverlayDuringPackBack(cardUi);
        }
        else if (PackOpeningState.ShouldShowActiveCardFlipBack(card3d))
        {
            ApplyPackOpeningActiveFlipBack(cardUi, card3d);
        }
        else if (PackOpeningState.ShouldShowFrontDuringPackFlip(card3d))
        {
            ApplyPackOpeningFlipFrontPresentation(cardUi, card3d);
        }
        else
        {
            DisableUiCardBackCover(cardUi);
            HidePackOpeningBackMesh(card3d);
        }
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

        SetCardFrontCanvasActive(cardUi, active: true);
        PrepareShopDisplayCardBack(cardUi);
        ApplyShopDisplayBackMesh(cardUi, card3d);
    }

    private static void ApplyShopDisplayBackMesh(CardUI cardUi, Card3dUIGroup card3d)
    {
        if (card3d.m_CardBackMesh == null)
        {
            return;
        }

        SyncCard3dBackMeshFromUiBack(cardUi, card3d, overscan: 1f, usePackStackBack: false);
        card3d.m_CardBackMesh.SetActive(true);
        SetCard3dBackMeshVisible(card3d, visible: true);
    }

    /// <summary>Opaque Pokemon back on m_CardBack (opposite face from the front overlay).</summary>
    private static void PrepareShopDisplayCardBack(CardUI cardUi)
    {
        if (cardUi.m_CardBackImage == null)
        {
            return;
        }

        Sprite? backSprite = cardUi.m_CardBackImage.sprite;
        if (backSprite == null && cardUi.GetCardData() is CardData cardData)
        {
            backSprite = CSingleton<InventoryBase>.Instance.m_MonsterData_SO.GetCardBackSprite(cardData.expansionType);
        }

        if (backSprite == null)
        {
            return;
        }

        if (cardUi.m_CardBack != null)
        {
            cardUi.m_CardBack.SetActive(true);
        }

        cardUi.m_CardBackImage.enabled = true;
        cardUi.m_CardBackImage.sprite = backSprite;
        cardUi.m_CardBackImage.type = Image.Type.Simple;
        cardUi.m_CardBackImage.preserveAspect = false;
        cardUi.m_CardBackImage.color = Color.white;
        StretchImageToFill(cardUi.m_CardBackImage.rectTransform);
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
        SetCardFrontCanvasActive(cardUi, active: true);
        PrepareShopDisplayCardBack(cardUi);
        ApplyShopDisplayBackMesh(cardUi, card3d);
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

    /// <summary>Pack opening: opaque UI back cover using the stack-back sprite.</summary>
    private static void EnsurePackUiCardBackCover(CardUI cardUi)
    {
        Sprite? backSprite = CardExtrasCacheAccess.TryGetCachedSprite("T_CardBackMesh")
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

    /// <summary>
    /// Pack opening only: apply Pokemon back UVs to the 3D back mesh. Does not touch UI back image.
    /// </summary>
    private static void SyncCard3dBackMeshFromUiBack(
        CardUI cardUi,
        Card3dUIGroup card3d,
        float overscan = 1f,
        bool usePackStackBack = true)
    {
        Sprite? backSprite = usePackStackBack
            ? CardExtrasCacheAccess.TryGetCachedSprite("T_CardBackMesh") ?? cardUi.m_CardBackImage?.sprite
            : cardUi.m_CardBackImage?.sprite;
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
        Transform? blockerTransform = GetCardFrontTransform(cardUi).Find("TetramonFrontBlocker071");
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
        Transform? legacyCover = cardUi.transform.Find("TetramonPackBackCover071");
        if (legacyCover != null)
        {
            UnityEngine.Object.Destroy(legacyCover.gameObject);
        }
    }

    private static Transform? FindOverlayTransform(CardUI cardUi)
    {
        Transform? underFront = GetCardFrontTransform(cardUi).Find("TetramonOverlay071");
        if (underFront != null)
        {
            return underFront;
        }

        return cardUi.transform.Find("TetramonOverlay071");
    }

    private static Image GetOrCreateOverlayImage(CardUI cardUi)
    {
        if (FindOverlayTransform(cardUi) is Transform overlayTransform
            && overlayTransform.TryGetComponent(out Image cachedOverlay))
        {
            return cachedOverlay;
        }

        GameObject overlayObject = new("TetramonOverlay071");
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
