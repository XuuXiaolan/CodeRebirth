using System.Collections;
using System.Collections.Generic;
using CodeRebirth.src.Util;
using UnityEngine;

namespace CodeRebirth.src.MiscScripts;
public static class SlowDownEffect
{
    public static AudioSource[] audioSourcesToAffect = []; // Patch AudioSource.Awake or find Audio Listener.
    public static void DoSlowdownEffect(float timeLength, float timeScale)
    {
        CodeRebirthUtils.Instance.TimeSlowVolume.weight = 1f;
        if (audioSourcesToAffect.Length == 0) audioSourcesToAffect = Resources.FindObjectsOfTypeAll<AudioSource>();
        float timeDelay = timeLength;
        Time.timeScale = timeScale;
        List<(AudioSource audioSource, float pitch, float volume, float dopplerLevel)> audioSourcesWithOldValues = new();
        List<OccludeAudio> occludeAudiosToReEnable = new();
        List<(AudioLowPassFilter filter, float oldCutOffFrequency)> lowPassFiltersWithOldValues = new();
        foreach (var player in StartOfRound.Instance.allPlayerScripts)
        {
            player.movementSpeed /= (Time.timeScale*0.8f);
            player.playerBodyAnimator.speed /= Time.timeScale;
        }
        foreach (var audiosource in audioSourcesToAffect)
        {
            if (audiosource == null) continue;
            audioSourcesWithOldValues.Add((audiosource, audiosource.pitch, audiosource.volume, audiosource.dopplerLevel));
            audiosource.pitch = 0.2f;
            audiosource.volume = 0.7f * audiosource.volume;
            audiosource.dopplerLevel = 0f;
            if (audiosource.gameObject.TryGetComponent(out OccludeAudio occludeAudio) && occludeAudio.enabled)
            {
                occludeAudiosToReEnable.Add(occludeAudio);
                occludeAudio.enabled = false;
            }
            if (audiosource.gameObject.TryGetComponent(out AudioLowPassFilter filter))
            {
                lowPassFiltersWithOldValues.Add((filter, filter.cutoffFrequency));
                filter.cutoffFrequency = 1000f;
                continue;
            }
            filter = audiosource.gameObject.AddComponent<AudioLowPassFilter>();
            Object.Destroy(filter, timeDelay * Time.timeScale);
            filter.cutoffFrequency = 1000f;
        }
        GameNetworkManager.Instance.localPlayerController.StartCoroutine(ResetTimeScaleAndMisc(audioSourcesWithOldValues, occludeAudiosToReEnable, lowPassFiltersWithOldValues, timeDelay * Time.timeScale));
    }

    public static void ReEnableOccludeAudio(OccludeAudio occludeAudio)
    {
        if (occludeAudio == null) return;
        occludeAudio.enabled = true;
    }

    public static void ResetAudioSourceVariables(AudioSource audioSource, float pitch, float volume, float dopplerLevel)
    {
        if (audioSource == null) return;
        audioSource.pitch = pitch;
        audioSource.volume = volume;
        audioSource.dopplerLevel = dopplerLevel;
    }

    public static void ResetLowPassFilterValue(AudioLowPassFilter filter, float cutoffFrequency)
    {
        if (filter == null) return;
        filter.cutoffFrequency = cutoffFrequency;
    }

    public static IEnumerator ResetTimeScaleAndMisc(List<(AudioSource audioSource, float pitch, float volume, float dopplerLevel)> audioSourcesWithOldValues, List<OccludeAudio> occludeAudiosToReEnable, List<(AudioLowPassFilter filter, float oldCutOffFrequency)> lowPassFiltersWithOldValues, float delay)
    {
        yield return new WaitForSeconds(delay);
        Time.timeScale = 1f;
        foreach (var player in StartOfRound.Instance.allPlayerScripts)
        {
            player.movementSpeed /= 6f;
            player.playerBodyAnimator.speed /= 5f;
        }
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
            CodeRebirthUtils.Instance.TimeSlowVolume.weight = timeElapsed;
        }
        CodeRebirthUtils.Instance.TimeSlowVolume.weight = 0f;
    }
}