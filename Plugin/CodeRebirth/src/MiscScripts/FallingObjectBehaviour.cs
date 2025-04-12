using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;

namespace CodeRebirth.src.MiscScripts;

public class FallingObjectBehaviour : NetworkBehaviour
{
    protected Vector3 _origin { get; private set; }
    protected Vector3 _target { get; private set; }
    protected Vector3 _normal { get; private set; }
    protected float _speed { get; private set; }

    public float Progress => _timeInAir / _travelTime;
    private float _timeInAir = 0;
    protected float _travelTime { get; private set; }
    private bool _isMoving = false;

    public AnimationCurve animationCurve = AnimationCurve.Linear(0, 0, 1, 1);
    
    [SerializeField]
    private UnityEvent _onImpact;
    
    protected virtual void Update()
    {
        if (!_isMoving)
            return;

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

    protected virtual float CalculateTravelTime(float distance)
    {
        return Mathf.Sqrt(2 * distance / _speed);  // Time to reach the target, adjusted for acceleration
    }

    public void StopMoving()
    {
        _isMoving = false;
    }

    [ClientRpc]
    public void SetupFallingObjectClientRPC(Vector3 origin, Vector3 target, float speed)
    {
        _origin = origin;
        _target = target;
        _speed = speed;
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