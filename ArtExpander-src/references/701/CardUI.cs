using System.Collections.Generic;
using I2.Loc;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CardUI : MonoBehaviour
{
	public GameObject m_CardFront;

	public GameObject m_CardBack;

	public GameObject m_CenterFrameMaskGrp;

	public GameObject m_CenterFrameImageGrp;

	public GameObject m_CardFullBGOffsetGrp;

	public GameObject m_CardFullBGTransparentLayeredOffsetGrp;

	public GameObject m_FoilGrp;

	public GameObject m_CenterFoilGlitter;

	public GameObject m_CenterFoilGlitterBtm;

	public GameObject m_BorderFoilGlitter;

	public GameObject m_GradedCardCaseGrp;

	public GameObject m_GradedCardCaseBackGrp;

	public bool m_Show2DGradedCase;

	public bool m_UpdateFoilMaterialOnSetCardUI;

	public Transform m_GradedCardFrontScaling;

	public Transform m_SimplifiedCullingGradedCardFrontScaling;

	public List<Image> m_FoilShowList;

	public List<Image> m_FoilBlendedShowList;

	public List<Image> m_FoilDarkenImageList;

	public Image m_CardBackImage;

	public Image m_CenterFrameImage;

	public Image m_CenterFrameMaskImage;

	public Image m_CardFrontImage;

	public Image m_CardFrontImageTopLayer;

	public Image m_FadeBarTopImage;

	public Image m_FadeBarBtmImage;

	public Image m_EvoBGImage;

	public Image m_PlayEffectBGImage;

	public Image m_DescriptionBGImage;

	public Image m_CardBorderMask;

	public Image m_CardBorderImage;

	public Image m_CardBGImage;

	public Image m_CardFullBGImage;

	public Image m_CardFullTransparentLayerBGImage;

	public Image m_RarityImage;

	public Image m_StatImage;

	public Image m_BrightnessControl;

	public Image m_GradedCardBrightnessControl;

	public Image m_GradedCardBackBrightnessControl;

	public Image m_GradedCardTextureImage;

	public TextMeshProUGUI m_FirstEditionText;

	public TextMeshProUGUI m_MonsterNameText;

	public TextMeshProUGUI m_NumberText;

	public TextMeshProUGUI m_DescriptionText;

	public TextMeshProUGUI m_RarityText;

	public TextMeshProUGUI m_Stat1Text;

	public TextMeshProUGUI m_Stat2Text;

	public TextMeshProUGUI m_Stat3Text;

	public TextMeshProUGUI m_Stat4Text;

	public TextMeshProUGUI m_ArtistText;

	public TextMeshProUGUI m_GradeNumberText;

	public TextMeshProUGUI m_GradeDescriptionText;

	public TextMeshProUGUI m_GradeNameText;

	public TextMeshProUGUI m_GradeExpansionRarityText;

	public TextMeshProUGUI m_GradeSerialText;

	private CardData m_CardData;

	private MonsterData m_MonsterData;

	private ECardBorderType m_CardBorderType;

	private bool m_IsFoil;

	private bool m_IsDimensionCard;

	private CardUISetting m_CardUISetting;

	private CardUISettingData m_CardUISettingData;

	private Vector3 m_ArtworkImageLocalPos;

	private Card3dUIGroup m_Card3dUIGroup;

	public List<GameObject> m_FarDistanceCullObjList;

	public List<bool> m_FarDistanceCullObjVisibilityList = new List<bool>();

	public bool m_IsFarDistanceCulled;

	public GameObject m_StatGrp;

	public GameObject m_EvoAndArtistNameGrp;

	public GameObject m_EvoGrp;

	public GameObject m_EvoBasicGrp;

	public GameObject m_ArtistGrp;

	public GameObject m_DescriptionGrp;

	public Image m_EvoPreviousStageIcon;

	public TextMeshProUGUI m_EvoPreviousStageNameText;

	public void InitCard3dUIGroup(Card3dUIGroup card3dUIGroup)
	{
		m_Card3dUIGroup = card3dUIGroup;
	}

	public void ShowFoilList(bool isActive)
	{
		//IL_004d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		if (isActive)
		{
			for (int i = 0; i < m_FoilDarkenImageList.Count; i++)
			{
				m_FoilDarkenImageList[i].color = m_CardUISettingData.cardBGColorFoil;
			}
		}
		else
		{
			for (int j = 0; j < m_FoilDarkenImageList.Count; j++)
			{
				m_FoilDarkenImageList[j].color = m_CardUISettingData.cardBGColorNonFoil;
			}
		}
		for (int k = 0; k < m_FoilShowList.Count; k++)
		{
			((Behaviour)m_FoilShowList[k]).enabled = isActive;
		}
	}

	public void ShowFoilBlendedList(bool isActive)
	{
		for (int i = 0; i < m_FoilBlendedShowList.Count; i++)
		{
			((Behaviour)m_FoilBlendedShowList[i]).enabled = isActive;
		}
	}

	public void SetFoilCullListVisibility(bool isActive)
	{
		if (!(!m_IsFarDistanceCulled && isActive) && (!m_IsFarDistanceCulled || isActive))
		{
			for (int i = 0; i < m_FoilShowList.Count; i++)
			{
				((Component)m_FoilShowList[i]).gameObject.SetActive(isActive);
			}
			for (int j = 0; j < m_FoilBlendedShowList.Count; j++)
			{
				((Component)m_FoilBlendedShowList[j]).gameObject.SetActive(isActive);
			}
		}
	}

	public void SetFoilMaterialList(List<Material> mat)
	{
		for (int i = 0; i < m_FoilShowList.Count; i++)
		{
			m_FoilShowList[i].material = mat[i];
		}
	}

	public void SetFoilBlendedMaterialList(List<Material> mat)
	{
		for (int i = 0; i < m_FoilBlendedShowList.Count; i++)
		{
			m_FoilBlendedShowList[i].material = mat[i];
		}
	}

	public void SetFoilMaterialListFromSettingData(bool isWorldView)
	{
		if (m_CardUISettingData.foilMaterialTangentView.Count == 0)
		{
			return;
		}
		if (isWorldView)
		{
			for (int i = 0; i < m_FoilShowList.Count; i++)
			{
				m_FoilShowList[i].material = m_CardUISettingData.foilMaterialWorldView[i];
			}
		}
		else
		{
			for (int j = 0; j < m_FoilShowList.Count; j++)
			{
				m_FoilShowList[j].material = m_CardUISettingData.foilMaterialTangentView[j];
			}
		}
	}

	public void SetFoilBlendedMaterialListFromSettingData(bool isWorldView)
	{
		if (m_CardUISettingData.foilBlendedMaterialWorldView.Count == 0)
		{
			return;
		}
		if (isWorldView)
		{
			for (int i = 0; i < m_FoilBlendedShowList.Count; i++)
			{
				m_FoilBlendedShowList[i].material = m_CardUISettingData.foilBlendedMaterialWorldView[i];
			}
		}
		else
		{
			for (int j = 0; j < m_FoilBlendedShowList.Count; j++)
			{
				m_FoilBlendedShowList[j].material = m_CardUISettingData.foilBlendedMaterialTangentView[j];
			}
		}
	}

	public void ShowGradedCardCase(bool isShow)
	{
		//IL_014a: Unknown result type (might be due to invalid IL or missing references)
		//IL_015f: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fd: Unknown result type (might be due to invalid IL or missing references)
		//IL_0112: Unknown result type (might be due to invalid IL or missing references)
		if (m_CardData.cardGrade <= 0)
		{
			isShow = false;
		}
		m_GradedCardCaseGrp.SetActive(isShow);
		if (isShow)
		{
			m_GradeNumberText.text = m_CardData.cardGrade.ToString();
			m_GradeDescriptionText.text = GameInstance.GetCardGradeString(m_CardData.cardGrade);
			m_GradeNameText.text = m_MonsterNameText.text;
			m_GradeExpansionRarityText.text = LocalizationManager.GetTranslation(m_CardData.expansionType.ToString()) + " " + CPlayerData.GetFullCardTypeName(m_CardData);
			m_CardFront.transform.localPosition = ((Component)m_GradedCardFrontScaling).transform.localPosition;
			m_CardFront.transform.localScale = ((Component)m_GradedCardFrontScaling).transform.localScale;
			m_GradedCardCaseGrp.transform.localPosition = Vector3.zero;
			m_GradedCardCaseGrp.transform.localScale = Vector3.one;
			((Component)m_GradedCardBrightnessControl).gameObject.SetActive(false);
			((Component)m_GradedCardBackBrightnessControl).gameObject.SetActive(false);
		}
		else
		{
			m_CardFront.transform.localPosition = Vector3.zero;
			m_CardFront.transform.localScale = Vector3.one;
		}
	}

	public void ShowSimplifiedCullingGradedCardCase(bool isShow)
	{
		//IL_0137: Unknown result type (might be due to invalid IL or missing references)
		//IL_014c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0101: Unknown result type (might be due to invalid IL or missing references)
		//IL_0121: Unknown result type (might be due to invalid IL or missing references)
		if (m_CardData != null)
		{
			if (m_CardData.cardGrade <= 0)
			{
				isShow = false;
			}
			m_GradedCardCaseGrp.SetActive(isShow);
			if (Object.op_Implicit((Object)(object)m_Card3dUIGroup) && !m_Card3dUIGroup.m_GradedCaseCullCardBackMeshBlocker.activeSelf)
			{
				m_Card3dUIGroup.m_GradedCaseCullCardFrontMeshBlocker.SetActive(isShow);
			}
			if (isShow)
			{
				m_GradeNumberText.text = m_CardData.cardGrade.ToString();
				m_GradeDescriptionText.text = GameInstance.GetCardGradeString(m_CardData.cardGrade);
				m_GradeNameText.text = m_MonsterNameText.text;
				m_GradeExpansionRarityText.text = LocalizationManager.GetTranslation(m_CardData.expansionType.ToString()) + " " + CPlayerData.GetFullCardTypeName(m_CardData);
				m_GradedCardCaseGrp.transform.localPosition = ((Component)m_SimplifiedCullingGradedCardFrontScaling).transform.localPosition;
				m_GradedCardCaseGrp.transform.localScale = ((Component)m_SimplifiedCullingGradedCardFrontScaling).transform.localScale;
			}
			else
			{
				m_GradedCardCaseGrp.transform.localPosition = Vector3.zero;
				m_GradedCardCaseGrp.transform.localScale = Vector3.one;
			}
		}
	}

	public void GradedCardOcclusionCull(bool isCull)
	{
		if (m_GradedCardCaseGrp.activeSelf && isCull)
		{
			m_GradedCardCaseBackGrp.SetActive(true);
			m_Card3dUIGroup.m_GradedCaseCullCardFrontMeshBlocker.SetActive(false);
		}
		else
		{
			m_GradedCardCaseBackGrp.SetActive(false);
		}
		if (Object.op_Implicit((Object)(object)m_Card3dUIGroup))
		{
			m_Card3dUIGroup.m_GradedCaseCullCardBackMeshBlocker.SetActive(m_GradedCardCaseBackGrp.activeSelf);
		}
	}

	private void LoadStreamTextureCompleted(CEventPlayer_LoadStreamTextureCompleted evt)
	{
		if (evt.m_FileName == m_CardData.expansionType.ToString() + "_" + m_CardData.monsterType)
		{
			CEventManager.RemoveListener<CEventPlayer_LoadStreamTextureCompleted>(LoadStreamTextureCompleted);
			if (evt.m_IsSuccess)
			{
				m_CenterFrameImage.sprite = m_MonsterData.GetIcon(m_CardData.expansionType);
			}
		}
	}

	private void OnDisable()
	{
		if (Application.isPlaying || Application.isMobilePlatform)
		{
			CEventManager.RemoveListener<CEventPlayer_LoadStreamTextureCompleted>(LoadStreamTextureCompleted);
		}
	}

	public void SetCardUI(CardData cardData)
	{
		//IL_01eb: Unknown result type (might be due to invalid IL or missing references)
		//IL_0206: Unknown result type (might be due to invalid IL or missing references)
		//IL_021b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0226: Unknown result type (might be due to invalid IL or missing references)
		//IL_022b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0240: Unknown result type (might be due to invalid IL or missing references)
		//IL_024b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0250: Unknown result type (might be due to invalid IL or missing references)
		//IL_03ac: Unknown result type (might be due to invalid IL or missing references)
		//IL_03c1: Unknown result type (might be due to invalid IL or missing references)
		//IL_03cc: Unknown result type (might be due to invalid IL or missing references)
		//IL_03d1: Unknown result type (might be due to invalid IL or missing references)
		//IL_0407: Unknown result type (might be due to invalid IL or missing references)
		//IL_0427: Unknown result type (might be due to invalid IL or missing references)
		//IL_0458: Unknown result type (might be due to invalid IL or missing references)
		//IL_046d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0478: Unknown result type (might be due to invalid IL or missing references)
		//IL_047d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0498: Unknown result type (might be due to invalid IL or missing references)
		//IL_04ad: Unknown result type (might be due to invalid IL or missing references)
		//IL_04b8: Unknown result type (might be due to invalid IL or missing references)
		//IL_04bd: Unknown result type (might be due to invalid IL or missing references)
		//IL_04d8: Unknown result type (might be due to invalid IL or missing references)
		//IL_04ed: Unknown result type (might be due to invalid IL or missing references)
		//IL_04f8: Unknown result type (might be due to invalid IL or missing references)
		//IL_04fd: Unknown result type (might be due to invalid IL or missing references)
		//IL_0518: Unknown result type (might be due to invalid IL or missing references)
		//IL_052d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0538: Unknown result type (might be due to invalid IL or missing references)
		//IL_053d: Unknown result type (might be due to invalid IL or missing references)
		//IL_056e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0583: Unknown result type (might be due to invalid IL or missing references)
		//IL_058e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0593: Unknown result type (might be due to invalid IL or missing references)
		//IL_05ae: Unknown result type (might be due to invalid IL or missing references)
		//IL_05c3: Unknown result type (might be due to invalid IL or missing references)
		//IL_05ce: Unknown result type (might be due to invalid IL or missing references)
		//IL_05d3: Unknown result type (might be due to invalid IL or missing references)
		//IL_05ee: Unknown result type (might be due to invalid IL or missing references)
		//IL_0603: Unknown result type (might be due to invalid IL or missing references)
		//IL_060e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0613: Unknown result type (might be due to invalid IL or missing references)
		//IL_0629: Unknown result type (might be due to invalid IL or missing references)
		//IL_063f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0655: Unknown result type (might be due to invalid IL or missing references)
		m_CardData = cardData;
		if (m_CardData.cardGrade > 10)
		{
			m_CardData.cardGrade = 10;
		}
		m_MonsterData = InventoryBase.GetMonsterData(cardData.monsterType);
		m_CardUISetting = InventoryBase.GetCardUISetting(m_CardData.expansionType);
		m_CardUISettingData = m_CardUISetting.GetCardUISettingData(m_CardData.GetCardBorderType(), m_CardData.isDestiny);
		if ((Object.op_Implicit((Object)(object)m_Card3dUIGroup) || m_UpdateFoilMaterialOnSetCardUI) && m_CardData.isFoil)
		{
			SetFoilMaterialListFromSettingData(isWorldView: false);
			SetFoilBlendedMaterialListFromSettingData(isWorldView: false);
		}
		Sprite iconFromList = m_MonsterData.GetIconFromList(m_CardUISetting.iconIndex);
		if ((Object)(object)iconFromList != (Object)null)
		{
			m_CenterFrameImage.sprite = iconFromList;
		}
		else
		{
			m_CenterFrameImage.sprite = m_MonsterData.GetIcon(m_CardData.expansionType);
		}
		m_CardBackImage.sprite = CSingleton<InventoryBase>.Instance.m_MonsterData_SO.GetCardBackSprite(m_CardData.expansionType);
		m_CardBorderType = m_CardData.GetCardBorderType();
		m_IsDimensionCard = m_CardData.isDestiny;
		m_IsFoil = m_CardData.isFoil;
		m_FoilGrp.SetActive(m_IsFoil);
		ShowFoilList(m_IsFoil);
		ShowFoilBlendedList(m_IsFoil);
		Sprite val = m_MonsterData.GetBGFromList(m_CardUISetting.bgIndex);
		if ((Object)(object)val == (Object)null)
		{
			val = m_CardUISettingData.GetCardBGSprite(m_MonsterData.ElementIndex);
		}
		if (m_CardUISettingData.showCardFullBG)
		{
			m_CardFullBGImage.sprite = val;
			m_CardFullTransparentLayerBGImage.sprite = m_CardFullBGImage.sprite;
			m_CardFullBGOffsetGrp.transform.localPosition = m_CardUISettingData.centerImageGrpPosOffset;
			m_CardFullBGTransparentLayeredOffsetGrp.transform.localPosition = m_CardUISettingData.centerImageGrpPosOffset;
			m_CardFullBGOffsetGrp.transform.localScale = Vector3.one + m_CardUISettingData.centerImageGrpScaleOffset;
			m_CardFullBGTransparentLayeredOffsetGrp.transform.localScale = Vector3.one + m_CardUISettingData.centerImageGrpScaleOffset;
			m_CardFullBGOffsetGrp.gameObject.SetActive(true);
			m_CardFullBGTransparentLayeredOffsetGrp.gameObject.SetActive(m_CardUISettingData.showCardFullLayeredBG);
		}
		else
		{
			m_CardBGImage.sprite = val;
			m_CardFullBGOffsetGrp.gameObject.SetActive(false);
			m_CardFullBGTransparentLayeredOffsetGrp.gameObject.SetActive(false);
		}
		m_CardBorderImage.sprite = m_CardUISettingData.GetCardBorderSprite(m_CardBorderType);
		m_RarityImage.sprite = m_CardUISettingData.GetCardRaritySprite(m_MonsterData.Rarity);
		((Behaviour)m_CardFrontImage).enabled = m_CardUISettingData.showCardFront;
		((Behaviour)m_CardFrontImageTopLayer).enabled = m_CardUISettingData.showCardFrontTopLayer;
		((Behaviour)m_CardBGImage).enabled = !m_CardUISettingData.showCardFullBG;
		((Behaviour)m_FadeBarTopImage).enabled = m_CardUISettingData.showFadeBarTop;
		((Behaviour)m_FadeBarBtmImage).enabled = m_CardUISettingData.showFadeBarBtm;
		m_EvoAndArtistNameGrp.SetActive(m_CardUISettingData.showEvoAndArtistNameGrp);
		m_CardFrontImage.sprite = m_CardUISettingData.GetCardFrontSprite(m_MonsterData.ElementIndex);
		((Component)m_CardFrontImage).transform.localPosition = m_CardUISettingData.cardFrontImagePosOffset;
		((Component)m_CardFrontImage).transform.localScale = Vector3.one + m_CardUISettingData.cardFrontImageScaleOffset;
		m_CardFrontImageTopLayer.sprite = m_CardFrontImage.sprite;
		((Component)m_CardFrontImageTopLayer).transform.localPosition = ((Component)m_CardFrontImage).transform.localPosition;
		((Component)m_CardFrontImageTopLayer).transform.localScale = ((Component)m_CardFrontImage).transform.localScale;
		m_CenterFrameMaskImage.sprite = m_CardUISettingData.cardCenterFrameMask;
		m_EvoAndArtistNameGrp.transform.localPosition = m_CardUISettingData.evoAndArtistNameGrpPosOffset;
		m_EvoAndArtistNameGrp.transform.localScale = Vector3.one + m_CardUISettingData.evoAndArtistNameGrpScaleOffset;
		m_EvoGrp.transform.localPosition = m_CardUISettingData.evoGrpPosOffset;
		m_EvoGrp.transform.localScale = Vector3.one + m_CardUISettingData.evoGrpScaleOffset;
		m_ArtistGrp.transform.localPosition = m_CardUISettingData.artistNameGrpPosOffset;
		m_ArtistGrp.transform.localScale = Vector3.one + m_CardUISettingData.artistNameGrpScaleOffset;
		m_DescriptionGrp.transform.localPosition = m_CardUISettingData.descriptionGrpPosOffset;
		m_DescriptionGrp.transform.localScale = Vector3.one + m_CardUISettingData.descriptionGrpScaleOffset;
		m_StatImage.sprite = m_CardUISettingData.statImage;
		m_StatGrp.transform.localPosition = m_CardUISettingData.statGrpPosOffset;
		m_StatGrp.transform.localScale = Vector3.one + m_CardUISettingData.statGrpScaleOffset;
		m_CenterFrameMaskGrp.transform.localPosition = m_CardUISettingData.centerFrameMaskPosOffset;
		m_CenterFrameMaskGrp.transform.localScale = Vector3.one + m_CardUISettingData.centerFrameMaskScaleOffset;
		m_CenterFrameImageGrp.transform.localPosition = m_CardUISettingData.centerImageGrpPosOffset;
		m_CenterFrameImageGrp.transform.localScale = Vector3.one + m_CardUISettingData.centerImageGrpScaleOffset;
		m_EvoBGImage.color = m_CardUISettingData.evoBGColor;
		m_PlayEffectBGImage.color = m_CardUISettingData.playEffectBGColor;
		m_DescriptionBGImage.color = m_CardUISettingData.descriptionBGColor;
		m_CardBorderMask.sprite = m_CardUISettingData.cardBorderMask;
		((Component)m_CardBorderMask).gameObject.SetActive(m_CardUISettingData.showBorder);
		m_CenterFoilGlitter.SetActive(m_CardUISettingData.showCenterFoilGlitter && m_IsFoil);
		m_CenterFoilGlitterBtm.SetActive(m_CardUISettingData.showCenterBtmFoilGlitter && m_IsFoil);
		m_BorderFoilGlitter.SetActive(m_CardUISettingData.showBorderFoilGlitter && m_IsFoil);
		m_MonsterNameText.text = m_MonsterData.GetName();
		int num = (int)((int)(m_MonsterData.MonsterType - 1) * CPlayerData.GetCardAmountPerMonsterType(m_CardData.expansionType) + m_CardBorderType);
		num++;
		if (m_IsFoil)
		{
			num += 6;
		}
		string text = "";
		text = ((num < 10) ? ("00" + num) : ((num >= 100) ? num.ToString() : ("0" + num)));
		m_NumberText.text = text;
		m_DescriptionText.text = m_MonsterData.GetDescription();
		((Component)m_DescriptionText).gameObject.SetActive(true);
		string artistNameFromList = m_MonsterData.GetArtistNameFromList(m_CardUISetting.artistNameIndex);
		if (artistNameFromList != "")
		{
			m_ArtistText.text = artistNameFromList;
		}
		else
		{
			m_ArtistText.text = m_MonsterData.GetArtistName();
		}
		((Component)m_ArtistText).gameObject.SetActive(true);
		if (m_MonsterData.PreviousEvolution == EMonsterType.None)
		{
			m_EvoBasicGrp.SetActive(true);
			((Component)m_EvoPreviousStageIcon).gameObject.SetActive(false);
			((Component)m_EvoPreviousStageNameText).gameObject.SetActive(false);
		}
		else
		{
			m_EvoBasicGrp.SetActive(false);
			MonsterData monsterData = InventoryBase.GetMonsterData(m_MonsterData.PreviousEvolution);
			if (m_CardUISetting.previousEvoStageIconExpansion != ECardExpansionType.None)
			{
				m_EvoPreviousStageIcon.sprite = monsterData.GetIcon(m_CardUISetting.previousEvoStageIconExpansion);
			}
			else
			{
				m_EvoPreviousStageIcon.sprite = monsterData.GetIcon(m_CardData.expansionType);
			}
			m_EvoPreviousStageNameText.text = monsterData.GetName();
			((Component)m_EvoPreviousStageNameText).gameObject.SetActive(true);
			((Component)m_EvoPreviousStageIcon).gameObject.SetActive(true);
		}
		m_RarityText.text = m_MonsterData.GetRarityName();
		if (m_MonsterData.BaseStats.FireElement != 0)
		{
			m_Stat1Text.text = m_MonsterData.BaseStats.FireElement.ToString();
			m_Stat2Text.text = m_MonsterData.BaseStats.EarthElement.ToString();
			m_Stat3Text.text = m_MonsterData.BaseStats.WaterElement.ToString();
			m_Stat4Text.text = m_MonsterData.BaseStats.WindElement.ToString();
		}
		else
		{
			m_Stat1Text.text = m_MonsterData.BaseStats.Strength.ToString();
			m_Stat2Text.text = m_MonsterData.BaseStats.Vitality.ToString();
			m_Stat3Text.text = m_MonsterData.BaseStats.Spirit.ToString();
			m_Stat4Text.text = m_MonsterData.BaseStats.Magic.ToString();
		}
		EvaluateCardUISetting();
		if (m_CardBorderType == ECardBorderType.Base || m_CardBorderType == ECardBorderType.FullArt)
		{
			((Behaviour)m_FirstEditionText).enabled = false;
		}
		else
		{
			if (m_CardBorderType == ECardBorderType.FirstEdition)
			{
				m_FirstEditionText.text = LocalizationManager.GetTranslation("1st Edition");
			}
			else if (m_CardBorderType == ECardBorderType.Silver)
			{
				m_FirstEditionText.text = LocalizationManager.GetTranslation("Silver Edition");
			}
			else if (m_CardBorderType == ECardBorderType.Gold)
			{
				m_FirstEditionText.text = LocalizationManager.GetTranslation("Gold Edition");
			}
			else if (m_CardBorderType == ECardBorderType.EX)
			{
				m_FirstEditionText.text = "EX";
			}
			((Behaviour)m_FirstEditionText).enabled = true;
		}
		if (Object.op_Implicit((Object)(object)m_Card3dUIGroup))
		{
			m_Card3dUIGroup.EvaluateCardGrade(m_CardData);
		}
		m_GradedCardTextureImage.sprite = CSingleton<InventoryBase>.Instance.m_MonsterData_SO.GetGradedCardScratchTexture(m_CardData.cardGrade);
		ShowGradedCardCase(m_Show2DGradedCase);
	}

	private void EvaluateCardUISetting()
	{
		//IL_008c: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ac: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cc: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ec: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f1: Unknown result type (might be due to invalid IL or missing references)
		//IL_010c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0121: Unknown result type (might be due to invalid IL or missing references)
		//IL_012c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0131: Unknown result type (might be due to invalid IL or missing references)
		//IL_014c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0161: Unknown result type (might be due to invalid IL or missing references)
		//IL_016c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0171: Unknown result type (might be due to invalid IL or missing references)
		//IL_017c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0181: Unknown result type (might be due to invalid IL or missing references)
		//IL_01af: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b4: Unknown result type (might be due to invalid IL or missing references)
		//IL_0199: Unknown result type (might be due to invalid IL or missing references)
		//IL_0232: Unknown result type (might be due to invalid IL or missing references)
		//IL_024d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0268: Unknown result type (might be due to invalid IL or missing references)
		if (Object.op_Implicit((Object)(object)m_MonsterNameText))
		{
			((Behaviour)m_MonsterNameText).enabled = m_CardUISettingData.showName;
		}
		((Behaviour)m_Stat1Text).enabled = m_CardUISettingData.showStat1;
		((Behaviour)m_Stat2Text).enabled = m_CardUISettingData.showStat2;
		((Behaviour)m_Stat3Text).enabled = m_CardUISettingData.showStat3;
		((Behaviour)m_Stat4Text).enabled = m_CardUISettingData.showStat4;
		m_Stat1Text.transform.localPosition = m_CardUISettingData.stat1PosOffset;
		m_Stat1Text.transform.localScale = Vector3.one + m_CardUISettingData.stat1ScaleOffset;
		m_Stat2Text.transform.localPosition = m_CardUISettingData.stat2PosOffset;
		m_Stat2Text.transform.localScale = Vector3.one + m_CardUISettingData.stat2ScaleOffset;
		m_Stat3Text.transform.localPosition = m_CardUISettingData.stat3PosOffset;
		m_Stat3Text.transform.localScale = Vector3.one + m_CardUISettingData.stat3ScaleOffset;
		m_Stat4Text.transform.localPosition = m_CardUISettingData.stat4PosOffset;
		m_Stat4Text.transform.localScale = Vector3.one + m_CardUISettingData.stat4ScaleOffset;
		if (m_ArtworkImageLocalPos != Vector3.zero)
		{
			((Component)m_CenterFrameImage).transform.localPosition = m_ArtworkImageLocalPos;
		}
		m_ArtworkImageLocalPos = ((Component)m_CenterFrameImage).transform.localPosition;
		if (!m_CardUISettingData.showEdition && ((Behaviour)m_FirstEditionText).enabled)
		{
			((Behaviour)m_FirstEditionText).enabled = false;
		}
		((Behaviour)m_RarityImage).enabled = m_CardUISettingData.showRarity;
		((Behaviour)m_RarityText).enabled = m_CardUISettingData.showRarity;
		((Behaviour)m_NumberText).enabled = m_CardUISettingData.showNumber;
		m_NumberText.transform.localPosition = m_CardUISettingData.numberPosOffset;
		m_FirstEditionText.transform.localPosition = m_CardUISettingData.editionPosOffset;
		m_MonsterNameText.transform.localPosition = m_CardUISettingData.namePosOffset;
	}

	public CardData GetCardData()
	{
		return m_CardData;
	}

	public void SetBrightness(float brightness)
	{
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		//IL_0032: Unknown result type (might be due to invalid IL or missing references)
		//IL_003e: Unknown result type (might be due to invalid IL or missing references)
		//IL_005c: Unknown result type (might be due to invalid IL or missing references)
		Color color = m_BrightnessControl.color;
		color.a = (1f - brightness) * 0.95f;
		m_BrightnessControl.color = color;
		m_GradedCardBrightnessControl.color = color;
		m_GradedCardBackBrightnessControl.color = color;
		if (Object.op_Implicit((Object)(object)m_Card3dUIGroup))
		{
			m_Card3dUIGroup.m_GradedCardBrightnessControl.color = color;
		}
	}

	public void SetFarDistanceCull()
	{
		if (!m_IsFarDistanceCulled)
		{
			m_IsFarDistanceCulled = true;
			m_FarDistanceCullObjVisibilityList.Clear();
			for (int i = 0; i < m_FarDistanceCullObjList.Count; i++)
			{
				m_FarDistanceCullObjVisibilityList.Add(m_FarDistanceCullObjList[i].activeSelf);
				m_FarDistanceCullObjList[i].SetActive(false);
			}
			Object.op_Implicit((Object)(object)m_Card3dUIGroup);
		}
	}

	public void ResetFarDistanceCull()
	{
		if (m_IsFarDistanceCulled)
		{
			m_IsFarDistanceCulled = false;
			for (int i = 0; i < m_FarDistanceCullObjVisibilityList.Count; i++)
			{
				m_FarDistanceCullObjList[i].SetActive(m_FarDistanceCullObjVisibilityList[i]);
			}
			m_FarDistanceCullObjVisibilityList.Clear();
			Object.op_Implicit((Object)(object)m_Card3dUIGroup);
			GradedCardOcclusionCull(isCull: false);
		}
	}

	public bool IsCard3dUIGroupSet()
	{
		return (Object)(object)m_Card3dUIGroup != (Object)null;
	}
}
