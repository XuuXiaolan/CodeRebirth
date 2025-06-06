using CodeRebirth.src.Util;
using UnityEngine;

namespace CodeRebirth.src.MiscScripts;

public class ScreenShakeOnAnimation : MonoBehaviour
{
    [SerializeField]
    [Tooltip("List of screen shake targets that can be used to play audio when the screen shake is triggered.")]
    private ScreenShakeTarget[] _screenShakeTargets = [];

    public void ShakeScreenAnimEvent(AnimationEvent animationEvent)
    {
        Plugin.ExtendedLogging($"ShakeScreenAnimEvent called with AnimationEvent: {animationEvent}");
        Vector3 shakeStartPosition = this.transform.position;
        if (!string.IsNullOrEmpty(animationEvent.stringParameter))
        {
            foreach (var ScreenShakeTarget in _screenShakeTargets)
            {
                if (ScreenShakeTarget.screenShakeTarget.name != animationEvent.stringParameter)
                    continue;

                shakeStartPosition = ScreenShakeTarget.screenShakeTarget.transform.position;
                PlayScreenShakeAudio(ScreenShakeTarget, shakeStartPosition);
                PlayScreenShakeParticles(ScreenShakeTarget, shakeStartPosition);
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

    private void PlayScreenShakeAudio(ScreenShakeTarget screenShakeTarget, Vector3 shakeStartPosition)
    {
        if (screenShakeTarget.nearAudioClips.Length > 0)
        {
            AudioClip clipToPlay = screenShakeTarget.nearAudioClips[CodeRebirthUtils.Instance.CRRandom.Next(screenShakeTarget.nearAudioClips.Length)];
            if (screenShakeTarget.audioSourceNear != null)
            {
                screenShakeTarget.audioSourceNear.transform.position = shakeStartPosition;
                screenShakeTarget.audioSourceNear.PlayOneShot(clipToPlay);
            }
        }
        if (screenShakeTarget.farAudioClips.Length > 0)
        {
            AudioClip clipToPlay = screenShakeTarget.farAudioClips[CodeRebirthUtils.Instance.CRRandom.Next(screenShakeTarget.farAudioClips.Length)];
            if (screenShakeTarget.audioSourceFar != null)
            {
                screenShakeTarget.audioSourceFar.transform.position = shakeStartPosition;
                screenShakeTarget.audioSourceFar.PlayOneShot(clipToPlay);
            }
        }
    }

    private void PlayScreenShakeParticles(ScreenShakeTarget screenShakeTarget, Vector3 shakeStartPosition)
    {
        if (screenShakeTarget.particleSystems.Length <= 0)
            return;

        foreach (ParticleSystem particleSystem in screenShakeTarget.particleSystems)
        {
            particleSystem.transform.position = shakeStartPosition;
            particleSystem.Play();
        }
    }
}