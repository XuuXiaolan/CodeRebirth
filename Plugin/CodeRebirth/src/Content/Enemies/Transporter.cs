using System.Collections.Generic;
using System.Linq;
using CodeRebirth.src.Content.Maps;
using GameNetcodeStuff;
using UnityEngine;
using UnityEngine.AI;

namespace CodeRebirth.src.Content.Enemies;
public class Transporter : CodeRebirthEnemyAI
{
    /// <summary>
    /// Key = GameObject (the object the Transporter wants to move)
    /// Value = whether it's currently outside (true) or inside (false).
    /// </summary>
    [HideInInspector] 
    public Dictionary<GameObject, bool> objectsToTransport = new();

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
        foreach (var laser in FindObjectsOfType<ItemCrate>())
        {
            objectsToTransport.Add(laser.gameObject, true);
        }
        smartAgentNavigator.StartSearchRoutine(this.transform.position, 20);
        SwitchToBehaviourStateOnLocalClient((int)TransporterStates.Idle);

        smartAgentNavigator.OnUseEntranceTeleport.AddListener(OnEntranceTeleport);
    }

    // If you unspawn or disable this enemy, you may want to unsubscribe:
    public override void OnDestroy()
    {
        base.OnDestroy();
        smartAgentNavigator.OnUseEntranceTeleport.RemoveListener(OnEntranceTeleport);
    }

    private void OnEntranceTeleport(bool newOutsideState)
    {
        // If we are currently carrying a target (transportTarget), update its state.
        if (transportTarget != null && objectsToTransport.ContainsKey(transportTarget))
        {
            objectsToTransport[transportTarget] = newOutsideState;
            Debug.Log($"Transporter: Updated '{transportTarget.name}' to outside={newOutsideState}");
        }
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

        TryFindAnyTransportableObjectViaEntrances();
    }

    private void TryFindAnyTransportableObjectViaEntrances()
    {
        // Gather all valid objects
        var candidateObjects = objectsToTransport.Keys
            .Where(kv => kv != null)
            .ToList();

        // Example: pick them in random order, or just the first that works
        foreach (var obj in candidateObjects)
        {
            bool objectIsOutside = objectsToTransport[obj];
            if ((isOutside && objectIsOutside) || (!objectIsOutside && !isOutside))
            {
                transportTarget = obj;

                // Switch to transporting
                smartAgentNavigator.StopSearchRoutine();
                SwitchToBehaviourServerRpc((int)TransporterStates.Transporting);
                return;
            }

            Plugin.ExtendedLogging($"Transporter: Trying to find a viable entrance for {obj.name}");
            transportTarget = obj;

            // Switch to transporting
            smartAgentNavigator.StopSearchRoutine();
            SwitchToBehaviourServerRpc((int)TransporterStates.Transporting);
            return;
        }
    }

    private void DoTransporting()
    {
        if (transportTarget == null)
        {
            SwitchToBehaviourServerRpc((int)TransporterStates.Idle);
            return;
        }
        Plugin.ExtendedLogging($"Transporter: Transporting to {transportTarget.name}");

        float dist = Vector3.Distance(transportTarget.transform.position, transform.position);

        // Path to the object's position
        smartAgentNavigator.DoPathingToDestination(
            transportTarget.transform.position,
            !objectsToTransport[transportTarget]
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
            SwitchToBehaviourServerRpc((int)TransporterStates.Idle);
            return;
        }

        // Move to that final location
        smartAgentNavigator.DoPathingToDestination(
            currentEndDestination,
            (currentEndDestination.y <= -30 ? true : false)
        );

        // If we reached or nearly reached the drop-off
        if (Vector3.Distance(transform.position, currentEndDestination) <= agent.stoppingDistance + 1.5f)
        {
            currentEndDestination = Vector3.zero;
            transportTarget = null;
            SwitchToBehaviourServerRpc((int)TransporterStates.Idle);
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

        int idx = Random.Range(0, allNodes.Count);
        NavMesh.SamplePosition(allNodes[idx].transform.position, out NavMeshHit hit, 5, NavMesh.AllAreas);
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