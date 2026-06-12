using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using System.Reflection;
using TCGShopExpansionMod.Handlers;
using TCGShopExpansionMod071Patch.Patches;

namespace TCGShopExpansionMod071Patch;

[BepInPlugin(PluginGuid, PluginName, PluginVersion)]
[BepInDependency("com.DarkDragoon.TCGShopExpansionMod", BepInDependency.DependencyFlags.HardDependency)]
public sealed class Plugin : BaseUnityPlugin
{
    public const string PluginGuid = "local.tcgshop.expansionmod071patch";
    public const string PluginName = "TCGShopExpansionMod 0.71 Patch";
    public const string PluginVersion = "1.0.2";

    internal static ManualLogSource Log { get; private set; } = null!;

    private void Awake()
    {
        Log = Logger;
        Harmony harmony = new(PluginGuid);

        MethodInfo setCardExtras = AccessTools.Method(typeof(NewSwappingHandler), nameof(NewSwappingHandler.SetCardExtrasImages));
        if (setCardExtras == null)
        {
            Log.LogError("Could not find NewSwappingHandler.SetCardExtrasImages.");
            return;
        }

        harmony.Patch(
            setCardExtras,
            prefix: new HarmonyMethod(typeof(NewSwappingHandler071Patches), nameof(NewSwappingHandler071Patches.SetCardExtrasImages_Prefix)));

        harmony.PatchAll(typeof(CardUI071Patches));
        harmony.PatchAll(typeof(LightManager071Patches));

        System.Type? playerPatches = AccessTools.TypeByName("TCGShopExpansionMod.Patches.PlayerPatches");
        if (playerPatches == null)
        {
            Log.LogError("Could not find TCGShopExpansionMod.Patches.PlayerPatches.");
            return;
        }

        MethodInfo? cardUiPostfix = AccessTools.Method(playerPatches, "CardUI_SetCardUI_Postfix");
        if (cardUiPostfix != null)
        {
            harmony.Patch(
                cardUiPostfix,
                prefix: new HarmonyMethod(typeof(PlayerPatches071Patches), nameof(PlayerPatches071Patches.CardUI_SetCardUI_Postfix_Prefix)));
        }
        else
        {
            Log.LogWarning("Could not patch CardUI_SetCardUI_Postfix.");
        }

        MethodInfo? lightManagerPrefix = AccessTools.Method(playerPatches, "LightManager_Awake_Prefix");
        if (lightManagerPrefix != null)
        {
            harmony.Patch(
                lightManagerPrefix,
                prefix: new HarmonyMethod(typeof(PlayerPatches071Patches), nameof(PlayerPatches071Patches.LightManager_Awake_Prefix_Prefix)));
        }
        else
        {
            Log.LogWarning("Could not patch LightManager_Awake_Prefix.");
        }

        Log.LogInfo("Patched ExpansionMod for game 0.71 (skip SetCardExtrasImages + safe card UI hooks).");
    }
}
