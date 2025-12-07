
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

        Vector3 toTarget = GameNetworkManager.Instance.localPlayerController.transform.position - this.transform.position;

        float dot = Vector3.Dot(toTarget, GameNetworkManager.Instance.localPlayerController.gameplayCamera.transform.forward);
        if (dot <= 0f)
        {
            return;
        }

        toTarget.y = 0;
        float sharpness = 0.1f;
        this.transform.forward = Vector3.Lerp(this.transform.forward, toTarget, Time.deltaTime * sharpness);
    }
}