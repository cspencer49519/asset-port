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

    public static void SetCardUI_ApplyTetramonOverlay(CardUI __instance, CardData cardData)
    {
        if (__instance == null || cardData == null || cardData.expansionType != ECardExpansionType.Tetramon)
        {
            return;
        }

        Sprite? cardArt = ResolveCardArt(__instance, cardData);
        object? cardConfig = NewSwappingHandler.TryGetCardFromCache(cardData);

        if (cardArt != null && LooksLikeFullCard(cardArt))
        {
            ApplyFullCardOverlay(__instance, cardArt);
            return;
        }

        DisableOverlayImage(__instance);

        if (cardArt != null)
        {
            ApplyCenterArtLayout(__instance, cardArt, cardConfig);
            return;
        }

        RestoreCenterFrameIcon(__instance, cardData);
        ApplyNoArtFallback(__instance, cardConfig, cardData);
    }

    private static Sprite? ResolveCardArt(CardUI cardUi, CardData cardData)
    {
        // ArtExpander cache is authoritative; center frame may hold a wrongly scaled interim sprite.
        Sprite? fromBridge = ArtExpanderBridge.LoadCardArt(cardData);
        if (fromBridge != null)
        {
            return fromBridge;
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
        if (height >= FullCardMinHeight && width >= FullCardMinWidth)
        {
            return true;
        }

        return height > width * 1.1f && height >= 350f;
    }

    private static void ApplyFullCardOverlay(CardUI cardUi, Sprite cardArt)
    {
        Image target = GetOrCreateOverlayImage(cardUi);
        target.sprite = cardArt;
        target.type = Image.Type.Simple;
        target.enabled = true;
        target.preserveAspect = true;
        target.color = Color.white;
        target.raycastTarget = false;
        target.maskable = true;

        StretchOverlayToCardFront(cardUi, target.rectTransform);
        target.rectTransform.SetAsLastSibling();

        HideCenterFrameArt(cardUi);
        HideVanillaChromeWhenOverlayShown(cardUi);
        HideDuplicateTextWhenOverlayShown(cardUi);

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
        RectTransform cardRoot = cardUi.transform as RectTransform;
        if (cardRoot == null)
        {
            return;
        }

        overlayRect.SetParent(cardRoot, false);
        overlayRect.anchorMin = Vector2.zero;
        overlayRect.anchorMax = Vector2.one;
        overlayRect.pivot = new Vector2(0.5f, 0.5f);
        overlayRect.offsetMin = Vector2.zero;
        overlayRect.offsetMax = Vector2.zero;
        overlayRect.localScale = Vector3.one;
        overlayRect.localRotation = Quaternion.identity;
    }

    private static Image GetOrCreateOverlayImage(CardUI cardUi)
    {
        Transform? overlayTransform = cardUi.transform.Find("TetramonOverlay071");
        if (overlayTransform != null && overlayTransform.TryGetComponent(out Image cachedOverlay))
        {
            return cachedOverlay;
        }

        GameObject overlayObject = new("TetramonOverlay071");
        overlayObject.transform.SetParent(cardUi.transform, false);
        Image image = overlayObject.AddComponent<Image>();
        image.maskable = true;
        return image;
    }

    private static void DisableOverlayImage(CardUI cardUi)
    {
        Transform? overlayTransform = cardUi.transform.Find("TetramonOverlay071");
        if (overlayTransform != null && overlayTransform.TryGetComponent(out Image overlay))
        {
            overlay.enabled = false;
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
        SetImageEnabled(cardUi.m_RarityImage, false);
        SetImageEnabled(cardUi.m_FadeBarTopImage, false);
        SetImageEnabled(cardUi.m_FadeBarBtmImage, false);
        SetImageEnabled(cardUi.m_StatImage, false);
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
