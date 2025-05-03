using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using GameNetcodeStuff;
using UnityEngine;

namespace CodeRebirth.src.Content.Enemies;
public class Monarch : CodeRebirthEnemyAI
{
    public AudioSource UltraCreatureVoice = null!;
    public Transform[] AirAttackTransforms = [];
    public Transform MouthTransform = null!;

    public static HashSet<Monarch> Monarchs = new();

    public enum MonarchState
    {
        Idle,
        AttackingGround,
        AttackingAir,
        Death,
    }

    private bool canAttack = true;
    private bool isAttacking = false;
    private Collider[] cachedHits = new Collider[8];
    private static readonly int DoAttackAnimation = Animator.StringToHash("doAttack"); // trigger
    private static readonly int IsFlyingAnimation = Animator.StringToHash("isFlying"); // Bool
    private static readonly int IsDeadAnimation = Animator.StringToHash("isDead"); // Bool
    private static readonly int RunSpeedFloat = Animator.StringToHash("RunSpeed"); // Float

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        HUDManager.Instance.ShakeCamera(ScreenShakeType.VeryStrong);
        HUDManager.Instance.ShakeCamera(ScreenShakeType.Long);
        UltraCreatureVoice.Play();

        Monarchs.Add(this);

        int randomNumberToSpawn = enemyRandom.Next(2, 5);
        if (!IsServer) return;
        for (int i = 0; i < randomNumberToSpawn; i++)
        {
            RoundManager.Instance.SpawnEnemyGameObject(RoundManager.Instance.GetRandomNavMeshPositionInRadiusSpherical(this.transform.position, 30, default), -1, -1, EnemyHandler.Instance.Monarch!.EnemyDefinitions.GetCREnemyDefinitionWithEnemyName("CutieFly")!.enemyType);
        }
    }

    public override void Start()
    {
        base.Start();
        if (IsServer) smartAgentNavigator.StartSearchRoutine(50);
        SwitchToBehaviourStateOnLocalClient((int)MonarchState.Idle);
    }

    #region StateMachines
    public override void DoAIInterval()
    {
        base.DoAIInterval();
        if (StartOfRound.Instance.allPlayersDead) return;

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
            if (player.isPlayerDead || !player.isPlayerControlled) continue;
            if (Vector3.Distance(transform.position, player.transform.position) > 40) continue;
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
        if (isAttacking) closestPlayer = targetPlayer;
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
        if (IsServer) creatureAnimator.SetBool(IsDeadAnimation, true);
        SwitchToBehaviourStateOnLocalClient((int)MonarchState.Death);
        Monarchs.Remove(this);
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        if (Monarchs.Contains(this)) Monarchs.Remove(this);
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

    #endregion

    #region Animation Events
    public void GroundAttackAnimationEvent()
    {
        int numHits = Physics.OverlapSphereNonAlloc(MouthTransform.position, 1.5f, cachedHits, StartOfRound.Instance.playersMask, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < numHits; i++)
        {
            if (!cachedHits[i].TryGetComponent(out PlayerControllerB player) || player != GameNetworkManager.Instance.localPlayerController) continue;
            player.DamagePlayer(35, true, true, CauseOfDeath.Mauling, 0, false, default);
        }
        targetPlayer = null;
        isAttacking = false;
    }

    public void AirAttackAnimationEvent()
    {
        // aim at target player set before the animation event plays.
        targetPlayer = null;
        isAttacking = false;
    }

    #endregion
}