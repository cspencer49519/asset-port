using System;
using HarmonyLib;

namespace TCGShopExpansionMod071Patch.Patches;

internal static class CardUI071Patches
{
    [HarmonyFinalizer]
    [HarmonyPatch(typeof(CardUI), "SetCardUI")]
    public static Exception SetCardUI_Finalizer(Exception __exception)
    {
        if (__exception == null)
        {
            return null;
        }

        Plugin.Log.LogWarning($"Suppressed ExpansionMod SetCardUI error: {__exception.GetType().Name}");
        return null;
    }
}
