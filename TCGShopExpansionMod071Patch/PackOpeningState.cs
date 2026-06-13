using System;
using System.Reflection;
using HarmonyLib;

namespace TCGShopExpansionMod071Patch;

internal static class PackOpeningState
{
    private const float FlipFrontRevealSlider = 0.45f;

    private static Func<CardOpeningSequence, int>? _openedCardIndexGetter;
    private static Func<CardOpeningSequence, float>? _sliderGetter;
    private static int _cachedOpenedCardIndex;
    private static int _cachedStateIndex;
    private static float _cachedSlider;

    /// <summary>Called each frame from CardOpeningSequence.Update while the screen is active.</summary>
    public static void SyncFromSequence(CardOpeningSequence sequence)
    {
        if (sequence == null)
        {
            return;
        }

        _cachedStateIndex = sequence.m_StateIndex;
        _cachedOpenedCardIndex = ReadOpenedCardIndex(sequence);
        _cachedSlider = ReadSlider(sequence);
    }

    /// <summary>True only while the pack opening UI is active (states 0-6).</summary>
    public static bool IsPackOpeningInProgress()
    {
        CardOpeningSequence? sequence = CSingleton<CardOpeningSequence>.Instance;
        if (sequence == null || !sequence.IsActive())
        {
            return false;
        }

        int state = sequence.m_StateIndex;
        return state >= 0 && state < 7;
    }

    /// <summary>True while the current card flip/reveal animation runs (states 4-6).</summary>
    public static bool IsPackFlipState()
    {
        return _cachedStateIndex is >= 4 and <= 6;
    }

    /// <summary>Highest-index active card in the stack.</summary>
    public static int GetPackStackTopCardIndex()
    {
        CardOpeningSequence? sequence = CSingleton<CardOpeningSequence>.Instance;
        if (sequence?.m_Card3dUIList == null)
        {
            return -1;
        }

        for (int i = sequence.m_Card3dUIList.Count - 1; i >= 0; i--)
        {
            Card3dUIGroup? card3d = sequence.m_Card3dUIList[i];
            if (card3d != null && card3d.gameObject.activeSelf)
            {
                return i;
            }
        }

        return -1;
    }

    /// <summary>ExpansionMod always targets the last slot for the stacked pack-back texture.</summary>
    public static int GetPackStackBackCardIndex()
    {
        CardOpeningSequence? sequence = CSingleton<CardOpeningSequence>.Instance;
        if (sequence?.m_Card3dUIList == null || sequence.m_Card3dUIList.Count == 0)
        {
            return -1;
        }

        int lastIndex = sequence.m_Card3dUIList.Count - 1;
        Card3dUIGroup? lastCard = sequence.m_Card3dUIList[lastIndex];
        if (lastCard != null && lastCard.gameObject.activeSelf)
        {
            return lastIndex;
        }

        return GetPackStackTopCardIndex();
    }

    /// <summary>Stacked pack back during rip / wait (states 0-3), not during per-card flips.</summary>
    public static bool ShouldShowPackBackFace(Card3dUIGroup card3d)
    {
        if (!IsPackOpeningInProgress() || card3d == null || IsPackFlipState())
        {
            return false;
        }

        int cardIndex = GetPackCardIndex(card3d);
        if (cardIndex < 0)
        {
            return false;
        }

        int stackBackIndex = GetPackStackBackCardIndex();
        return stackBackIndex >= 0 && cardIndex == stackBackIndex;
    }

    /// <summary>Single-card Pokemon back on the active card before its front is revealed.</summary>
    public static bool ShouldShowActiveCardFlipBack(Card3dUIGroup card3d)
    {
        if (!IsPackFlipState() || card3d == null)
        {
            return false;
        }

        if (GetPackCardIndex(card3d) != GetCurrentOpenedCardIndex())
        {
            return false;
        }

        return !ShouldShowFrontDuringPackFlip(card3d);
    }

    public static int GetPackCardIndex(Card3dUIGroup card3d)
    {
        CardOpeningSequence? sequence = CSingleton<CardOpeningSequence>.Instance;
        if (sequence?.m_Card3dUIList == null || card3d == null)
        {
            return -1;
        }

        int index = sequence.m_Card3dUIList.IndexOf(card3d);
        if (index >= 0)
        {
            return index;
        }

        if (card3d.m_CardUI != null)
        {
            for (int i = 0; i < sequence.m_Card3dUIList.Count; i++)
            {
                if (sequence.m_Card3dUIList[i]?.m_CardUI == card3d.m_CardUI)
                {
                    return i;
                }
            }
        }

        return -1;
    }

    public static bool IsFanRowVisible()
    {
        CardOpeningSequence? sequence = CSingleton<CardOpeningSequence>.Instance;
        if (sequence == null || !sequence.IsActive())
        {
            return false;
        }

        return sequence.m_StateIndex >= 7;
    }

    /// <summary>Front overlay once the flip reveal passes vanilla timing (slider / state).</summary>
    public static bool ShouldShowFrontDuringPackFlip(Card3dUIGroup card3d)
    {
        if (!IsPackFlipState() || card3d == null)
        {
            return false;
        }

        if (GetPackCardIndex(card3d) != GetCurrentOpenedCardIndex())
        {
            return false;
        }

        return _cachedStateIndex switch
        {
            4 => _cachedSlider >= FlipFrontRevealSlider,
            5 => true,
            6 => _cachedSlider < 0.5f,
            _ => false,
        };
    }

    public static int GetCurrentOpenedCardIndex()
    {
        CardOpeningSequence? sequence = CSingleton<CardOpeningSequence>.Instance;
        if (sequence == null || !sequence.IsActive())
        {
            return -1;
        }

        return _cachedOpenedCardIndex;
    }

    private static int ReadOpenedCardIndex(CardOpeningSequence sequence)
    {
        try
        {
            _openedCardIndexGetter ??= CreateOpenedCardIndexGetter();
            if (_openedCardIndexGetter != null)
            {
                return _openedCardIndexGetter(sequence);
            }
        }
        catch (Exception)
        {
            // Fall through to direct field read.
        }

        FieldInfo? field = typeof(CardOpeningSequence)
            .GetField("m_CurrentOpenedCardIndex", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

        return field?.GetValue(sequence) is int index ? index : 0;
    }

    private static float ReadSlider(CardOpeningSequence sequence)
    {
        try
        {
            _sliderGetter ??= CreateSliderGetter();
            if (_sliderGetter != null)
            {
                return _sliderGetter(sequence);
            }
        }
        catch (Exception)
        {
            // Fall through to direct field read.
        }

        FieldInfo? field = typeof(CardOpeningSequence)
            .GetField("m_Slider", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

        return field?.GetValue(sequence) is float slider ? slider : 0f;
    }

    private static Func<CardOpeningSequence, int>? CreateOpenedCardIndexGetter()
    {
        FieldInfo? field = AccessTools.Field(typeof(CardOpeningSequence), "m_CurrentOpenedCardIndex");
        if (field == null)
        {
            return null;
        }

        return sequence => field.GetValue(sequence) is int index ? index : 0;
    }

    private static Func<CardOpeningSequence, float>? CreateSliderGetter()
    {
        FieldInfo? field = AccessTools.Field(typeof(CardOpeningSequence), "m_Slider");
        if (field == null)
        {
            return null;
        }

        return sequence => field.GetValue(sequence) is float slider ? slider : 0f;
    }
}
