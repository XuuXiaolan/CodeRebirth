
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using GameNetcodeStuff;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;

namespace CodeRebirth.src.Content.Enemies;
public class SnailCatAI : CodeRebirthEnemyAI
{
    public SnailCatPhysicsProp propScript = null!;
    public string[] randomizedNames = [];

    private bool holdingBaby = false;
    private Coroutine? dropBabyCoroutine = null;
    private PlayerControllerB? playerHolding = null;
    public enum State
    {
        Wandering,
		Sleeping,
		Sitting,
		Grabbed
    }

    public override void Start()
    {
        base.Start();
        smartAgentNavigator.StartSearchRoutine(this.transform.position, 50);
        SwitchToBehaviourStateOnLocalClient((int)State.Wandering);
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
			case (int)State.Sleeping:
				DoSleeping();
				break;
			case (int)State.Sitting:
				DoSitting();
				break;
			case (int)State.Grabbed:
				DoGrabbed();
				break;
        }
    }

	private void DoWandering()
	{

	}

	private void DoSleeping()
	{

	}

	private void DoSitting()
	{

	}
	
	private void DoGrabbed()
	{

	}

	#endregion
    public void PickUpBabyLocalClient()
    {
		Plugin.ExtendedLogging($"picked up by {propScript.playerHeldBy}");
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
		holdingBaby = false;
		playerHolding = null;
		if (IsOwner)
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
		EnableOrDisableAgentWithRoutineServerRpc(true, true);
    }

    public override void HitEnemy(int force = 1, PlayerControllerB? playerWhoHit = null, bool playHitSFX = false, int hitID = -1)
    {
        base.HitEnemy(force, playerWhoHit, playHitSFX, hitID);
		if (IsOwner) propScript.ownerNetworkAnimator.SetTrigger(SnailCatPhysicsProp.HitAnimation);
        // trigger hit animation
    }

    private IEnumerator DropBabyAnimation(Vector3 dropOnPosition)
	{
		float time = Time.realtimeSinceStartup;
		yield return new WaitUntil(() => propScript.reachedFloorTarget || Time.realtimeSinceStartup - time > 2f);
        transform.position = dropOnPosition;
        inSpecialAnimation = false;
		dropBabyCoroutine = null;
		EnableOrDisableAgentWithRoutineServerRpc(true, true);
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
}