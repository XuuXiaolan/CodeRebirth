using System;
using System.Collections.Generic;
using CodeRebirth.src.MiscScripts.PathFinding;
using CodeRebirth.src.Util.Extensions;
using GameNetcodeStuff;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;
using UnityEngine.AI;

namespace CodeRebirth.src.Content.Unlockables;
[RequireComponent(typeof(SmartAgentNavigator))]
public class GalAI : NetworkBehaviour, IHittable, INoiseListener
{
    public string GalName = "";
    public Animator Animator = null!;
    public NetworkAnimator NetworkAnimator = null!;
    public NavMeshAgent Agent = null!;
    [NonSerialized] public Charger GalCharger = null!;
    public Collider[] colliders = [];
    public AudioSource GalVoice = null!;
    public AudioSource GalSFX = null!;
    public AudioClip ActivateSound = null!;
    public AudioClip GreetOwnerSound = null!;
    public AudioClip[] IdleSounds = [];
    public AudioClip DeactivateSound = null!;
    public AudioClip[] HitSounds = [];
    public AudioClip[] FootstepSounds = [];
    public float DoorOpeningSpeed = 1f;
    public Transform GalHead = null!;
    public Transform GalEye = null!;
    public Renderer[] renderersToHideIn = [];
    public SmartAgentNavigator smartAgentNavigator = null!;

    [HideInInspector] public static List<GalAI> Instances = new();
    [HideInInspector] public bool boomboxPlaying = false;
    [HideInInspector] public float staringTimer = 0f;
    [HideInInspector] public const float stareThreshold = 2f; // Set the threshold to 2 seconds, or adjust as needed
    [HideInInspector] public const float STARE_DOT_THRESHOLD = 0.8f;
    [HideInInspector] public const float STARE_ROTATION_SPEED = 2f;
    [HideInInspector] public EnemyAI? targetEnemy;
    [HideInInspector] public PlayerControllerB? ownerPlayer;
    [HideInInspector] public List<string> enemyTargetBlacklist = new();
    [HideInInspector] public int chargeCount = 10;
    [HideInInspector] public int maxChargeCount;
    [HideInInspector] public bool currentlyAttacking = false;
    [HideInInspector] public float boomboxTimer = 0f;
    [HideInInspector] public bool physicsEnabled = true;
    [HideInInspector] public float idleNeededTimer = 10f;
    [HideInInspector] public float idleTimer = 0f;
    [HideInInspector] public System.Random galRandom = new();
    [HideInInspector] public bool isInHangarShipRoom = true;
    [HideInInspector] public bool inActive = true;
    [HideInInspector] public bool doneOnce = false;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        Instances.Add(this);
        if (!IsServer) return;

        transform.SetParent(GalCharger.transform, false);
        transform.SetPositionAndRotation(GalCharger.transform.position, GalCharger.transform.rotation);
    }

    [ServerRpc(RequireOwnership = false)]
    public void RefillChargesServerRpc()
    {
        RefillChargesClientRpc();
    }

    [ClientRpc]
    public void RefillChargesClientRpc()
    {
        RefillCharges();
    }

    public virtual void RefillCharges()
    {
        chargeCount = maxChargeCount;
    }

    public void DoGalRadarAction(bool enabled)
    {
        if (enabled)
        {
            StartOfRound.Instance.mapScreen.AddTransformAsTargetToRadar(transform, GalName, isNonPlayer: true);
        }
        else
        {
            StartOfRound.Instance.mapScreen.RemoveTargetFromRadar(transform);
        }
        StartOfRound.Instance.mapScreen.SyncOrderOfRadarBoostersInList();
    }

    public virtual void InActiveUpdate()
    {
    }

    private void BoomboxUpdate()
    {
        if (!boomboxPlaying || inActive) return;

        boomboxTimer += Time.deltaTime;
        if (boomboxTimer >= 2f)
        {
            boomboxTimer = 0f;
            boomboxPlaying = false;
        }
    }

    private void IdleUpdate()
    {
        if (inActive) return;
        idleTimer += Time.deltaTime;
        if (idleTimer <= idleNeededTimer) return;

        idleTimer = 0f;
        idleNeededTimer = galRandom.NextFloat(10f, 15f);
        GalSFX.PlayOneShot(IdleSounds[galRandom.Next(0, IdleSounds.Length)]);
        GalVoice.pitch = galRandom.NextFloat(0.9f, 1.1f);
    }

    private void ShipRoomUpdate()
    {
        isInHangarShipRoom = StartOfRound.Instance.shipInnerRoomBounds.bounds.Contains(transform.position);
        if (isInHangarShipRoom) smartAgentNavigator.isOutside = true;
    }

    private void OwnerPlayerUpdate()
    {
        if (ownerPlayer != null && ownerPlayer.isPlayerDead)
        {
            ownerPlayer = null;
        }
    }

    public virtual void Update()
    {
        if (!NetworkObject.IsSpawned) return;
        InActiveUpdate();
        BoomboxUpdate();
        IdleUpdate();
        ShipRoomUpdate();
        OwnerPlayerUpdate();
    }

    public virtual void ActivateGal(PlayerControllerB owner)
    {
        ownerPlayer = owner;
        DoGalRadarAction(true);
        GalVoice.PlayOneShot(ActivateSound);
        smartAgentNavigator.SetAllValues(true);
        smartAgentNavigator.OnEnterOrExitElevator.AddListener(OnEnterOrExitElevator);
        smartAgentNavigator.OnUseEntranceTeleport.AddListener(OnUseEntranceTeleport);
        smartAgentNavigator.OnEnableOrDisableAgent.AddListener(OnEnableOrDisableAgent);
    }

    public virtual void OnEnterOrExitElevator(bool enteredElevator)
    {
        Plugin.ExtendedLogging($"Entered Elevator: {enteredElevator}");
    }

    public virtual void OnEnableOrDisableAgent(bool agentEnabled)
    {
        Plugin.ExtendedLogging($"Enabled Agent: {agentEnabled}");
    }

    public virtual void OnUseEntranceTeleport(bool setOutside)
    {
        Plugin.ExtendedLogging($"Used Entrance Teleport and should be set outside: {setOutside}");
        if (physicsEnabled) EnablePhysics(false);
    }

    public virtual void DeactivateGal()
    {
        ownerPlayer = null;
        DoGalRadarAction(false);
        GalVoice.PlayOneShot(DeactivateSound);
        smartAgentNavigator.ResetAllValues();
        smartAgentNavigator.OnEnterOrExitElevator.RemoveListener(OnEnterOrExitElevator);
        smartAgentNavigator.OnUseEntranceTeleport.RemoveListener(OnUseEntranceTeleport);
        smartAgentNavigator.OnEnableOrDisableAgent.RemoveListener(OnEnableOrDisableAgent);
    }

    public bool GoToChargerAndDeactivate()
    {
        smartAgentNavigator.DoPathingToDestination(GalCharger.ChargeTransform.position);
        if (Vector3.Distance(transform.position, GalCharger.ChargeTransform.position) <= Agent.stoppingDistance ||!Agent.hasPath || Agent.velocity.sqrMagnitude <= 0.01f)
        {
            GalCharger.ActivateGirlServerRpc(-1);
            return true;
        }
        return false;
    }

    public void DoStaringAtOwner(PlayerControllerB ownerPlayer)
    {
        Vector3 directionToDrone = (GalHead.position - ownerPlayer.gameplayCamera.transform.position).normalized;
        float dotProduct = Vector3.Dot(ownerPlayer.gameplayCamera.transform.forward, directionToDrone);

        if (dotProduct <= STARE_DOT_THRESHOLD) // Not staring
        {
            staringTimer = 0f;
            return;
        }

        staringTimer += Time.deltaTime;
        if (staringTimer < stareThreshold) return;

        Vector3 lookDirection = (ownerPlayer.gameplayCamera.transform.position - transform.position).normalized;
        lookDirection.y = 0f;
        Quaternion targetRotation = Quaternion.LookRotation(lookDirection);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * STARE_ROTATION_SPEED);
        if (staringTimer >= stareThreshold + 1.5f || targetRotation == transform.rotation)
        {
            staringTimer = 0f;
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void EnablePhysicsServerRpc(bool enablePhysics)
    {
        EnablePhysicsClientRpc(enablePhysics);
    }

    [ClientRpc]
    public void EnablePhysicsClientRpc(bool enablePhysics)
    {
        EnablePhysics(enablePhysics);
    }

    public void EnablePhysics(bool enablePhysics)
    {
        foreach (Collider collider in colliders)
        {
            collider.enabled = enablePhysics;
        }
        physicsEnabled = enablePhysics;
    }

    [ServerRpc(RequireOwnership = false)]
    public void SetEnemyTargetServerRpc(int enemyID)
    {
        SetEnemyTargetClientRpc(enemyID);
    }

    [ClientRpc]
    public void SetEnemyTargetClientRpc(int enemyID)
    {
        if (enemyID == -1)
        {
            targetEnemy = null;
            Plugin.ExtendedLogging($"Clearing Enemy target on {this}");
            return;
        }
        if (RoundManager.Instance.SpawnedEnemies[enemyID] == null)
        {
            Plugin.ExtendedLogging($"Enemy Index invalid! {this}");
            return;
        }
        targetEnemy = RoundManager.Instance.SpawnedEnemies[enemyID];
        Plugin.ExtendedLogging($"{this} setting target to: {targetEnemy.enemyType.enemyName}");
    }

	public virtual void DetectNoise(Vector3 noisePosition, float noiseLoudness, int timesPlayedInOneSpot = 0, int noiseID = 0)
	{
        if (inActive) return;
		if (noiseID == 5 && !Physics.Linecast(transform.position, noisePosition, StartOfRound.Instance.collidersAndRoomMask))
		{
            boomboxTimer = 0f;
			boomboxPlaying = true;
		}
	}

    public virtual bool Hit(int force, Vector3 hitDirection, PlayerControllerB? playerWhoHit = null, bool playHitSFX = false, int hitID = -1)
    {
        if (inActive) return false;
        PlayHurtSoundServerRpc();
        return true;
    }

    [ServerRpc(RequireOwnership = false)]
    public virtual void PlayHurtSoundServerRpc()
    {
        PlayHurtSoundClientRpc();
    }

    [ClientRpc]
    public virtual void PlayHurtSoundClientRpc()
    {
        GalVoice.PlayOneShot(HitSounds[galRandom.Next(0, HitSounds.Length)]);
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        Instances.Remove(this);
        if (inActive) return;
        DoGalRadarAction(false);
        smartAgentNavigator.ResetAllValues();
        smartAgentNavigator.OnEnterOrExitElevator.RemoveListener(OnEnterOrExitElevator);
        smartAgentNavigator.OnUseEntranceTeleport.RemoveListener(OnUseEntranceTeleport);
        smartAgentNavigator.OnEnableOrDisableAgent.RemoveListener(OnEnableOrDisableAgent);
    }
}