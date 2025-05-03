using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using PathfindingLib.API.SmartPathfinding;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;

namespace CodeRebirth.src.MiscScripts.PathFinding;
[RequireComponent(typeof(NavMeshAgent))]
public class SmartAgentNavigator : NetworkBehaviour
{
    [HideInInspector] public bool cantMove = false;
    [HideInInspector] public UnityEvent<bool> OnUseEntranceTeleport = new();
    [HideInInspector] public UnityEvent<bool> OnEnableOrDisableAgent = new();
    [HideInInspector] public UnityEvent<bool> OnEnterOrExitElevator = new();
    [HideInInspector] public EntranceTeleport lastUsedEntranceTeleport;

    private Vector3 pointToGo = Vector3.zero;
    private Coroutine? searchRoutine = null;

    private SmartPathTask? pathingTask = null;
    private SmartPathTask? checkPathsTask = null;
    private SmartPathTask? roamingTask = null;

    [HideInInspector]
    public NavMeshAgent agent = null!;
    [HideInInspector]
    public bool isOutside = true;
    [HideInInspector]
    public Coroutine? checkPathsRoutine = null;

    [Header("Search Algorithm")]
    [SerializeField]
    private float _nodeRemovalPrecision = 5f;
    [SerializeField]
    private bool _canWanderIntoOrOutOfInterior = false;
    [SerializeField]
    private SmartPathfindingLinkFlags _allowedLinks = SmartPathfindingLinkFlags.InternalTeleports | SmartPathfindingLinkFlags.Elevators | SmartPathfindingLinkFlags.MainEntrance | SmartPathfindingLinkFlags.FireExits;

    public enum GoToDestinationResult
    {
        Success,
        InProgress,
        Failure,
    }

    public void Awake()
    {
        agent = gameObject.GetComponent<NavMeshAgent>();
        SmartPathfinding.RegisterSmartAgent(agent);
    }

    public void SetAllValues(bool isOutside)
    {
        this.isOutside = isOutside;
    }

    private SmartPathfindingLinkFlags GetAllowedPathLinks()
    {
        return _allowedLinks;
    }

    private void UseTeleport(EntranceTeleport teleport)
    {
        if (teleport.exitPoint == null || !teleport.FindExitPoint())
            return;

        agent.Warp(teleport.exitPoint.position);
        SetThingOutsideServerRpc(new NetworkBehaviourReference(teleport));
    }

    public bool DoPathingToDestination(Vector3 destination)
    {
        if (searchRoutine != null)
        {
            StopSearchRoutine();
        }

        if (cantMove)
            return false;

        if (!agent.enabled)
        {
            HandleDisabledAgentPathing();
            return false;
        }
        GoToDestinationResult result = GoToDestination(destination);
        return result == GoToDestinationResult.Success || result == GoToDestinationResult.InProgress;
    }

    private GoToDestinationResult GoToDestination(Vector3 targetPosition)
    {
        var result = GoToDestinationResult.InProgress;

        if (pathingTask == null)
        {
            pathingTask = new SmartPathTask();
            pathingTask.StartPathTask(this.agent, this.transform.position, targetPosition, GetAllowedPathLinks());
        }

        if (!pathingTask.IsResultReady(0))
            return result;

        if (pathingTask.GetResult(0) is SmartPathDestination destination)
        {
            result = GoToSmartPathDestination(in destination) ? GoToDestinationResult.Success : GoToDestinationResult.Failure;
        }
        else
        {
            result = GoToDestinationResult.Failure;
        }

        pathingTask.StartPathTask(this.agent, this.transform.position, targetPosition, GetAllowedPathLinks());
        return result;
    }

    #region Destination Handling
    private bool GoToSmartPathDestination(in SmartPathDestination destination)
    {
        switch (destination.Type)
        {
            case SmartDestinationType.DirectToDestination:
                HandleDirectDestination(destination);
                break;
            case SmartDestinationType.InternalTeleport:
                HandleInternalTeleportDestination(destination);
                break;
            case SmartDestinationType.Elevator:
                HandleElevatorDestination(destination);
                break;
            case SmartDestinationType.EntranceTeleport:
                HandleEntranceTeleportDestination(destination);
                break;
            default:
                return false;
        }
        return true;
    }

    private void HandleDirectDestination(SmartPathDestination destination)
    {
        agent.SetDestination(destination.Position);
    }

    private void HandleInternalTeleportDestination(SmartPathDestination destination)
    {
        agent.SetDestination(destination.Position);

        if (Vector3.Distance(this.transform.position, destination.Position) >= 1f + agent.stoppingDistance)
            return;

        agent.Warp(destination.InternalTeleport!.Destination.position);
    }

    private void HandleElevatorDestination(SmartPathDestination destination)
    {
        agent.SetDestination(destination.Position);

        if (Vector3.Distance(this.transform.position, destination.Position) >= 1f + agent.stoppingDistance)
            return;

        destination.ElevatorFloor!.CallElevator();
    }

    private void HandleEntranceTeleportDestination(SmartPathDestination destination)
    {
        agent.SetDestination(destination.Position);

        if (Vector3.Distance(this.transform.position, destination.Position) >= 1f + agent.stoppingDistance)
            return;

        UseTeleport(destination.EntranceTeleport!);
    }
    #endregion

    public void StopAgent()
    {
        if (agent.enabled && agent.isOnNavMesh)
            agent.ResetPath();

        agent.velocity = Vector3.zero;
    }

    public void CheckPaths<T>(IEnumerable<(T, Vector3)> points, Action<List<T>> action)
    {
        if (checkPathsRoutine != null)
        {
            StopCoroutine(checkPathsRoutine);
        }
        checkPathsRoutine = StartCoroutine(CheckPathsCoroutine(points, action));
    }

    private IEnumerator CheckPathsCoroutine<T>(IEnumerable<(T, Vector3)> points, Action<List<T>> action)
    {
        var TList = new List<T>();
        checkPathsTask ??= new SmartPathTask();
        List<Vector3> pointsVectorList = points.Select(x => x.Item2).ToList();
        List<T> pointsTList = points.Select(x => x.Item1).ToList();
        checkPathsTask.StartPathTask(this.agent, this.transform.position, pointsVectorList, GetAllowedPathLinks());
        int listSize = pointsVectorList.Count;
        Plugin.ExtendedLogging($"Checking paths for {listSize} objects");
        yield return new WaitUntil(() => checkPathsTask.IsComplete);
        List<bool> checkPathsTaskResults = new();
        for (int i = 0; i < listSize; i++)
        {
            checkPathsTaskResults.Add(checkPathsTask.IsResultReady(i));
            if (!checkPathsTask.IsResultReady(i))
                continue;

            // Plugin.ExtendedLogging($"Checking result for task index: {i}, is result ready: {checkPathsTask.IsResultReady(i)}, result: {checkPathsTask.GetResult(i)}");
            if (checkPathsTask.GetResult(i) is not SmartPathDestination destination)
                continue;

            TList.Add(pointsTList[i]);
        }

        for (int i = 0; i < listSize; i++)
        {
            Plugin.ExtendedLogging($"Checking result for task index: {i}, is result ready: {checkPathsTaskResults[i]}");
        }

        action(TList);
        checkPathsRoutine = null;
    }

    private void HandleDisabledAgentPathing()
    {
        Vector3 targetPosition = pointToGo;
        float arcHeight = 10f;  // Adjusted arc height for a more pronounced arc
        float distanceToTarget = Vector3.Distance(transform.position, targetPosition);

        if (Vector3.Distance(transform.position, targetPosition) <= 1f)
        {
            OnEnableOrDisableAgent.Invoke(true);
            agent.enabled = true;
            agent.Warp(targetPosition);
            return;
        }

        // Calculate the new position in an arcing motion
        float normalizedDistance = Mathf.Clamp01(Vector3.Distance(transform.position, targetPosition) / distanceToTarget);
        Vector3 newPosition = Vector3.MoveTowards(transform.position, targetPosition, Time.deltaTime * 10f);
        newPosition.y += Mathf.Sin(normalizedDistance * Mathf.PI) * arcHeight;

        transform.SetPositionAndRotation(newPosition, Quaternion.LookRotation(targetPosition - transform.position));
    }

    private bool DetermineIfNeedToDisableAgent(Vector3 destination)
    {
        float distanceToDest = Vector3.Distance(transform.position, destination);
        if (distanceToDest <= agent.stoppingDistance + 5f)
        {
            return false;
        }

        if (!NavMesh.SamplePosition(destination, out NavMeshHit hit, 3, agent.areaMask))
        {
            return false;
        }
        Vector3 lastValidPoint = FindClosestValidPoint();
        agent.SetDestination(lastValidPoint);
        if (Vector3.Distance(agent.transform.position, lastValidPoint) <= agent.stoppingDistance)
        {
            pointToGo = hit.position;
            OnEnableOrDisableAgent.Invoke(false);
            agent.enabled = false;
            Plugin.ExtendedLogging($"Pathing to initial destination {destination} failed, going to fallback position {hit.position} instead.");
            return true;
        }

        return false;
    }

    public float CanPathToPoint(Vector3 startPos, Vector3 endPos)
    {
        NavMeshPath path = new();
        bool pathFound = NavMesh.CalculatePath(startPos, endPos, agent.areaMask, path);

        if (!pathFound || path.status != NavMeshPathStatus.PathComplete)
        {
            return -1;
        }

        float pathDistance = 0f;
        if (path.corners.Length > 1)
        {
            for (int i = 1; i < path.corners.Length; i++)
            {
                pathDistance += Vector3.Distance(path.corners[i - 1], path.corners[i]);
            }
        }
        // Plugin.ExtendedLogging($"[{this.gameObject.name}] Path distance: {pathDistance}");

        return pathDistance;
    }

    [ServerRpc(RequireOwnership = false)]
    public void SetThingOutsideServerRpc(NetworkBehaviourReference entranceTeleportReference)
    {
        SetThingOutsideClientRpc(entranceTeleportReference);
    }

    [ClientRpc]
    public void SetThingOutsideClientRpc(NetworkBehaviourReference entranceTeleportReference)
    {
        lastUsedEntranceTeleport = (EntranceTeleport)entranceTeleportReference;
        isOutside = !lastUsedEntranceTeleport.isEntranceToBuilding;
        OnUseEntranceTeleport.Invoke(isOutside);
    }

    private Vector3 FindClosestValidPoint()
    {
        return agent.pathEndPosition;
    }

    public void AdjustSpeedBasedOnDistance(float multiplierBoost)
    {
        float minDistance = 0f;
        float maxDistance = 40f;

        float minSpeed = 0f; // Speed when closest
        float maxSpeed = 10f; // Speed when farthest

        float clampedDistance = Mathf.Clamp(agent.remainingDistance, minDistance, maxDistance);
        float normalizedDistance = (clampedDistance - minDistance) / (maxDistance - minDistance);

        agent.speed = Mathf.Lerp(minSpeed, maxSpeed, normalizedDistance) * multiplierBoost;
    }

    public bool CurrentPathIsValid()
    {
        if (agent.path.status == NavMeshPathStatus.PathPartial || agent.path.status == NavMeshPathStatus.PathInvalid)
        {
            return false;
        }
        return true;
    }

    public void StartSearchRoutine(float radius)
    { // TODO: rework the search algorithm to use nodes and whatnot, similar to vanilla
        if (!agent.enabled)
            return;

        StopSearchRoutine();
        searchRoutine = StartCoroutine(SearchAlgorithm(radius));
    }

    public void StopSearchRoutine()
    {
        if (searchRoutine != null)
        {
            StopCoroutine(searchRoutine);
        }
        StopAgent();
        searchRoutine = null;
    }

    #region Search Algorithm
    private readonly List<Vector3> _positionsToSearch = new();
    private readonly List<Vector3> _roamingPointsVectorList = new();

    private IEnumerator SearchAlgorithm(float radius)
    {
        Plugin.ExtendedLogging($"Starting search routine for {this.gameObject.name} at {this.transform.position}");
        _positionsToSearch.Clear();
        yield return StartCoroutine(GetSetOfAcceptableNodesForRoaming(radius));
        while (true)
        {
            Vector3 positionToTravel = _positionsToSearch.FirstOrDefault();
            if (_positionsToSearch.Count == 0 || positionToTravel == Vector3.zero)
            {
                StartSearchRoutine(radius);
                yield break;
            }
            _positionsToSearch.RemoveAt(0);
            yield return StartCoroutine(ClearProximityNodes(_positionsToSearch, positionToTravel, _nodeRemovalPrecision));
            bool reachedDestination = false;
            while (!reachedDestination)
            {
                Plugin.ExtendedLogging($"{this.gameObject.name} Search: {positionToTravel}");
                GoToDestination(positionToTravel);
                yield return new WaitForSeconds(0.5f);

                if (!agent.enabled || Vector3.Distance(this.transform.position, positionToTravel) <= radius + agent.stoppingDistance || agent.velocity.magnitude <= 1f)
                {
                    reachedDestination = true;
                }
            }
        }
    }

    private IEnumerator GetSetOfAcceptableNodesForRoaming(float radius)
    {
        _roamingPointsVectorList.Clear();

        if (_canWanderIntoOrOutOfInterior)
        {
            _roamingPointsVectorList.AddRange(RoundManager.Instance.outsideAINodes.Select(x => x.transform.position));
            _roamingPointsVectorList.AddRange(RoundManager.Instance.insideAINodes.Select(x => x.transform.position));
        }
        else if (isOutside)
        {
            _roamingPointsVectorList.AddRange(RoundManager.Instance.outsideAINodes.Select(x => x.transform.position));
        }
        else
        {
            _roamingPointsVectorList.AddRange(RoundManager.Instance.insideAINodes.Select(x => x.transform.position));
        }

        roamingTask ??= new SmartPathTask();
        roamingTask.StartPathTask(this.agent, this.transform.position, _roamingPointsVectorList, GetAllowedPathLinks());
        int listSize = _roamingPointsVectorList.Count;
        Plugin.ExtendedLogging($"Checking paths for {listSize} objects");
        yield return new WaitUntil(() => roamingTask.IsComplete);
        List<bool> roamingPathsTaskResults = new();
        for (int i = 0; i < listSize; i++)
        {
            roamingPathsTaskResults.Add(roamingTask.IsResultReady(i));
            if (!roamingTask.IsResultReady(i))
            {
                Plugin.Logger.LogError($"Roaming task {i} is not ready");
                continue;
            }
            if (roamingTask.GetResult(i) is not SmartPathDestination destination)
                continue;

            if (roamingTask.GetPathLength(i) > radius)
                continue;

            _positionsToSearch.Add(destination.Position);
        }

        for (int i = 0; i < listSize; i++)
        {
            Plugin.ExtendedLogging($"Checking result for task index: {i}, is result ready: {roamingPathsTaskResults[i]}");
        }
    }

    private IEnumerator ClearProximityNodes(List<Vector3> positionsToSearch, Vector3 positionToTravel, float radius)
    {
        for (int i = positionsToSearch.Count - 1; i >= 0; i--)
        {
            if (Vector3.Distance(positionsToSearch[i], positionToTravel) <= radius)
            {
                positionsToSearch.RemoveAt(i);
            }
            yield return null;
        }
    }
    #endregion
}