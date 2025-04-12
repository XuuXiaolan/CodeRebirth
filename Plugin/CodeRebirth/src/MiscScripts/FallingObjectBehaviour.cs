using System;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.VFX.Utility;

namespace CodeRebirth.src.MiscScripts;

public abstract class FallingObjectBehaviour : NetworkBehaviour
{
    protected Vector3 _origin { get; private set; }
    protected Vector3 _target { get; private set; }
    protected Vector3 _normal { get; private set; }
    
    public float Progress => _timeInAir / _travelTime;
    private float _timeInAir = 0;
    protected float _travelTime { get; private set; }
    private bool _isMoving = false;

    public AnimationCurve animationCurve = AnimationCurve.Linear(0, 0, 1, 1);
    
    [SerializeField]
    private UnityEvent _onImpact;
    
    protected virtual void Update()
    {
        if(!_isMoving)
        MoveObject();
    }

    protected void MoveObject()
    {
        _timeInAir += Time.deltaTime;
        
        float progress = Progress;
        if (progress >= 1.0f)
        {
            transform.position = _target;
            _isMoving = false;
            OnImpact();
            
            return;
        }

        Vector3 nextPosition = Vector3.Lerp(_origin, _target, animationCurve.Evaluate(progress));
        transform.position = nextPosition;
    }

    protected virtual void OnImpact()
    {
        _onImpact.Invoke();
    }
    
    protected virtual void OnSetup() {}

    // should probably be changed from abstract to virtual but oh well
    protected abstract float CalculateTravelTime(float distance);

    public void StopMoving()
    {
        _isMoving = false;
    }

    [ClientRpc]
    public void SetupFallingObjectClientRPC(Vector3 origin, Vector3 target)
    {
        _origin = _origin;
        _target = _target;
        float distance = Vector3.Distance(_origin, _target);
        Ray ray = new Ray(_origin, _target - _origin);
        Physics.Raycast(ray, out RaycastHit hit, distance + 5f, StartOfRound.Instance.collidersAndRoomMask, QueryTriggerInteraction.Ignore);
        Plugin.ExtendedLogging($"Raycast hit: {hit.point} with normal: {hit.normal}");
        _target = hit.point;
        _normal = hit.normal;
        distance = Vector3.Distance(_origin, _target);
        _travelTime = CalculateTravelTime(distance);
        
        _isMoving = true;
        transform.LookAt(_target);
        OnSetup();
    }
}