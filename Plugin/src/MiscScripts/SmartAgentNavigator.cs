using System;
using System.Collections;
using System.Collections.Generic;
using CodeRebirth.src.Patches;
using GameNetcodeStuff;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;

namespace CodeRebirth.src.MiscScripts;
public class SmartAgentNavigator : NetworkBehaviour
{
    [NonSerialized] public bool cantMove = false;
    [NonSerialized] public UnityEvent<bool> OnUseEntranceTeleport = new();
    [NonSerialized] public UnityEvent<bool> OnEnableOrDisableAgent = new();

    private float nonAgentMovementSpeed = 10f;
    private NavMeshAgent agent = null!;
    private Vector3 pointToGo = Vector3.zero;
    [NonSerialized] public bool isOutside = true;
    private bool usingElevator = false;
    private Coroutine? searchRoutine = null;
    private Coroutine? searchCoroutine = null;
    private bool isSearching = false;
    private bool reachedDestination = false;
    private MineshaftElevatorController? elevatorScript = null;
    private EntranceTeleport? lastUsedEntranceTeleport = null;
    [NonSerialized] public Dictionary<PlayerControllerB, Vector3> positionsOfPlayersBeforeTeleport = new();
    private Dictionary<EntranceTeleport, Transform[]> exitPoints = new();

    public void Start()
    {
        agent = gameObject.GetComponent<NavMeshAgent>();
        PlayerControllerBPatch.smartAgentNavigators.Add(this);
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        PlayerControllerBPatch.smartAgentNavigators.Remove(this);
    }

    public void SetAllValues(bool isOutside)
    {
        this.isOutside = isOutside;
        positionsOfPlayersBeforeTeleport.Clear();
        foreach (var player in StartOfRound.Instance.allPlayerScripts)
        {
            positionsOfPlayersBeforeTeleport.Add(player, player.transform.position);
        }

        exitPoints.Clear();
        foreach (var exit in FindObjectsByType<EntranceTeleport>(FindObjectsInactive.Exclude, FindObjectsSortMode.InstanceID))
        {
            exitPoints.Add(exit, [exit.entrancePoint, exit.exitPoint]);
            if (exit.isEntranceToBuilding)
            {
                lastUsedEntranceTeleport = exit;
            }
            if (!exit.FindExitPoint())
            {
                Plugin.Logger.LogError("Something went wrong in the generation of the fire exits");
            }
        }
        elevatorScript = FindObjectOfType<MineshaftElevatorController>();
    }

    public void ResetAllValues()
    {
        exitPoints.Clear();
        positionsOfPlayersBeforeTeleport.Clear();
        lastUsedEntranceTeleport = null;
        elevatorScript = null;
    }

    public bool DoPathingToDestination(Vector3 destination, bool destinationIsInside, bool followingPlayer, PlayerControllerB? playerBeingFollowed)
    {
        if (!agent.enabled)
        {
            if (cantMove) return true;
            Vector3 targetPosition = pointToGo;
            float arcHeight = 10f;  // Adjusted arc height for a more pronounced arc
            float distanceToTarget = Vector3.Distance(transform.position, targetPosition);

            // Calculate the new position in an arcing motion
            float normalizedDistance = Mathf.Clamp01(Vector3.Distance(transform.position, targetPosition) / distanceToTarget);
            Vector3 newPosition = Vector3.MoveTowards(transform.position, targetPosition, Time.deltaTime * nonAgentMovementSpeed);
            newPosition.y += Mathf.Sin(normalizedDistance * Mathf.PI) * arcHeight;

            transform.position = newPosition;
            transform.rotation = Quaternion.LookRotation(targetPosition - transform.position);
            if (Vector3.Distance(transform.position, targetPosition) <= 1f)
            {
                OnEnableOrDisableAgent.Invoke(true);
                agent.enabled = true;
            }
            return true;
        }

        if ((isOutside && destinationIsInside) || (!isOutside && !destinationIsInside))
        {
            GoThroughEntrance(followingPlayer, playerBeingFollowed);
            return true;
        }

        if (!isOutside && elevatorScript != null && !usingElevator)
        {
            bool scriptCloserToTop = Vector3.Distance(transform.position, elevatorScript.elevatorTopPoint.position) < Vector3.Distance(transform.position, elevatorScript.elevatorBottomPoint.position);
            bool destinationCloserToTop = Vector3.Distance(destination, elevatorScript.elevatorTopPoint.position) < Vector3.Distance(destination, elevatorScript.elevatorBottomPoint.position);
            if (scriptCloserToTop != destinationCloserToTop)
            {
                UseTheElevator(elevatorScript);
                return true;
            }
        }

        bool playerIsInElevator = elevatorScript != null && !elevatorScript.elevatorFinishedMoving && Vector3.Distance(destination, elevatorScript.elevatorInsidePoint.position) < 7f;
        if (!usingElevator && !playerIsInElevator && DetermineIfNeedToDisableAgent(destination))
        {
            return true;
        }

        if (!usingElevator) agent.SetDestination(destination);
        if (usingElevator && elevatorScript != null) agent.Warp(elevatorScript.elevatorInsidePoint.position);
        return false;
    }

    private bool DetermineIfNeedToDisableAgent(Vector3 destination)
    {
        NavMeshPath path = new NavMeshPath();
        if ((!agent.CalculatePath(destination, path) || path.status == NavMeshPathStatus.PathPartial) && Vector3.Distance(transform.position, destination) > 7f)
        {
            Vector3 lastValidPoint = FindClosestValidPoint();
            agent.SetDestination(lastValidPoint);
            if (Vector3.Distance(agent.transform.position, lastValidPoint) <= agent.stoppingDistance)
            {
                agent.SetDestination(destination);
                if (!agent.CalculatePath(destination, path) || path.status != NavMeshPathStatus.PathComplete)
                {
                    Vector3 nearbyPoint;
                    if (NavMesh.SamplePosition(destination, out NavMeshHit hit, 5f, NavMesh.AllAreas))
                    {
                        nearbyPoint = hit.position;
                        pointToGo = nearbyPoint;
                        OnEnableOrDisableAgent.Invoke(false);
                        agent.enabled = false;
                    }
                }
            }
            return true;
        }
        return false;
    }

    private void GoThroughEntrance(bool followingPlayer, PlayerControllerB? playerBeingFollowed)
    {
        Vector3 destination = Vector3.zero;
        Vector3 destinationAfterTeleport = Vector3.zero;
        EntranceTeleport entranceTeleportToUse = null!;

        if (followingPlayer && playerBeingFollowed != null)
        {
            Vector3 positionOfPlayerBeforeTeleport = positionsOfPlayersBeforeTeleport[playerBeingFollowed];
            // Find the closest entrance to the player
            EntranceTeleport? closestExitPointToPlayer = null;
            foreach (var exitpoint in exitPoints.Keys)
            {
                if (closestExitPointToPlayer == null || Vector3.Distance(positionOfPlayerBeforeTeleport, exitpoint.transform.position) < Vector3.Distance(positionOfPlayerBeforeTeleport, closestExitPointToPlayer.transform.position))
                {
                    closestExitPointToPlayer = exitpoint;
                }
            }
            if (closestExitPointToPlayer != null)
            {
                entranceTeleportToUse = closestExitPointToPlayer;
                destination = closestExitPointToPlayer.entrancePoint.transform.position;
                destinationAfterTeleport = closestExitPointToPlayer.exitPoint.transform.position;
            }
        }
        else
        {
            if (lastUsedEntranceTeleport != null)
            {
                entranceTeleportToUse = lastUsedEntranceTeleport;
                destination = !isOutside ? lastUsedEntranceTeleport.exitPoint.transform.position : lastUsedEntranceTeleport.entrancePoint.transform.position;
                destinationAfterTeleport = !isOutside ? lastUsedEntranceTeleport.entrancePoint.transform.position : lastUsedEntranceTeleport.exitPoint.transform.position;
            }
            else
            {
                EntranceTeleport? closestExitPointToScript = null;
                foreach (var exitpoint in exitPoints.Keys)
                {
                    if (closestExitPointToScript == null || Vector3.Distance(this.transform.position, exitpoint.transform.position) < Vector3.Distance(this.transform.position, closestExitPointToScript.transform.position))
                    {
                        closestExitPointToScript = exitpoint;
                    }
                }
                if (closestExitPointToScript != null)
                {
                    entranceTeleportToUse = closestExitPointToScript;
                    destination = closestExitPointToScript.entrancePoint.transform.position;
                    destinationAfterTeleport = closestExitPointToScript.exitPoint.transform.position;
                }
            }
        }

        if (elevatorScript != null && NeedsElevator(destination, entranceTeleportToUse, elevatorScript))
        {
            UseTheElevator(elevatorScript);
            return;
        }

        float distanceToDestination = Vector3.Distance(transform.position, destination);
        if (distanceToDestination <= agent.stoppingDistance || (agent.velocity.sqrMagnitude <= 0.01f && distanceToDestination <= 10f))
        {
            lastUsedEntranceTeleport = entranceTeleportToUse;
            agent.Warp(destinationAfterTeleport);
            SetThingOutsideServerRpc(!isOutside);
        }
        else
        {
            agent.SetDestination(destination);
        }
    }

    private bool NeedsElevator(Vector3 destination, EntranceTeleport entranceTeleportToUse, MineshaftElevatorController elevatorScript)
    {
        // Determine if the elevator is needed based on destination proximity and current position
        bool nearMainEntrance = Vector3.Distance(destination, RoundManager.FindMainEntrancePosition(true, false)) < Vector3.Distance(destination, entranceTeleportToUse.transform.position);
        bool closerToTop = Vector3.Distance(transform.position, elevatorScript.elevatorTopPoint.position) < Vector3.Distance(transform.position, elevatorScript.elevatorBottomPoint.position);
        return !isOutside && ((nearMainEntrance && !closerToTop) || (!nearMainEntrance && closerToTop));
    }

    private void UseTheElevator(MineshaftElevatorController elevatorScript)
    {
        // Determine if we need to go up or down based on current position and destination
        bool goUp = Vector3.Distance(transform.position, elevatorScript.elevatorBottomPoint.position) < Vector3.Distance(transform.position, elevatorScript.elevatorTopPoint.position);
        // Check if the elevator is finished moving
        if (elevatorScript.elevatorFinishedMoving)
        {
            if (elevatorScript.elevatorDoorOpen)
            {
                // If elevator is not called yet and is at the wrong level, call it
                if (NeedToCallElevator(elevatorScript, goUp))
                {
                    elevatorScript.CallElevatorOnServer(goUp);
                    MoveToWaitingPoint(elevatorScript, goUp);
                    return;
                }
                // Move to the inside point of the elevator if not already there
                if (Vector3.Distance(transform.position, elevatorScript.elevatorInsidePoint.position) > 1f)
                {
                    agent.SetDestination(elevatorScript.elevatorInsidePoint.position);
                }
                else if (!usingElevator)
                {
                    // Press the button to start moving the elevator
                    elevatorScript.PressElevatorButtonOnServer(true);
                    StartCoroutine(StopUsingElevator(elevatorScript));
                }
            }
        }
        else
        {
            MoveToWaitingPoint(elevatorScript, goUp);
        }
    }

    private IEnumerator StopUsingElevator(MineshaftElevatorController elevatorScript)
    {
        usingElevator = true;
        yield return new WaitForSeconds(2f);
        yield return new WaitUntil(() => elevatorScript.elevatorDoorOpen && elevatorScript.elevatorFinishedMoving);
        Plugin.ExtendedLogging("Stopped using elevator");
        usingElevator = false;
    }

    private bool NeedToCallElevator(MineshaftElevatorController elevatorScript, bool needToGoUp)
    {
        return !elevatorScript.elevatorCalled && ((!elevatorScript.elevatorIsAtBottom && needToGoUp) || (elevatorScript.elevatorIsAtBottom && !needToGoUp));
    }

    private void MoveToWaitingPoint(MineshaftElevatorController elevatorScript, bool needToGoUp)
    {
        // Elevator is currently moving
        // Move to the appropriate waiting point (bottom or top)
        if (Vector3.Distance(transform.position, elevatorScript.elevatorInsidePoint.position) > 1f)
        {
            agent.SetDestination(needToGoUp ? elevatorScript.elevatorBottomPoint.position : elevatorScript.elevatorTopPoint.position);
        }
        else
        {
            // Wait at the inside point for the elevator to arrive
            agent.SetDestination(elevatorScript.elevatorInsidePoint.position);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void SetThingOutsideServerRpc(bool setOutside)
    {
        SetThingOutsideClientRpc(setOutside);
    }

    [ClientRpc]
    public void SetThingOutsideClientRpc(bool setOutside)
    {
        isOutside = setOutside;
        OnUseEntranceTeleport.Invoke(setOutside);
    }

    /// <summary>
    /// Finds the closest valid point from a partial path.
    /// </summary>
    /// <returns>The closest valid point to continue the journey from.</returns>
    private Vector3 FindClosestValidPoint()
    {
        return agent.pathEndPosition;
    }

    /// <summary>
    /// Warps the agent forward in its current direction until it lands on a valid NavMesh.
    /// </summary>
    /// <param name="originalDestination">The target destination outside the navmesh.</param>
    /// <returns>The point on the navmesh.</returns>
    private Vector3 WarpForwardUntilOnNavMesh(Vector3 originalDestination)
    {
        Vector3 warpPoint = originalDestination;
        Vector3 forwardDirection = agent.transform.forward; // Get the agent's current facing direction
        float warpDistance = 0.2f; // The distance to warp forward each time
        float maxWarpAttempts = 50; // Limit the number of warp attempts
        float navMeshCheckDistance = 1f; // The radius to check for a valid NavMesh

        for (int i = 0; i < maxWarpAttempts; i++)
        {
            Vector3 testPoint = warpPoint + forwardDirection * warpDistance * (i + 1);

            // Check if this new point is on the NavMesh
            if (NavMesh.SamplePosition(testPoint, out NavMeshHit hit, navMeshCheckDistance, NavMesh.AllAreas))
            {
                // Found a valid point on the NavMesh
                return hit.position;
            }
        }

        // If no valid point is found after multiple attempts, return zero vector
        Plugin.Logger.LogWarning("Unable to find valid point on NavMesh by warping forward.");
        return Vector3.zero;
    }

    /// <summary>
    /// Sets a destination and adjusts the agent's speed based on the distance to the target.
    /// The farther the distance, the faster the agent moves.
    /// </summary>
    /// <param name="destination">The target destination.</param>
    public void SetDestinationWithSpeedAdjustment(Vector3 destination)
    {
        agent.SetDestination(destination);
        AdjustSpeedBasedOnDistance(agent.remainingDistance);
    }

    /// <summary>
    /// Adjusts the agent's speed based on its remaining distance.
    /// The farther the distance, the faster the agent moves.
    /// </summary>
    /// <param name="distance">The distance to the target.</param>
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

    /// <summary>
    /// Cancels the current movement and stops the agent.
    /// </summary>
    public void StopNavigation()
    {
        if (agent != null)
        {
            agent.isStopped = true;
            agent.ResetPath();
        }
    }

    /// <summary>
    /// Teleports the agent to a specified location.
    /// </summary>
    /// <param name="location">The target location to warp to.</param>
    public void WarpToLocation(Vector3 location)
    {
        agent.Warp(location);
    }

    public void StartSearchRoutine(Vector3 position, float radius)
    {
        if (searchRoutine != null)
        {
            StopCoroutine(searchRoutine);
        }
        isSearching = true;
        searchRoutine = StartCoroutine(SearchAlgorithm(position, radius));
    }

    public void StopSearchRoutine()
    {
        isSearching = false;
        if (searchCoroutine != null)
        {
            StopCoroutine(searchRoutine);
        }
        searchRoutine = null;
    }

    private IEnumerator SearchAlgorithm(Vector3 position, float radius)
    {
        while (isSearching)
        {
            Vector3 positionToTravel = RoundManager.Instance.GetRandomNavMeshPositionInRadius(position, radius, default);
            Plugin.ExtendedLogging($"Search: {positionToTravel}");
            reachedDestination = false;

            while (!reachedDestination && isSearching)
            {
                agent.SetDestination(positionToTravel);
                yield return new WaitForSeconds(3f);

                if (Vector3.Distance(this.transform.position, positionToTravel) <= 10f || agent.velocity.magnitude <= 1f)
                {
                    reachedDestination = true;
                }
            }
        }

        searchRoutine = null; // Clear the coroutine reference when it finishes
    }
}