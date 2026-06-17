using UnityEngine;
using UnityEngine.SceneManagement;

namespace TCGShopExpansionMod071Patch;

/// <summary>
/// Never use CSingleton&lt;PhoneManager&gt;.Instance for phone UI repair: it can create a broken
/// runtime singleton before scene refs are wired (see PhoneManager.Awake NRE on m_BarcodeScannerMesh).
/// </summary>
internal static class PhoneManagerAccess
{
    private static PhoneManager? cachedPhoneManager;

    static PhoneManagerAccess()
    {
        SceneManager.sceneLoaded += (_, __) => InvalidateCache();
        SceneManager.sceneUnloaded += _ => InvalidateCache();
    }

    public static void InvalidateCache()
    {
        cachedPhoneManager = null;
    }

    public static PhoneManager? FindPhoneManager()
    {
        if (cachedPhoneManager != null)
        {
            return cachedPhoneManager;
        }

        cachedPhoneManager = Object.FindObjectOfType<PhoneManager>();
        return cachedPhoneManager;
    }

    public static PhoneManager? TryGetReadyPhoneManager()
    {
        PhoneManager? phoneManager = FindPhoneManager();
        if (!IsReady(phoneManager))
        {
            return null;
        }

        return phoneManager;
    }

    public static bool IsReady(PhoneManager? phoneManager)
    {
        return phoneManager != null
            && phoneManager.m_PhoneGrp != null
            && phoneManager.m_UI_PhoneScreen != null
            && phoneManager.m_BarcodeScannerMesh != null;
    }
}
