using UnityEngine;

namespace TCGShopExpansionMod0703Patch;

/// <summary>
/// Holographic Overhaul HoloFixMatLock.LateUpdate re-locks foil materials (often white _MainTex).
/// Re-bind album foil hosts to the ArtExpander face after HO each frame.
/// </summary>
[DefaultExecutionOrder(32000)]
internal sealed class AlbumHoFoilRepairBehaviour : MonoBehaviour
{
    private CardUI? _cardUi;

    public static void EnsureOn(CardUI cardUi)
    {
        if (cardUi == null)
        {
            return;
        }

        AlbumHoFoilRepairBehaviour behaviour = cardUi.GetComponent<AlbumHoFoilRepairBehaviour>();
        if (behaviour == null)
        {
            behaviour = cardUi.gameObject.AddComponent<AlbumHoFoilRepairBehaviour>();
        }

        behaviour._cardUi = cardUi;
        behaviour.enabled = true;
    }

    public static void DisableOn(CardUI cardUi)
    {
        if (cardUi == null)
        {
            return;
        }

        AlbumHoFoilRepairBehaviour? behaviour = cardUi.GetComponent<AlbumHoFoilRepairBehaviour>();
        if (behaviour != null)
        {
            behaviour.enabled = false;
        }
    }

    private void LateUpdate()
    {
        if (_cardUi == null)
        {
            enabled = false;
            return;
        }

        if (!CardUiDisplayContext.IsBinderAlbumCard(_cardUi))
        {
            enabled = false;
            return;
        }

        Patches.TetramonOverlay0703Patches.RepairAlbumHoFoilMainTex(_cardUi);
    }
}
