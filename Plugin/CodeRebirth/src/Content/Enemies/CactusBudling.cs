using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using CodeRebirth.src.Content.Maps;
using Dusk;
using Dawn.Utils;
using GameNetcodeStuff;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem.Utilities;
using CodeRebirth.src.MiscScripts;

namespace CodeRebirth.src.Content.Enemies;
public class CactusBudling : CodeRebirthEnemyAI, IVisibleThreat
{
    [Header("Audio")]
    [SerializeField]
    private AudioSource _rollingSource = null!;

    [Header("Animations")]
    [SerializeField]
    private AnimationClip _spawnAnimation = null!;
    [SerializeField]
    private AnimationClip _rootingEndAnimation = null!;
    [SerializeField]
    private AnimationClip _rollingStartAnimation = null!;
    [SerializeField]
    private AnimationClip _rollingEndAnimation = null!;

    [Header("Mechanics")]
    [SerializeField]
    private float _rollingDuration = 20f;
    [SerializeField]
    private float _rootingDuration = 60f;
    [SerializeField]
    private int _attackAmount = 5;

    private Vector3 _targetRootPosition = Vector3.zero;
    private Vector3 _targetRollingPosition = Vector3.zero;
    private float _rotationProgressTimer = 0f;
    private float _rollingTimer = 0f;
    private float _rootingTimer = 0f;
    private float _attackInterval = 2f;
    private List<GameObject> _budlingCacti = new();
    private Coroutine? _nextStateRoutine = null;

    private static readonly int RunSpeedFloat = Animator.StringToHash("RunSpeed"); // Float
    private static readonly int RollingAnimation = Animator.StringToHash("isRolling"); // Bool
    private static readonly int RootingAnimation = Animator.StringToHash("isRooting"); // Bool
    private static readonly int DeadAnimation = Animator.StringToHash("isDead"); // Bool

    #region ThreatType
    ThreatType IVisibleThreat.type => ThreatType.ForestGiant;

    int IVisibleThreat.SendSpecialBehaviour(int id)
    {
        return 0;
    }

    int IVisibleThreat.GetThreatLevel(Vector3 seenByPosition)
    {
        return 18;
    }

    int IVisibleThreat.GetInterestLevel()
    {
        return 0;
    }

    Transform IVisibleThreat.GetThreatLookTransform()
    {
        return eye;
    }

    Transform IVisibleThreat.GetThreatTransform()
    {
        return base.transform;
    }

    Vector3 IVisibleThreat.GetThreatVelocity()
    {
        if (base.IsOwner)
        {
            return agent.velocity;
        }
        return Vector3.zero;
    }

    float IVisibleThreat.GetVisibility()
    {
        if (isEnemyDead)
        {
            return 0f;
        }
        if (agent.velocity.sqrMagnitude > 0f)
        {
            return 1f;
        }
        return 0.75f;
    }

    bool IVisibleThreat.IsThreatDead()
    {
        return this.isEnemyDead;
    }

    GrabbableObject? IVisibleThreat.GetHeldObject()
    {
        return null;
    }
    #endregion

    public enum CactusBudlingState
    {
        Spawning,
        SearchingForRoot,
        Rooted,
        Rolling,
        Dead,
    }

    #region Unity Lifecycles
    public override void Start()
    {
        base.Start();
        foreach (var crContentDefinition in EnemyHandler.Instance.CactusBudling!.Content)
        {
            if (crContentDefinition is not DuskMapObjectDefinition duskMapObjectDefinition)
                continue;

            if (duskMapObjectDefinition.MapObjectName.Contains("Cactus", StringComparison.OrdinalIgnoreCase))
                _budlingCacti.Add(duskMapObjectDefinition.GameObject);
        }

        if (!IsServer)
            return;

        GetNextRootPosition();
    }

    public override void Update()
    {
        base.Update();

        _idleTimer -= Time.deltaTime;
        if (_idleTimer <= 0)
        {
            _idleTimer = enemyRandom.NextFloat(_idleAudioClips.minTime, _idleAudioClips.maxTime);
            creatureVoice.PlayOneShot(_idleAudioClips.audioClips[enemyRandom.Next(0, _idleAudioClips.audioClips.Length)]);
        }

        if (currentBehaviourStateIndex != (int)CactusBudlingState.Rolling)
            return;

        if (Vector3.Distance(GameNetworkManager.Instance.localPlayerController.transform.position, this.transform.position) < 15f)
        {
            HUDManager.Instance.ShakeCamera(ScreenShakeType.Small);
        }
    }

    public void LateUpdate()
    {
        if (!IsServer)
            return;

        if (currentBehaviourStateIndex != (int)CactusBudlingState.Rooted && _rotationProgressTimer <= 0)
            return;

        if (!Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit, 1f, StartOfRound.Instance.collidersAndRoomMaskAndDefault, QueryTriggerInteraction.Ignore))
            return;

        Vector3 hitNormal = hit.normal;
        Vector3 projectedForward = Vector3.ProjectOnPlane(transform.forward, hitNormal).normalized;
        Quaternion targetRotation = Quaternion.LookRotation(projectedForward, hitNormal);

        if (currentBehaviourStateIndex == (int)CactusBudlingState.Rooted)
        {
            _rotationProgressTimer = Mathf.Clamp01(_rotationProgressTimer + Time.deltaTime * 0.5f);
        }
        else
        {
            _rotationProgressTimer = Mathf.Clamp01(_rotationProgressTimer - Time.deltaTime * 0.5f);
        }
        this.transform.rotation = Quaternion.Slerp(Quaternion.identity, targetRotation, _rotationProgressTimer);
    }
    #endregion

    #region StateMachines
    public override void DoAIInterval()
    {
        base.DoAIInterval();
        if (StartOfRound.Instance.allPlayersDead || isEnemyDead)
            return;

        creatureAnimator.SetFloat(RunSpeedFloat, agent.velocity.magnitude / 3f);
        switch (currentBehaviourStateIndex)
        {
            case (int)CactusBudlingState.Spawning:
                DoSpawning();
                break;
            case (int)CactusBudlingState.SearchingForRoot:
                DoSearchingForRoot();
                break;
            case (int)CactusBudlingState.Rooted:
                DoRooted();
                break;
            case (int)CactusBudlingState.Rolling:
                DoRolling();
                break;
            case (int)CactusBudlingState.Dead:
                DoDead();
                break;
        }
    }

    private void DoSpawning()
    {

    }

    private void DoSearchingForRoot()
    {
        if (Vector3.Distance(transform.position, _targetRootPosition) < 2f + agent.stoppingDistance)
        {
            if (_nextStateRoutine != null)
                return;

            _rootingTimer = _rootingDuration;
            smartAgentNavigator.StopAgent();
            creatureAnimator.SetBool(RootingAnimation, true);
            SwitchToBehaviourServerRpc((int)CactusBudlingState.Rooted);
            return;
        }
        smartAgentNavigator.DoPathingToDestination(_targetRootPosition);
    }

    private void DoRooted()
    {
        _rootingTimer -= AIIntervalTime;
        if (_rootingTimer <= 0)
        {
            if (_nextStateRoutine != null)
                return;

            creatureAnimator.SetBool(RootingAnimation, false);
            GetNextRootPosition();
            return;
        }

        _attackInterval -= AIIntervalTime;
        if (_attackInterval <= 0)
        {
            _attackInterval = _rootingDuration / _attackAmount;

            List<PlayerControllerB> playersList = StartOfRound.Instance.allPlayerScripts.Where(x => !x.isPlayerDead && x.isPlayerControlled).ToList();
            PlayerControllerB randomPlayer = playersList[UnityEngine.Random.Range(0, playersList.Count)];

            Vector3 randomPosition = Vector3.zero;
            for (int i = 0; i < 5; i++)
            {
                randomPosition = RoundManager.Instance.GetRandomNavMeshPositionInRadiusSpherical(randomPlayer.transform.position, 5f);
                if (Vector3.Distance(randomPosition, randomPlayer.transform.position) > 1f)
                {
                    break;
                }
            }
            Vector3 normal = Vector3.zero;
            if (Physics.Raycast(randomPosition + Vector3.up * 2f, Vector3.down, out RaycastHit hit, 5f, StartOfRound.Instance.collidersAndRoomMask, QueryTriggerInteraction.Ignore))
            {
                randomPosition = hit.point;
                normal = hit.normal;
            }
            else
            {
                _attackInterval = 0;
                return;
            }
            int randomCactiIndex = UnityEngine.Random.Range(0, _budlingCacti.Count);
            int randomAngle = UnityEngine.Random.Range(0, 360);
            SpawnCactiServerRpc(randomPosition, normal, randomAngle, randomCactiIndex, randomPlayer.isInsideFactory);
        }
    }

    private void DoRolling()
    {
        _rollingTimer -= AIIntervalTime;
        if (_rollingTimer <= 0)
        {
            if (_nextStateRoutine != null)
                return;

            creatureAnimator.SetBool(RollingAnimation, false);
            GetNextRootPosition();
            return;
        }

        if (Vector3.Distance(transform.position, _targetRollingPosition) < 3f + agent.stoppingDistance)
        {
            for (int i = 0; i < 10; i++)
            {
                _targetRollingPosition = RoundManager.Instance.GetRandomNavMeshPositionInRadius(this.transform.position, 75f, default);
                if (smartAgentNavigator.CanPathToPoint(this.transform.position, _targetRollingPosition) > 0f)
                {
                    break;
                }
            }
        }
        smartAgentNavigator.DoPathingToDestination(_targetRollingPosition);
    }

    private void DoDead()
    {

    }
    #endregion

    #region Misc Functions
    private void GetNextRootPosition()
    {
        List<(Vector3 position, Vector3 alsoPosition)> possiblePositions = new();
        for (int i = 0; i < 10; i++)
        {
            Vector3 randomPosition = RoundManager.Instance.GetRandomNavMeshPositionInRadius(this.transform.position, 40f, default);
            possiblePositions.Add((randomPosition, randomPosition));
        }
        smartAgentNavigator.CheckPaths(possiblePositions, FoundNextRootPosition);
    }

    private void FoundNextRootPosition(List<GenericPath<Vector3>> args)
    {
        SyncRootPositionServerRpc(args[UnityEngine.Random.Range(0, args.Count)].Generic);
    }

    [ServerRpc(RequireOwnership = false)]
    private void SyncRootPositionServerRpc(Vector3 rootPosition)
    {
        SyncRootPositionClientRpc(rootPosition);
    }

    [ClientRpc]
    private void SyncRootPositionClientRpc(Vector3 rootPosition)
    {
        _targetRootPosition = rootPosition;
        _nextStateRoutine = StartCoroutine(DelayToNextState(_rollingStartAnimation.length, 3f, CactusBudlingState.SearchingForRoot));
    }

    private IEnumerator DelayToNextState(float delay, float agentSpeed, CactusBudlingState nextState)
    {
        Plugin.ExtendedLogging($"Switching to next state: {nextState}");
        agent.speed = 0f;
        smartAgentNavigator.StopAgent();
        SwitchToBehaviourStateOnLocalClient((int)nextState);
        yield return new WaitForSeconds(delay);
        agent.speed = agentSpeed;
        _nextStateRoutine = null;
    }

    private IEnumerator RollingSoundStuff(float delayTimer)
    {
        yield return new WaitForSeconds(delayTimer);
        _rollingSource.Play();
        while (_rollingTimer > 0)
        {
            yield return null;
        }
        _rollingSource.Stop();
    }

    [ServerRpc(RequireOwnership = false)]
    private void SpawnCactiServerRpc(Vector3 position, Vector3 normal, int angle, int index, bool insideSpawn)
    {
        GameObject randomCacti = _budlingCacti[index];
        var newCacti = GameObject.Instantiate(randomCacti, position, Quaternion.Euler(0, angle, 0), RoundManager.Instance.mapPropsContainer.transform);
        var netObj = newCacti.GetComponent<NetworkObject>();
        netObj.Spawn(true);
        SpawnCactiClientRpc(netObj, normal, insideSpawn);
    }

    [ClientRpc]
    private void SpawnCactiClientRpc(NetworkObjectReference netObjRef, Vector3 normal, bool insideSpawn)
    {
        if (netObjRef.TryGet(out NetworkObject netObj))
        {
            netObj.transform.up = normal;
            if (insideSpawn)
            {
                RiseFromGroundOnSpawn riseFromGroundOnSpawn = netObj.gameObject.GetComponent<RiseFromGroundOnSpawn>();
                foreach (RiseFromDifferentGroundTypes riseFromDifferentGroundTypes in riseFromGroundOnSpawn._riseFromDifferentGroundTypes)
                {
                    riseFromDifferentGroundTypes.raiseSpeed /= 4f;
                }
            }
            DestructibleObject destructibleObject = netObj.gameObject.GetComponent<DestructibleObject>();
            destructibleObject._destroyCactiRoutine = destructibleObject.StartCoroutine(destructibleObject.DestroyObjectWithDelay(enemyRandom.NextFloat(30f, 45f), true));
        }
    }
    #endregion

    #region Animation Events
    #endregion

    #region Call Backs
    public override void OnCollideWithPlayer(Collider other)
    {
        base.OnCollideWithPlayer(other);
        if (currentBehaviourStateIndex != (int)CactusBudlingState.Rolling)
            return;

        Plugin.ExtendedLogging($"Collided with player: {other.gameObject.name}");
        Vector3 directionToPush = this.transform.forward;
        PlayerControllerB player = other.GetComponent<PlayerControllerB>();
        player.externalForceAutoFade = directionToPush * 75f;
        player.externalForces = directionToPush * 75f;
        player.DamagePlayer(50, true, true, CauseOfDeath.Crushing, 0, false, directionToPush * 75f);
    }

    public override void HitEnemy(int force = 1, PlayerControllerB? playerWhoHit = null, bool playHitSFX = false, int hitID = -1)
    {
        base.HitEnemy(force, playerWhoHit, playHitSFX, hitID);
        if (isEnemyDead)
            return;

        enemyHP -= force;

        if (enemyHP <= 0)
        {
            if (IsOwner)
            {
                KillEnemyOnOwnerClient();
            }
            return;
        }
        if (currentBehaviourStateIndex == (int)CactusBudlingState.Rolling)
        {
            _rollingTimer += 5f;
            return;
        }

        if (currentBehaviourStateIndex != (int)CactusBudlingState.Rolling)
        {
            if (_nextStateRoutine != null)
            {
                StopCoroutine(_nextStateRoutine);
            }
            if (IsServer)
            {
                creatureAnimator.SetBool(RootingAnimation, false);
                creatureAnimator.SetBool(RollingAnimation, true);
            }
            float delayTimer = _rollingStartAnimation.length;
            if (currentBehaviourStateIndex == (int)CactusBudlingState.Rooted)
            {
                delayTimer += _rootingEndAnimation.length;
            }
            StartCoroutine(RollingSoundStuff(delayTimer));

            for (int i = 0; i < 10; i++)
            {
                _targetRollingPosition = RoundManager.Instance.GetRandomNavMeshPositionInRadius(this.transform.position, 75f, default);
                if (smartAgentNavigator.CanPathToPoint(this.transform.position, _targetRollingPosition) > 15f)
                {
                    break;
                }
            }
            _rollingTimer = _rollingDuration + delayTimer;
            _nextStateRoutine = StartCoroutine(DelayToNextState(delayTimer, 10f, CactusBudlingState.Rolling));
        }
    }

    public override void KillEnemy(bool destroy = false)
    {
        base.KillEnemy(destroy);
        agent.speed = 0f;
        _rollingTimer = 0f;

        if (_nextStateRoutine != null)
            StopCoroutine(_nextStateRoutine);

        SwitchToBehaviourStateOnLocalClient((int)CactusBudlingState.Dead);
        if (!IsServer)
            return;

        creatureAnimator.SetBool(DeadAnimation, true);
    }
    #endregion
}