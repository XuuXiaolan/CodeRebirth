using System.Collections;
using CodeRebirth.src.Util;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Video;

namespace CodeRebirth.src.Content.Items;
public class CasettePlayer : MonoBehaviour
{
    [SerializeField]
    private VideoPlayer _videoPlayer = null!;
    [SerializeField]
    private Transform _tapeRespawnTransform = null!;

    [SerializeField]
    private AudioSource _generalAudioSource = null!;
    [SerializeField]
    private AudioSource _projectorAudioSource = null!;
    [SerializeField]
    private AudioClip _tapeInsertSound = null!;
    [SerializeField]
    private AudioClip _tapeEjectSound = null!;

    private bool _playing = false;

    public IEnumerator PlayTape(VideoClip videoClip, CasetteTape casetteTapeUsed)
    {
        if (videoClip == null)
            yield break;

        if (_playing)
            yield break;

        _videoPlayer.Stop();
        _playing = true;
        string itemName = casetteTapeUsed.itemProperties.itemName;
        if (casetteTapeUsed.playerHeldBy != null && casetteTapeUsed.isHeld && casetteTapeUsed.playerHeldBy.currentlyHeldObjectServer == casetteTapeUsed)
        {
            casetteTapeUsed.playerHeldBy.DespawnHeldObject();
        }
        _generalAudioSource.PlayOneShot(_tapeInsertSound);
        yield return new WaitForSeconds(_tapeInsertSound.length);
        _videoPlayer.audioOutputMode = VideoAudioOutputMode.AudioSource;
        _videoPlayer.EnableAudioTrack(0, true);
        _videoPlayer.SetTargetAudioSource(0, _projectorAudioSource);
        _videoPlayer.controlledAudioTrackCount = 1;
        _videoPlayer.clip = videoClip;
        _videoPlayer.Play();
        yield return new WaitUntil(() => _videoPlayer.isPlaying);
        yield return new WaitUntil(() => !_videoPlayer.isPlaying);
        _generalAudioSource.PlayOneShot(_tapeEjectSound);
        yield return new WaitForSeconds(_tapeEjectSound.length);
        _playing = false;
        if (!NetworkManager.Singleton.IsServer)
            yield break;

        Item item = CodeRebirthRegistry.RegisteredCRItems.GetCRItemDefinitionWithItemName(itemName)!.item;
        CodeRebirthUtils.Instance.SpawnScrap(item, _tapeRespawnTransform.position, false, true, 0);
    }
}