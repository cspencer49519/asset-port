using BepInEx;

using BepInEx.Logging;

using HarmonyLib;

using System;

using System.Reflection;

using TCGShopExpansionMod.Handlers;

using TCGShopExpansionMod071Patch.Patches;



namespace TCGShopExpansionMod071Patch;



[BepInPlugin(PluginGuid, PluginName, PluginVersion)]

[BepInDependency("com.DarkDragoon.TCGShopExpansionMod", BepInDependency.DependencyFlags.HardDependency)]

[BepInDependency("cklapperich.ArtExpander", BepInDependency.DependencyFlags.SoftDependency)]

public sealed class Plugin : BaseUnityPlugin

{

    public const string PluginGuid = "local.tcgshop.expansionmod071patch";

    public const string PluginName = "TCGShopExpansionMod 0.71 Patch";

    public const string PluginVersion = "1.0.99";



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

        harmony.PatchAll(typeof(PackOpening071Patches));

        harmony.PatchAll(typeof(MonsterData071Patches));

        harmony.PatchAll(typeof(InteractableCard3d071Patches));

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



        MethodInfo? initOpenSequence = AccessTools.Method(playerPatches, "InitOpenSequence_Postfix");

        if (initOpenSequence != null)
        {
            harmony.Patch(
                initOpenSequence,
                prefix: new HarmonyMethod(typeof(PlayerPatches071Patches), nameof(PlayerPatches071Patches.InitOpenSequence_BlockExpansionPackBack_Prefix)));
        }
        else
        {
            Log.LogWarning("Could not patch InitOpenSequence_Postfix pack-back paint.");
        }



        MethodInfo? mainPostfix = AccessTools.Method(playerPatches, "CardUI_SetCardUI_Main_Postfix");



        if (mainPostfix != null)

        {

            harmony.Patch(

                mainPostfix,

                prefix: new HarmonyMethod(typeof(TetramonOverlay071Patches), nameof(TetramonOverlay071Patches.SkipMainPostfixForTetramon_Prefix)));

        }

        else

        {

            Log.LogWarning("Could not patch CardUI_SetCardUI_Main_Postfix for Tetramon skip.");

        }



        TryPatchExtrasHandlerCardBacks(harmony);

        gameObject.AddComponent<PackOpeningLateSyncBehaviour>();

        ArtExpanderBridge.TryInitialize();

        Log.LogInfo($"Patched ExpansionMod for game 0.71 (Tetramon overlay + safe card UI hooks). v{PluginVersion}");

    }



    private static void TryPatchExtrasHandlerCardBacks(Harmony harmony)

    {

        System.Type? extrasHandler = AccessTools.TypeByName("TCGShopExpansionMod.Handlers.ExtrasHandler");

        if (extrasHandler == null)

        {

            Log.LogWarning("Could not patch ExtrasHandler card back methods.");

            return;

        }



        TryPatchMethod(
            harmony,
            AccessTools.Method(extrasHandler, "SetCardBacks"),
            nameof(ExtrasHandler071Patches.SetCardBacks_Postfix));

        TryPatchMethod(
            harmony,
            AccessTools.Method(extrasHandler, "SetCardBackPackOpening"),
            nameof(ExtrasHandler071Patches.SetCardBackPackOpening_Postfix));

    }



    private static void TryPatchMethod(
        Harmony harmony,
        MethodInfo? original,
        string postfixName,
        string? prefixName = null)
    {
        if (original == null)
        {
            Log.LogWarning($"Could not find ExtrasHandler method for patch {postfixName}.");
            return;
        }

        try
        {
            HarmonyMethod? prefix = prefixName != null
                ? new HarmonyMethod(typeof(ExtrasHandler071Patches), prefixName)
                : null;
            harmony.Patch(
                original,
                prefix: prefix,
                postfix: new HarmonyMethod(typeof(ExtrasHandler071Patches), postfixName));
        }
        catch (Exception ex)
        {
            Log.LogError($"Failed to patch {original.DeclaringType?.Name}.{original.Name}: {ex.Message}");
        }
    }

}


