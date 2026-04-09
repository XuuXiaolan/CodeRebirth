using System;
using UnityEngine;

namespace CodeRebirth.src.Util.Timer;

public enum TimerExecutionTime
{
    Update,
    FixedUpdate,
    LateUpdate
}

public class TimerCreateParams(float duration, TimerExecutionTime executionTime, bool loop)
{
    public float duration = duration;
    public TimerExecutionTime executionTime = executionTime;
    public bool loop = loop;
}

public class Timer
{
    public float Elapsed { get; private set; }
    public float Duration { get; private set; }
    public bool IsPaused { get; private set; } = true;

    public bool IsFinished { get; private set; }

    public bool IsLooping { get; private set; }
    public bool IsRunning => !IsPaused && !IsFinished;

    public float NormalizedProgress => Mathf.Clamp01(Elapsed / Duration);

    public TimerExecutionTime executionTime;

    private Func<bool> _conditionalUpdate = () => true;

    public Func<bool> ConditionalUpdate
    {
        get => _conditionalUpdate;
        set => _conditionalUpdate = value ?? _conditionalUpdate;
    }

    public event Action<Timer> OnStart;
    public event Action<Timer> OnUpdate;
    public event Action<Timer> OnFinish;


    public Timer(TimerCreateParams createParams) : this(createParams.duration, createParams.executionTime, createParams.loop) { }

    public Timer(float duration, TimerExecutionTime executionTime, bool loop = false, bool autostart = false)
    {
        this.Duration = duration;
        this.executionTime = executionTime;

        IsLooping = loop;

        Elapsed = 0f;
        IsPaused = true;


        if (autostart) Start();
    }

    public static Timer CreateTimer(TimerCreateParams createParams, Action<Timer> onStart, Action<Timer> onUpdate, Action<Timer> onFinish)
    {
        var timer = new Timer(createParams);

        timer.OnStart += onStart;
        timer.OnUpdate += onUpdate;
        timer.OnFinish += onFinish;

        return timer;
    }

    public static Timer CreateAutoTimer(TimerCreateParams createParams, Action<Timer> onStart, Action<Timer> onUpdate, Action<Timer> onFinish)
    {
        var timer = CreateTimer(createParams, onStart, onUpdate, onFinish);

        timer.Start();

        return timer;
    }

    //starts the timer
    public void Start()
    {
        if (!IsPaused)
        {
            return;
        }
        IsPaused = false;
        IsFinished = false;


        OnStart?.Invoke(this);
        TimerExecutor.Instance?.RegisterTimer(this);
    }

    //pauses the timer without resetting the elapsed time
    public void Pause()
    {
        if (IsPaused)
        {
            return;
        }
        IsPaused = true;

    }

    public void Stop()
    {
        IsPaused = true;

        Elapsed = 0;
        TimerExecutor.Instance?.UnRegisterTimer(this);
    }

    public void Reset()
    {
        Elapsed = 0;
    }

    public void Restart()
    {
        Elapsed = 0;
        Start();
    }

    public void SetDuration(float duration)
    {
        Duration = duration;
    }

    public void Update(float deltaTime)
    {
        if (!_conditionalUpdate())
        {
            return;
        }

        if (IsPaused || IsFinished)
        {
            return;
        }

        Elapsed += deltaTime;
        if (Elapsed > Duration)
        {
            Elapsed = Duration;
        }
        OnUpdate?.Invoke(this);

        if (Elapsed >= Duration)
        {

            OnFinish?.Invoke(this);

            if (IsLooping)
            {
                Elapsed -= Duration;
                IsFinished = false;
                OnStart?.Invoke(this);
            }
            else
            {
                Elapsed = Duration;
                IsFinished = true;
                TimerExecutor.Instance?.UnRegisterTimer(this);
            }
        }
    }
}