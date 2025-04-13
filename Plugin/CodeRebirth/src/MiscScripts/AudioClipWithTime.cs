using System;
using UnityEngine;

namespace CodeRebirth.src.MiscScripts;
[Serializable]
public class AudioClipsWithTime
{
    public AudioClip[] audioClips;
    public float minTime;
    public float maxTime;
}