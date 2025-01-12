using System.Collections.Generic;
using System.Linq;
using GameNetcodeStuff;
using UnityEngine;
using UnityEngine.AI;

namespace CodeRebirth.src.Content.Enemies;
public class Transporter : CodeRebirthEnemyAI
{
    [HideInInspector] public static List<GameObject> objectsToTransport = new();

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
        smartAgentNavigator.StartSearchRoutine(this.transform.position, 20);
        SwitchToBehaviourStateOnLocalClient((int)TransporterStates.Idle);
        if (!IsServer) return;
        TryFindAnyTransportableObjectViaAsyncPathfinding();
    }

    public override void DoAIInterval()
    {
        base.DoAIInterval();
        creatureAnimator.SetFloat("RunSpeedFloat", agent.velocity.magnitude); // todo: turn that into static int thing
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

    #region State Behaviors

    private void DoIdle()
    {
        if (objectsToTransport.Count == 0)
        {
            transportTarget = null;
            return;
        }
    }

    private void TryFindAnyTransportableObjectViaAsyncPathfinding()
    {
        // Gather all valid objects#
        Plugin.ExtendedLogging($"Transporter: Transporting {objectsToTransport.Count} objects", (int)Logging_Level.Medium);
        IEnumerable<(GameObject obj, Vector3 position)> candidateObjects = objectsToTransport
            .Where(kv => kv != null)
            .Select(kv => (kv, kv.transform.position));

        smartAgentNavigator.CheckPaths(candidateObjects, CheckIfNeedToChangeState);
    }

    public void CheckIfNeedToChangeState(List<GameObject> objects)
    {
        if (objects.Count > 0)
        {
            Plugin.ExtendedLogging($"Transporter: Found {objects.Count} objects", (int)Logging_Level.Low);
            transportTarget = objects[UnityEngine.Random.Range(0, objects.Count)];
            SwitchToBehaviourServerRpc((int)TransporterStates.Transporting);
        }
    }

    private void DoTransporting()
    {
        if (transportTarget == null)
        {
            SwitchToBehaviourServerRpc((int)TransporterStates.Idle);
            return;
        }
        Plugin.ExtendedLogging($"Transporter: Transporting to {transportTarget.name}", (int)Logging_Level.Highest);

        float dist = Vector3.Distance(transportTarget.transform.position, transform.position);

        // Path to the object's position
        smartAgentNavigator.DoPathingToDestination(
            transportTarget.transform.position
        );

        // If close enough to "pick up"
        float pickupRange = 2.5f;
        if (dist <= pickupRange)
        {
            bool foundRoute = false;
            while (!foundRoute)
            {
                NavMesh.SamplePosition(PickRandomOutsideOrInsideAINode(), out NavMeshHit hit, 5, NavMesh.AllAreas);
                currentEndDestination = hit.position;
                if (smartAgentNavigator.CanPathToPoint(this.transform.position, currentEndDestination) > 0)
                {
                    foundRoute = true;
                }
            }
            SwitchToBehaviourServerRpc((int)TransporterStates.Repositioning);
        }
    }

    private void DoRepositioning()
    {
        if (transportTarget == null)
        {
            TryFindAnyTransportableObjectViaAsyncPathfinding();
            return;
        }

        // Move to that final location
        smartAgentNavigator.DoPathingToDestination(
            currentEndDestination
        );

        // If we reached or nearly reached the drop-off
        if (Vector3.Distance(transform.position, currentEndDestination) <= agent.stoppingDistance + 1.5f)
        {
            Plugin.ExtendedLogging($"Transporter: Dropped off {transportTarget.name}", (int)Logging_Level.Low);
            currentEndDestination = Vector3.zero;
            transportTarget = null;
            SwitchToBehaviourServerRpc((int)TransporterStates.Idle);
            TryFindAnyTransportableObjectViaAsyncPathfinding();
        }
    }

    #endregion

    #region Example Utility

    /// <summary>
    /// If you want to pick a random node inside or outside, 
    /// you can adapt this to use the dictionary's boolean or other logic.
    /// </summary>
    public Vector3 PickRandomOutsideOrInsideAINode()
    {
        // In your actual game: 
        // - If you want an outside node, pick from RoundManager.Instance.outsideAINodes
        // - If you want an inside node, pick from RoundManager.Instance.insideAINodes
        List<GameObject> allNodes = RoundManager.Instance.outsideAINodes
            .Concat(RoundManager.Instance.insideAINodes)
            .ToList();
        if (allNodes.Count == 0) return transform.position;

        int index = UnityEngine.Random.Range(0, allNodes.Count);
        NavMesh.SamplePosition(allNodes[index].transform.position, out NavMeshHit hit, 5, NavMesh.AllAreas);
        return hit.position;
    }

    #endregion

    #region Combat / Animation

    public override void HitEnemy(int force = 1, PlayerControllerB? playerWhoHit = null, bool playHitSFX = false, int hitID = -1)
    {
        base.HitEnemy(force, playerWhoHit, playHitSFX, hitID);
        // Possibly trigger an "on hit" animation or effect
    }

    public void OnHitAnimEvent()
    {
        // Example effect: speed up in annoyance
        agent.speed += 0.5f;
    }

    #endregion
}