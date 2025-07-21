
using UnityEngine;

namespace CodeRebirth.src.Content.Items;

public class MistressPlushie : PlushieItem
{
    public override void LateUpdate()
    {
        base.LateUpdate();
        if (isHeld || isPocketed || playerHeldBy != null)
            return;

        float distanceToLocalPlayer = Vector3.Distance(GameNetworkManager.Instance.localPlayerController.gameplayCamera.transform.position, this.transform.position);
        if (distanceToLocalPlayer > 20)
            return;

        if (Physics.Raycast(GameNetworkManager.Instance.localPlayerController.gameplayCamera.transform.position, this.transform.position - GameNetworkManager.Instance.localPlayerController.gameplayCamera.transform.position, distanceToLocalPlayer, StartOfRound.Instance.collidersAndRoomMaskAndDefault, QueryTriggerInteraction.Ignore))
        {
            return;
        }

        this.transform.LookAt(GameNetworkManager.Instance.localPlayerController.transform);
    }
}