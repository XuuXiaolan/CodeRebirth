using CodeRebirth.src.Util;
using UnityEngine;

namespace CodeRebirth.src.MiscScripts;

public class ScreenShakeTarget : MonoBehaviour
{
    [Tooltip("The target GameObject that will have it's position used for screen shake effects.")]
    public GameObject? screenShakeTarget = null;
    [Space(5)]
    [Header("Visual Settings")]
    public ParticleSystem[] particleSystems = [];
    [Space(5)]
    [Header("Audio Settings")]
    public AudioSource? audioSourceNear = null;
    public AudioClip[] nearAudioClips = [];
    public AudioSource? audioSourceFar = null;
    public AudioClip[] farAudioClips = [];

    public void ShakeScreenAnimEvent(AnimationEvent animationEvent)
    {
        ShakeScreen(animationEvent.floatParameter, animationEvent.intParameter, animationEvent.stringParameter, animationEvent.objectReferenceParameter);
    }

    public void ShakeScreen(float outwardDistance, int shakeType, string stringParameters, Object objectReferenceParameter)
    {
        DynamicScreenShakeFalloff? dynamicFalloffSO = null;
        if (objectReferenceParameter is DynamicScreenShakeFalloff shakeFalloff)
        {
            dynamicFalloffSO = shakeFalloff;
        }
        Vector3 shakeStartPosition = this.transform.position;
        if (screenShakeTarget != null)
        {
            shakeStartPosition = screenShakeTarget.transform.position;
        }
        PlayScreenShakeAudio(shakeStartPosition);
        PlayScreenShakeParticles(shakeStartPosition);

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

                HUDManager.Instance.ShakeCamera((ScreenShakeType)shakeType - i);
                break;
            }
        }
    }

    private void PlayScreenShakeAudio(Vector3 shakeStartPosition)
    {
        if (nearAudioClips.Length > 0)
        {
            AudioClip clipToPlay = nearAudioClips[CodeRebirthUtils.Instance.CRRandom.Next(nearAudioClips.Length)];
            if (audioSourceNear != null)
            {
                audioSourceNear.transform.position = shakeStartPosition;
                audioSourceNear.PlayOneShot(clipToPlay);
            }
        }

        if (farAudioClips.Length > 0)
        {
            AudioClip clipToPlay = farAudioClips[CodeRebirthUtils.Instance.CRRandom.Next(farAudioClips.Length)];
            if (audioSourceFar != null)
            {
                audioSourceFar.transform.position = shakeStartPosition;
                audioSourceFar.PlayOneShot(clipToPlay);
            }
        }
    }

    private void PlayScreenShakeParticles(Vector3 shakeStartPosition)
    {
        if (particleSystems.Length <= 0)
            return;

        foreach (ParticleSystem particleSystem in particleSystems)
        {
            particleSystem.transform.position = shakeStartPosition;
            particleSystem.Play();
        }
    }
}