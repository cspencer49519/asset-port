using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using HarmonyLib;
using I2.Loc;
using TCGShopExpansionMod.Handlers;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace TCGShopExpansionMod.Patches;

[HarmonyPatch]
internal class PlayerPatches
{
	public static bool containsNew = false;

	public static string gameInstallPath = Path.GetDirectoryName(Application.dataPath);

	public static string gameBepinexPath = Path.Combine(gameInstallPath, "BepInEx", "plugins");

	public static string customExpansionPackImages = Path.Combine(gameBepinexPath, "CustomExpansionPackImages") + Path.DirectorySeparatorChar;

	public static string pluginPath = Path.Combine(gameBepinexPath, "TCGShopExpansionMod") + Path.DirectorySeparatorChar;

	public static string fantasyPackImages = Path.Combine(customExpansionPackImages, "FantasyRPGPackImages") + Path.DirectorySeparatorChar;

	public static string catJobPackImages = Path.Combine(customExpansionPackImages, "CatJobPackImages") + Path.DirectorySeparatorChar;

	public static string megabotPackImages = Path.Combine(customExpansionPackImages, "MegabotPackImages") + Path.DirectorySeparatorChar;

	public static string cardExtrasImages = Path.Combine(customExpansionPackImages, "CardExtrasImages") + Path.DirectorySeparatorChar;

	public static string tetramonPackImages = Path.Combine(customExpansionPackImages, "TetramonPackImages") + Path.DirectorySeparatorChar;

	public static string ghostPackImages = Path.Combine(customExpansionPackImages, "GhostPackImages") + Path.DirectorySeparatorChar;

	public static string configPath = Path.Combine(customExpansionPackImages, "Configs", "Custom") + Path.DirectorySeparatorChar;

	public static string originalConfigsPath = Path.Combine(customExpansionPackImages, "Configs", "Original") + Path.DirectorySeparatorChar;

	public static string originalFullExpansionsConfigPath = Path.Combine(originalConfigsPath, "FullExpansionsConfigs") + Path.DirectorySeparatorChar;

	public static string catJobConfigPath = Path.Combine(configPath, "CatJobConfigs") + Path.DirectorySeparatorChar;

	public static string destinyConfigPath = Path.Combine(configPath, "DestinyConfigs") + Path.DirectorySeparatorChar;

	public static string fantasyRPGConfigPath = Path.Combine(configPath, "FantasyRPGConfigs") + Path.DirectorySeparatorChar;

	public static string fullExpansionsConfigPath = Path.Combine(configPath, "FullExpansionsConfigs") + Path.DirectorySeparatorChar;

	public static string ghostConfigPath = Path.Combine(configPath, "GhostConfigs") + Path.DirectorySeparatorChar;

	public static string megabotConfigPath = Path.Combine(configPath, "MegabotConfigs") + Path.DirectorySeparatorChar;

	public static string tetramonConfigPath = Path.Combine(configPath, "TetramonConfigs") + Path.DirectorySeparatorChar;

	public static string configCaches = Path.Combine(configPath, "Caches") + Path.DirectorySeparatorChar;

	public static string zipConfigCachesPath = Path.Combine(configCaches, "Configs") + Path.DirectorySeparatorChar;

	public static string zipMonsterDataCachesPath = Path.Combine(configCaches, "MonsterData") + Path.DirectorySeparatorChar;

	public static string imageCachesPath = Path.Combine(customExpansionPackImages, "Caches") + Path.DirectorySeparatorChar;

	public static string newMonstersConfigPath = Path.Combine(configPath, "NewMonstersConfigs") + Path.DirectorySeparatorChar;

	public static string textureReplacerPath = Path.Combine(gameBepinexPath, "TextureReplacer") + Path.DirectorySeparatorChar;

	public static string textureReplacerImagesPath = Path.Combine(textureReplacerPath, "objects_textures") + Path.DirectorySeparatorChar;

	public static string textureReplacerCardNamesPath = Path.Combine(textureReplacerPath, "objects_data", "cards") + Path.DirectorySeparatorChar;

	public static string originalTetramonPackImages = Path.Combine(customExpansionPackImages, "OriginalMonsterImages") + Path.DirectorySeparatorChar;

	public static CustomCardObject lastLoadedCard;

	public static CustomCardObject lastLoadedFullExpansionCard;

	public static string newCatJobPackName = LocalizationManager.GetTranslation("CatJob", true, 0, true, false, (GameObject)null, (string)null, true);

	public static string newFantasyRPGPackName = LocalizationManager.GetTranslation("FantasyRPG", true, 0, true, false, (GameObject)null, (string)null, true);

	public static string newMegaBotPackName = LocalizationManager.GetTranslation("Megabot", true, 0, true, false, (GameObject)null, (string)null, true);

	public static Color defaultButtonBorder = new Color(0.118f, 0.309f, 0.537f, 1f);

	public static Color defaultButtonMidtone = new Color(0.09f, 0.664f, 1f, 1f);

	public static Color defaultButtonHighlight = new Color(0.353f, 0.909f, 1f, 1f);

	public static Color newButtonBorder = new Color(0.4f, 0.125f, 0.05f, 1f);

	public static Color newButtonMidtone = new Color(0.5f, 0.125f, 0.7f, 1f);

	public static Color newButtonHighlight = new Color(0.6f, 0.2f, 0.75f, 1f);

	public static Vector2 defaultTetramonMonsterImageSize = new Vector2(0.2f, 197f);

	public static Vector2 defaultTetramonMonsterImagePosition = new Vector2(0f, -21f);

	public static Vector2 defaultTetramonMonsterFullArtImageSize = new Vector2(0f, 442.45f);

	public static Vector2 defaultTetramonMonsterFullArtImagePosition = new Vector2(0f, -66f);

	public static Vector2 defaultGhostMonsterImageSize = new Vector2(0f, 205.75f);

	public static Vector2 defaultGhostMonsterImagePosition = new Vector2(-9.1f, -6.4f);

	public static Vector2 defaultOtherMonsterImagePosition = new Vector2(0f, -96f);

	public static Vector2 defaultOtherMonsterFullArtImagePosition = new Vector2(0f, -116f);

	public static bool isDoingLoad = false;

	public static Material newFantasyPackMaterial = null;

	public static Material newCatJobPackMaterial = null;

	public static Material newMegabotPackMaterial = null;

	public static Material newGhostPackMaterial = null;

	public static bool lightFix = false;

	[HarmonyPostfix]
	[HarmonyPatch(typeof(ItemPriceGraphScreen), "ShowCardPriceChart")]
	public static void ItemPriceGraphScreen_ShowCardPriceChart_Postfix(ItemPriceGraphScreen __instance)
	{
		string cardFullName = NewSwappingHandler.GetCardFullName(__instance.m_CardUI.m_CardData);
		if (cardFullName != null)
		{
			((TMP_Text)__instance.m_CardName).text = cardFullName;
		}
	}

	[HarmonyPostfix]
	[HarmonyPatch(typeof(CheckPricePanelUI), "InitCard")]
	public static void CheckPricePanelUI_InitCard_Postfix(CheckPricePanelUI __instance)
	{
		string cardFullName = NewSwappingHandler.GetCardFullName(__instance.m_CardUI.m_CardData);
		if (cardFullName != null)
		{
			((TMP_Text)__instance.m_NameText).text = cardFullName;
		}
	}

	[HarmonyPostfix]
	[HarmonyPatch(typeof(CardUI), "SetCardUI")]
	public static void CardUI_SetCardUI_Postfix(CardUI __instance)
	{
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0014: Invalid comparison between Unknown and I4
		//IL_0025: Unknown result type (might be due to invalid IL or missing references)
		//IL_002b: Invalid comparison between Unknown and I4
		//IL_003c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0042: Invalid comparison between Unknown and I4
		CardData cardData = __instance.m_CardData;
		if (cardData == null || (int)cardData.expansionType != 0)
		{
			CardData cardData2 = __instance.m_CardData;
			if (cardData2 == null || (int)cardData2.expansionType != 1)
			{
				CardData cardData3 = __instance.m_CardData;
				if (cardData3 == null || (int)cardData3.expansionType != 2)
				{
					if (TCGShopExpansionModPlugin.CustomNewExpansionImages.Value)
					{
						Sprite val = NewSwappingHandler.TryGetSpriteFromCache(CacheHandler.cardExtrasImagesCache, ((Object)__instance.m_CardBorderImage.sprite).name);
						if ((Object)(object)val != (Object)null)
						{
							__instance.m_CardBorderImage.sprite = val;
						}
					}
					else if (!TCGShopExpansionModPlugin.CustomNewExpansionImages.Value)
					{
						Sprite val2 = NewSwappingHandler.TryGetSpriteFromCache(CacheHandler.originalCardExtrasImagesCache, ((Object)__instance.m_CardBorderImage.sprite).name);
						if ((Object)(object)val2 != (Object)null)
						{
							__instance.m_CardBorderImage.sprite = val2;
						}
					}
					return;
				}
			}
		}
		if (TCGShopExpansionModPlugin.CustomBaseMonsterImages.Value)
		{
			Sprite val3 = NewSwappingHandler.TryGetSpriteFromCache(CacheHandler.cardExtrasImagesCache, ((Object)__instance.m_CardBorderImage.sprite).name);
			if ((Object)(object)val3 != (Object)null)
			{
				__instance.m_CardBorderImage.sprite = val3;
			}
		}
		else if (!TCGShopExpansionModPlugin.CustomBaseMonsterImages.Value)
		{
			Sprite val4 = NewSwappingHandler.TryGetSpriteFromCache(CacheHandler.originalCardExtrasImagesCache, ((Object)__instance.m_CardBorderImage.sprite).name);
			if ((Object)(object)val4 != (Object)null)
			{
				__instance.m_CardBorderImage.sprite = val4;
			}
		}
	}

	[HarmonyPrefix]
	[HarmonyPatch(typeof(InteractableCard3d), "SetCardUIFollow")]
	public static void SetCardUIFollow_Prefix(Card3dUIGroup card3dUI, InteractableCard3d __instance)
	{
		ExtrasHandler.SetCardBacks(card3dUI);
	}

	[HarmonyPostfix]
	[HarmonyPatch(typeof(LightManager), "EvaluateWorldUIBrightness")]
	public static void LightManager_EvaluateWorldUIBrightness_Postfix(LightManager __instance)
	{
		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
		//IL_0051: Unknown result type (might be due to invalid IL or missing references)
		foreach (KeyValuePair<ECardExpansionType, Material> cardBackMaterial in ExtrasHandler.cardBackMaterials)
		{
			if ((Object)(object)cardBackMaterial.Value != (Object)null)
			{
				cardBackMaterial.Value.SetColor("_EmissionColor", __instance.m_CardBackMatOriginalEmissionColor * Mathf.Lerp(0.2f, 1f, __instance.m_GlobalBrightness));
			}
		}
	}

	[HarmonyPrefix]
	[HarmonyPatch(typeof(CPlayerData), "CPlayer_OnSetFame")]
	public static void CPlayerData_CPlayer_OnSetFame_Prefix()
	{
		NewSwappingHandler.DoFirstWorldLoad();
	}

	[HarmonyPostfix]
	[HarmonyPatch(typeof(ItemSpawnManager), "Start")]
	public static void ItemSpawnManager_Start_Postfix()
	{
		ExtrasHandler.SwapNewPackItemImages();
	}

	[HarmonyPostfix]
	[HarmonyPatch(typeof(InteractableOpenCloseSign), "EvaluateSignOpenCloseMesh")]
	public static void InteractableOpenCloseSign_EvaluateSignOpenCloseMesh_Postfix()
	{
		Resources.UnloadUnusedAssets();
	}

	[HarmonyPostfix]
	[HarmonyPatch(typeof(CGameManager), "OnLevelFinishedLoading")]
	public static void CGameManager_OnLevelFinishedLoading_Postfix(ref Scene scene, LoadSceneMode mode)
	{
		if (CSingleton<CGameManager>.Instance.m_IsGameLevel)
		{
			ExtrasHandler.AddHiddenCards();
		}
		if (((Scene)(ref scene)).name == "Title" && !CacheHandler.firstLoad && !TCGShopExpansionModPlugin.isConfigGeneratorBuild)
		{
			CacheHandler.firstLoad = true;
		}
	}

	[HarmonyPrefix]
	[HarmonyPatch(typeof(CardOpeningSequence), "InitOpenSequence")]
	public static void InitOpenSequence_Postfix(CardOpeningSequence __instance)
	{
		if (__instance.m_Card3dUIList.Count() > 0)
		{
			ExtrasHandler.SetCardBackPackOpening(__instance.m_Card3dUIList[__instance.m_Card3dUIList.Count - 1]);
		}
	}

	[HarmonyPostfix]
	[HarmonyPatch(typeof(InteractionPlayerController), "AddHoldCard")]
	public static void InteractionPlayerController_AddHoldCard_Postfix(InteractionPlayerController __instance, InteractableCard3d card3d)
	{
		if (Object.op_Implicit((Object)(object)card3d))
		{
			Card3dUIGroup card3dUI = card3d.m_Card3dUI;
			ExtrasHandler.SetCardBacks(card3dUI);
		}
	}

	[HarmonyPostfix]
	[HarmonyPatch(typeof(CardExpansionSelectScreen), "OpenScreen")]
	public static void CardExpansionSelectScreen_OpenScreen_Postfix(CardExpansionSelectScreen __instance)
	{
		//IL_00dd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fc: Unknown result type (might be due to invalid IL or missing references)
		//IL_0111: Unknown result type (might be due to invalid IL or missing references)
		//IL_010a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0116: Unknown result type (might be due to invalid IL or missing references)
		//IL_0183: Unknown result type (might be due to invalid IL or missing references)
		//IL_018f: Unknown result type (might be due to invalid IL or missing references)
		//IL_019b: Unknown result type (might be due to invalid IL or missing references)
		GameObject screenGrp = CSingleton<CardExpansionSelectScreen>.Instance.m_ScreenGrp;
		Transform[] componentsInChildren = screenGrp.GetComponentsInChildren<Transform>();
		Transform val = componentsInChildren.FirstOrDefault((Transform t) => ((Object)t).name == "Tetramon_Button");
		Transform val2 = componentsInChildren.FirstOrDefault((Transform t) => ((Object)t).name == "Destiny_Button");
		Transform val3 = componentsInChildren.FirstOrDefault((Transform t) => ((Object)t).name == "Ghost_Button");
		if (Object.op_Implicit((Object)(object)val) && Object.op_Implicit((Object)(object)val2) && Object.op_Implicit((Object)(object)val3))
		{
			List<Transform> list = new List<Transform> { val, val2, val3 };
			Color color = (TCGShopExpansionModPlugin.SwapExpansions.Value ? newButtonBorder : defaultButtonBorder);
			Color color2 = (TCGShopExpansionModPlugin.SwapExpansions.Value ? newButtonMidtone : defaultButtonMidtone);
			Color color3 = (TCGShopExpansionModPlugin.SwapExpansions.Value ? newButtonHighlight : defaultButtonHighlight);
			foreach (Transform item in list)
			{
				Image[] componentsInChildren2 = ((Component)item).GetComponentsInChildren<Image>();
				foreach (Image val4 in componentsInChildren2)
				{
					switch (((Object)val4).name)
					{
					case "BGBorder":
						((Graphic)val4).color = color;
						break;
					case "BGMidtone":
						((Graphic)val4).color = color2;
						break;
					case "BGHighlight":
						((Graphic)val4).color = color3;
						break;
					}
				}
			}
		}
		TMP_Text[] componentsInChildren3 = screenGrp.GetComponentsInChildren<TMP_Text>();
		string translation = LocalizationManager.GetTranslation("Tetramon Base", true, 0, true, false, (GameObject)null, (string)null, true);
		string translation2 = LocalizationManager.GetTranslation("Tetramon Destiny", true, 0, true, false, (GameObject)null, (string)null, true);
		string translation3 = LocalizationManager.GetTranslation("Tetramon Ghost", true, 0, true, false, (GameObject)null, (string)null, true);
		string text = (TCGShopExpansionModPlugin.SwapExpansions.Value ? newMegaBotPackName : translation);
		string text2 = (TCGShopExpansionModPlugin.SwapExpansions.Value ? newFantasyRPGPackName : translation2);
		string text3 = (TCGShopExpansionModPlugin.SwapExpansions.Value ? newCatJobPackName : translation3);
		TMP_Text[] array = componentsInChildren3;
		foreach (TMP_Text val5 in array)
		{
			if (!string.IsNullOrEmpty(val5.text))
			{
				if (val5.text == translation || val5.text == newMegaBotPackName)
				{
					val5.text = text;
				}
				else if (val5.text == translation2 || val5.text == newFantasyRPGPackName)
				{
					val5.text = text2;
				}
				else if (val5.text == translation3 || val5.text == newCatJobPackName)
				{
					val5.text = text3;
				}
			}
		}
	}

	[HarmonyPrefix]
	[HarmonyPatch(typeof(CardExpansionSelectScreen), "OpenScreen")]
	public static void CardExpansionSelectScreen_OpenScreen_Prefix(ref ECardExpansionType initCardExpansion)
	{
		int num = (int)initCardExpansion;
		if (num >= 3)
		{
			initCardExpansion = (ECardExpansionType)(num - 3);
		}
	}

	[HarmonyPrefix]
	[HarmonyPatch(typeof(CheckPriceScreen), "OnCardExpansionUpdated")]
	public static void CheckPriceScreen_OnCardExpansionUpdated_Prefix(CheckPriceScreen __instance, ref CEventPlayer_OnCardExpansionSelectScreenUpdated evt)
	{
		if (TCGShopExpansionModPlugin.SwapExpansions.Value)
		{
			CEventPlayer_OnCardExpansionSelectScreenUpdated obj = evt;
			obj.m_CardExpansionTypeIndex += 3;
		}
	}

	[HarmonyPrefix]
	[HarmonyPatch(typeof(InventoryBase), "GetCardExpansionName")]
	public static bool InventoryBase_GetCardExpansionName_Prefix(InventoryBase __instance, ECardExpansionType cardExpansion, ref string __result)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0004: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Invalid comparison between Unknown and I4
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		//IL_000a: Invalid comparison between Unknown and I4
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_001b: Invalid comparison between Unknown and I4
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		//IL_002f: Invalid comparison between Unknown and I4
		//IL_0041: Unknown result type (might be due to invalid IL or missing references)
		//IL_0043: Invalid comparison between Unknown and I4
		if ((int)cardExpansion == 0 || (int)cardExpansion == 1 || (int)cardExpansion == 2)
		{
			return true;
		}
		if ((int)cardExpansion == 5)
		{
			__result = newCatJobPackName;
			return false;
		}
		if ((int)cardExpansion == 4)
		{
			__result = newFantasyRPGPackName;
			return false;
		}
		if ((int)cardExpansion == 3)
		{
			__result = newMegaBotPackName;
			return false;
		}
		return true;
	}

	[HarmonyPrefix]
	[HarmonyPatch(typeof(CollectionBinderUI), "OnPressSwitchExpansion")]
	public static bool CollectionBinderUI_OnPressSwitchExpansion_Prefix(ref CollectionBinderUI __instance, ref int expansionIndex)
	{
		if (TCGShopExpansionModPlugin.SwapExpansions.Value)
		{
			expansionIndex += 3;
		}
		return true;
	}

	[HarmonyPostfix]
	[HarmonyPatch(typeof(CollectionBinderUI), "OpenSortAlbumScreen")]
	public static void CollectionBinderUI_OpenSortAlbumScreen_HarmonyPostfix(ref CollectionBinderUI __instance, int sortingMethodIndex, ref int currentExpansionIndex)
	{
		//IL_021c: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bc: Unknown result type (might be due to invalid IL or missing references)
		//IL_0244: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e4: Unknown result type (might be due to invalid IL or missing references)
		//IL_026c: Unknown result type (might be due to invalid IL or missing references)
		//IL_010c: Unknown result type (might be due to invalid IL or missing references)
		if (TCGShopExpansionModPlugin.SwapExpansions.Value)
		{
			((Component)__instance.m_ExpansionBtnList[0]).GetComponentInChildren<TMP_Text>().text = newMegaBotPackName;
			((Component)__instance.m_ExpansionBtnList[1]).GetComponentInChildren<TMP_Text>().text = newFantasyRPGPackName;
			((Component)__instance.m_ExpansionBtnList[2]).GetComponentInChildren<TMP_Text>().text = newCatJobPackName;
			foreach (Transform expansionBtn in __instance.m_ExpansionBtnList)
			{
				Image[] componentsInChildren = ((Component)expansionBtn).GetComponentsInChildren<Image>();
				foreach (Image val in componentsInChildren)
				{
					if (((Object)val).name == "BGBorder")
					{
						((Graphic)val).color = newButtonBorder;
					}
					else if (((Object)val).name == "BGMidtone")
					{
						((Graphic)val).color = newButtonMidtone;
					}
					else if (((Object)val).name == "BGHighlight")
					{
						((Graphic)val).color = newButtonHighlight;
					}
				}
			}
		}
		else
		{
			((Component)__instance.m_ExpansionBtnList[0]).GetComponentInChildren<TMP_Text>().text = LocalizationManager.GetTranslation("Tetramon", true, 0, true, false, (GameObject)null, (string)null, true);
			((Component)__instance.m_ExpansionBtnList[1]).GetComponentInChildren<TMP_Text>().text = LocalizationManager.GetTranslation("Destiny", true, 0, true, false, (GameObject)null, (string)null, true);
			((Component)__instance.m_ExpansionBtnList[2]).GetComponentInChildren<TMP_Text>().text = LocalizationManager.GetTranslation("Ghost", true, 0, true, false, (GameObject)null, (string)null, true);
			foreach (Transform expansionBtn2 in __instance.m_ExpansionBtnList)
			{
				Image[] componentsInChildren2 = ((Component)expansionBtn2).GetComponentsInChildren<Image>();
				foreach (Image val2 in componentsInChildren2)
				{
					if (((Object)val2).name == "BGBorder")
					{
						((Graphic)val2).color = defaultButtonBorder;
					}
					else if (((Object)val2).name == "BGMidtone")
					{
						((Graphic)val2).color = defaultButtonMidtone;
					}
					else if (((Object)val2).name == "BGHighlight")
					{
						((Graphic)val2).color = defaultButtonHighlight;
					}
				}
			}
		}
		TMP_Text componentInChildren = ((Component)__instance.m_ExpansionBtnList[0]).GetComponentInChildren<TMP_Text>();
		TMP_Text componentInChildren2 = ((Component)__instance.m_ExpansionBtnList[0]).GetComponentInChildren<TMP_Text>();
		TMP_Text componentInChildren3 = ((Component)__instance.m_ExpansionBtnList[0]).GetComponentInChildren<TMP_Text>();
	}

	[HarmonyPrefix]
	[HarmonyPatch(typeof(CollectionBinderUI), "OpenSortAlbumScreen")]
	public static bool CollectionBinderUI_OpenSortAlbumScreen_Prefix(ref CollectionBinderUI __instance, int sortingMethodIndex, ref int currentExpansionIndex)
	{
		if (currentExpansionIndex >= 3)
		{
			currentExpansionIndex -= 3;
		}
		return true;
	}

	[HarmonyPostfix]
	[HarmonyPatch(typeof(CollectionBinderFlipAnimCtrl), "EnterViewUpCloseState")]
	public static void CollectionBinderFlipAnimCtrl_EnterViewUpCloseState_Postfix(CollectionBinderFlipAnimCtrl __instance)
	{
		//IL_0047: Unknown result type (might be due to invalid IL or missing references)
		//IL_004d: Invalid comparison between Unknown and I4
		CardData cardData = __instance.m_CurrentViewInteractableCard3d.m_Card3dUI.m_CardUI.GetCardData();
		if (!ExtrasHandler.isCardConfigDriven(cardData))
		{
			return;
		}
		bool flag = false;
		bool flag2 = false;
		flag = (cardData.isFoil ? true : false);
		flag2 = (int)cardData.expansionType == 2;
		if (((TMP_Text)__instance.m_CollectionBinderUI.m_CardNameText).text != ((TMP_Text)__instance.m_CurrentSpawnedInteractableCard3d.m_Card3dUI.m_CardUI.m_MonsterNameText).text)
		{
			((TMP_Text)__instance.m_CollectionBinderUI.m_CardNameText).text = ((TMP_Text)__instance.m_CurrentSpawnedInteractableCard3d.m_Card3dUI.m_CardUI.m_MonsterNameText).text;
		}
		CardUI cardUI = __instance.m_CurrentViewInteractableCard3d.m_Card3dUI.m_CardUI;
		CardUI ghostCard = __instance.m_CurrentViewInteractableCard3d.m_Card3dUI.m_CardUI.m_GhostCard;
		string text = ((TMP_Text)cardUI.m_FirstEditionText).text;
		string text2 = ((TMP_Text)cardUI.m_RarityText).text;
		string text3 = "";
		Transform val = ((Component)cardUI).transform.Find("FoilText");
		if (flag2)
		{
			val = ((Component)ghostCard).transform.Find("FoilText");
		}
		if ((Object)(object)val != (Object)null)
		{
			TextMeshProUGUI component = ((Component)val).GetComponent<TextMeshProUGUI>();
			if ((Object)(object)component != (Object)null)
			{
				text3 = ((TMP_Text)component).text;
			}
		}
		string text4 = "";
		if (!flag)
		{
			text4 = text + " " + text2;
		}
		else if (flag)
		{
			text4 = text + " " + text2 + " " + text3;
		}
		((TMP_Text)__instance.m_CollectionBinderUI.m_CardFullRarityNameText).text = text4;
	}

	[HarmonyPrefix]
	[HarmonyPatch(typeof(InventoryBase), "Awake")]
	public static void InventoryBase_Awake_Prefix(LightManager __instance)
	{
		//IL_00d0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00da: Expected O, but got Unknown
		//IL_0146: Unknown result type (might be due to invalid IL or missing references)
		//IL_0150: Expected O, but got Unknown
		//IL_01bc: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c6: Expected O, but got Unknown
		//IL_0232: Unknown result type (might be due to invalid IL or missing references)
		//IL_023c: Expected O, but got Unknown
		ItemMeshData val = CSingleton<InventoryBase>.Instance.m_StockItemData_SO.m_ItemMeshDataList[0];
		ItemMeshData val2 = CSingleton<InventoryBase>.Instance.m_StockItemData_SO.m_ItemMeshDataList[19];
		ItemMeshData val3 = CSingleton<InventoryBase>.Instance.m_StockItemData_SO.m_ItemMeshDataList[18];
		ItemMeshData val4 = CSingleton<InventoryBase>.Instance.m_StockItemData_SO.m_ItemMeshDataList[17];
		ItemMeshData val5 = CSingleton<InventoryBase>.Instance.m_StockItemData_SO.m_ItemMeshDataList[16];
		Sprite val6 = NewSwappingHandler.TryGetSpriteFromCache(CacheHandler.cardExtrasImagesCache, "T_CardPackCatJob");
		Sprite val7 = NewSwappingHandler.TryGetSpriteFromCache(CacheHandler.cardExtrasImagesCache, "T_CardPackFantasy");
		Sprite val8 = NewSwappingHandler.TryGetSpriteFromCache(CacheHandler.cardExtrasImagesCache, "T_CardPackGhost");
		Sprite val9 = NewSwappingHandler.TryGetSpriteFromCache(CacheHandler.cardExtrasImagesCache, "T_CardPackMegabot");
		if ((Object)(object)newFantasyPackMaterial == (Object)null)
		{
			newFantasyPackMaterial = new Material(val.material);
			newFantasyPackMaterial.CopyPropertiesFromMaterial(val.material);
			newFantasyPackMaterial.mainTexture = (Texture)(object)val7.texture;
			((Object)newFantasyPackMaterial.mainTexture).name = "T_CardPackFantasy";
			((Object)newFantasyPackMaterial).name = "MAT_CardPackFantasy";
		}
		val3.material = newFantasyPackMaterial;
		if ((Object)(object)newCatJobPackMaterial == (Object)null)
		{
			newCatJobPackMaterial = new Material(val.material);
			newCatJobPackMaterial.CopyPropertiesFromMaterial(val.material);
			newCatJobPackMaterial.mainTexture = (Texture)(object)val6.texture;
			((Object)newCatJobPackMaterial.mainTexture).name = "T_CardPackCatJob";
			((Object)newCatJobPackMaterial).name = "MAT_CardPackCatJob";
		}
		val2.material = newCatJobPackMaterial;
		if ((Object)(object)newMegabotPackMaterial == (Object)null)
		{
			newMegabotPackMaterial = new Material(val.material);
			newMegabotPackMaterial.CopyPropertiesFromMaterial(val.material);
			newMegabotPackMaterial.mainTexture = (Texture)(object)val9.texture;
			((Object)newMegabotPackMaterial.mainTexture).name = "T_CardPackMegabot";
			((Object)newMegabotPackMaterial).name = "MAT_CardPackMegabot";
		}
		val4.material = newMegabotPackMaterial;
		if ((Object)(object)newGhostPackMaterial == (Object)null)
		{
			newGhostPackMaterial = new Material(val.material);
			newGhostPackMaterial.CopyPropertiesFromMaterial(val.material);
			newGhostPackMaterial.mainTexture = (Texture)(object)val8.texture;
			((Object)newGhostPackMaterial.mainTexture).name = "T_CardPackGhost";
			((Object)newGhostPackMaterial).name = "MAT_CardPackGhost";
		}
		val5.material = newGhostPackMaterial;
	}

	[HarmonyPrefix]
	[HarmonyPatch(typeof(LightManager), "Awake")]
	public static void LightManager_Awake_Prefix(LightManager __instance)
	{
		//IL_0065: Unknown result type (might be due to invalid IL or missing references)
		//IL_007c: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bb: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d2: Unknown result type (might be due to invalid IL or missing references)
		//IL_0113: Unknown result type (might be due to invalid IL or missing references)
		//IL_012a: Unknown result type (might be due to invalid IL or missing references)
		//IL_016b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0182: Unknown result type (might be due to invalid IL or missing references)
		if (__instance.m_ItemMatList.Count != 0)
		{
			Color val = default(Color);
			((Color)(ref val))..ctor(0.885f, 0.885f, 0.885f, 1f);
			if (!__instance.m_ItemMatList.Contains(newFantasyPackMaterial))
			{
				__instance.m_ItemMatList.Add(newFantasyPackMaterial);
				newFantasyPackMaterial.SetColor("_Color", val);
				__instance.m_ItemMatOriginalColorList.Add(newFantasyPackMaterial.GetColor("_Color"));
			}
			if (!__instance.m_ItemMatList.Contains(newCatJobPackMaterial))
			{
				__instance.m_ItemMatList.Add(newCatJobPackMaterial);
				newCatJobPackMaterial.SetColor("_Color", val);
				__instance.m_ItemMatOriginalColorList.Add(newCatJobPackMaterial.GetColor("_Color"));
			}
			if (!__instance.m_ItemMatList.Contains(newMegabotPackMaterial))
			{
				__instance.m_ItemMatList.Add(newMegabotPackMaterial);
				newMegabotPackMaterial.SetColor("_Color", val);
				__instance.m_ItemMatOriginalColorList.Add(newMegabotPackMaterial.GetColor("_Color"));
			}
			if (!__instance.m_ItemMatList.Contains(newGhostPackMaterial))
			{
				__instance.m_ItemMatList.Add(newGhostPackMaterial);
				newGhostPackMaterial.SetColor("_Color", val);
				__instance.m_ItemMatOriginalColorList.Add(newGhostPackMaterial.GetColor("_Color"));
			}
		}
	}

	[HarmonyPrefix]
	[HarmonyPatch(typeof(RestockItemPanelUI), "Init")]
	public static bool RestockItemPanelUI_Init_Prefix(RestockItemPanelUI __instance, RestockItemScreen restockItemScreen, int index)
	{
		//IL_010b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0111: Expected O, but got Unknown
		//IL_0111: Unknown result type (might be due to invalid IL or missing references)
		//IL_0117: Expected O, but got Unknown
		//IL_0117: Unknown result type (might be due to invalid IL or missing references)
		//IL_011d: Expected O, but got Unknown
		//IL_011d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0124: Expected O, but got Unknown
		//IL_0124: Unknown result type (might be due to invalid IL or missing references)
		//IL_012b: Expected O, but got Unknown
		//IL_012b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0132: Expected O, but got Unknown
		//IL_0132: Unknown result type (might be due to invalid IL or missing references)
		//IL_0139: Expected O, but got Unknown
		//IL_0139: Unknown result type (might be due to invalid IL or missing references)
		//IL_0140: Expected O, but got Unknown
		//IL_0143: Unknown result type (might be due to invalid IL or missing references)
		//IL_0174: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b4: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e6: Unknown result type (might be due to invalid IL or missing references)
		//IL_022c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0261: Unknown result type (might be due to invalid IL or missing references)
		//IL_02a7: Unknown result type (might be due to invalid IL or missing references)
		//IL_02dc: Unknown result type (might be due to invalid IL or missing references)
		if (!containsNew)
		{
			if (!CSingleton<InventoryBase>.Instance.m_StockItemData_SO.m_ShownItemType.Contains((EItemType)18))
			{
				CSingleton<InventoryBase>.Instance.m_StockItemData_SO.m_ShownItemType.Add((EItemType)18);
				CSingleton<InventoryBase>.Instance.m_StockItemData_SO.m_ShownItemType.Add((EItemType)19);
				CSingleton<InventoryBase>.Instance.m_StockItemData_SO.m_ShownItemType.Add((EItemType)17);
				CSingleton<InventoryBase>.Instance.m_StockItemData_SO.m_ShownItemType.Add((EItemType)16);
			}
			if (!CSingleton<InventoryBase>.Instance.m_StockItemData_SO.m_ShownAllItemType.Contains((EItemType)18))
			{
				CSingleton<InventoryBase>.Instance.m_StockItemData_SO.m_ShownAllItemType.Add((EItemType)18);
				CSingleton<InventoryBase>.Instance.m_StockItemData_SO.m_ShownAllItemType.Add((EItemType)19);
				CSingleton<InventoryBase>.Instance.m_StockItemData_SO.m_ShownAllItemType.Add((EItemType)17);
				CSingleton<InventoryBase>.Instance.m_StockItemData_SO.m_ShownAllItemType.Add((EItemType)16);
			}
			RestockData val = new RestockData();
			RestockData val2 = new RestockData();
			RestockData val3 = new RestockData();
			RestockData val4 = new RestockData();
			RestockData val5 = new RestockData();
			RestockData val6 = new RestockData();
			RestockData val7 = new RestockData();
			RestockData val8 = new RestockData();
			val.itemType = (EItemType)18;
			val.licenseShopLevelRequired = 30;
			val.licensePrice = 2500f;
			CSingleton<InventoryBase>.Instance.m_StockItemData_SO.m_RestockDataList.Add(val);
			val2.itemType = (EItemType)18;
			val2.licenseShopLevelRequired = 30;
			val2.licensePrice = 5000f;
			val2.amount = 64;
			val2.isBigBox = true;
			CSingleton<InventoryBase>.Instance.m_StockItemData_SO.m_RestockDataList.Add(val2);
			val3.itemType = (EItemType)19;
			val3.licenseShopLevelRequired = 40;
			val3.licensePrice = 5000f;
			CSingleton<InventoryBase>.Instance.m_StockItemData_SO.m_RestockDataList.Add(val3);
			val4.itemType = (EItemType)19;
			val4.licenseShopLevelRequired = 40;
			val4.licensePrice = 7500f;
			val4.amount = 64;
			val4.isBigBox = true;
			CSingleton<InventoryBase>.Instance.m_StockItemData_SO.m_RestockDataList.Add(val4);
			val5.itemType = (EItemType)17;
			val5.licenseShopLevelRequired = 50;
			val5.licensePrice = 7500f;
			CSingleton<InventoryBase>.Instance.m_StockItemData_SO.m_RestockDataList.Add(val5);
			val6.itemType = (EItemType)17;
			val6.licenseShopLevelRequired = 50;
			val6.licensePrice = 10000f;
			val6.amount = 64;
			val6.isBigBox = true;
			CSingleton<InventoryBase>.Instance.m_StockItemData_SO.m_RestockDataList.Add(val6);
			val7.itemType = (EItemType)16;
			val7.licenseShopLevelRequired = 60;
			val7.licensePrice = 10000f;
			CSingleton<InventoryBase>.Instance.m_StockItemData_SO.m_RestockDataList.Add(val7);
			val8.itemType = (EItemType)16;
			val8.licenseShopLevelRequired = 60;
			val8.licensePrice = 15000f;
			val8.amount = 64;
			val8.isBigBox = true;
			CSingleton<InventoryBase>.Instance.m_StockItemData_SO.m_RestockDataList.Add(val8);
			containsNew = true;
		}
		return true;
	}

	[HarmonyPostfix]
	[HarmonyPatch(typeof(CardUI), "SetCardUI")]
	public static void CardUI_SetCardUI_Main_Postfix(CardUI __instance, CardData cardData)
	{
		if (TCGShopExpansionModPlugin.isConfigGeneratorBuild)
		{
			ConfigGeneratorHelper.writeMonsterData(cardData, __instance);
			ConfigGeneratorHelper.WriteAllFullExpansionConfigs();
		}
		if (!TCGShopExpansionModPlugin.isConfigGeneratorBuild)
		{
			HandleCards(__instance, cardData);
		}
	}

	public static void HandleCardsImages(CardUI cardUI)
	{
		cardUI.m_CardBGImage.sprite = NewSwappingHandler.TryGetSpriteFromCache(CacheHandler.cardExtrasImagesCache, ((Object)cardUI.m_CardBGImage.sprite).name);
		cardUI.m_CardBorderImage.sprite = NewSwappingHandler.TryGetSpriteFromCache(CacheHandler.cardExtrasImagesCache, ((Object)cardUI.m_CardBorderImage.sprite).name);
		cardUI.m_CardFoilMaskImage.sprite = NewSwappingHandler.TryGetSpriteFromCache(CacheHandler.cardExtrasImagesCache, ((Object)cardUI.m_CardFoilMaskImage.sprite).name);
		cardUI.m_MonsterImage.sprite = NewSwappingHandler.TryGetSpriteFromCache(CacheHandler.tetramonPackImagesCache, ((Object)cardUI.m_MonsterImage.sprite).name);
		Image monsterMaskImage = cardUI.m_MonsterMaskImage;
		object obj;
		if (monsterMaskImage == null)
		{
			obj = null;
		}
		else
		{
			Sprite sprite = monsterMaskImage.sprite;
			obj = ((sprite != null) ? ((Object)sprite).name : null);
		}
		if ((string)obj != "WhiteTile")
		{
			cardUI.m_MonsterMaskImage.sprite = NewSwappingHandler.TryGetSpriteFromCache(CacheHandler.cardExtrasImagesCache, ((Object)cardUI.m_MonsterMaskImage.sprite).name);
		}
	}

	public static void HandleCards(CardUI inputCardUI, CardData cardData)
	{
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_000c: Invalid comparison between Unknown and I4
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0023: Invalid comparison between Unknown and I4
		//IL_0165: Unknown result type (might be due to invalid IL or missing references)
		//IL_016b: Invalid comparison between Unknown and I4
		//IL_01bc: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c2: Invalid comparison between Unknown and I4
		//IL_0213: Unknown result type (might be due to invalid IL or missing references)
		//IL_0219: Invalid comparison between Unknown and I4
		//IL_026a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0270: Invalid comparison between Unknown and I4
		//IL_02c1: Unknown result type (might be due to invalid IL or missing references)
		//IL_02c7: Invalid comparison between Unknown and I4
		//IL_0315: Unknown result type (might be due to invalid IL or missing references)
		//IL_031b: Invalid comparison between Unknown and I4
		//IL_04ea: Unknown result type (might be due to invalid IL or missing references)
		//IL_0502: Unknown result type (might be due to invalid IL or missing references)
		//IL_0528: Unknown result type (might be due to invalid IL or missing references)
		//IL_055f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0564: Unknown result type (might be due to invalid IL or missing references)
		//IL_0583: Unknown result type (might be due to invalid IL or missing references)
		//IL_0586: Invalid comparison between Unknown and I4
		//IL_057b: Unknown result type (might be due to invalid IL or missing references)
		//IL_057d: Unknown result type (might be due to invalid IL or missing references)
		//IL_06c5: Unknown result type (might be due to invalid IL or missing references)
		//IL_06c8: Invalid comparison between Unknown and I4
		//IL_0979: Unknown result type (might be due to invalid IL or missing references)
		//IL_098d: Unknown result type (might be due to invalid IL or missing references)
		//IL_082d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0845: Unknown result type (might be due to invalid IL or missing references)
		//IL_05e9: Unknown result type (might be due to invalid IL or missing references)
		//IL_05da: Unknown result type (might be due to invalid IL or missing references)
		//IL_05fa: Unknown result type (might be due to invalid IL or missing references)
		//IL_07cb: Unknown result type (might be due to invalid IL or missing references)
		//IL_07df: Unknown result type (might be due to invalid IL or missing references)
		//IL_0b37: Unknown result type (might be due to invalid IL or missing references)
		//IL_0b4f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0a4e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0a53: Unknown result type (might be due to invalid IL or missing references)
		//IL_0a55: Unknown result type (might be due to invalid IL or missing references)
		//IL_0a57: Unknown result type (might be due to invalid IL or missing references)
		//IL_0a59: Unknown result type (might be due to invalid IL or missing references)
		//IL_0a74: Expected I4, but got Unknown
		//IL_0c63: Unknown result type (might be due to invalid IL or missing references)
		//IL_0c7b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0ced: Unknown result type (might be due to invalid IL or missing references)
		//IL_0d05: Unknown result type (might be due to invalid IL or missing references)
		//IL_0bd7: Unknown result type (might be due to invalid IL or missing references)
		//IL_0bef: Unknown result type (might be due to invalid IL or missing references)
		//IL_0f94: Unknown result type (might be due to invalid IL or missing references)
		//IL_0fac: Unknown result type (might be due to invalid IL or missing references)
		//IL_1063: Unknown result type (might be due to invalid IL or missing references)
		//IL_107b: Unknown result type (might be due to invalid IL or missing references)
		//IL_10ed: Unknown result type (might be due to invalid IL or missing references)
		//IL_1105: Unknown result type (might be due to invalid IL or missing references)
		//IL_1177: Unknown result type (might be due to invalid IL or missing references)
		//IL_118f: Unknown result type (might be due to invalid IL or missing references)
		//IL_1201: Unknown result type (might be due to invalid IL or missing references)
		//IL_1219: Unknown result type (might be due to invalid IL or missing references)
		//IL_1450: Unknown result type (might be due to invalid IL or missing references)
		//IL_1464: Unknown result type (might be due to invalid IL or missing references)
		//IL_13ab: Unknown result type (might be due to invalid IL or missing references)
		//IL_13c3: Unknown result type (might be due to invalid IL or missing references)
		//IL_1598: Unknown result type (might be due to invalid IL or missing references)
		//IL_15b0: Unknown result type (might be due to invalid IL or missing references)
		//IL_1318: Unknown result type (might be due to invalid IL or missing references)
		//IL_1335: Unknown result type (might be due to invalid IL or missing references)
		//IL_15f6: Unknown result type (might be due to invalid IL or missing references)
		bool flag = false;
		bool flag2 = false;
		flag2 = (int)cardData.borderType == 5;
		flag = (int)cardData.expansionType == 2;
		CardUI val = inputCardUI;
		val = ((!flag || !((Object)(object)inputCardUI.m_GhostCard != (Object)null)) ? inputCardUI : ExtrasHandler.CurrentCardUI(flag, inputCardUI, inputCardUI.m_GhostCard));
		if (TCGShopExpansionModPlugin.isConfigGeneratorBuild)
		{
			return;
		}
		CustomCardObject customCardObject = null;
		CustomCardObject customCardObject2 = null;
		bool flag3 = false;
		if (ExtrasHandler.isCardConfigDriven(cardData))
		{
			flag3 = true;
		}
		string text = null;
		string text2 = null;
		if (flag)
		{
			text = ((object)System.Runtime.CompilerServices.Unsafe.As<EMonsterType, EMonsterType>(ref cardData.monsterType)/*cast due to constrained. prefix*/).ToString();
			text2 = ((object)System.Runtime.CompilerServices.Unsafe.As<ECardExpansionType, ECardExpansionType>(ref cardData.expansionType)/*cast due to constrained. prefix*/).ToString();
		}
		else if (!flag)
		{
			if (!flag2)
			{
				text = ((object)System.Runtime.CompilerServices.Unsafe.As<EMonsterType, EMonsterType>(ref cardData.monsterType)/*cast due to constrained. prefix*/).ToString();
				text2 = ((object)System.Runtime.CompilerServices.Unsafe.As<ECardExpansionType, ECardExpansionType>(ref cardData.expansionType)/*cast due to constrained. prefix*/).ToString();
			}
			else if (flag2)
			{
				text = ((object)System.Runtime.CompilerServices.Unsafe.As<EMonsterType, EMonsterType>(ref cardData.monsterType)/*cast due to constrained. prefix*/).ToString() + "FullArt";
				text2 = ((object)System.Runtime.CompilerServices.Unsafe.As<ECardExpansionType, ECardExpansionType>(ref cardData.expansionType)/*cast due to constrained. prefix*/).ToString() + "FullArt";
			}
		}
		if (text != null && text2 != null)
		{
			if ((int)cardData.expansionType == 0)
			{
				CustomCardObject customCardObject3 = null;
				customCardObject3 = ((!flag3) ? NewSwappingHandler.TryGetCardFromCache(cardData, useOriginals: true) : NewSwappingHandler.TryGetCardFromCache(cardData));
				if (customCardObject3 != null)
				{
					customCardObject = customCardObject3;
				}
				else
				{
					LogError("Null card Tetramon");
				}
			}
			else if ((int)cardData.expansionType == 1)
			{
				CustomCardObject customCardObject4 = null;
				customCardObject4 = ((!flag3) ? NewSwappingHandler.TryGetCardFromCache(cardData, useOriginals: true) : NewSwappingHandler.TryGetCardFromCache(cardData));
				if (customCardObject4 != null)
				{
					customCardObject = customCardObject4;
				}
				else
				{
					LogError("Null card Destiny");
				}
			}
			else if ((int)cardData.expansionType == 2)
			{
				CustomCardObject customCardObject5 = null;
				customCardObject5 = ((!flag3) ? NewSwappingHandler.TryGetCardFromCache(cardData, useOriginals: true) : NewSwappingHandler.TryGetCardFromCache(cardData));
				if (customCardObject5 != null)
				{
					customCardObject = customCardObject5;
				}
				else
				{
					LogError("Null card Ghost");
				}
			}
			else if ((int)cardData.expansionType == 5)
			{
				CustomCardObject customCardObject6 = null;
				customCardObject6 = ((!flag3) ? NewSwappingHandler.TryGetCardFromCache(cardData, useOriginals: true) : NewSwappingHandler.TryGetCardFromCache(cardData));
				if (customCardObject6 != null)
				{
					customCardObject = customCardObject6;
				}
				else
				{
					LogError("Null card CatJob");
				}
			}
			else if ((int)cardData.expansionType == 4)
			{
				CustomCardObject customCardObject7 = null;
				customCardObject7 = ((!flag3) ? NewSwappingHandler.TryGetCardFromCache(cardData, useOriginals: true) : NewSwappingHandler.TryGetCardFromCache(cardData));
				if (customCardObject7 != null)
				{
					customCardObject = customCardObject7;
				}
				else
				{
					LogError("Null card Fantasy");
				}
			}
			else if ((int)cardData.expansionType == 3)
			{
				CustomCardObject customCardObject8 = null;
				customCardObject8 = ((!flag3) ? NewSwappingHandler.TryGetCardFromCache(cardData, useOriginals: true) : NewSwappingHandler.TryGetCardFromCache(cardData));
				if (customCardObject8 != null)
				{
					customCardObject = customCardObject8;
				}
				else
				{
					LogError("Null card Megabot");
				}
			}
			CustomCardObject customCardObject9 = null;
			customCardObject9 = ((!flag3) ? NewSwappingHandler.TryGetFullExpansionCardFromCache(cardData, useOriginal: true) : NewSwappingHandler.TryGetFullExpansionCardFromCache(cardData));
			if (customCardObject9 != null)
			{
				customCardObject2 = customCardObject9;
			}
			else
			{
				LogError("Null card Expansion");
			}
		}
		if (customCardObject == null || customCardObject2 == null)
		{
			return;
		}
		CustomCardObject customCardObject10 = ExtrasHandler.SelectConfig(customCardObject, customCardObject2);
		if (flag3)
		{
			((TMP_Text)inputCardUI.m_MonsterNameText).text = customCardObject.Name;
			((TMP_Text)val.m_MonsterNameText).text = customCardObject.Name;
		}
		if (!flag)
		{
			((TMP_Text)val.m_DescriptionText).text = customCardObject.Description;
			if (flag2 && (Object)(object)inputCardUI.m_FullArtCard != (Object)null && (Object)(object)inputCardUI.m_FullArtCard.m_DescriptionText != (Object)null && ((TMP_Text)inputCardUI.m_FullArtCard.m_DescriptionText).text != null)
			{
				((TMP_Text)inputCardUI.m_FullArtCard.m_DescriptionText).text = customCardObject.Description;
			}
			((TMP_Text)val.m_ChampionText).text = customCardObject10.ChampionText;
			((Behaviour)val.m_DescriptionText).enabled = customCardObject10.DescriptionEnabled;
			((TMP_Text)val.m_DescriptionText).fontSize = customCardObject10.DescriptionFontSize;
			((TMP_Text)val.m_DescriptionText).fontSizeMin = customCardObject10.DescriptionFontSizeMin;
			((TMP_Text)val.m_DescriptionText).fontSizeMax = customCardObject10.DescriptionFontSizeMax;
			((Graphic)val.m_DescriptionText).color = customCardObject10.DescriptionFontColorRGBA;
			((TMP_Text)val.m_DescriptionText).rectTransform.anchoredPosition = customCardObject10.DescriptionPosition;
			if (!Enum.TryParse<EMonsterType>(customCardObject.PreviousEvolution, out EMonsterType _))
			{
				customCardObject.PreviousEvolution = ((object)(EMonsterType)0/*cast due to constrained. prefix*/).ToString();
			}
			if (Enum.TryParse<EMonsterType>(customCardObject.PreviousEvolution, out EMonsterType result2))
			{
				bool flag4 = false;
				if (val.m_MonsterData.PreviousEvolution != result2)
				{
					flag4 = true;
					val.m_MonsterData.PreviousEvolution = result2;
				}
				if ((int)result2 > 0)
				{
					if (flag4)
					{
						val.m_EvoBasicGrp.SetActive(false);
						((Component)val.m_EvoPreviousStageNameText).gameObject.SetActive(true);
						((Component)val.m_EvoPreviousStageIcon).gameObject.SetActive(true);
					}
					CustomCardObject customCardObject11 = null;
					customCardObject11 = ((!flag3) ? NewSwappingHandler.TryGetPreviousEvolutionCardFromCache(cardData, result2, useOriginals: true) : NewSwappingHandler.TryGetPreviousEvolutionCardFromCache(cardData, result2));
					val.m_EvoPreviousStageIcon.sprite = InventoryBase.GetMonsterData(result2).Icon;
					((TMP_Text)val.m_EvoPreviousStageNameText).text = customCardObject11.Name;
					if (flag2)
					{
						TextMeshProUGUI evoPreviousStageNameText = inputCardUI.m_EvoPreviousStageNameText;
						if (((evoPreviousStageNameText != null) ? ((TMP_Text)evoPreviousStageNameText).text : null) != null)
						{
							((TMP_Text)inputCardUI.m_EvoPreviousStageNameText).text = customCardObject11.Name;
						}
						CardUI fullArtCard = inputCardUI.m_FullArtCard;
						object obj;
						if (fullArtCard == null)
						{
							obj = null;
						}
						else
						{
							TextMeshProUGUI evoPreviousStageNameText2 = fullArtCard.m_EvoPreviousStageNameText;
							obj = ((evoPreviousStageNameText2 != null) ? ((TMP_Text)evoPreviousStageNameText2).text : null);
						}
						if (obj != null)
						{
							((TMP_Text)inputCardUI.m_FullArtCard.m_EvoPreviousStageNameText).text = customCardObject11.Name;
						}
					}
					((Behaviour)val.m_EvoPreviousStageIcon).enabled = customCardObject10.PreviousEvolutionIconEnabled;
					((Behaviour)val.m_EvoPreviousStageNameText).enabled = customCardObject10.PreviousEvolutionBoxEnabled;
				}
				else if ((int)result2 == 0)
				{
					if (flag4)
					{
						val.m_EvoBasicGrp.SetActive(true);
						((Component)val.m_EvoPreviousStageIcon).gameObject.SetActive(false);
						((Component)val.m_EvoPreviousStageNameText).gameObject.SetActive(false);
					}
					if ((Object)(object)ExtrasHandler.GetImageComponentByName(((Component)val).gameObject, "EvoBasicIcon") != (Object)null)
					{
						((Behaviour)ExtrasHandler.GetImageComponentByName(((Component)val).gameObject, "EvoBasicIcon")).enabled = customCardObject10.BasicEvolutionIconEnabled;
					}
					if ((Object)(object)ExtrasHandler.GetTextComponentByName(((Component)val).gameObject, "EvoBasicText") != (Object)null)
					{
						TextMeshProUGUI textComponentByName = ExtrasHandler.GetTextComponentByName(((Component)val).gameObject, "EvoBasicText");
						((TMP_Text)textComponentByName).text = customCardObject10.BasicEvolutionText;
						((Behaviour)textComponentByName).enabled = customCardObject10.BasicEvolutionTextEnabled;
						((TMP_Text)textComponentByName).fontSize = customCardObject10.BasicEvolutionTextFontSize;
						((TMP_Text)textComponentByName).fontSizeMin = customCardObject10.BasicEvolutionTextFontSizeMin;
						((TMP_Text)textComponentByName).fontSizeMax = customCardObject10.BasicEvolutionTextFontSizeMax;
						((Graphic)textComponentByName).color = customCardObject10.BasicEvolutionTextFontColorRGBA;
						((TMP_Text)textComponentByName).rectTransform.anchoredPosition = customCardObject10.BasicEvolutionTextPosition;
					}
				}
				((TMP_Text)val.m_EvoPreviousStageNameText).fontSize = customCardObject10.PreviousEvolutionNameFontSize;
				((TMP_Text)val.m_EvoPreviousStageNameText).fontSizeMin = customCardObject10.PreviousEvolutionNameFontSizeMin;
				((TMP_Text)val.m_EvoPreviousStageNameText).fontSizeMax = customCardObject10.PreviousEvolutionNameFontSizeMax;
				((Graphic)val.m_EvoPreviousStageNameText).color = customCardObject10.PreviousEvolutionNameFontColorRGBA;
				((TMP_Text)val.m_EvoPreviousStageNameText).rectTransform.anchoredPosition = customCardObject10.PreviousEvolutionNamePosition;
			}
		}
		if (!flag2 && !flag)
		{
			((TMP_Text)val.m_NumberText).text = ExtrasHandler.GetCorrectMonsterNumberForCardType(val, int.Parse(customCardObject.Number));
		}
		if ((Object)(object)ExtrasHandler.GetImageComponentByName(((Component)val).gameObject, "EvoBG") != (Object)null)
		{
			((Behaviour)ExtrasHandler.GetImageComponentByName(((Component)val).gameObject, "EvoBG")).enabled = customCardObject10.PreviousEvolutionBoxEnabled;
		}
		if ((Object)(object)ExtrasHandler.GetImageComponentByName(((Component)val).gameObject, "EvoBorder") != (Object)null)
		{
			((Behaviour)ExtrasHandler.GetImageComponentByName(((Component)val).gameObject, "EvoBorder")).enabled = customCardObject10.PreviousEvolutionBoxEnabled;
		}
		if ((Object)(object)ExtrasHandler.GetTextComponentByName(((Component)val).gameObject, "TitleText") != (Object)null)
		{
			TextMeshProUGUI textComponentByName2 = ExtrasHandler.GetTextComponentByName(((Component)val).gameObject, "TitleText");
			((TMP_Text)textComponentByName2).text = customCardObject.PlayEffectText;
			((Behaviour)textComponentByName2).enabled = customCardObject10.PlayEffectTextEnabled;
			((TMP_Text)textComponentByName2).fontSize = customCardObject10.PlayEffectTextFontSize;
			((TMP_Text)textComponentByName2).fontSizeMin = customCardObject10.PlayEffectTextFontSizeMin;
			((TMP_Text)textComponentByName2).fontSizeMax = customCardObject10.PlayEffectTextFontSizeMax;
			((Graphic)textComponentByName2).color = customCardObject10.PlayEffectTextFontColorRGBA;
			((TMP_Text)textComponentByName2).rectTransform.anchoredPosition = customCardObject10.PlayEffectTextPosition;
		}
		if ((Object)(object)ExtrasHandler.GetTextComponentByName(((Component)val).gameObject, "EvoText") != (Object)null)
		{
			TextMeshProUGUI textComponentByName3 = ExtrasHandler.GetTextComponentByName(((Component)val).gameObject, "EvoText");
			((Behaviour)textComponentByName3).enabled = customCardObject10.PreviousEvolutionNameEnabled;
		}
		if ((Object)(object)ExtrasHandler.GetImageComponentByName(((Component)val).gameObject, "TitleBG") != (Object)null)
		{
			((Behaviour)ExtrasHandler.GetImageComponentByName(((Component)val).gameObject, "TitleBG")).enabled = customCardObject10.PlayEffectBoxEnabled;
		}
		if ((Object)(object)val.m_FirstEditionText != (Object)null && !flag && !flag2 && ((TMP_Text)val.m_FirstEditionText).text != null)
		{
			ECardBorderType borderType = cardData.borderType;
			ECardBorderType val2 = borderType;
			switch ((int)val2)
			{
			case 0:
				((TMP_Text)val.m_FirstEditionText).text = customCardObject10.BasicEditionText;
				break;
			case 1:
				((TMP_Text)val.m_FirstEditionText).text = customCardObject10.FirstEditionText;
				break;
			case 2:
				((TMP_Text)val.m_FirstEditionText).text = customCardObject10.SilverEditionText;
				break;
			case 3:
				((TMP_Text)val.m_FirstEditionText).text = customCardObject10.GoldEditionText;
				break;
			case 4:
				((TMP_Text)val.m_FirstEditionText).text = customCardObject10.EXEditionText;
				break;
			}
		}
		((Behaviour)val.m_MonsterNameText).enabled = customCardObject10.NameEnabled;
		((TMP_Text)val.m_MonsterNameText).fontSize = customCardObject10.NameFontSize;
		((TMP_Text)val.m_MonsterNameText).fontSizeMin = customCardObject10.NameFontSizeMin;
		((TMP_Text)val.m_MonsterNameText).fontSizeMax = customCardObject10.NameFontSizeMax;
		((Graphic)val.m_MonsterNameText).color = customCardObject10.NameFontColorRGBA;
		((TMP_Text)val.m_MonsterNameText).rectTransform.anchoredPosition = customCardObject10.NamePosition;
		if (!flag && !flag2)
		{
			if ((Object)(object)val.m_FirstEditionText != (Object)null && ((TMP_Text)val.m_FirstEditionText).text != null)
			{
				((TMP_Text)val.m_FirstEditionText).fontSize = customCardObject10.EditionTextFontSize;
				((TMP_Text)val.m_FirstEditionText).fontSizeMin = customCardObject10.EditionTextFontSizeMin;
				((TMP_Text)val.m_FirstEditionText).fontSizeMax = customCardObject10.EditionTextFontSizeMax;
				((Graphic)val.m_FirstEditionText).color = customCardObject10.EditionTextFontColorRGBA;
				((TMP_Text)val.m_FirstEditionText).rectTransform.anchoredPosition = customCardObject10.EditionTextPosition;
			}
			((TMP_Text)val.m_RarityText).text = customCardObject10.Rarity;
			((Behaviour)val.m_NumberText).enabled = customCardObject10.NumberEnabled;
			((TMP_Text)val.m_NumberText).fontSize = customCardObject10.NumberFontSize;
			((TMP_Text)val.m_NumberText).fontSizeMin = customCardObject10.NumberFontSizeMin;
			((TMP_Text)val.m_NumberText).fontSizeMax = customCardObject10.NumberFontSizeMax;
			((Graphic)val.m_NumberText).color = customCardObject10.NumberFontColorRGBA;
			((TMP_Text)val.m_NumberText).rectTransform.anchoredPosition = customCardObject10.NumberPosition;
			((Behaviour)val.m_FirstEditionText).enabled = customCardObject10.EditionTextEnabled;
			((Behaviour)val.m_RarityText).enabled = customCardObject10.RarityEnabled;
			((TMP_Text)val.m_RarityText).fontSize = customCardObject10.RarityFontSize;
			((TMP_Text)val.m_RarityText).fontSizeMin = customCardObject10.RarityFontSizeMin;
			((TMP_Text)val.m_RarityText).fontSizeMax = customCardObject10.RarityFontSizeMax;
			((Graphic)val.m_RarityText).color = customCardObject10.RarityFontColorRGBA;
			((TMP_Text)val.m_RarityText).rectTransform.anchoredPosition = customCardObject10.RarityPosition;
		}
		if (flag2 && !flag)
		{
			TextMeshProUGUI rarityText = inputCardUI.m_RarityText;
			if (((rarityText != null) ? ((TMP_Text)rarityText).text : null) != null)
			{
				((TMP_Text)inputCardUI.m_RarityText).text = customCardObject.Rarity;
			}
			CardUI fullArtCard2 = inputCardUI.m_FullArtCard;
			object obj2;
			if (fullArtCard2 == null)
			{
				obj2 = null;
			}
			else
			{
				TextMeshProUGUI rarityText2 = fullArtCard2.m_RarityText;
				obj2 = ((rarityText2 != null) ? ((TMP_Text)rarityText2).text : null);
			}
			if (obj2 != null)
			{
				((TMP_Text)inputCardUI.m_FullArtCard.m_RarityText).text = customCardObject.Rarity;
			}
			TextMeshProUGUI firstEditionText = inputCardUI.m_FirstEditionText;
			if (((firstEditionText != null) ? ((TMP_Text)firstEditionText).text : null) != null)
			{
				((TMP_Text)inputCardUI.m_FirstEditionText).text = customCardObject10.EditionText;
			}
			CardUI fullArtCard3 = inputCardUI.m_FullArtCard;
			object obj3;
			if (fullArtCard3 == null)
			{
				obj3 = null;
			}
			else
			{
				TextMeshProUGUI firstEditionText2 = fullArtCard3.m_FirstEditionText;
				obj3 = ((firstEditionText2 != null) ? ((TMP_Text)firstEditionText2).text : null);
			}
			if (obj3 != null)
			{
				((TMP_Text)inputCardUI.m_FullArtCard.m_FirstEditionText).text = customCardObject10.EditionText;
			}
		}
		if (flag)
		{
			TextMeshProUGUI rarityText3 = inputCardUI.m_RarityText;
			if (((rarityText3 != null) ? ((TMP_Text)rarityText3).text : null) != null)
			{
				((TMP_Text)inputCardUI.m_RarityText).text = customCardObject10.Rarity;
			}
			CardUI fullArtCard4 = inputCardUI.m_FullArtCard;
			object obj4;
			if (fullArtCard4 == null)
			{
				obj4 = null;
			}
			else
			{
				TextMeshProUGUI rarityText4 = fullArtCard4.m_RarityText;
				obj4 = ((rarityText4 != null) ? ((TMP_Text)rarityText4).text : null);
			}
			if (obj4 != null)
			{
				((TMP_Text)inputCardUI.m_FullArtCard.m_RarityText).text = customCardObject.Rarity;
			}
			TextMeshProUGUI firstEditionText3 = inputCardUI.m_FirstEditionText;
			if (((firstEditionText3 != null) ? ((TMP_Text)firstEditionText3).text : null) != null)
			{
				((TMP_Text)inputCardUI.m_FirstEditionText).text = customCardObject10.EditionText;
			}
			CardUI fullArtCard5 = inputCardUI.m_FullArtCard;
			object obj5;
			if (fullArtCard5 == null)
			{
				obj5 = null;
			}
			else
			{
				TextMeshProUGUI firstEditionText4 = fullArtCard5.m_FirstEditionText;
				obj5 = ((firstEditionText4 != null) ? ((TMP_Text)firstEditionText4).text : null);
			}
			if (obj5 != null)
			{
				((TMP_Text)inputCardUI.m_FullArtCard.m_FirstEditionText).text = customCardObject10.EditionText;
			}
		}
		if ((Object)(object)ExtrasHandler.GetImageComponentByName(((Component)val).gameObject, "RarityImage") != (Object)null)
		{
			Image imageComponentByName = ExtrasHandler.GetImageComponentByName(((Component)val).gameObject, "RarityImage");
			((Behaviour)imageComponentByName).enabled = customCardObject10.RarityImageEnabled;
		}
		if (!flag)
		{
			((Behaviour)val.m_ChampionText).enabled = customCardObject10.ChampionTextEnabled;
			((TMP_Text)val.m_ChampionText).fontSize = customCardObject10.ChampionFontSize;
			((TMP_Text)val.m_ChampionText).fontSizeMin = customCardObject10.ChampionFontSizeMin;
			((TMP_Text)val.m_ChampionText).fontSizeMax = customCardObject10.ChampionFontSizeMax;
			((Graphic)val.m_ChampionText).color = customCardObject10.ChampionFontColorRGBA;
			((TMP_Text)val.m_ChampionText).rectTransform.anchoredPosition = customCardObject10.ChampionPosition;
		}
		if (flag && (Object)(object)ExtrasHandler.GetImageComponentByName(((Component)val).gameObject, "CardStat") != (Object)null)
		{
			((Behaviour)ExtrasHandler.GetImageComponentByName(((Component)val).gameObject, "CardStat")).enabled = customCardObject.StatBackgroundImageEnabled;
		}
		((TMP_Text)val.m_Stat1Text).text = customCardObject.Stat1;
		((Behaviour)val.m_Stat1Text).enabled = customCardObject10.Stat1Enabled;
		((TMP_Text)val.m_Stat1Text).fontSize = customCardObject10.Stat1FontSize;
		((TMP_Text)val.m_Stat1Text).fontSizeMin = customCardObject10.Stat1FontSizeMin;
		((TMP_Text)val.m_Stat1Text).fontSizeMax = customCardObject10.Stat1FontSizeMax;
		((Graphic)val.m_Stat1Text).color = customCardObject10.Stat1FontColorRGBA;
		((TMP_Text)val.m_Stat1Text).rectTransform.anchoredPosition = customCardObject10.Stat1Position;
		((TMP_Text)val.m_Stat2Text).text = customCardObject.Stat2;
		((Behaviour)val.m_Stat2Text).enabled = customCardObject10.Stat2Enabled;
		((TMP_Text)val.m_Stat2Text).fontSize = customCardObject10.Stat2FontSize;
		((TMP_Text)val.m_Stat2Text).fontSizeMin = customCardObject10.Stat2FontSizeMin;
		((TMP_Text)val.m_Stat2Text).fontSizeMax = customCardObject10.Stat2FontSizeMax;
		((Graphic)val.m_Stat2Text).color = customCardObject10.Stat2FontColorRGBA;
		((TMP_Text)val.m_Stat2Text).rectTransform.anchoredPosition = customCardObject10.Stat2Position;
		((TMP_Text)val.m_Stat3Text).text = customCardObject.Stat3;
		((Behaviour)val.m_Stat3Text).enabled = customCardObject10.Stat3Enabled;
		((TMP_Text)val.m_Stat3Text).fontSize = customCardObject10.Stat3FontSize;
		((TMP_Text)val.m_Stat3Text).fontSizeMin = customCardObject10.Stat3FontSizeMin;
		((TMP_Text)val.m_Stat3Text).fontSizeMax = customCardObject10.Stat3FontSizeMax;
		((Graphic)val.m_Stat3Text).color = customCardObject10.Stat3FontColorRGBA;
		((TMP_Text)val.m_Stat3Text).rectTransform.anchoredPosition = customCardObject10.Stat3Position;
		((TMP_Text)val.m_Stat4Text).text = customCardObject.Stat4;
		((Behaviour)val.m_Stat4Text).enabled = customCardObject10.Stat4Enabled;
		((TMP_Text)val.m_Stat4Text).fontSize = customCardObject10.Stat4FontSize;
		((TMP_Text)val.m_Stat4Text).fontSizeMin = customCardObject10.Stat4FontSizeMin;
		((TMP_Text)val.m_Stat4Text).fontSizeMax = customCardObject10.Stat4FontSizeMax;
		((Graphic)val.m_Stat4Text).color = customCardObject10.Stat4FontColorRGBA;
		((TMP_Text)val.m_Stat4Text).rectTransform.anchoredPosition = customCardObject10.Stat4Position;
		if ((Object)(object)val.m_ArtistText != (Object)null)
		{
			if (flag2 && (Object)(object)val.m_FullArtCard != (Object)null && (Object)(object)val.m_FullArtCard.m_ArtistText != (Object)null && ((TMP_Text)val.m_FullArtCard.m_ArtistText).text != null)
			{
				((TMP_Text)val.m_FullArtCard.m_ArtistText).text = customCardObject10.ArtistText;
				((Behaviour)val.m_FullArtCard.m_ArtistText).enabled = customCardObject10.ArtistTextEnabled;
				((TMP_Text)val.m_FullArtCard.m_ArtistText).fontSize = customCardObject10.ArtistTextFontSize;
				((TMP_Text)val.m_FullArtCard.m_ArtistText).fontSizeMin = customCardObject10.ArtistTextFontSizeMin;
				((TMP_Text)val.m_FullArtCard.m_ArtistText).fontSizeMax = customCardObject10.ArtistTextFontSizeMax;
				((Graphic)val.m_FullArtCard.m_ArtistText).color = customCardObject10.ArtistTextFontColorRGBA;
				((TMP_Text)val.m_FullArtCard.m_ArtistText).rectTransform.anchoredPosition = customCardObject10.ArtistTextPosition;
			}
			((TMP_Text)val.m_ArtistText).text = customCardObject10.ArtistText;
			((Behaviour)val.m_ArtistText).enabled = customCardObject10.ArtistTextEnabled;
			((TMP_Text)val.m_ArtistText).fontSize = customCardObject10.ArtistTextFontSize;
			((TMP_Text)val.m_ArtistText).fontSizeMin = customCardObject10.ArtistTextFontSizeMin;
			((TMP_Text)val.m_ArtistText).fontSizeMax = customCardObject10.ArtistTextFontSizeMax;
			((Graphic)val.m_ArtistText).color = customCardObject10.ArtistTextFontColorRGBA;
			((TMP_Text)val.m_ArtistText).rectTransform.anchoredPosition = customCardObject10.ArtistTextPosition;
		}
		if ((Object)(object)ExtrasHandler.GetTextComponentByName(((Component)val).gameObject, "CompanyText") != (Object)null)
		{
			TextMeshProUGUI textComponentByName4 = ExtrasHandler.GetTextComponentByName(((Component)val).gameObject, "CompanyText");
			((TMP_Text)textComponentByName4).text = customCardObject10.CompanyText;
			((Behaviour)textComponentByName4).enabled = customCardObject10.CompanyTextEnabled;
			((TMP_Text)textComponentByName4).fontSize = customCardObject10.CompanyTextFontSize;
			((TMP_Text)textComponentByName4).fontSizeMin = customCardObject10.CompanyTextFontSizeMin;
			((TMP_Text)textComponentByName4).fontSizeMax = customCardObject10.CompanyTextFontSizeMax;
			((Graphic)textComponentByName4).color = customCardObject10.CompanyTextFontColorRGBA;
			((TMP_Text)textComponentByName4).rectTransform.anchoredPosition = customCardObject10.CompanyTextPosition;
		}
		if (!flag)
		{
			bool removeMonsterImageSizeLimit = customCardObject10.RemoveMonsterImageSizeLimit;
			if (flag2)
			{
				if (removeMonsterImageSizeLimit)
				{
					((Behaviour)val.m_MonsterMask).enabled = false;
				}
				else if (!removeMonsterImageSizeLimit)
				{
					((Behaviour)val.m_MonsterMask).enabled = true;
				}
			}
			else if (!flag2)
			{
				((Behaviour)val.m_MonsterMask).enabled = true;
				Image imageComponentByName2 = ExtrasHandler.GetImageComponentByName(((Component)val.m_MonsterMask).gameObject, "Image");
				if (removeMonsterImageSizeLimit)
				{
					if ((Object)(object)imageComponentByName2 != (Object)null)
					{
						((MaskableGraphic)imageComponentByName2).maskable = false;
						val.m_CardFoilMaskImage.sprite = val.m_CardBackImage.sprite;
						((Behaviour)val.m_MonsterMaskImage).enabled = false;
					}
				}
				else if (!removeMonsterImageSizeLimit && (Object)(object)imageComponentByName2 != (Object)null)
				{
					((MaskableGraphic)imageComponentByName2).maskable = true;
					((Behaviour)val.m_MonsterMaskImage).enabled = true;
				}
			}
		}
		if ((Object)(object)val.m_MonsterImage != (Object)null)
		{
			((Graphic)val.m_MonsterImage).rectTransform.sizeDelta = customCardObject10.MonsterImageSize;
			((Graphic)val.m_MonsterImage).rectTransform.anchoredPosition = customCardObject10.MonsterImagePosition;
		}
		if (!cardData.isFoil)
		{
			return;
		}
		CardUI val3 = val;
		Transform val4 = ((Component)val3).transform.Find("FoilText");
		if ((Object)(object)val4 == (Object)null)
		{
			TextMeshProUGUI val5 = new GameObject("FoilText").AddComponent<TextMeshProUGUI>();
			((TMP_Text)val5).text = customCardObject10.FoilText;
			((TMP_Text)val5).transform.SetParent(((Component)val3).transform, false);
			((Behaviour)val5).enabled = false;
			if ((Object)(object)val5 != (Object)null && !((Object)(object)((TMP_Text)val5).transform.parent == (Object)(object)((Component)val3).transform))
			{
			}
		}
		Transform val6 = ((Component)val3).transform.Find("FoilText");
		if ((Object)(object)val6 != (Object)null)
		{
			TextMeshProUGUI component = ((Component)val6).GetComponent<TextMeshProUGUI>();
			((TMP_Text)component).text = customCardObject10.FoilText;
		}
	}

	public static void Log(string log)
	{
		TCGShopExpansionModPlugin.Log.LogInfo((object)log);
	}

	public static void LogError(string log)
	{
		TCGShopExpansionModPlugin.Log.LogError((object)log);
	}
}
