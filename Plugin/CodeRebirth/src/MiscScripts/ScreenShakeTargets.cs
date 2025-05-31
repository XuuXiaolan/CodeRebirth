using UnityEngine;

namespace CodeRebirth.src.MiscScripts;

public class ScreenShakeTarget : MonoBehaviour
{
    [Tooltip("The target GameObject that will have it's position used for screen shake effects.")]
    public GameObject screenShakeTarget = null!;
    public AudioSource? audioSourceNear = null;
    public AudioClip[] nearAudioClips = [];
    public AudioSource? audioSourceFar = null;
    public AudioClip[] farAudioClips = [];
    
}