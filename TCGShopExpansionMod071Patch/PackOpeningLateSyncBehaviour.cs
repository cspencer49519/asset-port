using TCGShopExpansionMod071Patch.Patches;
using UnityEngine;

namespace TCGShopExpansionMod071Patch;

/// <summary>
/// Pack card animations can re-enable m_CardBackMesh after CardOpeningSequence.Update — sync again in LateUpdate.
/// Also retries pack ref bootstrap until CardOpeningSequence refs are wired.
/// </summary>
internal sealed class PackOpeningLateSyncBehaviour : MonoBehaviour
{
    private float _bootstrapRetryTimer;
    private bool _packRefsReady;

    private void Update()
    {
        if (_packRefsReady)
        {
            return;
        }

        _bootstrapRetryTimer += Time.unscaledDeltaTime;
        if (_bootstrapRetryTimer < 1f)
        {
            return;
        }

        _bootstrapRetryTimer = 0f;
        CardOpeningSequence? sequence = CSingleton<CardOpeningSequence>.Instance;
        if (sequence == null)
        {
            return;
        }

        PackOpeningRefsBootstrap.TryBootstrap(sequence);
        if (PackOpeningRefsBootstrap.HasMinimumPackOpenRefs(sequence)
            && PackOpeningRefsBootstrap.HasOpenSequenceUiRefs(sequence))
        {
            _packRefsReady = true;
            PackOpeningRefsBootstrap.TryRecoverStart(sequence);
        }
    }

    private void LateUpdate()
    {
        CardOpeningSequence? sequence = CSingleton<CardOpeningSequence>.Instance;
        if (!PackOpeningState.ShouldSyncPackPresentation(sequence))
        {
            return;
        }

        try
        {
            PackOpeningState.SyncFromSequence(sequence!);
            int state = sequence!.m_StateIndex;
            if (state is >= 0 and < 7)
            {
                PackOpening071Patches.SyncAllPackOpeningPresentations(sequence);
            }
            else if (state >= 7)
            {
                PackOpening071Patches.HidePackVisualsDuringFanRow(sequence);
                PackOpening071Patches.SyncAllFanRowPresentations(sequence);
            }
        }
        catch (System.Exception ex)
        {
            Plugin.Log.LogWarning($"Pack opening late sync failed: {ex.GetType().Name}: {ex.Message}");
        }
    }
}
