using System;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace ArtExpander.Patches
{
    [HarmonyPatch(typeof(CardUI))]
    [HarmonyPatch("SetCardUI")]
    [HarmonyPatch(new Type[] { typeof(CardData) })]
    public class CardUISetCardPatch
    {
        // CardDataTracker is still needed by GetFoilMaskPatch, which prefixes
        // GetCardFoilMaskSprite called during SetCardUI execution.
        internal static class CardDataTracker
        {
            private static CardData currentCardData;

            public static void SetCurrentCard(CardData data)
            {
                currentCardData = data;
            }

            public static void ClearCurrentCard()
            {
                currentCardData = null;
            }

            public static CardData GetCurrentCardInfo()
            {
                return currentCardData;
            }
        }

        private static readonly System.Reflection.FieldInfo m_CenterFrameImageField =
            AccessTools.Field(typeof(CardUI), "m_CenterFrameImage");

        private static Sprite TryLoadFromCache(Core.ArtCache cache, CardData cardData)
        {
            string artPath = cache.ResolveArtPath(
                cardData.monsterType,
                cardData.borderType,
                cardData.expansionType,
                cardData.isDestiny,
                cardData.isFoil
            );
            if (!string.IsNullOrEmpty(artPath))
            {
                return cache.LoadSprite(artPath);
            }
            return null;
        }

        static void Prefix(CardData cardData)
        {
            CardDataTracker.SetCurrentCard(cardData);
        }

        static void Postfix(CardUI __instance, CardData cardData)
        {
            if (cardData == null)
                return;

            var customSprite = TryLoadFromCache(Plugin.art_cache_directory, cardData)
                            ?? TryLoadFromCache(Plugin.art_cache_bundle, cardData);

            if (customSprite != null)
            {
                var image = m_CenterFrameImageField.GetValue(__instance) as Image;
                if (image != null)
                {
                    image.sprite = customSprite;
                }
            }

            CardDataTracker.ClearCurrentCard();
        }
    }
}
