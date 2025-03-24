using System.Collections;
using UnityEngine;

namespace CodeRebirth.src.Util.Extensions;
public static class AudioSourceExtensions
{
    public static void PlayClip(this AudioSource source, AudioClip clip)
    {
        GameNetworkManager.Instance.localPlayerController.StartCoroutine(PutBackClip(source, clip, source.clip));
    }

    public static IEnumerator PutBackClip(AudioSource source, AudioClip newClip, AudioClip oldClip)
    {
        source.clip = newClip;
        source.Stop();
        source.Play();
        yield return new WaitForSeconds(source.clip.length);
        source.Stop();
        source.clip = oldClip;
    }
}