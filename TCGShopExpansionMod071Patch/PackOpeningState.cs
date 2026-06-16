using System;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace TCGShopExpansionMod071Patch;

internal static class PackOpeningState
{
    private const float FlipFrontRevealSlider = 0.45f;

    // Dot(card.forward, camera.forward) > 0 means the card's front points the same way the camera looks, i.e.
    // the front face is toward the viewer. The deck rotates from face-down (facing < 0, states 0-4) to face-up
    // (facing > 0, states 5-6) during the open, so the visible side is read directly from this each frame.
    private const float FrontFacingDotThreshold = 0f;

    /// <summary>True when the card's front face is currently rotated toward the camera (show the face, not the back).</summary>
    public static bool IsCardFrontTowardCamera(Card3dUIGroup card3d)
    {
        if (card3d == null)
        {
            return false;
        }

        Camera? cam = CSingleton<InteractionPlayerController>.Instance?.m_Cam;
        if (cam == null)
        {
            // Without a camera reference, fall back to the rotate-to-front state so faces still appear.
            return _cachedStateIndex >= 5;
        }

        float facing = Vector3.Dot(card3d.transform.forward, cam.transform.forward);
        return facing > FrontFacingDotThreshold;
    }

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

    /// <summary>
    /// True during flip states 0-6 after InitOpenSequence (not pack-in-hand readying — UI group stays off until then).
    /// </summary>
    public static bool IsPackOpeningInProgress()
    {
        CardOpeningSequence? sequence = CSingleton<CardOpeningSequence>.Instance;
        if (!ShouldSyncPackPresentation(sequence))
        {
            return false;
        }

        return sequence!.m_StateIndex is >= 0 and < 7;
    }

    /// <summary>Skip readying/cancel lerp where m_IsScreenActive is true but the open UI is not up yet.</summary>
    public static bool ShouldSyncPackPresentation(CardOpeningSequence? sequence)
    {
        if (sequence == null || !sequence.IsActive())
        {
            return false;
        }

        if (sequence.m_CardOpeningUIGroup != null && !sequence.m_CardOpeningUIGroup.activeSelf)
        {
            return false;
        }

        return true;
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

        if (_cachedStateIndex is >= 0 and < 4)
        {
            return GetPackStackTopCardIndex();
        }

        int lastIndex = sequence.m_Card3dUIList.Count - 1;
        Card3dUIGroup? lastCard = sequence.m_Card3dUIList[lastIndex];
        if (lastCard != null && lastCard.gameObject.activeSelf)
        {
            return lastIndex;
        }

        return GetPackStackTopCardIndex();
    }

    /// <summary>
    /// The single face-down deck back shown once during rip / wait (states 0-3), before the group rotates
    /// to front. Vanilla shows exactly one back here, then every card is face up for the rest of the open.
    /// </summary>
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

        // The frontmost card in the depth-ordered deck (the one the player sees on top) shows the lone back.
        return cardIndex == GetCurrentOpenedCardIndex();
    }

    /// <summary>
    /// Cards still in the stack behind the active card during per-card flips (states 4-6). The group has
    /// already rotated to front, so these stay face up (front overlay) and visible behind the card that is
    /// currently sliding off — matching vanilla, where the next card is already showing as the top slides away.
    /// </summary>
    public static bool ShouldShowStackedFrontFace(Card3dUIGroup card3d)
    {
        if (!IsPackFlipState() || card3d == null)
        {
            return false;
        }

        int cardIndex = GetPackCardIndex(card3d);
        return cardIndex >= 0 && cardIndex > GetCurrentOpenedCardIndex();
    }

    /// <summary>
    /// Single Pokemon back shown on the very first card while it flips over to its face. Vanilla only
    /// shows a back here (and during states 0-3); every later card is already face up in the stack, so it
    /// must never flash a back as it becomes the active card.
    /// </summary>
    public static bool ShouldShowActiveCardFlipBack(Card3dUIGroup card3d)
    {
        if (!IsPackFlipState() || card3d == null)
        {
            return false;
        }

        // Only the first card (index 0) flips from the Pokemon back; all others are already face up.
        if (GetCurrentOpenedCardIndex() != 0)
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
