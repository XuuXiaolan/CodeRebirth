using CodeRebirth.src.Util.Extensions;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;

namespace CodeRebirth.src.MiscScripts;
public enum Direction
{
    East,
    West,
    North,
    South,
}

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

    public Vector3 CalculateRandomSkyOrigin(Direction direction, Vector3 target, System.Random random)
    {
        float x = 0, z = 0;
        float distanceX = random.NextFloat(250, 500);
        float distanceZ = random.NextFloat(250, 500);

        switch (direction)
        {
            case Direction.East:
                x = distanceX;  // Move east
                break;
            case Direction.West:
                x = -distanceX; // Move west
                break;
            case Direction.North:
                z = distanceZ;  // Move north
                break;
            case Direction.South:
                z = -distanceZ; // Move south
                break;
        }

        float y = random.NextFloat(600, 900); // Fixed vertical range

        return target + new Vector3(x, y, z);
    }

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

    protected virtual void OnSetup() { }

    protected virtual float CalculateTravelTime(float distance)
    {
        return Mathf.Sqrt(2 * distance / _speed);  // Time to reach the target, adjusted for acceleration
    }

    public void StopMoving()
    {
        _isMoving = false;
    }

    [ServerRpc(RequireOwnership = false)]
    public void SetupFallingObjectServerRpc(Vector3 origin, Vector3 target, float speed)
    {
        SetupFallingObjectClientRpc(origin, target, speed);
    }

    [ClientRpc]
    public void SetupFallingObjectClientRpc(Vector3 origin, Vector3 target, float speed)
    {
        SetupFallingObject(origin, target, speed);
    }

    public void SetupFallingObject(Vector3 origin, Vector3 target, float speed)
    {
        _origin = origin;
        _target = target;
        _speed = speed;
        float distance = Vector3.Distance(_origin, _target);
        Ray ray = new Ray(_origin, _target - _origin);
        Physics.Raycast(ray, out RaycastHit hit, distance + 5f, StartOfRound.Instance.collidersAndRoomMaskAndDefault, QueryTriggerInteraction.Ignore);
        Plugin.ExtendedLogging($"Raycast started at: {_origin} and wanted to end at: {_target}, hit: {hit.point} with normal: {hit.normal}");
        _target = hit.point;
        _normal = hit.normal;
        distance = Vector3.Distance(_origin, _target);
        _travelTime = CalculateTravelTime(distance);
        Plugin.ExtendedLogging($"Travel Time: {_travelTime}");

        _isMoving = true;
        transform.LookAt(_target);
        OnSetup();
    }
}