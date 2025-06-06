using System.Collections.Generic;
using CodeRebirth.src.Util;
using UnityEngine;
using UnityEngine.Events;

namespace CodeRebirth.src.MiscScripts;

public class ReactToVehicleCollision : MonoBehaviour
{
    [SerializeField]
    private UnityEvent _onVehicleCollision = new();

    private int _obstacleId = -1;
    private static int _nextObstacleId = 1;
    private static readonly Dictionary<int, ReactToVehicleCollision> _reactToVehicleCollisions = new();

    private void Awake()
    {
        _obstacleId = _nextObstacleId;
        _nextObstacleId++;
        _reactToVehicleCollisions.Add(_obstacleId, this);
        Plugin.ExtendedLogging($"ReactToVehicleCollision: Registered vehicle reaction with ID {_obstacleId} at position {transform.position}");
    }

    public void OnDestroy()
    {
        if (_reactToVehicleCollisions.ContainsKey(_obstacleId))
        {
            _reactToVehicleCollisions.Remove(_obstacleId);
            Plugin.ExtendedLogging($"ReactToVehicleCollision: Unregistered vehicle reaction with ID {_obstacleId}");
        }
    }

    public void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent(out VehicleController vehicle) && vehicle.IsOwner && vehicle.averageVelocity.magnitude > 5f && Vector3.Angle(vehicle.averageVelocity, base.transform.position - vehicle.mainRigidbody.position) < 80f)
        {
            CodeRebirthUtils.Instance.ReactToVehicleCollisionServerRpc(_obstacleId);
            vehicle.CarReactToObstacle(vehicle.mainRigidbody.position - base.transform.position, base.transform.position, Vector3.zero, CarObstacleType.Object, 1f, null, false);
        }
    }

    public void InvokeCollisionEvent()
    {
        _onVehicleCollision.Invoke();
    }

    public static bool TryGetById(int obstacleId, out ReactToVehicleCollision? result)
    {
        return _reactToVehicleCollisions.TryGetValue(obstacleId, out result);
    }
}