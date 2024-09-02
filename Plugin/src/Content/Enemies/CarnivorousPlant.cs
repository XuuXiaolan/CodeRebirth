using System;
using GameNetcodeStuff;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

namespace CodeRebirth.src.Content.Enemies;
public class CarnivorousPlant : CodeRebirthEnemyAI, INoiseListener
{
    public Transform[] eyes;
    public AnimationClip attackAnimation = null!;
    public NetworkAnimator networkAnimator = null!;
    public Transform mouth = null!;
    private bool carryingPlayerBody;
    private DeadBodyInfo? bodyBeingCarried;
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

    public override void Update()
    {
        base.Update();
        if (targetPlayer != null && currentBehaviourStateIndex == (int)State.AttackTargetPlayer)
        {
            // Calculate direction to target player
            Vector3 direction = (targetPlayer.transform.position - transform.position).normalized;
            
            // Calculate the rotation required to look at the target
            Quaternion lookRotation = Quaternion.LookRotation(direction);
            
            // Smoothly rotate towards the target player
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * 5f);
        }
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
            if (Vector3.Distance(transform.position, player.transform.position) <= 20) {
                SetTargetServerRpc(Array.IndexOf(StartOfRound.Instance.allPlayerScripts, player));
                SwitchToBehaviourServerRpc((int)State.AttackTargetPlayer);
            }
        }
    }

    private void DoAttackTargetPlayer()
    {
        if (!attacking) 
        {
            attacking = true;
            networkAnimator.SetTrigger("startAttack");
            Invoke(nameof(EndAttack), attackAnimation.length);
        }
    }

    private void EndAttack()
    {
        attacking = false;
        if (targetPlayer.isPlayerDead || Vector3.Distance(transform.position, targetPlayer.transform.position) > 5) {
            SetTargetServerRpc(-1);
            SwitchToBehaviourServerRpc((int)State.Idle);
        }
        DropPlayerBodyServerRpc();
    }

    public override void OnCollideWithPlayer(Collider other)
    {
        if (isEnemyDead) return;
        PlayerControllerB player = MeetsStandardPlayerCollisionConditions(other);
        if (player == null) return;
        player.KillPlayer(player.velocityLastFrame, true, CauseOfDeath.Crushing, 0, default);
        if (player.isPlayerDead && !carryingPlayerBody) CarryingDeadPlayerServerRpc(Array.IndexOf(StartOfRound.Instance.allPlayerScripts, player));
        // play player death particles.
    }

    [ServerRpc(RequireOwnership = false)]
    private void DropPlayerBodyServerRpc()
    {
        DropPlayerBodyClientRpc();
    }

    [ClientRpc]
    public void DropPlayerBodyClientRpc() 
    {
        DropPlayerBody();
    }

    private void DropPlayerBody()
	{
		if (!this.carryingPlayerBody || this.bodyBeingCarried == null)
		{
			return;
		}
		this.carryingPlayerBody = false;
		this.bodyBeingCarried.matchPositionExactly = false;
		this.bodyBeingCarried.attachedTo = null;
		this.bodyBeingCarried = null;
	}

    [ServerRpc(RequireOwnership = false)]
    private void CarryingDeadPlayerServerRpc(int playerIndex)
    {
        CarryingDeadPlayerClientRpc(playerIndex);
    }

    [ClientRpc]
    public void CarryingDeadPlayerClientRpc(int playerIndex)
    {
        var player = StartOfRound.Instance.allPlayerScripts[playerIndex];
        carryingPlayerBody = true;
        bodyBeingCarried = player.deadBody;
        bodyBeingCarried.attachedTo = this.mouth;
        bodyBeingCarried.attachedLimb = player.deadBody.bodyParts[0];
        bodyBeingCarried.matchPositionExactly = true;
    }
}