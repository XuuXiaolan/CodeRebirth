using System.Collections.Generic;
using System.Linq;
using GameNetcodeStuff;
using Unity.Netcode;
using UnityEngine;

namespace CodeRebirth.src.Content.Enemies;
public class Transporter : CodeRebirthEnemyAI
{
    public Transform palletTransform = null!;
    
    [HideInInspector] public static List<GameObject> objectsToTransport = new();

    private GameObject? transportTarget = null;
    private Vector3 currentEndDestination = Vector3.zero;
    private bool repositioning = false;
    private float speedIncrease = 0f;

    private readonly static int HoldingObjectAnimation = Animator.StringToHash("holdingObject");
    private readonly static int RunSpeedFloat = Animator.StringToHash("RunSpeedFloat");

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
        creatureAnimator.SetFloat(RunSpeedFloat, agent.velocity.magnitude); // todo: turn that into static int thing
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

        if (dist <= agent.stoppingDistance + 0.5f && smartAgentNavigator.checkPathsRoutine == null && !repositioning)
        {
            repositioning = true;
            // Change from IEnumerable to List
            List<(GameObject obj, Vector3 position)> candidateObjects = new();

            // Loop 20 times, pick random nodes, add them to the list
            IEnumerable<GameObject> allNodes = RoundManager.Instance.outsideAINodes
                .Concat(RoundManager.Instance.insideAINodes);

            candidateObjects = allNodes
                .Select(kv => (kv, kv.transform.position))    
                .ToList();
            smartAgentNavigator.CheckPaths(candidateObjects, CheckIfCanReposition);
        }
    }

    public void CheckIfCanReposition(List<GameObject> objects)
    {
        if (transportTarget == null)
        {
            Plugin.Logger.LogError($"Transporter: transportTarget is null??");
            SwitchToBehaviourServerRpc((int)TransporterStates.Idle);
            return;
        }
        if (objects.Count > 0)
        {
            Plugin.ExtendedLogging($"Transporter: Found {objects.Count} objects", (int)Logging_Level.Low);
            currentEndDestination = objects[UnityEngine.Random.Range(0, objects.Count)].transform.position;
            creatureAnimator.SetBool(HoldingObjectAnimation, true);
            agent.speed = 0f;
            transportTarget.transform.SetParent(palletTransform, true);
            SyncPositionRotationOfTransportTargetServerRpc(new NetworkObjectReference(transportTarget));
        }
        else
        {
            repositioning = false;
            currentEndDestination = Vector3.zero;
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
        if (Vector3.Distance(transform.position, currentEndDestination) <= agent.stoppingDistance + 1.5f && currentEndDestination != Vector3.zero)
        {
            Plugin.ExtendedLogging($"Transporter: Dropped off {transportTarget.name}", (int)Logging_Level.Low);
            currentEndDestination = Vector3.zero;
            creatureAnimator.SetBool(HoldingObjectAnimation, false);
        }
    }

    #endregion

    #region RPC's

    [ServerRpc(RequireOwnership = false)]
    public void SyncPositionRotationOfTransportTargetServerRpc(NetworkObjectReference netObjRef)
    {
        SyncPositionRotationOfTransportTargetClientRpc(netObjRef);
    }

    [ClientRpc]
    public void SyncPositionRotationOfTransportTargetClientRpc(NetworkObjectReference netObjRef)
    {
        var _transportTarget = (GameObject)netObjRef;
        _transportTarget.transform.localPosition = Vector3.zero;
        _transportTarget.transform.localRotation = Quaternion.identity;
    }

    #endregion

    #region Example Utility

    public (GameObject obj, Vector3 position) PickRandomOutsideOrInsideAINode()
    {
        IEnumerable<GameObject> allNodes = RoundManager.Instance.outsideAINodes
            .Concat(RoundManager.Instance.insideAINodes);
        if (allNodes.Count() == 0) return (gameObject, transform.position);

        // Pick one randomly
        int index = UnityEngine.Random.Range(0, allNodes.Count());
        GameObject chosenNode = allNodes.ElementAt(index);

        return (chosenNode, chosenNode.transform.position);
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

    public void OnLiftHazardAnimEvent()
    {
        repositioning = false;
        agent.speed = 4 + speedIncrease;
        SwitchToBehaviourStateOnLocalClient((int)TransporterStates.Repositioning);
    }

    public void OnReleaseHazardAnimEvent()
    {
        if (transportTarget == null)
        {
            Plugin.Logger.LogError($"Transporter: transportTarget is null");
            return;
        }
        transportTarget.transform.SetParent(StartOfRound.Instance.propsContainer, true);
        agent.speed = 4 + speedIncrease;
        transportTarget = null;
        SwitchToBehaviourStateOnLocalClient((int)TransporterStates.Idle);

        if (!IsServer) return;
        TryFindAnyTransportableObjectViaAsyncPathfinding();
    }
    #endregion
}