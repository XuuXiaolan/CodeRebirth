using System;
using GameNetcodeStuff;
using Unity.Netcode.Components;
using UnityEngine;

namespace CodeRebirth.src.Content.Enemies;
public class CarnivorousPlant : CodeRebirthEnemyAI
{
    public Transform[] eyes;
    public AnimationClip attackAnimation = null!;
    public NetworkAnimator networkAnimator = null!;
    private bool attacking = false;
    public enum State 
    {
        Idle,
        AttackTargetPlayer,
    }

    public override void Start() 
    {
        base.Start();
        agent.speed = 0;
        SwitchToBehaviourStateOnLocalClient((int)State.Idle);
    }

    public override void DoAIInterval()
    {
        base.DoAIInterval();

        switch (currentBehaviourStateIndex)
        {
            case (int)State.Idle:
                DoIdle();
                break;
            case (int)State.AttackTargetPlayer:
                DoAttackTargetPlayer();
                break;
        }
    }


    private void DoIdle()
    {
        foreach (PlayerControllerB player in StartOfRound.Instance.allPlayerScripts) {
            if (player.isInHangarShipRoom || player.isPlayerDead || !player.isPlayerControlled) continue;
            if (Vector3.Distance(transform.position, player.transform.position) <= 5) {
                SetTargetServerRpc(Array.IndexOf(StartOfRound.Instance.allPlayerScripts, player));
            }
        }
    }

    private void DoAttackTargetPlayer()
    {
        if (!attacking) 
        {
            attacking = true;
            TriggerAnimationServerRpc("startAttack");
            Invoke(nameof(EndAttack), attackAnimation.length);
        }
    }

    private void EndAttack()
    {
        attacking = false;
        if (targetPlayer.isPlayerDead || Vector3.Distance(transform.position, targetPlayer.transform.position) > 5) {
            SetTargetServerRpc(-1);
            SwitchToBehaviourStateOnLocalClient((int)State.Idle);
        }
    }
}