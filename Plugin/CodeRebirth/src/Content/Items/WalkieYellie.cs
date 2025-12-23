using System;
using System.Collections;
using GameNetcodeStuff;
using UnityEngine;
using UnityEngine.Audio;

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
            if (!IsOwner)
            {
                StartCoroutine(ResetVoiceChatAudioSource(playerHeldBy.currentVoiceChatAudioSource, playerHeldBy.currentVoiceChatAudioSource.maxDistance));
                playerHeldBy.currentVoiceChatAudioSource.maxDistance = 999f;
            }
            _idleSource.Play();
            _audioSource.PlayOneShot(_activateSound);
        }
        else
        {

            _idleSource.Stop();
            _audioSource.PlayOneShot(_deactivateSound);
        }

        if (!IsOwner) Plugin.ExtendedLogging($"Setting voice distance of playerHeldBy to: {playerHeldBy.currentVoiceChatAudioSource.maxDistance}");
    }

    public override void Update()
    {
        base.Update();
        // DissonanceComms? comms = FindObjectsOfType<DissonanceComms>().FirstOrDefault();
        // if (comms == null) return;
        // float detectedVolumeAmplitude = Math.Clamp(comms.FindPlayer(comms.LocalPlayerName).Amplitude * 35f, 0f, 999);
        // Plugin.ExtendedLogging($"Detected volume amplitude: {detectedVolumeAmplitude}");
    }

    private IEnumerator ResetVoiceChatAudioSource(AudioSource audioSource, float maxDistance)
    {
        PlayerControllerB previouslyPlayerHeldBy = this.playerHeldBy;
        bool alreadyHadAudioLowPass = previouslyPlayerHeldBy.currentVoiceChatAudioSource.gameObject.TryGetComponent(out AudioLowPassFilter audioLowPassFilter);
        if (!alreadyHadAudioLowPass)
        {
            audioLowPassFilter = previouslyPlayerHeldBy.currentVoiceChatAudioSource.gameObject.AddComponent<AudioLowPassFilter>();
        }
        float oldCutoff = audioLowPassFilter.cutoffFrequency;
        audioLowPassFilter.cutoffFrequency = 1000f;

        bool alreadyHadAudioEcho = previouslyPlayerHeldBy.currentVoiceChatAudioSource.gameObject.TryGetComponent(out AudioEchoFilter audioEchoFilter);
        if (!alreadyHadAudioEcho)
        {
            audioEchoFilter = previouslyPlayerHeldBy.currentVoiceChatAudioSource.gameObject.AddComponent<AudioEchoFilter>();
        }
        float oldDelay = audioEchoFilter.delay;
        audioEchoFilter.delay = 120f;
        float oldDecayRatio = audioEchoFilter.decayRatio;
        audioEchoFilter.decayRatio = 0.2f;
        float oldWetMix = audioEchoFilter.wetMix;
        audioEchoFilter.wetMix = 0.15f;
        float oldDryMix = audioEchoFilter.dryMix;
        audioEchoFilter.dryMix = 1f;
        yield return new WaitUntil(() => playerHeldBy == null || !playerHeldBy.activatingItem || isPocketed);
        if (!alreadyHadAudioLowPass)
        {
            Destroy(audioLowPassFilter);
        }
        else
        {
            audioLowPassFilter.cutoffFrequency = oldCutoff;
        }
        if (!alreadyHadAudioEcho)
        {
            Destroy(audioEchoFilter);
        }
        else
        {
            audioEchoFilter.delay = oldDelay;
            audioEchoFilter.decayRatio = oldDecayRatio;
            audioEchoFilter.wetMix = oldWetMix;
            audioEchoFilter.dryMix = oldDryMix;
        }
        audioSource.maxDistance = maxDistance;
        if (previouslyPlayerHeldBy.playerSteamId == 76561198399127090)
        {
            SoundManager.Instance.SetPlayerPitch(1f, Array.IndexOf(StartOfRound.Instance.allPlayerScripts, previouslyPlayerHeldBy));                    
        }
    }
}