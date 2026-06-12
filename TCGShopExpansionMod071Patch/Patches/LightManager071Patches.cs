using System;
using HarmonyLib;

namespace TCGShopExpansionMod071Patch.Patches;

internal static class LightManager071Patches
{
    [HarmonyFinalizer]
    [HarmonyPatch(typeof(LightManager), "Awake")]
    public static Exception Awake_Finalizer(Exception __exception)
    {
        if (__exception == null)
        {
            return null;
        }

        Plugin.Log.LogWarning($"Suppressed ExpansionMod LightManager.Awake error: {__exception.GetType().Name}");
        return null;
    }
}
