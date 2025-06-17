using System;
using System.Collections;
using System.Collections.Generic;
using CodeRebirth.src.Util;
using CodeRebirth.src.Util.Extensions;
using GameNetcodeStuff;
using UnityEngine;

namespace CodeRebirth.src.Content.Enemies;
public class Monarch : CodeRebirthEnemyAI, IVisibleThreat
{
    [SerializeField]
    private MonarchBeamController BeamController = null!;
    [SerializeField]
    private AudioSource UltraCreatureVoice = null!;
    [SerializeField]
    private AudioClip[] _biteSounds = [];
    [SerializeField]
    private Transform MouthTransform = null!;

    public static List<Monarch> Monarchs = new();

    public enum MonarchState
    {
        Idle,
        AttackingGround,
        AttackingAir,
        Death,
    }

    private bool canAttack = true;
    private bool isAttacking = false;
    private Collider[] _cachedHits = new Collider[8];
    private bool wasParallaxOnLastFrame = false;
    private static readonly int DoAttackAnimation = Animator.StringToHash("doAttack"); // trigger
    private static readonly int IsFlyingAnimation = Animator.StringToHash("isFlying"); // Bool
    private static readonly int IsDeadAnimation = Animator.StringToHash("isDead"); // Bool
    private static readonly int RunSpeedFloat = Animator.StringToHash("RunSpeed"); // Float

    private static readonly int ParallaxSwitch = Shader.PropertyToID("_ParallaxSwitch"); // TODO
    #region IVisibleThreat
    public ThreatType type => ThreatType.EyelessDog;

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
        return base.transform;
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
		return isEnemyDead;
	}

	GrabbableObject? IVisibleThreat.GetHeldObject()
	{
		return null;
	}
    #endregion
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        Monarchs.Add(this);
        HUDManager.Instance.ShakeCamera(ScreenShakeType.VeryStrong);
        HUDManager.Instance.ShakeCamera(ScreenShakeType.Long);
        UltraCreatureVoice.Play();

        if (!IsServer)
            return;

        int randomNumberToSpawn = UnityEngine.Random.Range(2, 5);
        for (int i = 0; i <= randomNumberToSpawn; i++)
        {
            RoundManager.Instance.SpawnEnemyGameObject(RoundManager.Instance.GetRandomNavMeshPositionInRadiusSpherical(this.transform.position, 30, default), -1, -1, EnemyHandler.Instance.Monarch!.EnemyDefinitions.GetCREnemyDefinitionWithEnemyName("CutieFly")!.enemyType);
        }
    }

    public override void Start()
    {
        base.Start();
        List<CRDynamicConfig> configDefinitions = EnemyHandler.Instance.Monarch!.EnemyDefinitions.GetCREnemyDefinitionWithEnemyName(enemyType.enemyName)!.ConfigEntries;
        CRDynamicConfig? configSetting = configDefinitions.GetCRDynamicConfigWithSetting("Monarch", "Parallax Wings");
        if (configSetting != null)
        {
            wasParallaxOnLastFrame = CRConfigManager.GetGeneralConfigEntry<bool>(configSetting.settingName, configSetting.settingDesc).Value;
            skinnedMeshRenderers[0].sharedMaterials[0].SetInt(ParallaxSwitch, wasParallaxOnLastFrame ? 1 : 0);
        }
        BeamController._monarchParticle.transform.SetParent(null);
        BeamController._monarchParticle.transform.position = Vector3.zero;
        SwitchToBehaviourStateOnLocalClient((int)MonarchState.Idle);
        if (!IsServer)
            return;

        smartAgentNavigator.StartSearchRoutine(50);
    }

    public override void Update()
    {
        base.Update();

        if (isEnemyDead)
            return;

        _idleTimer -= Time.deltaTime;
        if (_idleTimer > 0)
            return;

        _idleTimer = enemyRandom.NextFloat(_idleAudioClips.minTime, _idleAudioClips.maxTime);
        creatureVoice.PlayOneShot(_idleAudioClips.audioClips[enemyRandom.Next(0, _idleAudioClips.audioClips.Length)]);
    }

    #region StateMachines
    public override void DoAIInterval()
    {
        base.DoAIInterval();
        if (StartOfRound.Instance.allPlayersDead || isEnemyDead)
            return;

        creatureAnimator.SetFloat(RunSpeedFloat, agent.velocity.magnitude / 3f);
        switch (currentBehaviourStateIndex)
        {
            case (int)MonarchState.Idle:
                DoIdleUpdate();
                break;
            case (int)MonarchState.AttackingGround:
                DoAttackingGroundUpdate();
                break;
            case (int)MonarchState.AttackingAir:
                DoAttackingAirUpdate();
                break;
            case (int)MonarchState.Death:
                DoDeathUpdate();
                break;
        }
    }

    private void DoIdleUpdate()
    {
        foreach (var player in StartOfRound.Instance.allPlayerScripts)
        {
            if (player.isPlayerDead || !player.isPlayerControlled)
                continue;

            if (Vector3.Distance(transform.position, player.transform.position) > 40)
                continue;

            creatureAnimator.SetBool(IsFlyingAnimation, true);
            smartAgentNavigator.StopSearchRoutine();
            agent.speed = 10f;
            agent.stoppingDistance = 10f;
            SwitchToBehaviourServerRpc((int)MonarchState.AttackingAir);
            return;
        }
    }

    private void DoAttackingGroundUpdate()
    {
        PlayerControllerB? closestPlayer = GetClosestPlayerToMonarch(out float distanceToClosestPlayer);
        if (closestPlayer == null && !isAttacking)
        {
            smartAgentNavigator.StartSearchRoutine(50);
            agent.stoppingDistance = 1f;
            SwitchToBehaviourServerRpc((int)MonarchState.Idle);
            return;
        }
        else if (distanceToClosestPlayer > 15 && !isAttacking)
        {
            agent.speed = 10f;
            creatureAnimator.SetBool(IsFlyingAnimation, true);
            agent.stoppingDistance = 10f;
            SwitchToBehaviourServerRpc((int)MonarchState.AttackingAir);
            return;
        }
        if (isAttacking) closestPlayer = targetPlayer;
        if (closestPlayer == null)
        {
            Plugin.Logger.LogWarning($"closestPlayer is null, distanceToClosestPlayer: {distanceToClosestPlayer}, isAttacking: {isAttacking}, targetPlayer: {targetPlayer}");
            return;
        }
        smartAgentNavigator.DoPathingToDestination(closestPlayer.transform.position);
        if (canAttack && distanceToClosestPlayer <= 1.5f + agent.stoppingDistance)
        {
            SetTargetServerRpc(Array.IndexOf(StartOfRound.Instance.allPlayerScripts, closestPlayer));
            StartCoroutine(AttackCooldownTimer(1.5f));
            isAttacking = true;
            creatureNetworkAnimator.SetTrigger(DoAttackAnimation);
        }
    }

    private void DoAttackingAirUpdate()
    {
        PlayerControllerB? closestPlayer = GetClosestPlayerToMonarch(out float distanceToClosestPlayer);
        if (closestPlayer == null && isAttacking)
        {
            smartAgentNavigator.StartSearchRoutine(50);
            agent.stoppingDistance = 1f;
            SwitchToBehaviourServerRpc((int)MonarchState.Idle);
            return;
        }
        else if (distanceToClosestPlayer <= 10 && isAttacking)
        {
            agent.speed = 5f;
            creatureAnimator.SetBool(IsFlyingAnimation, false);
            agent.stoppingDistance = 2f;
            SwitchToBehaviourServerRpc((int)MonarchState.AttackingGround);
            return;
        }

        if (isAttacking)
            closestPlayer = targetPlayer;

        if (closestPlayer == null)
        {
            Plugin.Logger.LogWarning($"closestPlayer is null, distanceToClosestPlayer: {distanceToClosestPlayer}, isAttacking: {isAttacking}, targetPlayer: {targetPlayer}");
            return;
        }
        smartAgentNavigator.DoPathingToDestination(closestPlayer.transform.position);
        if (canAttack && distanceToClosestPlayer <= 5f + agent.stoppingDistance)
        {
            SetTargetServerRpc(Array.IndexOf(StartOfRound.Instance.allPlayerScripts, closestPlayer));
            StartCoroutine(AttackCooldownTimer(5f));
            isAttacking = true;
            creatureNetworkAnimator.SetTrigger(DoAttackAnimation);
        }
    }

    private void DoDeathUpdate()
    {
        // Do nothing
    }

    #endregion

    public override void HitEnemy(int force = 1, PlayerControllerB? playerWhoHit = null, bool playHitSFX = false, int hitID = -1)
    {
        base.HitEnemy(force, playerWhoHit, playHitSFX, hitID);
        if (isEnemyDead) return;
        enemyHP -= force;

        if (IsOwner && enemyHP <= 0)
        {
            KillEnemyOnOwnerClient();
        }
    }

    public override void KillEnemy(bool destroy = false)
    {
        base.KillEnemy(destroy);
        smartAgentNavigator.StopSearchRoutine();
        if (IsServer)
            creatureAnimator.SetBool(IsDeadAnimation, true);

        SwitchToBehaviourStateOnLocalClient((int)MonarchState.Death);
        Monarchs.Remove(this);
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        if (Monarchs.Contains(this))
            Monarchs.Remove(this);
    }

    #region Misc Functions
    public IEnumerator AttackCooldownTimer(float seconds)
    {
        canAttack = false;
        yield return new WaitForSeconds(seconds);
        canAttack = true;
    }

    public PlayerControllerB? GetClosestPlayerToMonarch(out float distanceToClosestPlayer)
    {
        PlayerControllerB? closestPlayer = null;
        distanceToClosestPlayer = -1;
        foreach (var player in StartOfRound.Instance.allPlayerScripts)
        {
            if (player.isPlayerDead || !player.isPlayerControlled) continue;
            float distance = Vector3.Distance(transform.position, player.transform.position);
            if (distance > 50) continue;
            if (closestPlayer == null || distance < distanceToClosestPlayer)
            {
                closestPlayer = player;
                distanceToClosestPlayer = distance;
            }
        }
        return closestPlayer;
    }

    private Vector3 _currentBeamEnd = Vector3.zero;
    public IEnumerator ShootingEffect()
    {
        creatureSFX.PlayOneShot(BeamController._beamSound);
        BeamController._monarchParticle.Play();

        float totalDuration = 3f;
        float damageInterval = 0.25f;
        const float maxRange = 30f;
        const float followRadius = 5f;
        const float followSmoothing = 5f;

        _currentBeamEnd = GetDesiredBeamEnd(maxRange, followRadius);
        BeamController.SetBeamPosition(_currentBeamEnd);

        while (totalDuration > 0f)
        {
            yield return null;

            Vector3 targetEnd = GetDesiredBeamEnd(maxRange, followRadius);
            _currentBeamEnd = Vector3.Lerp(_currentBeamEnd, targetEnd, followSmoothing * Time.deltaTime);
            BeamController.SetBeamPosition(_currentBeamEnd);

            if (damageInterval <= 0f)
            {
                DoHitStuff(2, _currentBeamEnd);
                damageInterval = 0.25f;
            }

            damageInterval -= Time.deltaTime;
            totalDuration -= Time.deltaTime;
        }

        agent.speed = 5f;
        targetPlayer = null;
        isAttacking = false;
    }

    private Vector3 GetDesiredBeamEnd(float maxRange, float followRadius)
    {
        Transform start = BeamController._startBeamTransform;
        Transform dir = BeamController._raycastDirectionBeamTransform;
        RaycastHit hit;

        bool didHit = Physics.Raycast(
            start.position,
            dir.forward,
            out hit,
            maxRange,
            StartOfRound.Instance.collidersRoomMaskDefaultAndPlayers,
            QueryTriggerInteraction.Ignore
        );

        Vector3 rawEnd = didHit
            ? hit.point
            : start.position + dir.forward * maxRange;

        if (didHit && targetPlayer != null)
        {
            float distToPlayer = Vector3.Distance(hit.point, targetPlayer.transform.position);
            if (distToPlayer <= followRadius)
                return targetPlayer.transform.position;
        }

        return rawEnd;
    }

    private Collider[] _cachedColliders = new Collider[24];
    private List<IHittable> _iHittableList = new();
    private List<EnemyAI> _enemyAIList = new();
    private List<PlayerControllerB> _playerList = new();

    private void DoHitStuff(int damageToDeal, Vector3 startPosition)
    {
        _iHittableList.Clear();
        _enemyAIList.Clear();
        _playerList.Clear();

        int numHits = Physics.OverlapSphereNonAlloc(startPosition, 2f, _cachedColliders, CodeRebirthUtils.Instance.playersAndInteractableAndEnemiesAndPropsHazardMask, QueryTriggerInteraction.Collide);
        for (int i = 0; i < numHits; i++)
        {
            if (!_cachedColliders[i].gameObject.TryGetComponent(out IHittable iHittable))
                continue;

            if (iHittable is EnemyAICollisionDetect enemyAICollisionDetect)
            {
                if (_enemyAIList.Contains(enemyAICollisionDetect.mainScript))
                    continue;

                _enemyAIList.Add(enemyAICollisionDetect.mainScript);
                enemyAICollisionDetect.mainScript.HitEnemyOnLocalClient(damageToDeal, startPosition);
            }
            else if (iHittable is PlayerControllerB playerController)
            {
                if (_playerList.Contains(playerController))
                    continue;

                _playerList.Add(playerController);
                playerController.DamagePlayer(damageToDeal * 10, true, false, CauseOfDeath.Burning, 0, false, default);
            }
            else
            {
                _iHittableList.Add(iHittable);
            }
        }

        foreach (var iHittable in _iHittableList)
        {
            if (!IsOwner)
                continue;

            iHittable.Hit(damageToDeal, startPosition, null, true, -1);
        }
    }
    #endregion

    #region Animation Events
    public void GroundAttackAnimationEvent()
    {
        int numHits = Physics.OverlapSphereNonAlloc(MouthTransform.position, 1.5f, _cachedHits, StartOfRound.Instance.playersMask, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < numHits; i++)
        {
            if (!_cachedHits[i].TryGetComponent(out PlayerControllerB player))
                continue;

            player.DamagePlayer(35, true, false, CauseOfDeath.Snipped, 7, false, default);
        }
        creatureSFX.PlayOneShot(_biteSounds[enemyRandom.Next(0, _biteSounds.Length)]);
        targetPlayer = null;
        isAttacking = false;
    }

    public void AirAttackStartAnimEvent()
    {
        agent.speed = 0.25f;
        StartCoroutine(ShootingEffect());
    }

    #endregion
}