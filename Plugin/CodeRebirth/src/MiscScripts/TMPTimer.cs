using System;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

namespace CodeRebirth.src.MiscScripts;
public class TMPTimer : MonoBehaviour
{
    [field: SerializeField]
    public TextMeshPro TimerText { get; private set; }
    [field: SerializeField]
    public int TotalSeconds { get; private set; }
    [field: SerializeField]
    public bool StartOnAwake { get; private set; }
    [field: SerializeField]
    public AudioSource AudioSource { get; private set; }
    [field: SerializeField]
    public float TimeToPlaySource { get; private set; }
    [field: SerializeField]
    public UnityEvent OnTimerEnd { get; private set; }

    private bool _timerIsActive = true;
    private float _currentTime;
    private bool _canPlaySound = true;

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
        TimerText.text = time.ToString(@"mm\:ss\:fff");

        if (time < TimeSpan.FromSeconds(TimeToPlaySource) && !AudioSource.isPlaying)
        {
            _canPlaySound = false;
            AudioSource.Play();
        }
        // _timerText.text = time.Minutes.ToString() + " : " + time.Seconds.ToString() + " : " + time.Milliseconds.ToString();
    }
}