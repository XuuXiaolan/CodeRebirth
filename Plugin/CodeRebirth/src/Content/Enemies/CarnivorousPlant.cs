using System;
using GameNetcodeStuff;
using Unity.Netcode;
using UnityEngine;

namespace CodeRebirth.src.Content.Enemies;
public class CarnivorousPlant : CodeRebirthEnemyAI
{
    public Transform[] eyes;
    public AnimationClip attackAnimation = null!;
    public Transform mouth = null!;
    public AudioClip BiteSound = null!;
    public Gradient CarnivorousPlantColourGradient = new Gradient();
    private bool carryingPlayerBody;
    private DeadBodyInfo? bodyBeingCarried;
    private bool attacking = false;
    private static readonly int startAttack = Animator.StringToHash("startAttack");
    private static readonly int takeDamage = Animator.StringToHash("takeDamage");
    private static readonly int isDead = Animator.StringToHash("isDead");
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
        if (isEnemyDead) return;
        if (targetPlayer != null && currentBehaviourStateIndex == (int)State.AttackTargetPlayer)
        {
            // Calculate direction to target player
            Vector3 direction = (targetPlayer.transform.position - transform.position).normalized;
            
            // Calculate the rotation required to look at the target
            Quaternion lookRotation = Quaternion.LookRotation(direction);
            
            // Smoothly rotate towards the target player
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * 10f);
        }
        else
        {
            // Default slow rotation to the right (positive y-axis)
            float rotationSpeed = 2f; // Adjust speed here if needed
            transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime);
            
            // To rotate to the left (negative y-axis), use:
            // transform.Rotate(Vector3.up, -rotationSpeed * Time.deltaTime);
        }
    }

    public override void DoAIInterval()
    {
        base.DoAIInterval();
        if (isEnemyDead) return;
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
        foreach (PlayerControllerB player in StartOfRound.Instance.allPlayerScripts)
        {
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
            PlayBiteSoundClientRpc();
            attacking = true;
            creatureNetworkAnimator.SetTrigger(startAttack);
            Invoke(nameof(EndAttack), attackAnimation.length);
        }
    }

    [ClientRpc]
    private void PlayBiteSoundClientRpc()
    {
        creatureVoice.PlayOneShot(BiteSound);
    }

    private void EndAttack()
    {
        attacking = false;
        if (targetPlayer.isPlayerDead || Vector3.Distance(transform.position, targetPlayer.transform.position) > 5)
        {
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

    public override void HitEnemy(int force = 1, PlayerControllerB? playerWhoHit = null, bool playHitSFX = false, int hitID = -1)
    {
        base.HitEnemy(force, playerWhoHit, playHitSFX, hitID);
        enemyHP -= force;
        creatureVoice.PlayOneShot(enemyType.hitBodySFX);
        if (IsServer) creatureNetworkAnimator.SetTrigger(takeDamage);

        if (enemyHP <= 0 && !isEnemyDead)
        {
            if (IsOwner)
            {
                creatureAnimator.SetBool(isDead, true);
                KillEnemyOnOwnerClient();
            }
        }
    }
}