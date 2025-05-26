using UnityEngine;

namespace CodeRebirth.src.MiscScripts;

public class ScreenShakeOnAnimation : MonoBehaviour
{
    [SerializeField]
    private ScreenShakeTarget[] _screenShakeTargets = [];

    public void ShakeScreenAnimEvent(AnimationEvent animationEvent)
    {
        Vector3 shakeStartPosition = this.transform.position;
        if (string.IsNullOrEmpty(animationEvent.stringParameter))
        {
            foreach (var ScreenShakeTarget in _screenShakeTargets)
            {
                if (ScreenShakeTarget.gameObject.name == animationEvent.stringParameter)
                {
                    shakeStartPosition = ScreenShakeTarget.transform.position;
                }
            }            
        }

        ShakeScreen(animationEvent.floatParameter, animationEvent.intParameter, shakeStartPosition);
    }

    private void ShakeScreen(float outwardDistance, int shakeType, Vector3 shakeStartPosition, DynamicScreenShakeFalloff? dynamicFalloffSO = null)
    {
        shakeType = Mathf.Clamp(shakeType, 0, 3);
        float distanceToPlayer = Vector3.Distance(shakeStartPosition, GameNetworkManager.Instance.localPlayerController.transform.position);
        if (outwardDistance == -1 || distanceToPlayer < outwardDistance)
        {
            HUDManager.Instance.ShakeCamera((ScreenShakeType)shakeType);
            return;
        }
        if (dynamicFalloffSO != null)
        {
            float outwardDistanceIncrease = 0f;
            for (int i = 0; i <= shakeType; i++)
            {
                switch (shakeType - i)
                {
                    case 0:
                        outwardDistanceIncrease += dynamicFalloffSO.dynamicIncreaseFromBigToSmall;
                        break;
                    case 1:
                        outwardDistanceIncrease += dynamicFalloffSO.dynamicIncreaseFromLongToBig;
                        break;
                    case 2:
                        outwardDistanceIncrease += dynamicFalloffSO.dynamicIncreaseFromVeryStrongToLong;
                        break;
                    case 3:
                        outwardDistanceIncrease += 0;
                        break;
                }

                if (distanceToPlayer > outwardDistance + outwardDistanceIncrease)
                    return;

                HUDManager.Instance.ShakeCamera((ScreenShakeType)shakeType - i);
            }
        }
    }
}