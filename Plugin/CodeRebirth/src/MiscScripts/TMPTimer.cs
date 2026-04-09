using System;
using Dawn.Utils;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;

namespace CodeRebirth.src.MiscScripts;

public class TMPTimer : MonoBehaviour
{
    [field: SerializeField]
    public TextMeshPro TimerText { get; private set; }
    [field: SerializeField]
    public BoundedRange TotalSecondsRange { get; private set; } = new BoundedRange(90f, 180f);
    [field: SerializeField]
    public bool StartOnAwake { get; private set; }
    [field: SerializeField]
    public AudioSource AudioSource { get; private set; }
    [field: SerializeField]
    public AudioClip SecondPassSound { get; private set; }
    [field: SerializeField]
    public float TimeToPlaySource { get; private set; }
    [field: SerializeField]
    public UnityEvent OnTimerEnd { get; private set; }

    private bool _timerIsActive = true;
    private float _currentTime;
    private bool _canPlaySound = true;
    private int _currentSecond = 0;

    private void Start()
    {
        if (StartOnAwake)
        {
            _timerIsActive = true;
        }
        else
        {
            _timerIsActive = false;
        }

        if (!NetworkManager.Singleton.IsServer)
        {
            return;
        }

        SyncTimerRpc(TotalSecondsRange.GetRandomInRange(new System.Random(StartOfRound.Instance.randomMapSeed + UnityEngine.Random.Range(0, 1000))));
    }

    [Rpc(SendTo.Everyone)]
    public void SyncTimerRpc(float TotalSeconds)
    {
        _currentSecond = Mathf.ClampToInt((long)TotalSeconds);
        _currentTime = TotalSeconds;
    }

    public void StartTimer()
    {
        _timerIsActive = true;
    }

    private void Update()
    {
        if (_timerIsActive)
        {
            _currentTime -= Time.deltaTime;
            if (_currentTime <= 0)
            {
                _timerIsActive = false;
                OnTimerEnd.Invoke();
                _currentTime = 0;
            }
        }

        TimeSpan time = TimeSpan.FromSeconds(_currentTime);
        int totalSecondClamped = Mathf.ClampToInt((long)time.TotalSeconds);
        if (totalSecondClamped < _currentSecond)
        {
            // second has passed
            AudioSource.PlayOneShot(SecondPassSound);
            _currentSecond = totalSecondClamped;
        }
        TimerText.text = time.ToString(@"mm\:ss\:ff");

        if (_canPlaySound && _currentSecond <= TimeToPlaySource && !AudioSource.isPlaying)
        {
            _canPlaySound = false;
            AudioSource.Play();
        }
    }
}