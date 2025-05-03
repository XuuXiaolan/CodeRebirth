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

    private float nonAgentMovementSpeed = 10f;
    private Vector3 pointToGo = Vector3.zero;
    private Coroutine? searchRoutine = null;
    private bool isSearching = false;
    private bool reachedDestination = false;

    private SmartPathTask? task = null;
    private SmartPathTask? checkPathsTask = null;

    [HideInInspector]
    public NavMeshAgent agent = null!;
    [HideInInspector]
    public bool isOutside = true;
    [HideInInspector]
    public Coroutine? checkPathsRoutine = null;

    public SmartPathfindingLinkFlags allowedLinks = SmartPathfindingLinkFlags.InternalTeleports | SmartPathfindingLinkFlags.Elevators | SmartPathfindingLinkFlags.MainEntrance | SmartPathfindingLinkFlags.FireExits;
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
        return allowedLinks;
    }

    private void UseTeleport(EntranceTeleport teleport)
    {
        if (teleport.exitPoint == null || !teleport.FindExitPoint())
            return;

        agent.Warp(teleport.exitPoint.position);
        SetThingOutsideServerRpc(new NetworkBehaviourReference(teleport));
    }

    private GoToDestinationResult GoToDestination(Vector3 targetPosition)
    {
        var result = GoToDestinationResult.InProgress;

        if (task == null)
        {
            task = new SmartPathTask();
            task.StartPathTask(this.agent, this.transform.position, targetPosition, GetAllowedPathLinks());
        }

        if (!task.IsResultReady(0))
            return result;

        if (task.GetResult(0) is SmartPathDestination destination)
        {
            result = GoToSmartPathDestination(in destination) ? GoToDestinationResult.Success : GoToDestinationResult.Failure;
        }
        else
        {
            result = GoToDestinationResult.Failure;
        }

        task.StartPathTask(this.agent, this.transform.position, targetPosition, GetAllowedPathLinks());
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

    public bool DoPathingToDestination(Vector3 destination)
    {
        if (isSearching)
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
        for (int i = 0; i < listSize; i++)
        {
            // Plugin.ExtendedLogging($"Checking result for task index: {i}, is result ready: {checkPathsTask.IsResultReady(i)}, result: {checkPathsTask.GetResult(i)}");
            if (checkPathsTask.GetResult(i) is not SmartPathDestination destination)
                continue;

            TList.Add(pointsTList[i]);
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
        Vector3 newPosition = Vector3.MoveTowards(transform.position, targetPosition, Time.deltaTime * nonAgentMovementSpeed);
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

    public void StartSearchRoutine(Vector3 position, float radius)
    { // TODO: rework the search algorithm to use nodes and whatnot, similar to vanilla
        if (!agent.enabled)
            return;

        StopSearchRoutine();
        isSearching = true;
        searchRoutine = StartCoroutine(SearchAlgorithm(position, radius));
    }

    public void StopSearchRoutine()
    {
        isSearching = false;
        if (searchRoutine != null)
        {
            StopCoroutine(searchRoutine);
        }
        StopAgent();
        searchRoutine = null;
    }

    private IEnumerator SearchAlgorithm(Vector3 position, float radius)
    {
        while (isSearching)
        {
            Vector3 positionToTravel = RoundManager.Instance.GetRandomNavMeshPositionInRadius(position, radius, default);
            // Plugin.ExtendedLogging($"Search: {positionToTravel}");
            reachedDestination = false;

            while (!reachedDestination && isSearching)
            {
                Plugin.ExtendedLogging($"{this} Search: {positionToTravel}");
                agent.SetDestination(positionToTravel);
                yield return new WaitForSeconds(3f);

                if (!agent.enabled || Vector3.Distance(this.transform.position, positionToTravel) <= 10f || agent.velocity.magnitude <= 1f)
                {
                    reachedDestination = true;
                }
            }
        }

        searchRoutine = null;
    }
}