
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using CodeRebirth.src.MiscScripts;
using CodeRebirth.src.Util.Extensions;
using GameNetcodeStuff;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;

namespace CodeRebirth.src.Content.Enemies;
public class SnailCatAI : CodeRebirthEnemyAI
{
    public SnailCatPhysicsProp propScript = null!;
    public string[] randomizedNames = [];
	public ScanNodeProperties scanNodeProperties = null!;
	public AudioClip[] randomNoises = [];
	public AudioClip[] hitSounds = [];
	public AudioClip enemyDetectSound = null!;
	public AudioClip[] wiwiwiiiSound = [];
	public AudioClip[] footStepSounds = [];

	private string currentName = "";
    private bool holdingBaby = false;
    private Coroutine? dropBabyCoroutine = null;
    private PlayerControllerB? playerHolding = null;
	private float specialActionTimer = 1f;
	private float randomNoiseInterval = 0f;
	private float detectEnemyInterval = 0f;
	private bool isWiWiWiii = false;

	public enum State
    {
        Wandering,
		Following,
		Sleeping,
		Sitting,
		Grabbed
    }

    public override void Start()
    {
        base.Start();
        QualitySettings.skinWeights = SkinWeights.FourBones;
		string randomName = randomizedNames[enemyRandom.Next(0, randomizedNames.Length)];
		float randomScale = enemyRandom.NextFloat(0.9f, 1.1f);
		this.transform.localScale *= randomScale;
		propScript.originalScale = this.transform.localScale;
		scanNodeProperties.headerText = randomName;
		currentName = randomName;
		isWiWiWiii = currentName == "Wiwiwii";
        if (IsServer) smartAgentNavigator.StartSearchRoutine(this.transform.position, 50);
    }

    public override void Update()
    {
        base.Update();
		if (isEnemyDead || StartOfRound.Instance.allPlayersDead) return;
		randomNoiseInterval -= Time.deltaTime;
		if (randomNoiseInterval <= 0)
		{
			randomNoiseInterval = enemyRandom.NextFloat(7.5f, 15.5f);
			if (isWiWiWiii)
			{
				creatureVoice.PlayOneShot(wiwiwiiiSound[enemyRandom.Next(0, wiwiwiiiSound.Length)]);
			}
			else
			{
				creatureVoice.PlayOneShot(randomNoises[enemyRandom.Next(0, randomNoises.Length)]);
			}
		}

		if (currentBehaviourStateIndex != (int)State.Grabbed && currentBehaviourStateIndex != (int)State.Following) return;
		detectEnemyInterval -= Time.deltaTime;
		if (detectEnemyInterval <= 0)
		{
			detectEnemyInterval = enemyRandom.NextFloat(7.5f, 15.5f);
			bool enemyNearby = false;
			foreach (var enemy in RoundManager.Instance.SpawnedEnemies)
			{
				if (enemy is SnailCatAI) continue;
				float distance = Vector3.Distance(transform.position, enemy.transform.position);
				if (distance < 15)
				{
					enemyNearby = true;
					break;
				}
			}
			if (!enemyNearby) return;
			creatureVoice.PlayOneShot(enemyDetectSound);
		}
    }

    #region State Machines
    public override void DoAIInterval()
    {
        base.DoAIInterval();
        if (isEnemyDead || StartOfRound.Instance.allPlayersDead) return;

        switch(currentBehaviourStateIndex)
        {
            case (int)State.Wandering:
				DoWandering();
                break;
			case (int)State.Following:
				DoFollowing();
				break;
			case (int)State.Sleeping:
				break;
			case (int)State.Sitting:
				break;
			case (int)State.Grabbed:
				break;
        }
    }

	private void DoWandering()
	{
		foreach (var player in StartOfRound.Instance.allPlayerScripts)
		{
			if (player.isPlayerDead || !player.isPlayerControlled) continue;
			float distance = Vector3.Distance(transform.position, player.transform.position);
			if (distance > 5) continue;
			SetTargetServerRpc(Array.IndexOf(StartOfRound.Instance.allPlayerScripts, player));
			SwitchToBehaviourServerRpc((int)State.Following);
			return;
		}

		specialActionTimer -= AIIntervalTime;
		if (specialActionTimer <= 0)
		{
			smartAgentNavigator.StopSearchRoutine();
			agent.SetDestination(transform.position);
			specialActionTimer = UnityEngine.Random.Range(7.5f, 15f);
			if (UnityEngine.Random.Range(0, 100) < 50)
			{
				creatureAnimator.SetBool(SnailCatPhysicsProp.SleepingAnimation, true);
				SwitchToBehaviourServerRpc((int)State.Sleeping);
				StartCoroutine(ChangeStateWithDelayRoutine(20, State.Sleeping, SnailCatPhysicsProp.SleepingAnimation));
			}
			else
			{
				creatureAnimator.SetBool(SnailCatPhysicsProp.SittingAnimation, true);
				SwitchToBehaviourServerRpc((int)State.Sitting);
				StartCoroutine(ChangeStateWithDelayRoutine(10, State.Sitting, SnailCatPhysicsProp.SittingAnimation));
			}
		}
	}

	private void DoFollowing()
	{
		smartAgentNavigator.DoPathingToDestination(targetPlayer.transform.position);
		float distanceToPlayer = Vector3.Distance(transform.position, targetPlayer.transform.position);
		if (distanceToPlayer > 15)
		{
			// agent.speed = 4f;
			SetTargetServerRpc(-1);
			SwitchToBehaviourServerRpc((int)State.Wandering);
			smartAgentNavigator.StartSearchRoutine(this.transform.position, 50);
			return;
		}
	}

	#endregion
	public IEnumerator ChangeStateWithDelayRoutine(float delay, State stateToSwitchOutOf, int animationToSwitchOutOf)
	{
		yield return new WaitForSeconds(delay);
		if (currentBehaviourStateIndex != (int)stateToSwitchOutOf) yield break;
		smartAgentNavigator.StartSearchRoutine(this.transform.position, 50);
		creatureAnimator.SetBool(animationToSwitchOutOf, false);
		SwitchToBehaviourServerRpc((int)State.Wandering);
	}

    public void PickUpBabyLocalClient()
    {
		Plugin.ExtendedLogging($"picked up by {propScript.playerHeldBy}");
		SwitchToBehaviourServerRpc((int)State.Grabbed);
		EnableOrDisableAgentWithRoutineServerRpc(false, false);
		currentOwnershipOnThisClient = (int)propScript.playerHeldBy.playerClientId;
		inSpecialAnimation = true;
		holdingBaby = true;
		if (dropBabyCoroutine != null)
		{
			StopCoroutine(dropBabyCoroutine);
		}

		playerHolding = propScript.playerHeldBy;
    }

    public void DropBabyLocalClient()
    {
		Plugin.ExtendedLogging($"dropped by {propScript.previousPlayerHeldBy}");
		SwitchToBehaviourStateOnLocalClient((int)State.Wandering);
		holdingBaby = false;
		playerHolding = null;
		if (IsOwner && !IsOwnedByServer)
		{
			ChangeOwnershipOfEnemy(StartOfRound.Instance.allPlayerScripts[0].actualClientId);
		}

		bool gotValidDropPosition = true;
		Vector3 startPosition = this.transform.position;
		if (propScript.previousPlayerHeldBy == null)
		{
			gotValidDropPosition = false;
		}
		else
		{
			startPosition = propScript.previousPlayerHeldBy.transform.position;
		}
		Vector3 itemFloorPosition = propScript.GetItemFloorPosition(startPosition + Vector3.up * 0.5f);
		Vector3 groundNavmeshPosition = RoundManager.Instance.GetNavMeshPosition(itemFloorPosition, default(NavMeshHit), 10f, -1);

		if (!RoundManager.Instance.GotNavMeshPositionResult)
		{
			gotValidDropPosition = false;
			itemFloorPosition = propScript.startFallingPosition;
			if (propScript.transform.parent != null)
			{
				itemFloorPosition = propScript.transform.parent.TransformPoint(propScript.startFallingPosition);
			}
			Transform transform = ChooseClosestNodeToPositionNoPathCheck(itemFloorPosition);
			groundNavmeshPosition = RoundManager.Instance.GetNavMeshPosition(transform.transform.position, default(NavMeshHit), 5f, -1);
		}

		if (propScript.transform.parent == null)
		{
			propScript.targetFloorPosition = groundNavmeshPosition;
		}
		else
		{
			propScript.targetFloorPosition = propScript.transform.parent.InverseTransformPoint(groundNavmeshPosition);
		}

		if (gotValidDropPosition)
		{
			if (dropBabyCoroutine != null)
			{
				StopCoroutine(dropBabyCoroutine);
			}
			dropBabyCoroutine = StartCoroutine(DropBabyAnimation(groundNavmeshPosition));
			return;
		}

		transform.position = groundNavmeshPosition;
		inSpecialAnimation = false;
		if (!IsServer) return;
		agent.enabled = true;
		smartAgentNavigator.enabled = true;
		smartAgentNavigator.StartSearchRoutine(this.transform.position, 50);
    }

    public override void HitEnemy(int force = 1, PlayerControllerB? playerWhoHit = null, bool playHitSFX = false, int hitID = -1)
    {
        base.HitEnemy(force, playerWhoHit, playHitSFX, hitID);
		if (propScript.IsOwner) propScript.ownerNetworkAnimator.SetTrigger(SnailCatPhysicsProp.HitAnimation);
        // trigger hit animation
		creatureVoice.PlayOneShot(hitSounds[enemyRandom.Next(0, hitSounds.Length)]);
    }

    private IEnumerator DropBabyAnimation(Vector3 dropOnPosition)
	{
		float time = Time.realtimeSinceStartup;
		yield return new WaitUntil(() => propScript.reachedFloorTarget || Time.realtimeSinceStartup - time > 2f);
        transform.position = dropOnPosition;
        inSpecialAnimation = false;
		dropBabyCoroutine = null;
		if (!IsServer) yield break;
		agent.enabled = true;
		smartAgentNavigator.enabled = true;
		smartAgentNavigator.StartSearchRoutine(this.transform.position, 50);
	}

	public Transform ChooseClosestNodeToPositionNoPathCheck(Vector3 pos)
	{
        IEnumerable<GameObject> allAINodes = RoundManager.Instance.insideAINodes.Concat(RoundManager.Instance.outsideAINodes);
		var nodesTempArray = allAINodes.OrderBy(x => Vector3.Distance(pos, x.transform.position));
		return nodesTempArray.First().transform;
	}

	[ServerRpc(RequireOwnership = false)]
	private void EnableOrDisableAgentWithRoutineServerRpc(bool enable, bool startSearchRoutine)
	{
		agent.enabled = enable;
		smartAgentNavigator.enabled = enable;
		if (startSearchRoutine)
		{
			smartAgentNavigator.StartSearchRoutine(this.transform.position, 50);
		}
		else
		{
			smartAgentNavigator.StopSearchRoutine();
		}
	}

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
		// CRUtilities.CreateExplosion(this.transform.position, true, 99999, 0, 15, 999, null, null, 1000f);
    }

	public void PlayFootStepSoundAnimEvent()
	{
		// creatureSFX.PlayOneShot(footStepSounds[UnityEngine.Random.Range(0, footStepSounds.Length)]);
	}
}