using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using CodeRebirth.src.Content.Maps;
using GameNetcodeStuff;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem.Utilities;

namespace CodeRebirth.src.Content.Enemies;
public class CactusBudling : CodeRebirthEnemyAI, IVisibleThreat
{
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
    private float _rollingTimer = 20f;
    private float _rootingTimer = 60f;
    private float _attackInterval = 2f;
    private List<GameObject> _budlingCacti = new();
    private CactusBudlingState _nextState = CactusBudlingState.Spawning;
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
        foreach (var mapObjectDefinition in EnemyHandler.Instance.CactusBudling!.MapObjectDefinitions)
        {
            if (mapObjectDefinition.objectName.Contains("Cactus", StringComparison.OrdinalIgnoreCase))
                _budlingCacti.Add(mapObjectDefinition.gameObject);
        }
        _targetRootPosition = RoundManager.Instance.GetRandomNavMeshPositionInRadius(this.transform.position, 40f, default);
        _nextStateRoutine = StartCoroutine(DelayToNextState(_spawnAnimation.length, 3f, CactusBudlingState.SearchingForRoot));
    }

    public override void Update()
    {
        base.Update();

        if (currentBehaviourStateIndex != (int)CactusBudlingState.Rolling)
            return;

        if (Vector3.Distance(GameNetworkManager.Instance.localPlayerController.transform.position, this.transform.position) < 15f)
        {
            HUDManager.Instance.ShakeCamera(ScreenShakeType.Small);
        }
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
            _targetRootPosition = RoundManager.Instance.GetRandomNavMeshPositionInRadius(this.transform.position, 40f, default);
            _nextStateRoutine = StartCoroutine(DelayToNextState(_rollingEndAnimation.length, 3f, CactusBudlingState.SearchingForRoot));
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
            SpawnCactiServerRpc(randomPosition, normal, randomAngle, randomCactiIndex);
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
            _targetRootPosition = RoundManager.Instance.GetRandomNavMeshPositionInRadius(this.transform.position, 40f, default);
            _nextStateRoutine = StartCoroutine(DelayToNextState(_rollingEndAnimation.length, 3f, CactusBudlingState.SearchingForRoot));
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
    private IEnumerator DelayToNextState(float delay, float agentSpeed, CactusBudlingState nextState)
    {
        Plugin.ExtendedLogging($"Switching to next state: {nextState}");
        _nextState = nextState;
        agent.speed = 0f;
        smartAgentNavigator.StopAgent();
        yield return new WaitForSeconds(delay);
        SwitchToBehaviourStateOnLocalClient((int)nextState);
        agent.speed = agentSpeed;
        _nextStateRoutine = null;
    }

    [ServerRpc(RequireOwnership = false)]
    private void SpawnCactiServerRpc(Vector3 position, Vector3 normal, int angle, int index)
    {
        SpawnCactiClientRpc(position, normal, angle, index);
    }

    [ClientRpc]
    private void SpawnCactiClientRpc(Vector3 position, Vector3 normal, int angle, int index)
    {
        GameObject randomCacti = _budlingCacti[index];
        var newCacti = GameObject.Instantiate(randomCacti, position, Quaternion.Euler(0, angle, 0), RoundManager.Instance.mapPropsContainer.transform);
        newCacti.transform.up = normal;
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
        player.DamagePlayer(50, true, false, CauseOfDeath.Crushing, 0, false, directionToPush * 75f);
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
                if (_nextState != CactusBudlingState.Rolling)
                {
                    StopCoroutine(_nextStateRoutine);
                }
                else
                {
                    return;
                }
            }
            if (IsServer)
            {
                creatureAnimator.SetBool(RootingAnimation, false);
                creatureAnimator.SetBool(RollingAnimation, true);
            }

            _rollingTimer = _rollingDuration;
            _targetRollingPosition = RoundManager.Instance.GetRandomNavMeshPositionInRadius(this.transform.position, 40f, default);
            float delayTimer = _rollingStartAnimation.length;
            if (currentBehaviourStateIndex == (int)CactusBudlingState.Rooted)
            {
                delayTimer += _rootingEndAnimation.length;
            }
            _nextStateRoutine = StartCoroutine(DelayToNextState(delayTimer, 10f, CactusBudlingState.Rolling));
        }
    }

    public override void KillEnemy(bool destroy = false)
    {
        base.KillEnemy(destroy);
        agent.speed = 0f;

        if (_nextStateRoutine != null)
            StopCoroutine(_nextStateRoutine);

        SwitchToBehaviourStateOnLocalClient((int)CactusBudlingState.Dead);
        if (!IsServer)
            return;

        creatureAnimator.SetBool(DeadAnimation, true);
    }
    #endregion
}