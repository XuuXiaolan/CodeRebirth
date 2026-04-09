using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Dawn;
using Dawn.Utils;
using GameNetcodeStuff;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

namespace CodeRebirth.src.Content.Enemies;

[Serializable]
public class GameObjectWithPriority(GameObject gameObject, int priority) : IWeighted
{
    public GameObject gameObject = gameObject;
    public int priority = priority;

    public int GetWeight()
    {
        return priority;
    }
}

public class Transporter : CodeRebirthEnemyAI
{
    public PlayerPhysicsRegion PhysicsRegion = null!;
    public AudioSource StunSource = null!;
    public AudioClip[] engineAndIdleSounds = null!;
    public AudioClip dumpHazardSound = null!;
    public AudioClip hitJimothySound = null!;
    public AudioClip pickUpHazardSound = null!;

    public Transform palletTransform = null!;
    public Transform jimothyTransform = null!;

    internal static List<GameObjectWithPriority> objectsWithPriorityToTransport = new();

    private Coroutine? onHitRoutine = null;
    private GameObjectWithPriority? transportTarget = null;
    private Scene previousSceneOfTransportTarget = new();
    private bool droppingObject = false;
    private NavMeshHit currentEndHit = new();
    private bool repositioning = false;
    private float speedIncrease = 0f;

    private static readonly int PickUpObjectAnimation = Animator.StringToHash("pickUpObject"); // Trigger
    private static readonly int DropObjectAnimation = Animator.StringToHash("dropObject"); // Trigger
    private static readonly int OnHitAnim = Animator.StringToHash("onHit"); // Trigger
    private static readonly int RunSpeedFloat = Animator.StringToHash("RunSpeedFloat"); // Float
    private static readonly int RotationSpeed = Animator.StringToHash("RotationSpeed"); // Float
    private static readonly int StunnedAnimation = Animator.StringToHash("Stunned"); // Bool

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
            var networkObject = transportTarget.gameObject.GetComponent<NetworkObject>();
            if (networkObject.IsSpawned)
            {
                if (IsServer) networkObject.Despawn();
            }
            else
            {
                Destroy(transportTarget.gameObject);
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

        this.transform.position = smartAgentNavigator.lastUsedEntranceTeleport!.exitScript.transform.position;
        if (GameNetworkManager.Instance.localPlayerController.transform.parent != this.transform)
        {
            return;
        }

        PhysicsRegion.disablePhysicsRegion = true;
        StartCoroutine(ReEnableRegion());

        if (StartOfRound.Instance.CurrentPlayerPhysicsRegions.Contains(PhysicsRegion))
        {
            StartOfRound.Instance.CurrentPlayerPhysicsRegions.Remove(PhysicsRegion);
        }

        smartAgentNavigator.lastUsedEntranceTeleport.TeleportPlayer();
    }

    private IEnumerator ReEnableRegion()
    {
        yield return null;
        smartAgentNavigator.lastUsedEntranceTeleport!.TeleportPlayer();
        yield return new WaitForSeconds(0.2f);
        smartAgentNavigator.lastUsedEntranceTeleport!.TeleportPlayer();
        PhysicsRegion.disablePhysicsRegion = false;
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

    private bool currentlyStunned = false;
    public override void Update()
    {
        base.Update();
        if (stunNormalizedTimer > 0f && !currentlyStunned)
        {
            StunSource.Play();
            currentlyStunned = true;
            if (IsServer)
            {
                creatureAnimator.SetBool(StunnedAnimation, true);
            }
        }


        if (currentlyStunned && stunNormalizedTimer <= 0f)
        {
            StunSource.Stop();
            currentlyStunned = false;
            if (IsServer)
            {
                creatureAnimator.SetBool(StunnedAnimation, false);
            }
        }

        if (currentlyStunned)
        {
            smartAgentNavigator.StopAgent();
            return;
        }

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

        _idleTimer -= Time.deltaTime;
        if (_idleTimer <= 0)
        {
            _idleTimer = enemyRandom.NextFloat(_idleAudioClips.minTime, _idleAudioClips.maxTime);
            creatureVoice.PlayOneShot(_idleAudioClips.audioClips[enemyRandom.Next(_idleAudioClips.audioClips.Length)]);
        }

        creatureAnimator.SetFloat(RunSpeedFloat, agent.velocity.magnitude);
    }

    #region State Behaviors
    public override void DoAIInterval()
    {
        base.DoAIInterval();
        if (smartAgentNavigator.CheckPathsOngoing() || currentlyStunned)
        {
            return;
        }

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
        if (objectsWithPriorityToTransport.Count == 0)
        {
            transportTarget = null;
            return;
        }
    }

    private void TryFindAnyTransportableObjectViaAsyncPathfinding()
    {
        // Gather all valid objects
        Plugin.ExtendedLogging($"Transporter: Transporting {objectsWithPriorityToTransport.Count} objects");
        IEnumerable<(GameObjectWithPriority, Vector3 position)> candidateObjects = objectsWithPriorityToTransport
            .Where(kv => kv.gameObject != null)
            .Select(kv => (kv, kv.gameObject.transform.position));

        smartAgentNavigator.CheckPaths(candidateObjects, CheckIfNeedToChangeState);
    }

    public void CheckIfNeedToChangeState(List<GenericPath<GameObjectWithPriority>> args)
    {
        int totalAmount = args.Count;
        if (totalAmount > 0)
        {
            Plugin.ExtendedLogging($"Transporter: Found {totalAmount} objects");
            List<GameObjectWithPriority> transportableTargets = args.Select(x => x.Generic).ToList();

            transportTarget = enemyHostRandom.NextWeighted(transportableTargets);
            objectsWithPriorityToTransport.Remove(transportTarget);
            smartAgentNavigator.StopSearchRoutine();
            SwitchToBehaviourServerRpc((int)TransporterStates.Transporting);
        }
        else
        {
            droppingObject = false;
            SwitchToBehaviourServerRpc((int)TransporterStates.Idle);
            smartAgentNavigator.StartSearchRoutine(20);
            Plugin.ExtendedLogging($"Transporter: No more objects to transport");
        }
    }

    private void DoTransporting()
    {
        if (transportTarget == null)
        {
            droppingObject = false;
            SwitchToBehaviourServerRpc((int)TransporterStates.Idle);
            TryFindAnyTransportableObjectViaAsyncPathfinding();
            return;
        }
        // Plugin.ExtendedLogging($"Transporter: Transporting to {transportTarget.name}");

        float dist = Vector3.Distance(transportTarget.gameObject.transform.position, transform.position);

        // Path to the object's position
        smartAgentNavigator.DoPathingToDestination(transportTarget.gameObject.transform.position);

        if (dist <= agent.stoppingDistance)
        {
            repositioning = true;
            // Change from IEnumerable to List
            IEnumerable<(GameObject obj, Vector3 position)> candidateObjects = [];

            // Loop 20 times, pick random nodes, add them to the list
            List<GameObject> allNodes = [.. RoundManager.Instance.outsideAINodes, .. RoundManager.Instance.insideAINodes];

            candidateObjects = allNodes
                .Where(kv => kv != null).Select(kv => (kv, kv.transform.position));

            creatureNetworkAnimator.SetTrigger(PickUpObjectAnimation);
            previousSceneOfTransportTarget = transportTarget.gameObject.scene;
            transportTarget.gameObject.transform.SetParent(palletTransform, true);
            SyncPositionRotationOfTransportTargetServerRpc(new NetworkObjectReference(transportTarget.gameObject));
            smartAgentNavigator.StopAgent();
            smartAgentNavigator.CheckPaths(candidateObjects, CheckIfCanReposition);
        }
    }

    public void CheckIfCanReposition(List<GenericPath<GameObject>> args)
    {
        if (transportTarget == null)
        {
            Plugin.Logger.LogError($"Transporter: transportTarget is null??");
            SwitchToBehaviourServerRpc((int)TransporterStates.Idle);
            TryFindAnyTransportableObjectViaAsyncPathfinding();
            return;
        }

        if (args.Count > 0)
        {
            // Plugin.ExtendedLogging($"Transporter: Found {objects.Count} objects");
            Vector3 currentEndDestination = args[UnityEngine.Random.Range(0, args.Count)].Generic.transform.position;
            NavMesh.SamplePosition(currentEndDestination, out currentEndHit, 4f, NavMesh.AllAreas);
            if (repositioning)
            {
                SwitchToBehaviourServerRpc((int)TransporterStates.Repositioning);
            }
        }
        else
        {
            // drop hazard?
        }
    }

    private void DoRepositioning()
    {
        if (droppingObject || repositioning)
            return;

        if (transportTarget == null)
        {
            droppingObject = false;
            SwitchToBehaviourServerRpc((int)TransporterStates.Idle);
            TryFindAnyTransportableObjectViaAsyncPathfinding();
            return;
        }

        // Move to that final location
        smartAgentNavigator.DoPathingToDestination(currentEndHit.position);

        // If we reached or nearly reached the drop-off
        if (Vector3.Distance(transform.position, currentEndHit.position) <= agent.stoppingDistance)
        {
            // Plugin.ExtendedLogging($"Transporter: Dropped off {transportTarget.name}");
            var directionToLookAt = (currentEndHit.position - transform.position).normalized;
            transform.LookAt(directionToLookAt);
            smartAgentNavigator.StopAgent();
            creatureNetworkAnimator.SetTrigger(DropObjectAnimation);
            droppingObject = true;
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
        smartAgentNavigator.StopAgent();
        agent.speed = 0f;

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
        StartCoroutine(WaitUntilFinishedCalculatingPath());
    }

    private IEnumerator WaitUntilFinishedCalculatingPath()
    {
        while (smartAgentNavigator.CheckPathsOngoing())
        {
            yield return null;
        }

        repositioning = false;
        if ((int)currentBehaviourStateIndex == (int)TransporterStates.Repositioning)
            yield break;

        SwitchToBehaviourStateOnLocalClient((int)TransporterStates.Repositioning);
    }

    public void OnReleaseHazardAnimEvent()
    {
        if (transportTarget == null)
        {
            Plugin.Logger.LogError($"Transporter: transportTarget is null");
            return;
        }

        transportTarget.gameObject.transform.SetParent(null, true);
        SceneManager.MoveGameObjectToScene(transportTarget.gameObject, previousSceneOfTransportTarget);

        transportTarget.gameObject.transform.position = currentEndHit.position;
        transportTarget.gameObject.transform.up = currentEndHit.normal;
        droppingObject = false;

        SwitchToBehaviourStateOnLocalClient((int)TransporterStates.Idle);

        if (!IsServer) return;
        objectsWithPriorityToTransport.Add(transportTarget);
        transportTarget = null;
        TryFindAnyTransportableObjectViaAsyncPathfinding();
    }
    #endregion
}