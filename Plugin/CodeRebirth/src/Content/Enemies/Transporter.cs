using System.Collections.Generic;
using System.Linq;
using GameNetcodeStuff;
using UnityEngine;

namespace CodeRebirth.src.Content.Enemies;
public class Transporter : CodeRebirthEnemyAI
{
    [HideInInspector] public Dictionary<GameObject, bool> objectsToTransport = new(); // Remove object from list once transported.

    private GameObject? transportTarget = null;
    private Vector3 currentEndDestination = Vector3.zero;

    public enum TransporterStates
    {
        Idle,
        Transporting,
        Repositioning
    }

    public override void Start()
    {
        base.Start();
        SwitchToBehaviourStateOnLocalClient((int)TransporterStates.Idle);
    }

    public override void DoAIInterval()
    {
        base.DoAIInterval();

        switch (currentBehaviourStateIndex)
        {
            case (int)TransporterStates.Idle:
                DoIdle();
                break;
            case (int)TransporterStates.Transporting:
                DoTransporting();
                break;
            case (int)TransporterStates.Repositioning:
                DoRepositioning();
                break;
        }
    }

    public void DoIdle()
    {
        if (objectsToTransport.Count == 0)
        {
            transportTarget = null;
            return;
        }

        // PickRandomTargetToTransport();
        foreach (var objectToTransport in objectsToTransport.Keys)
        {
            if (objectToTransport == null) continue;
            if (objectsToTransport[objectToTransport]) continue;

            if (Vector3.Distance(objectToTransport.transform.position, this.transform.position) <= 10)
            {
                transportTarget = objectToTransport;
                SwitchToBehaviourServerRpc((int)TransporterStates.Transporting);
                return;
            }
        }
    }

    public void DoTransporting()
    {
        if (transportTarget == null || Vector3.Distance(transportTarget.transform.position, this.transform.position) > 5)
        {
            transportTarget = null;
            SwitchToBehaviourServerRpc((int)TransporterStates.Idle);
            return;
        }

        smartAgentNavigator.DoPathingToDestination(transportTarget.transform.position, !isOutside, false, null);
        
    }

    public void DoRepositioning()
    {
        if (transportTarget == null)
        {
            SwitchToBehaviourServerRpc((int)TransporterStates.Idle);
            return;
        }
        if (Vector3.Distance(transportTarget.transform.position, currentEndDestination) <= agent.stoppingDistance + 1.5f)
        {
            // Drop the object down slowly via animation?
            transportTarget = null;
            SwitchToBehaviourServerRpc((int)TransporterStates.Idle);
        }
    }

    public void PickRandomTargetToTransport()
    {
        List<GameObject> availableTargets = objectsToTransport.Keys
            .Where(kv => kv != null)
            .ToList();

        // We’ll try a certain number of times to pick a target; 
        // in the worst case, we check every available target once.
        int attempts = 0;

        while (attempts < availableTargets.Count)
        {
            // 1) Pick a random target.
            int randomTargetIndex = Random.Range(0, availableTargets.Count);
            GameObject candidateTarget = availableTargets[randomTargetIndex];

            bool foundViableNode = false;

            for (int i = 0; i < 10; i++)
            {
                Vector3 candidateNode = PickRandomOutsideOrInsideAINode();
                // Replace the "IsPathViable" check with whatever logic 
                // you use to see if your agent can reach candidateNode.
                // This example presumes a hypothetical "CanPathToDestination" method.
                if (smartAgentNavigator.CanPathToDestination(candidateNode, !isOutside, false))
                {
                    // 3) Found a valid node for this target: set everything, then exit.
                    transportTarget = candidateTarget;
                    currentEndDestination = candidateNode;
                    foundViableNode = true;
                    break;
                }
            }

            if (foundViableNode)
            {
                // As soon as we find a viable target+destination, we’re done.
                return;
            }
            else
            {
                // If this particular target had no valid node in 10 tries, 
                // remove it from our list and try another random target.
                availableTargets.RemoveAt(randomTargetIndex);

                // If we run out of targets, no luck. Just exit.
                if (availableTargets.Count == 0) return;

                attempts++;
            }
        }
    }

    public Vector3 PickRandomOutsideOrInsideAINode()
    {
        List<GameObject> listOfNodes = RoundManager.Instance.outsideAINodes.ToList().Concat(RoundManager.Instance.insideAINodes).ToList();
        // Pick random node, check if it's viable to path to, if not, remove it from list, and repeat.
        return Vector3.zero;
    }

    public override void HitEnemy(int force = 1, PlayerControllerB? playerWhoHit = null, bool playHitSFX = false, int hitID = -1)
    {
        base.HitEnemy(force, playerWhoHit, playHitSFX, hitID);
        // Play on hit animation.
    }

    public void OnHitAnimEvent()
    {
        agent.speed += 0.5f;
    }
}