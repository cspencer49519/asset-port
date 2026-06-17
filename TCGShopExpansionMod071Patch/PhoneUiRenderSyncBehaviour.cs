using TCGShopExpansionMod071Patch.Patches;
using UnityEngine;

namespace TCGShopExpansionMod071Patch;

/// <summary>
/// One-shot phone label refresh after open. Per-frame canvas hooks made debugging harder.
/// </summary>
internal sealed class PhoneUiRenderSyncBehaviour : MonoBehaviour
{
    internal static PhoneUiRenderSyncBehaviour? Instance { get; private set; }

    private Coroutine? _openRefresh;

    private void Awake()
    {
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public static void ScheduleOpenRefresh()
    {
        Instance?.BeginOpenRefresh();
    }

    private void BeginOpenRefresh()
    {
        if (_openRefresh != null)
        {
            StopCoroutine(_openRefresh);
        }

        _openRefresh = StartCoroutine(OpenRefreshRoutine());
    }

    private System.Collections.IEnumerator OpenRefreshRoutine()
    {
        yield return null;
        yield return null;

        if (PhoneUi071Patches.IsPhoneModeActive)
        {
            PhoneUi071Patches.SyncVisiblePhoneLabelMaterials();
        }

        _openRefresh = null;
    }
}
