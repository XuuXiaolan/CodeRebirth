using System;
using UnityEngine;

namespace CodeRebirth.src.MiscScripts;
[Serializable]
public class AudioSourceWithClips
{
    [Tooltip("Audio source that will play the clips.")]
    public AudioSource _audioSource = null!;

    [Tooltip("List of audio clips that can be played.")]
    public AudioClip[] _clips = null!;
}