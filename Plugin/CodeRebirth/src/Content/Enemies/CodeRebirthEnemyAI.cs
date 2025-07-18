﻿using Unity.Netcode;
using GameNetcodeStuff;
using UnityEngine;
using System.Linq;
using Unity.Netcode.Components;
using CodeRebirth.src.Util.Extensions;
using System.Collections;
using CodeRebirth.src.MiscScripts;
using CodeRebirthLib.Util.Pathfinding;
using CodeRebirthLib.Util.INetworkSerializables;
using CodeRebirth.src.Util;

namespace CodeRebirth.src.Content.Enemies;
[RequireComponent(typeof(SmartAgentNavigator))]
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(NetworkAnimator))]
[RequireComponent(typeof(NetworkTransform))]
[RequireComponent(typeof(Collider))]
public abstract class CodeRebirthEnemyAI : EnemyAI
{
    internal EnemyAI? targetEnemy = null;
    internal PlayerControllerB? previousTargetPlayer = null;

    [Header("Required Components")]
    [SerializeField]
    internal NetworkAnimator creatureNetworkAnimator = null!;
    [SerializeField]
    internal SmartAgentNavigator smartAgentNavigator = null!;

    [Header("Optional Settings: Palettes")]
    [SerializeField]
    private bool _hasVariants = false;
    [SerializeField]
    private bool _usesTemperature = false;
    [SerializeField]
    internal Renderer? _specialRenderer = null;

    [Header("Inherited Fields")]
    public AudioClipsWithTime _idleAudioClips = null!;
    public AudioClip[] _hitBodySounds = [];
    public AudioClip spawnSound = null!;

    [HideInInspector]
    public float _idleTimer = 1f;
    [HideInInspector]
    public System.Random enemyRandom = new();

    private float _previousLightValue = 0f;
    internal DetectLightInSurroundings? detectLightInSurroundings = null;
    internal static int ShiftHash = Shader.PropertyToID("_Shift");
    private static int TemperatureHash = Shader.PropertyToID("_Temperature");

    public override void Start()
    {
        base.Start();
        enemyRandom = new System.Random(StartOfRound.Instance.randomMapSeed + 6699 + CodeRebirthUtils.Instance.CRRandom.Next(100000));

        if (spawnSound != null)
            creatureVoice.PlayOneShot(spawnSound);

        _idleTimer = enemyRandom.NextFloat(_idleAudioClips.minTime, _idleAudioClips.maxTime);

        smartAgentNavigator.OnUseEntranceTeleport.AddListener(SetEnemyOutside); // todo driftwood
        smartAgentNavigator.SetAllValues(isOutside);
        Plugin.ExtendedLogging(enemyType.enemyName + " Spawned.");

        if (Plugin.ModConfig.ConfigExtendedLogging.Value)
            GrabEnemyRarity(enemyType.enemyName);

        if (_hasVariants && _specialRenderer != null)
        {
            ApplyVariants(_specialRenderer);
        }
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        detectLightInSurroundings?.OnLightValueChange.RemoveListener(OnLightValueChange);
    }

    public override void HitEnemy(int force = 1, PlayerControllerB? playerWhoHit = null, bool playHitSFX = false, int hitID = -1)
    {
        base.HitEnemy(force, playerWhoHit, playHitSFX, hitID);
        if (!isEnemyDead && playHitSFX && _hitBodySounds.Length > 0)
        {
            creatureSFX.PlayOneShot(_hitBodySounds[enemyRandom.Next(_hitBodySounds.Length)]);
        }
    }

    private void ApplyVariants(Renderer renderer)
    {
        float number = enemyRandom.NextFloat(0f, 1f);
        renderer.GetMaterial().SetFloat(ShiftHash, number);
        if (_usesTemperature)
        {
            detectLightInSurroundings = this.gameObject.AddComponent<DetectLightInSurroundings>();
            detectLightInSurroundings.OnLightValueChange.AddListener(OnLightValueChange);
        }
    }

    public virtual void OnLightValueChange(float lightValue)
    {
        // Plugin.ExtendedLogging($"Light Value: {lightValue}");
        float newLightValue = Mathf.Sqrt(lightValue);
        StartCoroutine(LerpToHotOrCold(_previousLightValue, newLightValue));
    }

    private IEnumerator LerpToHotOrCold(float oldValue, float newValue)
    {
        if (_specialRenderer == null) yield break;
        for (int i = 1; i <= 3; i++)
        {
            float step = Mathf.Lerp(oldValue, newValue, i / 3f);
            _specialRenderer.GetMaterial().SetFloat(TemperatureHash, Mathf.Clamp(step / 5f - 0.5f, -0.5f, 0.5f));
            yield return null;
        }
        _previousLightValue = newValue;
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

    public bool FindClosestPlayerInRange(float range, bool targetAlreadyTargettedPerson = true)
    {
        PlayerControllerB? closestPlayer = null;
        float minDistance = float.MaxValue;

        foreach (PlayerControllerB player in StartOfRound.Instance.allPlayerScripts)
        {
            if (player == null || !player.isPlayerControlled || player.isPlayerDead || player.isInHangarShipRoom || !EnemyHasLineOfSightToPosition(player.transform.position, 60f, range))
                continue;

            if (!targetAlreadyTargettedPerson && CheckIfPersonAlreadyTargetted(player))
                continue;

            float distance = Vector3.Distance(transform.position, player.transform.position);
            if (distance >= minDistance)
                continue;

            minDistance = distance;
            closestPlayer = player;
        }

        if (closestPlayer == null)
            return false;

        SetPlayerTargetServerRpc(closestPlayer);
        return true;
    }

    public bool CheckIfPersonAlreadyTargetted(PlayerControllerB playerToCheck)
    {
        foreach (var enemy in RoundManager.Instance.SpawnedEnemies)
        {
            if (enemy == this)
                continue;

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

        float distance = Vector3.Distance(eyeTransform.position, pos);
        if (distance < proximityAwareness)
            return true;

        if (distance >= range || Physics.Linecast(eyeTransform.position, pos, StartOfRound.Instance.collidersAndRoomMaskAndDefault, QueryTriggerInteraction.Ignore))
            return false;

        Vector3 to = pos - eyeTransform.position;
        return Vector3.Angle(eyeTransform.forward, to) < width;
    }

    public bool PlayerLookingAtEnemy(PlayerControllerB player, float dotProductThreshold)
    {
        Vector3 directionToEnemy = (transform.position - player.gameObject.transform.position).normalized;
        if (Vector3.Dot(player.gameplayCamera.transform.forward, directionToEnemy) < dotProductThreshold)
            return false;

        if (Physics.Linecast(player.gameplayCamera.transform.position, transform.position, StartOfRound.Instance.collidersAndRoomMaskAndDefault, QueryTriggerInteraction.Ignore))
            return false;

        return true;
    }

    public bool EnemySeesPlayer(PlayerControllerB player, float dotThreshold)
    {
        Transform mainTransform;
        if (eye == null)
        {
            mainTransform = this.transform;
        }
        else
        {
            mainTransform = eye.transform;
        }

        Vector3 directionToPlayer = (player.transform.position - mainTransform.position).normalized;
        if (Vector3.Dot(transform.forward, directionToPlayer) < dotThreshold)
            return false;

        float distanceToPlayer = Vector3.Distance(mainTransform.position, player.transform.position);
        if (Physics.Raycast(mainTransform.position, directionToPlayer, distanceToPlayer, StartOfRound.Instance.collidersAndRoomMaskAndDefault, QueryTriggerInteraction.Ignore))
            return false;

        return true;
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
    public void ClearPlayerTargetServerRpc()
    {
        ClearPlayerTargetClientRpc();
    }

    [ClientRpc]
    public void ClearPlayerTargetClientRpc()
    {
        targetPlayer = null;
        PlayerSetAsTarget(null);
    }

    [ServerRpc(RequireOwnership = false)]
    public void SetPlayerTargetServerRpc(PlayerControllerReference playerControllerReference)
    {
        SetPlayerTargetClientRpc(playerControllerReference);
    }

    [ClientRpc]
    public void SetPlayerTargetClientRpc(PlayerControllerReference playerControllerReference)
    {
        PlayerControllerB player = playerControllerReference;
        previousTargetPlayer = targetPlayer;
        targetPlayer = player;
        PlayerSetAsTarget(targetPlayer);
        Plugin.ExtendedLogging($"{this} setting target to: {targetPlayer.playerUsername}");
    }

    [ServerRpc(RequireOwnership = false)]
    public void ClearEnemyTargetServerRpc()
    {
        ClearEnemyTargetClientRpc();
    }

    [ClientRpc]
    public void ClearEnemyTargetClientRpc()
    {
        targetEnemy = null;
        EnemySetAsTarget(null);
    }

    [ServerRpc(RequireOwnership = false)]
    public void SetEnemyTargetServerRpc(NetworkBehaviourReference networkBehaviourReference)
    {
        SetEnemyTargetClientRpc(networkBehaviourReference);
    }

    [ClientRpc]
    public void SetEnemyTargetClientRpc(NetworkBehaviourReference networkBehaviourReference)
    {
        targetEnemy = (EnemyAI)networkBehaviourReference;
        Plugin.ExtendedLogging($"{this} setting target to: {targetEnemy.enemyType.enemyName}");
        EnemySetAsTarget(targetEnemy);
    }

    public virtual void EnemySetAsTarget(EnemyAI? enemy)
    {

    }

    public virtual void PlayerSetAsTarget(PlayerControllerB? player)
    {

    }
}
