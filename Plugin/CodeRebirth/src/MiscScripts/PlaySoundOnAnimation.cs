using System.Collections.Generic;
using UnityEngine;

namespace CodeRebirth.src.MiscScripts;

public class PlaySoundOnAnimation : MonoBehaviour
{
    [SerializeField]
    private List<AudioSourceWithClips> _audioSourcesWithClips = new List<AudioSourceWithClips>();

    public void PlaySelectSourcesWithRandomSound(int index)
    {
        if (index < 0 || index >= _audioSourcesWithClips.Count)
        {
            Plugin.Logger.LogWarning($"Index {index} is out of range for audio sources with clips.");
            return;
        }

        bool isNear = false;
        bool isFar = false;
        AudioSourceWithClips audioSourceWithClips = _audioSourcesWithClips[index];
        if (audioSourceWithClips.nearAudioSource != null && audioSourceWithClips.nearClips.Length > 0)
        {
            isNear = true;
            PlayNearAudioClip(audioSourceWithClips);
        }

        if (audioSourceWithClips.farAudioSource != null && audioSourceWithClips.farClips.Length > 0)
        {
            isFar = true;
            PlayFarAudioClip(audioSourceWithClips);
        }

        if (!isNear && !isFar)
        {
            Plugin.Logger.LogWarning("No valid audio sources or clips found for the specified index.");
        }
    }

    private void PlayNearAudioClip(AudioSourceWithClips audioSourceWithClips)
    {
        int randomIndex = Random.Range(0, audioSourceWithClips.nearClips.Length);
        AudioClip clip = audioSourceWithClips.nearClips[randomIndex];
        audioSourceWithClips.nearAudioSource.PlayOneShot(clip);
        Plugin.ExtendedLogging($"Playing near audio clip: {clip.name}");
    }
    
    private void PlayFarAudioClip(AudioSourceWithClips audioSourceWithClips)
    {
        int randomIndex = Random.Range(0, audioSourceWithClips.farClips.Length);
        AudioClip clip = audioSourceWithClips.farClips[randomIndex];
        audioSourceWithClips.farAudioSource.PlayOneShot(clip);
        Plugin.ExtendedLogging($"Playing far audio clip: {clip.name}");
    }
}