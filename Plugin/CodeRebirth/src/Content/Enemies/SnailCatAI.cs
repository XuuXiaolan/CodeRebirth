
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using GameNetcodeStuff;
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
    }

    public override void Start()
    {
        base.Start();
        StartSearch(transform.position);
        SwitchToBehaviourStateOnLocalClient((int)State.Wandering);
    }

    public override void DoAIInterval()
    {
        base.DoAIInterval();
        if (isEnemyDead || StartOfRound.Instance.allPlayersDead) return;

        switch(currentBehaviourStateIndex)
        {
            case (int)State.Wandering:
                agent.speed = 4f;
                break;
        }
    }

    public void PickUpBabyLocalClient()
    {
		currentOwnershipOnThisClient = (int)propScript.playerHeldBy.playerClientId;
		inSpecialAnimation = true;
		agent.enabled = false;
		holdingBaby = true;
		if (dropBabyCoroutine != null)
		{
			StopCoroutine(dropBabyCoroutine);
		}

		playerHolding = propScript.playerHeldBy;
    }

    public void DropBabyLocalClient()
    {
		holdingBaby = false;
		playerHolding = null;
		if (IsOwner)
		{
			ChangeOwnershipOfEnemy(StartOfRound.Instance.allPlayerScripts[0].actualClientId);
		}

		bool gotValidDropPosition = true;
		Vector3 itemFloorPosition = propScript.GetItemFloorPosition(propScript.previousPlayerHeldBy.transform.position + Vector3.up * 0.5f);
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
    }

	private IEnumerator DropBabyAnimation(Vector3 dropOnPosition)
	{
		float time = Time.realtimeSinceStartup;
		yield return new WaitUntil(() => propScript.reachedFloorTarget || Time.realtimeSinceStartup - time > 2f);
        transform.position = dropOnPosition;
        inSpecialAnimation = false;
		dropBabyCoroutine = null;
	}

	public Transform ChooseClosestNodeToPositionNoPathCheck(Vector3 pos)
	{
        IEnumerable<GameObject> allAINodes = RoundManager.Instance.insideAINodes.Concat(RoundManager.Instance.outsideAINodes);
		var nodesTempArray = allAINodes.OrderBy(x => Vector3.Distance(pos, x.transform.position));
		return nodesTempArray.First().transform;
	}
}