using System.Collections;
using TCGShopExpansionMod071Patch.Patches;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TCGShopExpansionMod071Patch;

/// <summary>
/// TextureReplacer runs DoReplace before this plugin loads; retry phone TMP repair once the shop scene is live.
/// Also defers phone-home repairs until the phone canvas has a valid world scale.
/// </summary>
internal sealed class PhoneUiLateRepairBehaviour : MonoBehaviour
{
    private const float ShopSceneRetrySeconds = 0.5f;
    private const float PhoneOpenRepairDelaySeconds = 0.75f;
    private const int PhoneLayoutReadyMaxFrames = 45;

    internal static PhoneUiLateRepairBehaviour? Instance { get; private set; }

    private float _retryTimer;
    private bool _repairedInShopScene;
    private Coroutine? _deferredHomeRepair;
    private Coroutine? _deferredScreenRepair;
    private Coroutine? _deferredMaterialSweep;
    private Coroutine? _deferredPhoneOpenRefresh;
    private Transform? _pendingScreenRoot;

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

    private void Update()
    {
        if (_repairedInShopScene)
        {
            return;
        }

        _retryTimer += Time.unscaledDeltaTime;
        if (_retryTimer < ShopSceneRetrySeconds)
        {
            return;
        }

        _retryTimer = 0f;
        Scene activeScene = SceneManager.GetActiveScene();
        if (activeScene.name != "Start")
        {
            return;
        }

        PhoneManager? phoneManager = PhoneManagerAccess.TryGetReadyPhoneManager();
        if (phoneManager?.m_PhoneGrp == null)
        {
            return;
        }

        PhoneUi071Patches.SchedulePhoneHomeRepair();
        _repairedInShopScene = true;
        Plugin.Log.LogInfo("Phone UI late repair scheduled for Start scene.");
    }

    public static void RequestDeferredPhoneOpenRefresh()
    {
        if (Instance == null)
        {
            return;
        }

        Instance.ScheduleDeferredPhoneOpenRefresh();
    }

    public static void RequestDeferredMaterialSweep()
    {
        if (Instance == null)
        {
            return;
        }

        Instance.ScheduleDeferredMaterialSweep();
    }

    public static void RequestDeferredHomeRepair()
    {
        if (Instance == null)
        {
            return;
        }

        Instance.ScheduleDeferredHomeRepair();
    }

    public static void RequestDeferredScreenRepair(Transform screenRoot)
    {
        if (Instance == null || screenRoot == null)
        {
            return;
        }

        Instance.ScheduleDeferredScreenRepair(screenRoot);
    }

    private void ScheduleDeferredMaterialSweep()
    {
        if (_deferredMaterialSweep != null)
        {
            StopCoroutine(_deferredMaterialSweep);
        }

        _deferredMaterialSweep = StartCoroutine(DeferredMaterialSweepRoutine());
    }

    private IEnumerator DeferredMaterialSweepRoutine()
    {
        yield return null;

        try
        {
            PhoneUi071Patches.RepairFredokaFontAssetsPublic();
            PhoneUi071Patches.RepairAllPhoneAppLabels(materialOnly: true, activeOnly: false);
        }
        catch (System.Exception ex)
        {
            Plugin.Log.LogWarning($"Deferred phone TMP repair failed: {ex.Message}");
        }

        _deferredMaterialSweep = null;
    }

    private void ScheduleDeferredPhoneOpenRefresh()
    {
        if (_deferredPhoneOpenRefresh != null)
        {
            StopCoroutine(_deferredPhoneOpenRefresh);
        }

        _deferredPhoneOpenRefresh = StartCoroutine(DeferredPhoneOpenRefreshRoutine());
    }

    private IEnumerator DeferredPhoneOpenRefreshRoutine()
    {
        for (int frame = 0; frame < PhoneLayoutReadyMaxFrames; frame++)
        {
            if (PhoneUi071Patches.IsPhoneHomeLayoutReady())
            {
                break;
            }

            yield return null;
        }

        yield return new WaitForSeconds(PhoneOpenRepairDelaySeconds);

        try
        {
            PhoneUi071Patches.RepairAllPhoneAppLabels(materialOnly: true, activeOnly: true);
            PhoneManager? phoneManager = PhoneManagerAccess.FindPhoneManager();
            if (phoneManager?.m_PhoneGrp != null)
            {
                PhoneUi071Patches.RepairPhoneHomeScreen(phoneManager.m_PhoneGrp);
            }

            PhoneUi071Patches.LogPhoneUiDiagnosticsAfterPhoneOpen();
        }
        catch (System.Exception ex)
        {
            Plugin.Log.LogWarning($"Deferred phone open refresh failed: {ex.Message}");
        }

        _deferredPhoneOpenRefresh = null;
    }

    private void ScheduleDeferredScreenRepair(Transform screenRoot)
    {
        _pendingScreenRoot = screenRoot;
        if (_deferredScreenRepair != null)
        {
            StopCoroutine(_deferredScreenRepair);
        }

        _deferredScreenRepair = StartCoroutine(DeferredScreenRepairRoutine());
    }

    private IEnumerator DeferredScreenRepairRoutine()
    {
        Transform? screenRoot = _pendingScreenRoot;
        if (screenRoot == null)
        {
            _deferredScreenRepair = null;
            yield break;
        }

        float waitSeconds = PhoneOpenRepairDelaySeconds;
        for (int frame = 0; frame < PhoneLayoutReadyMaxFrames; frame++)
        {
            if (PhoneUi071Patches.IsPhoneScreenLayoutReady(screenRoot))
            {
                waitSeconds = 0.05f;
                break;
            }

            yield return null;
        }

        if (waitSeconds > 0f)
        {
            yield return new WaitForSeconds(waitSeconds);
        }

        if (_pendingScreenRoot != null)
        {
            for (int attempt = 0; attempt < 6; attempt++)
            {
                int repaired = PhoneUi071Patches.RepairPhoneScreen(_pendingScreenRoot);
                if (repaired > 0)
                {
                    break;
                }

                if (PhoneUi071Patches.IsPhoneScreenLayoutReady(_pendingScreenRoot))
                {
                    break;
                }

                yield return new WaitForSeconds(0.1f);
            }
        }

        _pendingScreenRoot = null;
        _deferredScreenRepair = null;
    }

    private void ScheduleDeferredHomeRepair()
    {
        if (_deferredHomeRepair != null)
        {
            StopCoroutine(_deferredHomeRepair);
        }

        _deferredHomeRepair = StartCoroutine(DeferredHomeRepairRoutine());
    }

    private IEnumerator DeferredHomeRepairRoutine()
    {
        float waitSeconds = PhoneOpenRepairDelaySeconds;
        for (int frame = 0; frame < PhoneLayoutReadyMaxFrames; frame++)
        {
            if (PhoneUi071Patches.IsPhoneHomeLayoutReady())
            {
                waitSeconds = 0.05f;
                break;
            }

            yield return null;
        }

        if (waitSeconds > 0f)
        {
            yield return new WaitForSeconds(waitSeconds);
        }

        PhoneManager? phoneManager = PhoneManagerAccess.TryGetReadyPhoneManager()
            ?? PhoneManagerAccess.FindPhoneManager();
        if (phoneManager?.m_PhoneGrp != null)
        {
            int repaired = PhoneUi071Patches.RepairPhoneHomeScreen(phoneManager.m_PhoneGrp);
            if (repaired == 0)
            {
                yield return new WaitForSeconds(0.15f);
                PhoneUi071Patches.RepairPhoneHomeScreen(phoneManager.m_PhoneGrp);
            }
        }

        _deferredHomeRepair = null;
    }
}
