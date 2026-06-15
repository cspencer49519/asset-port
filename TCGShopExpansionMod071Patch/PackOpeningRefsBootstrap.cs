using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace TCGShopExpansionMod071Patch;

/// <summary>
/// Ported sharedassets can leave CardOpeningSequence inspector refs null while the scene objects still exist elsewhere.
/// </summary>
internal static class PackOpeningRefsBootstrap
{
    private const int CardDataPoolSize = 10;

    private static bool _loggedBootstrap;
    private static bool _loggedDiagnostics;
    private static bool _loggedPoolRepair;

    public static void TryBootstrap(CardOpeningSequence sequence)
    {
        if (sequence == null)
        {
            return;
        }

        bool repaired = false;

        repaired |= TryBootstrapFromOpeningUiAnchor(sequence);
        repaired |= TryBootstrapFromLocalHierarchy(sequence);
        if (!HasMinimumPackOpenRefs(sequence) || !HasOpenSequenceUiRefs(sequence))
        {
            repaired |= TryBootstrapFromSceneSearch(sequence);
        }

        repaired |= TryBootstrapCardLists(sequence);
        repaired |= TryBootstrapAnimationAndIcons(sequence);
        repaired |= TryBootstrapOpenPackVfx(sequence);
        repaired |= EnsureCardOpeningUiGroup(sequence);
        EnsurePackOpenFeedbackIcons(sequence);
        TryEnsureCardDataPools(sequence);

        if (repaired && !_loggedBootstrap)
        {
            _loggedBootstrap = true;
            Plugin.Log.LogWarning(
                "Repaired missing CardOpeningSequence pack references from scene search (sharedassets port).");
        }

        if ((!HasMinimumPackOpenRefs(sequence) || !HasOpenSequenceUiRefs(sequence)) && !_loggedDiagnostics)
        {
            _loggedDiagnostics = true;
            LogDiagnostics(sequence);
        }
    }

    public static bool HasMinimumPackOpenRefs(CardOpeningSequence sequence)
    {
        return sequence.m_CardPackAnimator != null
            && sequence.m_CardPackMesh != null
            && sequence.m_StartLerpTransform != null;
    }

    public static bool HasOpenSequenceUiRefs(CardOpeningSequence sequence)
    {
        return sequence.m_CardOpeningUIGroup != null
            && sequence.m_CardOpeningSequenceUI != null
            && sequence.m_CardOpeningRotateToFrontAnim != null
            && sequence.m_Card3dUIList != null
            && sequence.m_Card3dUIList.Count > 0
            && sequence.m_CardAnimList != null
            && sequence.m_CardAnimList.Count > 0;
    }

    /// <summary>Vanilla Start() fills m_CardDataPool before any pack open; a Start NRE skips that and GetPackContent crashes.</summary>
    public static void TryEnsureCardDataPools(CardOpeningSequence sequence)
    {
        bool repaired = EnsureCardDataPool(sequence, "m_CardDataPool")
            | EnsureCardDataPool(sequence, "m_CardDataPool2");

        if (repaired && !_loggedPoolRepair)
        {
            _loggedPoolRepair = true;
            Plugin.Log.LogWarning(
                "Repaired empty CardOpeningSequence card data pools after Start() failure (sharedassets port).");
        }
    }

    private static bool _loggedPackOpenReadiness;

    public static bool EnsureCardOpeningUiGroup(CardOpeningSequence sequence)
    {
        if (sequence.m_CardOpeningUIGroup != null)
        {
            return false;
        }

        if (sequence.m_Card3dUIList != null && sequence.m_Card3dUIList.Count > 0)
        {
            Transform? parent = sequence.m_Card3dUIList[0].transform.parent;
            if (parent != null)
            {
                sequence.m_CardOpeningUIGroup = parent.gameObject;
                return true;
            }
        }

        if (sequence.m_CardOpeningSequenceUI != null)
        {
            Transform uiTransform = sequence.m_CardOpeningSequenceUI.transform;
            sequence.m_CardOpeningUIGroup = uiTransform.parent != null
                ? uiTransform.parent.gameObject
                : uiTransform.gameObject;
            return true;
        }

        return false;
    }

    /// <summary>Complete ReadyingCardPack when vanilla would NRE on missing m_CardOpeningUIGroup.</summary>
    public static bool TryRunReadyingCardPack(CardOpeningSequence sequence, Item item)
    {
        EnsureCardOpeningUiGroup(sequence);

        if (!HasMinimumPackOpenRefs(sequence) || sequence.m_CardOpeningUIGroup == null || item == null)
        {
            return false;
        }

        if (CardOpeningSequenceFieldAccess.GetValue(sequence, "m_IsReadyingToOpen") is true)
        {
            return true;
        }

        CardOpeningSequenceFieldAccess.SetValue(sequence, "m_IsScreenActive", true);
        CSingleton<InteractionPlayerController>.Instance.EnterLockMoveMode();
        CSingleton<InteractionPlayerController>.Instance.OnEnterOpenPackState();
        CardOpeningSequenceFieldAccess.SetValue(sequence, "m_IsReadyingToOpen", true);
        CardOpeningSequenceFieldAccess.SetValue(sequence, "m_IsReadyToOpen", false);
        CardOpeningSequenceFieldAccess.SetValue(sequence, "m_LerpPosTimer", 0f);
        CardOpeningSequenceFieldAccess.SetValue(sequence, "m_CurrentItem", item);

        sequence.m_CardPackAnimator.transform.position = sequence.m_StartLerpTransform.position;
        sequence.m_CardPackAnimator.transform.rotation = sequence.m_StartLerpTransform.rotation;
        sequence.m_CardPackAnimator.transform.localScale = sequence.m_StartLerpTransform.localScale;

        if (item.m_Mesh != null)
        {
            sequence.m_CardPackMesh.material = item.m_Mesh.sharedMaterial;
        }

        sequence.m_CardPackAnimator.gameObject.SetActive(true);
        item.gameObject.SetActive(false);
        sequence.m_CardOpeningUIGroup.SetActive(false);
        sequence.m_CardPackAnimator.Play("PackOpenAnim", -1, 0f);
        CSingleton<InteractionPlayerController>.Instance.m_BlackBGWorldUIFade.SetFadeIn(3f);
        TutorialManager.SetGameUIVisible(isVisible: false);
        CenterDot.SetVisibility(isVisible: false);
        GameUIScreen.HideEnterGoNextDayIndicatorVisible();
        InteractionPlayerController.TempHideToolTip();
        InteractionPlayerController.AddToolTip(EGameAction.OpenPack, isHold: true);
        InteractionPlayerController.AddToolTip(EGameAction.CancelOpenPack);
        InteractionPlayerController.SetAllHoldItemVisibility(isVisible: false);
        CSingleton<InteractionPlayerController>.Instance.m_CameraFOVController.StartLerpToFOV(40f);
        SoundManager.GenericPop();

        return true;
    }

    /// <summary>Safe replacement for vanilla InitOpenSequence when inspector refs were broken by sharedassets port.</summary>
    public static bool TryRunInitOpenSequence(CardOpeningSequence sequence)
    {
        EnsureCardOpeningUiGroup(sequence);

        if (sequence.m_CardPackAnimator == null
            || sequence.m_CardOpeningUIGroup == null
            || sequence.m_Card3dUIList == null
            || sequence.m_Card3dUIList.Count == 0)
        {
            return false;
        }

        sequence.m_CardPackAnimator.speed = 0f;
        sequence.m_CardPackAnimator.gameObject.SetActive(true);
        sequence.m_CardPackAnimator.Play("PackOpenAnim", -1, 0f);

        if (sequence.m_CardOpeningRotateToFrontAnim != null)
        {
            sequence.m_CardOpeningRotateToFrontAnim.Play("CardOpenSeq0_Idle");
        }

        sequence.m_CardOpeningUIGroup.SetActive(true);

        if (sequence.m_NewCardIcon != null)
        {
            sequence.m_NewCardIcon.SetActive(false);
        }

        if (sequence.m_HighValueCardIcon != null)
        {
            sequence.m_HighValueCardIcon.SetActive(false);
        }

        InteractionPlayerController.RemoveToolTip(EGameAction.CancelOpenPack);
        InteractionPlayerController.RemoveToolTip(EGameAction.OpenPack);
        InteractionPlayerController.AddToolTip(EGameAction.OpenPack, isHold: true);

        if (CardOpeningSequenceFieldAccess.GetValue(sequence, "m_MultiplierStateTimer") is float multiplier)
        {
            CGameManager? gameManager = CSingleton<CGameManager>.Instance;
            float speedSlider = gameManager != null ? gameManager.m_OpenPackSpeedSlider : 0f;
            CardOpeningSequenceFieldAccess.SetValue(sequence, "m_MultiplierStateTimer", 1f + 2.5f * speedSlider);
        }

        CardOpeningSequenceFieldAccess.SetValue(
            sequence,
            "m_HighValueCardThreshold",
            10f + CPlayerData.m_ShopLevel / 5f * 2f);

        return true;
    }

    public static void LogPackOpenReadiness(CardOpeningSequence sequence)
    {
        if (_loggedPackOpenReadiness)
        {
            return;
        }

        _loggedPackOpenReadiness = true;
        string rotatePath = sequence.m_CardOpeningRotateToFrontAnim != null
            ? GetHierarchyPath(sequence.m_CardOpeningRotateToFrontAnim.transform)
            : "null";
        Plugin.Log.LogWarning(
            "Pack open readiness: " +
            $"uiGroup={sequence.m_CardOpeningUIGroup != null}, rotateAnim={sequence.m_CardOpeningRotateToFrontAnim != null}, " +
            $"rotatePath={rotatePath}, newCardIcon={sequence.m_NewCardIcon != null}, highValueIcon={sequence.m_HighValueCardIcon != null}, " +
            $"card3d={sequence.m_Card3dUIList?.Count ?? 0}, cardAnim={sequence.m_CardAnimList?.Count ?? 0}, " +
            $"cardAnimClips={(sequence.m_CardAnimList != null && CardAnimListHasOpenCardClips(sequence.m_CardAnimList))}, " +
            $"showAllPos={sequence.m_ShowAllCardPosList?.Count ?? 0}, openVfx={sequence.m_OpenPackVFX != null}, " +
            $"sequenceUi={sequence.m_CardOpeningSequenceUI != null}, " +
            $"enableTooltip={CSingleton<CGameManager>.Instance?.m_EnableTooltip}");
    }

    private static bool _loggedCardStackDiagnostics;
    private static bool _loggedFanDiagnostics;

    /// <summary>One-time dump of the reconstructed card order vs physical stack so ordering bugs are visible.</summary>
    public static void DumpCardStackDiagnostics(CardOpeningSequence sequence)
    {
        if (_loggedCardStackDiagnostics || sequence.m_StateIndex < 2)
        {
            return;
        }

        if (sequence.m_Card3dUIList == null || sequence.m_Card3dUIList.Count == 0)
        {
            return;
        }

        _loggedCardStackDiagnostics = true;

        Camera? cam = CSingleton<InteractionPlayerController>.Instance?.m_Cam;
        Vector3 camPos = cam != null ? cam.transform.position : Vector3.zero;
        Vector3 camForward = cam != null ? cam.transform.forward : Vector3.forward;

        Plugin.Log.LogWarning($"CardStack dump at state={sequence.m_StateIndex}");

        for (int i = 0; i < sequence.m_Card3dUIList.Count; i++)
        {
            Card3dUIGroup? card3d = sequence.m_Card3dUIList[i];
            if (card3d == null)
            {
                Plugin.Log.LogWarning($"CardStack[{i}] = null");
                continue;
            }

            Transform t = card3d.transform;
            float camDist = cam != null ? Vector3.Distance(t.position, camPos) : -1f;
            // facing > 0 means the card's front (+forward) points the same way the camera looks => front toward camera.
            float facing = Vector3.Dot(t.forward, camForward);

            CardUI? cardUi = card3d.m_CardUI;
            bool frontActive = cardUi?.m_CardFront != null && cardUi.m_CardFront.activeSelf;
            bool backActive = cardUi?.m_CardBack != null && cardUi.m_CardBack.activeSelf;
            bool backImgEnabled = cardUi?.m_CardBackImage != null && cardUi.m_CardBackImage.enabled;
            string backSprite = cardUi?.m_CardBackImage?.sprite != null ? cardUi.m_CardBackImage.sprite.name : "null";
            bool backMeshActive = card3d.m_CardBackMesh != null && card3d.m_CardBackMesh.activeSelf;

            Plugin.Log.LogWarning(
                $"CardStack[{i}] name={t.name} sib={t.GetSiblingIndex()} active={card3d.gameObject.activeSelf} " +
                $"camDist={camDist:F3} facing={facing:F2} frontActive={frontActive} backActive={backActive} " +
                $"backImgEnabled={backImgEnabled} backSprite={backSprite} backMeshActive={backMeshActive}");
        }
    }

    /// <summary>Fan-phase (state &gt;= 7) diagnostics: card world depth vs camera and the darkening overlay setup.</summary>
    public static void DumpFanDiagnostics(CardOpeningSequence sequence)
    {
        if (_loggedFanDiagnostics || sequence.m_StateIndex < 7)
        {
            return;
        }

        if (sequence.m_Card3dUIList == null || sequence.m_Card3dUIList.Count == 0)
        {
            return;
        }

        _loggedFanDiagnostics = true;

        Camera? cam = CSingleton<InteractionPlayerController>.Instance?.m_Cam;
        Vector3 camPos = cam != null ? cam.transform.position : Vector3.zero;
        Vector3 camForward = cam != null ? cam.transform.forward : Vector3.forward;

        Plugin.Log.LogWarning($"FanDump at state={sequence.m_StateIndex} camPos={camPos} camForward={camForward}");

        for (int i = 0; i < sequence.m_Card3dUIList.Count; i++)
        {
            Card3dUIGroup? card3d = sequence.m_Card3dUIList[i];
            if (card3d == null)
            {
                continue;
            }

            Transform t = card3d.transform;
            // Depth along the camera's view direction: how far in front of the camera the card sits.
            float depth = Vector3.Dot(t.position - camPos, camForward.normalized);
            float camDist = Vector3.Distance(t.position, camPos);

            Canvas? canvas = card3d.m_CardUI != null ? card3d.m_CardUI.GetComponentInParent<Canvas>() : null;
            string canvasInfo = canvas != null
                ? $"canvas={canvas.name} mode={canvas.renderMode} order={canvas.sortingOrder} override={canvas.overrideSorting}"
                : "canvas=null";

            // Render queue of the front image's material — what actually orders it vs the backdrop mesh.
            int frontQueue = -1;
            Canvas? frontCanvas = null;
            if (card3d.m_CardUI != null && card3d.m_CardUI.m_CardFrontImage != null)
            {
                Material? fm = card3d.m_CardUI.m_CardFrontImage.materialForRendering;
                frontQueue = fm != null ? fm.renderQueue : -1;
                frontCanvas = card3d.m_CardUI.m_CardFrontImage.canvas;
            }
            string frontCanvasInfo = frontCanvas != null
                ? $"frontCanvas={frontCanvas.name} fOrder={frontCanvas.sortingOrder} fOverride={frontCanvas.overrideSorting}"
                : "frontCanvas=null";

            Plugin.Log.LogWarning(
                $"FanCard[{i}] name={t.name} active={card3d.gameObject.activeSelf} worldPos={t.position} " +
                $"depth={depth:F3} camDist={camDist:F3} frontQueue={frontQueue} {canvasInfo} {frontCanvasInfo}");
        }

        // Dump the fan target positions so we can compare their depth to the cards.
        if (sequence.m_ShowAllCardPosList != null)
        {
            for (int i = 0; i < sequence.m_ShowAllCardPosList.Count; i++)
            {
                Transform? pos = sequence.m_ShowAllCardPosList[i];
                if (pos == null)
                {
                    continue;
                }

                float depth = Vector3.Dot(pos.position - camPos, camForward.normalized);
                Plugin.Log.LogWarning($"FanPos[{i}] name={pos.name} worldPos={pos.position} depth={depth:F3}");
            }
        }

        DumpCardOpeningUiOverlay(sequence, cam, camPos, camForward);
        DumpBlackBgBackdrop(cam, camPos, camForward);
    }

    private static void DumpBlackBgBackdrop(Camera? cam, Vector3 camPos, Vector3 camForward)
    {
        InteractionPlayerController? ipc = CSingleton<InteractionPlayerController>.Instance;
        MaterialFadeInOut? fade = ipc != null ? ipc.m_BlackBGWorldUIFade : null;
        if (fade == null)
        {
            Plugin.Log.LogWarning("Backdrop: m_BlackBGWorldUIFade is NULL");
            return;
        }

        MeshRenderer? mesh = fade.m_Mesh;
        if (mesh == null)
        {
            Plugin.Log.LogWarning("Backdrop: m_BlackBGWorldUIFade.m_Mesh is NULL");
            return;
        }

        Material? mat = mesh.material;
        Color col = mat != null ? mat.color : new Color(-1f, -1f, -1f, -1f);
        Transform t = mesh.transform;
        float depth = cam != null ? Vector3.Dot(t.position - camPos, camForward.normalized) : -1f;
        int backdropQueue = mat != null ? mat.renderQueue : -1;
        Plugin.Log.LogWarning(
            $"Backdrop: meshEnabled={mesh.enabled} active={mesh.gameObject.activeInHierarchy} matColor={col} " +
            $"matAlpha={col.a:F3} worldPos={t.position} depth={depth:F3} scale={t.lossyScale} renderQueue={backdropQueue} " +
            $"sortLayer={mesh.sortingLayerID} sortOrder={mesh.sortingOrder} " +
            $"matName={(mat != null ? mat.name : "null")} srcMat={(fade.m_Mat != null ? fade.m_Mat.name : "null")}");
    }

    private static void DumpCardOpeningUiOverlay(CardOpeningSequence sequence, Camera? cam, Vector3 camPos, Vector3 camForward)
    {
        // Scene-wide scan: the fullscreen darkening overlay is NOT under m_CardOpeningUIGroup, so look at every
        // active Image and report ones that look like a fullscreen dark/opaque backdrop (stretched or huge or dark+big).
        Image[] images = UnityEngine.Object.FindObjectsOfType<Image>(includeInactive: false);
        Plugin.Log.LogWarning($"FanOverlay: scene-wide scan of {images.Length} active Image(s)");

        int reported = 0;
        for (int i = 0; i < images.Length && reported < 30; i++)
        {
            Image img = images[i];
            if (img == null || !img.enabled)
            {
                continue;
            }

            RectTransform rt = img.rectTransform;
            Rect worldRect = GetWorldRectApprox(rt);
            bool fullscreenish = worldRect.width > 800f && worldRect.height > 600f;
            bool darkOpaque = img.color.a > 0.4f && img.color.r < 0.4f && img.color.g < 0.4f && img.color.b < 0.4f;
            if (!fullscreenish && !darkOpaque)
            {
                continue;
            }

            Canvas? c = img.canvas;
            string canvasInfo = c != null ? $"{c.name}/{c.renderMode}/order={c.sortingOrder}" : "null";
            float depth = cam != null ? Vector3.Dot(img.transform.position - camPos, camForward.normalized) : -1f;
            Plugin.Log.LogWarning(
                $"FanOverlayImg name={img.name} path={GetHierarchyPath(img.transform)} active={img.gameObject.activeSelf} " +
                $"color={img.color} screenRect={worldRect.width:F0}x{worldRect.height:F0} depth={depth:F3} canvas={canvasInfo}");
            reported++;
        }
    }

    private static Rect GetWorldRectApprox(RectTransform rt)
    {
        Vector3[] corners = new Vector3[4];
        rt.GetWorldCorners(corners);
        Camera? cam = CSingleton<InteractionPlayerController>.Instance?.m_Cam;
        if (cam == null)
        {
            float w = Vector3.Distance(corners[0], corners[3]);
            float h = Vector3.Distance(corners[0], corners[1]);
            return new Rect(0f, 0f, w, h);
        }

        Vector3 bl = cam.WorldToScreenPoint(corners[0]);
        Vector3 tr = cam.WorldToScreenPoint(corners[2]);
        return new Rect(bl.x, bl.y, Mathf.Abs(tr.x - bl.x), Mathf.Abs(tr.y - bl.y));
    }

    /// <summary>Vanilla Update NREs on null NewCardIcon/HighValueCardIcon during flip states.</summary>
    public static void EnsurePackOpenFeedbackIcons(CardOpeningSequence sequence)
    {
        Transform parent = sequence.m_CardOpeningUIGroup != null
            ? sequence.m_CardOpeningUIGroup.transform
            : sequence.transform;

        if (sequence.m_NewCardIcon == null)
        {
            sequence.m_NewCardIcon = FindSceneGameObjectByName("NewCardIcon")
                ?? EnsureRuntimeStubObject(parent, "NewCardIcon071Bootstrap");
        }

        if (sequence.m_HighValueCardIcon == null)
        {
            sequence.m_HighValueCardIcon = FindSceneGameObjectByName("HighValueCardIcon")
                ?? EnsureRuntimeStubObject(parent, "HighValueCardIcon071Bootstrap");
        }

        sequence.m_NewCardIcon.SetActive(false);
        sequence.m_HighValueCardIcon.SetActive(false);
    }

    private static GameObject EnsureRuntimeStubObject(Transform parent, string name)
    {
        Transform? existing = parent.Find(name);
        if (existing != null)
        {
            return existing.gameObject;
        }

        GameObject stub = new(name);
        stub.transform.SetParent(parent, worldPositionStays: false);
        stub.SetActive(false);
        return stub;
    }

    private static bool TryBootstrapFromOpeningUiAnchor(CardOpeningSequence sequence)
    {
        CardOpeningSequenceUI[] uiComponents = FindSceneComponents<CardOpeningSequenceUI>();
        if (uiComponents.Length == 0)
        {
            return false;
        }

        CardOpeningSequenceUI? bestUi = null;
        int bestScore = int.MinValue;
        for (int i = 0; i < uiComponents.Length; i++)
        {
            CardOpeningSequenceUI candidate = uiComponents[i];
            if (candidate == null)
            {
                continue;
            }

            int score = 100 - (int)Vector3.Distance(candidate.transform.position, sequence.transform.position);
            Card3dUIGroup[] nearbyCards = candidate.GetComponentsInChildren<Card3dUIGroup>(includeInactive: true);
            score += nearbyCards.Length * 10;
            if (score > bestScore)
            {
                bestScore = score;
                bestUi = candidate;
            }
        }

        if (bestUi == null)
        {
            return false;
        }

        bool repaired = false;
        if (sequence.m_CardOpeningSequenceUI == null)
        {
            sequence.m_CardOpeningSequenceUI = bestUi;
            repaired = true;
        }

        Transform? bestRoot = null;
        int bestCardCount = 0;
        Transform? current = bestUi.transform;
        for (int depth = 0; depth < 10 && current != null; depth++)
        {
            Card3dUIGroup[] groups = current.GetComponentsInChildren<Card3dUIGroup>(includeInactive: true);
            if (groups.Length > bestCardCount)
            {
                bestCardCount = groups.Length;
                bestRoot = current;
            }

            current = current.parent;
        }

        if (bestRoot != null)
        {
            repaired |= TryAssignFromTransformTree(bestRoot, sequence);
        }

        return repaired;
    }

    private static bool TryBootstrapOpenPackVfx(CardOpeningSequence sequence)
    {
        if (sequence.m_OpenPackVFX != null)
        {
            return false;
        }

        ParticleSystem[] particleSystems = FindSceneComponents<ParticleSystem>();
        ParticleSystem? best = null;
        int bestScore = int.MinValue;

        for (int i = 0; i < particleSystems.Length; i++)
        {
            ParticleSystem candidate = particleSystems[i];
            if (candidate == null)
            {
                continue;
            }

            int score = 0;
            string path = GetHierarchyPath(candidate.transform);
            if (path.IndexOf("OpenPack", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                score += 80;
            }

            if (path.IndexOf("CardOpening", StringComparison.OrdinalIgnoreCase) >= 0
                || path.IndexOf("PackOpen", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                score += 40;
            }

            float distance = Vector3.Distance(candidate.transform.position, sequence.transform.position);
            if (distance < 25f)
            {
                score += Math.Max(0, 20 - (int)distance);
            }

            if (score > bestScore)
            {
                bestScore = score;
                best = candidate;
            }
        }

        if (best != null && bestScore > 0)
        {
            sequence.m_OpenPackVFX = best;
            return true;
        }

        return false;
    }

    private static bool EnsureCardDataPool(CardOpeningSequence sequence, string fieldName)
    {
        if (CardOpeningSequenceFieldAccess.GetValue(sequence, fieldName) is not List<CardData> pool)
        {
            return false;
        }

        if (pool.Count >= CardDataPoolSize)
        {
            return false;
        }

        while (pool.Count < CardDataPoolSize)
        {
            pool.Add(new CardData());
        }

        return true;
    }

    /// <summary>Run Start() steps that may have been skipped when vanilla Start threw on null refs.</summary>
    public static void TryRecoverStart(CardOpeningSequence sequence)
    {
        TryEnsureCardDataPools(sequence);

        if (sequence.m_CardPackAnimator == null || sequence.m_CardOpeningUIGroup == null)
        {
            return;
        }

        try
        {
            sequence.m_CardPackAnimator.gameObject.SetActive(false);
            sequence.m_CardPackAnimator.speed = 0f;
            sequence.m_CardOpeningUIGroup.SetActive(false);

            if (sequence.m_NewCardIcon != null)
            {
                sequence.m_NewCardIcon.SetActive(false);
            }

            if (sequence.m_HighValueCardIcon != null)
            {
                sequence.m_HighValueCardIcon.SetActive(false);
            }

            if (sequence.m_CardOpeningSequenceUI != null)
            {
                if (sequence.m_CardOpeningSequenceUI.m_CardValueTextGrp != null)
                {
                    sequence.m_CardOpeningSequenceUI.m_CardValueTextGrp.SetActive(false);
                }

                if (sequence.m_CardOpeningSequenceUI.m_TotalCardValueTextGrp != null)
                {
                    sequence.m_CardOpeningSequenceUI.m_TotalCardValueTextGrp.SetActive(false);
                }

                if (sequence.m_CardOpeningSequenceUI.m_FoilRainbowGlowingBG != null)
                {
                    sequence.m_CardOpeningSequenceUI.m_FoilRainbowGlowingBG.SetActive(false);
                }
            }
        }
        catch (Exception ex)
        {
            Plugin.Log.LogWarning($"CardOpeningSequence start recovery failed: {ex.GetType().Name}: {ex.Message}");
        }
    }

    private static bool TryBootstrapFromLocalHierarchy(CardOpeningSequence sequence)
    {
        bool repaired = false;

        Transform[] searchRoots =
        {
            sequence.transform,
            sequence.transform.parent,
        };

        for (int r = 0; r < searchRoots.Length; r++)
        {
            Transform? root = searchRoots[r];
            if (root == null)
            {
                continue;
            }

            repaired |= TryAssignFromTransformTree(root, sequence);
            if (HasMinimumPackOpenRefs(sequence))
            {
                return repaired;
            }
        }

        return repaired;
    }

    private static bool TryBootstrapFromSceneSearch(CardOpeningSequence sequence)
    {
        bool repaired = false;

        if (sequence.m_CardOpeningSequenceUI == null)
        {
            CardOpeningSequenceUI[] uiComponents = FindSceneComponents<CardOpeningSequenceUI>();
            if (uiComponents.Length > 0)
            {
                sequence.m_CardOpeningSequenceUI = uiComponents[0];
                repaired = true;
            }
        }

        if (sequence.m_CardOpeningUIGroup == null)
        {
            Transform? uiGroup = FindSceneTransformByName("CardOpeningUIGroup");
            if (uiGroup != null)
            {
                sequence.m_CardOpeningUIGroup = uiGroup.gameObject;
                repaired = true;
            }
        }

        if (sequence.m_StartLerpTransform == null)
        {
            sequence.m_StartLerpTransform = FindSceneTransformByName("StartLerpTransform")
                ?? FindSceneTransformByName("StartLerpPos")
                ?? FindSceneTransformByName("PackStartLerpTransform");
            if (sequence.m_StartLerpTransform != null)
            {
                repaired = true;
            }
        }

        if (sequence.m_CardPackAnimator == null)
        {
            Animator[] animators = FindSceneComponents<Animator>();
            Animator? best = null;
            int bestScore = int.MinValue;

            for (int i = 0; i < animators.Length; i++)
            {
                Animator candidate = animators[i];
                if (candidate == null)
                {
                    continue;
                }

                int score = ScorePackAnimatorCandidate(candidate, sequence.transform);
                if (score > bestScore)
                {
                    bestScore = score;
                    best = candidate;
                }
            }

            if (best != null && bestScore > 0)
            {
                sequence.m_CardPackAnimator = best;
                repaired = true;
            }
        }

        if (sequence.m_CardPackMesh == null && sequence.m_CardPackAnimator != null)
        {
            sequence.m_CardPackMesh = sequence.m_CardPackAnimator.GetComponent<SkinnedMeshRenderer>()
                ?? sequence.m_CardPackAnimator.GetComponentInChildren<SkinnedMeshRenderer>(includeInactive: true);
            if (sequence.m_CardPackMesh != null)
            {
                repaired = true;
            }
        }

        if (sequence.m_CardPackMesh == null)
        {
            SkinnedMeshRenderer[] meshes = FindSceneComponents<SkinnedMeshRenderer>();
            SkinnedMeshRenderer? bestMesh = null;
            int bestScore = int.MinValue;

            for (int i = 0; i < meshes.Length; i++)
            {
                SkinnedMeshRenderer candidate = meshes[i];
                if (candidate == null)
                {
                    continue;
                }

                int score = ScorePackMeshCandidate(candidate, sequence.transform);
                if (score > bestScore)
                {
                    bestScore = score;
                    bestMesh = candidate;
                }
            }

            if (bestMesh != null && bestScore > 0)
            {
                sequence.m_CardPackMesh = bestMesh;
                if (sequence.m_CardPackAnimator == null)
                {
                    sequence.m_CardPackAnimator = bestMesh.GetComponentInParent<Animator>(includeInactive: true);
                }

                repaired = true;
            }
        }

        if (sequence.m_StartLerpTransform == null && sequence.m_CardPackAnimator != null)
        {
            sequence.m_StartLerpTransform = EnsureRuntimeStartLerpTransform(sequence);
            repaired = true;
        }

        repaired |= TryBootstrapAnimationAndIcons(sequence);
        repaired |= TryBootstrapCardLists(sequence);

        return repaired;
    }

    private static bool TryBootstrapAnimationAndIcons(CardOpeningSequence sequence)
    {
        bool repaired = false;
        EnsurePackOpenFeedbackIcons(sequence);

        if (sequence.m_CardOpeningRotateToFrontAnim == null
            || IsPerCardPackAnimation(sequence.m_CardOpeningRotateToFrontAnim))
        {
            Animation? rotateAnim = FindPackOpeningSequenceAnimation(sequence);
            if (rotateAnim != null)
            {
                sequence.m_CardOpeningRotateToFrontAnim = rotateAnim;
                repaired = true;
            }
        }

        if (sequence.m_NewCardIcon != null && sequence.m_HighValueCardIcon != null)
        {
            repaired = true;
        }

        return repaired;
    }

    private static Animation? FindPackOpeningSequenceAnimation(CardOpeningSequence sequence)
    {
        Animation? rotateAnim = FindAnimationWithClip("CardOpenSeq1_RotateToFront", sequence.transform);
        if (rotateAnim != null && !IsPerCardPackAnimation(rotateAnim))
        {
            return rotateAnim;
        }

        rotateAnim = FindAnimationWithClip("CardOpenSeq0_Idle", sequence.transform);
        if (rotateAnim != null && !IsPerCardPackAnimation(rotateAnim))
        {
            return rotateAnim;
        }

        return null;
    }

    private static bool IsPerCardPackAnimation(Animation animation)
    {
        string path = GetHierarchyPath(animation.transform);
        return path.IndexOf("Card3d", StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private static bool TryBootstrapCardLists(CardOpeningSequence sequence)
    {
        bool repaired = false;

        if (sequence.m_Card3dUIList == null || sequence.m_Card3dUIList.Count == 0)
        {
            List<Card3dUIGroup> cards = CollectCard3dGroups(sequence);
            if (cards.Count > 0)
            {
                sequence.m_Card3dUIList = cards;
                repaired = true;
            }
        }

        if (sequence.m_Card3dUIList != null && sequence.m_Card3dUIList.Count > 0
            && (sequence.m_CardAnimList == null
                || sequence.m_CardAnimList.Count != sequence.m_Card3dUIList.Count
                || !CardAnimListHasOpenCardClips(sequence.m_CardAnimList)))
        {
            List<Animation> cardAnims = new(sequence.m_Card3dUIList.Count);
            for (int i = 0; i < sequence.m_Card3dUIList.Count; i++)
            {
                Card3dUIGroup? card3d = sequence.m_Card3dUIList[i];
                Animation? slotAnim = card3d != null ? FindOpenCardAnimationFor(card3d) : null;
                if (slotAnim != null)
                {
                    cardAnims.Add(slotAnim);
                }
            }

            if (cardAnims.Count == sequence.m_Card3dUIList.Count)
            {
                sequence.m_CardAnimList = cardAnims;
                repaired = true;
            }
        }

        if (sequence.m_ShowAllCardPosList == null
            || sequence.m_ShowAllCardPosList.Count == 0
            || ShowAllCardPositionsLookLikeAnimGroupFallback(sequence))
        {
            List<Transform> showAllPositions = CollectShowAllCardPositions(sequence);
            if (showAllPositions.Count > 0)
            {
                sequence.m_ShowAllCardPosList = showAllPositions;
                repaired = true;
            }
        }

        return repaired;
    }

    /// <summary>OpenCard slide clips the sequence Update() requires on every m_CardAnimList entry.</summary>
    private static readonly string[] OpenCardClipNames =
    {
        "OpenCardSlideExit",
        "OpenCardNewCard",
        "OpenCardFinalReveal",
        "OpenCardDefaultPos",
    };

    private static bool CardAnimListHasOpenCardClips(List<Animation> cardAnims)
    {
        for (int i = 0; i < cardAnims.Count; i++)
        {
            Animation? animation = cardAnims[i];
            if (animation == null || !AnimationHasAnyOpenCardClip(animation))
            {
                return false;
            }
        }

        return true;
    }

    private static bool AnimationHasAnyOpenCardClip(Animation animation)
    {
        for (int i = 0; i < OpenCardClipNames.Length; i++)
        {
            if (AnimationHasClip(animation, OpenCardClipNames[i]))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// The slot Animation (carries OpenCard* clips) is the parent rig the card sits under, not card3d.m_Anim
    /// (a card-flip clip). Search the card itself, ancestors, then descendants.
    /// </summary>
    private static Animation? FindOpenCardAnimationFor(Card3dUIGroup card3d)
    {
        if (card3d.m_Anim != null && AnimationHasAnyOpenCardClip(card3d.m_Anim))
        {
            return card3d.m_Anim;
        }

        Transform? current = card3d.transform;
        for (int depth = 0; depth < 12 && current != null; depth++)
        {
            Animation? animation = current.GetComponent<Animation>();
            if (animation != null && AnimationHasAnyOpenCardClip(animation))
            {
                return animation;
            }

            current = current.parent;
        }

        Animation[] descendants = card3d.GetComponentsInChildren<Animation>(includeInactive: true);
        for (int i = 0; i < descendants.Length; i++)
        {
            if (descendants[i] != null && AnimationHasAnyOpenCardClip(descendants[i]))
            {
                return descendants[i];
            }
        }

        return null;
    }

    private static bool ShowAllCardPositionsLookLikeAnimGroupFallback(CardOpeningSequence sequence)
    {
        if (sequence.m_ShowAllCardPosList == null || sequence.m_ShowAllCardPosList.Count == 0)
        {
            return false;
        }

        for (int i = 0; i < sequence.m_ShowAllCardPosList.Count; i++)
        {
            Transform? position = sequence.m_ShowAllCardPosList[i];
            if (position != null
                && position.name.IndexOf("CardUIAnim", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return true;
            }
        }

        return false;
    }

    private static List<Card3dUIGroup> CollectCard3dGroups(CardOpeningSequence sequence)
    {
        List<Card3dUIGroup> cards = new();

        if (sequence.m_CardOpeningUIGroup != null)
        {
            Card3dUIGroup[] underUiGroup = sequence.m_CardOpeningUIGroup
                .GetComponentsInChildren<Card3dUIGroup>(includeInactive: true);
            AppendSortedCardGroups(underUiGroup, cards);
        }

        if (cards.Count == 0)
        {
            Card3dUIGroup[] sceneCards = FindSceneComponents<Card3dUIGroup>();
            if (sceneCards.Length is >= 7 and <= 12)
            {
                AppendSortedCardGroups(sceneCards, cards);
            }
            else
            {
                for (int i = 0; i < sceneCards.Length; i++)
                {
                    Card3dUIGroup candidate = sceneCards[i];
                    if (candidate == null)
                    {
                        continue;
                    }

                    string path = GetHierarchyPath(candidate.transform);
                    if (path.IndexOf("CardOpening", StringComparison.OrdinalIgnoreCase) >= 0
                        || path.IndexOf("OpenPack", StringComparison.OrdinalIgnoreCase) >= 0
                        || path.IndexOf("PackOpen", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        cards.Add(candidate);
                    }
                }

                cards.Sort(CompareSiblingOrder);
            }
        }

        // The ported scene stacks the deck with the highest sibling closest to the camera (physical top),
        // but CardOpeningSequence reveals m_Card3dUIList[0] first. Reverse so index 0 = top/closest card,
        // so reveals pull from the top of the visible stack (vanilla behaviour).
        cards.Reverse();

        return cards;
    }

    private static void AppendSortedCardGroups(IReadOnlyList<Card3dUIGroup> source, List<Card3dUIGroup> destination)
    {
        for (int i = 0; i < source.Count; i++)
        {
            Card3dUIGroup? card3d = source[i];
            if (card3d != null && !destination.Contains(card3d))
            {
                destination.Add(card3d);
            }
        }

        destination.Sort(CompareCardGroupOrder);
    }

    private static int CompareCardGroupOrder(Card3dUIGroup left, Card3dUIGroup right)
    {
        int leftNumber = ExtractTrailingNumber(left?.transform.name);
        int rightNumber = ExtractTrailingNumber(right?.transform.name);
        if (leftNumber >= 0 && rightNumber >= 0 && leftNumber != rightNumber)
        {
            return leftNumber.CompareTo(rightNumber);
        }

        return CompareSiblingOrder(left, right);
    }

    private static int ExtractTrailingNumber(string? name)
    {
        if (string.IsNullOrEmpty(name))
        {
            return -1;
        }

        int end = name.Length - 1;
        while (end >= 0 && char.IsDigit(name[end]))
        {
            end--;
        }

        if (end == name.Length - 1)
        {
            return -1;
        }

        string digits = name[(end + 1)..];
        return int.TryParse(digits, out int value) ? value : -1;
    }

    private static int CompareSiblingOrder(Card3dUIGroup left, Card3dUIGroup right)
    {
        Transform? leftTransform = left?.transform;
        Transform? rightTransform = right?.transform;
        if (leftTransform == null || rightTransform == null)
        {
            return 0;
        }

        if (leftTransform.parent == rightTransform.parent)
        {
            return leftTransform.GetSiblingIndex().CompareTo(rightTransform.GetSiblingIndex());
        }

        return string.Compare(GetHierarchyPath(leftTransform), GetHierarchyPath(rightTransform), StringComparison.Ordinal);
    }

    private static List<Transform> CollectShowAllCardPositions(CardOpeningSequence sequence)
    {
        List<Transform> positions = new();
        Transform? searchRoot = sequence.m_CardOpeningUIGroup != null
            ? sequence.m_CardOpeningUIGroup.transform
            : sequence.m_CardOpeningSequenceUI != null
                ? sequence.m_CardOpeningSequenceUI.transform
                : sequence.transform;

        CollectShowAllCardPositionsUnderRoot(searchRoot, positions);

        if (positions.Count == 0)
        {
            Transform[] transforms = FindSceneComponents<Transform>();
            for (int i = 0; i < transforms.Length; i++)
            {
                Transform transform = transforms[i];
                if (transform != null && transform.name.StartsWith("ShowAllCardPos", StringComparison.OrdinalIgnoreCase))
                {
                    positions.Add(transform);
                }
            }
        }

        positions.Sort(CompareShowAllCardPositionOrder);
        return positions;
    }

    private static void CollectShowAllCardPositionsUnderRoot(Transform? root, List<Transform> positions)
    {
        if (root == null)
        {
            return;
        }

        if (root.name.StartsWith("ShowAllCardPos", StringComparison.OrdinalIgnoreCase))
        {
            positions.Add(root);
        }

        for (int i = 0; i < root.childCount; i++)
        {
            CollectShowAllCardPositionsUnderRoot(root.GetChild(i), positions);
        }
    }

    private static int CompareShowAllCardPositionOrder(Transform left, Transform right)
    {
        int leftNumber = ExtractTrailingNumber(left.name);
        int rightNumber = ExtractTrailingNumber(right.name);
        if (leftNumber >= 0 && rightNumber >= 0 && leftNumber != rightNumber)
        {
            return leftNumber.CompareTo(rightNumber);
        }

        return string.Compare(left.name, right.name, StringComparison.OrdinalIgnoreCase);
    }

    private static Animation? FindAnimationWithClip(string clipName, Transform sequenceTransform)
    {
        Animation[] animations = FindSceneComponents<Animation>();
        Animation? best = null;
        int bestScore = int.MinValue;

        for (int i = 0; i < animations.Length; i++)
        {
            Animation candidate = animations[i];
            if (candidate == null || !AnimationHasClip(candidate, clipName))
            {
                continue;
            }

            int score = 0;
            string path = GetHierarchyPath(candidate.transform);
            if (path.IndexOf("CardOpening", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                score += 50;
            }

            if (path.IndexOf("Rotate", StringComparison.OrdinalIgnoreCase) >= 0
                || path.IndexOf("OpenSeq", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                score += 30;
            }

            if (path.IndexOf("Card3d", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                score -= 100;
            }

            float distance = Vector3.Distance(candidate.transform.position, sequenceTransform.position);
            if (distance < 25f)
            {
                score += Math.Max(0, 20 - (int)distance);
            }

            if (score > bestScore)
            {
                bestScore = score;
                best = candidate;
            }
        }

        return best;
    }

    private static bool AnimationHasClip(Animation animation, string clipName)
    {
        foreach (AnimationState state in animation)
        {
            if (state != null && string.Equals(state.name, clipName, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    private static GameObject? FindSceneGameObjectByName(string name)
    {
        Transform? transform = FindSceneTransformByName(name);
        return transform?.gameObject;
    }

    private static bool TryAssignFromTransformTree(Transform root, CardOpeningSequence sequence)
    {
        bool repaired = false;

        if (sequence.m_CardPackAnimator == null)
        {
            Animator? animator = root.GetComponentInChildren<Animator>(includeInactive: true);
            if (animator != null && ScorePackAnimatorCandidate(animator, sequence.transform) > 0)
            {
                sequence.m_CardPackAnimator = animator;
                repaired = true;
            }
        }

        if (sequence.m_CardPackMesh == null)
        {
            SkinnedMeshRenderer? mesh = root.GetComponentInChildren<SkinnedMeshRenderer>(includeInactive: true);
            if (mesh != null && ScorePackMeshCandidate(mesh, sequence.transform) > 0)
            {
                sequence.m_CardPackMesh = mesh;
                repaired = true;
            }
        }

        if (sequence.m_StartLerpTransform == null)
        {
            Transform? startLerp = FindDeepChild(root, "StartLerpTransform")
                ?? FindDeepChild(root, "StartLerpPos");
            if (startLerp != null)
            {
                sequence.m_StartLerpTransform = startLerp;
                repaired = true;
            }
        }

        if (sequence.m_CardOpeningUIGroup == null)
        {
            Transform? uiGroup = FindDeepChild(root, "CardOpeningUIGroup");
            if (uiGroup != null)
            {
                sequence.m_CardOpeningUIGroup = uiGroup.gameObject;
                repaired = true;
            }
        }

        if (sequence.m_CardOpeningSequenceUI == null)
        {
            CardOpeningSequenceUI? ui = root.GetComponentInChildren<CardOpeningSequenceUI>(includeInactive: true);
            if (ui != null)
            {
                sequence.m_CardOpeningSequenceUI = ui;
                repaired = true;
            }
        }

        if (sequence.m_CardOpeningRotateToFrontAnim == null
            || IsPerCardPackAnimation(sequence.m_CardOpeningRotateToFrontAnim))
        {
            Animation? rotateAnim = FindPackOpeningSequenceAnimation(sequence);
            if (rotateAnim != null)
            {
                sequence.m_CardOpeningRotateToFrontAnim = rotateAnim;
                repaired = true;
            }
        }

        if (sequence.m_NewCardIcon == null)
        {
            Transform? newCardIcon = FindDeepChild(root, "NewCardIcon");
            if (newCardIcon != null)
            {
                sequence.m_NewCardIcon = newCardIcon.gameObject;
                repaired = true;
            }
        }

        if (sequence.m_HighValueCardIcon == null)
        {
            Transform? highValueIcon = FindDeepChild(root, "HighValueCardIcon");
            if (highValueIcon != null)
            {
                sequence.m_HighValueCardIcon = highValueIcon.gameObject;
                repaired = true;
            }
        }

        repaired |= TryBootstrapCardLists(sequence);

        return repaired;
    }

    private static int ScorePackAnimatorCandidate(Animator animator, Transform sequenceTransform)
    {
        int score = 0;
        RuntimeAnimatorController? controller = animator.runtimeAnimatorController;
        if (controller != null)
        {
            AnimationClip[] clips = controller.animationClips;
            for (int i = 0; i < clips.Length; i++)
            {
                AnimationClip? clip = clips[i];
                if (clip != null && clip.name.IndexOf("PackOpen", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    score += 100;
                    break;
                }
            }
        }

        string path = GetHierarchyPath(animator.transform);
        if (path.IndexOf("CardOpening", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            score += 40;
        }

        if (path.IndexOf("CardPack", StringComparison.OrdinalIgnoreCase) >= 0
            || path.IndexOf("PackOpen", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            score += 30;
        }

        float distance = Vector3.Distance(animator.transform.position, sequenceTransform.position);
        if (distance < 25f)
        {
            score += Math.Max(0, 20 - (int)distance);
        }

        return score;
    }

    private static int ScorePackMeshCandidate(SkinnedMeshRenderer mesh, Transform sequenceTransform)
    {
        int score = 0;
        string path = GetHierarchyPath(mesh.transform);
        if (path.IndexOf("CardPack", StringComparison.OrdinalIgnoreCase) >= 0
            || path.IndexOf("PackOpen", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            score += 50;
        }

        if (path.IndexOf("CardOpening", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            score += 30;
        }

        float distance = Vector3.Distance(mesh.transform.position, sequenceTransform.position);
        if (distance < 25f)
        {
            score += Math.Max(0, 20 - (int)distance);
        }

        return score;
    }

    private static Transform EnsureRuntimeStartLerpTransform(CardOpeningSequence sequence)
    {
        Transform animTransform = sequence.m_CardPackAnimator.transform;
        Transform parent = animTransform.parent != null ? animTransform.parent : sequence.transform;

        Transform? existing = parent.Find("PackOpenStartLerp071Bootstrap");
        if (existing != null)
        {
            return existing;
        }

        InteractionPlayerController? player = CSingleton<InteractionPlayerController>.Instance;
        Transform? holdPos = player?.m_HoldCardPackPosList != null && player.m_HoldCardPackPosList.Count > 0
            ? player.m_HoldCardPackPosList[0]
            : null;

        GameObject bootstrapObject = new("PackOpenStartLerp071Bootstrap");
        Transform bootstrap = bootstrapObject.transform;
        bootstrap.SetParent(parent, worldPositionStays: false);

        if (holdPos != null)
        {
            bootstrap.position = holdPos.position;
            bootstrap.rotation = holdPos.rotation;
            bootstrap.localScale = holdPos.lossyScale;
        }
        else
        {
            bootstrap.localPosition = animTransform.localPosition;
            bootstrap.localRotation = animTransform.localRotation;
            bootstrap.localScale = animTransform.localScale;
        }

        return bootstrap;
    }

    private static T[] FindSceneComponents<T>() where T : Component
    {
        T[] all = Resources.FindObjectsOfTypeAll<T>();
        List<T> sceneComponents = new(all.Length);
        for (int i = 0; i < all.Length; i++)
        {
            T component = all[i];
            if (component == null || !IsSceneObject(component.gameObject))
            {
                continue;
            }

            sceneComponents.Add(component);
        }

        return sceneComponents.ToArray();
    }

    private static Transform? FindSceneTransformByName(string name)
    {
        Transform[] transforms = FindSceneComponents<Transform>();
        for (int i = 0; i < transforms.Length; i++)
        {
            Transform transform = transforms[i];
            if (transform.name == name)
            {
                return transform;
            }
        }

        return null;
    }

    private static bool IsSceneObject(GameObject gameObject)
    {
        return gameObject != null && gameObject.scene.IsValid();
    }

    private static string GetHierarchyPath(Transform transform)
    {
        string path = transform.name;
        Transform? current = transform.parent;
        while (current != null)
        {
            path = current.name + "/" + path;
            current = current.parent;
        }

        return path;
    }

    private static Transform? FindDeepChild(Transform root, string name)
    {
        if (root.name == name)
        {
            return root;
        }

        for (int i = 0; i < root.childCount; i++)
        {
            Transform? found = FindDeepChild(root.GetChild(i), name);
            if (found != null)
            {
                return found;
            }
        }

        return null;
    }

    private static void LogDiagnostics(CardOpeningSequence sequence)
    {
        Animator[] animators = FindSceneComponents<Animator>();
        int packAnimators = 0;
        for (int i = 0; i < animators.Length; i++)
        {
            if (ScorePackAnimatorCandidate(animators[i], sequence.transform) > 0)
            {
                packAnimators++;
            }
        }

        SkinnedMeshRenderer[] meshes = FindSceneComponents<SkinnedMeshRenderer>();
        Transform? startLerp = FindSceneTransformByName("StartLerpTransform");

        Plugin.Log.LogError(
            "CardOpeningSequence pack refs still missing after scene search. " +
            $"animator={sequence.m_CardPackAnimator != null}, mesh={sequence.m_CardPackMesh != null}, " +
            $"startLerp={sequence.m_StartLerpTransform != null}, uiGroup={sequence.m_CardOpeningUIGroup != null}, " +
            $"rotateAnim={sequence.m_CardOpeningRotateToFrontAnim != null}, card3dCount={sequence.m_Card3dUIList?.Count ?? 0}, " +
            $"cardAnimCount={sequence.m_CardAnimList?.Count ?? 0}, " +
            $"sceneAnimators={animators.Length}, packAnimatorCandidates={packAnimators}, " +
            $"sceneSkinnedMeshes={meshes.Length}, namedStartLerp={startLerp != null}");
    }
}
