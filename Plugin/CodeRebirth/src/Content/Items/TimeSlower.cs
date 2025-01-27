using System.Collections;
using UnityEngine;

namespace CodeRebirth.src.Content.Items;
public class TimeSlower : GrabbableObject
{
    [HideInInspector] public static AudioSource[] audioSourcesToAffect = [];
    public override void Start()
    {
        base.Start();
        if (audioSourcesToAffect.Length == 0) audioSourcesToAffect = Resources.FindObjectsOfTypeAll<AudioSource>();
    }

    public override void ItemActivate(bool used, bool buttonDown = true)
    {
        base.ItemActivate(used, buttonDown);
        float timeDelay = 30f;
        Time.timeScale = 0.2f;
        foreach (var player in StartOfRound.Instance.allPlayerScripts)
        {
            player.movementSpeed *= 6f;
            player.playerBodyAnimator.speed *= 5f;
        }
        foreach (var audiosource in audioSourcesToAffect)
        {
            StartCoroutine(ResetAudioSourceVariables(audiosource, audiosource.pitch, audiosource.volume, audiosource.dopplerLevel, timeDelay * Time.timeScale));
            audiosource.pitch = 0.2f;
            audiosource.volume = 0.7f * audiosource.volume;
            audiosource.dopplerLevel = 0f;
            if (audiosource.gameObject.TryGetComponent(out OccludeAudio occludeAudio) && occludeAudio.enabled)
            {
                occludeAudio.enabled = false;
                StartCoroutine(ReEnableOccludeAudio(occludeAudio, timeDelay * Time.timeScale));
            }
            if (audiosource.gameObject.TryGetComponent(out AudioLowPassFilter filter))
            {
                StartCoroutine(ResetLowPassFilterValue(filter, filter.cutoffFrequency, timeDelay * Time.timeScale));
                filter.cutoffFrequency = 1000f;
                continue;
            }
            filter = audiosource.gameObject.AddComponent<AudioLowPassFilter>();
            Destroy(filter, timeDelay * Time.timeScale);
            filter.cutoffFrequency = 1000f;
        }
        StartCoroutine(ResetTimeScale(timeDelay * Time.timeScale));
    }

    public IEnumerator ReEnableOccludeAudio(OccludeAudio occludeAudio, float delay)
    {
        yield return new WaitForSeconds(delay);
        occludeAudio.enabled = true;
    }

    public IEnumerator ResetAudioSourceVariables(AudioSource audioSource, float pitch, float volume, float dopplerLevel, float delay)
    {
        yield return new WaitForSeconds(delay);
        audioSource.pitch = pitch;
        audioSource.volume = volume;
        audioSource.dopplerLevel = dopplerLevel;
    }

    public IEnumerator ResetLowPassFilterValue(AudioLowPassFilter filter, float cutoffFrequency, float delay)
    {
        yield return new WaitForSeconds(delay);
        filter.cutoffFrequency = cutoffFrequency;
    }

    public IEnumerator ResetTimeScale(float delay)
    {
        yield return new WaitForSeconds(delay);
        Time.timeScale = 1f;
        foreach (var player in StartOfRound.Instance.allPlayerScripts)
        {
            player.movementSpeed /= 6f;
            player.playerBodyAnimator.speed /= 5f;
        }
    }
}