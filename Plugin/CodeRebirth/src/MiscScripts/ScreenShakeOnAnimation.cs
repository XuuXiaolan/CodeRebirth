using UnityEngine;

namespace CodeRebirth.src.MiscScripts;

public class ScreenShakeOnAnimation : MonoBehaviour
{
    public void ShakeScreenAnimEvent(AnimationEvent animationEvent)
    {
        ShakeScreen(animationEvent.floatParameter, animationEvent.intParameter);
    }

    private void ShakeScreen(float distanceToPlayer, int shakeType)
    {
        if (distanceToPlayer != -1 && Vector3.Distance(this.transform.position, GameNetworkManager.Instance.localPlayerController.transform.position) <= distanceToPlayer)
            return;

        switch (shakeType)
        {
            case 0:
                HUDManager.Instance.ShakeCamera(ScreenShakeType.Small);
                break;
            case 1:
                HUDManager.Instance.ShakeCamera(ScreenShakeType.Big);
                break;
            case 2:
                HUDManager.Instance.ShakeCamera(ScreenShakeType.Long);
                break;
            case 3:
                HUDManager.Instance.ShakeCamera(ScreenShakeType.VeryStrong);
                break;
        }
    }
}