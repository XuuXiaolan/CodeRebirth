using Unity.Netcode;
using GameNetcodeStuff;
using UnityEngine;
using UnityEngine.AI;
using System.Linq;
using System.Collections;
using System;
using System.Collections.Generic;

namespace CodeRebirth.src.Content.Enemies;
public abstract class CodeRebirthEnemyAI : EnemyAI
{
    public EnemyAI? targetEnemy;
    private Coroutine? searchRoutine;
    private bool reachedDestination;
    private bool isSearching = false;
    [NonSerialized] public Dictionary<PlayerControllerB, Vector3> positionsOfPlayersBeforeTeleport = new();
    private EntranceTeleport lastUsedEntranceTeleport = null!;
    private Dictionary<EntranceTeleport, Transform[]> exitPoints = new();
    private MineshaftElevatorController? elevatorScript = null;
    private bool usingElevator = false;
    private Vector3 pointToGo = Vector3.zero;

    public override void Start()
    {
        base.Start();
        foreach (var player in StartOfRound.Instance.allPlayerScripts)
        {
            if (!player.isPlayerControlled) continue;
            positionsOfPlayersBeforeTeleport.Add(player, player.transform.position);
        }
        
        exitPoints = new();
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
        Plugin.ExtendedLogging(enemyType.enemyName + " Spawned.");
        GrabEnemyRarity(enemyType.enemyName);
    }

    public void GrabEnemyRarity(string enemyName)
    {
        // Search in OutsideEnemies
        var enemy = RoundManager.Instance.currentLevel.OutsideEnemies
            .OfType<SpawnableEnemyWithRarity>()
            .FirstOrDefault(x => x.enemyType.enemyName.Equals(enemyName)) ?? RoundManager.Instance.currentLevel.DaytimeEnemies
                .OfType<SpawnableEnemyWithRarity>()
                .FirstOrDefault(x => x.enemyType.enemyName.Equals(enemyName));

        // If not found in DaytimeEnemies, search in Enemies
        enemy ??= RoundManager.Instance.currentLevel.Enemies
                .OfType<SpawnableEnemyWithRarity>()
                .FirstOrDefault(x => x.enemyType.enemyName.Equals(enemyName));

        // Log the result
        if (enemy != null)
        {
            Plugin.ExtendedLogging(enemyName + " has Rarity: " + enemy.rarity.ToString());
        }
        else
        {
            Plugin.Logger.LogWarning("Enemy not found.");
        }
    }

    [ClientRpc]
    public void SetFloatAnimationClientRpc(string name, float value)
    {
        SetFloatAnimationOnLocalClient(name, value);
    }

    public void SetFloatAnimationOnLocalClient(string name, float value)
    {
        Plugin.ExtendedLogging(name + " " + value);
        creatureAnimator.SetFloat(name, value);
    }

    [ClientRpc]
    public void SetBoolAnimationClientRpc(int nameInt, bool active)
    {
        SetBoolAnimationOnLocalClient(nameInt, active);
    }

    public void SetBoolAnimationOnLocalClient(int intName, bool active)
    {
        creatureAnimator.SetBool(intName, active);
    }

    [ServerRpc(RequireOwnership = false)]
    public void TriggerAnimationServerRpc(string triggerName)
    {
        TriggerAnimationClientRpc(triggerName);
    }

    [ClientRpc]
    public void TriggerAnimationClientRpc(string triggerName)
    {
        TriggerAnimationOnLocalClient(triggerName);
    }

    public void TriggerAnimationOnLocalClient(string triggerName)
    {
        Plugin.ExtendedLogging(triggerName);
        creatureAnimator.SetTrigger(triggerName);
    }

    public void ToggleEnemySounds(bool toggle)
    {
        creatureSFX.enabled = toggle;
        creatureVoice.enabled = toggle;
    }

    [ClientRpc]
    public void ChangeSpeedClientRpc(float speed)
    {
        ChangeSpeedOnLocalClient(speed);
    }

    public void ChangeSpeedOnLocalClient(float speed)
    {
        agent.speed = speed;
    }

    public bool FindClosestPlayerInRange(float range)
    {
        PlayerControllerB? closestPlayer = null;
        float minDistance = float.MaxValue;

        foreach (PlayerControllerB player in StartOfRound.Instance.allPlayerScripts)
        {
            bool onSight = player.IsSpawned && player.isPlayerControlled && !player.isPlayerDead && !player.isInHangarShipRoom && EnemyHasLineOfSightToPosition(player.transform.position, 60f, range);
            if (!onSight) continue;

            float distance = Vector3.Distance(transform.position, player.transform.position);
            bool closer = distance < minDistance;
            if (!closer) continue;

            minDistance = distance;
            closestPlayer = player;
        }
        if (closestPlayer == null) return false;

        targetPlayer = closestPlayer;
        return true;
    }

    public bool EnemyHasLineOfSightToPosition(Vector3 pos, float width = 60f, float range = 20f, float proximityAwareness = 5f)
    {
        if (eye == null)
        {
            _ = transform;
        }
        else
        {
            _ = eye;
        }

        if (Vector3.Distance(eye!.position, pos) >= range || Physics.Linecast(eye.position, pos, StartOfRound.Instance.collidersAndRoomMaskAndDefault)) return false;

        Vector3 to = pos - eye.position;
        return Vector3.Angle(eye.forward, to) < width || Vector3.Distance(transform.position, pos) < proximityAwareness;
    }
    public bool IsPlayerReachable(PlayerControllerB PlayerToCheck)
    {
        Vector3 Position = RoundManager.Instance.GetNavMeshPosition(PlayerToCheck.transform.position, RoundManager.Instance.navHit, 2.7f);
        if (!RoundManager.Instance.GotNavMeshPositionResult)
        {
            Plugin.ExtendedLogging("Player Reach Test: No Navmesh position");
            return false; 
        }
        agent.CalculatePath(Position, agent.path);
        bool HasPath = (agent.path.status == NavMeshPathStatus.PathComplete);
        Plugin.ExtendedLogging($"Player Reach Test: {HasPath}");
        return HasPath;
    }

    public float PlayerDistanceFromShip(PlayerControllerB PlayerToCheck)
    {
        if(PlayerToCheck == null) return -1;
        float DistanceFromShip = Vector3.Distance(PlayerToCheck.transform.position, StartOfRound.Instance.shipBounds.transform.position);
        Plugin.ExtendedLogging($"PlayerNearShip check: {DistanceFromShip}");
        return DistanceFromShip;
    }

    private float DistanceFromPlayer(PlayerControllerB player, bool IncludeYAxis)
    {
        if (player == null) return -1f;
        if (IncludeYAxis)
        {
            return Vector3.Distance(player.transform.position, this.transform.position);
        }
        Vector2 PlayerFlatLocation = new Vector2(player.transform.position.x, player.transform.position.z);
        Vector2 EnemyFlatLocation = new Vector2(transform.position.x, transform.position.z);
        return Vector2.Distance(PlayerFlatLocation, EnemyFlatLocation);
    }

    public bool AnimationIsFinished(string AnimName)
    {
        if (!creatureAnimator.GetCurrentAnimatorStateInfo(0).IsName(AnimName))
        {
            Plugin.ExtendedLogging(__getTypeName() + ": Checking for animation " + AnimName + ", but current animation is " + creatureAnimator.GetCurrentAnimatorClipInfo(0)[0].clip.name);
            return true;
        }
        return (creatureAnimator.GetCurrentAnimatorStateInfo(0).normalizedTime >= 1f);
    }

    [ServerRpc(RequireOwnership = false)]
    public void SetTargetServerRpc(int PlayerID)
    {
        SetTargetClientRpc(PlayerID);
    }

    [ClientRpc]
    public void SetTargetClientRpc(int PlayerID)
    {
        if (PlayerID == -1)
        {
            targetPlayer = null;
            Plugin.ExtendedLogging($"Clearing target on {this}");
            return;
        }
        if (StartOfRound.Instance.allPlayerScripts[PlayerID] == null)
        {
            Plugin.ExtendedLogging($"Index invalid! {this}");
            return;
        }
        targetPlayer = StartOfRound.Instance.allPlayerScripts[PlayerID];
        Plugin.ExtendedLogging($"{this} setting target to: {targetPlayer.playerUsername}");
    }

    [ServerRpc(RequireOwnership = false)]
    public void SetEnemyTargetServerRpc(int enemyID)
    {
        SetEnemyTargetClientRpc(enemyID);
    }

    [ClientRpc]
    public void SetEnemyTargetClientRpc(int enemyID)
    {
        if (enemyID == -1)
        {
            targetEnemy = null;
            Plugin.ExtendedLogging($"Clearing Enemy target on {this}");
            return;
        }
        if (RoundManager.Instance.SpawnedEnemies[enemyID] == null)
        {
            Plugin.ExtendedLogging($"Enemy Index invalid! {this}");
            return;
        }
        targetEnemy = RoundManager.Instance.SpawnedEnemies[enemyID];
        Plugin.ExtendedLogging($"{this} setting target to: {targetEnemy.enemyType.enemyName}");
    }

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
            bool galCloserToTop = Vector3.Distance(transform.position, elevatorScript.elevatorTopPoint.position) < Vector3.Distance(transform.position, elevatorScript.elevatorBottomPoint.position);
            bool destinationCloserToTop = Vector3.Distance(destination, elevatorScript.elevatorTopPoint.position) < Vector3.Distance(destination, elevatorScript.elevatorBottomPoint.position);
            if (galCloserToTop != destinationCloserToTop)
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
            SetEnemyOutsideServerRpc(!isOutside);
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
    public void SetEnemyOutsideServerRpc(bool setOutside)
    {
        SetEnemyOutsideClientRpc(setOutside);
    }

    [ClientRpc]
    public void SetEnemyOutsideClientRpc(bool setOutisde)
    {
        this.SetEnemyOutside(setOutisde);
    }

    public void StartSearchRoutine(Vector3 position, float radius, LayerMask agentMask)
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
                SetDestinationToPosition(positionToTravel);
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