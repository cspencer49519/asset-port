using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace TCGShopExpansionMod071Patch.Patches;

internal static class PlayerPatches071Patches
{
    private static FieldInfo? FantasyMaterialField;
    private static FieldInfo? CatJobMaterialField;
    private static FieldInfo? MegabotMaterialField;
    private static FieldInfo? GhostMaterialField;

    /// <summary>
    /// Skip ExpansionMod's SetCardUI postfix on 0.71 — SetCardExtrasImages is disabled so
    /// border caches are never populated, and the postfix NREs on partially initialized cards.
    /// </summary>
    public static bool CardUI_SetCardUI_Postfix_Prefix()
    {
        return false;
    }

    /// <summary>
    /// ExpansionMod InitOpenSequence_Postfix calls Count() on m_Card3dUIList before cards exist — freezes pack open on 0.71.
    /// Pack backs are handled by ExtrasHandler071Patches + TetramonOverlay071Patches instead.
    /// </summary>
    public static bool InitOpenSequence_BlockExpansionPackBack_Prefix()
    {
        return false;
    }

    /// <summary>
    /// Album close-up postfix reads removed CardUI.m_GhostCard — removed via Harmony.Unpatch instead.
    /// Kept as unused helper documentation; do not Prefix-patch the ExpansionMod method body.
    /// </summary>
    public static bool EnterViewUpCloseState_Postfix_Prefix()
    {
        return false;
    }

    /// <summary>
    /// Sort-album postfix NREs on 0.71 — removed via Harmony.Unpatch instead.
    /// </summary>
    public static bool OpenSortAlbumScreen_Postfix_Prefix()
    {
        return false;
    }

    public static bool LightManager_Awake_Prefix_Prefix()
    {
        EnsureMaterialFields();

        if (!AllPackMaterialsReady())
        {
            Plugin.Log.LogDebug("Skipping LightManager pack-material patch; pack materials not initialized yet.");
            return false;
        }

        return true;
    }

    private static void EnsureMaterialFields()
    {
        if (FantasyMaterialField != null)
        {
            return;
        }

        System.Type? playerPatches = AccessTools.TypeByName("TCGShopExpansionMod.Patches.PlayerPatches");
        if (playerPatches == null)
        {
            return;
        }

        FantasyMaterialField = AccessTools.Field(playerPatches, "newFantasyPackMaterial");
        CatJobMaterialField = AccessTools.Field(playerPatches, "newCatJobPackMaterial");
        MegabotMaterialField = AccessTools.Field(playerPatches, "newMegabotPackMaterial");
        GhostMaterialField = AccessTools.Field(playerPatches, "newGhostPackMaterial");
    }

    private static bool AllPackMaterialsReady()
    {
        return IsMaterialReady(FantasyMaterialField)
            && IsMaterialReady(CatJobMaterialField)
            && IsMaterialReady(MegabotMaterialField)
            && IsMaterialReady(GhostMaterialField);
    }

    private static bool IsMaterialReady(FieldInfo? field)
    {
        if (field == null)
        {
            return false;
        }

        return field.GetValue(null) is Material material && material != null;
    }
}
