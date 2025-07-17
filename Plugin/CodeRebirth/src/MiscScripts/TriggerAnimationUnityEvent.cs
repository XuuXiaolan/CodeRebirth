using System;
using UnityEngine;
using UnityEngine.Events;

namespace CodeRebirth.src.MiscScripts;

[Serializable]
public class FloatIntStringObjectEvent : UnityEvent<float, int, string, UnityEngine.Object> { }

[Serializable]
public class AnimationEventParameters
{
    public float floatParameter;
    public int intParameter;
    public string stringParameter;
    public UnityEngine.Object objectReferenceParameter;
}

public class TriggerAnimationUnityEvent : MonoBehaviour
{
    [SerializeField]
    private FloatIntStringObjectEvent _animationUnityEvent = new();

    [SerializeField]
    private AnimationEventParameters animationEventParameters;

    public void TriggerScreenShake()
    {
        _animationUnityEvent.Invoke(animationEventParameters.floatParameter, animationEventParameters.intParameter, animationEventParameters.stringParameter, animationEventParameters.objectReferenceParameter);
    }
}