using System.Collections.Generic;
using UnityEngine;

namespace CodeRebirth.src.MiscScripts;

public class PlaySoundOnAnimation : MonoBehaviour
{
    [SerializeField]
    private List<AudioSourceWithClips> _audioSourcesWithClips = new List<AudioSourceWithClips>();

    public void PlaySelectSourceWithRandomSound(int index)
    {
        if (index < 0 || index >= _audioSourcesWithClips.Count)
        {
            Plugin.Logger.LogWarning($"Index {index} is out of range for audio sources with clips.");
            return;
        }
        _audioSourcesWithClips[index]._audioSource.PlayOneShot(_audioSourcesWithClips[index]._clips[UnityEngine.Random.Range(0, _audioSourcesWithClips[index]._clips.Length)]);
    }
}