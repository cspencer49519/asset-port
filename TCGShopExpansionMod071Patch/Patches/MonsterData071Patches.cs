using HarmonyLib;
using UnityEngine;

namespace TCGShopExpansionMod071Patch.Patches;

/// <summary>
/// Vanilla SetCardUI assigns GetCardBackSprite before our pack routing — substitute Pokemon CardBack during pack open.
/// </summary>
internal static class MonsterData071Patches
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
}
