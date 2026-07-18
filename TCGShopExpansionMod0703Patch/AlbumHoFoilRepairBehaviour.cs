using UnityEngine;

namespace TCGShopExpansionMod0703Patch;

/// <summary>
/// Holographic Overhaul HoloFixMatLock.LateUpdate re-locks foil materials (often white _MainTex).
/// Re-bind or hide foil hosts after HO each frame for album and world cards (Destiny shelf/trade).
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

        Patches.TetramonOverlay0703Patches.RepairAlbumHoFoilMainTex(_cardUi);
    }
}
