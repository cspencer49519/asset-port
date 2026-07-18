using System.Collections;
using UnityEngine;

namespace TCGShopExpansionMod0703Patch;

/// <summary>
/// Holographic Overhaul HoloFixMatLock.LateUpdate re-locks foil materials (often white _MainTex).
/// TextureReplacer can also re-enable GradeCardScratch (CardBack look) after our LateUpdate.
/// Re-assert foil hide / graded Destiny face after HO and again at end of frame.
/// </summary>
[DefaultExecutionOrder(32760)]
internal sealed class AlbumHoFoilRepairBehaviour : MonoBehaviour
{
    private CardUI? _cardUi;
    private Coroutine? _endOfFrameLoop;

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
        if (behaviour._endOfFrameLoop == null && behaviour.isActiveAndEnabled)
        {
            behaviour._endOfFrameLoop = behaviour.StartCoroutine(behaviour.EndOfFrameRepairLoop());
        }
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

    private void OnEnable()
    {
        if (_cardUi != null && _endOfFrameLoop == null)
        {
            _endOfFrameLoop = StartCoroutine(EndOfFrameRepairLoop());
        }
    }

    private void OnDisable()
    {
        if (_endOfFrameLoop != null)
        {
            StopCoroutine(_endOfFrameLoop);
            _endOfFrameLoop = null;
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

    private IEnumerator EndOfFrameRepairLoop()
    {
        var wait = new WaitForEndOfFrame();
        while (enabled && _cardUi != null)
        {
            yield return wait;
            if (_cardUi == null)
            {
                yield break;
            }

            Patches.TetramonOverlay0703Patches.RepairAlbumHoFoilMainTex(_cardUi);
        }

        _endOfFrameLoop = null;
    }
}
