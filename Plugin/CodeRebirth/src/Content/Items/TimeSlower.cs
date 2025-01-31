using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CodeRebirth.src.Content.Items;
public class TimeSlower : GrabbableObject
{
    [HideInInspector] public static AudioSource[] audioSourcesToAffect = [];
    public override void Start()
    {
        base.Start();
        if (audioSourcesToAffect.Length == 0) audioSourcesToAffect = Resources.FindObjectsOfTypeAll<AudioSource>(); // Patch AudioSource.Awake or find Audio Listener
    }

    public override void ItemActivate(bool used, bool buttonDown = true)
    {
        base.ItemActivate(used, buttonDown);
        float timeDelay = 30f;
        Time.timeScale = 0.2f;
        List<(AudioSource audioSource, float pitch, float volume, float dopplerLevel)> audioSourcesWithOldValues = new();
        List<OccludeAudio> occludeAudiosToReEnable = new();
        List<(AudioLowPassFilter filter, float oldCutOffFrequency)> lowPassFiltersWithOldValues = new();
        foreach (var player in StartOfRound.Instance.allPlayerScripts)
        {
            player.movementSpeed *= 6f;
            player.playerBodyAnimator.speed *= 5f;
        }
        foreach (var audiosource in audioSourcesToAffect)
        {
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
            Destroy(filter, timeDelay * Time.timeScale);
            filter.cutoffFrequency = 1000f;
        }
        StartCoroutine(ResetTimeScaleAndMisc(audioSourcesWithOldValues, occludeAudiosToReEnable, lowPassFiltersWithOldValues, timeDelay * Time.timeScale));
    }

    public void ReEnableOccludeAudio(OccludeAudio occludeAudio)
    {
        occludeAudio.enabled = true;
    }

    public void ResetAudioSourceVariables(AudioSource audioSource, float pitch, float volume, float dopplerLevel)
    {
        audioSource.pitch = pitch;
        audioSource.volume = volume;
        audioSource.dopplerLevel = dopplerLevel;
    }

    public void ResetLowPassFilterValue(AudioLowPassFilter filter, float cutoffFrequency)
    {
        filter.cutoffFrequency = cutoffFrequency;
    }

    public IEnumerator ResetTimeScaleAndMisc(List<(AudioSource audioSource, float pitch, float volume, float dopplerLevel)> audioSourcesWithOldValues, List<OccludeAudio> occludeAudiosToReEnable, List<(AudioLowPassFilter filter, float oldCutOffFrequency)> lowPassFiltersWithOldValues, float delay)
    {
        yield return new WaitForSeconds(delay);
        Time.timeScale = 1f;
        foreach (var player in StartOfRound.Instance.allPlayerScripts)
        {
            player.movementSpeed /= 6f;
            player.playerBodyAnimator.speed /= 5f;
        }
        foreach (var audiosourceThing in audioSourcesWithOldValues)
        {
            ResetAudioSourceVariables(audiosourceThing.audioSource, audiosourceThing.pitch, audiosourceThing.volume, audiosourceThing.dopplerLevel);
        }

        foreach (var occludeAudio in occludeAudiosToReEnable)
        {
            ReEnableOccludeAudio(occludeAudio);
        }

        foreach (var lowPassFilter in lowPassFiltersWithOldValues)
        {
            ResetLowPassFilterValue(lowPassFilter.filter, lowPassFilter.oldCutOffFrequency);
        }
    }
}