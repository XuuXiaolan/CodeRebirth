using UnityEngine;

namespace CodeRebirth.src.MiscScripts;

public class ScreenShakeTarget : MonoBehaviour
{
    public GameObject screenShakeTarget = null!;
    public AudioSource? audioSourceNear = null;
    public AudioClip[] nearAudioClips = [];
    public AudioSource? audioSourceFar = null;
    public AudioClip[] farAudioClips = [];
    
}