using System;
using System.Collections.Generic;
using HarmonyLib;
using I2.Loc;
using TCGShopExpansionMod0703Patch;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TCGShopExpansionMod0703Patch.Patches;
/// <summary>
/// Restores readable phone / shop-app UI text after TextureReplacer and ExpansionMod interactions on 0.70.3.
/// </summary>
internal static class PhoneUi0703Patches
{
    private static readonly Dictionary<string, string> PhoneButtonDefaultLabels = new(StringComparer.Ordinal)
    {
        ["PhoneButtonGrp_ScannerOrder"] = "Scanner",
        ["PhoneButtonGrp_HostTournament"] = "Tournament",
        ["PhoneButtonGrp_CameraTakePhoto"] = "Camera",
        ["PhoneButtonGrp_PhotoGallery"] = "Gallery",
        ["PhoneButtonGrp_RestockBoardGame"] = "Board Games",
        ["PhoneButtonGrp_CustomerReview"] = "Reviews",
        ["PhoneButtonGrp_Restock"] = "Restock",
        ["PhoneButtonGrp_Furniture"] = "Furniture",
        ["PhoneButtonGrp_ExpandShop"] = "Expand",
        ["PhoneButtonGrp_Setting"] = "Settings",
        ["PhoneButtonGrp_GameEvent"] = "Events",
        ["PhoneButtonGrp_PriceCheck"] = "Check Price",
        ["PhoneButtonGrp_Hiring"] = "Hire",
        ["PhoneButtonGrp_RentBill"] = "Bills",
        ["PhoneButtonGrp_BuyDecoration"] = "Decorate",
        ["PhoneButtonGrp_Grading"] = "Grade",
    };

    private static readonly Dictionary<string, TMP_FontAsset> CachedFredokaFonts = new(StringComparer.Ordinal);
    private static readonly Dictionary<int, string> ScannerTitleTextById = new();
    private static bool fontsCached;
    private static bool loggedPhoneUiDiagnostics;
    private static bool loggedSubScreenLayering;

    internal static bool IsPhoneModeActive { get; private set; }

    private static readonly string[] TmpShaderNames =
    {
        "TextMeshPro/Distance Field",
        "TextMeshPro/Mobile/Distance Field",
    };

    [HarmonyPostfix]
    [HarmonyPatch(
        typeof(LocalizationManager),
        "GetTranslation",
        typeof(string),
        typeof(bool),
        typeof(int),
        typeof(bool),
        typeof(bool),
        typeof(GameObject),
        typeof(string),
        typeof(bool))]
    public static void LocalizationManager_GetTranslation_Postfix(string Term, ref string __result)
    {
        if (string.IsNullOrEmpty(__result) && !string.IsNullOrEmpty(Term))
        {
            __result = Term;
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(ItemData), nameof(ItemData.GetName))]
    public static void ItemData_GetName_Postfix(ItemData __instance, ref string __result)
    {
        if (!string.IsNullOrEmpty(__result))
        {
            return;
        }

        if (!string.IsNullOrEmpty(__instance.name))
        {
            __result = LocalizationManager.GetTranslation(__instance.name, true, 0, true, false, null, null, true);
        }

        if (string.IsNullOrEmpty(__result))
        {
            __result = ResolveExpansionPackDisplayName(__instance.name);
        }

        if (string.IsNullOrEmpty(__result) && !string.IsNullOrEmpty(__instance.name))
        {
            __result = __instance.name;
        }
    }

    public static void RepairPhoneUi(Transform? root)
    {
        RepairPhoneHomeScreen(root);
    }

    public static void SchedulePhoneScreenRepair(Transform? screenRoot)
    {
        Transform? resolvedRoot = PhoneAppUiScope.ResolvePhoneScreenContentRoot(screenRoot);
        if (resolvedRoot == null)
        {
            return;
        }

        PhoneUiLateRepairBehaviour.RequestDeferredScreenRepair(resolvedRoot);
    }

    public static void SchedulePhoneAppRepair(Transform? fromTransform)
    {
        SchedulePhoneScreenRepair(fromTransform);
    }

    public static bool IsPhoneScreenLayoutReady(Transform root)
    {
        if (root == null)
        {
            return false;
        }

        foreach (TextMeshProUGUI label in root.GetComponentsInChildren<TextMeshProUGUI>(true))
        {
            if (label != null
                && label.gameObject.activeInHierarchy
                && IsLabelLayoutReady(label))
            {
                return true;
            }
        }

        return false;
    }

    public static void RepairAllPhoneAppTmpMaterials()
    {
        RepairAllPhoneAppLabels(materialOnly: true);
    }

    public static void RepairAllPhoneAppLabels(bool materialOnly = false, bool activeOnly = true)
    {
        try
        {
            ResetFontCache();
            RepairFredokaFontAssetsPublic();

            PhoneManager? phoneManager = PhoneManagerAccess.FindPhoneManager();
            if (phoneManager == null)
            {
                Plugin.Log.LogInfo("Phone TMP repair skipped: PhoneManager not found.");
                return;
            }

            PhoneCanvasRepair.EnsurePhoneCanvasesReady(phoneManager.m_PhoneGrp);

            int repaired = 0;
            HashSet<int> seenLabels = new HashSet<int>();
            foreach (Transform root in PhoneAppUiScope.EnumeratePhoneUiRoots(phoneManager))
            {
                foreach (TextMeshProUGUI label in root.GetComponentsInChildren<TextMeshProUGUI>(true))
                {
                    if (label == null || !seenLabels.Add(label.GetInstanceID()))
                    {
                        continue;
                    }

                    if (activeOnly && !label.gameObject.activeInHierarchy)
                    {
                        continue;
                    }

                    if (materialOnly)
                    {
                        if (EnsurePhoneLabelMaterial(label))
                        {
                            repaired++;
                        }
                    }
                    else if (RepairPhoneWorldLabel(label, skipScopeCheck: true))
                    {
                        repaired++;
                    }
                }
            }

            Plugin.Log.LogInfo(
                materialOnly
                    ? $"Phone TMP material sweep repaired {repaired} label material(s)."
                    : $"Phone TMP label repair refreshed {repaired} label(s).");
        }
        catch (System.Exception ex)
        {
            Plugin.Log.LogWarning($"Phone TMP repair failed: {ex.Message}");
        }
    }

    public static int RepairPhoneScreen(Transform? screenRoot)
    {
        Transform? contentRoot = PhoneAppUiScope.ResolvePhoneScreenContentRoot(screenRoot);
        if (contentRoot == null)
        {
            return 0;
        }

        if (!loggedSubScreenLayering && IsPhoneModeActive)
        {
            loggedSubScreenLayering = true;
            DumpPhoneCanvasLayering(contentRoot, $"SUB:{(screenRoot != null ? screenRoot.name : contentRoot.name)}");
        }

        int repaired = 0;
        int meshRefreshed = 0;
        try
        {
            ResetFontCache();
            RepairFredokaFontAssetsPublic();
            RestoreScannerTitleTexts(contentRoot);
            foreach (TextMeshProUGUI label in contentRoot.GetComponentsInChildren<TextMeshProUGUI>(true))
            {
                if (label == null || !label.gameObject.activeInHierarchy)
                {
                    continue;
                }

                if (RepairPhoneWorldLabel(label, skipScopeCheck: true))
                {
                    repaired++;
                    if (RefreshPhoneLabelMesh(label))
                    {
                        meshRefreshed++;
                    }
                }
            }

            if (meshRefreshed > 0)
            {
                Canvas.ForceUpdateCanvases();
            }

            Plugin.Log.LogInfo(
                $"Phone screen UI repair on '{contentRoot.name}': {repaired} label(s), {meshRefreshed} mesh refresh(es).");
        }
        catch (System.Exception ex)
        {
            Plugin.Log.LogWarning($"Phone screen UI repair failed: {ex.Message}");
        }

        return repaired;
    }

    public static void SchedulePhoneHomeRepair()
    {
        PhoneUiLateRepairBehaviour.RequestDeferredHomeRepair();
    }

    public static int RepairPhoneHomeScreen(Transform? root)
    {
        Transform? homeRoot = ResolvePhoneHomeRoot(root);
        if (homeRoot == null)
        {
            return 0;
        }

        PhoneManager? phoneManager = PhoneManagerAccess.FindPhoneManager();
        PhoneCanvasRepair.EnsurePhoneCanvasesReady(phoneManager?.m_PhoneGrp);

        int repaired = 0;
        try
        {
            ResetFontCache();
            RepairFredokaFontAssetsPublic();
            RestorePhoneHomeButtonLabels(homeRoot);
            repaired += RepairPhoneHomeStatusTexts(homeRoot);
            FixPhoneHomeTextClipping(homeRoot);
            FixPhoneHomeOverlayArtifacts(homeRoot);
            RefreshPhoneLabelsInRoot(homeRoot);
            if (IsPhoneModeActive)
            {
                LogPhoneUiDiagnosticsOnce(homeRoot);
            }

            Plugin.Log.LogInfo($"Phone home UI repair on '{homeRoot.name}': {repaired} header label(s).");
        }
        catch (System.Exception ex)
        {
            Plugin.Log.LogWarning($"Phone UI repair failed: {ex.Message}");
        }

        return repaired;
    }

    public static bool IsPhoneHomeLayoutReady()
    {
        PhoneManager? phoneManager = PhoneManagerAccess.TryGetReadyPhoneManager();
        Transform? homeRoot = ResolvePhoneHomeRoot(phoneManager?.m_PhoneGrp);
        if (homeRoot == null)
        {
            return false;
        }

        foreach (Transform child in homeRoot.GetComponentsInChildren<Transform>(true))
        {
            if (!child.name.StartsWith("PhoneButtonGrp_", StringComparison.Ordinal))
            {
                continue;
            }

            TextMeshProUGUI? label = child.Find("Text")?.GetComponent<TextMeshProUGUI>();
            if (label != null && IsLabelLayoutReady(label))
            {
                return true;
            }
        }

        return false;
    }

    private static Transform? ResolvePhoneHomeRoot(Transform? root)
    {
        PhoneManager? phoneManager = PhoneManagerAccess.FindPhoneManager();
        if (phoneManager?.m_UI_PhoneScreen?.m_ScreenGroup != null)
        {
            return phoneManager.m_UI_PhoneScreen.m_ScreenGroup.transform;
        }

        if (root == null)
        {
            return null;
        }

        if (root.name == "UI_PhoneScreen_Grp")
        {
            return root;
        }

        Transform? nested = root.Find("UI_PhoneScreen_Grp");
        if (nested != null)
        {
            return nested;
        }

        foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
        {
            if (child.name == "UI_PhoneScreen_Grp")
            {
                return child;
            }
        }

        return phoneManager?.m_UI_PhoneScreen != null
            ? phoneManager.m_UI_PhoneScreen.transform
            : root;
    }

    /// <summary>
    /// One-shot dump of the phone canvas/sorting layering so we can see exactly what occludes the
    /// text labels. Logs every Canvas under the phone (sorting context), the graphics of a sample
    /// button cell (which canvas each renders to + computed depth/queue), and any full-screen image
    /// that could be the overlay drawn over the text.
    /// </summary>
    private static void DumpPhoneCanvasLayering(Transform root, string tag)
    {
        try
        {
            foreach (Canvas canvas in root.GetComponentsInChildren<Canvas>(true))
            {
                Plugin.Log.LogInfo(
                    $"[{tag}] CANVAS '{GetTransformPath(canvas.transform, root)}' mode={canvas.renderMode} overrideSorting={canvas.overrideSorting} sortingLayer='{canvas.sortingLayerName}' sortingOrder={canvas.sortingOrder} enabled={canvas.isActiveAndEnabled}");
            }

            Transform? cell = FindDescendantByName(root, "PhoneButtonGrp_Restock");
            if (cell != null)
            {
                foreach (Graphic g in cell.GetComponentsInChildren<Graphic>(true))
                {
                    Canvas? gc = g.canvas;
                    Plugin.Log.LogInfo(
                        $"[{tag}] GFX '{GetTransformPath(g.transform, root)}' type={g.GetType().Name} canvas='{(gc != null ? GetTransformPath(gc.transform, root) : "null")}' sortOrder={(gc != null ? gc.sortingOrder : -999)} queue={g.materialForRendering?.renderQueue} depth={g.depth} sibling={g.transform.GetSiblingIndex()}");
                }
            }

            foreach (Image img in root.GetComponentsInChildren<Image>(true))
            {
                RectTransform rt = img.rectTransform;
                if (rt.rect.width < 60f || rt.rect.height < 60f)
                {
                    continue;
                }

                Canvas? ic = img.canvas;
                Plugin.Log.LogInfo(
                    $"[{tag}] FULLSCREEN '{GetTransformPath(img.transform, root)}' rect=({rt.rect.width:F0}x{rt.rect.height:F0}) canvas='{(ic != null ? GetTransformPath(ic.transform, root) : "null")}' sortOrder={(ic != null ? ic.sortingOrder : -999)} queue={img.materialForRendering?.renderQueue} depth={img.depth} sibling={img.transform.GetSiblingIndex()} colorA={img.color.a:F2} raycast={img.raycastTarget}");
            }
        }
        catch (System.Exception ex)
        {
            Plugin.Log.LogWarning($"[{tag}] canvas layering dump failed: {ex.Message}");
        }
    }

    private static Transform? FindDescendantByName(Transform root, string name)
    {
        foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
        {
            if (t.name == name)
            {
                return t;
            }
        }

        return null;
    }

    private static string GetTransformPath(Transform t, Transform stopAt)
    {
        string path = t.name;
        Transform? node = t.parent;
        while (node != null && node != stopAt)
        {
            path = node.name + "/" + path;
            node = node.parent;
        }

        return path;
    }

    /// <summary>
    /// Shows / hides the phone home content (clock, day, app grid) while a sub-screen is open.
    /// The home content and every sub-screen group are sibling children of the same home
    /// <c>m_ScreenGroup</c>, so sub-screens with transparent bodies (e.g. Hire) otherwise let the
    /// home icons show through. We deactivate only the home-content children, never the sub-screen
    /// containers, so the nested sub-screens keep rendering.
    /// </summary>
    private static void SetPhoneHomeContentActive(bool active)
    {
        try
        {
            PhoneManager? phoneManager = PhoneManagerAccess.FindPhoneManager();
            UI_PhoneScreen? home = phoneManager?.m_UI_PhoneScreen;
            GameObject? group = home?.m_ScreenGroup;
            if (phoneManager == null || home == null || group == null)
            {
                return;
            }

            Transform homeRoot = group.transform;

            List<Transform> subScreenTransforms = new();
            foreach (UIScreenBase? screen in PhoneAppUiScope.EnumerateScreens(phoneManager))
            {
                if (screen == null || ReferenceEquals(screen, home))
                {
                    continue;
                }

                subScreenTransforms.Add(screen.transform);
                if (screen.m_ScreenGroup != null)
                {
                    subScreenTransforms.Add(screen.m_ScreenGroup.transform);
                }
            }

            foreach (Transform child in homeRoot)
            {
                bool isSubScreenContainer = false;
                foreach (Transform sub in subScreenTransforms)
                {
                    if (sub == child || sub.IsChildOf(child))
                    {
                        isSubScreenContainer = true;
                        break;
                    }
                }

                if (isSubScreenContainer)
                {
                    continue;
                }

                if (child.gameObject.activeSelf != active)
                {
                    child.gameObject.SetActive(active);
                }
            }
        }
        catch (System.Exception ex)
        {
            Plugin.Log.LogWarning($"Phone home content toggle failed: {ex.Message}");
        }
    }

    private static int RepairPhoneHomeStatusTexts(Transform homeRoot)
    {
        int repaired = 0;
        PhoneManager? phoneManager = PhoneManagerAccess.TryGetReadyPhoneManager()
            ?? PhoneManagerAccess.FindPhoneManager();
        UI_PhoneScreen? screen = phoneManager?.m_UI_PhoneScreen;
        if (screen != null)
        {
            if (screen.m_TimeText != null && RepairPhoneWorldLabel(screen.m_TimeText, skipScopeCheck: true))
            {
                repaired++;
            }

            if (screen.m_DayText != null && RepairPhoneWorldLabel(screen.m_DayText, skipScopeCheck: true))
            {
                repaired++;
            }

            return repaired;
        }

        foreach (TextMeshProUGUI label in homeRoot.GetComponentsInChildren<TextMeshProUGUI>(true))
        {
            if (IsPhoneHeaderLabel(label) && RepairPhoneWorldLabel(label, skipScopeCheck: true))
            {
                repaired++;
            }
        }

        return repaired;
    }

    private static bool IsPhoneAppLabel(TextMeshProUGUI label)
    {
        return label != null && PhoneAppUiScope.IsPhoneAppTransform(label.transform);
    }

    private static bool IsLabelLayoutReady(TextMeshProUGUI label)
    {
        Vector3 lossyScale = label.transform.lossyScale;
        return lossyScale.x > 0.0001f && lossyScale.y > 0.0001f;
    }

    public static void RestorePhoneHomeButtonLabels(Transform? root)
    {
        if (root == null)
        {
            return;
        }

        foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
        {
            if (!child.name.StartsWith("PhoneButtonGrp_", StringComparison.Ordinal))
            {
                continue;
            }

            if (!PhoneButtonDefaultLabels.TryGetValue(child.name, out string? fallbackLabel))
            {
                continue;
            }

            foreach (TextMeshProUGUI label in child.GetComponentsInChildren<TextMeshProUGUI>(true))
            {
                if (label == null || label.name != "Text")
                {
                    continue;
                }

                string localized = LocalizationManager.GetTranslation(fallbackLabel, true, 0, true, false, null, null, true);
                string display = string.IsNullOrEmpty(localized) ? fallbackLabel : localized;
                if (!string.Equals(label.text, display, StringComparison.Ordinal))
                {
                    label.text = display;
                }

                RepairPhoneWorldLabel(label, skipScopeCheck: true);
            }
        }
    }

    public static void LogPhoneUiDiagnosticsOnce(Transform? root)
    {
        if (loggedPhoneUiDiagnostics || root == null)
        {
            return;
        }

        loggedPhoneUiDiagnostics = true;
        DumpButtonHierarchyOnce(root);
        DumpPhoneCanvasLayering(root, "HOME");
        int logged = 0;
        foreach (TextMeshProUGUI label in root.GetComponentsInChildren<TextMeshProUGUI>(true))
        {
            if (label == null || label.name != "Text" || label.transform.parent == null)
            {
                continue;
            }

            if (!label.transform.parent.name.StartsWith("PhoneButtonGrp_", StringComparison.Ordinal))
            {
                continue;
            }

            Plugin.Log.LogInfo(
                $"Phone UI sample [{label.transform.parent.name}]: text='{label.text}', font='{label.font?.name}', fontSize={label.fontSize:F1}, verts={label.mesh?.vertexCount ?? 0}, {DescribePhoneLabelRender(label)}, lossyScale=({label.transform.lossyScale.x:F4},{label.transform.lossyScale.y:F4}), active={label.gameObject.activeInHierarchy}, canvas={DescribePhoneLabelCanvas(label)}, sibling={label.transform.GetSiblingIndex()}");
            logged++;
            if (logged >= 4)
            {
                break;
            }
        }
    }

    public static void LogPhoneUiDiagnosticsAfterPhoneOpen()
    {
        loggedPhoneUiDiagnostics = false;
        PhoneManager? phoneManager = PhoneManagerAccess.FindPhoneManager();
        LogPhoneUiDiagnosticsOnce(ResolvePhoneHomeRoot(phoneManager?.m_PhoneGrp));
    }

    private static void DumpButtonHierarchyOnce(Transform root)
    {
        foreach (Transform cell in root.GetComponentsInChildren<Transform>(true))
        {
            if (cell.name != "PhoneButtonGrp_Restock")
            {
                continue;
            }

            Plugin.Log.LogInfo($"Phone button DUMP cell '{cell.name}' rect={DescribeRect(cell)} localScale={cell.localScale}");
            foreach (Transform child in cell.GetComponentsInChildren<Transform>(true))
            {
                if (child == cell)
                {
                    continue;
                }

                Plugin.Log.LogInfo($"  child '{child.name}' active={child.gameObject.activeSelf} rect={DescribeRect(child)} localScale={child.localScale} localPos={child.localPosition}");
            }

            return;
        }
    }

    private static string DescribeRect(Transform t)
    {
        if (t is RectTransform rt)
        {
            Rect r = rt.rect;
            return $"(w={r.width:F1},h={r.height:F1},sizeDelta={rt.sizeDelta},anchMin={rt.anchorMin},anchMax={rt.anchorMax})";
        }

        return "(no RectTransform)";
    }

    private static string DescribePhoneLabelRender(TextMeshProUGUI label)
    {
        CanvasRenderer? cr = label.canvasRenderer;
        string crInfo;
        if (cr == null)
        {
            crInfo = "cr=null";
        }
        else
        {
            Material? crMat = cr.materialCount > 0 ? cr.GetMaterial(0) : null;
            crInfo = $"crMatCount={cr.materialCount}, crMat='{crMat?.name}', crQueue={crMat?.renderQueue}, crTex='{crMat?.mainTexture?.name}', crCull={cr.cull}, crAlpha={cr.GetAlpha():F2}";
        }

        Bounds bounds = label.mesh != null ? label.mesh.bounds : default;
        float worldHeight = bounds.size.y * Mathf.Abs(label.transform.lossyScale.y);
        float worldWidth = bounds.size.x * Mathf.Abs(label.transform.lossyScale.x);
        float iconHeight = IsPhoneButtonLabel(label) ? GetButtonIconHeight(label) : GetRectHeight(label.rectTransform);

        return $"{crInfo}, faceA={label.faceColor.a}, colorA={label.color.a:F2}, refH={iconHeight:F1}, meshLocal=({bounds.size.x:F2}x{bounds.size.y:F2}), meshWorld=({worldWidth:F5}x{worldHeight:F5})";
    }

    private static string DescribePhoneLabelCanvas(TextMeshProUGUI label)
    {
        Canvas? canvas = label.canvas;
        if (canvas == null)
        {
            return "null";
        }

        Canvas root = canvas.rootCanvas;
        bool rootHasTmpChannels =
            (root.additionalShaderChannels & AdditionalCanvasShaderChannels.TexCoord1) != 0
            && (root.additionalShaderChannels & AdditionalCanvasShaderChannels.Normal) != 0
            && (root.additionalShaderChannels & AdditionalCanvasShaderChannels.Tangent) != 0;

        return $"{canvas.renderMode}/enabled={canvas.isActiveAndEnabled}/root='{root.name}'/rootChannels={root.additionalShaderChannels}/rootTmpOk={rootHasTmpChannels}";
    }

    private static void ResetFontCache()
    {
        fontsCached = false;
        CachedFredokaFonts.Clear();
    }

    [HarmonyPostfix]
    [HarmonyPriority(999)]
    [HarmonyPatch(typeof(PhoneManager), "EnterPhoneMode")]
    public static void PhoneManager_EnterPhoneMode_Postfix()
    {
        IsPhoneModeActive = true;
        loggedPhoneUiDiagnostics = false;
        loggedSubScreenLayering = false;
        SchedulePhoneHomeRepair();
        PhoneUiLateRepairBehaviour.RequestDeferredPhoneOpenRefresh();
        PhoneUiRenderSyncBehaviour.ScheduleOpenRefresh();
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(PhoneManager), "ExitPhoneMode")]
    public static void PhoneManager_ExitPhoneMode_Postfix()
    {
        IsPhoneModeActive = false;
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(UI_PhoneScreen), "OnOpenScreen")]
    public static void UI_PhoneScreen_OnOpenScreen_Postfix(UI_PhoneScreen __instance)
    {
        if (__instance.m_ScreenGroup != null && !__instance.m_ScreenGroup.activeSelf)
        {
            __instance.m_ScreenGroup.SetActive(true);
        }

        // Home screen is showing: make sure its content (icons / clock / day) is visible again.
        SetPhoneHomeContentActive(true);
        SchedulePhoneHomeRepair();
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(UI_PhoneScreen), "OnChildScreenClosed")]
    public static void UI_PhoneScreen_OnChildScreenClosed_Postfix()
    {
        // Back on the home screen: restore the home content we hid while the sub-screen was open.
        SetPhoneHomeContentActive(true);
        SchedulePhoneHomeRepair();
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(RestockItemPanelUI), nameof(RestockItemPanelUI.Init))]
    public static void RestockItemPanelUI_Init_Postfix(RestockItemPanelUI __instance)
    {
        SchedulePhoneAppRepair(__instance.transform);
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(RestockItemScreen), "EvaluateRestockItemPanelUI")]
    public static void RestockItemScreen_EvaluateRestockItemPanelUI_Postfix(RestockItemScreen __instance)
    {
        SchedulePhoneAppRepair(__instance.transform);
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(HireWorkerScreen), "OnOpenScreen")]
    public static void HireWorkerScreen_OnOpenScreen_Postfix(HireWorkerScreen __instance)
    {
        SetPhoneHomeContentActive(false);
        SchedulePhoneAppRepair(__instance.transform);
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(HireWorkerPanelUI), "Init", typeof(HireWorkerScreen), typeof(int))]
    public static void HireWorkerPanelUI_Init_Postfix(HireWorkerPanelUI __instance)
    {
        RepairPhoneWorldLabelsInTransform(__instance.transform);
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(RestockItemScreen), "OnOpenScreen")]
    public static void RestockItemScreen_OnOpenScreen_Postfix(RestockItemScreen __instance)
    {
        SetPhoneHomeContentActive(false);
        SchedulePhoneAppRepair(__instance.transform);
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(ScannerRestockScreen), "Awake")]
    public static void ScannerRestockScreen_Awake_Prefix(ScannerRestockScreen __instance)
    {
        CacheScannerTitleTexts(__instance.transform);
    }

    [HarmonyPostfix]
    [HarmonyPriority(1000)]
    [HarmonyPatch(typeof(ScannerRestockScreen), "OnOpenScreen")]
    public static void ScannerRestockScreen_OnOpenScreen_Postfix(ScannerRestockScreen __instance)
    {
        SetPhoneHomeContentActive(false);
        SchedulePhoneAppRepair(__instance.transform);
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(RentBillScreen), "OnOpenScreen")]
    public static void RentBillScreen_OnOpenScreen_Postfix(RentBillScreen __instance)
    {
        SetPhoneHomeContentActive(false);
        SchedulePhoneAppRepair(__instance.transform);
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(RentBillScreen), "EvaluateUI")]
    public static void RentBillScreen_EvaluateUI_Postfix(RentBillScreen __instance)
    {
        SchedulePhoneAppRepair(__instance.transform);
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(RentBillPanelUI), nameof(RentBillPanelUI.EvaluateUI))]
    public static void RentBillPanelUI_EvaluateUI_Postfix(RentBillPanelUI __instance)
    {
        SchedulePhoneAppRepair(__instance.transform);
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(RestockCheckoutItemBar), nameof(RestockCheckoutItemBar.UpdateData))]
    public static void RestockCheckoutItemBar_UpdateData_Postfix(RestockCheckoutItemBar __instance)
    {
        RepairPhoneWorldLabelsInTransform(__instance.transform);
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(CheckPricePanelUI), nameof(CheckPricePanelUI.InitItem))]
    public static void CheckPricePanelUI_InitItem_Postfix(CheckPricePanelUI __instance)
    {
        SchedulePhoneAppRepair(__instance.transform);
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(CheckPricePanelUI), "InitCard")]
    public static void CheckPricePanelUI_InitCard_Postfix(CheckPricePanelUI __instance)
    {
        SchedulePhoneAppRepair(__instance.transform);
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(CustomerReviewPanelUI), nameof(CustomerReviewPanelUI.Init))]
    public static void CustomerReviewPanelUI_Init_Postfix(CustomerReviewPanelUI __instance)
    {
        SchedulePhoneAppRepair(__instance.transform);
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(UIScreenBase), "OnOpenScreen")]
    public static void UIScreenBase_OnOpenScreen_Postfix(UIScreenBase __instance)
    {
        if (!PhoneAppUiScope.IsPhoneRelatedScreen(__instance))
        {
            return;
        }

        // A sub-screen (anything other than the home screen) is opening; hide the home content so it
        // doesn't show through sub-screens that have transparent bodies (e.g. Hire).
        if (!(__instance is UI_PhoneScreen))
        {
            SetPhoneHomeContentActive(false);
        }

        SchedulePhoneAppRepair(__instance.transform);
    }

    private static string ResolveExpansionPackDisplayName(string? itemNameKey)
    {
        if (string.IsNullOrEmpty(itemNameKey))
        {
            return string.Empty;
        }

        string? translationKey = itemNameKey switch
        {
            "MegabotPack" => "Megabot",
            "FantasyRPGPack" => "FantasyRPG",
            "CatJobPack" => "CatJob",
            "GhostPack" => "Ghost",
            _ => null
        };

        if (translationKey == null)
        {
            return string.Empty;
        }

        string translated = LocalizationManager.GetTranslation(translationKey, true, 0, true, false, null, null, true);
        if (!string.IsNullOrEmpty(translated))
        {
            return translated;
        }

        return TryGetExpansionModPackName(translationKey);
    }

    private static string TryGetExpansionModPackName(string translationKey)
    {
        System.Type? playerPatches = AccessTools.TypeByName("TCGShopExpansionMod.Patches.PlayerPatches");
        if (playerPatches == null)
        {
            return string.Empty;
        }

        string? fieldName = translationKey switch
        {
            "Megabot" => "newMegaBotPackName",
            "FantasyRPG" => "newFantasyRPGPackName",
            "CatJob" => "newCatJobPackName",
            _ => null
        };

        if (fieldName == null)
        {
            return string.Empty;
        }

        return AccessTools.Field(playerPatches, fieldName)?.GetValue(null) as string ?? string.Empty;
    }

    private static void CacheScannerTitleTexts(Transform root)
    {
        if (root == null)
        {
            return;
        }

        foreach (TextMeshProUGUI label in root.GetComponentsInChildren<TextMeshProUGUI>(true))
        {
            if (label == null || label.name != "TitleText")
            {
                continue;
            }

            int id = label.GetInstanceID();
            if (!ScannerTitleTextById.ContainsKey(id) && !string.IsNullOrEmpty(label.text))
            {
                ScannerTitleTextById[id] = label.text;
            }
        }
    }

    private static void RestoreScannerTitleTexts(Transform root)
    {
        if (root == null)
        {
            return;
        }

        foreach (TextMeshProUGUI label in root.GetComponentsInChildren<TextMeshProUGUI>(true))
        {
            if (label == null || label.name != "TitleText" || !string.IsNullOrEmpty(label.text))
            {
                continue;
            }

            int id = label.GetInstanceID();
            if (ScannerTitleTextById.TryGetValue(id, out string? original) && !string.IsNullOrEmpty(original))
            {
                label.text = original;
            }
        }
    }

    public static void RepairTmpInHierarchy(Transform? root)
    {
        if (root == null)
        {
            return;
        }

        if (PhoneAppUiScope.IsPhoneAppTransform(root))
        {
            RepairPhoneWorldLabelsInTransform(root);
            return;
        }

        EnsureFontsCached();
        TextMeshProUGUI[] labels = root.GetComponentsInChildren<TextMeshProUGUI>(true);
        foreach (TextMeshProUGUI label in labels)
        {
            if (!label.gameObject.activeInHierarchy)
            {
                continue;
            }

            RepairHudTmp(label);
        }
    }

    private static void RepairPhoneWorldLabelsInTransform(Transform root)
    {
        EnsureFontsCached();
        foreach (TextMeshProUGUI label in root.GetComponentsInChildren<TextMeshProUGUI>(true))
        {
            if (label == null || !label.gameObject.activeInHierarchy)
            {
                continue;
            }

            RepairPhoneWorldLabel(label, skipScopeCheck: true);
        }
    }

    private static bool RepairPhoneWorldLabel(TextMeshProUGUI label, bool skipScopeCheck = false)
    {
        if (label == null)
        {
            return false;
        }

        if (!skipScopeCheck && !PhoneAppUiScope.IsPhoneAppTransform(label.transform))
        {
            return false;
        }

        TMP_FontAsset? phoneFont = ResolvePhoneFont();
        if (phoneFont != null)
        {
            // Instance material only — never rewrite shared font.material (breaks cash register TMP).
            label.font = phoneFont;
            label.fontMaterial = PhoneFontMaterialSnapshot.CreateLabelMaterial(phoneFont);
        }

        phoneFont = label.font;
        if (phoneFont != null)
        {
            RepairFontMaterial(phoneFont);
        }

        ApplyPhoneLabelColors(label);
        ApplyPhoneWorldFontSizing(label);
        PhoneCanvasRepair.EnsureLabelCanvasChannels(label.canvas);
        EnsurePhoneLabelMaterial(label);
        SyncPhoneLabelCanvasRenderer(label);

        if (IsPhoneButtonLabel(label))
        {
            EnsurePhoneButtonTextDrawOrder(label);
        }

        if (label.gameObject.activeInHierarchy && ShouldRebuildPhoneLabelMesh(label))
        {
            RefreshPhoneLabelMesh(label);
        }

        label.enabled = true;
        label.SetAllDirty();
        return true;
    }

    public static void SyncVisiblePhoneLabelMaterials()
    {
        PhoneManager? phoneManager = PhoneManagerAccess.FindPhoneManager();
        if (phoneManager == null)
        {
            return;
        }

        foreach (Transform root in EnumerateActivePhoneUiRoots(phoneManager))
        {
            foreach (TextMeshProUGUI label in root.GetComponentsInChildren<TextMeshProUGUI>(true))
            {
                if (label == null || !label.gameObject.activeInHierarchy)
                {
                    continue;
                }

                if (!ShouldRebuildPhoneLabelMesh(label))
                {
                    continue;
                }

                if (!IsPhoneLabelAtlasBound(label))
                {
                    TMP_FontAsset? font = ResolvePhoneFont();
                    if (font != null)
                    {
                        label.font = font;
                    }

                    EnsurePhoneLabelMaterial(label);
                }

                if (label.mesh == null || label.mesh.vertexCount == 0)
                {
                    label.ForceMeshUpdate(true, false);
                }
            }
        }
    }

    private static System.Collections.Generic.IEnumerable<Transform> EnumerateActivePhoneUiRoots(PhoneManager phoneManager)
    {
        foreach (Transform root in PhoneAppUiScope.EnumeratePhoneUiRoots(phoneManager))
        {
            if (root != null && root.gameObject.activeInHierarchy)
            {
                yield return root;
            }
        }
    }

    private static void FixPhoneHomeTextClipping(Transform homeRoot)
    {
        foreach (RectMask2D mask in homeRoot.GetComponentsInChildren<RectMask2D>(true))
        {
            if (mask != null)
            {
                mask.enabled = false;
            }
        }

        foreach (TextMeshProUGUI label in homeRoot.GetComponentsInChildren<TextMeshProUGUI>(true))
        {
            if (label == null)
            {
                continue;
            }

            label.maskable = false;
            label.raycastTarget = false;
        }
    }

    private static void FixPhoneHomeOverlayArtifacts(Transform homeRoot)
    {
        foreach (Transform buttonGrp in homeRoot.GetComponentsInChildren<Transform>(true))
        {
            if (!buttonGrp.name.StartsWith("PhoneButtonGrp_", StringComparison.Ordinal))
            {
                continue;
            }

            foreach (Transform child in buttonGrp)
            {
                string childName = child.name;
                if (childName == "Icon2"
                    || childName == "Icon (1)"
                    || childName == "Icon (2)")
                {
                    child.gameObject.SetActive(false);
                }
            }
        }
    }

    private static readonly Dictionary<int, PhoneLabelGeometry> OriginalLabelGeometry = new();

    private readonly struct PhoneLabelGeometry
    {
        public PhoneLabelGeometry(float fontSize, bool autoSizing, Vector3 localScale, Vector3 localPosition, bool wordWrap, TextOverflowModes overflow)
        {
            FontSize = fontSize;
            AutoSizing = autoSizing;
            LocalScale = localScale;
            LocalPosition = localPosition;
            WordWrap = wordWrap;
            Overflow = overflow;
        }

        public float FontSize { get; }
        public bool AutoSizing { get; }
        public Vector3 LocalScale { get; }
        public Vector3 LocalPosition { get; }
        public bool WordWrap { get; }
        public TextOverflowModes Overflow { get; }
    }

    /// <summary>
    /// Restore the prefab's original text geometry. The phone labels already have correct vanilla
    /// font size / scale / position; our prior overrides (and any z-nudge) only broke them. We
    /// capture the values the first time we see each label, before touching anything, then keep
    /// re-applying them so repeated repair passes never drift.
    /// </summary>
    private static void RestorePhoneLabelGeometry(TextMeshProUGUI label)
    {
        int id = label.GetInstanceID();
        if (!OriginalLabelGeometry.TryGetValue(id, out PhoneLabelGeometry geo))
        {
            geo = new PhoneLabelGeometry(
                label.fontSize,
                label.enableAutoSizing,
                label.transform.localScale,
                label.transform.localPosition,
                label.enableWordWrapping,
                label.overflowMode);
            OriginalLabelGeometry[id] = geo;
        }

        label.enableAutoSizing = geo.AutoSizing;
        label.fontSize = geo.FontSize;
        label.enableWordWrapping = geo.WordWrap;
        label.overflowMode = geo.Overflow;

        Transform t = label.transform;
        if (t.localScale != geo.LocalScale)
        {
            t.localScale = geo.LocalScale;
        }

        if (t.localPosition != geo.LocalPosition)
        {
            t.localPosition = geo.LocalPosition;
        }
    }

    private static void EnsurePhoneButtonTextDrawOrder(TextMeshProUGUI label)
    {
        label.transform.SetAsLastSibling();
    }

    private static void ApplyPhoneLabelColors(TextMeshProUGUI label)
    {
        label.maskable = false;

        TMP_FontAsset? font = label.font;
        if (PhoneFontMaterialSnapshot.IsBorderFont(font))
        {
            label.color = Color.white;
            label.faceColor = Color.white;
            return;
        }

        if (IsPhoneHeaderLabel(label))
        {
            label.color = Color.white;
            label.faceColor = Color.white;
            label.outlineColor = new Color32(0, 0, 0, 255);
            label.outlineWidth = 0.18f;
            return;
        }

        if (IsPhoneButtonLabel(label))
        {
            label.color = Color.white;
            label.faceColor = Color.white;
            label.outlineColor = new Color32(0, 0, 0, 255);
            label.outlineWidth = 0.18f;
            return;
        }

        if (IsPhoneSolidButtonLabel(label))
        {
            Color32 darkGray = new Color32(35, 35, 35, 255);
            label.color = darkGray;
            label.faceColor = darkGray;
            label.outlineWidth = 0f;
            return;
        }

        if (label.color.a < 0.05f)
        {
            label.color = Color.white;
            label.faceColor = Color.white;
        }
    }

    private static bool IsPhoneSolidButtonLabel(TextMeshProUGUI label)
    {
        Transform? node = label.transform.parent;
        while (node != null)
        {
            string name = node.name;
            if (name.IndexOf("Btn", StringComparison.OrdinalIgnoreCase) >= 0
                || name.IndexOf("Button", StringComparison.OrdinalIgnoreCase) >= 0
                || name.IndexOf("Checkout", StringComparison.OrdinalIgnoreCase) >= 0
                || name.IndexOf("Cart", StringComparison.OrdinalIgnoreCase) >= 0
                || name.IndexOf("Confirm", StringComparison.OrdinalIgnoreCase) >= 0
                || name.IndexOf("Back", StringComparison.OrdinalIgnoreCase) >= 0
                || name.IndexOf("Close", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return true;
            }

            node = node.parent;
        }

        return false;
    }

    private static void SyncPhoneLabelCanvasRenderer(TextMeshProUGUI label)
    {
        CanvasRenderer? canvasRenderer = label.canvasRenderer;
        if (canvasRenderer == null)
        {
            return;
        }

        canvasRenderer.SetColor(Color.white);
        canvasRenderer.SetAlpha(1f);
        canvasRenderer.cullTransparentMesh = false;
        canvasRenderer.cull = false;

        // We are on the single-material plain SDF font now, so it is safe to force the
        // CanvasRenderer material directly. If TMP failed to push its material to the
        // CanvasRenderer, the mesh exists but draws nothing.
        Material? material = label.materialForRendering ?? label.fontMaterial;
        if (material != null)
        {
            // Only bump renderQueue on per-label PhoneLabel_ instances. Mutating the shared
            // Fredoka font.material made cash-register / HUD TMP invisible game-wide.
            bool isPhoneLabelInstance = material.name != null
                && material.name.StartsWith("PhoneLabel_", StringComparison.Ordinal);
            if (isPhoneLabelInstance)
            {
                material.renderQueue = ResolvePhoneLabelRenderQueue(label);
            }

            canvasRenderer.materialCount = 1;
            canvasRenderer.SetMaterial(material, 0);

            Texture? atlas = label.font?.atlasTexture;
            if (atlas != null)
            {
                canvasRenderer.SetTexture(atlas);
            }
        }
    }

    private const int PhoneUiDefaultImageQueue = 3005;

    private static int ResolvePhoneLabelRenderQueue(TextMeshProUGUI label)
    {
        int queue = PhoneUiDefaultImageQueue;
        Transform? cell = label.transform.parent;
        if (cell != null)
        {
            foreach (Image img in cell.GetComponentsInChildren<Image>(true))
            {
                Material? m = img.materialForRendering;
                if (m != null && m.renderQueue > queue)
                {
                    queue = m.renderQueue;
                }
            }
        }

        return queue;
    }

    private static bool EnsurePhoneLabelMaterial(TextMeshProUGUI label)
    {
        TMP_FontAsset? font = label.font;
        if (font?.atlasTexture == null || font.material == null)
        {
            return false;
        }

        // Do not RestoreSharedFontMaterial here — that raced with cash-register HUD TMP.
        Material instanceMaterial = label.fontMaterial;
        if (instanceMaterial == null || !instanceMaterial.name.StartsWith("PhoneLabel_", StringComparison.Ordinal))
        {
            instanceMaterial = PhoneFontMaterialSnapshot.CreateLabelMaterial(font);
            label.fontMaterial = instanceMaterial;
        }

        Texture atlas = font.atlasTexture;
        bool changed = false;

        if (!ReferenceEquals(instanceMaterial.mainTexture, atlas))
        {
            instanceMaterial.mainTexture = atlas;
            changed = true;
        }

        Shader? tmpShader = ResolveTmpShader();
        if (tmpShader != null && instanceMaterial.shader != tmpShader && !PhoneFontMaterialSnapshot.IsBorderFont(font))
        {
            instanceMaterial.shader = tmpShader;
            changed = true;
        }

        if (instanceMaterial.HasProperty("_FaceColor"))
        {
            Color faceColor = instanceMaterial.GetColor("_FaceColor");
            if (faceColor.a < 0.05f)
            {
                instanceMaterial.SetColor("_FaceColor", Color.white);
                changed = true;
            }
        }

        label.SetMaterialDirty();
        return changed;
    }

    private static bool IsFontAtlasTexture(Texture? texture)
    {
        if (texture == null)
        {
            return false;
        }

        string textureName = texture.name ?? string.Empty;
        return textureName.IndexOf("Atlas", StringComparison.OrdinalIgnoreCase) >= 0
            || textureName.IndexOf("SDF", StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private static bool IsPhoneLabelAtlasBound(TextMeshProUGUI label)
    {
        Texture? atlas = label.font?.atlasTexture;
        return atlas != null && ReferenceEquals(label.fontMaterial.mainTexture, atlas);
    }

    private static void RefreshPhoneLabelsInRoot(Transform root)
    {
        bool rebuiltAny = false;
        foreach (TextMeshProUGUI label in root.GetComponentsInChildren<TextMeshProUGUI>(true))
        {
            if (label != null && label.gameObject.activeInHierarchy)
            {
                if (RefreshPhoneLabelMesh(label))
                {
                    rebuiltAny = true;
                }
            }
        }

        if (rebuiltAny)
        {
            Canvas.ForceUpdateCanvases();
        }
    }

    private static bool IsPhoneWorldScale(TextMeshProUGUI label)
    {
        Vector3 lossyScale = label.transform.lossyScale;
        float scale = Mathf.Max(Mathf.Abs(lossyScale.x), Mathf.Abs(lossyScale.y), Mathf.Abs(lossyScale.z));
        return scale >= PhoneWorldScaleMin && scale <= PhoneWorldScaleMax;
    }

    private static bool ShouldRebuildPhoneLabelMesh(TextMeshProUGUI label)
    {
        if (!label.gameObject.activeInHierarchy)
        {
            return false;
        }

        if (label.fontSize > PhoneWorldMaxFontSize)
        {
            return false;
        }

        Canvas? canvas = label.canvas;
        if (canvas == null || !canvas.isActiveAndEnabled || canvas.renderMode != RenderMode.WorldSpace)
        {
            return false;
        }

        return IsPhoneWorldScale(label);
    }

    private static bool RefreshPhoneLabelMesh(TextMeshProUGUI label)
    {
        if (label == null || !label.gameObject.activeInHierarchy)
        {
            return false;
        }

        label.SetAllDirty();

        if (!ShouldRebuildPhoneLabelMesh(label))
        {
            return false;
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(label.rectTransform);
        label.ForceMeshUpdate(true, false);
        return label.mesh != null && label.mesh.vertexCount > 0;
    }

    private static void RepairHudTmp(TextMeshProUGUI label)
    {
        if (label == null)
        {
            return;
        }

        ApplyHudFontSize(label);
        ApplyReadableColors(label);
        label.enabled = true;
        label.SetAllDirty();
    }

    private static void RepairScreenTmp(TextMeshProUGUI label)
    {
        if (label == null)
        {
            return;
        }

        if (IsPhoneAppLabel(label))
        {
            RepairPhoneWorldLabel(label);
            return;
        }

        RepairHudTmp(label);
    }

    private static void RepairPhoneFontBinding(TextMeshProUGUI label)
    {
        if (label == null)
        {
            return;
        }

        if (label.font != null
            && label.font.atlasTexture != null
            && label.font.name.StartsWith("FredokaOne", StringComparison.Ordinal))
        {
            RepairFontMaterial(label.font);
            EnsureLabelMaterialUsesFontAtlas(label);
            return;
        }

        TMP_FontAsset? font = ResolvePhoneFont();
        if (font != null)
        {
            label.font = font;
            RepairFontMaterial(font);
            EnsureLabelMaterialUsesFontAtlas(label);
        }
    }

    private static void EnsureLabelMaterialUsesFontAtlas(TextMeshProUGUI label)
    {
        EnsurePhoneLabelMaterial(label);
    }

    private const float PhoneWorldMaxFontSize = 8.5f;
    private const float PhoneWorldScaleMin = 0.00005f;
    private const float PhoneWorldScaleMax = 0.05f;

    private static void ApplyPhoneWorldFontSizing(TextMeshProUGUI label)
    {
        // Every phone label (home headers/buttons AND sub-screen text) already carries the correct
        // vanilla font size / scale / position in the prefab. Only the material/font was ever broken,
        // so we just preserve the prefab geometry instead of auto-sizing (which shrinks sub-screen
        // text to sub-pixel size).
        RestorePhoneLabelGeometry(label);
    }

    private static float GetRectHeight(RectTransform? rect)
    {
        return rect == null ? 0f : Mathf.Abs(rect.rect.height);
    }

    private static float GetButtonIconHeight(TextMeshProUGUI label)
    {
        Transform? cell = label.transform.parent;
        if (cell == null)
        {
            return 0f;
        }

        foreach (string childName in new[] { "Icon", "BG", "BtnInteraction" })
        {
            Transform? child = cell.Find(childName);
            if (child is RectTransform childRect)
            {
                float height = Mathf.Abs(childRect.rect.height);
                if (height > 1f)
                {
                    return height;
                }
            }
        }

        return GetRectHeight(cell as RectTransform);
    }

    private static void ApplyHudFontSize(TextMeshProUGUI label)
    {
        if (IsPhoneAppLabel(label))
        {
            ApplyPhoneWorldFontSizing(label);
            return;
        }

        if (label.fontSize < 8f)
        {
            if (label.enableAutoSizing)
            {
                label.fontSizeMin = Mathf.Max(label.fontSizeMin, 10f);
                label.fontSizeMax = Mathf.Max(label.fontSizeMax, 24f);
                label.fontSize = Mathf.Max(label.fontSize, 12f);
            }
            else
            {
                label.fontSize = 14f;
            }
        }
    }

    private static bool IsPhoneButtonLabel(TextMeshProUGUI label)
    {
        Transform? parent = label.transform.parent;
        return label.name == "Text"
            && parent != null
            && parent.name.StartsWith("PhoneButtonGrp_", StringComparison.Ordinal);
    }

    private static bool IsPhoneHeaderLabel(TextMeshProUGUI label)
    {
        return label.name is "MobileProviderText"
            or "TimeText"
            or "DayText";
    }

    private static void ApplyReadableColors(TextMeshProUGUI label)
    {
        Color color = label.color;
        if (color.a < 0.05f || (color.r < 0.05f && color.g < 0.05f && color.b < 0.05f))
        {
            label.color = Color.white;
        }

        label.faceColor = new Color32(255, 255, 255, 255);

        if (IsPhoneAppLabel(label) || IsPhoneButtonLabel(label) || IsPhoneHeaderLabel(label))
        {
            if (!PhoneFontMaterialSnapshot.IsBorderFont(label.font))
            {
                label.outlineColor = new Color32(0, 0, 0, 255);
                label.outlineWidth = Mathf.Max(label.outlineWidth, 0.1f);
            }
        }
    }

    private static void RepairFontMaterial(TMP_FontAsset font)
    {
        if (font?.material == null || font.atlasTexture == null)
        {
            return;
        }

        if (!ReferenceEquals(font.material.mainTexture, font.atlasTexture))
        {
            font.material.mainTexture = font.atlasTexture;
        }

        if (font.material.HasProperty("_FaceColor"))
        {
            Color faceColor = font.material.GetColor("_FaceColor");
            if (faceColor.a < 0.05f)
            {
                font.material.SetColor("_FaceColor", Color.white);
            }
        }
    }

    private static Shader? ResolveTmpShader()
    {
        foreach (string shaderName in TmpShaderNames)
        {
            Shader? shader = Shader.Find(shaderName);
            if (shader != null)
            {
                return shader;
            }
        }

        return null;
    }

    private static void EnsureFontsCached()
    {
        if (fontsCached)
        {
            return;
        }

        PhoneFontMaterialSnapshot.CaptureIfNeeded();
        TMP_FontAsset[] fonts = Resources.FindObjectsOfTypeAll<TMP_FontAsset>();
        foreach (TMP_FontAsset font in fonts)
        {
            if (font == null || string.IsNullOrEmpty(font.name))
            {
                continue;
            }

            if (!font.name.StartsWith("FredokaOne", StringComparison.Ordinal)
                && !font.name.StartsWith("LiberationSans", StringComparison.Ordinal))
            {
                continue;
            }

            RepairFontMaterial(font);
            PhoneFontMaterialSnapshot.RestoreSharedFontMaterial(font);
            CachedFredokaFonts[font.name] = font;
        }

        fontsCached = true;
    }

    private static TMP_FontAsset? ResolvePhoneFont()
    {
        EnsureFontsCached();

        // Prefer the plain SDF variant: TextureReplacer's own custom UI uses this exact font and
        // it renders correctly in-game, whereas the "border2" variant material renders invisible
        // under this mod stack despite a fully valid mesh/atlas/shader.
        if (CachedFredokaFonts.TryGetValue("FredokaOne-Regular SDF", out TMP_FontAsset regular))
        {
            return regular;
        }

        if (CachedFredokaFonts.TryGetValue("LiberationSans SDF", out TMP_FontAsset liberation))
        {
            return liberation;
        }

        if (CachedFredokaFonts.TryGetValue("FredokaOne-Regular SDF border2", out TMP_FontAsset border2))
        {
            return border2;
        }

        if (CachedFredokaFonts.TryGetValue("FredokaOne-Regular SDF border", out TMP_FontAsset border))
        {
            return border;
        }

        return null;
    }

    private static TMP_FontAsset? ResolveFredokaFont(TMP_FontAsset? current)
    {
        TMP_FontAsset? phoneFont = ResolvePhoneFont();
        return phoneFont ?? current;
    }

    public static void RepairFredokaFontAssetsPublic()
    {
        RepairFredokaFontAssets();
    }

    private static void RepairFredokaFontAssets()
    {
        PhoneFontMaterialSnapshot.CaptureIfNeeded();
        TMP_FontAsset[] fonts = Resources.FindObjectsOfTypeAll<TMP_FontAsset>();
        foreach (TMP_FontAsset font in fonts)
        {
            if (font == null || string.IsNullOrEmpty(font.name))
            {
                continue;
            }

            if (!font.name.StartsWith("FredokaOne", StringComparison.Ordinal)
                && !font.name.StartsWith("LiberationSans", StringComparison.Ordinal))
            {
                continue;
            }

            RepairFontMaterial(font);
            PhoneFontMaterialSnapshot.RestoreSharedFontMaterial(font);
            CachedFredokaFonts[font.name] = font;
        }

        fontsCached = true;
    }
}
