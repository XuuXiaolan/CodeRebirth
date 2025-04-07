using System.Collections;
using UnityEngine;

namespace CodeRebirth.src.Content.Items;
public class WalkieYellie : GrabbableObject
{
    public override void ItemActivate(bool used, bool buttonDown = true) // look into audio reverb zones
    {
        base.ItemActivate(used, buttonDown);

        StartOfRound.Instance.RefreshPlayerVoicePlaybackObjects();
        Plugin.ExtendedLogging($"Letting go of thing: {!buttonDown}");
        Plugin.ExtendedLogging($"Is owner: {IsOwner}");
        playerHeldBy.activatingItem = buttonDown;
        if (buttonDown)
        {
            if (IsOwner)
            {
                SoundManager.Instance.echoEnabled = true;
                SoundManager.Instance.diageticMixer.SetFloat("EchoWetness", 0.4f);
            }
            else
            {
                StartCoroutine(ResetVoiceChatAudioSource(playerHeldBy.currentVoiceChatAudioSource, playerHeldBy.currentVoiceChatAudioSource.maxDistance));
                playerHeldBy.currentVoiceChatAudioSource.maxDistance = 999f;
            }
        }

        if (!IsOwner) Plugin.ExtendedLogging($"Setting voice distance of playerHeldBy to: {playerHeldBy.currentVoiceChatAudioSource.maxDistance}");
    }

    private IEnumerator DestroyDistortionFilter(AudioDistortionFilter distortionFilter)
    {
        yield return new WaitUntil(() => playerHeldBy == null || !playerHeldBy.activatingItem || isPocketed);
        Destroy(distortionFilter);
    }

    private IEnumerator ResetVoiceChatAudioSource(AudioSource audioSource, float maxDistance)
    {
        yield return new WaitUntil(() => playerHeldBy == null || !playerHeldBy.activatingItem || isPocketed);
        audioSource.maxDistance = maxDistance;
    }

    private IEnumerator ResetLowPassFilterValue(AudioLowPassFilter audioLowPAudioLowPassFilter, float cutoffFrequency)
    {
        yield return new WaitUntil(() => playerHeldBy == null || !playerHeldBy.activatingItem || isPocketed);
        audioLowPAudioLowPassFilter.cutoffFrequency = cutoffFrequency;
    }

    private IEnumerator ResetHighPassFilterValue(AudioHighPassFilter audioHighPassFilter, float cutoffFrequency)
    {
        yield return new WaitUntil(() => playerHeldBy == null || !playerHeldBy.activatingItem || isPocketed);
        audioHighPassFilter.cutoffFrequency = cutoffFrequency;
    }
}