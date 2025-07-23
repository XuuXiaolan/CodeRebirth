
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

        float dot = Vector3.Dot(this.transform.forward, GameNetworkManager.Instance.localPlayerController.gameplayCamera.transform.forward);
        if (dot > 0f)
        {
            return;
        }

        Vector3 toTarget = GameNetworkManager.Instance.localPlayerController.transform.position - this.transform.position;
        Quaternion targetRotation = Quaternion.LookRotation(toTarget);
        targetRotation.y = 0;
       float sharpness = 0.1f;
       this.transform.rotation = Quaternion.Lerp(this.transform.rotation, targetRotation, Time.deltaTime * sharpness);
    }
}