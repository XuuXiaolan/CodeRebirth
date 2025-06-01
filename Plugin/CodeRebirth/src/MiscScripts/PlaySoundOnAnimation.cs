using UnityEngine;

namespace CodeRebirth.src.MiscScripts;

public class PlaySoundOnAnimation : MonoBehaviour
{
    [SerializeField]
    private AudioSource _soundSource = null!;
    [SerializeField]
    private AudioClip[] _clips = null!;
    [SerializeField]
    private AudioClip[] _specialClips = null!;

    public void PlayRandomSound()
    {
        _soundSource.PlayOneShot(_clips[Random.Range(0, _clips.Length)]);
    }

    public void PlaySelectSpecialSound(int index)
    {
        _soundSource.PlayOneShot(_specialClips[index]);
    }
}