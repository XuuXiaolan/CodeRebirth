using Unity.Netcode;
using GameNetcodeStuff;
using UnityEngine;
using UnityEngine.AI;
using System.Linq;
using System.Collections;

namespace CodeRebirth.src.Content.Enemies;
public abstract class CodeRebirthEnemyAI : EnemyAI
{
    public EnemyAI? targetEnemy;
    private Coroutine? searchRoutine;
    private bool reachedDestination;
    private bool isSearching = false;

    public override void Start()
    {
        base.Start();
        Plugin.ExtendedLogging(enemyType.enemyName + " Spawned.");
        GrabEnemyRarity(enemyType.enemyName);
    }

    public void GrabEnemyRarity(string enemyName)
    {
        // Search in OutsideEnemies
        var enemy = RoundManager.Instance.currentLevel.OutsideEnemies
            .OfType<SpawnableEnemyWithRarity>()
            .FirstOrDefault(x => x.enemyType.enemyName.Equals(enemyName));

        // If not found in OutsideEnemies, search in DaytimeEnemies
        if (enemy == null)
        {
            enemy = RoundManager.Instance.currentLevel.DaytimeEnemies
                .OfType<SpawnableEnemyWithRarity>()
                .FirstOrDefault(x => x.enemyType.enemyName.Equals(enemyName));
        }

        // If not found in DaytimeEnemies, search in Enemies
        if (enemy == null)
        {
            enemy = RoundManager.Instance.currentLevel.Enemies
                .OfType<SpawnableEnemyWithRarity>()
                .FirstOrDefault(x => x.enemyType.enemyName.Equals(enemyName));
        }

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
    public void SetBoolAnimationClientRpc(string name, bool active)
    {
        SetBoolAnimationOnLocalClient(name, active);
    }

    public void SetBoolAnimationOnLocalClient(string name, bool active)
    {
        Plugin.ExtendedLogging(name + " " + active);
        creatureAnimator.SetBool(name, active);
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
    public bool FindClosestPlayerInRange(float range) {
        PlayerControllerB? closestPlayer = null;
        float minDistance = float.MaxValue;

        foreach (PlayerControllerB player in StartOfRound.Instance.allPlayerScripts) {
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

    public bool EnemyHasLineOfSightToPosition(Vector3 pos, float width = 60f, float range = 20f, float proximityAwareness = 5f) {
        if (eye == null) {
            _ = transform;
        } else {
            _ = eye;
        }

        if (Vector3.Distance(eye!.position, pos) >= range || Physics.Linecast(eye.position, pos, StartOfRound.Instance.collidersAndRoomMaskAndDefault)) return false;

        Vector3 to = pos - eye.position;
        return Vector3.Angle(eye.forward, to) < width || Vector3.Distance(transform.position, pos) < proximityAwareness;
    }
    public bool IsPlayerReachable(PlayerControllerB PlayerToCheck) {
        Vector3 Position = RoundManager.Instance.GetNavMeshPosition(PlayerToCheck.transform.position, RoundManager.Instance.navHit, 2.7f);
        if (!RoundManager.Instance.GotNavMeshPositionResult) {
            Plugin.ExtendedLogging("Player Reach Test: No Navmesh position");
            return false; 
        }
        agent.CalculatePath(Position, agent.path);
        bool HasPath = (agent.path.status == NavMeshPathStatus.PathComplete);
        Plugin.ExtendedLogging($"Player Reach Test: {HasPath}");
        return HasPath;
    }

    public float PlayerDistanceFromShip(PlayerControllerB PlayerToCheck) {
        if(PlayerToCheck == null) return -1;
        float DistanceFromShip = Vector3.Distance(PlayerToCheck.transform.position, StartOfRound.Instance.shipBounds.transform.position);
        Plugin.ExtendedLogging($"PlayerNearShip check: {DistanceFromShip}");
        return DistanceFromShip;
    }

    private float DistanceFromPlayer(PlayerControllerB player, bool IncludeYAxis) {
        if (player == null) return -1f;
        if (IncludeYAxis) {
            return Vector3.Distance(player.transform.position, this.transform.position);
        }
        Vector2 PlayerFlatLocation = new Vector2(player.transform.position.x, player.transform.position.z);
        Vector2 EnemyFlatLocation = new Vector2(transform.position.x, transform.position.z);
        return Vector2.Distance(PlayerFlatLocation, EnemyFlatLocation);
    }

    public bool AnimationIsFinished(string AnimName) {
        if (!creatureAnimator.GetCurrentAnimatorStateInfo(0).IsName(AnimName)) {
            Plugin.ExtendedLogging(__getTypeName() + ": Checking for animation " + AnimName + ", but current animation is " + creatureAnimator.GetCurrentAnimatorClipInfo(0)[0].clip.name);
            return true;
        }
        return (creatureAnimator.GetCurrentAnimatorStateInfo(0).normalizedTime >= 1f);
    }

    [ServerRpc(RequireOwnership = false)]
    public void SetTargetServerRpc(int PlayerID) {
        SetTargetClientRpc(PlayerID);
    }

    [ClientRpc]
    public void SetTargetClientRpc(int PlayerID) {
        if (PlayerID == -1) {
            targetPlayer = null;
            Plugin.ExtendedLogging($"Clearing target on {this}");
            return;
        }
        if (StartOfRound.Instance.allPlayerScripts[PlayerID] == null) {
            Plugin.ExtendedLogging($"Index invalid! {this}");
            return;
        }
        targetPlayer = StartOfRound.Instance.allPlayerScripts[PlayerID];
        Plugin.ExtendedLogging($"{this} setting target to: {targetPlayer.playerUsername}");
    }

    [ServerRpc(RequireOwnership = false)]
    public void SetEnemyTargetServerRpc(int enemyID) {
        SetEnemyTargetClientRpc(enemyID);
    }

    [ClientRpc]
    public void SetEnemyTargetClientRpc(int enemyID) {
        if (enemyID == -1) {
            targetEnemy = null;
            Plugin.ExtendedLogging($"Clearing Enemy target on {this}");
            return;
        }
        if (RoundManager.Instance.SpawnedEnemies[enemyID] == null) {
            Plugin.ExtendedLogging($"Enemy Index invalid! {this}");
            return;
        }
        targetEnemy = RoundManager.Instance.SpawnedEnemies[enemyID];
        Plugin.ExtendedLogging($"{this} setting target to: {targetEnemy.enemyType.enemyName}");
    }

    public void GoThroughEntrance() {
        var insideEntrancePosition = RoundManager.FindMainEntrancePosition(true, false);
        var outsideEntrancePosition = RoundManager.FindMainEntrancePosition(true, true);
        if (isOutside) {
            SetDestinationToPosition(outsideEntrancePosition);
            
            if (Vector3.Distance(transform.position, outsideEntrancePosition) < 1f) {
                this.agent.Warp(insideEntrancePosition);
                SetEnemyOutsideServerRpc(false);
            }
        } else {
            SetDestinationToPosition(insideEntrancePosition);
            if (Vector3.Distance(transform.position, insideEntrancePosition) < 1f) {
                this.agent.Warp(outsideEntrancePosition);
                SetEnemyOutsideServerRpc(true);
            }
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void SetEnemyOutsideServerRpc(bool setOutside)
    {
        SetEnemyOutsideClientRpc(setOutside);
    }

    [ClientRpc]
    public void SetEnemyOutsideClientRpc(bool setOutisde) {
        this.SetEnemyOutside(setOutisde);
    }

    public void StartSearchRoutine(Vector3 position, float radius, LayerMask agentMask) {
        if (searchRoutine != null) {
            StopCoroutine(searchRoutine);
        }
        isSearching = true;
        searchRoutine = StartCoroutine(SearchAlgorithm(position, radius));
    }

    public void StopSearchRoutine() {
        isSearching = false;
        StopCoroutine(searchRoutine);
        searchRoutine = null;
    }

    private IEnumerator SearchAlgorithm(Vector3 position, float radius)
    {
        while (isSearching) {
            Vector3 positionToTravel = RoundManager.Instance.GetRandomNavMeshPositionInRadius(position, radius, default);
            reachedDestination = false;
            while (!reachedDestination && isSearching) {
                SetDestinationToPosition(positionToTravel);
                yield return new WaitForSeconds(8f);
                if (Vector3.Distance(this.transform.position, positionToTravel) < 5f || agent.velocity.magnitude <= 0.5f) {
                    reachedDestination = true;
                }
            }
        }
        searchRoutine = null; // Clear the coroutine reference when it finishes
    }
}