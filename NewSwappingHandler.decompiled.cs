using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using TCGShopExpansionMod.Patches;
using UnityEngine;

namespace TCGShopExpansionMod.Handlers;

public class NewSwappingHandler
{
	public static bool hasLoadedSaveOnce = false;

	public static Dictionary<string, CustomCardObject> customCardCache = new Dictionary<string, CustomCardObject>();

	public static Dictionary<string, CustomCardObject> fullExpansionCardCache = new Dictionary<string, CustomCardObject>();

	public static Dictionary<ECardExpansionType, Dictionary<string, CustomCardObject>> expansionCaches = new Dictionary<ECardExpansionType, Dictionary<string, CustomCardObject>>
	{
		{
			(ECardExpansionType)0,
			new Dictionary<string, CustomCardObject>()
		},
		{
			(ECardExpansionType)1,
			new Dictionary<string, CustomCardObject>()
		},
		{
			(ECardExpansionType)2,
			new Dictionary<string, CustomCardObject>()
		},
		{
			(ECardExpansionType)5,
			new Dictionary<string, CustomCardObject>()
		},
		{
			(ECardExpansionType)4,
			new Dictionary<string, CustomCardObject>()
		},
		{
			(ECardExpansionType)3,
			new Dictionary<string, CustomCardObject>()
		}
	};

	public static Sprite TryGetSpriteFromCache(List<Sprite> spriteCache, string spriteName)
	{
		bool flag = false;
		if (string.IsNullOrEmpty(spriteName))
		{
			LogError("Tried to get sprite data with an empty or null name");
			return null;
		}
		List<Sprite> list = ((!CacheHandler.IsNewMonster(spriteName)) ? spriteCache : CacheHandler.newMonstersPackImagesCache);
		if (list != null && list.Count != 0)
		{
			foreach (Sprite item in list)
			{
				if (((Object)item).name == spriteName)
				{
					return item;
				}
			}
		}
		else if (list == null)
		{
			LogError("Selected sprite cache is NULL");
		}
		string text = Path.Combine(PlayerPatches.customExpansionPackImages, CacheHandler.GetSpriteFolderNameByList(list) + Path.DirectorySeparatorChar + spriteName + ".png");
		if ((list == CacheHandler.originalCardExtrasImagesCache || list == CacheHandler.originalTetramonMonsterImageList || list == CacheHandler.originalGhostMonsterImageList) && TCGShopExpansionModPlugin.hasTextureReplacer)
		{
			string text2 = PlayerPatches.textureReplacerImagesPath + spriteName + ".png";
			if (File.Exists(text2))
			{
				text = text2;
				flag = true;
			}
		}
		if (File.Exists(text))
		{
			Sprite val = null;
			val = ((!flag) ? ImageSwapHandler.GetCustomImage(spriteName, PlayerPatches.customExpansionPackImages + CacheHandler.GetSpriteFolderNameByList(list) + Path.DirectorySeparatorChar) : ImageSwapHandler.GetCustomImage(spriteName, PlayerPatches.textureReplacerImagesPath));
			if ((Object)(object)val == (Object)null)
			{
				LogError("SPRITE TO CACHE IS NULL!!!!!!!!!");
			}
			if ((Object)(object)val != (Object)null && !spriteCache.Contains(val))
			{
				spriteCache.Add(val);
				return val;
			}
			if (spriteCache == null)
			{
				LogError("Sprite cache is null");
			}
			if (spriteCache.Contains(val))
			{
				LogError("Sprite cache already has sprite");
			}
		}
		else
		{
			LogError("Couldn't find sprite " + text);
		}
		return null;
	}

	public static string GetConfigFolderByExpansion(ECardExpansionType expansionType)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0003: Invalid comparison between Unknown and I4
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		//IL_0014: Invalid comparison between Unknown and I4
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		//IL_0025: Invalid comparison between Unknown and I4
		//IL_0034: Unknown result type (might be due to invalid IL or missing references)
		//IL_0036: Invalid comparison between Unknown and I4
		//IL_0047: Unknown result type (might be due to invalid IL or missing references)
		//IL_0049: Invalid comparison between Unknown and I4
		//IL_005a: Unknown result type (might be due to invalid IL or missing references)
		//IL_005c: Invalid comparison between Unknown and I4
		if ((int)expansionType == 5)
		{
			return "CatJobConfigs";
		}
		if ((int)expansionType == 1)
		{
			return "DestinyConfigs";
		}
		if ((int)expansionType == 4)
		{
			return "FantasyRPGConfigs";
		}
		if ((int)expansionType == 2)
		{
			return "GhostConfigs";
		}
		if ((int)expansionType == 3)
		{
			return "MegabotConfigs";
		}
		if ((int)expansionType == 0)
		{
			return "TetramonConfigs";
		}
		LogError("Couldn't find name for given config list");
		return null;
	}

	public static string GetFullExpansionsConfigByExpansion(ECardExpansionType expansionType)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0003: Invalid comparison between Unknown and I4
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		//IL_0014: Invalid comparison between Unknown and I4
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		//IL_0025: Invalid comparison between Unknown and I4
		//IL_0034: Unknown result type (might be due to invalid IL or missing references)
		//IL_0036: Invalid comparison between Unknown and I4
		//IL_0047: Unknown result type (might be due to invalid IL or missing references)
		//IL_0049: Invalid comparison between Unknown and I4
		//IL_005a: Unknown result type (might be due to invalid IL or missing references)
		//IL_005c: Invalid comparison between Unknown and I4
		if ((int)expansionType == 5)
		{
			return "CatJob";
		}
		if ((int)expansionType == 1)
		{
			return "Destiny";
		}
		if ((int)expansionType == 4)
		{
			return "FantasyRPG";
		}
		if ((int)expansionType == 2)
		{
			return "Ghost";
		}
		if ((int)expansionType == 3)
		{
			return "Megabot";
		}
		if ((int)expansionType == 0)
		{
			return "Tetramon";
		}
		LogError("Couldn't find name for given config list");
		return null;
	}

	public static void LogCacheCounts()
	{
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		foreach (KeyValuePair<ECardExpansionType, Dictionary<string, CustomCardObject>> expansionCache in expansionCaches)
		{
			ECardExpansionType key = expansionCache.Key;
			Dictionary<string, CustomCardObject> value = expansionCache.Value;
			Log($"Cache count for {key}: {value.Count}");
		}
	}

	public static void ClearAllCaches()
	{
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		//IL_001b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		//IL_0033: Unknown result type (might be due to invalid IL or missing references)
		foreach (ECardExpansionType key in expansionCaches.Keys)
		{
			expansionCaches[key].Clear();
			Log($"Cleared cache for {key}.");
		}
		fullExpansionCardCache.Clear();
		Log("All caches have been cleared.");
		Resources.UnloadUnusedAssets();
	}

	public static IEnumerable<CustomCardObject> FillAllConfigCachesIEnumerable()
	{
		Dictionary<ECardExpansionType, string> configPaths = new Dictionary<ECardExpansionType, string>
		{
			{
				(ECardExpansionType)0,
				PlayerPatches.tetramonConfigPath
			},
			{
				(ECardExpansionType)1,
				PlayerPatches.destinyConfigPath
			},
			{
				(ECardExpansionType)2,
				PlayerPatches.ghostConfigPath
			},
			{
				(ECardExpansionType)5,
				PlayerPatches.catJobConfigPath
			},
			{
				(ECardExpansionType)4,
				PlayerPatches.fantasyRPGConfigPath
			},
			{
				(ECardExpansionType)3,
				PlayerPatches.megabotConfigPath
			}
		};
		foreach (KeyValuePair<ECardExpansionType, string> kvp in configPaths)
		{
			ECardExpansionType expansionType = kvp.Key;
			string configPath = kvp.Value;
			if (!Directory.Exists(configPath))
			{
				LogError($"Config path not found for {expansionType}: {configPath}");
				continue;
			}
			string[] files = Directory.GetFiles(configPath, "*.ini");
			foreach (string iniFile in files)
			{
				string monsterName = Path.GetFileNameWithoutExtension(iniFile);
				CustomCardObject loadedCard = LoadedCustomCard(iniFile, monsterName);
				if (loadedCard != null)
				{
					expansionCaches[expansionType][monsterName] = loadedCard;
					yield return loadedCard;
				}
				else
				{
					LogError("Failed to load card from " + iniFile);
				}
			}
		}
		Log("All config caches filled.");
	}

	public static void FillAllConfigCaches()
	{
		//IL_006d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0072: Unknown result type (might be due to invalid IL or missing references)
		//IL_0092: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e8: Unknown result type (might be due to invalid IL or missing references)
		Dictionary<ECardExpansionType, string> dictionary = new Dictionary<ECardExpansionType, string>
		{
			{
				(ECardExpansionType)0,
				PlayerPatches.tetramonConfigPath
			},
			{
				(ECardExpansionType)1,
				PlayerPatches.destinyConfigPath
			},
			{
				(ECardExpansionType)2,
				PlayerPatches.ghostConfigPath
			},
			{
				(ECardExpansionType)5,
				PlayerPatches.catJobConfigPath
			},
			{
				(ECardExpansionType)4,
				PlayerPatches.fantasyRPGConfigPath
			},
			{
				(ECardExpansionType)3,
				PlayerPatches.megabotConfigPath
			}
		};
		foreach (KeyValuePair<ECardExpansionType, string> item in dictionary)
		{
			ECardExpansionType key = item.Key;
			string value = item.Value;
			if (!Directory.Exists(value))
			{
				LogError($"Config path not found for {key}: {value}");
				continue;
			}
			string[] files = Directory.GetFiles(value, "*.ini");
			foreach (string text in files)
			{
				string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(text);
				CustomCardObject customCardObject = LoadedCustomCard(text, fileNameWithoutExtension);
				if (customCardObject != null)
				{
					expansionCaches[key][fileNameWithoutExtension] = customCardObject;
				}
				else
				{
					LogError("Failed to load card from " + text);
				}
			}
		}
		Log("All config caches filled.");
	}

	public unsafe static CustomCardObject TryGetPreviousEvolutionCardFromCache(CardData cardData, EMonsterType monsterType, bool useOriginals = false)
	{
		//IL_0011: Unknown result type (might be due to invalid IL or missing references)
		//IL_0035: Unknown result type (might be due to invalid IL or missing references)
		//IL_003b: Invalid comparison between Unknown and I4
		//IL_003f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0045: Invalid comparison between Unknown and I4
		//IL_008a: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ff: Unknown result type (might be due to invalid IL or missing references)
		string text = ((object)(*(EMonsterType*)(&monsterType))/*cast due to constrained. prefix*/).ToString();
		if (text == ((object)(EMonsterType)0/*cast due to constrained. prefix*/).ToString())
		{
			return null;
		}
		bool flag = (int)cardData.expansionType == 2;
		bool flag2 = (int)cardData.borderType == 5;
		bool flag3 = false;
		if (CacheHandler.IsNewMonster(((object)System.Runtime.CompilerServices.Unsafe.As<EMonsterType, EMonsterType>(ref cardData.monsterType)/*cast due to constrained. prefix*/).ToString()))
		{
			flag3 = true;
		}
		if (!flag && flag2)
		{
			text += "FullArt";
		}
		if (!expansionCaches.TryGetValue(cardData.expansionType, out var value))
		{
			LogError("Expansion type not found in caches: " + ((object)System.Runtime.CompilerServices.Unsafe.As<ECardExpansionType, ECardExpansionType>(ref cardData.expansionType)/*cast due to constrained. prefix*/).ToString());
			return null;
		}
		if (value.TryGetValue(text, out var value2))
		{
			return value2;
		}
		string text2 = null;
		string path = (useOriginals ? PlayerPatches.originalConfigsPath : PlayerPatches.configPath);
		string path2 = "NewMonstersConfigs";
		string configFolderByExpansion = GetConfigFolderByExpansion(cardData.expansionType);
		if (flag3)
		{
			text2 = Path.Combine(path, path2, text + ".ini");
			if (useOriginals && text2 == null)
			{
				text2 = Path.Combine(PlayerPatches.configPath, path2, text + ".ini");
			}
		}
		else
		{
			text2 = Path.Combine(path, configFolderByExpansion, text + ".ini");
			if (useOriginals && text2 == null)
			{
				text2 = Path.Combine(PlayerPatches.configPath, configFolderByExpansion, text + ".ini");
			}
		}
		if (File.Exists(text2))
		{
			CustomCardObject customCardObject = LoadedCustomCard(text2, text);
			if (customCardObject != null)
			{
				value[text] = customCardObject;
				return customCardObject;
			}
			LogError("Loaded card is NULL!!!");
		}
		else
		{
			LogError("COULDNT FIND INI: " + text2);
		}
		return null;
	}

	public static CustomCardObject TryGetCardFromCache(CardData cardData, bool useOriginals = false)
	{
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		//IL_0039: Unknown result type (might be due to invalid IL or missing references)
		//IL_003f: Invalid comparison between Unknown and I4
		//IL_0043: Unknown result type (might be due to invalid IL or missing references)
		//IL_0049: Invalid comparison between Unknown and I4
		//IL_00b5: Unknown result type (might be due to invalid IL or missing references)
		//IL_012a: Unknown result type (might be due to invalid IL or missing references)
		string text = ((object)System.Runtime.CompilerServices.Unsafe.As<EMonsterType, EMonsterType>(ref cardData.monsterType)/*cast due to constrained. prefix*/).ToString();
		if (text == ((object)(EMonsterType)0/*cast due to constrained. prefix*/).ToString())
		{
			return null;
		}
		bool flag = (int)cardData.expansionType == 2;
		bool flag2 = (int)cardData.borderType == 5;
		bool flag3 = false;
		if (CacheHandler.IsNewMonster(((object)System.Runtime.CompilerServices.Unsafe.As<EMonsterType, EMonsterType>(ref cardData.monsterType)/*cast due to constrained. prefix*/).ToString()))
		{
			flag3 = true;
		}
		if (!flag && flag2)
		{
			text += "FullArt";
		}
		if (flag && flag3)
		{
			text = "Ghost_" + ((object)System.Runtime.CompilerServices.Unsafe.As<EMonsterType, EMonsterType>(ref cardData.monsterType)/*cast due to constrained. prefix*/).ToString();
		}
		if (!expansionCaches.TryGetValue(cardData.expansionType, out var value))
		{
			LogError("Expansion type not found in caches: " + ((object)System.Runtime.CompilerServices.Unsafe.As<ECardExpansionType, ECardExpansionType>(ref cardData.expansionType)/*cast due to constrained. prefix*/).ToString());
			return null;
		}
		if (value.TryGetValue(text, out var value2))
		{
			return value2;
		}
		string text2 = null;
		string path = (useOriginals ? PlayerPatches.originalConfigsPath : PlayerPatches.configPath);
		string path2 = "NewMonstersConfigs";
		string configFolderByExpansion = GetConfigFolderByExpansion(cardData.expansionType);
		if (flag3)
		{
			text2 = Path.Combine(path, path2, text + ".ini");
			if (useOriginals && text2 == null)
			{
				text2 = Path.Combine(PlayerPatches.configPath, path2, text + ".ini");
			}
		}
		else
		{
			text2 = Path.Combine(path, configFolderByExpansion, text + ".ini");
			if (useOriginals && text2 == null)
			{
				text2 = Path.Combine(PlayerPatches.configPath, configFolderByExpansion, text + ".ini");
			}
		}
		if (File.Exists(text2))
		{
			CustomCardObject customCardObject = LoadedCustomCard(text2, text);
			if (customCardObject != null)
			{
				value[text] = customCardObject;
				return customCardObject;
			}
			LogError("Loaded card is NULL!!!");
		}
		else
		{
			LogError("COULDNT FIND INI: " + text2);
		}
		return null;
	}

	public static CustomCardObject TryGetFullExpansionCardFromCache(CardData cardData, bool useOriginal = false)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Invalid comparison between Unknown and I4
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0020: Invalid comparison between Unknown and I4
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		//IL_002a: Invalid comparison between Unknown and I4
		//IL_002e: Unknown result type (might be due to invalid IL or missing references)
		if ((int)cardData.monsterType == 0)
		{
			return null;
		}
		bool flag = (int)cardData.expansionType == 2;
		bool flag2 = (int)cardData.borderType == 5;
		string text = GetFullExpansionsConfigByExpansion(cardData.expansionType);
		if (!flag && flag2)
		{
			text += "FullArt";
		}
		if (fullExpansionCardCache.TryGetValue(text, out var value))
		{
			return value;
		}
		string text2 = Path.Combine(PlayerPatches.fullExpansionsConfigPath, text + ".ini");
		if (useOriginal)
		{
			text2 = Path.Combine(PlayerPatches.originalFullExpansionsConfigPath, text + ".ini");
		}
		if (File.Exists(text2))
		{
			CustomCardObject customCardObject = LoadedCustomCard(text2, text);
			if (customCardObject != null)
			{
				fullExpansionCardCache[text] = customCardObject;
				return customCardObject;
			}
			LogError("Loaded card is NULL!!!");
		}
		else
		{
			LogError("COULDNT FIND INI: " + text2);
		}
		return null;
	}

	public static CustomCardObject LoadedCustomCard(string fileToLoad, string fileNameOnly)
	{
		//IL_00cb: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ed: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ef: Unknown result type (might be due to invalid IL or missing references)
		//IL_0172: Unknown result type (might be due to invalid IL or missing references)
		//IL_0178: Unknown result type (might be due to invalid IL or missing references)
		//IL_017a: Unknown result type (might be due to invalid IL or missing references)
		//IL_018e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0194: Unknown result type (might be due to invalid IL or missing references)
		//IL_0196: Unknown result type (might be due to invalid IL or missing references)
		//IL_022c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0232: Unknown result type (might be due to invalid IL or missing references)
		//IL_0234: Unknown result type (might be due to invalid IL or missing references)
		//IL_0248: Unknown result type (might be due to invalid IL or missing references)
		//IL_024e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0250: Unknown result type (might be due to invalid IL or missing references)
		//IL_02e6: Unknown result type (might be due to invalid IL or missing references)
		//IL_02ec: Unknown result type (might be due to invalid IL or missing references)
		//IL_02ee: Unknown result type (might be due to invalid IL or missing references)
		//IL_0302: Unknown result type (might be due to invalid IL or missing references)
		//IL_0308: Unknown result type (might be due to invalid IL or missing references)
		//IL_030a: Unknown result type (might be due to invalid IL or missing references)
		//IL_03a0: Unknown result type (might be due to invalid IL or missing references)
		//IL_03a6: Unknown result type (might be due to invalid IL or missing references)
		//IL_03a8: Unknown result type (might be due to invalid IL or missing references)
		//IL_03bc: Unknown result type (might be due to invalid IL or missing references)
		//IL_03c2: Unknown result type (might be due to invalid IL or missing references)
		//IL_03c4: Unknown result type (might be due to invalid IL or missing references)
		//IL_045a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0460: Unknown result type (might be due to invalid IL or missing references)
		//IL_0462: Unknown result type (might be due to invalid IL or missing references)
		//IL_0476: Unknown result type (might be due to invalid IL or missing references)
		//IL_047c: Unknown result type (might be due to invalid IL or missing references)
		//IL_047e: Unknown result type (might be due to invalid IL or missing references)
		//IL_052b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0531: Unknown result type (might be due to invalid IL or missing references)
		//IL_0533: Unknown result type (might be due to invalid IL or missing references)
		//IL_0547: Unknown result type (might be due to invalid IL or missing references)
		//IL_054d: Unknown result type (might be due to invalid IL or missing references)
		//IL_054f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0658: Unknown result type (might be due to invalid IL or missing references)
		//IL_065e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0660: Unknown result type (might be due to invalid IL or missing references)
		//IL_0674: Unknown result type (might be due to invalid IL or missing references)
		//IL_067a: Unknown result type (might be due to invalid IL or missing references)
		//IL_067c: Unknown result type (might be due to invalid IL or missing references)
		//IL_06ff: Unknown result type (might be due to invalid IL or missing references)
		//IL_0705: Unknown result type (might be due to invalid IL or missing references)
		//IL_0707: Unknown result type (might be due to invalid IL or missing references)
		//IL_071b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0721: Unknown result type (might be due to invalid IL or missing references)
		//IL_0723: Unknown result type (might be due to invalid IL or missing references)
		//IL_07b9: Unknown result type (might be due to invalid IL or missing references)
		//IL_07bf: Unknown result type (might be due to invalid IL or missing references)
		//IL_07c1: Unknown result type (might be due to invalid IL or missing references)
		//IL_07d5: Unknown result type (might be due to invalid IL or missing references)
		//IL_07db: Unknown result type (might be due to invalid IL or missing references)
		//IL_07dd: Unknown result type (might be due to invalid IL or missing references)
		//IL_0860: Unknown result type (might be due to invalid IL or missing references)
		//IL_0866: Unknown result type (might be due to invalid IL or missing references)
		//IL_0868: Unknown result type (might be due to invalid IL or missing references)
		//IL_087c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0882: Unknown result type (might be due to invalid IL or missing references)
		//IL_0884: Unknown result type (might be due to invalid IL or missing references)
		//IL_0907: Unknown result type (might be due to invalid IL or missing references)
		//IL_090d: Unknown result type (might be due to invalid IL or missing references)
		//IL_090f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0923: Unknown result type (might be due to invalid IL or missing references)
		//IL_0929: Unknown result type (might be due to invalid IL or missing references)
		//IL_092b: Unknown result type (might be due to invalid IL or missing references)
		//IL_09ae: Unknown result type (might be due to invalid IL or missing references)
		//IL_09b4: Unknown result type (might be due to invalid IL or missing references)
		//IL_09b6: Unknown result type (might be due to invalid IL or missing references)
		//IL_09ca: Unknown result type (might be due to invalid IL or missing references)
		//IL_09d0: Unknown result type (might be due to invalid IL or missing references)
		//IL_09d2: Unknown result type (might be due to invalid IL or missing references)
		//IL_0a55: Unknown result type (might be due to invalid IL or missing references)
		//IL_0a5b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0a5d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0a71: Unknown result type (might be due to invalid IL or missing references)
		//IL_0a77: Unknown result type (might be due to invalid IL or missing references)
		//IL_0a79: Unknown result type (might be due to invalid IL or missing references)
		//IL_0afc: Unknown result type (might be due to invalid IL or missing references)
		//IL_0b02: Unknown result type (might be due to invalid IL or missing references)
		//IL_0b04: Unknown result type (might be due to invalid IL or missing references)
		//IL_0b18: Unknown result type (might be due to invalid IL or missing references)
		//IL_0b1e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0b20: Unknown result type (might be due to invalid IL or missing references)
		//IL_0b47: Unknown result type (might be due to invalid IL or missing references)
		//IL_0b4d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0b4f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0b63: Unknown result type (might be due to invalid IL or missing references)
		//IL_0b69: Unknown result type (might be due to invalid IL or missing references)
		//IL_0b6b: Unknown result type (might be due to invalid IL or missing references)
		if (fileToLoad != null)
		{
			if (File.Exists(fileToLoad))
			{
				IniFile.Load(fileToLoad);
				CustomCardObject customCardObject = new CustomCardObject();
				if (fileNameOnly.StartsWith("Ghost_"))
				{
					fileNameOnly = fileNameOnly.Substring("Ghost_".Length);
				}
				customCardObject.Header = fileNameOnly;
				customCardObject.Name = IniFile.GetStringValue(fileNameOnly, "Name");
				customCardObject.NameEnabled = IniFile.GetBoolValue(fileNameOnly, "Name Enabled");
				customCardObject.NameFontSize = IniFile.GetFloatValue(fileNameOnly, "Name Font Size");
				customCardObject.NameFontSizeMin = IniFile.GetFloatValue(fileNameOnly, "Name Font Size Min");
				customCardObject.NameFontSizeMax = IniFile.GetFloatValue(fileNameOnly, "Name Font Size Max");
				customCardObject.NameFontColorRGBA = IniFile.GetColorValue(fileNameOnly, "Name Font Color RGBA");
				customCardObject.NamePosition = IniFile.GetVector2Value(fileNameOnly, "Name Position");
				customCardObject.Description = IniFile.GetStringValue(fileNameOnly, "Description");
				customCardObject.DescriptionEnabled = IniFile.GetBoolValue(fileNameOnly, "Description Enabled");
				customCardObject.DescriptionFontSize = IniFile.GetFloatValue(fileNameOnly, "Description Font Size");
				customCardObject.DescriptionFontSizeMin = IniFile.GetFloatValue(fileNameOnly, "Description Font Size Min");
				customCardObject.DescriptionFontSizeMax = IniFile.GetFloatValue(fileNameOnly, "Description Font Size Max");
				customCardObject.DescriptionFontColorRGBA = IniFile.GetColorValue(fileNameOnly, "Description Font Color RGBA");
				customCardObject.DescriptionPosition = IniFile.GetVector2Value(fileNameOnly, "Description Position");
				customCardObject.IndividualOverrides = IniFile.GetBoolValue(fileNameOnly, "Individual Overrides");
				customCardObject.Number = IniFile.GetStringValue(fileNameOnly, "Number");
				customCardObject.NumberEnabled = IniFile.GetBoolValue(fileNameOnly, "Number Enabled");
				customCardObject.NumberFontSize = IniFile.GetFloatValue(fileNameOnly, "Number Font Size");
				customCardObject.NumberFontSizeMin = IniFile.GetFloatValue(fileNameOnly, "Number Font Size Min");
				customCardObject.NumberFontSizeMax = IniFile.GetFloatValue(fileNameOnly, "Number Font Size Max");
				customCardObject.NumberFontColorRGBA = IniFile.GetColorValue(fileNameOnly, "Number Font Color RGBA");
				customCardObject.NumberPosition = IniFile.GetVector2Value(fileNameOnly, "Number Position");
				customCardObject.BasicEvolutionIconEnabled = IniFile.GetBoolValue(fileNameOnly, "Basic Evolution Icon Enabled");
				customCardObject.BasicEvolutionText = IniFile.GetStringValue(fileNameOnly, "Basic Evolution Text");
				customCardObject.BasicEvolutionTextEnabled = IniFile.GetBoolValue(fileNameOnly, "Basic Evolution Text Enabled");
				customCardObject.BasicEvolutionTextFontSize = IniFile.GetFloatValue(fileNameOnly, "Basic Evolution Text Font Size");
				customCardObject.BasicEvolutionTextFontSizeMin = IniFile.GetFloatValue(fileNameOnly, "Basic Evolution Text Font Size Min");
				customCardObject.BasicEvolutionTextFontSizeMax = IniFile.GetFloatValue(fileNameOnly, "Basic Evolution Text Font Size Max");
				customCardObject.BasicEvolutionTextFontColorRGBA = IniFile.GetColorValue(fileNameOnly, "Basic Evolution Text Font Color RGBA");
				customCardObject.BasicEvolutionTextPosition = IniFile.GetVector2Value(fileNameOnly, "Basic Evolution Text Position");
				customCardObject.PreviousEvolution = IniFile.GetStringValue(fileNameOnly, "Previous Evolution");
				customCardObject.PreviousEvolutionIconEnabled = IniFile.GetBoolValue(fileNameOnly, "Previous Evolution Icon Enabled");
				customCardObject.PreviousEvolutionNameEnabled = IniFile.GetBoolValue(fileNameOnly, "Previous Evolution Name Enabled");
				customCardObject.PreviousEvolutionNameFontSize = IniFile.GetFloatValue(fileNameOnly, "Previous Evolution Name Font Size");
				customCardObject.PreviousEvolutionNameFontSizeMin = IniFile.GetFloatValue(fileNameOnly, "Previous Evolution Name Font Size Min");
				customCardObject.PreviousEvolutionNameFontSizeMax = IniFile.GetFloatValue(fileNameOnly, "Previous Evolution Name Font Size Max");
				customCardObject.PreviousEvolutionNameFontColorRGBA = IniFile.GetColorValue(fileNameOnly, "Previous Evolution Name Font Color RGBA");
				customCardObject.PreviousEvolutionNamePosition = IniFile.GetVector2Value(fileNameOnly, "Previous Evolution Name Position");
				customCardObject.PreviousEvolutionBoxEnabled = IniFile.GetBoolValue(fileNameOnly, "Previous Evolution Box Enabled");
				customCardObject.PlayEffectText = IniFile.GetStringValue(fileNameOnly, "Play Effect Text");
				customCardObject.PlayEffectTextEnabled = IniFile.GetBoolValue(fileNameOnly, "Play Effect Text Enabled");
				customCardObject.PlayEffectTextFontSize = IniFile.GetFloatValue(fileNameOnly, "Play Effect Text Font Size");
				customCardObject.PlayEffectTextFontSizeMin = IniFile.GetFloatValue(fileNameOnly, "Play Effect Text Font Size Min");
				customCardObject.PlayEffectTextFontSizeMax = IniFile.GetFloatValue(fileNameOnly, "Play Effect Text Font Size Max");
				customCardObject.PlayEffectTextFontColorRGBA = IniFile.GetColorValue(fileNameOnly, "Play Effect Text Font Color RGBA");
				customCardObject.PlayEffectTextPosition = IniFile.GetVector2Value(fileNameOnly, "Play Effect Text Position");
				customCardObject.PlayEffectBoxEnabled = IniFile.GetBoolValue(fileNameOnly, "Play Effect Box Enabled");
				customCardObject.FoilText = IniFile.GetStringValue(fileNameOnly, "Foil Text");
				customCardObject.Rarity = IniFile.GetStringValue(fileNameOnly, "Rarity");
				customCardObject.RarityEnabled = IniFile.GetBoolValue(fileNameOnly, "Rarity Enabled");
				customCardObject.RarityFontSize = IniFile.GetFloatValue(fileNameOnly, "Rarity Font Size");
				customCardObject.RarityFontSizeMin = IniFile.GetFloatValue(fileNameOnly, "Rarity Font Size Min");
				customCardObject.RarityFontSizeMax = IniFile.GetFloatValue(fileNameOnly, "Rarity Font Size Max");
				customCardObject.RarityFontColorRGBA = IniFile.GetColorValue(fileNameOnly, "Rarity Font Color RGBA");
				customCardObject.RarityPosition = IniFile.GetVector2Value(fileNameOnly, "Rarity Position");
				customCardObject.RarityImageEnabled = IniFile.GetBoolValue(fileNameOnly, "Rarity Image Enabled");
				customCardObject.EditionText = IniFile.GetStringValue(fileNameOnly, "Edition Text");
				customCardObject.BasicEditionText = IniFile.GetStringValue(fileNameOnly, "Basic Edition Text");
				customCardObject.FirstEditionText = IniFile.GetStringValue(fileNameOnly, "First Edition Text");
				customCardObject.GoldEditionText = IniFile.GetStringValue(fileNameOnly, "Gold Edition Text");
				customCardObject.SilverEditionText = IniFile.GetStringValue(fileNameOnly, "Silver Edition Text");
				customCardObject.EXEditionText = IniFile.GetStringValue(fileNameOnly, "EX Edition Text");
				customCardObject.EditionTextEnabled = IniFile.GetBoolValue(fileNameOnly, "Edition Text Enabled");
				customCardObject.EditionTextFontSize = IniFile.GetFloatValue(fileNameOnly, "Edition Text Font Size");
				customCardObject.EditionTextFontSizeMin = IniFile.GetFloatValue(fileNameOnly, "Edition Text Font Size Min");
				customCardObject.EditionTextFontSizeMax = IniFile.GetFloatValue(fileNameOnly, "Edition Text Font Size Max");
				customCardObject.EditionTextFontColorRGBA = IniFile.GetColorValue(fileNameOnly, "Edition Text Font Color RGBA");
				customCardObject.EditionTextPosition = IniFile.GetVector2Value(fileNameOnly, "Edition Text Position");
				customCardObject.ChampionText = IniFile.GetStringValue(fileNameOnly, "Champion Text");
				customCardObject.ChampionTextEnabled = IniFile.GetBoolValue(fileNameOnly, "Champion Text Enabled");
				customCardObject.ChampionFontSize = IniFile.GetFloatValue(fileNameOnly, "Champion Font Size");
				customCardObject.ChampionFontSizeMin = IniFile.GetFloatValue(fileNameOnly, "Champion Font Size Min");
				customCardObject.ChampionFontSizeMax = IniFile.GetFloatValue(fileNameOnly, "Champion Font Size Max");
				customCardObject.ChampionFontColorRGBA = IniFile.GetColorValue(fileNameOnly, "Champion Font Color RGBA");
				customCardObject.ChampionPosition = IniFile.GetVector2Value(fileNameOnly, "Champion Position");
				customCardObject.StatBackgroundImageEnabled = IniFile.GetBoolValue(fileNameOnly, "Stat Background Image Enabled");
				customCardObject.Stat1 = IniFile.GetStringValue(fileNameOnly, "Stat1");
				customCardObject.Stat1Enabled = IniFile.GetBoolValue(fileNameOnly, "Stat1 Enabled");
				customCardObject.Stat1FontSize = IniFile.GetFloatValue(fileNameOnly, "Stat1 Font Size");
				customCardObject.Stat1FontSizeMin = IniFile.GetFloatValue(fileNameOnly, "Stat1 Font Size Min");
				customCardObject.Stat1FontSizeMax = IniFile.GetFloatValue(fileNameOnly, "Stat1 Font Size Max");
				customCardObject.Stat1FontColorRGBA = IniFile.GetColorValue(fileNameOnly, "Stat1 Font Color RGBA");
				customCardObject.Stat1Position = IniFile.GetVector2Value(fileNameOnly, "Stat1 Position");
				customCardObject.Stat2 = IniFile.GetStringValue(fileNameOnly, "Stat2");
				customCardObject.Stat2Enabled = IniFile.GetBoolValue(fileNameOnly, "Stat2 Enabled");
				customCardObject.Stat2FontSize = IniFile.GetFloatValue(fileNameOnly, "Stat2 Font Size");
				customCardObject.Stat2FontSizeMin = IniFile.GetFloatValue(fileNameOnly, "Stat2 Font Size Min");
				customCardObject.Stat2FontSizeMax = IniFile.GetFloatValue(fileNameOnly, "Stat2 Font Size Max");
				customCardObject.Stat2FontColorRGBA = IniFile.GetColorValue(fileNameOnly, "Stat2 Font Color RGBA");
				customCardObject.Stat2Position = IniFile.GetVector2Value(fileNameOnly, "Stat2 Position");
				customCardObject.Stat3 = IniFile.GetStringValue(fileNameOnly, "Stat3");
				customCardObject.Stat3Enabled = IniFile.GetBoolValue(fileNameOnly, "Stat3 Enabled");
				customCardObject.Stat3FontSize = IniFile.GetFloatValue(fileNameOnly, "Stat3 Font Size");
				customCardObject.Stat3FontSizeMin = IniFile.GetFloatValue(fileNameOnly, "Stat3 Font Size Min");
				customCardObject.Stat3FontSizeMax = IniFile.GetFloatValue(fileNameOnly, "Stat3 Font Size Max");
				customCardObject.Stat3FontColorRGBA = IniFile.GetColorValue(fileNameOnly, "Stat3 Font Color RGBA");
				customCardObject.Stat3Position = IniFile.GetVector2Value(fileNameOnly, "Stat3 Position");
				customCardObject.Stat4 = IniFile.GetStringValue(fileNameOnly, "Stat4");
				customCardObject.Stat4Enabled = IniFile.GetBoolValue(fileNameOnly, "Stat4 Enabled");
				customCardObject.Stat4FontSize = IniFile.GetFloatValue(fileNameOnly, "Stat4 Font Size");
				customCardObject.Stat4FontSizeMin = IniFile.GetFloatValue(fileNameOnly, "Stat4 Font Size Min");
				customCardObject.Stat4FontSizeMax = IniFile.GetFloatValue(fileNameOnly, "Stat4 Font Size Max");
				customCardObject.Stat4FontColorRGBA = IniFile.GetColorValue(fileNameOnly, "Stat4 Font Color RGBA");
				customCardObject.Stat4Position = IniFile.GetVector2Value(fileNameOnly, "Stat4 Position");
				customCardObject.ArtistText = IniFile.GetStringValue(fileNameOnly, "Artist Text");
				customCardObject.ArtistTextEnabled = IniFile.GetBoolValue(fileNameOnly, "Artist Text Enabled");
				customCardObject.ArtistTextFontSize = IniFile.GetFloatValue(fileNameOnly, "Artist Text Font Size");
				customCardObject.ArtistTextFontSizeMin = IniFile.GetFloatValue(fileNameOnly, "Artist Text Font Size Min");
				customCardObject.ArtistTextFontSizeMax = IniFile.GetFloatValue(fileNameOnly, "Artist Text Font Size Max");
				customCardObject.ArtistTextFontColorRGBA = IniFile.GetColorValue(fileNameOnly, "Artist Text Font Color RGBA");
				customCardObject.ArtistTextPosition = IniFile.GetVector2Value(fileNameOnly, "Artist Text Position");
				customCardObject.CompanyText = IniFile.GetStringValue(fileNameOnly, "Company Text");
				customCardObject.CompanyTextEnabled = IniFile.GetBoolValue(fileNameOnly, "Company Text Enabled");
				customCardObject.CompanyTextFontSize = IniFile.GetFloatValue(fileNameOnly, "Company Text Font Size");
				customCardObject.CompanyTextFontSizeMin = IniFile.GetFloatValue(fileNameOnly, "Company Text Font Size Min");
				customCardObject.CompanyTextFontSizeMax = IniFile.GetFloatValue(fileNameOnly, "Company Text Font Size Max");
				customCardObject.CompanyTextFontColorRGBA = IniFile.GetColorValue(fileNameOnly, "Company Text Font Color RGBA");
				customCardObject.CompanyTextPosition = IniFile.GetVector2Value(fileNameOnly, "Company Text Position");
				customCardObject.RemoveMonsterImageSizeLimit = IniFile.GetBoolValue(fileNameOnly, "Remove Monster Image Size Limit");
				customCardObject.MonsterImageSize = IniFile.GetVector2Value(fileNameOnly, "Monster Image Size");
				customCardObject.MonsterImagePosition = IniFile.GetVector2Value(fileNameOnly, "Monster Image Position");
				return customCardObject;
			}
			LogError("Can't find INI file - " + fileToLoad);
			return null;
		}
		return null;
	}

	public static string GetCardName(CardData cardData)
	{
		return TryGetCardFromCache(cardData)?.Name;
	}

	public static string GetCardFullName(CardData cardData)
	{
		//IL_002f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0035: Invalid comparison between Unknown and I4
		//IL_004c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0052: Invalid comparison between Unknown and I4
		//IL_00fa: Unknown result type (might be due to invalid IL or missing references)
		//IL_0100: Invalid comparison between Unknown and I4
		//IL_0066: Unknown result type (might be due to invalid IL or missing references)
		//IL_006c: Invalid comparison between Unknown and I4
		//IL_0117: Unknown result type (might be due to invalid IL or missing references)
		//IL_011d: Invalid comparison between Unknown and I4
		//IL_0080: Unknown result type (might be due to invalid IL or missing references)
		//IL_0086: Invalid comparison between Unknown and I4
		//IL_0131: Unknown result type (might be due to invalid IL or missing references)
		//IL_0137: Invalid comparison between Unknown and I4
		//IL_009a: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a0: Invalid comparison between Unknown and I4
		//IL_014b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0151: Invalid comparison between Unknown and I4
		//IL_00b4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ba: Invalid comparison between Unknown and I4
		//IL_0165: Unknown result type (might be due to invalid IL or missing references)
		//IL_016b: Invalid comparison between Unknown and I4
		//IL_017f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0185: Invalid comparison between Unknown and I4
		CustomCardObject customCardObject = TryGetCardFromCache(cardData);
		CustomCardObject customCardObject2 = TryGetFullExpansionCardFromCache(cardData);
		string text = null;
		string text2 = null;
		string text3 = null;
		string foilText = customCardObject2.FoilText;
		if (customCardObject2 != null)
		{
			if ((int)cardData.borderType == 0)
			{
				text2 = customCardObject2.BasicEditionText;
			}
			else if ((int)cardData.borderType == 1)
			{
				text2 = customCardObject2.FirstEditionText;
			}
			else if ((int)cardData.borderType == 3)
			{
				text2 = customCardObject2.GoldEditionText;
			}
			else if ((int)cardData.borderType == 2)
			{
				text2 = customCardObject2.SilverEditionText;
			}
			else if ((int)cardData.borderType == 4)
			{
				text2 = customCardObject2.EXEditionText;
			}
			else if ((int)cardData.borderType == 5)
			{
				text2 = customCardObject2.EditionText;
			}
			foilText = customCardObject2.FoilText;
		}
		if (customCardObject != null)
		{
			text = customCardObject.Name;
			if (customCardObject.IndividualOverrides)
			{
				if ((int)cardData.borderType == 0)
				{
					text2 = customCardObject.BasicEditionText;
				}
				else if ((int)cardData.borderType == 1)
				{
					text2 = customCardObject.FirstEditionText;
				}
				else if ((int)cardData.borderType == 3)
				{
					text2 = customCardObject.GoldEditionText;
				}
				else if ((int)cardData.borderType == 2)
				{
					text2 = customCardObject.SilverEditionText;
				}
				else if ((int)cardData.borderType == 4)
				{
					text2 = customCardObject.EXEditionText;
				}
				else if ((int)cardData.borderType == 5)
				{
					text2 = customCardObject.EditionText;
				}
				foilText = customCardObject.FoilText;
			}
			text3 = customCardObject.Rarity;
		}
		if (text != null && text2 != null && text3 != null && foilText != null)
		{
			if (cardData.isFoil)
			{
				return text + " - " + text2 + " " + text3 + " " + foilText;
			}
			return text + " - " + text2 + " " + text3;
		}
		return null;
	}

	public static string GetCardFullEditionName(CardData cardData)
	{
		return TryGetCardFromCache(cardData)?.Name;
	}

	public static void DoFirstWorldLoad()
	{
		if (!hasLoadedSaveOnce && CSingleton<CGameManager>.Instance.m_IsGameLevel)
		{
			DoFirstLoad();
		}
		else if (hasLoadedSaveOnce && CSingleton<CGameManager>.Instance.m_IsGameLevel)
		{
			DoReload();
		}
	}

	public static void DoFirstLoad()
	{
		ImageSwapHandler.SetPlayCardImages();
		ExtrasHandler.SwapPackNames();
		ExtrasHandler.SwapNewPackItemImages();
		ExtrasHandler.AddHiddenCards();
		ReplaceBaseMonsterIcons();
		ReplaceAllExpansionMonsterIcons();
		SetCardExtrasImages();
		Resources.UnloadUnusedAssets();
	}

	public static void DoReload()
	{
		ImageSwapHandler.SetPlayCardImages();
		ExtrasHandler.SwapPackNames();
		ExtrasHandler.SwapNewPackItemImages();
		ReplaceBaseMonsterIcons();
		ReplaceAllExpansionMonsterIcons();
		SetCardExtrasImages();
		Resources.UnloadUnusedAssets();
	}

	public static void DoNewExpansionsImagesReload()
	{
		ReplaceAllExpansionMonsterIcons();
		SetCardExtrasImages();
		Resources.UnloadUnusedAssets();
	}

	public static void DoOriginalExpansionsImagesReload()
	{
		ReplaceBaseMonsterIcons();
		SetCardExtrasImages();
		Resources.UnloadUnusedAssets();
	}

	public static void DoFullConfigReload()
	{
		ClearAllCaches();
		ImageSwapHandler.SetPlayCardImages();
		ExtrasHandler.SwapPackNames();
		ExtrasHandler.SwapNewPackItemImages();
		Resources.UnloadUnusedAssets();
	}

	public static void DoNewExpansionsConfigReload()
	{
		ClearNewExpansionsConfigCaches();
		Resources.UnloadUnusedAssets();
	}

	public static void DoOriginalExpansionsConfigReload()
	{
		ClearOriginalExpansionsConfigCaches();
		Resources.UnloadUnusedAssets();
	}

	public static void DoConfigCacheFilling()
	{
		ClearAllCaches();
		FillAllConfigCaches();
		Resources.UnloadUnusedAssets();
	}

	public static void ClearOriginalExpansionsConfigCaches()
	{
		//IL_002b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0030: Unknown result type (might be due to invalid IL or missing references)
		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
		//IL_0049: Unknown result type (might be due to invalid IL or missing references)
		//IL_005a: Unknown result type (might be due to invalid IL or missing references)
		List<ECardExpansionType> list = new List<ECardExpansionType>
		{
			(ECardExpansionType)0,
			(ECardExpansionType)1,
			(ECardExpansionType)2
		};
		foreach (ECardExpansionType item in list)
		{
			if (expansionCaches.ContainsKey(item))
			{
				expansionCaches[item].Clear();
				Log($"Cleared cache for {item}.");
			}
		}
		List<string> list2 = new List<string> { "Destiny", "DestinyFullArt", "Ghost", "Tetramon", "TetramonFullArt" };
		foreach (string item2 in list2)
		{
			if (fullExpansionCardCache.ContainsKey(item2))
			{
				fullExpansionCardCache.Remove(item2);
			}
		}
		Log("Original expansion caches have been cleared.");
		Resources.UnloadUnusedAssets();
	}

	public static void ClearNewExpansionsConfigCaches()
	{
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		//IL_002f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0041: Unknown result type (might be due to invalid IL or missing references)
		//IL_0052: Unknown result type (might be due to invalid IL or missing references)
		List<ECardExpansionType> list = new List<ECardExpansionType>
		{
			(ECardExpansionType)5,
			(ECardExpansionType)4
		};
		foreach (ECardExpansionType item in list)
		{
			if (expansionCaches.ContainsKey(item))
			{
				expansionCaches[item].Clear();
				Log($"Cleared cache for {item}.");
			}
		}
		List<string> list2 = new List<string> { "CatJob", "CatJobFullArt", "FantasyRPG", "FantasyRPGFullArt", "Megabot", "MegabotFullArt" };
		foreach (string item2 in list2)
		{
			if (fullExpansionCardCache.ContainsKey(item2))
			{
				fullExpansionCardCache.Remove(item2);
			}
		}
		Log("New expansion caches have been cleared.");
		Resources.UnloadUnusedAssets();
	}

	public static void SetCardExtrasImages()
	{
		ReplaceCardBorders();
		ReplaceCardBGs();
		ReplaceCardFronts();
	}

	public static void ReplaceCardEditionBorders()
	{
		List<Sprite> cardBorderSpriteList = CSingleton<CardUI>.Instance.m_CardBorderSpriteList;
		List<Sprite> cardBorderSpriteList2 = CSingleton<CardUI>.Instance.m_CardBorderSpriteList;
		if (cardBorderSpriteList != null)
		{
			Log("Its not null!!!");
			Log("Count is " + cardBorderSpriteList.Count);
		}
		else
		{
			Log("Its null");
		}
	}

	public static void ReplaceCardBorders()
	{
		List<Sprite> cardBorderList = CSingleton<InventoryBase>.Instance.m_MonsterData_SO.m_CardBorderList;
		for (int i = 0; i < cardBorderList.Count; i++)
		{
			if (!((Object)(object)cardBorderList[i] != (Object)null))
			{
				continue;
			}
			if (TCGShopExpansionModPlugin.CustomBaseMonsterImages.Value)
			{
				Sprite val = TryGetSpriteFromCache(CacheHandler.cardExtrasImagesCache, ((Object)cardBorderList[i]).name);
				if ((Object)(object)val != (Object)null)
				{
					cardBorderList[i] = val;
				}
			}
			else if (!TCGShopExpansionModPlugin.CustomBaseMonsterImages.Value)
			{
				Sprite val2 = TryGetSpriteFromCache(CacheHandler.originalCardExtrasImagesCache, ((Object)cardBorderList[i]).name);
				if ((Object)(object)val2 != (Object)null)
				{
					cardBorderList[i] = val2;
				}
			}
		}
	}

	public static void ReplaceCardBGs()
	{
		List<Sprite> cardBGList = CSingleton<InventoryBase>.Instance.m_MonsterData_SO.m_CardBGList;
		for (int i = 0; i < cardBGList.Count; i++)
		{
			if (!((Object)(object)cardBGList[i] != (Object)null))
			{
				continue;
			}
			if (((Object)cardBGList[i]).name == "CardBG_CatJob" || ((Object)cardBGList[i]).name == "CardBG_FantasyRPG" || ((Object)cardBGList[i]).name == "CardBG_Megabot")
			{
				if (TCGShopExpansionModPlugin.CustomNewExpansionConfigs.Value)
				{
					Sprite val = TryGetSpriteFromCache(CacheHandler.cardExtrasImagesCache, ((Object)cardBGList[i]).name);
					if ((Object)(object)val != (Object)null)
					{
						cardBGList[i] = val;
					}
				}
				else if (!TCGShopExpansionModPlugin.CustomNewExpansionImages.Value)
				{
					Sprite val2 = TryGetSpriteFromCache(CacheHandler.originalCardExtrasImagesCache, ((Object)cardBGList[i]).name);
					if ((Object)(object)val2 != (Object)null)
					{
						cardBGList[i] = val2;
					}
				}
			}
			else if (TCGShopExpansionModPlugin.CustomBaseMonsterImages.Value)
			{
				Sprite val3 = TryGetSpriteFromCache(CacheHandler.cardExtrasImagesCache, ((Object)cardBGList[i]).name);
				if ((Object)(object)val3 != (Object)null)
				{
					cardBGList[i] = val3;
				}
			}
			else if (!TCGShopExpansionModPlugin.CustomBaseMonsterImages.Value)
			{
				Sprite val4 = TryGetSpriteFromCache(CacheHandler.originalCardExtrasImagesCache, ((Object)cardBGList[i]).name);
				if ((Object)(object)val4 != (Object)null)
				{
					cardBGList[i] = val4;
				}
			}
		}
	}

	public static void ReplaceCardFronts()
	{
		List<Sprite> cardFrontImageList = CSingleton<InventoryBase>.Instance.m_MonsterData_SO.m_CardFrontImageList;
		for (int i = 0; i < cardFrontImageList.Count; i++)
		{
			if (!((Object)(object)cardFrontImageList[i] != (Object)null))
			{
				continue;
			}
			if (((Object)cardFrontImageList[i]).name == "CardFrontCatJob" || ((Object)cardFrontImageList[i]).name == "CardFrontFantasyRPG" || ((Object)cardFrontImageList[i]).name == "CardFrontMegabot")
			{
				if (TCGShopExpansionModPlugin.CustomNewExpansionConfigs.Value)
				{
					Sprite val = TryGetSpriteFromCache(CacheHandler.cardExtrasImagesCache, ((Object)cardFrontImageList[i]).name);
					if ((Object)(object)val != (Object)null)
					{
						cardFrontImageList[i] = val;
					}
				}
				else if (!TCGShopExpansionModPlugin.CustomNewExpansionImages.Value)
				{
					Sprite val2 = TryGetSpriteFromCache(CacheHandler.originalCardExtrasImagesCache, ((Object)cardFrontImageList[i]).name);
					if ((Object)(object)val2 != (Object)null)
					{
						cardFrontImageList[i] = val2;
					}
				}
			}
			else if (TCGShopExpansionModPlugin.CustomBaseMonsterImages.Value)
			{
				Sprite val3 = TryGetSpriteFromCache(CacheHandler.cardExtrasImagesCache, ((Object)cardFrontImageList[i]).name);
				if ((Object)(object)val3 != (Object)null)
				{
					cardFrontImageList[i] = val3;
				}
			}
			else if (!TCGShopExpansionModPlugin.CustomBaseMonsterImages.Value)
			{
				Sprite val4 = TryGetSpriteFromCache(CacheHandler.originalCardExtrasImagesCache, ((Object)cardFrontImageList[i]).name);
				if ((Object)(object)val4 != (Object)null)
				{
					cardFrontImageList[i] = val4;
				}
			}
		}
	}

	public static void TestReplacer2()
	{
		List<Sprite> cardBorderSpriteList = CSingleton<CardUI>.Instance.m_CardBorderSpriteList;
		List<Sprite> cardBorderList = CSingleton<InventoryBase>.Instance.m_MonsterData_SO.m_CardBorderList;
		List<Sprite> cardBGList = CSingleton<InventoryBase>.Instance.m_MonsterData_SO.m_CardBGList;
		List<Sprite> cardFrontImageList = CSingleton<InventoryBase>.Instance.m_MonsterData_SO.m_CardFrontImageList;
		List<Sprite> cardBackImageList = CSingleton<InventoryBase>.Instance.m_MonsterData_SO.m_CardBackImageList;
		List<Sprite> cardFoilMaskImageList = CSingleton<InventoryBase>.Instance.m_MonsterData_SO.m_CardFoilMaskImageList;
		for (int i = 0; i < cardFrontImageList.Count; i++)
		{
			if (((Object)cardFrontImageList[i]).name == "CardFrontFire")
			{
				if (TCGShopExpansionModPlugin.CustomBaseMonsterImages.Value)
				{
					Sprite val = TryGetSpriteFromCache(CacheHandler.cardExtrasImagesCache, ((Object)cardFrontImageList[i]).name);
					((Object)val).name = ((Object)cardFrontImageList[i]).name;
					cardFrontImageList[i] = val;
					Log("Swapped to custom " + ((Object)cardFrontImageList[i]).name);
				}
				else
				{
					Sprite val2 = TryGetSpriteFromCache(CacheHandler.originalCardExtrasImagesCache, ((Object)cardFrontImageList[i]).name);
					((Object)val2).name = ((Object)cardFrontImageList[i]).name;
					cardFrontImageList[i] = val2;
					Log("Swapped to original " + ((Object)cardFrontImageList[i]).name);
				}
			}
		}
	}

	public static void TestReplacer()
	{
		Log("Doing test");
		List<Sprite> cardBorderSpriteList = CSingleton<CardUI>.Instance.m_CardBorderSpriteList;
		List<Sprite> cardBorderList = CSingleton<InventoryBase>.Instance.m_MonsterData_SO.m_CardBorderList;
		List<Sprite> cardBGList = CSingleton<InventoryBase>.Instance.m_MonsterData_SO.m_CardBGList;
		List<Sprite> cardFrontImageList = CSingleton<InventoryBase>.Instance.m_MonsterData_SO.m_CardFrontImageList;
		List<Sprite> cardBackImageList = CSingleton<InventoryBase>.Instance.m_MonsterData_SO.m_CardBackImageList;
		List<Sprite> cardFoilMaskImageList = CSingleton<InventoryBase>.Instance.m_MonsterData_SO.m_CardFoilMaskImageList;
		for (int i = 0; i < cardFrontImageList.Count; i++)
		{
			if (((Object)cardFrontImageList[i]).name == "CardFrontFire")
			{
				Sprite customImage = ImageSwapHandler.GetCustomImage("CardFrontCatJob", PlayerPatches.cardExtrasImages);
				((Object)customImage).name = ((Object)cardFrontImageList[i]).name;
				cardFrontImageList[i] = customImage;
				Log("Swapped " + ((Object)cardFrontImageList[i]).name);
			}
		}
	}

	public unsafe static List<string> SetNameAndPreviousEvolution(ECardExpansionType expansionType, EMonsterType monsterType)
	{
		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
		string text = Path.Combine(GetConfigFolderFromExpansionType(expansionType) + ((object)(*(EMonsterType*)(&monsterType))/*cast due to constrained. prefix*/).ToString() + ".ini");
		List<string> list = new List<string>();
		if (File.Exists(text))
		{
			Log("Found ini file!");
			IniFile.Load(text);
			string stringValue = IniFile.GetStringValue(((object)(*(EMonsterType*)(&monsterType))/*cast due to constrained. prefix*/).ToString(), "Name");
			string stringValue2 = IniFile.GetStringValue(((object)(*(EMonsterType*)(&monsterType))/*cast due to constrained. prefix*/).ToString(), "Previous Evolution");
			if (stringValue != null)
			{
				list.Add(stringValue);
			}
			if (stringValue2 != null)
			{
				list.Add(stringValue2);
			}
			return list;
		}
		LogError("Didn't find ini file " + text);
		return null;
	}

	public static string GetConfigFolderFromExpansionType(ECardExpansionType expansionType)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0003: Unknown result type (might be due to invalid IL or missing references)
		//IL_0004: Unknown result type (might be due to invalid IL or missing references)
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		//IL_0023: Expected I4, but got Unknown
		return (int)expansionType switch
		{
			0 => PlayerPatches.tetramonConfigPath, 
			1 => PlayerPatches.destinyConfigPath, 
			2 => PlayerPatches.ghostConfigPath, 
			3 => PlayerPatches.megabotConfigPath, 
			4 => PlayerPatches.fantasyRPGConfigPath, 
			5 => PlayerPatches.catJobConfigPath, 
			_ => null, 
		};
	}

	public static void ReplaceBaseMonsterIcons()
	{
		List<MonsterData> dataList = CSingleton<InventoryBase>.Instance.m_MonsterData_SO.m_DataList;
		bool value = TCGShopExpansionModPlugin.CustomBaseMonsterImages.Value;
		bool hasTextureReplacer = TCGShopExpansionModPlugin.hasTextureReplacer;
		for (int i = 0; i < dataList.Count && !CacheHandler.IsNewMonster(((object)System.Runtime.CompilerServices.Unsafe.As<EMonsterType, EMonsterType>(ref dataList[i].MonsterType)/*cast due to constrained. prefix*/).ToString()); i++)
		{
			Sprite icon = dataList[i].Icon;
			Sprite ghostIcon = dataList[i].GhostIcon;
			if ((Object)(object)icon != (Object)null)
			{
				Sprite val = null;
				val = (value ? ImageSwapHandler.GetCustomImage(((Object)icon).name, PlayerPatches.tetramonPackImages) : ((!hasTextureReplacer) ? ImageSwapHandler.GetCustomImage(((Object)icon).name, PlayerPatches.originalTetramonPackImages) : (ImageSwapHandler.GetCustomImage(((Object)icon).name, PlayerPatches.textureReplacerImagesPath) ?? ImageSwapHandler.GetCustomImage(((Object)icon).name, PlayerPatches.originalTetramonPackImages))));
				if ((Object)(object)val != (Object)null)
				{
					dataList[i].Icon = val;
				}
				else
				{
					LogError("Replacement sprite is NULL for " + ((Object)icon).name);
				}
			}
			if ((Object)(object)ghostIcon != (Object)null)
			{
				Sprite val2 = null;
				val2 = (value ? ImageSwapHandler.GetCustomImage(((Object)ghostIcon).name, PlayerPatches.ghostPackImages) : ((!hasTextureReplacer) ? ImageSwapHandler.GetCustomImage(((Object)ghostIcon).name, PlayerPatches.ghostPackImages) : (ImageSwapHandler.GetCustomImage(((Object)ghostIcon).name, PlayerPatches.textureReplacerImagesPath) ?? ImageSwapHandler.GetCustomImage(((Object)ghostIcon).name, PlayerPatches.ghostPackImages))));
				if ((Object)(object)val2 != (Object)null)
				{
					dataList[i].GhostIcon = val2;
				}
				else
				{
					LogError("Replacement GHOST sprite is NULL for " + ((Object)ghostIcon).name);
				}
			}
		}
	}

	public static void ReplaceAllExpansionMonsterIcons()
	{
		ReplaceExpansionMonsterIcons(CSingleton<InventoryBase>.Instance.m_MonsterData_SO.m_CatJobDataList, PlayerPatches.catJobPackImages);
		ReplaceExpansionMonsterIcons(CSingleton<InventoryBase>.Instance.m_MonsterData_SO.m_FantasyRPGDataList, PlayerPatches.fantasyPackImages);
		ReplaceExpansionMonsterIcons(CSingleton<InventoryBase>.Instance.m_MonsterData_SO.m_MegabotDataList, PlayerPatches.megabotPackImages);
	}

	public unsafe static void ReplaceExpansionMonsterIcons(List<MonsterData> monsterDataList, string customImagePackPath)
	{
		//IL_00ca: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d1: Expected I4, but got Unknown
		//IL_00dc: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e6: Invalid comparison between Unknown and I4
		//IL_00a6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ea: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ee: Unknown result type (might be due to invalid IL or missing references)
		//IL_0100: Expected I4, but got Unknown
		//IL_01c0: Unknown result type (might be due to invalid IL or missing references)
		bool value = TCGShopExpansionModPlugin.CustomNewExpansionImages.Value;
		bool hasTextureReplacer = TCGShopExpansionModPlugin.hasTextureReplacer;
		for (int i = 0; i < monsterDataList.Count && !CacheHandler.IsNewMonster(((object)System.Runtime.CompilerServices.Unsafe.As<EMonsterType, EMonsterType>(ref monsterDataList[i].MonsterType)/*cast due to constrained. prefix*/).ToString()); i++)
		{
			Sprite icon = monsterDataList[i].Icon;
			Sprite val = null;
			if (value)
			{
				val = ImageSwapHandler.GetCustomImage(((object)System.Runtime.CompilerServices.Unsafe.As<EMonsterType, EMonsterType>(ref monsterDataList[i].MonsterType)/*cast due to constrained. prefix*/).ToString(), customImagePackPath);
				if ((Object)(object)val != (Object)null)
				{
					monsterDataList[i].Icon = val;
				}
				else
				{
					LogError($"Replacement sprite is NULL for {monsterDataList[i].MonsterType}");
				}
				continue;
			}
			int monsterId = (int)monsterDataList[i].MonsterType;
			int monsterIdModifier = GetMonsterIdModifier(monsterId);
			EMonsterType val2 = (EMonsterType)monsterIdModifier;
			if (1 == 0)
			{
			}
			string text = (((int)val2 == 38) ? "Mummy" : ((val2 - 85) switch
			{
				0 => "CrystalA", 
				1 => "CrystalB", 
				2 => "CrystalC", 
				_ => ((object)(*(EMonsterType*)(&val2))/*cast due to constrained. prefix*/).ToString(), 
			}));
			if (1 == 0)
			{
			}
			string fileName = text;
			string imagePath = (hasTextureReplacer ? PlayerPatches.textureReplacerImagesPath : PlayerPatches.originalTetramonPackImages);
			val = ImageSwapHandler.GetCustomImage(fileName, imagePath);
			if ((Object)(object)val != (Object)null)
			{
				monsterDataList[i].Icon = val;
			}
			else if ((Object)(object)val == (Object)null && hasTextureReplacer)
			{
				val = ImageSwapHandler.GetCustomImage(fileName, PlayerPatches.originalTetramonPackImages);
				if ((Object)(object)val != (Object)null)
				{
					monsterDataList[i].Icon = val;
				}
				else
				{
					LogError($"Original sprite is NULL for {val2}");
				}
			}
		}
	}

	public static int GetMonsterIdModifier(int monsterId)
	{
		if (monsterId >= 1000 && monsterId <= 1112)
		{
			return (monsterId >= 1110) ? (monsterId - 1109) : (monsterId - 999);
		}
		if (monsterId >= 2000 && monsterId <= 2049)
		{
			return monsterId - 1999;
		}
		if (monsterId >= 3000 && monsterId <= 3039)
		{
			return monsterId - 2949;
		}
		return monsterId;
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
