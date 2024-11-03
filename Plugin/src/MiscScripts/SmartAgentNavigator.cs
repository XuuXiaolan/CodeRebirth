using System.Collections;
using System.Collections.Generic;
using GameNetcodeStuff;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;

namespace CodeRebirth.src.MiscScripts;
public class SmartAgentNavigator(NavMeshAgent agent) : NetworkBehaviour
{
    public UnityEvent<bool> OnUseEntranceTeleport;

    private readonly NavMeshAgent agent = agent;

    private Vector3 pointToGo;
    private bool isOutside;
    private bool usingElevator;
    private Coroutine? searchRoutine;
    private Coroutine? searchCoroutine;
    private bool isSearching;
    private bool reachedDestination;
    private MineshaftElevatorController elevatorScript;
    private EntranceTeleport lastUsedEntranceTeleport;
    private Dictionary<PlayerControllerB, Vector3> positionsOfPlayersBeforeTeleport;
    private Dictionary<EntranceTeleport, Vector3> exitPoints;

    public bool DoPathingToDestination(Vector3 destination, bool destinationIsInside, bool followingPlayer, PlayerControllerB? playerBeingFollowed)
    {
        if (!agent.enabled)
        {
            Vector3 targetPosition = pointToGo;
            float moveSpeed = 6f;  // Increased speed for a faster approach
            float arcHeight = 10f;  // Adjusted arc height for a more pronounced arc
            float distanceToTarget = Vector3.Distance(transform.position, targetPosition);

            // Calculate the new position in an arcing motion
            float normalizedDistance = Mathf.Clamp01(Vector3.Distance(transform.position, targetPosition) / distanceToTarget);
            Vector3 newPosition = Vector3.MoveTowards(transform.position, targetPosition, Time.deltaTime * moveSpeed);
            newPosition.y += Mathf.Sin(normalizedDistance * Mathf.PI) * arcHeight;

            transform.position = newPosition;
            transform.rotation = Quaternion.LookRotation(targetPosition - transform.position);
            if (Vector3.Distance(transform.position, targetPosition) <= 1f)
            {
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
        bool playerIsInElevator = elevatorScript != null && !elevatorScript.elevatorFinishedMoving && Vector3.Distance(destination, elevatorScript.elevatorInsidePoint.position) < 3f;
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
            agent.SetDestination(agent.pathEndPosition);
            if (Vector3.Distance(agent.transform.position, agent.pathEndPosition) <= agent.stoppingDistance)
            {
                agent.SetDestination(destination);
                if (!agent.CalculatePath(destination, path) || path.status != NavMeshPathStatus.PathComplete)
                {
                    Vector3 nearbyPoint;
                    if (NavMesh.SamplePosition(destination, out NavMeshHit hit, 5f, NavMesh.AllAreas))
                    {
                        nearbyPoint = hit.position;
                        pointToGo = nearbyPoint;
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
            EntranceTeleport? closestExitPoint = null;
            foreach (var exitpoint in exitPoints.Keys)
            {
                if (closestExitPoint == null || Vector3.Distance(positionOfPlayerBeforeTeleport, exitpoint.transform.position) < Vector3.Distance(positionOfPlayerBeforeTeleport, closestExitPoint.transform.position))
                {
                    closestExitPoint = exitpoint;
                }
            }
            if (closestExitPoint != null)
            {
                entranceTeleportToUse = closestExitPoint;
                destination = closestExitPoint.entrancePoint.transform.position;
                destinationAfterTeleport = closestExitPoint.exitPoint.transform.position;
            }
        }
        else
        {
            entranceTeleportToUse = lastUsedEntranceTeleport;
            destination = !isOutside ? lastUsedEntranceTeleport.exitPoint.transform.position : lastUsedEntranceTeleport.entrancePoint.transform.position;
            destinationAfterTeleport = !isOutside ? lastUsedEntranceTeleport.entrancePoint.transform.position : lastUsedEntranceTeleport.exitPoint.transform.position;
        }

        if (elevatorScript != null && NeedsElevator(destination, entranceTeleportToUse, elevatorScript))
        {
            UseTheElevator(elevatorScript);
            return;
        }

        if (Vector3.Distance(transform.position, destination) <= agent.stoppingDistance)
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
    private void AdjustSpeedBasedOnDistance(float distance)
    {
        float minDistance = 0f;
        float maxDistance = 40f;

        float minSpeed = 0f; // Speed when closest
        float maxSpeed = 20f; // Speed when farthest

        float clampedDistance = Mathf.Clamp(distance, minDistance, maxDistance);
        float normalizedDistance = (clampedDistance - minDistance) / (maxDistance - minDistance);

        agent.speed = Mathf.Lerp(minSpeed, maxSpeed, normalizedDistance);
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