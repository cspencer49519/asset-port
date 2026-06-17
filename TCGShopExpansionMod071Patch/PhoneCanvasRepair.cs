using UnityEngine;
using UnityEngine.UI;

namespace TCGShopExpansionMod071Patch;

internal static class PhoneCanvasRepair
{
    private const AdditionalCanvasShaderChannels RequiredShaderChannels =
        AdditionalCanvasShaderChannels.TexCoord1
        | AdditionalCanvasShaderChannels.TexCoord2
        | AdditionalCanvasShaderChannels.Normal
        | AdditionalCanvasShaderChannels.Tangent;

    public static void EnsurePhoneCanvasesReady(Transform? phoneRoot)
    {
        if (phoneRoot == null)
        {
            return;
        }

        foreach (Canvas canvas in phoneRoot.GetComponentsInChildren<Canvas>(true))
        {
            EnsureCanvasChannels(canvas);
        }
    }

    /// <summary>
    /// TMP SDF shaders require UV2/Normal/Tangent channels on the canvas that actually batches/draws
    /// the text. That is the label's rootCanvas, which is usually an ancestor of m_PhoneGrp, so we
    /// must set the channels there as well as on the nearest canvas.
    /// </summary>
    public static void EnsureLabelCanvasChannels(Canvas? canvas)
    {
        if (canvas == null)
        {
            return;
        }

        EnsureCanvasChannels(canvas);
        EnsureCanvasChannels(canvas.rootCanvas);
    }

    private static void EnsureCanvasChannels(Canvas? canvas)
    {
        if (canvas == null)
        {
            return;
        }

        canvas.additionalShaderChannels |= RequiredShaderChannels;
    }
}
