using System.Collections;
using System.Collections.Generic;
using GameNetcodeStuff;
using UnityEngine;

namespace CodeRebirth.src.Content.Items;
public class WalkieYellie : GrabbableObject
{
    public override void DiscardItem()
    {
        base.DiscardItem();
        
    }

    public override void ItemActivate(bool used, bool buttonDown = true)
    {
        base.ItemActivate(used, buttonDown);

        StartOfRound.Instance.RefreshPlayerVoicePlaybackObjects();
        Plugin.ExtendedLogging($"Letting go of thing: {!buttonDown}");
        Plugin.ExtendedLogging($"Is owner: {IsOwner}");
        playerHeldBy.activatingItem = buttonDown;
        if (buttonDown && !IsOwner)
        {
            StartCoroutine(ResetVoiceChatAudioSource(playerHeldBy.currentVoiceChatAudioSource, playerHeldBy.currentVoiceChatAudioSource.maxDistance));
            playerHeldBy.currentVoiceChatAudioSource.maxDistance = 999f;

            AudioDistortionFilter distortionFilter = playerHeldBy.currentVoiceChatAudioSource.gameObject.AddComponent<AudioDistortionFilter>();
            distortionFilter.distortionLevel = 0.4f;
            StartCoroutine(DestroyDistortionFilter(distortionFilter));

            if (playerHeldBy.currentVoiceChatAudioSource.gameObject.TryGetComponent(out AudioDistortionFilter audioDistortionFilter))
            {
                Plugin.ExtendedLogging($"Found Audio Distortion Filter");
            }

            if (playerHeldBy.currentVoiceChatAudioSource.gameObject.TryGetComponent(out AudioLowPassFilter audioLowPassFilter))
            {
                StartCoroutine(ResetLowPassFilterValue(audioLowPassFilter, audioLowPassFilter.cutoffFrequency));
                audioLowPassFilter.cutoffFrequency = 2899f;
            }

            if (playerHeldBy.currentVoiceChatAudioSource.gameObject.TryGetComponent(out AudioHighPassFilter audioHighPassFilter))
            {
                StartCoroutine(ResetHighPassFilterValue(audioHighPassFilter, audioHighPassFilter.cutoffFrequency));
                audioHighPassFilter.cutoffFrequency = 1613f;
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