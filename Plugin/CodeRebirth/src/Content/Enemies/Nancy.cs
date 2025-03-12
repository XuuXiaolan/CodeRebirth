using System;
using GameNetcodeStuff;
using Unity.Netcode;
using UnityEngine;

namespace CodeRebirth.src.Content.Enemies;
public class Nancy : CodeRebirthEnemyAI
{

    private float checkLengthTimer = 2f;
    private float checkTimer = 0f;
    private Vector3 playersLastPosition = Vector3.zero;

    private static readonly int HealModeAnimation = Animator.StringToHash("HealMode"); // Bool
    private static readonly int HealingPlayerAnimation = Animator.StringToHash("HealingPlayer"); // Bool
    private static readonly int FailHealAnimation = Animator.StringToHash("FailHeal"); // Trigger
    private static readonly int RunSpeedFloat = Animator.StringToHash("RunSpeed"); // Float
    public enum NancyState
    {
        Wandering,
        ChasingHealTarget,
        HealingTarget,
    }

    public override void Start()
    {
        base.Start();
        smartAgentNavigator.StartSearchRoutine(this.transform.position, 30f);
    }

    #region StateMachine

    public override void Update()
    {
        base.Update();

        checkTimer -= Time.deltaTime;
        if (targetPlayer != null || currentBehaviourStateIndex != (int)NancyState.Wandering || checkTimer > 0) return;

        checkTimer = checkLengthTimer;
        PlayerControllerB localPlayer = GameNetworkManager.Instance.localPlayerController;
        if (localPlayer.isInsideFactory && isOutside || !isOutside && !localPlayer.isInsideFactory) return;
        if (localPlayer.health >= 100) return;
        float distance = Vector3.Distance(transform.position, localPlayer.transform.position);
        if (distance < 30 && smartAgentNavigator.CanPathToPoint(this.transform.position, localPlayer.transform.position) <= 20f)
        {
            DoBoolAnimationServerRpc(HealModeAnimation, true);
            SetTargetServerRpc(Array.IndexOf(StartOfRound.Instance.allPlayerScripts, localPlayer));
            smartAgentNavigator.StopSearchRoutine();
            SwitchToBehaviourServerRpc((int)NancyState.ChasingHealTarget);
        }
    }

    public override void DoAIInterval()
    {
        base.DoAIInterval();
        if (StartOfRound.Instance.allPlayersDead || isEnemyDead) return;

        creatureAnimator.SetFloat(RunSpeedFloat, agent.velocity.magnitude);
        if (targetPlayer != null && targetPlayer.isPlayerDead)
        {
            creatureAnimator.SetBool(HealModeAnimation, false);
            creatureAnimator.SetBool(HealingPlayerAnimation, false);
            SetTargetServerRpc(-1);
            smartAgentNavigator.StartSearchRoutine(this.transform.position, 30f);
            SwitchToBehaviourServerRpc((int)NancyState.Wandering);
            return;
        }

        switch (currentBehaviourStateIndex)
        {
            case (int)NancyState.Wandering:
                DoWandering();
                break;
            case (int)NancyState.ChasingHealTarget:
                DoChasingHealTarget();
                break;
            case (int)NancyState.HealingTarget:
                DoHealingTarget();
                break;
        }
    }

    public void DoWandering()
    {

    }

    public void DoChasingHealTarget()
    {
        float distanceToPlayer = Vector3.Distance(this.transform.position, targetPlayer.transform.position);
        if (distanceToPlayer > 35)
        {
            creatureAnimator.SetBool(HealModeAnimation, false);
            SetTargetServerRpc(-1);
            smartAgentNavigator.StartSearchRoutine(this.transform.position, 30f);
            SwitchToBehaviourServerRpc((int)NancyState.Wandering);
            return;
        }
        smartAgentNavigator.DoPathingToDestination(targetPlayer.transform.position);

        if (Vector3.Distance(this.transform.position, targetPlayer.transform.position) <= 2)
        {
            SwitchToBehaviourServerRpc((int)NancyState.HealingTarget);
            playersLastPosition = targetPlayer.transform.position;
            agent.velocity = Vector3.zero;
            creatureAnimator.SetBool(HealingPlayerAnimation, true);
        }
    }

    public void DoHealingTarget()
    {
        float distanceToPlayer = Vector3.Distance(this.transform.position, targetPlayer.transform.position);
        int currentHealth = targetPlayer.health;
        if (currentHealth >= 100)
        {
            creatureAnimator.SetBool(HealModeAnimation, false);
            creatureAnimator.SetBool(HealingPlayerAnimation, false);
            SetTargetServerRpc(-1);
            smartAgentNavigator.StartSearchRoutine(this.transform.position, 30f);
            SwitchToBehaviourServerRpc((int)NancyState.Wandering);
            return;
        }

        float distanceFromLastAIInterval = Vector3.Distance(targetPlayer.transform.position, playersLastPosition);
        playersLastPosition = targetPlayer.transform.position;
        Plugin.ExtendedLogging($"Distance from last AI interval: {distanceFromLastAIInterval}");
        if (distanceFromLastAIInterval > 0.5f)
        {
            creatureNetworkAnimator.SetTrigger(FailHealAnimation);
            targetPlayer.DamagePlayer(40, true, true, CauseOfDeath.Stabbing, 0, false, default);
        }
        else if (currentHealth < 100)
        {
            targetPlayer.DamagePlayer(-10, false, true, CauseOfDeath.Unknown, 0, false, default);
        }

        if (distanceToPlayer > 35)
        {
            creatureAnimator.SetBool(HealModeAnimation, false);
            creatureAnimator.SetBool(HealingPlayerAnimation, false);
            SetTargetServerRpc(-1);
            smartAgentNavigator.StartSearchRoutine(this.transform.position, 30f);
            SwitchToBehaviourServerRpc((int)NancyState.Wandering);
            return;
        }
        else if (distanceToPlayer > 2.5f)
        {
            creatureAnimator.SetBool(HealingPlayerAnimation, false);
            SwitchToBehaviourServerRpc((int)NancyState.ChasingHealTarget);
            return;
        }
    }

    #endregion
    [ServerRpc(RequireOwnership=false)]
    private void DoBoolAnimationServerRpc(int animationHash, bool value)
    {
        creatureAnimator.SetBool(animationHash, value);
    }
}