using System;
using System.Collections.Generic;
using Dawn.Utils;
using UnityEngine;

namespace CodeRebirth.src.Util.Timer;

public class TimerExecutor : Singleton<TimerExecutor>
{
    public readonly HashSet<Timer> ActiveTimers = new();

    public readonly List<Timer> UpdateTimers = new();
    public readonly List<Timer> FixedUpdateTimers = new();
    public readonly List<Timer> LateUpdateTimers = new();

    /*[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    public static void EnsureExists()
    {
        if (Instance == null)
        {
            var go = new GameObject("Timer Executor");
            DontDestroyOnLoad(go);
            go.AddComponent<TimerExecutor>();
        }
    }*/

    public void RegisterTimer(Timer timer)
    {
        if (ActiveTimers.Add(timer))
        {
            switch (timer.executionTime)
            {
                case TimerExecutionTime.Update:
                    UpdateTimers.Add(timer);
                    break;
                case TimerExecutionTime.FixedUpdate:
                    FixedUpdateTimers.Add(timer);
                    break;
                case TimerExecutionTime.LateUpdate:
                    LateUpdateTimers.Add(timer);
                    break;
            }
        }
    }

    public void UnRegisterTimer(Timer timer)
    {
        ActiveTimers.Remove(timer);
        UpdateTimers.Remove(timer);
        FixedUpdateTimers.Remove(timer);
        LateUpdateTimers.Remove(timer);
    }

    private void Update() => RunTimers(UpdateTimers, Time.deltaTime);
    private void FixedUpdate() => RunTimers(FixedUpdateTimers, Time.fixedDeltaTime);
    private void LateUpdate() => RunTimers(LateUpdateTimers, Time.deltaTime);


    private void RunTimers(List<Timer> timerList, float delta)
    {
        for (int i = timerList.Count - 1; i >= 0; i--)
        {
            var t = timerList[i];
            try
            {
                t.Update(delta);

                if (t.IsFinished)
                {
                    UnRegisterTimer(t);
                }
            }
            catch (Exception e)
            {
                Plugin.Logger.LogError($"[TimerSystem]: {t} failed during update: {e.Message}\n{e.StackTrace}");
                UnRegisterTimer(t);
            }

        }
    }
}