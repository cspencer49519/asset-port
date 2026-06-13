namespace TCGShopExpansionMod071Patch;

internal static class PackOpeningState
{
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

    /// <summary>True during individual card flip animations (states 3-6).</summary>
    public static bool IsPackFlipState()
    {
        CardOpeningSequence? sequence = CSingleton<CardOpeningSequence>.Instance;
        if (sequence == null || !sequence.IsActive())
        {
            return false;
        }

        int state = sequence.m_StateIndex;
        return state is >= 3 and <= 6;
    }

    public static bool ShouldShowPackBackFace(Card3dUIGroup card3d)
    {
        if (!IsPackOpeningInProgress() || card3d == null)
        {
            return false;
        }

        CardOpeningSequence? sequence = CSingleton<CardOpeningSequence>.Instance;
        if (sequence?.m_Card3dUIList == null)
        {
            return false;
        }

        int cardIndex = sequence.m_Card3dUIList.IndexOf(card3d);
        if (cardIndex < 0)
        {
            return false;
        }

        int openedIndex = GetCurrentOpenedCardIndex();
        return cardIndex > openedIndex;
    }

    public static bool ShouldShowPackFlipBackMesh(Card3dUIGroup card3d)
    {
        if (!IsPackFlipState() || card3d == null)
        {
            return false;
        }

        CardOpeningSequence? sequence = CSingleton<CardOpeningSequence>.Instance;
        if (sequence?.m_Card3dUIList == null)
        {
            return false;
        }

        int activeIndex = GetCurrentOpenedCardIndex();
        if (activeIndex < 0 || activeIndex >= sequence.m_Card3dUIList.Count)
        {
            return false;
        }

        return sequence.m_Card3dUIList[activeIndex] == card3d;
    }

    public static int GetCurrentOpenedCardIndex()
    {
        CardOpeningSequence? sequence = CSingleton<CardOpeningSequence>.Instance;
        if (sequence == null || !sequence.IsActive())
        {
            return -1;
        }

        System.Reflection.FieldInfo? field = typeof(CardOpeningSequence)
            .GetField("m_CurrentOpenedCardIndex", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);

        return field?.GetValue(sequence) is int index ? index : -1;
    }
}
