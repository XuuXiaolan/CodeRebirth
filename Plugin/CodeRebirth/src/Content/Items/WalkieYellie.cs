using System.Collections;
using UnityEngine;

namespace CodeRebirth.src.Content.Items;
public class WalkieYellie : GrabbableObject
{
    [Header("Audio")]
    [SerializeField]
    private AudioSource _idleSource = null!;
    [SerializeField]
    private AudioSource _audioSource = null!;
    [SerializeField]
    private AudioClip _activateSound = null!;
    [SerializeField]
    private AudioClip _deactivateSound = null!;

    public override void ItemActivate(bool used, bool buttonDown = true) // look into audio reverb zones
    {
        base.ItemActivate(used, buttonDown);

        StartOfRound.Instance.RefreshPlayerVoicePlaybackObjects();
        Plugin.ExtendedLogging($"Letting go of thing: {!buttonDown}");
        Plugin.ExtendedLogging($"Is owner: {IsOwner}");
        playerHeldBy.activatingItem = buttonDown;
        if (buttonDown)
        {
            if (IsOwner)
            {
                SoundManager.Instance.echoEnabled = true;
                SoundManager.Instance.diageticMixer.SetFloat("EchoWetness", 1f);
            }
            else
            {
                StartCoroutine(ResetVoiceChatAudioSource(playerHeldBy.currentVoiceChatAudioSource, playerHeldBy.currentVoiceChatAudioSource.maxDistance));
                playerHeldBy.currentVoiceChatAudioSource.maxDistance = 999f;
            }
            _idleSource.Play();
            _audioSource.PlayOneShot(_activateSound);
        }
        else
        {
            if (IsOwner)
                SoundManager.Instance.SetEchoFilter(false);

            _idleSource.Stop();
            _audioSource.PlayOneShot(_deactivateSound);
        }

        if (!IsOwner) Plugin.ExtendedLogging($"Setting voice distance of playerHeldBy to: {playerHeldBy.currentVoiceChatAudioSource.maxDistance}");
    }

    private IEnumerator ResetVoiceChatAudioSource(AudioSource audioSource, float maxDistance)
    {
        yield return new WaitUntil(() => playerHeldBy == null || !playerHeldBy.activatingItem || isPocketed);
        audioSource.maxDistance = maxDistance;
    }
}