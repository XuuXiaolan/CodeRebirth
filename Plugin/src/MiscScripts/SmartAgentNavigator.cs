using System.Collections;
using UnityEngine;
using UnityEngine.AI;

namespace CodeRebirth.src.MiscScripts;
public class SmartAgentNavigator(NavMeshAgent agent) : MonoBehaviour
{
    private NavMeshAgent agent = agent;

    /// <summary>
    /// Sets the agent's destination and handles partial paths.
    /// If a partial path is detected, the agent moves to the closest valid point,
    /// then warps in the agent's forward direction until it lands on a valid navmesh.
    /// If the final destination is still part of a partial path, it repeats the process.
    /// </summary>
    /// <param name="destination">The target destination.</param>
    public IEnumerator NavigateWithPartialPath(Vector3 destination)
    {
        agent.SetDestination(destination);

        // Wait until the path has been computed
        yield return new WaitUntil(() => agent.hasPath);

        // Handle only partial paths, ignore invalid paths
        if (agent.pathStatus == NavMeshPathStatus.PathPartial)
        {
            // Move to the closest valid point first
            Vector3 closestValidPoint = FindClosestValidPoint(agent.path);
            if (closestValidPoint != Vector3.zero)
            {
                agent.SetDestination(closestValidPoint);

                // Wait until the agent reaches the closest valid point
                yield return new WaitUntil(() => agent.remainingDistance <= agent.stoppingDistance);

                // Now warp in the agent's forward direction until it lands on a valid NavMesh
                Vector3 forwardWarpPoint = WarpForwardUntilOnNavMesh(destination);
                if (forwardWarpPoint != Vector3.zero)
                {
                    agent.Warp(forwardWarpPoint);
                    agent.SetDestination(destination);

                    // Wait for the new path to be computed
                    yield return new WaitUntil(() => agent.hasPath);

                    // Check again if the new path is still partial, handle recursively
                    if (agent.pathStatus == NavMeshPathStatus.PathPartial)
                    {
                        Plugin.Logger.LogWarning("New destination also results in partial path. Attempting again...");
                        yield return StartCoroutine(NavigateWithPartialPath(destination));
                    }
                }
            }
            else
            {
                Plugin.Logger.LogWarning("No valid point found. Unable to complete path.");
            }
        }
        else if (agent.pathStatus == NavMeshPathStatus.PathInvalid)
        {
            Plugin.Logger.LogWarning("Path is invalid. No further navigation possible.");
        }
    }

    /// <summary>
    /// Finds the closest valid point from a partial path.
    /// </summary>
    /// <param name="path">The NavMeshPath containing the partial path.</param>
    /// <returns>The closest valid point to continue the journey from.</returns>
    private Vector3 FindClosestValidPoint(NavMeshPath path)
    {
        if (path.corners.Length > 0)
        {
            // The last valid point in the partial path
            return path.corners[^1];
        }
        // should i maybe be using Agent.pathEndPosition?
        return Vector3.zero;
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
    /// Navigates through a door or entrance, warping the agent from one point to another.
    /// </summary>
    /// <param name="insidePosition">The position inside the building/door.</param>
    /// <param name="outsidePosition">The position outside the building/door.</param>
    /// <param name="isInside">Indicates if the agent is inside.</param>
    public void GoThroughEntrance(Vector3 insidePosition, Vector3 outsidePosition, bool isInside)
    {
        Vector3 targetPosition = isInside ? insidePosition : outsidePosition;
        agent.SetDestination(targetPosition);

        // Wait until the agent reaches the target position
        if (Vector3.Distance(agent.transform.position, targetPosition) <= agent.stoppingDistance)
        {
            // Warp to the opposite position
            Vector3 warpPosition = isInside ? outsidePosition : insidePosition;
            agent.Warp(warpPosition);
        }
    }

    /// <summary>
    /// Checks if the agent's current path is valid and recalculates it if necessary.
    /// </summary>
    /// <param name="destination">The target destination.</param>
    public IEnumerator CheckAndRecalculatePath(Vector3 destination)
    {
        agent.SetDestination(destination);

        yield return new WaitUntil(() => agent.hasPath);

        if (agent.pathStatus == NavMeshPathStatus.PathInvalid)
        {
            Plugin.Logger.LogWarning("Path is invalid, recalculating...");
            agent.ResetPath();
            agent.SetDestination(destination);
        }
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
}