using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TCGShopExpansionMod0703Patch;

/// <summary>
/// Restores cash-register / credit-card TMP visibility.
/// TextureReplacer bumps nearby UI Image materials to ~3005; shared TMP stays at 3000 and draws under panels.
/// </summary>
internal static class CashRegisterTmpRepair
{
    private const int DefaultHudImageQueue = 3005;
    private static bool _loggedQueueSample;

    public static void RepairHierarchy(Transform? root, string reason)
    {
        if (root == null)
        {
            return;
        }

        PhoneFontMaterialSnapshot.CaptureIfNeeded();

        int maxImageQueue = ResolveMaxImageRenderQueue(root);
        int repaired = 0;
        TextMeshProUGUI[] labels = root.GetComponentsInChildren<TextMeshProUGUI>(includeInactive: true);
        for (int i = 0; i < labels.Length; i++)
        {
            TextMeshProUGUI? label = labels[i];
            if (label == null)
            {
                continue;
            }

            if (RepairLabel(label, maxImageQueue))
            {
                repaired++;
            }
        }

        PhoneCanvasRepair.EnsurePhoneCanvasesReady(root);

        if (!_loggedQueueSample)
        {
            _loggedQueueSample = true;
            Plugin.Log.LogInfo(
                $"Cash/credit TMP repair sample: maxImageQueue={maxImageQueue}, labels={repaired}, reason={reason}.");
        }
        else
        {
            Plugin.Log.LogInfo($"Cash/credit TMP repair ({reason}): {repaired} label(s) under '{root.name}'.");
        }
    }

    private static int ResolveMaxImageRenderQueue(Transform root)
    {
        int queue = DefaultHudImageQueue;
        Image[] images = root.GetComponentsInChildren<Image>(includeInactive: true);
        for (int i = 0; i < images.Length; i++)
        {
            Image? image = images[i];
            if (image == null)
            {
                continue;
            }

            Material? material = image.materialForRendering ?? image.material;
            if (material != null && material.renderQueue > queue)
            {
                queue = material.renderQueue;
            }
        }

        return queue;
    }

    private static bool RepairLabel(TextMeshProUGUI label, int targetQueue)
    {
        TMP_FontAsset? font = label.font;
        if (font == null)
        {
            return false;
        }

        // Drop phone-only instance materials that may have leaked onto HUD labels.
        Material? current = label.fontMaterial;
        if (current != null
            && current.name != null
            && current.name.StartsWith("PhoneLabel_", System.StringComparison.Ordinal))
        {
            label.fontMaterial = null;
            current = null;
        }

        Material hudMaterial;
        if (current != null
            && current.name != null
            && current.name.StartsWith("HudLabel_", System.StringComparison.Ordinal))
        {
            hudMaterial = current;
        }
        else
        {
            hudMaterial = PhoneFontMaterialSnapshot.CreateHudLabelMaterial(font);
            label.fontMaterial = hudMaterial;
        }

        // Draw above nearby UI panels (TextureReplacer bumps Image materials to ~3005).
        int textQueue = targetQueue + 1;
        if (hudMaterial.renderQueue < textQueue)
        {
            hudMaterial.renderQueue = textQueue;
        }

        if (label.color.a < 0.05f
            || (label.color.r < 0.05f && label.color.g < 0.05f && label.color.b < 0.05f))
        {
            label.color = Color.white;
        }

        label.faceColor = new Color32(255, 255, 255, 255);
        label.enabled = true;
        if (label.gameObject != null && !label.gameObject.activeSelf)
        {
            label.gameObject.SetActive(true);
        }

        PhoneCanvasRepair.EnsureLabelCanvasChannels(label.canvas);

        CanvasRenderer? canvasRenderer = label.canvasRenderer;
        if (canvasRenderer != null)
        {
            canvasRenderer.SetColor(Color.white);
            canvasRenderer.SetAlpha(1f);
            canvasRenderer.cull = false;
            canvasRenderer.cullTransparentMesh = false;
            canvasRenderer.materialCount = 1;
            canvasRenderer.SetMaterial(hudMaterial, 0);
            if (font.atlasTexture != null)
            {
                canvasRenderer.SetTexture(font.atlasTexture);
            }
        }

        label.SetAllDirty();
        if (label.gameObject.activeInHierarchy)
        {
            label.ForceMeshUpdate(true, false);
        }

        return true;
    }
}
