using HarmonyLib;
using UnityEngine;

namespace TCGShopExpansionMod0703Patch.Patches;

/// <summary>
/// Vanilla SetCardUI assigns GetCardBackSprite before our pack routing — substitute Pokemon CardBack during pack open.
/// GradeCardScratch* is remapped to CardBack by TextureReplacer — return a clear sprite instead.
/// </summary>
internal static class MonsterData0703Patches
{
    [HarmonyPostfix]
    [HarmonyPatch(typeof(MonsterData_ScriptableObject), nameof(MonsterData_ScriptableObject.GetCardBackSprite))]
    public static void GetCardBackSprite_Postfix(ECardExpansionType cardExpansionType, ref Sprite __result)
    {
        if (cardExpansionType != ECardExpansionType.Tetramon || !PackOpeningState.IsPackOpeningInProgress())
        {
            return;
        }

        Sprite? pokemonBack = CardExtrasCacheAccess.TryGetUiCardBackSprite();
        if (pokemonBack != null)
        {
            __result = pokemonBack;
        }
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(MonsterData_ScriptableObject), nameof(MonsterData_ScriptableObject.GetGradedCardScratchTexture))]
    public static bool GetGradedCardScratchTexture_Prefix(ref Sprite __result)
    {
        __result = GradedScratchClearSprite.Get();
        return false;
    }
}
