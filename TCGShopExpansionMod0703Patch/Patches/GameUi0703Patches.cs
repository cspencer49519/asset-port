using System;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace TCGShopExpansionMod0703Patch.Patches;

/// <summary>
/// Game HUD / cashier register fixes. Patched manually from Plugin (not PatchAll) so one missing
/// method cannot abort the rest of Awake.
/// </summary>
internal static class GameUi0703Patches
{
    public static int ApplyPatches(Harmony harmony)
    {
        int applied = 0;
        applied += TryPostfix(harmony, typeof(EndOfDayReportScreen), nameof(EndOfDayReportScreen.OnPressGoNextDay), nameof(EndOfDay_OnPressGoNextDay_Postfix));
        applied += TryPostfix(harmony, typeof(EndOfDayReportScreen), nameof(EndOfDayReportScreen.OnPressGoNextButton), nameof(EndOfDay_OnPressGoNextButton_Postfix));
        applied += TryPostfix(harmony, typeof(EndOfDayReportScreen), "DelayGoNextDay", nameof(EndOfDay_DelayGoNextDay_Postfix));

        applied += TryPostfix(harmony, typeof(UI_CashCounterScreen), "OnEnable", nameof(CashCounter_OnEnable_Postfix));
        applied += TryPostfix(harmony, typeof(UI_CashCounterScreen), nameof(UI_CashCounterScreen.Init), nameof(CashCounter_Init_Postfix));
        applied += TryPostfix(harmony, typeof(UI_CashCounterScreen), nameof(UI_CashCounterScreen.OnItemScanned), nameof(CashCounter_OnItemScanned_Postfix));
        applied += TryPostfix(harmony, typeof(UI_CashCounterScreen), nameof(UI_CashCounterScreen.OnCardScanned), nameof(CashCounter_OnCardScanned_Postfix));

        // UI_CreditCardScreen has no OnEnable on 0.70.3 — only EnableCreditCardMode.
        applied += TryPostfix(harmony, typeof(UI_CreditCardScreen), nameof(UI_CreditCardScreen.EnableCreditCardMode), nameof(CreditCard_EnableCreditCardMode_Postfix));

        applied += TryPostfix(harmony, typeof(UI_CheckoutItemBar), nameof(UI_CheckoutItemBar.AddScannedItem), nameof(CheckoutItemBar_AddScannedItem_Postfix));

        Plugin.Log.LogInfo($"Game UI patches applied ({applied}).");
        return applied;
    }

    private static int TryPostfix(Harmony harmony, Type targetType, string methodName, string patchMethodName)
    {
        try
        {
            MethodInfo? target = AccessTools.Method(targetType, methodName);
            if (target == null)
            {
                Plugin.Log.LogWarning($"Game UI skip: {targetType.Name}.{methodName} not found.");
                return 0;
            }

            MethodInfo? patch = AccessTools.Method(typeof(GameUi0703Patches), patchMethodName);
            if (patch == null)
            {
                Plugin.Log.LogWarning($"Game UI skip: patch method {patchMethodName} missing.");
                return 0;
            }

            harmony.Patch(target, postfix: new HarmonyMethod(patch));
            return 1;
        }
        catch (Exception ex)
        {
            Plugin.Log.LogWarning($"Game UI patch {targetType.Name}.{methodName} failed: {ex.GetType().Name}: {ex.Message}");
            return 0;
        }
    }

    public static void EndOfDay_OnPressGoNextDay_Postfix()
    {
        ClearNextDayPrompt("OnPressGoNextDay");
    }

    public static void EndOfDay_OnPressGoNextButton_Postfix()
    {
        ClearNextDayPrompt("OnPressGoNextButton");
    }

    public static void EndOfDay_DelayGoNextDay_Postfix()
    {
        ClearNextDayPrompt("DelayGoNextDay");
    }

    private static void ClearNextDayPrompt(string reason)
    {
        try
        {
            GameUIScreen.HideEnterGoNextDayIndicatorVisible();
            GameUIScreen.ResetEnterGoNextDayIndicatorVisible();

            GameUIScreen? screen = CSingleton<GameUIScreen>.Instance;
            if (screen == null)
            {
                return;
            }

            if (screen.m_PressEnterGoNextDayIndicator != null)
            {
                screen.m_PressEnterGoNextDayIndicator.SetActive(false);
            }

            if (screen.m_PressEnterGoNextDayIndicatorText != null)
            {
                screen.m_PressEnterGoNextDayIndicatorText.SetActive(false);
            }

            if (screen.m_GoNextDayIconGrp != null)
            {
                screen.m_GoNextDayIconGrp.gameObject.SetActive(false);
            }

            Plugin.Log.LogInfo($"Cleared next-day prompt ({reason}).");
        }
        catch (Exception ex)
        {
            Plugin.Log.LogWarning($"Clear next-day prompt failed: {ex.GetType().Name}: {ex.Message}");
        }
    }

    public static void CashCounter_OnEnable_Postfix(UI_CashCounterScreen __instance)
    {
        RepairCheckoutScreen(__instance != null ? __instance.transform : null, "CashCounter.OnEnable");
    }

    public static void CashCounter_Init_Postfix(UI_CashCounterScreen __instance)
    {
        RepairCheckoutScreen(__instance != null ? __instance.transform : null, "CashCounter.Init");
    }

    public static void CashCounter_OnItemScanned_Postfix(UI_CashCounterScreen __instance)
    {
        RepairCheckoutScreen(__instance != null ? __instance.transform : null, "CashCounter.OnItemScanned");
    }

    public static void CashCounter_OnCardScanned_Postfix(UI_CashCounterScreen __instance)
    {
        RepairCheckoutScreen(__instance != null ? __instance.transform : null, "CashCounter.OnCardScanned");
    }

    public static void CreditCard_EnableCreditCardMode_Postfix(UI_CreditCardScreen __instance)
    {
        RepairCheckoutScreen(__instance != null ? __instance.transform : null, "CreditCard.EnableCreditCardMode");
    }

    public static void CheckoutItemBar_AddScannedItem_Postfix(UI_CheckoutItemBar __instance)
    {
        RepairCheckoutScreen(__instance != null ? __instance.transform : null, "CheckoutItemBar.AddScannedItem");
    }

    private static void RepairCheckoutScreen(Transform? root, string reason)
    {
        if (root == null)
        {
            return;
        }

        try
        {
            if (!TutorialManager.IsGameUIVisible())
            {
                TutorialManager.SetGameUIVisible(isVisible: true);
                Plugin.Log.LogInfo($"Restored Game UI visibility during {reason}.");
            }

            InteractionPlayerController? ipc = CSingleton<InteractionPlayerController>.Instance;
            MaterialFadeInOut? fade = ipc != null ? ipc.m_BlackBGWorldUIFade : null;
            if (fade != null)
            {
                fade.SetFadeOut(0f);
            }

            if (root.gameObject != null && !root.gameObject.activeSelf)
            {
                root.gameObject.SetActive(true);
            }

            CashRegisterTmpRepair.RepairHierarchy(root, reason);
        }
        catch (Exception ex)
        {
            Plugin.Log.LogWarning($"Checkout UI repair failed ({reason}): {ex.GetType().Name}: {ex.Message}");
        }
    }
}
