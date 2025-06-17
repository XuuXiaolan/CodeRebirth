using CodeRebirth.src.Util;
using CodeRebirthLib.Util;
using UnityEngine;
using UnityEngine.Video;

namespace CodeRebirth.src.Content.Items;
public class CasetteTape : GrabbableObject
{
    [SerializeField]
    private VideoClip _videoToPlay = null!;

    public override void ItemActivate(bool used, bool buttonDown = true)
    {
        base.ItemActivate(used, buttonDown);
        Ray ray = new Ray(playerHeldBy.gameplayCamera.transform.position, playerHeldBy.gameplayCamera.transform.forward);
        if (!Physics.Raycast(ray, out RaycastHit raycastHit, 3f, MoreLayerMasks.propsMask, QueryTriggerInteraction.Ignore))
            return;

        if (!raycastHit.transform.TryGetComponent(out CasettePlayer casettePlayer))
            return;

        casettePlayer.StartCoroutine(casettePlayer.PlayTape(_videoToPlay, this));
    }
}