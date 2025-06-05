using CodeRebirth.src.Util;
using CodeRebirth.src.Util.Extensions;
using UnityEngine;
using UnityEngine.Events;

namespace CodeRebirth.src.MiscScripts;

public class AmbientNoisePlayer : MonoBehaviour
{
    [Header("Audio")]
    [SerializeField]
    private AudioSource _ambientAudioSource = null!;
    [SerializeField]
    private AudioClipsWithTime _idleAudioClips = null!;

    [Header("Extras")]
    [SerializeField]
    private UnityEvent _onAmbientSoundPlayed = new();

    private bool _canPlaySounds = true;
    private float _idleTimer = 0f;

    public void Start()
    {
        _idleTimer = CodeRebirthUtils.Instance.CRRandom.NextFloat(_idleAudioClips.minTime, _idleAudioClips.maxTime);
        PlayRandomAmbientSound();
    }

    public void Update()
    {
        if (!_canPlaySounds)
            return;

        _idleTimer -= Time.deltaTime;
        if (_idleTimer > 0)
            return;

        PlayRandomAmbientSound();
    }

    private void PlayRandomAmbientSound()
    {
        if (_idleAudioClips.audioClips.Length <= 0)
            return;

        _idleTimer = CodeRebirthUtils.Instance.CRRandom.NextFloat(_idleAudioClips.minTime, _idleAudioClips.maxTime);
        _ambientAudioSource.PlayOneShot(_idleAudioClips.audioClips[CodeRebirthUtils.Instance.CRRandom.Next(_idleAudioClips.audioClips.Length)]);
        _onAmbientSoundPlayed.Invoke();
    }

    public void SetPlayable(bool isPlayable)
    {
        _canPlaySounds = isPlayable;
    }

    public void ResetAmbientTimer()
    {
        _idleTimer = CodeRebirthUtils.Instance.CRRandom.NextFloat(_idleAudioClips.minTime, _idleAudioClips.maxTime);
    }
    
    public void ForcePlayAmbientSound()
    {
        PlayRandomAmbientSound();
    }
}