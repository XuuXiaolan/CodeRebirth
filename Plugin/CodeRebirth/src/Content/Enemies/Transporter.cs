using System.Collections;
using System.Collections.Generic;
using System.Linq;
using GameNetcodeStuff;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

namespace CodeRebirth.src.Content.Enemies;
public class Transporter : CodeRebirthEnemyAI
{
    public AudioClip[] engineAndIdleSounds = null!;
    public AudioClip dumpHazardSound = null!;
    public AudioClip hitJimothySound = null!;
    public AudioClip jimHonkSound = null!;
    public AudioClip pickUpHazardSound = null!;

    public Transform palletTransform = null!;
    public Transform jimothyTransform = null!;

    [HideInInspector] public static List<GameObject> objectsToTransport = new();

    private float idleTimer = 20f;
    private Coroutine? onHitRoutine = null;
    private GameObject? transportTarget = null;
    private Scene previousSceneOfTransportTarget = new();
    private bool droppingObject = false;
    private NavMeshHit currentEndHit = new();
    private bool repositioning = false;
    private float speedIncrease = 0f;

    private readonly static int PickUpObjectAnimation = Animator.StringToHash("pickUpObject"); // Trigger
    private readonly static int DropObjectAnimation = Animator.StringToHash("dropObject"); // Trigger
    private readonly static int OnHitAnim = Animator.StringToHash("onHit"); // Trigger
    private readonly static int RunSpeedFloat = Animator.StringToHash("RunSpeedFloat"); // Float
    private readonly static int RotationSpeed = Animator.StringToHash("RotationSpeed"); // Float

    public enum TransporterStates
    {
        Idle,
        Transporting,
        Repositioning
    }

    [HideInInspector] public static List<Transporter> transporters = new();

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        transporters.Add(this);
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        if (transportTarget != null && currentBehaviourStateIndex == (int)TransporterStates.Repositioning)
        {
            var networkObject = transportTarget.GetComponent<NetworkObject>();
            if (networkObject.IsSpawned)
            {
                if (IsServer) networkObject.Despawn();
            }
            else
            {
                Destroy(transportTarget);
            }
        }
        transporters.Remove(this);
    }

    public override void Start()
    {
        base.Start();
        smartAgentNavigator.OnUseEntranceTeleport.AddListener(OnUseEntranceTeleport);
        SwitchToBehaviourStateOnLocalClient((int)TransporterStates.Idle);
        if (!IsServer) return;
        smartAgentNavigator.StartSearchRoutine(20);
        var emptyNetworkObject = GameObject.Instantiate(Plugin.Assets.EmptyNetworkObject, palletTransform.position, Quaternion.identity);
        emptyNetworkObject.GetComponent<NetworkObject>().Spawn();
        SyncNetworkObjectParentServerRpc(new NetworkObjectReference(emptyNetworkObject));
        palletTransform = emptyNetworkObject.transform;
        /*if (!objectsToTransport.Contains(StartOfRound.Instance.shipAnimatorObject.gameObject))
        {
            objectsToTransport.Add(StartOfRound.Instance.shipAnimatorObject.gameObject);
        }*/
        TryFindAnyTransportableObjectViaAsyncPathfinding();
    }

    private void OnUseEntranceTeleport(bool setOutside)
    {
        foreach (GrabbableObject grabbableObject in transform.GetComponentsInChildren<GrabbableObject>())
        {
            if (grabbableObject == null)
                continue;

            grabbableObject.EnableItemMeshes(true);
            grabbableObject.EnablePhysics(true);
        }
        foreach (var player in StartOfRound.Instance.allPlayerScripts)
        {
            if (GameNetworkManager.Instance.localPlayerController != player || player.transform.parent != this.transform)
                continue;

            smartAgentNavigator.lastUsedEntranceTeleport.TeleportPlayer();
            player.transform.position = smartAgentNavigator.lastUsedEntranceTeleport.exitPoint.position;
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void SyncNetworkObjectParentServerRpc(NetworkObjectReference netObjRef)
    {
        SyncNetworkObjectParentClientRpc(netObjRef);
    }

    [ClientRpc]
    public void SyncNetworkObjectParentClientRpc(NetworkObjectReference netObjRef)
    {
        var emptyNetworkObject = (GameObject)netObjRef;
        emptyNetworkObject.transform.SetParent(palletTransform, true);
        emptyNetworkObject.transform.SetPositionAndRotation(palletTransform.position, palletTransform.rotation);
    }

    public override void Update()
    {
        base.Update();
        if (agent.velocity.magnitude > 0.5f && creatureSFX.clip != engineAndIdleSounds[0])
        {
            creatureSFX.clip = engineAndIdleSounds[0];
            creatureSFX.Play();
        }
        else if (creatureSFX.clip != engineAndIdleSounds[1] && agent.velocity.magnitude <= 0.5f)
        {
            creatureSFX.clip = engineAndIdleSounds[1];
            creatureSFX.Play();
        }

        idleTimer -= Time.deltaTime;
        if (idleTimer <= 0)
        {
            idleTimer = 25;
            creatureVoice.PlayOneShot(jimHonkSound);
        }
        if (!IsServer) return;
        creatureAnimator.SetFloat(RunSpeedFloat, agent.velocity.magnitude);
    }

    #region State Behaviors
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
        // Gather all valid objects
        Plugin.ExtendedLogging($"Transporter: Transporting {objectsToTransport.Count} objects");
        IEnumerable<(GameObject obj, Vector3 position)> candidateObjects = objectsToTransport
            .Where(kv => kv != null)
            .Select(kv => (kv, kv.transform.position));

        smartAgentNavigator.CheckPaths(candidateObjects, CheckIfNeedToChangeState);
    }

    public void CheckIfNeedToChangeState(List<GameObject> objects)
    {
        int totalAmount = objects.Count;
        if (totalAmount > 0)
        {
            Plugin.ExtendedLogging($"Transporter: Found {totalAmount} objects");
            transportTarget = objects[UnityEngine.Random.Range(0, totalAmount)];
            objectsToTransport.Remove(transportTarget);
            smartAgentNavigator.StopSearchRoutine();
            SwitchToBehaviourServerRpc((int)TransporterStates.Transporting);
        }
        else
        {
            Plugin.ExtendedLogging($"Transporter: No more objects to transport");
        }
    }

    private void DoTransporting()
    {
        if (transportTarget == null)
        {
            smartAgentNavigator.StartSearchRoutine(20);
            SwitchToBehaviourServerRpc((int)TransporterStates.Idle);
            return;
        }
        // Plugin.ExtendedLogging($"Transporter: Transporting to {transportTarget.name}");

        float dist = Vector3.Distance(transportTarget.transform.position, transform.position);

        // Path to the object's position
        smartAgentNavigator.DoPathingToDestination(
            transportTarget.transform.position
        );

        if (dist <= agent.stoppingDistance && smartAgentNavigator.checkPathsRoutine == null && !repositioning)
        {
            repositioning = true;
            // Change from IEnumerable to List
            IEnumerable<(GameObject obj, Vector3 position)> candidateObjects = [];

            // Loop 20 times, pick random nodes, add them to the list
            IEnumerable<GameObject> allNodes = [.. RoundManager.Instance.outsideAINodes, .. RoundManager.Instance.insideAINodes];

            candidateObjects = allNodes
                .Select(kv => (kv, kv.transform.position));

            smartAgentNavigator.CheckPaths(candidateObjects, CheckIfCanReposition);
        }
    }

    public void CheckIfCanReposition(List<GameObject> objects)
    {
        if (transportTarget == null)
        {
            Plugin.Logger.LogError($"Transporter: transportTarget is null??");
            smartAgentNavigator.StartSearchRoutine(20);
            SwitchToBehaviourServerRpc((int)TransporterStates.Idle);
            return;
        }
        if (objects.Count > 0)
        {
            // Plugin.ExtendedLogging($"Transporter: Found {objects.Count} objects");
            Vector3 currentEndDestination = objects[UnityEngine.Random.Range(0, objects.Count)].transform.position;
            creatureNetworkAnimator.SetTrigger(PickUpObjectAnimation);
            agent.speed = 0f;
            agent.velocity = Vector3.zero;
            previousSceneOfTransportTarget = transportTarget.scene;
            transportTarget.transform.SetParent(palletTransform, true);
            SyncPositionRotationOfTransportTargetServerRpc(new NetworkObjectReference(transportTarget), currentEndDestination);
        }
        else
        {
            repositioning = false;
            currentEndHit.position = Vector3.zero;
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
            currentEndHit.position
        );

        // If we reached or nearly reached the drop-off
        if (Vector3.Distance(transform.position, currentEndHit.position) <= agent.stoppingDistance && !droppingObject)
        {
            // Plugin.ExtendedLogging($"Transporter: Dropped off {transportTarget.name}");
            var directionToLookAt = (currentEndHit.position - transform.position).normalized;
            directionToLookAt.y = 0f;
            transform.LookAt(directionToLookAt);
            agent.velocity = Vector3.zero;
            creatureNetworkAnimator.SetTrigger(DropObjectAnimation);
            droppingObject = true;
        }
    }
    #endregion

    #region RPC's
    [ServerRpc(RequireOwnership = false)]
    public void SyncPositionRotationOfTransportTargetServerRpc(NetworkObjectReference netObjRef, Vector3 currentEndPosition)
    {
        SyncPositionRotationOfTransportTargetClientRpc(netObjRef, currentEndPosition);
    }

    [ClientRpc]
    public void SyncPositionRotationOfTransportTargetClientRpc(NetworkObjectReference netObjRef, Vector3 currentEndPosition)
    {
        var _transportTarget = (GameObject)netObjRef;
        _transportTarget.transform.localPosition = Vector3.zero;
        _transportTarget.transform.localRotation = Quaternion.identity;
        NavMesh.SamplePosition(currentEndPosition, out currentEndHit, 4f, NavMesh.AllAreas);
    }
    #endregion

    #region Combat / Animation
    public override void HitEnemy(int force = 1, PlayerControllerB? playerWhoHit = null, bool playHitSFX = false, int hitID = -1)
    {
        base.HitEnemy(force, playerWhoHit, playHitSFX, hitID);
        creatureVoice.PlayOneShot(hitJimothySound);
        if (onHitRoutine != null || playerWhoHit == null || !IsServer) return;
        onHitRoutine = StartCoroutine(OnHitAnimation(playerWhoHit));
    }

    public IEnumerator OnHitAnimation(PlayerControllerB playerWhoHit)
    {
        creatureNetworkAnimator.SetTrigger(OnHitAnim);
        agent.speed = 0f;
        agent.velocity = Vector3.zero;

        Vector3 direction = (playerWhoHit.transform.position - jimothyTransform.position).normalized;
        direction.y = 0f;

        float angleToPlayer = Vector3.SignedAngle(jimothyTransform.forward, direction, Vector3.up);

        float timeToLookAtPlayer = angleToPlayer <= 0
            ? (angleToPlayer * -1) / 360f
            : (360f - angleToPlayer) / 360f;

        // todo: maybe override animation by doing this in late update
        // Plugin.ExtendedLogging($"Looking at player for {timeToLookAtPlayer} seconds");
        creatureAnimator.SetFloat(RotationSpeed, 1f);
        yield return new WaitForSeconds(timeToLookAtPlayer);
        creatureAnimator.SetFloat(RotationSpeed, 0f);
        yield return new WaitForSeconds(0.5f);
        speedIncrease += 0.5f;
        agent.speed = 4 + speedIncrease;
        creatureAnimator.SetFloat(RotationSpeed, 1f);
        yield return new WaitForSeconds(1 - timeToLookAtPlayer);
        creatureAnimator.SetFloat(RotationSpeed, 0f);

        onHitRoutine = null;
    }

    public void OnLiftOrDropObjectImmediateAnimEvent(int liftNumber)
    {
        if (liftNumber == 0)
        {
            creatureVoice.PlayOneShot(pickUpHazardSound);
        }
        else
        {
            creatureVoice.PlayOneShot(dumpHazardSound);
        }
    }

    public void OnLiftHazardAnimEvent()
    {
        repositioning = false;
        agent.speed = 4 + speedIncrease;
        SwitchToBehaviourStateOnLocalClient((int)TransporterStates.Repositioning);
        if (!IsServer) return;
        smartAgentNavigator.StopSearchRoutine();
    }

    public void OnReleaseHazardAnimEvent()
    {
        if (transportTarget == null)
        {
            Plugin.Logger.LogError($"Transporter: transportTarget is null");
            return;
        }

        transportTarget.transform.SetParent(null, true);
        SceneManager.MoveGameObjectToScene(transportTarget, previousSceneOfTransportTarget);

        transportTarget.transform.position = currentEndHit.position;
        transportTarget.transform.up = currentEndHit.normal;
        currentEndHit = new();
        agent.speed = 4 + speedIncrease;
        droppingObject = false;

        SwitchToBehaviourStateOnLocalClient((int)TransporterStates.Idle);

        if (!IsServer) return;
        objectsToTransport.Add(transportTarget);
        transportTarget = null;
        smartAgentNavigator.StartSearchRoutine(20);
        TryFindAnyTransportableObjectViaAsyncPathfinding();
    }
    #endregion
}