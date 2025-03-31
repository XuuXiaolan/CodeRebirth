using Unity.Netcode;
using GameNetcodeStuff;
using UnityEngine;
using UnityEngine.AI;
using System.Linq;
using CodeRebirth.src.MiscScripts.PathFinding;
using Unity.Netcode.Components;
using CodeRebirth.src.Util.Extensions;
using System.Collections;
using CodeRebirth.src.MiscScripts;

namespace CodeRebirth.src.Content.Enemies;
[RequireComponent(typeof(SmartAgentNavigator))]
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(NetworkAnimator))]
[RequireComponent(typeof(NetworkTransform))]
[RequireComponent(typeof(Collider))]
public abstract class CodeRebirthEnemyAI : EnemyAI
{
    [HideInInspector] public EnemyAI? targetEnemy;

    public bool hasVariants = false;
    public bool usesTemperature = false;
    public Renderer? specialRenderer = null;
    public NetworkAnimator creatureNetworkAnimator = null!;
    public SmartAgentNavigator smartAgentNavigator = null!;

    private float previousLightValue = 0f;
    private DetectLightInSurroundings? detectLightInSurroundings = null;
    private LineRenderer line; // Debug line that shows destination of movement
    [HideInInspector] public System.Random enemyRandom = new System.Random();
    private static int ShiftHash = Shader.PropertyToID("_Shift");
    private static int TemperatureHash = Shader.PropertyToID("_Temperature");

    public override void Start()
    {
        base.Start();
#if DEBUG
        line = gameObject.AddComponent<LineRenderer>();
        line.widthMultiplier = 0.2f; // reduce width of the line
        StartCoroutine(DrawPath(line, agent));
#endif
        enemyRandom = new System.Random(StartOfRound.Instance.randomMapSeed + RoundManager.Instance.SpawnedEnemies.Count + 69);
        smartAgentNavigator.OnUseEntranceTeleport.AddListener(SetEnemyOutside);
        smartAgentNavigator.SetAllValues(isOutside);
        Plugin.ExtendedLogging(enemyType.enemyName + " Spawned.");
        GrabEnemyRarity(enemyType.enemyName);
        if (hasVariants && specialRenderer != null)
        {
            ApplyVariants(specialRenderer);
        }
    }

    private void ApplyVariants(Renderer renderer)
    {
        System.Random random = new System.Random(RoundManager.Instance.SpawnedEnemies.Count + StartOfRound.Instance.randomMapSeed);
        float number = random.NextFloat(0f, 1f);
        renderer.GetMaterial().SetFloat(ShiftHash, number);
        if (usesTemperature)
        {
            detectLightInSurroundings = this.gameObject.AddComponent<DetectLightInSurroundings>();
            detectLightInSurroundings.OnLightValueChange.AddListener(OnLightValueChange);
        }
    }

    public virtual void OnLightValueChange(float lightValue)
    {
        // Plugin.ExtendedLogging($"Light Value: {lightValue}");
        float newLightValue = Mathf.Sqrt(lightValue);
        StartCoroutine(LerpToHotOrCold(previousLightValue, newLightValue));
    }

    private IEnumerator LerpToHotOrCold(float oldValue, float newValue)
    {
        if (specialRenderer == null) yield break;
        for (int i = 1; i <= 3; i++)
        {
            float step = Mathf.Lerp(oldValue, newValue, i/3f);
            specialRenderer.GetMaterial().SetFloat(TemperatureHash, Mathf.Clamp(step/5f - 0.5f, -0.5f, 0.5f));
            yield return null;
        }
        previousLightValue = newValue;
    }

    public void GrabEnemyRarity(string enemyName)
    {
        // Search in OutsideEnemies
        SpawnableEnemyWithRarity? enemy = RoundManager.Instance.currentLevel.OutsideEnemies
            .FirstOrDefault(x => x.enemyType.enemyName.Equals(enemyName)) ?? RoundManager.Instance.currentLevel.DaytimeEnemies
                .FirstOrDefault(x => x.enemyType.enemyName.Equals(enemyName)) ?? RoundManager.Instance.currentLevel.Enemies
                    .FirstOrDefault(x => x.enemyType.enemyName.Equals(enemyName));

        /*foreach (var spawnableEnemyWithRarity in RoundManager.Instance.currentLevel.Enemies)
        {
            Plugin.ExtendedLogging($"{spawnableEnemyWithRarity.enemyType.enemyName} has Rarity: {spawnableEnemyWithRarity.rarity.ToString()}");
        }

        foreach (var spawnableEnemyWithRarity in RoundManager.Instance.currentLevel.DaytimeEnemies)
        {
            Plugin.ExtendedLogging($"{spawnableEnemyWithRarity.enemyType.enemyName} has Rarity: {spawnableEnemyWithRarity.rarity.ToString()}");
        }

        foreach(var spawnableEnemyWithRarity in RoundManager.Instance.currentLevel.OutsideEnemies)
        {
            Plugin.ExtendedLogging($"{spawnableEnemyWithRarity.enemyType.enemyName} has Rarity: {spawnableEnemyWithRarity.rarity.ToString()}");
        }*/

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

    public IEnumerator DrawPath(LineRenderer line, NavMeshAgent agent)
    {
        while (true)
        {
            yield return new WaitUntil(() => agent.enabled);
            yield return new WaitForSeconds(1f);
            yield return new WaitForEndOfFrame();
            if (!agent.enabled) yield break;
            line.SetPosition(0, agent.transform.position); //set the line's origin

            line.positionCount = agent.path.corners.Length; //set the array of positions to the amount of corners
            for (var i = 1; i < agent.path.corners.Length; i++)
            {
                line.SetPosition(i, agent.path.corners[i]); //go through each corner and set that to the line renderer's position
            }
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

    public bool FindClosestPlayerInRange(float range, bool targetAlreadyTargettedPerson = true)
    {
        PlayerControllerB? closestPlayer = null;
        float minDistance = float.MaxValue;

        foreach (PlayerControllerB player in StartOfRound.Instance.allPlayerScripts)
        {
            bool onSight = player.IsSpawned && player.isPlayerControlled && !player.isPlayerDead && !player.isInHangarShipRoom && EnemyHasLineOfSightToPosition(player.transform.position, 60f, range);
            if (!onSight) continue;

            if (CheckIfPersonAlreadyTargetted(targetAlreadyTargettedPerson, player)) continue;
            
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

    public bool CheckIfPersonAlreadyTargetted(bool targetAlreadyTargettedPerson, PlayerControllerB playerToCheck)
    {
        if (!targetAlreadyTargettedPerson) return false;
        foreach (var enemy in RoundManager.Instance.SpawnedEnemies)
        {
            if (enemy is CodeRebirthEnemyAI codeRebirthEnemyAI)
            {
                if (codeRebirthEnemyAI.targetPlayer == playerToCheck)
                return true;
            }
        }
        return false;
    }

    public bool EnemyHasLineOfSightToPosition(Vector3 pos, float width = 60f, float range = 20f, float proximityAwareness = 5f)
    {
        Transform eyeTransform;
        if (eye == null)
        {
            eyeTransform = transform;
        }
        else
        {
            eyeTransform = eye;
        }

        if (Vector3.Distance(eyeTransform.position, pos) >= range || Physics.Linecast(eyeTransform.position, pos, StartOfRound.Instance.collidersAndRoomMaskAndDefault, QueryTriggerInteraction.Ignore)) return false;

        Vector3 to = pos - eyeTransform.position;
        return Vector3.Angle(eyeTransform.forward, to) < width || Vector3.Distance(transform.position, pos) < proximityAwareness;
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
}