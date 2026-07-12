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

[BepInDependency("shaklin.TextureReplacer", BepInDependency.DependencyFlags.HardDependency)]

[BepInDependency("cklapperich.ArtExpander", BepInDependency.DependencyFlags.SoftDependency)]

public sealed class Plugin : BaseUnityPlugin

{

    public const string PluginGuid = "local.tcgshop.expansionmod071patch";

    public const string PluginName = "TCGShopExpansionMod 0.71 Patch";

    public const string PluginVersion = "1.1.038";



    internal static ManualLogSource Log { get; private set; } = null!;



    private void Awake()

    {

        Log = Logger;
        PhoneFontMaterialSnapshot.CaptureIfNeeded();
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
        harmony.PatchAll(typeof(GradedCardSetCheckStatusScreen071Patches));
        TryPatchPhoneUi(harmony);
        TryPatchTextureReplacerGuards(harmony);



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



        MethodInfo? enterViewUpClose = AccessTools.Method(playerPatches, "CollectionBinderFlipAnimCtrl_EnterViewUpCloseState_Postfix");

        if (enterViewUpClose != null)

        {

            harmony.Patch(

                enterViewUpClose,

                prefix: new HarmonyMethod(typeof(PlayerPatches071Patches), nameof(PlayerPatches071Patches.EnterViewUpCloseState_Postfix_Prefix)));

        }

        else

        {

            Log.LogWarning("Could not patch CollectionBinderFlipAnimCtrl_EnterViewUpCloseState_Postfix.");

        }



        MethodInfo? openSortAlbumPostfix = AccessTools.Method(playerPatches, "CollectionBinderUI_OpenSortAlbumScreen_HarmonyPostfix");

        if (openSortAlbumPostfix != null)

        {

            harmony.Patch(

                openSortAlbumPostfix,

                prefix: new HarmonyMethod(typeof(PlayerPatches071Patches), nameof(PlayerPatches071Patches.OpenSortAlbumScreen_Postfix_Prefix)));

        }

        else

        {

            Log.LogWarning("Could not patch CollectionBinderUI_OpenSortAlbumScreen_HarmonyPostfix.");

        }



        TryPatchExtrasHandlerCardBacks(harmony);

        gameObject.AddComponent<PackOpeningLateSyncBehaviour>();
        gameObject.AddComponent<PhoneUiLateRepairBehaviour>();
        gameObject.AddComponent<PhoneUiRenderSyncBehaviour>();

        ArtExpanderBridge.TryInitialize();

        Log.LogInfo($"Patched ExpansionMod for game 0.71 (album/binder skips + HandleCards skip + Tetramon overlay). v{PluginVersion}");

    }



    private static void TryPatchPhoneUi(Harmony harmony)
    {
        try
        {
            harmony.PatchAll(typeof(PhoneUi071Patches));
            Log.LogInfo("Phone UI repair patches applied.");
        }
        catch (Exception ex)
        {
            Log.LogError($"Failed to apply phone UI repair patches: {ex.Message}");
        }
    }

    private static void TryPatchTextureReplacerGuards(Harmony harmony)
    {
        try
        {
            TextureReplacerMaterialGuardPatches.ApplyPatches(harmony);
            Log.LogInfo("TextureReplacer material guard patch applied.");
        }
        catch (Exception ex)
        {
            Log.LogWarning($"Failed to apply TextureReplacer material guard patch: {ex.Message}");
        }

        System.Type? textureReplacer = TextureReplacerPhoneUiGuardPatches.ResolveTextureReplacerPluginType();
        if (textureReplacer == null)
        {
            Log.LogWarning("TextureReplacer.BepInExPlugin type not found; skipping phone UI guard patches.");
            return;
        }

        int applied = 0;
        applied += TryPatchTextureReplacerMethod(
            harmony,
            textureReplacer,
            "GetCachedTexture",
            prefix: nameof(TextureReplacerPhoneUiGuardPatches.GetCachedTexture_Prefix)) ? 1 : 0;
        applied += TryPatchTextureReplacerMethod(
            harmony,
            textureReplacer,
            "GetCachedTexture_static",
            prefix: nameof(TextureReplacerPhoneUiGuardPatches.GetCachedTexture_Static_Prefix)) ? 1 : 0;
        applied += TryPatchTextureReplacerMethod(
            harmony,
            textureReplacer,
            "ForceWhiteIfNotGrayOrWhite",
            prefix: nameof(TextureReplacerPhoneUiGuardPatches.ForceWhiteIfNotGrayOrWhite_Prefix)) ? 1 : 0;
        applied += TryPatchTextureReplacerMethod(
            harmony,
            textureReplacer,
            "UpdateTitleTexts",
            prefix: nameof(TextureReplacerPhoneUiGuardPatches.UpdateTitleTexts_Prefix)) ? 1 : 0;
        applied += TryPatchTextureReplacerMethod(
            harmony,
            textureReplacer,
            "ReplaceItemDataInList",
            prefix: nameof(TextureReplacerPhoneUiGuardPatches.ReplaceItemDataInList_Prefix),
            postfix: nameof(TextureReplacerPhoneUiGuardPatches.ReplaceItemDataInList_Postfix)) ? 1 : 0;
        applied += TryPatchTextureReplacerMethod(
            harmony,
            textureReplacer,
            "FixPhone",
            postfix: nameof(TextureReplacerPhoneUiGuardPatches.FixPhone_Postfix)) ? 1 : 0;

        MethodInfo? doReplace = AccessTools.Method(textureReplacer, "DoReplace");
        if (doReplace == null)
        {
            Log.LogWarning("TextureReplacer.DoReplace not found; skipping phone font repair hook.");
        }
        else
        {
            try
            {
                harmony.Patch(
                    doReplace,
                    prefix: new HarmonyMethod(typeof(TextureReplacerPhoneUiGuardPatches), nameof(TextureReplacerPhoneUiGuardPatches.DoReplace_Prefix)),
                    finalizer: new HarmonyMethod(typeof(TextureReplacerPhoneUiGuardPatches), nameof(TextureReplacerPhoneUiGuardPatches.DoReplace_Finalizer)));
                applied++;
            }
            catch (Exception ex)
            {
                Log.LogWarning($"Failed to patch TextureReplacer.DoReplace for phone UI repair: {ex.Message}");
            }
        }

        Log.LogInfo($"TextureReplacer phone UI guard patches applied ({applied}).");
    }

    private static bool TryPatchTextureReplacerMethod(
        Harmony harmony,
        System.Type textureReplacerType,
        string methodName,
        string? prefix = null,
        string? postfix = null)
    {
        MethodInfo? method = AccessTools.Method(textureReplacerType, methodName);
        if (method == null)
        {
            Log.LogDebug($"TextureReplacer method not found: {methodName}");
            return false;
        }

        try
        {
            HarmonyMethod? prefixMethod = prefix != null
                ? new HarmonyMethod(typeof(TextureReplacerPhoneUiGuardPatches), prefix)
                : null;
            HarmonyMethod? postfixMethod = postfix != null
                ? new HarmonyMethod(typeof(TextureReplacerPhoneUiGuardPatches), postfix)
                : null;
            harmony.Patch(method, prefix: prefixMethod, postfix: postfixMethod);
            return true;
        }
        catch (Exception ex)
        {
            Log.LogWarning($"Failed to patch TextureReplacer.{methodName}: {ex.Message}");
            return false;
        }
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


