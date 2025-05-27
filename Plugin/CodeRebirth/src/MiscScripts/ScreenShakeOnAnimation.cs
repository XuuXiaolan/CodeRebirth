using CodeRebirth.src.Util;
using UnityEngine;

namespace CodeRebirth.src.MiscScripts;

public class ScreenShakeOnAnimation : MonoBehaviour
{
    [SerializeField]
    private ScreenShakeTarget[] _screenShakeTargets = [];

    public void ShakeScreenAnimEvent(AnimationEvent animationEvent)
    {
        Vector3 shakeStartPosition = this.transform.position;
        if (!string.IsNullOrEmpty(animationEvent.stringParameter))
        {
            foreach (var ScreenShakeTarget in _screenShakeTargets)
            {
                if (ScreenShakeTarget.screenShakeTarget.name != animationEvent.stringParameter)
                    continue;
                shakeStartPosition = ScreenShakeTarget.transform.position;
                if (ScreenShakeTarget.nearAudioClips.Length > 0)
                {
                    AudioClip clipToPlay = ScreenShakeTarget.nearAudioClips[CodeRebirthUtils.Instance.CRRandom.Next(ScreenShakeTarget.nearAudioClips.Length)];
                    if (ScreenShakeTarget.audioSourceNear != null)
                    {
                        ScreenShakeTarget.audioSourceNear.transform.position = shakeStartPosition;
                        ScreenShakeTarget.audioSourceNear.PlayOneShot(clipToPlay);
                    }
                }
                if (ScreenShakeTarget.farAudioClips.Length > 0)
                {
                    AudioClip clipToPlay = ScreenShakeTarget.farAudioClips[CodeRebirthUtils.Instance.CRRandom.Next(ScreenShakeTarget.farAudioClips.Length)];
                    if (ScreenShakeTarget.audioSourceFar != null)
                    {
                        ScreenShakeTarget.audioSourceFar.transform.position = shakeStartPosition;
                        ScreenShakeTarget.audioSourceFar.PlayOneShot(clipToPlay);
                    }
                }
                break;
            }            
        }

        DynamicScreenShakeFalloff? shakeFalloff = null;
        if (animationEvent.objectReferenceParameter is DynamicScreenShakeFalloff shakeFalloff1)
        {
            shakeFalloff = shakeFalloff1;
        }
        ShakeScreen(animationEvent.floatParameter, animationEvent.intParameter, shakeStartPosition, shakeFalloff);
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
                    continue;

                Plugin.ExtendedLogging($"ShakeType: {shakeType} - i: {i} - outwardDistanceIncrease: {outwardDistanceIncrease} - distanceToPlayer: {distanceToPlayer} - outwardDistance: {outwardDistance}");
                HUDManager.Instance.ShakeCamera((ScreenShakeType)shakeType - i);
                break;
            }
        }
    }
}