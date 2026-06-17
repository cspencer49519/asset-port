using UnityEngine;

namespace TCGShopExpansionMod071Patch;

/// <summary>
/// Identifies transforms that belong to phone apps, including screens not parented under m_PhoneGrp.
/// </summary>
internal static class PhoneAppUiScope
{
    private static readonly string[] PhoneScreenRootNameTokens =
    {
        "UI_PhoneScreen",
        "RestockItem",
        "RentBill",
        "ScannerRestock",
        "FurnitureShop",
        "ExpansionShop",
        "CheckPrice",
        "HireWorker",
        "CustomerReview",
        "BuyDeco",
        "Decoration",
        "GradeCard",
        "HostTournament",
        "CameraPhone",
        "PhotoGallery",
        "BarcodeScanner",
        "Checkout",
        "AddToCart",
        "GameEvent",
        "SortUI",
    };

    public static bool IsPhoneAppTransform(Transform? transform)
    {
        if (transform == null)
        {
            return false;
        }

        PhoneManager? phoneManager = PhoneManagerAccess.FindPhoneManager();
        if (phoneManager?.m_PhoneGrp != null && transform.IsChildOf(phoneManager.m_PhoneGrp))
        {
            return true;
        }

        if (phoneManager != null && IsTransformUnderPhoneManagerScreen(transform, phoneManager))
        {
            return true;
        }

        return HasPhoneScreenRootName(transform);
    }

    public static System.Collections.Generic.IEnumerable<Transform> EnumeratePhoneUiRoots(PhoneManager phoneManager)
    {
        if (phoneManager.m_PhoneGrp != null)
        {
            yield return phoneManager.m_PhoneGrp;
        }

        foreach (UIScreenBase? screen in EnumeratePhoneManagerScreens(phoneManager))
        {
            if (screen != null)
            {
                yield return screen.transform;
            }
        }

        if (phoneManager.m_CameraPhoneModeUIScreen != null)
        {
            yield return phoneManager.m_CameraPhoneModeUIScreen.transform;
        }
    }

    public static Transform? ResolvePhoneScreenContentRoot(Transform? fromTransform)
    {
        UIScreenBase? screen = FindOwnerScreen(fromTransform);
        if (screen?.m_ScreenGroup != null)
        {
            return screen.m_ScreenGroup.transform;
        }

        return ResolvePhoneAppScreenRoot(fromTransform);
    }

    public static UIScreenBase? FindOwnerScreen(Transform? fromTransform)
    {
        if (fromTransform == null)
        {
            return null;
        }

        UIScreenBase? direct = fromTransform.GetComponent<UIScreenBase>();
        if (direct != null)
        {
            return direct;
        }

        PhoneManager? phoneManager = PhoneManagerAccess.FindPhoneManager();
        if (phoneManager != null)
        {
            foreach (UIScreenBase? screen in EnumeratePhoneManagerScreens(phoneManager))
            {
                if (screen == null)
                {
                    continue;
                }

                if (fromTransform == screen.transform || fromTransform.IsChildOf(screen.transform))
                {
                    return screen;
                }

                if (screen.m_ScreenGroup != null)
                {
                    Transform screenGroup = screen.m_ScreenGroup.transform;
                    if (fromTransform == screenGroup || fromTransform.IsChildOf(screenGroup))
                    {
                        return screen;
                    }
                }
            }
        }

        return fromTransform.GetComponentInParent<UIScreenBase>();
    }

    public static Transform? ResolvePhoneAppScreenRoot(Transform? fromTransform)
    {
        if (fromTransform == null)
        {
            return null;
        }

        PhoneManager? phoneManager = PhoneManagerAccess.FindPhoneManager();
        if (phoneManager != null)
        {
            foreach (UIScreenBase? screen in EnumeratePhoneManagerScreens(phoneManager))
            {
                if (screen == null)
                {
                    continue;
                }

                Transform screenTransform = screen.transform;
                if (fromTransform == screenTransform || fromTransform.IsChildOf(screenTransform))
                {
                    return screenTransform;
                }

                if (screen.m_ScreenGroup != null)
                {
                    Transform screenGroup = screen.m_ScreenGroup.transform;
                    if (fromTransform == screenGroup || fromTransform.IsChildOf(screenGroup))
                    {
                        return screenTransform;
                    }
                }
            }

            CameraPhoneModeUIScreen? cameraModeScreen = phoneManager.m_CameraPhoneModeUIScreen;
            if (cameraModeScreen != null)
            {
                Transform cameraTransform = cameraModeScreen.transform;
                if (fromTransform == cameraTransform || fromTransform.IsChildOf(cameraTransform))
                {
                    return cameraTransform;
                }

                if (cameraModeScreen.m_ScreenGrp != null)
                {
                    Transform screenGroup = cameraModeScreen.m_ScreenGrp.transform;
                    if (fromTransform == screenGroup || fromTransform.IsChildOf(screenGroup))
                    {
                        return cameraTransform;
                    }
                }
            }

            if (phoneManager.m_PhoneGrp != null && fromTransform.IsChildOf(phoneManager.m_PhoneGrp))
            {
                return phoneManager.m_PhoneGrp;
            }
        }

        Transform? namedRoot = FindNamedPhoneScreenRoot(fromTransform);
        return namedRoot ?? fromTransform;
    }

    public static bool IsPhoneRelatedScreen(UIScreenBase? screen)
    {
        if (screen == null)
        {
            return false;
        }

        if (IsPhoneAppTransform(screen.transform))
        {
            return true;
        }

        PhoneManager? phoneManager = PhoneManagerAccess.FindPhoneManager();
        if (phoneManager == null)
        {
            return false;
        }

        foreach (UIScreenBase? registered in EnumeratePhoneManagerScreens(phoneManager))
        {
            if (registered != null && ReferenceEquals(screen, registered))
            {
                return true;
            }
        }

        return false;
    }

    private static bool HasPhoneScreenRootName(Transform transform)
    {
        Transform? node = transform;
        while (node != null)
        {
            string name = node.name;
            foreach (string token in PhoneScreenRootNameTokens)
            {
                if (name.IndexOf(token, System.StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return true;
                }
            }

            node = node.parent;
        }

        return false;
    }

    private static Transform? FindNamedPhoneScreenRoot(Transform fromTransform)
    {
        Transform? bestMatch = null;
        Transform? node = fromTransform;
        while (node != null)
        {
            if (HasPhoneScreenRootName(node))
            {
                bestMatch = node;
            }

            node = node.parent;
        }

        return bestMatch;
    }

    private static bool IsTransformUnderPhoneManagerScreen(Transform transform, PhoneManager phoneManager)
    {
        foreach (UIScreenBase? screen in EnumeratePhoneManagerScreens(phoneManager))
        {
            if (screen != null && IsTransformUnderScreen(transform, screen))
            {
                return true;
            }
        }

        CameraPhoneModeUIScreen? cameraModeScreen = phoneManager.m_CameraPhoneModeUIScreen;
        if (cameraModeScreen == null)
        {
            return false;
        }

        if (transform == cameraModeScreen.transform || transform.IsChildOf(cameraModeScreen.transform))
        {
            return true;
        }

        if (cameraModeScreen.m_ScreenGrp == null)
        {
            return false;
        }

        Transform screenGroup = cameraModeScreen.m_ScreenGrp.transform;
        return transform == screenGroup || transform.IsChildOf(screenGroup);
    }

    private static bool IsTransformUnderScreen(Transform transform, UIScreenBase screen)
    {
        if (transform == screen.transform || transform.IsChildOf(screen.transform))
        {
            return true;
        }

        if (screen.m_ScreenGroup == null)
        {
            return false;
        }

        Transform screenGroup = screen.m_ScreenGroup.transform;
        return transform == screenGroup || transform.IsChildOf(screenGroup);
    }

    private static System.Collections.Generic.IEnumerable<UIScreenBase?> EnumeratePhoneManagerScreens(PhoneManager phoneManager)
    {
        foreach (UIScreenBase? screen in GetPhoneManagerScreens(phoneManager))
        {
            if (screen == null)
            {
                continue;
            }

            yield return screen;

            if (screen is RestockItemScreen restockItemScreen)
            {
                yield return restockItemScreen.m_RestockItemAddToCartScreen;
                yield return restockItemScreen.m_RestockItemCheckoutScreen;
                yield return restockItemScreen.m_SortUIScreen;
            }
        }
    }

    private static UIScreenBase?[] GetPhoneManagerScreens(PhoneManager phoneManager)
    {
        return new UIScreenBase?[]
        {
            phoneManager.m_UI_PhoneScreen,
            phoneManager.m_RestockItemScreen,
            phoneManager.m_RestockItemBoardGameScreen,
            phoneManager.m_FurnitureShopUIScreen,
            phoneManager.m_ExpandShopUIScreen,
            phoneManager.m_SetGameEventUIScreen,
            phoneManager.m_CheckPriceScreen,
            phoneManager.m_HireWorkerScreen,
            phoneManager.m_RentBillScreen,
            phoneManager.m_CustomerReviewScreen,
            phoneManager.m_ShopBuyDecoUIScreen,
            phoneManager.m_GradeCardWebsiteUIScreen,
            phoneManager.m_ScannerRestockScreen,
            phoneManager.m_HostTournamentScreen,
            phoneManager.m_CameraPhoneScreen,
            phoneManager.m_CameraPhotoGalleryScreen,
        };
    }
}
