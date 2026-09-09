using System;
using UnityEngine;
using UnityEngine.Events;

namespace CodeRebirth.src.MiscScripts;

public class LightningTarget : MonoBehaviour, ILightningTarget
{
    public bool IsStrikeable => _isStrikeable();
    public Transform LightningStrikePoint => _lightningStrikePoint();

    private Func<bool> _isStrikeable;
    private Func<Transform> _lightningStrikePoint;

    [field: SerializeField]
    public UnityEvent OnLightningStrike { get; private set; }

    public void SetStrikeable(Func<bool> isStrikeable)
    {
        _isStrikeable = isStrikeable;
    }

    public void SetLightningStrikePoint(Func<Transform> lightningStrikePoint)
    {
        _lightningStrikePoint = lightningStrikePoint;
    }

    public void AddToLightningStrikeEvent(UnityAction call)
    {
        OnLightningStrike.AddListener(call);
    }
}