using TCGShopExpansionMod071Patch.Patches;
using UnityEngine;

namespace TCGShopExpansionMod071Patch;

/// <summary>
/// Pack card animations can re-enable m_CardBackMesh after CardOpeningSequence.Update — sync again in LateUpdate.
/// Also retries pack ref bootstrap until CardOpeningSequence refs are wired.
/// </summary>
internal sealed class PackOpeningLateSyncBehaviour : MonoBehaviour
{
    private const float BootstrapRetryIntervalSeconds = 1f;
    private const int BootstrapGiveUpRetries = 45;

    private float _bootstrapRetryTimer;
    private int _bootstrapRetryCount;
    private bool _packRefsReady;
    private bool _loggedBootstrapSuccess;
    private bool _loggedBootstrapGiveUp;

    private void Update()
    {
        if (_packRefsReady)
        {
            return;
        }

        _bootstrapRetryTimer += Time.unscaledDeltaTime;
        if (_bootstrapRetryTimer < BootstrapRetryIntervalSeconds)
        {
            return;
        }

        _bootstrapRetryTimer = 0f;
        _bootstrapRetryCount++;

        CardOpeningSequence? sequence = CSingleton<CardOpeningSequence>.Instance;
        if (sequence == null)
        {
            return;
        }

        PackOpeningRefsBootstrap.TryBootstrap(sequence);
        bool hasMin = PackOpeningRefsBootstrap.HasMinimumPackOpenRefs(sequence);
        bool hasUi = PackOpeningRefsBootstrap.HasOpenSequenceUiRefs(sequence);
        if (hasMin && hasUi)
        {
            _packRefsReady = true;
            PackOpeningRefsBootstrap.TryRecoverStart(sequence);
            if (!_loggedBootstrapSuccess)
            {
                _loggedBootstrapSuccess = true;
                Plugin.Log.LogInfo(
                    "CardOpeningSequence pack refs ready after late sync " +
                    $"(retries={_bootstrapRetryCount}, animator=True, mesh=True, startLerp=True, ui=True).");
            }

            return;
        }

        if (!_loggedBootstrapGiveUp && _bootstrapRetryCount >= BootstrapGiveUpRetries)
        {
            _loggedBootstrapGiveUp = true;
            Plugin.Log.LogWarning(
                "CardOpeningSequence pack ref late sync giving up after " +
                $"{BootstrapGiveUpRetries}s: minRefs={hasMin}, uiRefs={hasUi}, " +
                $"animator={sequence.m_CardPackAnimator != null}, mesh={sequence.m_CardPackMesh != null}, " +
                $"startLerp={sequence.m_StartLerpTransform != null}. " +
                "Pack wrapper animation may stay broken until scene objects are restored from vanilla 0.71.");
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
