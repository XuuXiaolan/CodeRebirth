using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CodeRebirth.src.MiscScripts;
public static class SlowDownEffect
{
    public static bool isSlowDownEffectActive = false;
    public static AudioSource[] audioSourcesToAffect = []; // Patch AudioSource.Awake or find Audio Listener.

    public static InteractTrigger? CurrentlyEditedTrigger { get; private set; }
    public static List<(AudioSource audioSource, float pitch, float volume, float dopplerLevel)> audioSourcesWithOldValues = new();
    public static List<OccludeAudio> occludeAudiosToReEnable = new();
    public static List<(AudioLowPassFilter filter, float oldCutOffFrequency)> lowPassFiltersWithOldValues = new();

    public static void SlowTrigger(InteractTrigger? trigger)
    {
        if (!isSlowDownEffectActive)
            return;

        if (trigger == null)
            return;

        if (CurrentlyEditedTrigger == trigger)
            return;

        if (CurrentlyEditedTrigger != null)
            ResetSlowTrigger(CurrentlyEditedTrigger);

        CurrentlyEditedTrigger = trigger;
        CurrentlyEditedTrigger.timeToHoldSpeedMultiplier /= Time.timeScale;
    }

    public static void ResetSlowTrigger(InteractTrigger trigger)
    {
        if(!isSlowDownEffectActive)
            return;

        if (trigger != CurrentlyEditedTrigger)
            return;

        CurrentlyEditedTrigger.timeToHoldSpeedMultiplier *= Time.timeScale;
        CurrentlyEditedTrigger = null;
    }

    public static void DoSlowdownEffect(float timeLength, float timeScale)
    {
        // todo: patch play and playoneshot
        isSlowDownEffectActive = true;
        audioSourcesToAffect = Resources.FindObjectsOfTypeAll<AudioSource>();
        float timeDelay = timeLength;
        Time.timeScale = timeScale;

        audioSourcesWithOldValues.Clear();
        occludeAudiosToReEnable.Clear();
        lowPassFiltersWithOldValues.Clear();

        GameNetworkManager.Instance.localPlayerController.movementSpeed /= Time.timeScale * 0.8f;
        GameNetworkManager.Instance.localPlayerController.playerBodyAnimator.speed /= Time.timeScale;
        foreach (var audiosource in audioSourcesToAffect)
        {
            SlowdownAudioSource(audiosource, timeDelay);
        }

        GameNetworkManager.Instance.localPlayerController.StartCoroutine(ResetTimeScaleAndMisc(audioSourcesWithOldValues, occludeAudiosToReEnable, lowPassFiltersWithOldValues, timeDelay * Time.timeScale));
    }

    public static void SlowdownAudioSource(AudioSource audioSource, float timeDelay)
    {
        audioSourcesWithOldValues.Add((audioSource, audioSource.pitch, audioSource.volume, audioSource.dopplerLevel));
        audioSource.pitch = 0.2f;
        audioSource.volume = 0.7f * audioSource.volume;
        audioSource.dopplerLevel = 0f;

        if (audioSource.gameObject.TryGetComponent(out OccludeAudio occludeAudio) && occludeAudio.enabled)
        {
            occludeAudiosToReEnable.Add(occludeAudio);
            occludeAudio.enabled = false;
        }

        if (audioSource.gameObject.TryGetComponent(out AudioLowPassFilter filter))
        {
            lowPassFiltersWithOldValues.Add((filter, filter.cutoffFrequency));
            filter.cutoffFrequency = 1000f;
            return;
        }

        filter = audioSource.gameObject.AddComponent<AudioLowPassFilter>();
        Object.Destroy(filter, timeDelay * Time.timeScale);
        filter.cutoffFrequency = 1000f;
    }

    public static void ReEnableOccludeAudio(OccludeAudio occludeAudio)
    {
        if (occludeAudio == null)
            return;

        occludeAudio.enabled = true;
    }

    public static void ResetAudioSourceVariables(AudioSource audioSource, float pitch, float volume, float dopplerLevel)
    {
        if (audioSource == null)
            return;

        audioSource.pitch = pitch;
        audioSource.volume = volume;
        audioSource.dopplerLevel = dopplerLevel;
    }

    public static void ResetLowPassFilterValue(AudioLowPassFilter filter, float cutoffFrequency)
    {
        if (filter == null)
            return;

        filter.cutoffFrequency = cutoffFrequency;
    }

    public static IEnumerator ResetTimeScaleAndMisc(List<(AudioSource audioSource, float pitch, float volume, float dopplerLevel)> audioSourcesWithOldValues, List<OccludeAudio> occludeAudiosToReEnable, List<(AudioLowPassFilter filter, float oldCutOffFrequency)> lowPassFiltersWithOldValues, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (CurrentlyEditedTrigger != null)
            ResetSlowTrigger(CurrentlyEditedTrigger);

        Time.timeScale = 1f;
        GameNetworkManager.Instance.localPlayerController.movementSpeed /= 6f;
        GameNetworkManager.Instance.localPlayerController.playerBodyAnimator.speed /= 5f;
        foreach (var (audioSource, pitch, volume, dopplerLevel) in audioSourcesWithOldValues)
        {
            ResetAudioSourceVariables(audioSource, pitch, volume, dopplerLevel);
        }

        foreach (var occludeAudio in occludeAudiosToReEnable)
        {
            ReEnableOccludeAudio(occludeAudio);
        }

        foreach (var (filter, oldCutOffFrequency) in lowPassFiltersWithOldValues)
        {
            ResetLowPassFilterValue(filter, oldCutOffFrequency);
        }
        
        float timeElapsed = 1f;
        while (timeElapsed > 0)
        {
            timeElapsed -= Time.deltaTime;
            yield return null;
        }
        isSlowDownEffectActive = false;
    }
}